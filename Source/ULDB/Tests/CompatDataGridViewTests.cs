using System.ComponentModel;
using System.Windows.Forms;

namespace ULDB.Tests
{
	// ULDB.Compat's DataGridView model: upstream's custom fields editor depends on WinForms'
	// "new row" behaviour, programmatic editing and sorting.
	public class CompatDataGridViewTests
	{
		private static DataGridView MakeGrid()
		{
			DataGridView grid = new DataGridView { SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false };
			grid.Columns.AddRange(new DataGridViewTextBoxColumn { Name = "name" }, new DataGridViewComboBoxColumn { Name = "type" }, new DataGridViewTextBoxColumn { Name = "value" });
			return grid;
		}

		[Fact]
		public void NewRowStaysLast()
		{
			DataGridView grid = MakeGrid();
			Assert.Single(grid.Rows);
			Assert.Equal(0, grid.NewRowIndex);

			grid.Rows.Add("a", null, "1");
			grid.Rows.Insert(grid.Rows.Count, new DataGridViewRow()); // past the new row: goes before it
			Assert.Equal(3, grid.Rows.Count);
			Assert.True(grid.Rows[2].IsNewRow);
			Assert.Equal(2, grid.NewRowIndex);
			Assert.Equal(3, grid.Rows[0].Cells.Count);
		}

		[Fact]
		public void CommittingTheNewRowAppendsAnother()
		{
			DataGridView grid = MakeGrid();
			int endrow = -1, newrowatend = -1;
			grid.CellEndEdit += (s, e) => { endrow = e.RowIndex; newrowatend = grid.NewRowIndex; };

			grid.CurrentCell = grid.Rows[0].Cells[0];
			Assert.True(grid.BeginEdit(false));
			grid.EditingValue = "user_test";
			grid.EndEdit();

			Assert.Equal("user_test", grid.Rows[0].Cells[0].Value);
			Assert.False(grid.Rows[0].IsNewRow);
			Assert.Equal(0, endrow);
			Assert.Equal(1, newrowatend); // upstream inserts its field at e.RowIndex + 1
			Assert.True(grid.Rows[1].IsNewRow);
		}

		[Fact]
		public void CancelledBeginEditDoesNotEdit()
		{
			DataGridView grid = MakeGrid();
			grid.Rows.Add("a", null, "1");
			grid.CellBeginEdit += (s, e) => e.Cancel = true;
			grid.CurrentCell = grid.Rows[0].Cells[2];
			Assert.False(grid.BeginEdit(true));
			Assert.False(grid.IsCurrentCellInEditMode);
		}

		[Fact]
		public void SortKeepsNewRowLastAndSelectionFollowsCurrentCell()
		{
			DataGridView grid = MakeGrid();
			grid.Rows.Add("b", null, "2");
			grid.Rows.Add("a", null, "1");
			grid.Sort(grid.Columns[0], ListSortDirection.Ascending);

			Assert.Equal("a", grid.Rows[0].Cells[0].Value);
			Assert.Equal("b", grid.Rows[1].Cells[0].Value);
			Assert.True(grid.Rows[2].IsNewRow);
			Assert.Equal(SortOrder.Ascending, grid.SortOrder);

			grid.CurrentCell = grid.Rows[1].Cells[2];
			Assert.Single(grid.SelectedRows);
			Assert.Same(grid.Rows[1], grid.SelectedRows[0]);
		}

		[Fact]
		public void UserDeletingRowCanCancel()
		{
			DataGridView grid = MakeGrid();
			grid.Rows.Add("a", null, "1");
			grid.Rows.Add("b", null, "2");
			grid.UserDeletingRow += (s, e) => e.Cancel = (string)e.Row.Cells[0].Value == "a";

			grid.RaiseUserDeletingRow(grid.Rows[0]);
			Assert.Equal(3, grid.Rows.Count);
			grid.RaiseUserDeletingRow(grid.Rows[1]);
			Assert.Equal(2, grid.Rows.Count);
		}
	}
}
