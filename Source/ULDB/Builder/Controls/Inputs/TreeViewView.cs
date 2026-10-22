// ULDB: the view of compat's TreeView model (upstream trees, e.g. the thing type browser's
// MultiSelectTreeview). Visible nodes are flattened into virtualized rows; the model answers
// GetNodeAt/Bounds through the locators installed here, and mouse and keyboard input go to the
// model as WinForms' native tree would send them, so upstream's own selection logic runs.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ULDB.Shell;
using SD = System.Drawing;
using WF = System.Windows.Forms;

namespace ULDB.Controls
{
	internal sealed class TreeViewView : UserControl
	{
		private const double RowHeight = 22;
		private const double IndentWidth = 16;

		private readonly WF.TreeView model;
		private readonly ListBox list;
		private readonly ObservableCollection<Row> rows = new ObservableCollection<Row>();
		private WF.TreeNode lastselected;
		private bool pending;

		/// <summary>Icon for a node when the model's ImageList has no image for it (null for none).</summary>
		public Func<WF.TreeNode, Control> IconProvider { get; set; }

		/// <summary>Highlight the model's SelectedNode (plain trees); multi-select trees colour their nodes instead.</summary>
		public bool HighlightSelectedNode { get; set; } = true;

		/// <summary>The colour upstream uses to mark selected nodes (MultiSelectTreeview.SelectionBackColor).</summary>
		public Func<SD.Color> SelectionColor { get; set; } = () => SD.SystemColors.Highlight;

		public TreeViewView(WF.TreeView model)
		{
			this.model = model;
			list = new ListBox
			{
				// Avalonia asks for a null row when it clears a recycled row (issue #26)
				ItemTemplate = new FuncDataTemplate<Row>((row, _) => row == null ? null : BuildRow(row), true),
				SelectionMode = SelectionMode.Single,
				Focusable = true,
			};
			list.Classes.Add("uldb-tree");
			Content = new Border { Child = list, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), ClipToBounds = true, Classes = { "uldb-grid" } };

			model.NodeLocator = p => NodeAt(p.Y);
			model.NodeBoundsProvider = BoundsOf;
			model.TreeChanged += Schedule;
			model.VisibleChanged += Schedule;
			model.EnabledChanged += Schedule;

