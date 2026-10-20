// ULDB: a toolbar that doesn't scroll or clip when its buttons don't fit (as UDB's ToolStrip):
// the buttons that fit are shown in order and the rest move into a drop-down on an overflow
// button (»), which opens a popup with them. Used for the main toolbar, the mode-controls row
// and the vertical edit-modes toolbar; the status bar keeps its own layout.
//
// The hidden items get their own controls in the popup (made by the same factory as the bar's),
// so nothing moves between parents during layout.

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using WF = System.Windows.Forms;

namespace ULDB.Shell
{
	internal sealed class OverflowToolPanel : Panel
	{
		private static readonly Geometry ChevronDown = Geometry.Parse("M1.5,5.5 L7,11 L12.5,5.5");
		private static readonly Geometry ChevronRight = Geometry.Parse("M5.5,1.5 L11,7 L5.5,12.5");

		private readonly Orientation orientation;
		private readonly Func<WF.ToolStripItem, Control> factory;
		private readonly List<(Control control, WF.ToolStripItem item)> entries = new List<(Control, WF.ToolStripItem)>();
		private readonly Dictionary<WF.ToolStripItem, Control> overflowcontrols = new Dictionary<WF.ToolStripItem, Control>();
		private readonly Button chevron;
		private readonly Flyout flyout;
		private int firsthidden;
		private bool showchevron;

		public OverflowToolPanel(Orientation orientation, Func<WF.ToolStripItem, Control> factory)
		{
			this.orientation = orientation;
			this.factory = factory;
			ClipToBounds = true;

			chevron = new Button
			{
				Content = new PathIcon { Data = orientation == Orientation.Horizontal ? ChevronDown : ChevronRight, Width = 9, Height = 9 },
				Padding = new Thickness(orientation == Orientation.Horizontal ? 3 : 4, orientation == Orientation.Horizontal ? 5 : 3),
				VerticalAlignment = VerticalAlignment.Center,
			};
			chevron.Classes.Add("uldb-tool");
			ToolTip.SetTip(chevron, "More tools");
			flyout = new Flyout { Placement = orientation == Orientation.Horizontal ? PlacementMode.BottomEdgeAlignedRight : PlacementMode.RightEdgeAlignedTop, ShowMode = FlyoutShowMode.Standard };
			flyout.FlyoutPresenterClasses.Add("uldb-overflow");
			flyout.Opening += (s, e) => flyout.Content = BuildOverflow();
			chevron.Flyout = flyout;
			Children.Add(chevron);
		}

		/// <summary>Replaces the items (each with its control, in order).</summary>
		public void SetItems(IEnumerable<(Control control, WF.ToolStripItem item)> items)
		{
			foreach((Control control, WF.ToolStripItem item) e in entries) Children.Remove(e.control);
			entries.Clear();
			overflowcontrols.Clear();
			foreach((Control control, WF.ToolStripItem item) e in items)
			{
				entries.Add(e);
				Children.Insert(Children.Count - 1, e.control); // before the overflow button
			}
			InvalidateMeasure();
		}

		private bool Horizontal => orientation == Orientation.Horizontal;
		private double Length(Size s) => Horizontal ? s.Width : s.Height;
		private double Cross(Size s) => Horizontal ? s.Height : s.Width;

		#region ================== Layout

