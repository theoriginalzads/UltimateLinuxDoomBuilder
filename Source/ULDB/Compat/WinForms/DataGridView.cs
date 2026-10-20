// ULDB compatibility layer: a state-only model of WinForms' DataGridView, so upstream code that
// fills and edits grids (the UDMF custom fields editor, statistics windows) runs unchanged.
//
// It keeps rows, cells, columns, selection, the current cell and edit state, and raises the
// events upstream subscribes to. An Avalonia view shows it and drives it with the Raise… and
// edit methods (RaiseCellClick, BeginEdit/SetEditingValue/EndEdit, RaiseUserDeletingRow, …).
//
// Behaviour kept from WinForms where upstream depends on it:
//  - AllowUserToAddRows keeps a trailing "new row" (NewRowIndex); Rows.Add inserts before it
//  - EditMode/BeginEdit raise CellBeginEdit (cancelable), EndEdit raises CellEndEdit
//  - FullRowSelect selection follows CurrentCell; SelectionChanged is raised on change

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms
{
	#region ================== Grid

	public class DataGridView : Control, ISupportInitialize
	{
		private bool allowusertoaddrows = true;
		private DataGridViewCell currentcell;
		private bool editing;

		public DataGridView()
		{
			Columns = new DataGridViewColumnCollection(this);
			Rows = new DataGridViewRowCollection(this);
			Rows.EnsureNewRow();
		}

		public void BeginInit() { }
		public void EndInit() { Rows.EnsureNewRow(); }

		#region Properties

		public DataGridViewColumnCollection Columns { get; }
		public DataGridViewRowCollection Rows { get; }
		public DataGridViewRow RowTemplate { get; set; } = new DataGridViewRow();

		public bool AllowUserToAddRows
		{
			get => allowusertoaddrows;
			set { if(allowusertoaddrows == value) return; allowusertoaddrows = value; Rows.EnsureNewRow(); OnGridChanged(); }
		}

		/// <summary>Index of the trailing "new row", or -1 when users can't add rows.</summary>
		public int NewRowIndex => allowusertoaddrows && Rows.Count > 0 && Rows[Rows.Count - 1].IsNewRow ? Rows.Count - 1 : -1;

		public bool AllowUserToDeleteRows { get; set; } = true;
		public bool AllowUserToResizeColumns { get; set; } = true;
		public bool AllowUserToResizeRows { get; set; } = true;
		public bool AllowUserToOrderColumns { get; set; }
		public Color BackgroundColor { get; set; } = SystemColors.AppWorkspace;
		public Color GridColor { get; set; } = SystemColors.ControlDark;
		public BorderStyle BorderStyle { get; set; } = BorderStyle.FixedSingle;
		public DataGridViewCellBorderStyle CellBorderStyle { get; set; } = DataGridViewCellBorderStyle.Single;
		public DataGridViewClipboardCopyMode ClipboardCopyMode { get; set; }
		public DataGridViewHeaderBorderStyle ColumnHeadersBorderStyle { get; set; }
		public DataGridViewHeaderBorderStyle RowHeadersBorderStyle { get; set; }
		public DataGridViewColumnHeadersHeightSizeMode ColumnHeadersHeightSizeMode { get; set; }
		public DataGridViewRowHeadersWidthSizeMode RowHeadersWidthSizeMode { get; set; }
		public int ColumnHeadersHeight { get; set; } = 23;
		public bool ColumnHeadersVisible { get; set; } = true;
		public bool RowHeadersVisible { get; set; } = true;
		public bool ShowCellErrors { get; set; } = true;
		public bool ShowRowErrors { get; set; } = true;
		public int RowHeadersWidth { get; set; } = 41;
		public DataGridViewCellStyle ColumnHeadersDefaultCellStyle { get; set; } = new DataGridViewCellStyle();
		public DataGridViewCellStyle RowHeadersDefaultCellStyle { get; set; } = new DataGridViewCellStyle();
		public DataGridViewCellStyle DefaultCellStyle { get; set; } = new DataGridViewCellStyle();
		public DataGridViewCellStyle RowsDefaultCellStyle { get; set; } = new DataGridViewCellStyle();
		public DataGridViewCellStyle AlternatingRowsDefaultCellStyle { get; set; } = new DataGridViewCellStyle();
		public DataGridViewEditMode EditMode { get; set; } = DataGridViewEditMode.EditOnKeystrokeOrF2;
		public DataGridViewSelectionMode SelectionMode { get; set; } = DataGridViewSelectionMode.RowHeaderSelect;
		public DataGridViewAutoSizeColumnsMode AutoSizeColumnsMode { get; set; }
		public DataGridViewAutoSizeRowsMode AutoSizeRowsMode { get; set; }
		public bool MultiSelect { get; set; } = true;
		public bool ReadOnly { get; set; }
		public ScrollBars ScrollBars { get; set; } = ScrollBars.Both;
		public bool EnableHeadersVisualStyles { get; set; } = true;
		public bool ShowCellToolTips { get; set; } = true;
		public bool ShowEditingIcon { get; set; } = true;
		public bool StandardTab { get; set; }
		public int FirstDisplayedScrollingRowIndex { get; set; }
		public int RowCount { get => Rows.Count; }
		public int ColumnCount { get => Columns.Count; }

		/// <summary>Row height the views use (for GetCellDisplayRectangle).</summary>
		public const int ViewRowHeight = 22;

		public DataGridViewCell CurrentCell
		{
			get => currentcell;
			set
			{
				if(currentcell == value) return;
				if(editing) EndEdit();
				currentcell = value;
				if(value != null && SelectionMode == DataGridViewSelectionMode.FullRowSelect && value.OwningRow != null)
					SelectOnly(value.OwningRow);
				OnGridChanged();
			}
		}

		public DataGridViewRow CurrentRow => currentcell?.OwningRow;
		public Point CurrentCellAddress => currentcell == null ? new Point(-1, -1) : new Point(currentcell.ColumnIndex, currentcell.RowIndex);
		public bool IsCurrentCellInEditMode => editing;

		public DataGridViewSelectedRowCollection SelectedRows
		{
			get
			{
				DataGridViewSelectedRowCollection result = new DataGridViewSelectedRowCollection();
				// WinForms lists the most recently selected first; with single selection that's the only one.
				for(int i = Rows.Count - 1; i >= 0; i--) if(Rows[i].Selected) result.Add(Rows[i]);
				return result;
			}
		}

		public DataGridViewSelectedCellCollection SelectedCells
		{
			get
			{
				DataGridViewSelectedCellCollection result = new DataGridViewSelectedCellCollection();
				foreach(DataGridViewRow r in Rows)
					foreach(DataGridViewCell c in r.Cells)
						if(r.Selected || c.Selected) result.Add(c);
				return result;
			}
		}

		public DataGridViewColumn SortedColumn { get; private set; }
		public SortOrder SortOrder { get; private set; } = SortOrder.None;

		#endregion

		#region Events

		public event DataGridViewCellCancelEventHandler CellBeginEdit;
		public event DataGridViewCellEventHandler CellEndEdit;
		public event DataGridViewCellEventHandler CellClick;
		public event DataGridViewCellEventHandler CellDoubleClick;
		public event DataGridViewCellEventHandler CellContentClick;
		public event DataGridViewCellEventHandler CellContentDoubleClick;
		public event DataGridViewCellEventHandler CellValueChanged;
		public event DataGridViewCellEventHandler CellEnter;
		public event DataGridViewCellEventHandler CellLeave;
		public event DataGridViewCellMouseEventHandler CellMouseClick;
		public event DataGridViewCellMouseEventHandler CellMouseDoubleClick;
		public event DataGridViewCellMouseEventHandler ColumnHeaderMouseClick;
		public event DataGridViewRowCancelEventHandler UserDeletingRow;
		public event DataGridViewRowEventHandler UserDeletedRow;
		public event DataGridViewRowEventHandler UserAddedRow;
		public event DataGridViewDataErrorEventHandler DataError;
		public event DataGridViewRowsAddedEventHandler RowsAdded;
		public event DataGridViewRowsRemovedEventHandler RowsRemoved;
		public event EventHandler SelectionChanged;
		public event EventHandler CurrentCellDirtyStateChanged;
		public event ScrollEventHandler Scroll;
		public event DataGridViewCellFormattingEventHandler CellFormatting;
		public event DataGridViewCellPaintingEventHandler CellPainting;
		public event DataGridViewEditingControlShowingEventHandler EditingControlShowing;
		public event DataGridViewSortCompareEventHandler SortCompare;

		/// <summary>ULDB: raised when rows, cells, selection or edit state change, so views refresh.</summary>
		public event EventHandler GridChanged;

		internal void OnGridChanged() => GridChanged?.Invoke(this, EventArgs.Empty);
		internal void OnRowsAdded(int index, int count) => RowsAdded?.Invoke(this, new DataGridViewRowsAddedEventArgs(index, count));
		internal void OnRowsRemoved(int index, int count) => RowsRemoved?.Invoke(this, new DataGridViewRowsRemovedEventArgs(index, count));
		internal void OnCellValueChanged(DataGridViewCell cell)
		{
			if(cell.RowIndex >= 0) CellValueChanged?.Invoke(this, new DataGridViewCellEventArgs(cell.ColumnIndex, cell.RowIndex));
			OnGridChanged();
		}

		#endregion

		#region Methods

		public void ClearSelection()
		{
			bool changed = false;
			foreach(DataGridViewRow r in Rows)
			{
				if(r.Selected) { r.SetSelectedSilently(false); changed = true; }
				foreach(DataGridViewCell c in r.Cells) if(c.Selected) { c.Selected = false; changed = true; }
			}
			if(changed) { SelectionChanged?.Invoke(this, EventArgs.Empty); OnGridChanged(); }
		}

		internal void SelectOnly(DataGridViewRow row)
		{
			bool changed = false;
			foreach(DataGridViewRow r in Rows)
			{
				bool want = r == row;
				if(r.Selected != want) { r.SetSelectedSilently(want); changed = true; }
			}
			if(changed) { SelectionChanged?.Invoke(this, EventArgs.Empty); OnGridChanged(); }
		}

		internal void OnRowSelectionChanged()
		{
			SelectionChanged?.Invoke(this, EventArgs.Empty);
			OnGridChanged();
		}

		/// <summary>Starts editing the current cell (CellBeginEdit may cancel). The view shows an editor.</summary>
		public bool BeginEdit(bool selectAll)
		{
			if(currentcell == null || currentcell.ReadOnly || ReadOnly) return false;
			if(editing) return true;
			DataGridViewCellCancelEventArgs e = new DataGridViewCellCancelEventArgs(currentcell.ColumnIndex, currentcell.RowIndex);
			CellBeginEdit?.Invoke(this, e);
			if(e.Cancel) return false;
			editing = true;
			EditSelectAll = selectAll;
			EditingValue = currentcell.Value;
			OnGridChanged();
			return true;
		}

		/// <summary>ULDB: whether the editor should select all its text when it opens.</summary>
		public bool EditSelectAll { get; private set; }

		/// <summary>ULDB: the value in the editor; the view sets it while the user types.</summary>
		public object EditingValue { get; set; }

		/// <summary>Commits the edit: the cell gets the editor's value, then CellEndEdit is raised.</summary>
		public bool EndEdit()
		{
			if(!editing) return true;
			editing = false;
			DataGridViewCell cell = currentcell;
			if(cell != null)
			{
				if(!Equals(cell.Value, EditingValue)) cell.Value = EditingValue;

				// WinForms: editing the "new row" commits it as a normal row and appends a fresh new row.
				DataGridViewRow row = cell.OwningRow;
				if(row != null && row.IsNewRow && !string.IsNullOrEmpty(cell.Value?.ToString()))
				{
					row.IsNewRow = false;
					Rows.EnsureNewRow();
					UserAddedRow?.Invoke(this, new DataGridViewRowEventArgs(Rows[NewRowIndex]));
				}

				CellEndEdit?.Invoke(this, new DataGridViewCellEventArgs(cell.ColumnIndex, cell.RowIndex));
			}
			OnGridChanged();
			return true;
		}

		public bool EndEdit(DataGridViewDataErrorContexts context) => EndEdit();
		public bool CommitEdit(DataGridViewDataErrorContexts context) => true;

		/// <summary>Abandons the edit: the cell keeps its value; CellEndEdit is still raised (as WinForms does).</summary>
		public bool CancelEdit()
		{
			if(!editing) return true;
			EditingValue = currentcell?.Value;
			return EndEdit();
		}

		public void Sort(DataGridViewColumn column, ListSortDirection direction)
		{
			if(column == null) return;
			if(editing) EndEdit();
			int col = column.Index;
			DataGridViewRow newrow = NewRowIndex >= 0 ? Rows[NewRowIndex] : null;
			List<DataGridViewRow> rows = new List<DataGridViewRow>();
			foreach(DataGridViewRow r in Rows) if(r != newrow) rows.Add(r);

			Comparison<DataGridViewRow> compare = (a, b) =>
			{
				object va = a.Cells[col].Value, vb = b.Cells[col].Value;
				if(SortCompare != null)
				{
					DataGridViewSortCompareEventArgs args = new DataGridViewSortCompareEventArgs(column, va, vb, a.Index, b.Index);
					SortCompare(this, args);
					if(args.Handled) return args.SortResult;
				}
				if(va is IComparable ca && vb != null && va.GetType() == vb.GetType()) return ca.CompareTo(vb);
				return string.Compare(va?.ToString(), vb?.ToString(), StringComparison.CurrentCulture);
			};
			// Stable sort, like WinForms keeps equal rows in place
			List<KeyValuePair<int, DataGridViewRow>> indexed = new List<KeyValuePair<int, DataGridViewRow>>();
			for(int i = 0; i < rows.Count; i++) indexed.Add(new KeyValuePair<int, DataGridViewRow>(i, rows[i]));
			indexed.Sort((a, b) =>
			{
				int c = compare(a.Value, b.Value);
				if(direction == ListSortDirection.Descending) c = -c;
				return c != 0 ? c : a.Key.CompareTo(b.Key);
			});

			Rows.Reorder(indexed.ConvertAll(p => p.Value), newrow);
			SortedColumn = column;
			SortOrder = direction == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending;
			OnGridChanged();
		}

		public void Sort(IComparer comparer)
		{
			DataGridViewRow newrow = NewRowIndex >= 0 ? Rows[NewRowIndex] : null;
			List<DataGridViewRow> rows = new List<DataGridViewRow>();
			foreach(DataGridViewRow r in Rows) if(r != newrow) rows.Add(r);
			rows.Sort((a, b) => comparer.Compare(a, b));
			Rows.Reorder(rows, newrow);
			OnGridChanged();
		}

		/// <summary>Cell bounds as the views lay them out: visible columns side by side, rows ViewRowHeight high.</summary>
		public Rectangle GetCellDisplayRectangle(int columnIndex, int rowIndex, bool cutOverflow)
		{
			int x = RowHeadersVisible ? RowHeadersWidth : 0;
			for(int i = 0; i < columnIndex && i < Columns.Count; i++) if(Columns[i].Visible) x += Columns[i].Width;
			int y = (ColumnHeadersVisible ? ColumnHeadersHeight : 0) + (rowIndex - FirstDisplayedScrollingRowIndex) * ViewRowHeight;
			int w = columnIndex >= 0 && columnIndex < Columns.Count ? Columns[columnIndex].Width : 0;
			return new Rectangle(x, y, w, ViewRowHeight);
		}

		public Rectangle GetRowDisplayRectangle(int rowIndex, bool cutOverflow)
		{
			int y = (ColumnHeadersVisible ? ColumnHeadersHeight : 0) + (rowIndex - FirstDisplayedScrollingRowIndex) * ViewRowHeight;
			return new Rectangle(0, y, Width, ViewRowHeight);
		}

		/// <summary>ULDB: set by the view; maps a point in the grid to (row, column), or (-1, -1) for none.</summary>
		public Func<int, int, (int row, int column)> CellLocator { get; set; }

		public HitTestInfo HitTest(int x, int y)
		{
			(int row, int column) = CellLocator?.Invoke(x, y) ?? (-1, -1);
			return new HitTestInfo(row, column);
		}

		public void AutoResizeColumns() { }
		public void AutoResizeColumns(DataGridViewAutoSizeColumnsMode mode) { }
		public void AutoResizeRows() { }
		public void AutoResizeColumn(int columnIndex) { }
		public void InvalidateRow(int rowIndex) => OnGridChanged();
		public void InvalidateCell(DataGridViewCell cell) => OnGridChanged();
		public void InvalidateColumn(int columnIndex) => OnGridChanged();
		public void UpdateCellValue(int columnIndex, int rowIndex) => OnGridChanged();
		public void NotifyCurrentCellDirty(bool dirty) => CurrentCellDirtyStateChanged?.Invoke(this, EventArgs.Empty);
		public void SelectAll()
		{
			foreach(DataGridViewRow r in Rows) r.SetSelectedSilently(true);
			OnRowSelectionChanged();
		}

		public class HitTestInfo
		{
			public HitTestInfo() : this(-1, -1) { }
			internal HitTestInfo(int row, int column) { RowIndex = row; ColumnIndex = column; }
			public int RowIndex { get; }
			public int ColumnIndex { get; }
			public DataGridViewHitTestType Type => RowIndex >= 0 && ColumnIndex >= 0 ? DataGridViewHitTestType.Cell : DataGridViewHitTestType.None;
		}

		#endregion

		#region View input (ULDB)

		private DataGridViewCell CellAt(int columnIndex, int rowIndex)
		{
			if(rowIndex < 0 || rowIndex >= Rows.Count || columnIndex < 0 || columnIndex >= Rows[rowIndex].Cells.Count) return null;
			return Rows[rowIndex].Cells[columnIndex];
		}

		/// <summary>A click on a cell: makes it current (selecting its row), then raises CellMouseClick and CellClick.</summary>
		public void RaiseCellClick(int columnIndex, int rowIndex, MouseButtons button = MouseButtons.Left)
		{
			DataGridViewCell cell = CellAt(columnIndex, rowIndex);
			if(cell != null) CurrentCell = cell;
			CellMouseClick?.Invoke(this, new DataGridViewCellMouseEventArgs(columnIndex, rowIndex, 0, 0, new MouseEventArgs(button, 1, 0, 0, 0)));
			CellClick?.Invoke(this, new DataGridViewCellEventArgs(columnIndex, rowIndex));
			CellContentClick?.Invoke(this, new DataGridViewCellEventArgs(columnIndex, rowIndex));
			OnMouseUp(new MouseEventArgs(button, 1, 0, 0, 0));
		}

		public void RaiseCellDoubleClick(int columnIndex, int rowIndex)
		{
			CellMouseDoubleClick?.Invoke(this, new DataGridViewCellMouseEventArgs(columnIndex, rowIndex, 0, 0, new MouseEventArgs(MouseButtons.Left, 2, 0, 0, 0)));
			CellDoubleClick?.Invoke(this, new DataGridViewCellEventArgs(columnIndex, rowIndex));
			CellContentDoubleClick?.Invoke(this, new DataGridViewCellEventArgs(columnIndex, rowIndex));
		}

		public void RaiseColumnHeaderMouseClick(int columnIndex, MouseButtons button = MouseButtons.Left)
		{
			ColumnHeaderMouseClick?.Invoke(this, new DataGridViewCellMouseEventArgs(columnIndex, -1, 0, 0, new MouseEventArgs(button, 1, 0, 0, 0)));
		}

		/// <summary>The user pressed Delete on a row: UserDeletingRow may cancel; otherwise it's removed.</summary>
		public void RaiseUserDeletingRow(DataGridViewRow row)
		{
			if(row == null || row.IsNewRow || !AllowUserToDeleteRows) return;
			DataGridViewRowCancelEventArgs e = new DataGridViewRowCancelEventArgs(row);
			UserDeletingRow?.Invoke(this, e);
			if(e.Cancel) return;
			Rows.Remove(row);
			UserDeletedRow?.Invoke(this, new DataGridViewRowEventArgs(row));
		}

		public void RaiseScroll(int newValue)
		{
			int old = FirstDisplayedScrollingRowIndex;
			FirstDisplayedScrollingRowIndex = newValue;
			Scroll?.Invoke(this, new ScrollEventArgs(ScrollEventType.ThumbPosition, old, newValue, ScrollOrientation.VerticalScroll));
		}

		public void RaiseDataError(Exception exception, int columnIndex, int rowIndex)
		{
			DataGridViewDataErrorEventArgs e = new DataGridViewDataErrorEventArgs(exception, columnIndex, rowIndex, DataGridViewDataErrorContexts.Commit);
			DataError?.Invoke(this, e);
		}

		#endregion
	}

	#endregion

	#region ================== Rows

	public class DataGridViewRowCollection : IEnumerable<DataGridViewRow>, IList
	{
		private readonly DataGridView owner;
		private readonly List<DataGridViewRow> rows = new List<DataGridViewRow>();

		internal DataGridViewRowCollection(DataGridView owner) { this.owner = owner; }

		public int Count => rows.Count;
		public DataGridViewRow this[int index] => rows[index];

		// Index where new rows go: before the trailing "new row".
		private int AppendIndex => rows.Count > 0 && rows[rows.Count - 1].IsNewRow ? rows.Count - 1 : rows.Count;

		internal void EnsureNewRow()
		{
			bool has = rows.Count > 0 && rows[rows.Count - 1].IsNewRow;
			if(owner.AllowUserToAddRows && !has)
			{
				DataGridViewRow newrow = new DataGridViewRow { IsNewRow = true };
				newrow.CreateCells(owner);
				Attach(newrow);
				rows.Add(newrow);
				Reindex();
			}
			else if(!owner.AllowUserToAddRows && has)
			{
				rows.RemoveAt(rows.Count - 1);
				Reindex();
			}
		}

		/// <summary>ULDB: the columns changed; give every row cells for each column.</summary>
		internal void SyncCells()
		{
			foreach(DataGridViewRow r in rows) r.EnsureCells(owner);
		}

		private void Attach(DataGridViewRow row)
		{
			row.Owner = owner;
			if(row.Cells.Count < owner.Columns.Count) row.EnsureCells(owner);
		}

		private void Reindex()
		{
			for(int i = 0; i < rows.Count; i++) rows[i].Index = i;
		}

		public int Add(DataGridViewRow row)
		{
			int index = AppendIndex;
			Insert(index, row);
			return index;
		}

		public int Add()
		{
			DataGridViewRow row = new DataGridViewRow();
			row.CreateCells(owner);
			return Add(row);
		}

		public int Add(params object[] values)
		{
			DataGridViewRow row = new DataGridViewRow();
			row.CreateCells(owner, values);
			return Add(row);
		}

		public int Add(int count)
		{
			int first = -1;
			for(int i = 0; i < count; i++) { int idx = Add(); if(first < 0) first = idx; }
			return first;
		}

		public void AddRange(params DataGridViewRow[] newrows)
		{
			foreach(DataGridViewRow r in newrows) Add(r);
		}

		public void Insert(int index, DataGridViewRow row)
		{
			Attach(row);
			// Nothing goes after the "new row" (WinForms throws; inserting at its index is fine).
			rows.Insert(Math.Max(0, Math.Min(index, AppendIndex)), row);
			Reindex();
			owner.OnRowsAdded(row.Index, 1);
			owner.OnGridChanged();
		}

		public void Insert(int index, params object[] values)
		{
			DataGridViewRow row = new DataGridViewRow();
			row.CreateCells(owner, values);
			Insert(index, row);
		}

		public void RemoveAt(int index)
		{
			DataGridViewRow row = rows[index];
			if(row.IsNewRow) throw new InvalidOperationException("Uncommitted new row cannot be deleted.");
			if(owner.CurrentCell?.OwningRow == row) owner.CurrentCell = null;
			rows.RemoveAt(index);
			row.Owner = null;
			row.Index = -1;
			Reindex();
			owner.OnRowsRemoved(index, 1);
			owner.OnGridChanged();
		}

		public void Remove(DataGridViewRow row)
		{
			int index = rows.IndexOf(row);
			if(index >= 0) RemoveAt(index);
		}

		/// <summary>Removes every row except the "new row".</summary>
		public void Clear()
		{
			owner.CurrentCell = null;
			int count = rows.Count;
			rows.RemoveAll(r => !r.IsNewRow);
			Reindex();
			if(count > rows.Count) owner.OnRowsRemoved(0, count - rows.Count);
			owner.OnGridChanged();
		}

		internal void Reorder(List<DataGridViewRow> ordered, DataGridViewRow newrow)
		{
			rows.Clear();
			rows.AddRange(ordered);
			if(newrow != null) rows.Add(newrow);
			Reindex();
		}

		public int IndexOf(DataGridViewRow row) => rows.IndexOf(row);
		public bool Contains(DataGridViewRow row) => rows.Contains(row);
		public int GetRowCount(DataGridViewElementStates state) => rows.Count;
		public DataGridViewRow SharedRow(int index) => rows[index];

		public IEnumerator<DataGridViewRow> GetEnumerator() => rows.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => rows.GetEnumerator();

		// IList (for code that treats the collection untyped)
		bool IList.IsFixedSize => false;
		bool IList.IsReadOnly => false;
		object IList.this[int index] { get => rows[index]; set => throw new NotSupportedException(); }
		int IList.Add(object value) => Add((DataGridViewRow)value);
		bool IList.Contains(object value) => rows.Contains(value as DataGridViewRow);
		int IList.IndexOf(object value) => rows.IndexOf(value as DataGridViewRow);
		void IList.Insert(int index, object value) => Insert(index, (DataGridViewRow)value);
		void IList.Remove(object value) => Remove(value as DataGridViewRow);
		bool ICollection.IsSynchronized => false;
		object ICollection.SyncRoot => this;
		void ICollection.CopyTo(Array array, int index) => ((ICollection)rows).CopyTo(array, index);
	}

	public class DataGridViewBand
	{
		private bool readonlyflag, visible = true;

		public DataGridViewCellStyle DefaultCellStyle { get; set; } = new DataGridViewCellStyle();
		public bool ReadOnly { get => readonlyflag; set { if(readonlyflag == value) return; readonlyflag = value; Owner?.OnGridChanged(); } }
		public bool Visible { get => visible; set { if(visible == value) return; visible = value; Owner?.OnGridChanged(); } }
		public bool Frozen { get; set; }
		public DataGridViewTriState Resizable { get; set; } = DataGridViewTriState.NotSet;
		public object Tag { get; set; }
		public int Index { get; internal set; } = -1;
		public DataGridView DataGridView => Owner;
		internal DataGridView Owner { get; set; }
	}

	public class DataGridViewRow : DataGridViewBand
	{
		private bool selected;

		public DataGridViewCellCollection Cells { get; }
		public bool IsNewRow { get; internal set; }
		public int Height { get; set; } = DataGridView.ViewRowHeight;
		public int MinimumHeight { get; set; } = 3;
		public string ErrorText { get; set; } = "";
		public object DataBoundItem => null;
		public DataGridViewHeaderCell HeaderCell { get; } = new DataGridViewHeaderCell();

		public DataGridViewRow() { Cells = new DataGridViewCellCollection(this); }

		public bool Selected
		{
			get => selected;
			set
			{
				if(selected == value) return;
				if(value && Owner != null && !Owner.MultiSelect) { Owner.SelectOnly(this); return; }
				selected = value;
				Owner?.OnRowSelectionChanged();
			}
		}

		internal void SetSelectedSilently(bool value) => selected = value;

		/// <summary>Creates a cell for each of the grid's columns (from the column's cell template).</summary>
		public void CreateCells(DataGridView dataGridView)
		{
			Cells.Clear();
			foreach(DataGridViewColumn c in dataGridView.Columns) Cells.Add(c.CreateCell());
		}

		public void CreateCells(DataGridView dataGridView, params object[] values)
		{
			CreateCells(dataGridView);
			SetValues(values);
		}

		internal void EnsureCells(DataGridView dataGridView)
		{
			for(int i = Cells.Count; i < dataGridView.Columns.Count; i++) Cells.Add(dataGridView.Columns[i].CreateCell());
		}

		public bool SetValues(params object[] values)
		{
			if(values == null) return true;
			for(int i = 0; i < values.Length && i < Cells.Count; i++) Cells[i].Value = values[i];
			return values.Length <= Cells.Count;
		}

		public DataGridViewElementStates State => selected ? DataGridViewElementStates.Selected : DataGridViewElementStates.None;
		public object Clone() => MemberwiseClone();
	}

	public class DataGridViewSelectedRowCollection : List<DataGridViewRow> { }
	public class DataGridViewSelectedCellCollection : List<DataGridViewCell> { }

	#endregion

	#region ================== Cells

	public class DataGridViewCellCollection : List<DataGridViewCell>
	{
		private readonly DataGridViewRow row;

		internal DataGridViewCellCollection(DataGridViewRow row) { this.row = row; }

		public new void Add(DataGridViewCell cell)
		{
			cell.OwningRow = row;
			base.Add(cell);
		}

		public new void AddRange(IEnumerable<DataGridViewCell> cells)
		{
			foreach(DataGridViewCell c in cells) Add(c);
		}

		public DataGridViewCell this[string columnName]
		{
			get
			{
				DataGridView grid = row.DataGridView;
				if(grid == null) return null;
				for(int i = 0; i < grid.Columns.Count && i < Count; i++)
					if(grid.Columns[i].Name == columnName) return this[i];
				return null;
			}
		}
	}

	public class DataGridViewCell
	{
		private object value;
		private bool readonlyflag;
		private DataGridViewCellStyle style;

		public object Value
		{
			get => value;
			set
			{
				if(Equals(this.value, value)) return;
				this.value = value;
				DataGridView?.OnCellValueChanged(this);
			}
		}

		public bool ReadOnly
		{
			get => readonlyflag || (OwningRow?.ReadOnly ?? false) || (OwningColumn?.ReadOnly ?? false);
			set => readonlyflag = value;
		}

		/// <summary>The cell's own style (created on first use, like WinForms).</summary>
		public DataGridViewCellStyle Style { get => style ?? (style = new DataGridViewCellStyle()); set => style = value; }
		public bool HasStyle => style != null;
		public object Tag { get; set; }
		public string ToolTipText { get; set; } = "";
		public string ErrorText { get; set; } = "";
		public bool Selected { get; set; }
		public virtual Type ValueType { get; set; } = typeof(object);
		public virtual Type FormattedValueType => typeof(string);
		public object FormattedValue => Value;
		public object EditedFormattedValue => DataGridView != null && DataGridView.IsCurrentCellInEditMode && DataGridView.CurrentCell == this ? DataGridView.EditingValue : Value;
		public bool IsInEditMode => DataGridView != null && DataGridView.IsCurrentCellInEditMode && DataGridView.CurrentCell == this;
		public bool Displayed => true;
		public bool Visible => OwningColumn?.Visible ?? true;
		public Size Size => new Size(OwningColumn?.Width ?? 100, OwningRow?.Height ?? DataGridView.ViewRowHeight);
		public Rectangle ContentBounds => new Rectangle(Point.Empty, Size);

		public DataGridViewRow OwningRow { get; internal set; }
		public DataGridView DataGridView => OwningRow?.DataGridView;
		public int RowIndex => OwningRow?.Index ?? -1;
		public int ColumnIndex => OwningRow != null ? OwningRow.Cells.IndexOf(this) : -1;
		public DataGridViewColumn OwningColumn
		{
			get
			{
				DataGridView grid = DataGridView;
				int i = ColumnIndex;
				return grid != null && i >= 0 && i < grid.Columns.Count ? grid.Columns[i] : null;
			}
		}

		/// <summary>The effective style: cell, row, column and grid defaults combined (cell wins).</summary>
		public DataGridViewCellStyle InheritedStyle
		{
			get
			{
				DataGridViewCellStyle result = new DataGridViewCellStyle();
				DataGridView grid = DataGridView;
				if(grid != null) result.ApplyStyle(grid.DefaultCellStyle);
				if(OwningColumn != null) result.ApplyStyle(OwningColumn.DefaultCellStyle);
				if(grid != null) result.ApplyStyle(grid.RowsDefaultCellStyle);
				if(OwningRow != null) result.ApplyStyle(OwningRow.DefaultCellStyle);
				if(style != null) result.ApplyStyle(style);
				return result;
			}
		}

		public virtual object Clone()
		{
			DataGridViewCell c = (DataGridViewCell)MemberwiseClone();
			c.OwningRow = null;
			c.style = style?.Clone();
			return c;
		}

		public override string ToString() => GetType().Name + " { ColumnIndex=" + ColumnIndex + ", RowIndex=" + RowIndex + " }";
	}

	public class DataGridViewTextBoxCell : DataGridViewCell
	{
		public int MaxInputLength { get; set; } = 32767;
	}

	public class DataGridViewComboBoxCell : DataGridViewCell
	{
		public ComboBox.ObjectCollection Items { get; } = new ComboBox.ObjectCollection();
		public DataGridViewComboBoxDisplayStyle DisplayStyle { get; set; }
		public bool AutoComplete { get; set; } = true;
		public bool Sorted { get; set; }
		public FlatStyle FlatStyle { get; set; }
		public string DisplayMember { get; set; } = "";
		public string ValueMember { get; set; } = "";
		public object DataSource { get; set; }

		public override object Clone()
		{
			DataGridViewComboBoxCell c = (DataGridViewComboBoxCell)base.Clone();
			// Items is shared on purpose: WinForms combo cells take their items from the column.
			return c;
		}
	}

	public class DataGridViewCheckBoxCell : DataGridViewCell
	{
		public bool ThreeState { get; set; }
		public object TrueValue { get; set; } = true;
		public object FalseValue { get; set; } = false;
	}

	public class DataGridViewImageCell : DataGridViewCell
	{
		public DataGridViewImageCellLayout ImageLayout { get; set; }
		public string Description { get; set; } = "";
		public bool ValueIsIcon { get; set; }
	}

	public class DataGridViewButtonCell : DataGridViewCell
	{
		public bool UseColumnTextForButtonValue { get; set; }
		public FlatStyle FlatStyle { get; set; }
	}

	public class DataGridViewHeaderCell : DataGridViewCell { }
	public class DataGridViewColumnHeaderCell : DataGridViewHeaderCell
	{
		public SortOrder SortGlyphDirection { get; set; }
	}

	public class DataGridViewCellStyle
	{
		public DataGridViewCellStyle() { }
		public DataGridViewCellStyle(DataGridViewCellStyle other) { ApplyStyle(other); }

		public DataGridViewContentAlignment Alignment { get; set; }
		public Color BackColor { get; set; }
		public Color ForeColor { get; set; }
		public Color SelectionBackColor { get; set; }
		public Color SelectionForeColor { get; set; }
		public Font Font { get; set; }
		public DataGridViewTriState WrapMode { get; set; }
		public object NullValue { get; set; } = "";
		public object DataSourceNullValue { get; set; }
		public string Format { get; set; } = "";
		public IFormatProvider FormatProvider { get; set; }
		public Padding Padding { get; set; }
		public object Tag { get; set; }

		/// <summary>Copies the values set in <paramref name="other"/> over this style.</summary>
		public void ApplyStyle(DataGridViewCellStyle other)
		{
			if(other == null) return;
			if(other.Alignment != DataGridViewContentAlignment.NotSet) Alignment = other.Alignment;
			if(!other.BackColor.IsEmpty) BackColor = other.BackColor;
			if(!other.ForeColor.IsEmpty) ForeColor = other.ForeColor;
			if(!other.SelectionBackColor.IsEmpty) SelectionBackColor = other.SelectionBackColor;
			if(!other.SelectionForeColor.IsEmpty) SelectionForeColor = other.SelectionForeColor;
			if(other.Font != null) Font = other.Font;
			if(other.WrapMode != DataGridViewTriState.NotSet) WrapMode = other.WrapMode;
			if(!string.IsNullOrEmpty(other.Format)) Format = other.Format;
			if(other.Padding != Padding.Empty) Padding = other.Padding;
		}

		public DataGridViewCellStyle Clone() => new DataGridViewCellStyle(this) { NullValue = NullValue, Tag = Tag };
	}

	#endregion

	#region ================== Columns

	public class DataGridViewColumnCollection : List<DataGridViewColumn>
	{
		private readonly DataGridView owner;

		internal DataGridViewColumnCollection(DataGridView owner) { this.owner = owner; }

		public new int Add(DataGridViewColumn column)
		{
			column.Owner = owner;
			column.Index = Count;
			base.Add(column);
			owner.Rows.SyncCells();
			owner.OnGridChanged();
			return column.Index;
		}

		public int Add(string columnName, string headerText)
		{
			return Add(new DataGridViewTextBoxColumn { Name = columnName, HeaderText = headerText });
		}

		public void AddRange(params DataGridViewColumn[] columns)
		{
			foreach(DataGridViewColumn c in columns) Add(c);
		}

		public new void Clear()
		{
			base.Clear();
			owner.OnGridChanged();
		}

		public DataGridViewColumn this[string columnName]
		{
			get { foreach(DataGridViewColumn c in this) if(c.Name == columnName) return c; return null; }
		}

		public int GetColumnCount(DataGridViewElementStates states) => Count;
	}

	public class DataGridViewColumn : DataGridViewBand
	{
		private int width = 100;
		private bool visible = true;

		public DataGridViewColumn() { }
		public DataGridViewColumn(DataGridViewCell cellTemplate) { CellTemplate = cellTemplate; }

		public string Name { get; set; } = "";
		public string HeaderText { get; set; } = "";
		public string ToolTipText { get; set; } = "";
		public string DataPropertyName { get; set; } = "";
		public int Width { get => width; set { width = value; Owner?.OnGridChanged(); } }
		public new bool Visible { get => visible; set { visible = value; Owner?.OnGridChanged(); } }
		public int MinimumWidth { get; set; } = 5;
		public float FillWeight { get; set; } = 100f;
		public int DisplayIndex { get; set; }
		public DataGridViewAutoSizeColumnMode AutoSizeMode { get; set; } = DataGridViewAutoSizeColumnMode.NotSet;
		public DataGridViewColumnSortMode SortMode { get; set; } = DataGridViewColumnSortMode.Automatic;
		public DataGridViewCell CellTemplate { get; set; }
		public DataGridViewColumnHeaderCell HeaderCell { get; } = new DataGridViewColumnHeaderCell();
		public Type ValueType { get; set; }
		public bool IsDataBound => false;

		/// <summary>A new cell for a row, cloned from the template (or the column's default cell type).</summary>
		internal virtual DataGridViewCell CreateCell()
		{
			if(CellTemplate != null) return (DataGridViewCell)CellTemplate.Clone();
			return new DataGridViewTextBoxCell();
		}
	}

	public class DataGridViewTextBoxColumn : DataGridViewColumn
	{
		public DataGridViewTextBoxColumn() : base(new DataGridViewTextBoxCell()) { }
		public int MaxInputLength { get; set; } = 32767;
	}

	public class DataGridViewComboBoxColumn : DataGridViewColumn
	{
		public DataGridViewComboBoxColumn() : base(new DataGridViewComboBoxCell()) { }

		/// <summary>Items offered in this column's cells (shared by its cells, as in WinForms).</summary>
		public ComboBox.ObjectCollection Items => ((DataGridViewComboBoxCell)CellTemplate).Items;
		public bool AutoComplete { get; set; } = true;
		public DataGridViewComboBoxDisplayStyle DisplayStyle { get; set; }
		public bool DisplayStyleForCurrentCellOnly { get; set; }
		public bool Sorted { get; set; }
		public FlatStyle FlatStyle { get; set; }
		public int DropDownWidth { get; set; } = 1;
		public int MaxDropDownItems { get; set; } = 8;
		public string DisplayMember { get; set; } = "";
		public string ValueMember { get; set; } = "";
		public object DataSource { get; set; }
	}

	public class DataGridViewCheckBoxColumn : DataGridViewColumn
	{
		public DataGridViewCheckBoxColumn() : base(new DataGridViewCheckBoxCell()) { }
		public bool ThreeState { get; set; }
		public object TrueValue { get; set; }
		public object FalseValue { get; set; }
	}

	public class DataGridViewImageColumn : DataGridViewColumn
	{
		public DataGridViewImageColumn() : base(new DataGridViewImageCell()) { }
		public DataGridViewImageCellLayout ImageLayout { get; set; }
		public Image Image { get; set; }
		public string Description { get; set; } = "";
		public bool ValuesAreIcons { get; set; }
	}

	public class DataGridViewButtonColumn : DataGridViewColumn
	{
		public DataGridViewButtonColumn() : base(new DataGridViewButtonCell()) { }
		public string Text { get; set; } = "";
		public bool UseColumnTextForButtonValue { get; set; }
		public FlatStyle FlatStyle { get; set; }
	}

	#endregion

	#region ================== Event args

	public delegate void DataGridViewCellEventHandler(object sender, DataGridViewCellEventArgs e);
	public delegate void DataGridViewCellCancelEventHandler(object sender, DataGridViewCellCancelEventArgs e);
	public delegate void DataGridViewCellMouseEventHandler(object sender, DataGridViewCellMouseEventArgs e);
	public delegate void DataGridViewRowCancelEventHandler(object sender, DataGridViewRowCancelEventArgs e);
	public delegate void DataGridViewRowEventHandler(object sender, DataGridViewRowEventArgs e);
	public delegate void DataGridViewDataErrorEventHandler(object sender, DataGridViewDataErrorEventArgs e);
	public delegate void DataGridViewRowsAddedEventHandler(object sender, DataGridViewRowsAddedEventArgs e);
	public delegate void DataGridViewRowsRemovedEventHandler(object sender, DataGridViewRowsRemovedEventArgs e);
	public delegate void DataGridViewCellFormattingEventHandler(object sender, DataGridViewCellFormattingEventArgs e);
	public delegate void DataGridViewCellPaintingEventHandler(object sender, DataGridViewCellPaintingEventArgs e);
	public delegate void DataGridViewEditingControlShowingEventHandler(object sender, DataGridViewEditingControlShowingEventArgs e);
	public delegate void DataGridViewSortCompareEventHandler(object sender, DataGridViewSortCompareEventArgs e);
	public delegate void ScrollEventHandler(object sender, ScrollEventArgs e);

	public class DataGridViewCellEventArgs : EventArgs
	{
		public DataGridViewCellEventArgs(int columnIndex, int rowIndex) { ColumnIndex = columnIndex; RowIndex = rowIndex; }
		public int ColumnIndex { get; }
		public int RowIndex { get; }
	}

	public class DataGridViewCellCancelEventArgs : CancelEventArgs
	{
		public DataGridViewCellCancelEventArgs(int columnIndex, int rowIndex) { ColumnIndex = columnIndex; RowIndex = rowIndex; }
		public int ColumnIndex { get; }
		public int RowIndex { get; }
	}

	public class DataGridViewCellMouseEventArgs : MouseEventArgs
	{
		public DataGridViewCellMouseEventArgs(int columnIndex, int rowIndex, int localX, int localY, MouseEventArgs e)
			: base(e.Button, e.Clicks, localX, localY, e.Delta)
		{
			ColumnIndex = columnIndex;
			RowIndex = rowIndex;
		}

		public int ColumnIndex { get; }
		public int RowIndex { get; }
	}

	public class DataGridViewRowCancelEventArgs : CancelEventArgs
	{
		public DataGridViewRowCancelEventArgs(DataGridViewRow row) { Row = row; }
		public DataGridViewRow Row { get; }
	}

	public class DataGridViewRowEventArgs : EventArgs
	{
		public DataGridViewRowEventArgs(DataGridViewRow row) { Row = row; }
		public DataGridViewRow Row { get; }
	}

	public class DataGridViewRowsAddedEventArgs : EventArgs
	{
		public DataGridViewRowsAddedEventArgs(int rowIndex, int rowCount) { RowIndex = rowIndex; RowCount = rowCount; }
		public int RowIndex { get; }
		public int RowCount { get; }
	}

	public class DataGridViewRowsRemovedEventArgs : EventArgs
	{
		public DataGridViewRowsRemovedEventArgs(int rowIndex, int rowCount) { RowIndex = rowIndex; RowCount = rowCount; }
		public int RowIndex { get; }
		public int RowCount { get; }
	}

	public class DataGridViewDataErrorEventArgs : DataGridViewCellCancelEventArgs
	{
		public DataGridViewDataErrorEventArgs(Exception exception, int columnIndex, int rowIndex, DataGridViewDataErrorContexts context)
			: base(columnIndex, rowIndex)
		{
			Exception = exception;
			Context = context;
		}

		public Exception Exception { get; }
		public DataGridViewDataErrorContexts Context { get; }
		public bool ThrowException { get; set; }
	}

	public class DataGridViewCellFormattingEventArgs : EventArgs
	{
		public DataGridViewCellFormattingEventArgs(int columnIndex, int rowIndex, object value, Type desiredType, DataGridViewCellStyle cellStyle)
		{
			ColumnIndex = columnIndex; RowIndex = rowIndex; Value = value; DesiredType = desiredType; CellStyle = cellStyle;
		}

		public int ColumnIndex { get; }
		public int RowIndex { get; }
		public object Value { get; set; }
		public Type DesiredType { get; }
		public DataGridViewCellStyle CellStyle { get; set; }
		public bool FormattingApplied { get; set; }
	}

	public class DataGridViewCellPaintingEventArgs : HandledEventArgs
	{
		public int ColumnIndex { get; }
		public int RowIndex { get; }
		public Graphics Graphics => null;
		public Rectangle CellBounds { get; }
		public object Value { get; }
		public DataGridViewCellStyle CellStyle { get; }
		public void PaintBackground(Rectangle clipBounds, bool cellsPaintSelectionBackground) { }
		public void PaintContent(Rectangle clipBounds) { }
		public void Paint(Rectangle clipBounds, DataGridViewPaintParts paintParts) { }
	}

	public class DataGridViewEditingControlShowingEventArgs : EventArgs
	{
		public DataGridViewEditingControlShowingEventArgs(Control control, DataGridViewCellStyle cellStyle) { Control = control; CellStyle = cellStyle; }
		public Control Control { get; }
		public DataGridViewCellStyle CellStyle { get; set; }
	}

	public class DataGridViewSortCompareEventArgs : HandledEventArgs
	{
		public DataGridViewSortCompareEventArgs(DataGridViewColumn column, object cellValue1, object cellValue2, int rowIndex1, int rowIndex2)
		{
			Column = column; CellValue1 = cellValue1; CellValue2 = cellValue2; RowIndex1 = rowIndex1; RowIndex2 = rowIndex2;
		}

		public DataGridViewColumn Column { get; }
		public object CellValue1 { get; }
		public object CellValue2 { get; }
		public int RowIndex1 { get; }
		public int RowIndex2 { get; }
		public int SortResult { get; set; }
	}

	public class ScrollEventArgs : EventArgs
	{
		public ScrollEventArgs(ScrollEventType type, int newValue) : this(type, newValue, newValue, ScrollOrientation.HorizontalScroll) { }
		public ScrollEventArgs(ScrollEventType type, int oldValue, int newValue) : this(type, oldValue, newValue, ScrollOrientation.HorizontalScroll) { }
		public ScrollEventArgs(ScrollEventType type, int oldValue, int newValue, ScrollOrientation scroll)
		{
			Type = type; OldValue = oldValue; NewValue = newValue; ScrollOrientation = scroll;
		}

		public ScrollEventType Type { get; }
		public int OldValue { get; }
		public int NewValue { get; set; }
		public ScrollOrientation ScrollOrientation { get; }
	}

	#endregion
}
