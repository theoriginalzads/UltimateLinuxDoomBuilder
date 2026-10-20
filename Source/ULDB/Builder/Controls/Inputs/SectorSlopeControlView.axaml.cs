// ULDB: code-behind for SectorSlopeControlView.axaml, the view of upstream's SectorSlopeControl.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class SectorSlopeControlView : UserControl
	{
		public SectorSlopeControlView()
		{
			InitializeComponent();
		}

		internal SectorSlopeControlView(SectorSlopeControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