			// Mouse: as the native tree, in content coordinates (row index * RowHeight).
			list.ItemsSource = rows;
			list.AddHandler(PointerPressedEvent, OnPointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			list.AddHandler(PointerReleasedEvent, OnPointerReleased, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			list.AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			list.AddHandler(TextInputEvent, (s, e) =>
			{
				foreach(char c in e.Text ?? "") model.RaiseKeyPress(new WF.KeyPressEventArgs(c));
			}, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			list.PointerEntered += (s, e) => model.RaiseMouseEnter();
			list.PointerExited += (s, e) => model.RaiseMouseLeave();
			list.GotFocus += (s, e) => model.RaiseGotFocus();
			list.LostFocus += (s, e) => model.RaiseLostFocus();
			Rebuild();
		}

		private void Schedule(object sender, EventArgs e)
		{
			if(pending) return;
			pending = true;
			Dispatcher.UIThread.Post(Rebuild, DispatcherPriority.Render);
		}

		#region ================== Rows

		private sealed class Row
		{
			public WF.TreeNode Node;
			public int Level;
			public bool HasChildren;
			public bool Expanded;
			public string Text;
			public SD.Color Fore, Back;
			public bool Selected, Editing;
			public int ImageIndex;

			// Whether a row looks the same, so an update can keep it (and the list's scroll position).
			public bool SameAs(Row o) =>
				Node == o.Node && Level == o.Level && HasChildren == o.HasChildren && Expanded == o.Expanded && Text == o.Text &&
				Fore == o.Fore && Back == o.Back && Selected == o.Selected && Editing == o.Editing && ImageIndex == o.ImageIndex;
		}

		private void Rebuild()
		{
			pending = false;
			IsVisible = model.Visible;
			IsEnabled = model.Enabled;

			List<Row> wanted = new List<Row>();
			void Add(WF.TreeNodeCollection nodes, int level)
			{
				foreach(WF.TreeNode n in nodes)
				{
					wanted.Add(new Row
					{
						Node = n, Level = level, HasChildren = n.Nodes.Count > 0, Expanded = n.IsExpanded, Text = n.Text, Fore = n.ForeColor, Back = n.BackColor,
						Selected = (!n.BackColor.IsEmpty && n.BackColor.ToArgb() == SelectionColor().ToArgb()) || (HighlightSelectedNode && n.IsSelected),
						Editing = model.EditingNode == n, ImageIndex = n.ImageIndex,
					});
					if(n.IsExpanded) Add(n.Nodes, level + 1);
				}
			}
			Add(model.Nodes, 0);

			// Apply only what changed (expanding a node inserts its children): the list keeps its
			// scroll position, and rows that look the same aren't rebuilt.
			int start = 0;
			while(start < rows.Count && start < wanted.Count && rows[start].SameAs(wanted[start])) start++;
			int oldend = rows.Count, newend = wanted.Count;
			while(oldend > start && newend > start && rows[oldend - 1].SameAs(wanted[newend - 1])) { oldend--; newend--; }
			for(int i = oldend - 1; i >= start; i--) rows.RemoveAt(i);
			for(int i = start; i < newend; i++) rows.Insert(i, wanted[i]);

			// Scroll when asked (EnsureVisible) or when the selection moved, not on every update.
			WF.TreeNode show = model.TopNode;
			model.TopNode = null;
			if(show == null && model.SelectedNode != lastselected) show = model.SelectedNode;
			lastselected = model.SelectedNode;
			if(show != null)
			{
				int index = -1;
				for(int i = 0; i < rows.Count; i++) if(rows[i].Node == show) { index = i; break; }
				if(index >= 0) list.ScrollIntoView(index);
			}
		}

		private Control BuildRow(Row row)
		{
			bool selected = row.Selected;
			Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto,*"), Height = RowHeight, Margin = new Thickness(row.Level * IndentWidth, 0, 0, 0) };
			g.Classes.Add("uldb-tree-row");
			g.Classes.Set("selected", selected);

			// Expander
			Control expander = new Border { Width = 16, Background = Brushes.Transparent };
			if(row.HasChildren)
			{
				PathIcon arrow = new PathIcon { Width = 8, Height = 8, Data = Geometry.Parse(row.Expanded ? "M0,2 L8,2 L4,7 Z" : "M2,0 L7,4 L2,8 Z"), Opacity = 0.7 };
				expander = new Border { Width = 16, Background = Brushes.Transparent, Child = arrow, Cursor = new Cursor(StandardCursorType.Hand) };
			}
			g.Children.Add(expander);

			// Icon
			Control icon = Icon(row.Node);
			if(icon != null)
			{
				icon.Margin = new Thickness(0, 0, 5, 0);
				icon.VerticalAlignment = VerticalAlignment.Center;
				Grid.SetColumn(icon, 1);
				g.Children.Add(icon);
			}

			Control text;
			if(row.Editing)
			{
				text = LabelEditor(row);
			}
			else
			{
				TextBlock block = new TextBlock { Text = row.Text, VerticalAlignment = VerticalAlignment.Center, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis };
				if(!selected) ModelBinder.SetForeground(block, row.Fore);
				text = block;
			}
			Grid.SetColumn(text, 2);
			g.Children.Add(text);
			if(!string.IsNullOrEmpty(row.Node.ToolTipText)) ToolTip.SetTip(g, row.Node.ToolTipText);
			return g;
		}

		// The inline editor for TreeNode.BeginEdit(): Enter or leaving the box commits, Escape cancels
		// (AfterLabelEdit gets a null label), as WinForms' label editing.
		private Control LabelEditor(Row row)
		{
			TextBox box = new TextBox { Text = row.Node.Text, FontSize = 12, MinHeight = 0, Height = RowHeight - 2, HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 60, Padding = new Thickness(4, 0), VerticalContentAlignment = VerticalAlignment.Center };
			bool done = false;
			void Finish(string label)
			{
				if(done) return;
				done = true;
				model.EndLabelEdit(row.Node, label);
				list.Focus();
			}
			box.KeyDown += (s, e) =>
			{
				if(e.Key == Key.Enter) { Finish(box.Text ?? ""); e.Handled = true; }
				else if(e.Key == Key.Escape) { Finish(null); e.Handled = true; }
			};
			box.LostFocus += (s, e) => Finish(box.Text ?? "");
			box.AttachedToVisualTree += (s, e) => Dispatcher.UIThread.Post(() => { box.Focus(); box.SelectAll(); }, DispatcherPriority.Input);
			return box;
		}

		private Control Icon(WF.TreeNode node)
		{
			int index = node.IsSelected && node.SelectedImageIndex >= 0 ? node.SelectedImageIndex : node.ImageIndex;
			SD.Image image = model.ImageList?.Images[index];
			if(image != null) return BreezeIcons.ForImage(image, 16) ?? new Image { Source = AvaloniaImages.From(image), Width = 16, Height = 16 };
			return IconProvider?.Invoke(node);
		}

		private WF.TreeNode NodeAt(double y)
		{
			int i = (int)Math.Floor(y / RowHeight);
			return i >= 0 && i < rows.Count ? rows[i].Node : null;
		}

		private SD.Rectangle BoundsOf(WF.TreeNode node)
		{
			int i = -1;
			for(int k = 0; k < rows.Count; k++) if(rows[k].Node == node) { i = k; break; }
			if(i < 0) return SD.Rectangle.Empty;
			int x = (int)(rows[i].Level * IndentWidth + 16 + 21);
			return new SD.Rectangle(x, (int)(i * RowHeight), Math.Max(10, (int)Bounds.Width - x), (int)RowHeight);
		}

		#endregion

		#region ================== Development aids

		/// <summary>Development aid (ULDB_SHOW_FORM "tree" steps): a click on a row, sent through the same
		/// pointer handlers as the mouse (on the expander arrow or on the text), clicks = 2 for a double-click.</summary>
		internal void DevClick(int rowindex, bool onexpander, int clicks)
		{
			Control container = list.ContainerFromIndex(rowindex);
			if(container == null) { list.ScrollIntoView(rowindex); container = list.ContainerFromIndex(rowindex); }
			TopLevel top = TopLevel.GetTopLevel(this);
			if(container == null || top == null || rowindex >= rows.Count) { Console.WriteLine("[devstep] tree row " + rowindex + " isn't showing"); return; }
			Row row = rows[rowindex];
			Point inrow = new Point(onexpander ? row.Level * IndentWidth + 4 + 8 : row.Level * IndentWidth + 4 + 60, container.Bounds.Height / 2);
			Point pos = container.TranslatePoint(inrow, top) ?? default;
			// The element under that point is the event's source, as for a real click.
			Visual source = top.GetVisualAt(pos) ?? container;
			Pointer pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
			bool wasexpanded = row.Node.IsExpanded;
			Console.WriteLine("[devstep] tree click row " + rowindex + " \"" + row.Text + "\" at " + (int)inrow.X + "," + (int)inrow.Y + " source=" + source.GetType().Name + " expanded=" + wasexpanded);
			for(int c = 1; c <= clicks; c++)
			{
				PointerPointProperties down = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed);
				((Interactive)source).RaiseEvent(new PointerPressedEventArgs(source, pointer, top, pos, 0, down, KeyModifiers.None, c));
				PointerPointProperties up = new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased);
				((Interactive)source).RaiseEvent(new PointerReleasedEventArgs(source, pointer, top, pos, 0, up, KeyModifiers.None, MouseButton.Left));
			}
			Console.WriteLine("[devstep] tree   → expanded=" + row.Node.IsExpanded + " selected=\"" + model.SelectedNode?.Text + "\"");
		}

