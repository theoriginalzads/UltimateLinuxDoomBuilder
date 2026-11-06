// ULDB: registers this plugin's Avalonia views for its model-ported UI. Runs when the plugin
// assembly is first used (the plugin manager creating its BuilderPlug).

using System.Runtime.CompilerServices;
using CodeImp.DoomBuilder.ColorPicker.Controls;
using CodeImp.DoomBuilder.ColorPicker.Windows;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ColorPicker
{
	internal static class PluginViews
	{
		[ModuleInitializer]
		internal static void Register()
		{
			ControlViews.Register<ColorPickerControl>(m => new ColorPickerControlView(m));
			ControlViews.Register<ColorPickerSlider>(m => new ColorPickerSliderView(m));
			FormViews.Register<LightColorPicker>(f => new LightColorPickerView(f));
			FormViews.Register<SectorColorPicker>(f => new SectorColorPickerView(f));
		}
	}
}
