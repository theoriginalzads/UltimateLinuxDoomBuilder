// ULDB: the view of upstream's FieldsEditorControl (the "Custom" tab of the edit forms: UDMF
// fields as a Property / Type / Value grid). Upstream's code runs unchanged on the compat
// DataGridView model; this view draws the model's rows and drives it like WinForms' grid would:
//  - clicking a cell makes it current and raises CellClick (upstream then calls BeginEdit)
//  - while the model is in edit mode, the current cell shows an editor: a text box, the type
//    column's combo box, or upstream's own enum combo box (enumscombo) for enum values
//  - Enter/focus loss commits (EndEdit → CellEndEdit), Escape cancels
//  - Delete removes a row (UserDeletingRow, which upstream turns into "undefine" for fixed fields)
//  - header clicks sort (as WinForms' automatic sorting) and raise ColumnHeaderMouseClick
//  - upstream's browse button shows at the right of the selected value cell

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using ULDB.Shell;
using WF = System.Windows.Forms;
using SD = System.Drawing;

namespace CodeImp.DoomBuilder.Controls
{
	internal sealed class FieldsEditorView : UserControl
	{
		private const double RowHeight = 24;

		private readonly FieldsEditorControl model;
		private readonly WF.DataGridView grid;
		private readonly WF.Button browsebutton;
		private readonly WF.ComboBox enumscombo;
		private readonly Grid header = new Grid();
		private readonly StackPanel rowspanel = new StackPanel();
		private readonly ScrollViewer scroller;
		private readonly Dictionary<WF.DataGridViewRow, RowView> rowviews = new Dictionary<WF.DataGridViewRow, RowView>();
		private readonly ComboBox enumsview = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = 0, Padding = new Thickness(6, 0) };
		private readonly Button browseview = new Button { Padding = new Thickness(2), MinHeight = 0, MinWidth = 0, Width = 26, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Stretch };
		private Control editor;
		private WF.DataGridViewCell editorcell;
		private bool pending;

		public FieldsEditorView(FieldsEditorControl model)
		{
			this.model = model;
			ModelBinder binder = new ModelBinder(model);
			grid = binder.Field<WF.DataGridView>("fieldslist");
			browsebutton = binder.Field<WF.Button>("browsebutton");
			enumscombo = binder.Field<WF.ComboBox>("enumscombo");

			// Upstream's enum combo box is shown in place of the value editor; it's a model control
			// like any other (Validating applies its value).
			binder.BindComboBox(enumsview, enumscombo);
			enumsview.DropDownClosed += (s, e) => enumscombo.RaiseValidation();
			enumsview.KeyDown += (s, e) =>
			{
				if(e.Key == Key.Enter) { enumscombo.RaiseValidation(); grid.EndEdit(); e.Handled = true; }
				else if(e.Key == Key.Escape) { grid.CancelEdit(); e.Handled = true; }
			};
			browseview.Click += (s, e) => browsebutton.PerformClick();
			ToolTip.SetTip(browseview, "Browse...");

			header.Classes.Add("uldb-grid-header");
			scroller = new ScrollViewer { Content = rowspanel, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
			scroller.ScrollChanged += (s, e) =>
			{
				int first = (int)(scroller.Offset.Y / RowHeight);
				if(first != grid.FirstDisplayedScrollingRowIndex && !grid.IsCurrentCellInEditMode) grid.RaiseScroll(first);
			};
			DockPanel layout = new DockPanel();
			DockPanel.SetDock(header, Dock.Top);
			layout.Children.Add(header);
			layout.Children.Add(scroller);
			Border frame = new Border { Child = layout, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), ClipToBounds = true };
			frame.Classes.Add("uldb-grid");
			Content = frame;

			Focusable = true;
			KeyDown += OnKeyDown;
			grid.GridChanged += Schedule;
			grid.SelectionChanged += Schedule;
			browsebutton.VisibleChanged += Schedule;
			browsebutton.StylesChanged += Schedule;
			enumscombo.VisibleChanged += Schedule;
			binder.Watch(model);
			binder.Custom(() => { IsVisible = model.Visible; IsEnabled = model.Enabled; });
			binder.Updated += Schedule0;
			binder.UpdateNow();
			UpdateView();
		}

		private void Schedule0() => Schedule(null, EventArgs.Empty);

		private void Schedule(object sender, EventArgs e)
		{
			if(pending) return;
			pending = true;
			Dispatcher.UIThread.Post(UpdateView, DispatcherPriority.Render);
		}

		#region ================== Layout

		private ColumnDefinitions Columns()
		{
			ColumnDefinitions defs = new ColumnDefinitions();
			for(int i = 0; i < grid.Columns.Count; i++)
			{
				WF.DataGridViewColumn c = grid.Columns[i];
				bool last = i == grid.Columns.Count - 1;
				defs.Add(new ColumnDefinition(!c.Visible ? new GridLength(0) : last ? new GridLength(1, GridUnitType.Star) : new GridLength(c.Width)));
			}
			return defs;
		}

