// ULDB: code-behind for PairedIntControlView.axaml, the view of upstream's PairedIntControl.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class PairedIntControlView : UserControl
	{
		public PairedIntControlView()
		{
			InitializeComponent();
		}

		internal PairedIntControlView(PairedIntControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
