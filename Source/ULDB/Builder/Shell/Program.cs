// ULDB: program entry point. Sets up Avalonia, installs the UI services the compat
// layer forwards to (WinFormsHost, Application.Run/Exit), then hands over to upstream's
// own startup, General.Main, unchanged.

using System;
using Avalonia;
using CodeImp.DoomBuilder;

namespace ULDB.Shell
{
	internal static class Program
	{
		[STAThread]
		public static int Main(string[] args)
		{
			BuildAvaloniaApp().SetupWithoutStarting();
			AvaloniaHost.Install();

			// First run: offer to import existing UDB settings before upstream loads (and creates) ours.
			// Also handles "--import-udb-settings[=PATH]", which upstream's argument parser mustn't see.
			args = ULDB.Settings.UdbSettingsImport.RunAtStartup(args);

			// Builds MainForm, loads configurations and plugins, then calls Application.Run,
			// which runs the Avalonia main loop until the main window closes.
			General.Main(args);
			return 0;
		}

		public static AppBuilder BuildAvaloniaApp()
		{
			return AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.With(X11Options())
				.LogToTrace();
		}

		// Development aid: ULDB_RENDERING=glx|egl|software forces Avalonia's X11 rendering mode
		// (headless Xvfb sessions otherwise fall back to software, which has no map view).
		private static X11PlatformOptions X11Options()
		{
			X11PlatformOptions options = new X11PlatformOptions { WmClass = Branding.EXECUTABLE };
			switch(Environment.GetEnvironmentVariable("ULDB_RENDERING")?.ToLowerInvariant())
			{
				case "glx": options.RenderingMode = new[] { X11RenderingMode.Glx }; break;
				case "egl": options.RenderingMode = new[] { X11RenderingMode.Egl }; break;
				case "software": options.RenderingMode = new[] { X11RenderingMode.Software }; break;
			}
			return options;
		}
	}
}
