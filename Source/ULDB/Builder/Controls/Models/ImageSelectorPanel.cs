// ULDB: the state of upstream's ImageSelectorPanel, the scrolling grid of image tiles in the
// texture and flat browsers. Items, groups, selection and settings are kept here with upstream's
// API; ImageBrowserControlView shows and drives them (layout, scrolling, drawing, keyboard).

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	internal class ImageSelectorPanel : Panel
	{
		private readonly List<ImageBrowserItem> items = new List<ImageBrowserItem>();
		private readonly List<ImageBrowserItem> selection = new List<ImageBrowserItem>();
		private readonly List<ImageBrowserItemGroup> itemgroups = new List<ImageBrowserItemGroup>();
		private int imagesize = 128;
		private bool classicview, usedtexturesfirst, centeritem;
		private bool allowselection = true;
		private bool allowmultipleselection;

		public delegate void ItemSelectedEventHandler(object sender, ImageBrowserItem item);
		public delegate void SelectionChangedEventHandler(object sender, List<ImageBrowserItem> selection);

		public event SelectionChangedEventHandler SelectionChanged;
		public event ItemSelectedEventHandler ItemDoubleClicked;

		/// <summary>The view should scroll to the item (second argument: centre it).</summary>
		internal event Action<ImageBrowserItem, bool> ScrollRequested;
		/// <summary>The view should move the selection by one tile in a direction.</summary>
		internal event Action FocusRequested;
		/// <summary>The view should jump between the two copies of an item.</summary>
		internal event Action<ImageBrowserItem> JumpRequested;
		/// <summary>The list, its groups or its look changed; the view rebuilds.</summary>
		internal event Action ContentChanged;

		public bool HideSelection
		{
			get { return !allowselection; }
			set
			{
				allowselection = !value;
				if(!allowselection && selection.Count > 0) { selection.Clear(); Refresh(); }
			}
		}

		public bool MultiSelect
		{
			get { return allowmultipleselection; }
			set
			{
				allowmultipleselection = value;
				if(!allowmultipleselection && selection.Count > 1)
				{
					ImageBrowserItem first = selection[0];
					selection.Clear();
					selection.Add(first);
					Refresh();
				}
			}
		}

		public int ImageSize { get { return imagesize; } set { imagesize = value; ContentChanged?.Invoke(); } }
		public bool ClassicView { get { return classicview; } set { classicview = value; ContentChanged?.Invoke(); } }
		public bool UsedTexturesFirst { get { return usedtexturesfirst; } set { usedtexturesfirst = value; ContentChanged?.Invoke(); } }
		public bool CenterItem { get { return centeritem; } set { centeritem = value; } }

		public List<ImageBrowserItem> Items { get { return items; } }
		public List<ImageBrowserItem> SelectedItems { get { return selection; } }
		internal List<ImageBrowserItemGroup> ItemGroups { get { return itemgroups; } }
		public string Title { get; set; } = "All images:";
		public string ContentType { get; set; } = "Textures";

		public void Clear()
		{
			selection.Clear();
			items.Clear();
			itemgroups.Clear();
			ContentChanged?.Invoke();
		}

		public void ClearSelection()
		{
			selection.Clear();
			SelectionChanged?.Invoke(this, selection);
			Refresh();
		}

		public void SetItems(IEnumerable<ImageBrowserItemGroup> groups)
		{
			items.Clear();
			selection.Clear();
			itemgroups.Clear();
			foreach(ImageBrowserItemGroup g in groups)
			{
				items.AddRange(g.Items);
				itemgroups.Add(g);
			}
			SelectionChanged?.Invoke(this, selection);
			ContentChanged?.Invoke();
		}

		public void SetItems(IEnumerable<ImageBrowserItem> newitems)
		{
			items.Clear();
			selection.Clear();
			itemgroups.Clear();
			items.AddRange(newitems);
			SelectionChanged?.Invoke(this, selection);
			ContentChanged?.Invoke();
		}

		public void SetSelectedItem(ImageBrowserItem item)
		{
			SetSelectedItems(new List<ImageBrowserItem> { item });
		}

		public void SetSelectedItems(List<ImageBrowserItem> newitems)
		{
			selection.Clear();
			if(newitems.Count > 0)
			{
				selection.AddRange(newitems);
				ScrollToItem(newitems[0]);
				Refresh();
			}
			SelectionChanged?.Invoke(this, selection);
		}

		public void ScrollToItem(ImageBrowserItem item) { ScrollToItem(item, centeritem); }
		public void ScrollToItem(ImageBrowserItem item, bool center) { ScrollRequested?.Invoke(item, center); }
		public void JumpBetweenItems(ImageBrowserItem item) { JumpRequested?.Invoke(item); }
		internal void UpdateRectangles() { ContentChanged?.Invoke(); }

		/// <summary>Selecting with the pointer or the keys (no scroll; the view has it in sight).</summary>
		internal void Select(ImageBrowserItem item, bool add)
		{
			if(!allowselection) return;
			if(add && allowmultipleselection)
			{
				if(!selection.Remove(item)) selection.Add(item);
			}
			else
			{
				selection.Clear();
				selection.Add(item);
			}
			SelectionChanged?.Invoke(this, selection);
			Refresh();
		}

		internal void DoubleClick(ImageBrowserItem item) { ItemDoubleClicked?.Invoke(this, item); }

		/// <summary>Previews finished loading (upstream's refresh timer): the view redraws.</summary>
		internal event Action Redraw;
		public override void Invalidate() { Redraw?.Invoke(); }

		public override bool Focus() { FocusRequested?.Invoke(); return true; }

		public static void ClearCachedPreviews() { }
	}
}
