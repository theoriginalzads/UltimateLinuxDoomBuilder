// ULDB: the view of upstream's CheckboxArrayControl (model-ported): flags as checkboxes in
// columns, filled top to bottom then left to right like upstream's PositionCheckboxes. Upstream
// adds the checkboxes in code, so the view follows the model's list.

using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	internal sealed class CheckboxArrayControlView : UserControl
	{
		private readonly CheckboxArrayControl model;
		private readonly Grid grid = new Grid();
		private readonly List<WF.CheckBox> shown = new List<WF.CheckBox>();
		private int showncolumns;

		public CheckboxArrayControlView(CheckboxArrayControl model)
		{
			this.model = model;
			Content = grid;
			ModelBinder binder = new ModelBinder(model);
			binder.Watch(model);
			binder.Custom(Update);
			binder.BindState(this, model);
			// Checkboxes are added without events; look again when the view is shown.
			AttachedToVisualTree += (s, e) => Dispatcher.UIThread.Post(Update);
			binder.UpdateNow();
		}

		private void Update()
		{
			List<WF.CheckBox> boxes = model.Checkboxes;
			int columns = System.Math.Max(1, model.Columns);
			if(columns == showncolumns && boxes.Count == shown.Count && boxes.TrueForAll(b => shown.Contains(b))) return;
			shown.Clear();
			shown.AddRange(boxes);
			showncolumns = columns;

			grid.Children.Clear();
			grid.ColumnDefinitions.Clear();
			grid.RowDefinitions.Clear();
			int rows = (int)System.Math.Ceiling(boxes.Count / (float)columns);
			for(int c = 0; c < columns; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			for(int r = 0; r < rows; r++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			for(int i = 0; i < boxes.Count; i++)
			{
				CheckBox view = new CheckBox { Margin = new Thickness(0, 0, 8, 1) };
				view.Classes.Add("uldb-flag");
				new ModelBinder(boxes[i]).BindCheckBox(view, boxes[i]).UpdateNow();
				ToolTip.SetTip(view, boxes[i].Text); // long names are trimmed
				Grid.SetRow(view, rows > 0 ? i % rows : 0);
				Grid.SetColumn(view, rows > 0 ? i / rows : 0);
				grid.Children.Add(view);
			}
		}
	}
}
