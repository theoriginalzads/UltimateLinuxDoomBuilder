// ULDB: code-behind for StatisticsView.axaml. Upstream's StatisticsControl (model-ported) fills its labels as
// always; the controls here are bound to them by name.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class StatisticsView : UserControl
	{
		public StatisticsView()
		{
			InitializeComponent();
		}

		internal StatisticsView(StatisticsControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.UpdateNow();
		}
	}
}
