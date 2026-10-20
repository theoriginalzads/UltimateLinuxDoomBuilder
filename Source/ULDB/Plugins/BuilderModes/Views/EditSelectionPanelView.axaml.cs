// ULDB: code-behind for EditSelectionPanelView.axaml, the view of Edit Selection mode's docker.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public partial class EditSelectionPanelView : UserControl
	{
		public EditSelectionPanelView()
		{
			InitializeComponent();
		}

		internal EditSelectionPanelView(EditSelectionPanel model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
