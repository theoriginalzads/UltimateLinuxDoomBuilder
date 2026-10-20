// ULDB compatibility layer: System.Windows.Forms.ListBox and ProgressBar as state-only models.
// Upstream fills and reads them (BuilderModes' Map Analysis and Find and Replace); ULDB's views show
// the items and feed selection back, raising SelectedIndexChanged as WinForms does.

using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace System.Windows.Forms
{
	public enum SelectionMode
	{
		None = 0,
		One = 1,
		MultiSimple = 2,
		MultiExtended = 3
	}

	public enum ProgressBarStyle
	{
		Blocks = 0,
		Continuous = 1,
		Marquee = 2
	}

	public class ListBox : Control
	{
		private readonly SortedSet<int> selected = new SortedSet<int>();
		private int updating;

		public const int NoMatches = -1;
		public const int DefaultItemHeight = 13;

		public ListBox()
		{
			Items = new ObjectCollection(this);
			SelectedItems = new SelectedObjectCollection(this);
			SelectedIndices = new SelectedIndexCollection(this);
		}

		public ObjectCollection Items { get; }
		public SelectedObjectCollection SelectedItems { get; }
		public SelectedIndexCollection SelectedIndices { get; }

		public SelectionMode SelectionMode { get; set; } = SelectionMode.One;
		public bool Sorted { get; set; }
		public bool FormattingEnabled { get; set; }
		public bool HorizontalScrollbar { get; set; }
		public bool IntegralHeight { get; set; } = true;
		public int ItemHeight { get; set; } = 16;
		public BorderStyle BorderStyle { get; set; } = BorderStyle.Fixed3D;
		public string DisplayMember { get; set; }

		public event EventHandler SelectedIndexChanged;

		public int SelectedIndex
		{
			get => selected.Count > 0 ? selected.Min : -1;
			set
			{
				if(value == SelectedIndex && selected.Count <= 1) return;
				selected.Clear();
				if(value >= 0 && value < Items.Count) selected.Add(value);
				OnSelectionChanged();
			}
		}

		public object SelectedItem
		{
			get => SelectedIndex >= 0 ? Items[SelectedIndex] : null;
			set => SelectedIndex = value == null ? -1 : Items.IndexOf(value);
		}

		public void ClearSelected() { if(selected.Count == 0) return; selected.Clear(); OnSelectionChanged(); }
		public bool GetSelected(int index) => selected.Contains(index);

		public void SetSelected(int index, bool value)
		{
			if(index < 0 || index >= Items.Count) return;
			if(value && SelectionMode == SelectionMode.One) selected.Clear();
			bool changed = value ? selected.Add(index) : selected.Remove(index);
			if(changed) OnSelectionChanged();
		}

		/// <summary>ULDB: set by the view; maps a point in the list to an item index, or -1.</summary>
		public Func<Point, int> ItemLocator { get; set; }
		public int IndexFromPoint(Point p) => ItemLocator?.Invoke(p) ?? -1;
		public int IndexFromPoint(int x, int y) => IndexFromPoint(new Point(x, y));

		public void BeginUpdate() => updating++;
		public void EndUpdate() { if(updating > 0 && --updating == 0) NotifyStylesChanged(); }

		/// <summary>ULDB: the view's selection, as indices (raises SelectedIndexChanged when it changed).</summary>
		public void SetSelectionFromView(IEnumerable<int> indices)
		{
			SortedSet<int> now = new SortedSet<int>(indices);
			if(now.SetEquals(selected)) return;
			selected.Clear();
			foreach(int i in now) if(i >= 0 && i < Items.Count) selected.Add(i);
			OnSelectionChanged();
		}

		private void OnSelectionChanged()
		{
			SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
			NotifyStylesChanged();
		}

		internal virtual void ItemInserted(int index) { }
		internal virtual void ItemRemoved(int index) { }
		internal virtual void ItemsCleared() { }

		internal void OnItemsChanged()
		{
			selected.RemoveWhere(i => i >= Items.Count);
			if(updating == 0) NotifyStylesChanged();
		}

		public class ObjectCollection : IList
		{
			private readonly ListBox owner;
			private readonly List<object> items = new List<object>();

			internal ObjectCollection(ListBox owner) { this.owner = owner; }

			public int Count => items.Count;
			public object this[int index] { get => items[index]; set { items[index] = value; owner.OnItemsChanged(); } }
			public int Add(object item) { int i = Insert(item); owner.ItemInserted(i); owner.OnItemsChanged(); return i; }
			public int Add(object item, bool isChecked) { int i = Add(item); (owner as CheckedListBox)?.SetItemChecked(i, isChecked); return i; }
			public void AddRange(object[] range) { foreach(object o in range) owner.ItemInserted(Insert(o)); owner.OnItemsChanged(); }
			public void AddRange(ObjectCollection range) => AddRange(range.items.ToArray());
			public void Clear() { items.Clear(); owner.selected.Clear(); owner.ItemsCleared(); owner.OnItemsChanged(); }
			public bool Contains(object item) => items.Contains(item);
			public int IndexOf(object item) => items.IndexOf(item);
			public void Insert(int index, object item) { items.Insert(index, item); owner.ItemInserted(index); owner.OnItemsChanged(); }
			public void Remove(object item) { int i = items.IndexOf(item); if(i >= 0) RemoveAt(i); }

			public void RemoveAt(int index)
			{
				items.RemoveAt(index);
				owner.ItemRemoved(index);
				// Selection indices after the removed item move up by one, as in WinForms.
				List<int> moved = new List<int>();
				foreach(int s in owner.selected) if(s != index) moved.Add(s > index ? s - 1 : s);
				owner.selected.Clear();
				foreach(int s in moved) owner.selected.Add(s);
				owner.OnItemsChanged();
			}

			public void CopyTo(Array array, int index) => ((ICollection)items).CopyTo(array, index);
			public IEnumerator GetEnumerator() => items.GetEnumerator();

			// Sorted lists keep their items in text order, as WinForms does.
			private int Insert(object item)
			{
				if(!owner.Sorted) { items.Add(item); return items.Count - 1; }
				string text = item?.ToString() ?? "";
				int i = 0;
				while(i < items.Count && string.Compare(items[i]?.ToString() ?? "", text, StringComparison.CurrentCulture) <= 0) i++;
				items.Insert(i, item);
				return i;
			}

			bool IList.IsFixedSize => false;
			bool IList.IsReadOnly => false;
			bool ICollection.IsSynchronized => false;
			object ICollection.SyncRoot => this;
		}

		public class SelectedObjectCollection : IEnumerable
		{
			private readonly ListBox owner;
			internal SelectedObjectCollection(ListBox owner) { this.owner = owner; }
			public int Count => owner.selected.Count;
			public object this[int index] { get { int i = 0; foreach(int s in owner.selected) if(i++ == index) return owner.Items[s]; throw new ArgumentOutOfRangeException(nameof(index)); } }
			public void Add(object item) => owner.SetSelected(owner.Items.IndexOf(item), true);
			public void Remove(object item) => owner.SetSelected(owner.Items.IndexOf(item), false);
			public void Clear() => owner.ClearSelected();
			public bool Contains(object item) => owner.selected.Contains(owner.Items.IndexOf(item));
			public IEnumerator GetEnumerator() { foreach(int s in owner.selected) yield return owner.Items[s]; }
		}

		public class SelectedIndexCollection : IEnumerable<int>
		{
			private readonly ListBox owner;
			internal SelectedIndexCollection(ListBox owner) { this.owner = owner; }
			public int Count => owner.selected.Count;
			public int this[int index] { get { int i = 0; foreach(int s in owner.selected) if(i++ == index) return s; throw new ArgumentOutOfRangeException(nameof(index)); } }
			public void Add(int index) => owner.SetSelected(index, true);
			public void Remove(int index) => owner.SetSelected(index, false);
			public void Clear() => owner.ClearSelected();
			public bool Contains(int index) => owner.selected.Contains(index);
			public IEnumerator<int> GetEnumerator() => owner.selected.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}
	}

	public class ItemCheckEventArgs : EventArgs
	{
		public ItemCheckEventArgs(int index, CheckState newCheckValue, CheckState currentValue)
		{
			Index = index;
			NewValue = newCheckValue;
			CurrentValue = currentValue;
		}

		public int Index { get; }
		public CheckState CurrentValue { get; }
		public CheckState NewValue { get; set; }
	}

	public delegate void ItemCheckEventHandler(object sender, ItemCheckEventArgs e);

	/// <summary>A ListBox whose items have a check state. ItemCheck is raised before a state changes and can change it.</summary>
	public class CheckedListBox : ListBox
	{
		private readonly List<CheckState> states = new List<CheckState>();

		public bool CheckOnClick { get; set; }
		public bool ThreeDCheckBoxes { get; set; }

		public event ItemCheckEventHandler ItemCheck;

		public CheckState GetItemCheckState(int index) => states[index];
		public bool GetItemChecked(int index) => states[index] != CheckState.Unchecked;
		public void SetItemChecked(int index, bool value) => SetItemCheckState(index, value ? CheckState.Checked : CheckState.Unchecked);

		public void SetItemCheckState(int index, CheckState value)
		{
			if(index < 0 || index >= states.Count) throw new ArgumentOutOfRangeException(nameof(index));
			ItemCheckEventArgs e = new ItemCheckEventArgs(index, value, states[index]);
			ItemCheck?.Invoke(this, e);
			if(states[index] == e.NewValue) return;
			states[index] = e.NewValue;
			NotifyStylesChanged();
		}

		public List<object> CheckedItems
		{
			get
			{
				List<object> result = new List<object>();
				for(int i = 0; i < states.Count; i++) if(states[i] != CheckState.Unchecked) result.Add(Items[i]);
				return result;
			}
		}

		public List<int> CheckedIndices
		{
			get
			{
				List<int> result = new List<int>();
				for(int i = 0; i < states.Count; i++) if(states[i] != CheckState.Unchecked) result.Add(i);
				return result;
			}
		}

		/// <summary>ULDB: a click on an item's checkbox. Cycles unchecked/checked, as WinForms does (indeterminate stays).</summary>
		public void ToggleFromView(int index)
		{
			if(index < 0 || index >= states.Count) return;
			SetItemCheckState(index, states[index] == CheckState.Unchecked ? CheckState.Checked : CheckState.Unchecked);
		}

		internal override void ItemInserted(int index) => states.Insert(Math.Min(index, states.Count), CheckState.Unchecked);
		internal override void ItemRemoved(int index) { if(index < states.Count) states.RemoveAt(index); }
		internal override void ItemsCleared() => states.Clear();
	}

	public class ProgressBar : Control
	{
		private int value, minimum, maximum = 100;

		public int Minimum { get => minimum; set { minimum = value; Changed(); } }
		public int Maximum { get => maximum; set { maximum = value; Changed(); } }
		public int Value { get => value; set { this.value = Math.Clamp(value, minimum, maximum); Changed(); } }
		public int Step { get; set; } = 10;
		public ProgressBarStyle Style { get; set; } = ProgressBarStyle.Blocks;
		public int MarqueeAnimationSpeed { get; set; } = 100;

		public void PerformStep() => Value = Math.Min(maximum, value + Step);
		public void Increment(int amount) => Value = value + amount;

		private void Changed() => NotifyStylesChanged();
	}
}
