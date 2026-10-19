// ULDB: code-behind for ArgumentBoxView.axaml, the view of upstream's ArgumentBox.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ArgumentBoxView : UserControl
	{
		public ArgumentBoxView()
		{
			InitializeComponent();
		}

		internal ArgumentBoxView(ArgumentBox model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			spin.Content = new ULDB.Controls.SpinButtons(binder.Field<WF.ScrollBar>("scrollbuttons"));
			binder.UpdateNow();
		}
	}
}
