// ULDB: registers this plugin's Avalonia views for its model-ported UI. Runs when the plugin
// assembly is first used (the plugin manager creating its BuilderPlug).

using System.Runtime.CompilerServices;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	internal static class PluginViews
	{
		[ModuleInitializer]
		internal static void Register()
		{
			ControlViews.Register<UndoRedoPanel>(m => new UndoRedoPanelView(m));
		}
	}
}
