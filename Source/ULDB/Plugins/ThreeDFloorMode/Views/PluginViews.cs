// ULDB: registers this plugin's Avalonia windows for its model-ported forms. Runs when the
// plugin assembly is first used (the plugin manager creating its BuilderPlug).

using System.Runtime.CompilerServices;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ThreeDFloorMode
{
	internal static class PluginViews
	{
		[ModuleInitializer]
		internal static void Register()
		{
			FormViews.Register<SlopeDataSectorDialog>(f => new SlopeDataSectorDialogView(f));
		}
	}
}
