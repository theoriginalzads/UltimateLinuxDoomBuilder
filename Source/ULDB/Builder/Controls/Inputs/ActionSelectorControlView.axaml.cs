// ULDB: code-behind for ActionSelectorControlView.axaml, the view of upstream's ActionSelectorControl.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ActionSelectorControlView : UserControl
	{
		public ActionSelectorControlView()
		{
			InitializeComponent();
		}

		internal ActionSelectorControlView(ActionSelectorControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
