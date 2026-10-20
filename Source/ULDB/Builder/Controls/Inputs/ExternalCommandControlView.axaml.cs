// ULDB: code-behind for ExternalCommandControlView.axaml, the view of upstream's ExternalCommandControl.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ExternalCommandControlView : UserControl
	{
		public ExternalCommandControlView()
		{
			InitializeComponent();
		}

		internal ExternalCommandControlView(ExternalCommandControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
