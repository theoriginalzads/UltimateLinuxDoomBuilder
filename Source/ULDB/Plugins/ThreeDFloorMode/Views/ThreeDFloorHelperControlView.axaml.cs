// ULDB: code-behind for ThreeDFloorHelperControlView.axaml.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ThreeDFloorMode
{
	public partial class ThreeDFloorHelperControlView : UserControl
	{
		public ThreeDFloorHelperControlView()
		{
			InitializeComponent();
		}

		internal ThreeDFloorHelperControlView(ThreeDFloorHelperControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
