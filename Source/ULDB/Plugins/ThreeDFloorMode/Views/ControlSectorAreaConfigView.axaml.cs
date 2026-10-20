// ULDB: code-behind for ControlSectorAreaConfigView.axaml.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ThreeDFloorMode
{
	public partial class ControlSectorAreaConfigView : Window
	{
		public ControlSectorAreaConfigView()
		{
			InitializeComponent();
		}

		internal ControlSectorAreaConfigView(ControlSectorAreaConfig model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.UpdateNow();
		}
	}
}
