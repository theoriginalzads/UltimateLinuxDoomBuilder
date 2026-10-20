// ULDB: registers this plugin's Avalonia views for its model-ported UI. Runs when the plugin
// assembly is first used (the plugin manager creating its BuilderPlug).

using System.Runtime.CompilerServices;
using CodeImp.DoomBuilder.BuilderModes.Interface;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal static class PluginViews
	{
		[ModuleInitializer]
		internal static void Register()
		{
			ControlViews.Register<UndoRedoPanel>(m => new UndoRedoPanelView(m));
			ControlViews.Register<SectorDrawingOptionsPanel>(m => new SectorDrawingOptionsPanelView(m));
			ControlViews.Register<DrawGridOptionsPanel>(m => new DrawGridOptionsPanelView(m));
			ControlViews.Register<EditSelectionPanel>(m => new EditSelectionPanelView(m));
			FormViews.Register<ErrorCheckForm>(f => new ErrorCheckFormView(f));
			FormViews.Register<FindReplaceForm>(f => new FindReplaceFormView(f));
		}
	}
}
