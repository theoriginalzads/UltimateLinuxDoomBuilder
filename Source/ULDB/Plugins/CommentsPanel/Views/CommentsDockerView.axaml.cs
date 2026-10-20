// ULDB: code-behind for CommentsDockerView.axaml, the view of the Comments docker. Upstream's grid
// (a DataGridView) is shown as a list of its rows; clicks reach the model as mouse events at the
// row's position, which the grid's CellLocator maps back, so upstream's click, selection and
// right-click menu logic run unchanged.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using ULDB.Shell;
using SD = System.Drawing;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.CommentsPanel
{
	public partial class CommentsDockerView : UserControl
	{
		private const int RowHeight = 20; // Model coordinates only: row i is at y = i * RowHeight

		private readonly WF.DataGridView grid;
		private bool updating;

		private sealed class Row
		{
			public int Index;
			public SD.Image Icon;
			public string Text;
		}

		public CommentsDockerView()
		{
			InitializeComponent();
		}

		internal CommentsDockerView(CommentsDocker model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();

			grid = (WF.DataGridView)typeof(CommentsDocker).GetField("grid", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(model);
			grid.CellLocator = (x, y) => y >= 0 && y / RowHeight < grid.Rows.Count ? (y / RowHeight, 1) : (-1, -1);

			list.ItemTemplate = new FuncDataTemplate<Row>((row, _) => BuildRow(row), true);
			list.SelectionChanged += (s, e) =>
			{
				if(updating) return;
				int index = list.SelectedIndex;
				for(int i = 0; i < grid.Rows.Count; i++) grid.Rows[i].Selected = i == index;
			};
			list.AddHandler(PointerPressedEvent, (s, e) => RaiseMouse(e, true), Avalonia.Interactivity.RoutingStrategies.Tunnel);
			list.AddHandler(PointerReleasedEvent, (s, e) => RaiseMouse(e, false), Avalonia.Interactivity.RoutingStrategies.Bubble, true);
			grid.GridChanged += (s, e) => Dispatcher.UIThread.Post(Rebuild);
			Rebuild();
		}

		private void Rebuild()
		{
			updating = true;
			list.ItemsSource = Enumerable.Range(0, grid.Rows.Count).Select(i => new Row
			{
				Index = i,
				Icon = grid.Rows[i].Cells[0].Value as SD.Image,
				Text = grid.Rows[i].Cells[1].Value as string,
			}).ToList();
			int selected = Enumerable.Range(0, grid.Rows.Count).FirstOrDefault(i => grid.Rows[i].Selected, -1);
			list.SelectedIndex = selected;
			updating = false;
		}

		private static Control BuildRow(Row row)
		{
			StackPanel p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			if(row.Icon != null)
				p.Children.Add(BreezeIcons.ForImage(row.Icon, 16) ?? new Image { Source = AvaloniaImages.From(row.Icon), Width = 16, Height = 16, VerticalAlignment = VerticalAlignment.Top });
			p.Children.Add(new TextBlock { Text = row.Text, TextWrapping = Avalonia.Media.TextWrapping.Wrap, MaxWidth = 220 });
			return p;
		}

		// The pointer's row, as model coordinates for the grid's MouseDown/MouseUp handlers.
		private void RaiseMouse(PointerEventArgs e, bool down)
		{
			int index = -1;
			for(Avalonia.Visual v = e.Source as Avalonia.Visual; v != null; v = Avalonia.VisualTree.VisualExtensions.GetVisualParent(v))
				if(v is ListBoxItem item) { index = list.IndexFromContainer(item); break; }
			if(index < 0) return;
			PointerPointProperties props = e.GetCurrentPoint(list).Properties;
			WF.MouseButtons button = down ? (props.IsRightButtonPressed ? WF.MouseButtons.Right : WF.MouseButtons.Left)
				: (((PointerReleasedEventArgs)e).InitialPressMouseButton == MouseButton.Right ? WF.MouseButtons.Right : WF.MouseButtons.Left);
			WF.MouseEventArgs args = new WF.MouseEventArgs(button, 1, 4, index * RowHeight + RowHeight / 2, 0);
			if(down) grid.RaiseMouseDown(args);
			else grid.RaiseMouseUp(args);
		}
	}
}
