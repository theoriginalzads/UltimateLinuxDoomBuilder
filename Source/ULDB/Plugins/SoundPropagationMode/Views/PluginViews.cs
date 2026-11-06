// ULDB: registers this plugin's Avalonia views. Runs when the plugin assembly is first used
// (the plugin manager creating its BuilderPlug).

using System.Runtime.CompilerServices;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	internal static class PluginViews
	{
		[ModuleInitializer]
		internal static void Register()
		{
			FormViews.Register<ColorConfiguration>(f => new ColorConfigurationView(f));
			FormViews.Register<ReverbsPickerForm>(f => new ReverbsPickerFormView(f));
			ControlViews.Register<SoundEnvironmentPanel>(m => new SoundEnvironmentPanelView(m));
		}
	}
}
