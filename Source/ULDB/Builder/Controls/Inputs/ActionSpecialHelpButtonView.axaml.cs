// ULDB: code-behind for ActionSpecialHelpButtonView.axaml, the view of upstream's ActionSpecialHelpButton.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ActionSpecialHelpButtonView : UserControl
	{
		public ActionSpecialHelpButtonView()
		{
			InitializeComponent();
		}

		internal ActionSpecialHelpButtonView(ActionSpecialHelpButton model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