		private void UpdateView()
		{
			pending = false;

			// Header
			string hsig = string.Join("|", grid.Columns.Select(c => c.HeaderText + c.Width + c.Visible)) + grid.SortedColumn?.Index + grid.SortOrder;
			if(header.Tag as string != hsig)
			{
				header.Tag = hsig;
				header.Children.Clear();
				header.ColumnDefinitions = Columns();
				for(int i = 0; i < grid.Columns.Count; i++)
				{
					int col = i;
					WF.DataGridViewColumn c = grid.Columns[i];
					string arrow = grid.SortedColumn == c ? (grid.SortOrder == WF.SortOrder.Ascending ? "  ▴" : "  ▾") : "";
					Button h = new Button { Content = c.HeaderText + arrow, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left, IsVisible = c.Visible };
					h.Classes.Add("uldb-grid-headercell");
					h.Click += (s, e) => HeaderClicked(col);
					Grid.SetColumn(h, i);
					header.Children.Add(h);
				}
			}

			// Rows, in order; existing row views are reused so an open editor keeps its focus.
			List<RowView> wanted = new List<RowView>();
			HashSet<WF.DataGridViewRow> present = new HashSet<WF.DataGridViewRow>();
			foreach(WF.DataGridViewRow row in grid.Rows)
			{
				present.Add(row);
				if(!rowviews.TryGetValue(row, out RowView rv)) rowviews[row] = rv = new RowView(this, row);
				rv.Update();
				wanted.Add(rv);
			}
			foreach(WF.DataGridViewRow gone in rowviews.Keys.Where(r => !present.Contains(r)).ToList()) rowviews.Remove(gone);
			bool sameorder = rowspanel.Children.Count == wanted.Count && wanted.Select((w, i) => rowspanel.Children[i] == w.Root).All(b => b);
			if(!sameorder)
			{
				rowspanel.Children.Clear();
				foreach(RowView rv in wanted) rowspanel.Children.Add(rv.Root);
			}

			UpdateEditor();
		}

		private void HeaderClicked(int col)
		{
			WF.DataGridViewColumn c = grid.Columns[col];
			if(c.SortMode == WF.DataGridViewColumnSortMode.Automatic)
			{
				System.ComponentModel.ListSortDirection dir = grid.SortedColumn == c && grid.SortOrder == WF.SortOrder.Ascending
					? System.ComponentModel.ListSortDirection.Descending : System.ComponentModel.ListSortDirection.Ascending;
				grid.Sort(c, dir);
			}
			grid.RaiseColumnHeaderMouseClick(col);
		}

		#endregion

		#region ================== Editing

		// Shows the editor (or browse button) in the current cell, following the model's state.
		private void UpdateEditor()
		{
			WF.DataGridViewCell cell = grid.IsCurrentCellInEditMode ? grid.CurrentCell : null;
			bool useenums = cell != null && cell.ColumnIndex == 2 && enumscombo.Visible;

			if(cell != editorcell || (useenums != (editor == enumsview)))
			{
				RemoveEditor();
				editorcell = cell;
				if(cell != null && rowviews.TryGetValue(cell.OwningRow, out RowView rv))
				{
					editor = useenums ? enumsview : CreateEditor(cell);
					rv.ShowEditor(cell.ColumnIndex, editor);
					Dispatcher.UIThread.Post(() =>
					{
						editor?.Focus();
						if(editor is TextBox tb && grid.EditSelectAll) tb.SelectAll();
						else if(editor is TextBox tb2) tb2.CaretIndex = tb2.Text?.Length ?? 0;
						else if(editor is ComboBox cb && editor != enumsview) cb.IsDropDownOpen = true;
					}, DispatcherPriority.Input);
				}
			}

			// Upstream's browse button: right side of the selected row's value cell.
			if(browseview.Parent is Panel bp) bp.Children.Remove(browseview);
			if(browsebutton.Visible && editor == null && grid.SelectedRows.Count > 0 && rowviews.TryGetValue(grid.SelectedRows[0], out RowView sel))
			{
				browseview.Content = browsebutton.Image != null
					? new Image { Source = AvaloniaImages.From(browsebutton.Image), Width = 16, Height = 16 }
					: (object)"...";
				sel.ShowBrowse(browseview);
			}
		}

		private Control CreateEditor(WF.DataGridViewCell cell)
		{
			if(cell.OwningColumn is WF.DataGridViewComboBoxColumn combocol)
			{
				List<object> items = combocol.Items.Cast<object>().ToList();
				ComboBox cb = new ComboBox { ItemsSource = items.Select(i => i.ToString()).ToList(), HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = 0, Padding = new Thickness(6, 0) };
				int index = items.FindIndex(i => i.ToString() == grid.EditingValue?.ToString());
				cb.SelectedIndex = index;
				cb.DropDownClosed += (s, e) =>
				{
					if(cb.SelectedIndex >= 0) grid.EditingValue = items[cb.SelectedIndex].ToString();
					Commit();
				};
				cb.KeyDown += (s, e) => { if(e.Key == Key.Escape) { grid.CancelEdit(); e.Handled = true; } };
				return cb;
			}

			TextBox tb = new TextBox { Text = grid.EditingValue?.ToString() ?? "", MinHeight = 0, Padding = new Thickness(5, 2), VerticalContentAlignment = VerticalAlignment.Center };
			tb.Classes.Add("uldb-grid-editor");
			tb.TextChanged += (s, e) => grid.EditingValue = tb.Text;
			tb.KeyDown += (s, e) =>
			{
				if(e.Key == Key.Enter) { Commit(); e.Handled = true; }
				else if(e.Key == Key.Escape) { grid.CancelEdit(); Focus(); e.Handled = true; }
			};
			tb.LostFocus += (s, e) => { if(editor == tb) Commit(); };
			return tb;
		}