		/// <summary>Development aid: types into the label editor (TreeNode.BeginEdit) and presses Enter.</summary>
		internal void DevEdit(string text)
		{
			TextBox box = this.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
			if(box == null) { Console.WriteLine("[devstep] tree: no label editor showing"); return; }
			Console.WriteLine("[devstep] tree editor shows \"" + box.Text + "\" focused=" + box.IsFocused);
			box.Text = text;
			box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter, Source = box });
		}

		/// <summary>Development aid: the index of the first row whose text starts with the given text, or -1.</summary>
		internal int DevFindRow(string prefix)
		{
			for(int i = 0; i < rows.Count; i++) if(rows[i].Text.StartsWith(prefix, StringComparison.Ordinal)) return i;
			return -1;
		}

		/// <summary>Development aid: scrolls the list to a vertical offset in pixels (like the mouse wheel).</summary>
		internal void DevScrollTo(double y)
		{
			if(list.Scroll is ScrollViewer sv) sv.Offset = new Vector(sv.Offset.X, y);
		}

		/// <summary>Development aid: what the tree is showing, to check scrolling, expansion and selection.</summary>
		internal void DevReport(string label)
		{
			double offset = (list.Scroll as ScrollViewer)?.Offset.Y ?? -1;
			string first = rows.Count > 0 ? rows[0].Text : "";
			Console.WriteLine("[devstep] tree " + label + ": rows=" + rows.Count + " scroll=" + (int)offset + " selected=\"" + model.SelectedNode?.Text + "\" editing=" + (model.EditingNode != null)
				+ " expanded=" + string.Join("", rows.Select(r => r.HasChildren ? (r.Expanded ? "-" : "+") : ".").Take(40)));
		}

		#endregion

		#region ================== Input

		// The row under the pointer (from the row element it's over), or -1.
		private int RowAt(PointerEventArgs e)
		{
			for(Visual v = e.Source as Visual; v != null && v != list; v = v.GetVisualParent())
			{
				if(v is StyledElement se && se.DataContext is Row r) return rows.IndexOf(r);
			}
			return -1;
		}

		// Model coordinates: x from the pointer, y in the middle of the row under it (row index *
		// RowHeight), matching NodeLocator and NodeBoundsProvider whatever the rows' real height.
		private Point ContentPoint(PointerEventArgs e)
		{
			int i = RowAt(e);
			double y = i >= 0 ? i * RowHeight + RowHeight / 2 : rows.Count * RowHeight + RowHeight;
			return new Point(e.GetPosition(list).X, y);
		}

		private static WF.MouseButtons Button(PointerPointProperties p)
		{
			if(p.IsLeftButtonPressed) return WF.MouseButtons.Left;
			if(p.IsRightButtonPressed) return WF.MouseButtons.Right;
			if(p.IsMiddleButtonPressed) return WF.MouseButtons.Middle;
			return WF.MouseButtons.None;
		}

		private WF.MouseButtons pressed;

		private void OnPointerPressed(object sender, PointerPressedEventArgs e)
		{
			if(e.Handled || e.Source is Visual src && src.FindAncestorOfType<TextBox>() != null) return; // clicks in the label editor
			Point p = ContentPoint(e);
			pressed = Button(e.GetCurrentPoint(list).Properties);
			list.Focus();

			// The expander arrow (drawn by the view; WinForms' native tree handles it itself), and
			// double-clicking a node with children, toggle it.
			int rowindex = (int)Math.Floor(p.Y / RowHeight);
			if(rowindex >= 0 && rowindex < rows.Count && rows[rowindex].HasChildren && pressed == WF.MouseButtons.Left)
			{
				Row row = rows[rowindex];
				double x0 = row.Level * IndentWidth + 4; // + the list item's left padding
				if((p.X >= x0 && p.X < x0 + 18) || e.ClickCount == 2)
				{
					row.Node.Toggle();
					e.Handled = true;
					if(e.ClickCount != 2) return;
				}
			}
			WF.MouseEventArgs args = new WF.MouseEventArgs(pressed, e.ClickCount, (int)p.X, (int)p.Y, 0);
			if(e.ClickCount == 2) model.RaiseNodeDoubleClick(model.GetNodeAt(args.X, args.Y), args);
			else model.RaiseTreeMouseDown(args);
			e.Handled = true; // selection is the model's (ListBox selection isn't used)
		}

		private void OnPointerReleased(object sender, PointerReleasedEventArgs e)
		{
			Point p = ContentPoint(e);
			model.RaiseTreeMouseUp(new WF.MouseEventArgs(pressed, 1, (int)p.X, (int)p.Y, 0));
			e.Handled = true;
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if(model.EditingNode != null) return; // the label editor takes the keys
			WF.KeyEventArgs args = new WF.KeyEventArgs(ULDB.Input.KeyMapping.ToKeys(e.Key, e.KeyModifiers));
			model.RaiseKeyDown(args);
			if(args.Handled) { e.Handled = true; return; }

			// The native tree's own navigation, for trees that don't handle keys themselves.
			WF.TreeNode sel = model.SelectedNode;
			if(sel == null) return;
			switch(e.Key)
			{
				case Key.Up: if(sel.PrevVisibleNode != null) model.SelectByKeyboard(sel.PrevVisibleNode); e.Handled = true; break;
				case Key.Down: if(sel.NextVisibleNode != null) model.SelectByKeyboard(sel.NextVisibleNode); e.Handled = true; break;
				case Key.Left: if(sel.IsExpanded) sel.Collapse(); else if(sel.Parent != null) model.SelectByKeyboard(sel.Parent); e.Handled = true; break;
				case Key.Right: if(!sel.IsExpanded && sel.Nodes.Count > 0) sel.Expand(); e.Handled = true; break;
			}
		}

		#endregion
	}
}