		protected override Size MeasureOverride(Size available)
		{
			double avail = Horizontal ? available.Width : available.Height;
			double total = 0, cross = 0;
			double[] lengths = new double[entries.Count];
			for(int i = 0; i < entries.Count; i++)
			{
				entries[i].control.Measure(Size.Infinity);
				lengths[i] = entries[i].control.IsVisible ? Length(entries[i].control.DesiredSize) : 0;
				total += lengths[i];
				if(entries[i].control.IsVisible) cross = Math.Max(cross, Cross(entries[i].control.DesiredSize));
			}
			chevron.Measure(Size.Infinity);
			double chevronlength = Length(chevron.DesiredSize);
			cross = Math.Max(cross, Cross(chevron.DesiredSize));

			if(double.IsInfinity(avail) || total <= avail)
			{
				firsthidden = entries.Count;
				showchevron = false;
				return Horizontal ? new Size(total, cross) : new Size(cross, total);
			}

			// What fits beside the overflow button; the run doesn't end on a separator.
			double fit = avail - chevronlength, sum = 0;
			int count = 0;
			while(count < entries.Count && sum + lengths[count] <= fit) { sum += lengths[count]; count++; }
			while(count > 0 && (entries[count - 1].item is WF.ToolStripSeparator || !entries[count - 1].control.IsVisible)) count--;
			firsthidden = count;

			bool anyhidden = false;
			for(int i = count; i < entries.Count; i++)
				if(entries[i].control.IsVisible && !(entries[i].item is WF.ToolStripSeparator)) { anyhidden = true; break; }
			showchevron = anyhidden;
			return Horizontal ? new Size(avail, cross) : new Size(cross, avail);
		}

		protected override Size ArrangeOverride(Size final)
		{
			double pos = 0;
			double crosslength = Horizontal ? final.Height : final.Width;
			for(int i = 0; i < entries.Count; i++)
			{
				Control c = entries[i].control;
				double len = c.IsVisible ? Length(c.DesiredSize) : 0;
				if(i < firsthidden)
				{
					c.Arrange(Horizontal ? new Rect(pos, 0, len, crosslength) : new Rect(0, pos, crosslength, len));
					pos += len;
				}
				else
				{
					// Not shown: placed outside the panel (clipped, and unreachable for the pointer).
					c.Arrange(new Rect(-10000, -10000, Math.Max(0, Horizontal ? len : crosslength), Math.Max(0, Horizontal ? crosslength : len)));
				}
			}

			if(showchevron)
			{
				double len = Length(chevron.DesiredSize);
				double at = Math.Max(pos, (Horizontal ? final.Width : final.Height) - len);
				chevron.Arrange(Horizontal ? new Rect(at, 0, len, crosslength) : new Rect(0, at, crosslength, len));
			}
			else
			{
				chevron.Arrange(new Rect(-10000, -10000, chevron.DesiredSize.Width, chevron.DesiredSize.Height));
			}
			return final;
		}

		#endregion

		/// <summary>Development aid: opens the overflow popup and describes the bar and the popup.</summary>
		internal string DevOpen()
		{
			if(!showchevron) return orientation + " bar: everything fits (" + entries.Count + " items)";
			flyout.ShowAt(chevron);
			int shown = (flyout.Content as Panel)?.Children.Count ?? 0;
			flyout.Hide();
			return orientation + " bar: " + firsthidden + " of " + entries.Count + " items shown; the popup holds " + shown;
		}

		#region ================== Overflow popup

		private Control BuildOverflow()
		{
			Panel content = Horizontal
				? (Panel)new WrapPanel { Orientation = Orientation.Horizontal, MaxWidth = 420 }
				: new StackPanel { Orientation = Orientation.Vertical };
			bool lastseparator = true; // no leading or doubled separators
			for(int i = firsthidden; i < entries.Count; i++)
			{
				WF.ToolStripItem item = entries[i].item;
				if(!item.Available) continue;
				if(item is WF.ToolStripSeparator)
				{
					if(lastseparator) continue;
					lastseparator = true;
				}
				else lastseparator = false;

				if(!overflowcontrols.TryGetValue(item, out Control c)) overflowcontrols[item] = c = factory(item);
				if(c.Parent is Panel old) old.Children.Remove(c);
				content.Children.Add(c);
			}

			// Choosing something closes the popup, as a menu does.
			content.AddHandler(Button.ClickEvent, (s, e) => flyout.Hide());
			return content;
		}

		#endregion
	}
}
