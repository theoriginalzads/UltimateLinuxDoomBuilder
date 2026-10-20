// ULDB: code-behind for SectorDrawingOptionsPanelView.axaml, the view of BuilderModes' Draw Settings docker.

using Avalonia.Controls;
using CodeImp.DoomBuilder.BuilderModes.Interface;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public partial class SectorDrawingOptionsPanelView : UserControl
	{
		public SectorDrawingOptionsPanelView()
		{
			InitializeComponent();
		}

		internal SectorDrawingOptionsPanelView(SectorDrawingOptionsPanel model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
