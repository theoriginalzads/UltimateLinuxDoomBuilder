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

			// Builds MainForm, loads configurations and plugins, then calls Application.Run,
			// which runs the Avalonia main loop until the main window closes.
			General.Main(args);
			return 0;
		}

		public static AppBuilder BuildAvaloniaApp()
		{
			return AppBuilder.Configure<App>()
				.UsePlatformDetect()
				.With(new X11PlatformOptions { WmClass = Branding.EXECUTABLE })
				.LogToTrace();
		}
	}
}
