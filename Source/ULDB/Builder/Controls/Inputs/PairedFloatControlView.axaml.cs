// ULDB: code-behind for PairedFloatControlView.axaml, the view of upstream's PairedFloatControl.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class PairedFloatControlView : UserControl
	{
		public PairedFloatControlView()
		{
			InitializeComponent();
		}

		internal PairedFloatControlView(PairedFloatControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
