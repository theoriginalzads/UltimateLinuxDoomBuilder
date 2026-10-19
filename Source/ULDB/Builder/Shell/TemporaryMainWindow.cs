// ULDB: stands in for the main window until MainForm is ported (Phase 3, step 4).
// Delete this file then; AvaloniaHost.Run uses the port's own window.

using System;
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
		private CodeImp.DoomBuilder.Rendering.RenderDevice device;
		private float hue = 0.6f;

		private void DrawTest()
		{
			if(device == null) return;
			System.Drawing.Color c = FromHsv(hue, 0.6f, 0.35f);
			device.StartRendering(true, new CodeImp.DoomBuilder.Rendering.Color4(c.R / 255f, c.G / 255f, c.B / 255f, 1f));
			device.FinishRendering();
			device.Present();
		}

		private static System.Drawing.Color FromHsv(float h, float s, float v)
		{
			float r = Math.Clamp(Math.Abs(h * 6 - 3) - 1, 0, 1), g = Math.Clamp(2 - Math.Abs(h * 6 - 2), 0, 1), b = Math.Clamp(2 - Math.Abs(h * 6 - 4), 0, 1);
			return System.Drawing.Color.FromArgb((int)(((r - 1) * s + 1) * v * 255), (int)(((g - 1) * s + 1) * v * 255), (int)(((b - 1) * s + 1) * v * 255));
		}

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
				Margin = new Thickness(0, 16),
				Spacing = 8,
			};
			panel.Children.Add(new TextBlock { Text = Branding.NAME, FontSize = 28, FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Center });
			panel.Children.Add(new TextBlock { Text = Branding.TAGLINE, Opacity = 0.7, HorizontalAlignment = HorizontalAlignment.Center });
			panel.Children.Add(new TextBlock
			{
				Text = "Editor started: " + configs + " game configurations loaded (" + enabled + " enabled).\nThe main window (menus, toolbars, map view) is ported next.",
				TextAlignment = TextAlignment.Center,
				Margin = new Thickness(0, 8, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Center,
			});

			// Viewport smoke test: UDB's RenderDevice clears the view through the shared context.
			TextBlock status = new TextBlock { Text = "Setting up OpenGL...", Opacity = 0.7, HorizontalAlignment = HorizontalAlignment.Center };
			panel.Children.Add(status);
			ViewportControl viewport = new ViewportControl(General.MainWindow.Display) { Margin = new Thickness(16, 0, 16, 16) };
			viewport.ContextReady += error =>
			{
				if(error != null) { status.Text = "OpenGL viewport failed: " + error.Message; return; }
				device = new CodeImp.DoomBuilder.Rendering.RenderDevice(General.MainWindow.Display);
				status.Text = "Viewport: UDB's renderer (BuilderNative) on a context shared with Avalonia. Click it to change colour.";
				DrawTest();
			};
			General.MainWindow.Display.Resize += (s, e) => DrawTest();
			viewport.PointerPressed += (s, e) => { hue = (hue + 0.13f) % 1f; DrawTest(); };

			DockPanel root = new DockPanel();
			DockPanel.SetDock(panel, Dock.Top);
			root.Children.Add(panel);
			root.Children.Add(viewport);
			Content = root;

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
