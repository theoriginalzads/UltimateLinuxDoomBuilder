// ULDB: code-behind for ColorControlView.axaml, the view of upstream's ColorControl.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ColorControlView : UserControl
	{
		public ColorControlView()
		{
			InitializeComponent();
		}

		internal ColorControlView(ColorControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			WF.Control panelmodel = binder.Field<WF.Control>("panel");
			panel.PointerReleased += (s, e) => panelmodel.RaiseClick();
			binder.UpdateNow();
		}
	}
}
