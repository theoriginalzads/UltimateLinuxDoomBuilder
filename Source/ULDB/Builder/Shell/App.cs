// ULDB: the Avalonia application object. No XAML: the theme is added in code.

using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace ULDB.Shell
{
	internal sealed class App : Application
	{
		public override void Initialize()
		{
			Name = Branding.NAME;
			Styles.Add(new FluentTheme());
			Styles.Add(new StyleInclude(new System.Uri("avares://Builder/"))
			{
				Source = new System.Uri("avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml")
			});

			// Follow the desktop's light/dark setting.
			RequestedThemeVariant = ThemeVariant.Default;
		}
	}
}
