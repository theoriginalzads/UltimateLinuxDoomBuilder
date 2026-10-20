// ULDB: code-behind for DrawGridOptionsPanelView.axaml, the view of Draw Grid mode's docker.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public partial class DrawGridOptionsPanelView : UserControl
	{
		public DrawGridOptionsPanelView()
		{
			InitializeComponent();
		}

		internal DrawGridOptionsPanelView(DrawGridOptionsPanel model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
