// ULDB: registers this plugin's Avalonia view for its docker control. Runs when the plugin
// assembly is first used (the plugin manager creating its BuilderPlug).

using System.Runtime.CompilerServices;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.CommentsPanel
{
	internal static class PluginViews
	{
		[ModuleInitializer]
		internal static void Register()
		{
			ControlViews.Register<CommentsDocker>(m => new CommentsDockerView(m));
		}
	}
}
