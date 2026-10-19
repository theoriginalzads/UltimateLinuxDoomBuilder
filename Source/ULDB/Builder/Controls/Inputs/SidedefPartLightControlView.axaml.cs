// ULDB: code-behind for SidedefPartLightControlView.axaml, the view of upstream's SidedefPartLightControl.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class SidedefPartLightControlView : UserControl
	{
		public SidedefPartLightControlView()
		{
			InitializeComponent();
		}

		internal SidedefPartLightControlView(SidedefPartLightControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
