// ULDB: registers this plugin's Avalonia views. Runs when the plugin assembly is first used
// (the plugin manager creating its BuilderPlug).

using System.Runtime.CompilerServices;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ThreeDFloorMode
{
	internal static class PluginViews
	{
		[ModuleInitializer]
		internal static void Register()
		{
			TabPageViews.Register<PreferencesForm>((f, page) => new PreferencesFormView(f));
			FormViews.Register<SlopeDataSectorDialog>(f => new SlopeDataSectorDialogView(f));
			ControlViews.Register<ThreeDFloorPanel>(m => new ThreeDFloorPanelView(m));
			ControlViews.Register<ThreeDFloorHelperTooltipElementControl>(m => new ThreeDFloorTooltipView(m));
			FormViews.Register<ControlSectorAreaConfig>(f => new ControlSectorAreaConfigView(f));
			FormViews.Register<SlopeVertexEditForm>(f => new SlopeVertexEditFormView(f));
			FormViews.Register<ThreeDFloorEditorWindow>(f => new ThreeDFloorEditorWindowView(f));
			ControlViews.Register<ThreeDFloorHelperControl>(m => new ThreeDFloorHelperControlView(m));
		}
	}
}