		private void Commit()
		{
			if(!grid.IsCurrentCellInEditMode) return;
			grid.EndEdit();
			Focus();
		}

		private void RemoveEditor()
		{
			if(editor?.Parent is Panel p) p.Children.Remove(editor);
			editor = null;
			editorcell = null;
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if(grid.IsCurrentCellInEditMode) return;
			if(e.Key == Key.Delete && grid.SelectedRows.Count > 0)
			{
				grid.RaiseUserDeletingRow(grid.SelectedRows[0]);
				e.Handled = true;
			}
			else if((e.Key == Key.Up || e.Key == Key.Down) && grid.CurrentCell != null)
			{
				int row = grid.CurrentCell.RowIndex + (e.Key == Key.Up ? -1 : 1);
				while(row >= 0 && row < grid.Rows.Count && !grid.Rows[row].Visible) row += e.Key == Key.Up ? -1 : 1;
				if(row >= 0 && row < grid.Rows.Count) grid.CurrentCell = grid.Rows[row].Cells[grid.CurrentCell.ColumnIndex];
				e.Handled = true;
			}
			else if((e.Key == Key.Enter || e.Key == Key.F2) && grid.CurrentCell != null)
			{
				grid.RaiseCellClick(grid.CurrentCell.ColumnIndex, grid.CurrentCell.RowIndex);
				e.Handled = true;
			}
		}

		#endregion

		#region ================== Row view

		private sealed class RowView
		{
			private readonly FieldsEditorView owner;
			private readonly WF.DataGridViewRow row;
			private readonly Grid cells = new Grid { Height = RowHeight };
			private readonly List<Panel> cellpanels = new List<Panel>();
			private readonly List<TextBlock> texts = new List<TextBlock>();

			public Control Root => cells;

			public RowView(FieldsEditorView owner, WF.DataGridViewRow row)
			{
				this.owner = owner;
				this.row = row;
				cells.Classes.Add("uldb-grid-row");
			}

			public void Update()
			{
				WF.DataGridView grid = owner.grid;
				if(cellpanels.Count != row.Cells.Count)
				{
					cells.Children.Clear();
					cellpanels.Clear();
					texts.Clear();
					for(int i = 0; i < row.Cells.Count; i++)
					{
						int col = i;
						TextBlock t = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0), TextTrimming = TextTrimming.CharacterEllipsis, FontSize = 12 };
						Panel p = new Panel { Background = Brushes.Transparent };
						p.Children.Add(t);
						p.PointerPressed += (s, e) =>
						{
							if(!e.GetCurrentPoint(p).Properties.IsLeftButtonPressed) return;
							owner.Focus();
							if(e.ClickCount == 2) grid.RaiseCellDoubleClick(col, row.Index);
							else grid.RaiseCellClick(col, row.Index);
							e.Handled = true;
						};
						Grid.SetColumn(p, i);
						cells.Children.Add(p);
						cellpanels.Add(p);
						texts.Add(t);
					}
				}
				cells.ColumnDefinitions = owner.Columns();
				cells.IsVisible = row.Visible;
				cells.Classes.Set("selected", row.Selected);
				for(int i = 0; i < row.Cells.Count; i++)
				{
					WF.DataGridViewCell cell = row.Cells[i];
					texts[i].Text = cell.Value?.ToString() ?? "";
					SD.Color fore = cell.InheritedStyle.ForeColor;
					bool gray = fore.IsKnownColor && fore.ToKnownColor() == SD.KnownColor.GrayText;
					texts[i].Opacity = gray ? 0.5 : 1.0;
					ModelBinder.SetForeground(texts[i], gray ? SD.Color.Empty : fore);
					texts[i].FontStyle = row.IsNewRow && i == 0 && gray ? FontStyle.Italic : FontStyle.Normal;
				}
			}

			public void ShowEditor(int col, Control editor)
			{
				if(col < 0 || col >= cellpanels.Count) return;
				if(editor.Parent is Panel old) old.Children.Remove(editor);
				texts[col].IsVisible = false;
				cellpanels[col].Children.Add(editor);
				editor.DetachedFromLogicalTree += Restore;
				void Restore(object s, Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
				{
					editor.DetachedFromLogicalTree -= Restore;
					texts[col].IsVisible = true;
				}
			}

			public void ShowBrowse(Control button)
			{
				if(cellpanels.Count > 2) cellpanels[2].Children.Add(button);
			}
		}

		#endregion
	}
}
