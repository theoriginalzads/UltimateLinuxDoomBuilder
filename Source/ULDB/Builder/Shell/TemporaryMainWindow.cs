// ULDB: stands in for the main window until MainForm is ported (Phase 3, step 4).
// Delete this file then; AvaloniaHost.Run uses the port's own window.

using System.Windows.Forms;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using CodeImp.DoomBuilder;

namespace ULDB.Shell
{
	internal sealed class TemporaryMainWindow : Window
	{
		private bool closing;

		public TemporaryMainWindow(Form mainform)
		{
			Title = Branding.NAME;
			Width = 1280;
			Height = 800;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;

			int configs = General.Configs?.Count ?? 0;
			int enabled = 0;
			if(General.Configs != null) foreach(var c in General.Configs) if(c.Enabled) enabled++;

			StackPanel panel = new StackPanel
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Spacing = 8,
			};
			panel.Children.Add(new TextBlock { Text = Branding.NAME, FontSize = 28, FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
			panel.Children.Add(new TextBlock { Text = Branding.TAGLINE, Opacity = 0.7, HorizontalAlignment = HorizontalAlignment.Center });
			panel.Children.Add(new TextBlock
			{
				Text = "Editor started: " + configs + " game configurations loaded (" + enabled + " enabled).\nThe main window (menus, toolbars, map view) is ported next.",
				TextAlignment = TextAlignment.Center,
				Margin = new Thickness(0, 24, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Center,
			});
			Content = panel;

			// Same as upstream's MainForm.OnFormClosing: close the map (asking to save),
			// then save settings and end the program.
			Closing += (s, e) =>
			{
				if(closing) return;
				if(General.CloseMap())
				{
					closing = true;
					General.WriteLogLine("Closing main interface window...");
					General.Terminate(true);
				}
				else
				{
					e.Cancel = true;
				}
			};
		}
	}
}
