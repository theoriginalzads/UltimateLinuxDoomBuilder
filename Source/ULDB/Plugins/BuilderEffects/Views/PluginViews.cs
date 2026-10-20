// ULDB: registers this plugin's Avalonia views for its model-ported forms and controls. Runs when
// the plugin assembly is first used (the plugin manager creating its BuilderPlug).

using System.Runtime.CompilerServices;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderEffects
{
	internal static class PluginViews
	{
		[ModuleInitializer]
		internal static void Register()
		{
			ControlViews.Register<IntControl>(m => new IntControlView(m));
			FormViews.Register<JitterVerticesForm>(f => new JitterVerticesFormView(f));
			FormViews.Register<JitterSectorsForm>(f => new JitterSectorsFormView(f));
			FormViews.Register<JitterThingsForm>(f => new JitterThingsFormView(f));
			FormViews.Register<DirectionalShadingForm>(f => new DirectionalShadingFormView(f));
			FormViews.Register<ObjImportSettingsForm>(f => new ObjImportSettingsFormView(f));
		}
	}
}
