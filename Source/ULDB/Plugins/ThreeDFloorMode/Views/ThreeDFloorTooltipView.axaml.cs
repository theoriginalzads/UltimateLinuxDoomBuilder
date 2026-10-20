// ULDB: code-behind for ThreeDFloorTooltipView.axaml, one 3D floor in 3D Floor mode's docker.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ThreeDFloorMode
{
	public partial class ThreeDFloorTooltipView : UserControl
	{
		public ThreeDFloorTooltipView()
		{
			InitializeComponent();
		}

		internal ThreeDFloorTooltipView(ThreeDFloorHelperTooltipElementControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.Updated += () => frame.Classes.Set("highlighted", model.Highlighted);
			binder.UpdateNow();
		}
	}
}
