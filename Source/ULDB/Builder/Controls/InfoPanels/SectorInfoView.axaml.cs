// ULDB: code-behind for SectorInfoView.axaml. Upstream's SectorInfoPanel (model-ported) fills its labels as
// always; the controls here are bound to them by name.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class SectorInfoView : UserControl
	{
		public SectorInfoView()
		{
			InitializeComponent();
		}

		internal SectorInfoView(SectorInfoPanel model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.UpdateNow();
		}
	}
}
