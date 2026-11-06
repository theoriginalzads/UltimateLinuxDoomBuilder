// ULDB: code-behind for SectorColorPickerView.axaml, the window of the ColorPicker plugin's SectorColorPicker.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ColorPicker.Windows
{
	public partial class SectorColorPickerView : Window
	{
		public SectorColorPickerView()
		{
			InitializeComponent();
		}

		internal SectorColorPickerView(SectorColorPicker form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
