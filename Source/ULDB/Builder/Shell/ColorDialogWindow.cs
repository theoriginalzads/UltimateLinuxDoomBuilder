// ULDB: the window behind ColorDialog.ShowDialog, built on Avalonia's ColorView.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvColor = Avalonia.Media.Color;

namespace ULDB.Shell
{
	internal sealed class ColorDialogWindow : Window
	{
		private readonly ColorView view;

		public ColorDialogWindow(System.Drawing.Color initial)
		{
			Title = "Color";
			SizeToContent = SizeToContent.WidthAndHeight;
			CanResize = false;
			ShowInTaskbar = false;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			view = new ColorView
			{
				Color = AvColor.FromArgb(255, initial.R, initial.G, initial.B),
				IsAlphaEnabled = false,
				IsAlphaVisible = false,
				Margin = new Thickness(12),
			};

			Button ok = new Button { Content = "OK", MinWidth = 80, IsDefault = true, HorizontalContentAlignment = HorizontalAlignment.Center };
			ok.Classes.Add("accent");
			ok.Click += (s, e) => { Accepted = true; Close(); };
			Button cancel = new Button { Content = "Cancel", MinWidth = 80, IsCancel = true, HorizontalContentAlignment = HorizontalAlignment.Center };
			cancel.Click += (s, e) => Close();

			StackPanel buttons = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				Spacing = 8,
				Margin = new Thickness(12, 0, 12, 12),
			};
			buttons.Children.Add(ok);
			buttons.Children.Add(cancel);

			DockPanel root = new DockPanel();
			DockPanel.SetDock(buttons, Dock.Bottom);
			root.Children.Add(buttons);
			root.Children.Add(view);
			Content = root;
		}

		public bool Accepted { get; private set; }

		public System.Drawing.Color Color
		{
			get
			{
				AvColor c = view.Color;
				return System.Drawing.Color.FromArgb(255, c.R, c.G, c.B);
			}
		}
	}
}
