// ULDB: code-behind for LightColorPickerView.axaml, the window of the ColorPicker plugin's LightColorPicker.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ColorPicker.Windows
{
	public partial class LightColorPickerView : Window
	{
		public LightColorPickerView()
		{
			InitializeComponent();
		}

		internal LightColorPickerView(LightColorPicker form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
