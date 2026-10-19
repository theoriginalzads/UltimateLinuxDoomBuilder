// ULDB: code-behind for PairedFieldsControlView.axaml, the view of upstream's PairedFieldsControl.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class PairedFieldsControlView : UserControl
	{
		public PairedFieldsControlView()
		{
			InitializeComponent();
		}

		internal PairedFieldsControlView(PairedFieldsControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
