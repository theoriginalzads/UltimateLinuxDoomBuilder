// ULDB: code-behind for ColorPickerSliderView.axaml, the view of the ColorPicker plugin's ColorPickerSlider.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ColorPicker.Controls
{
	public partial class ColorPickerSliderView : UserControl
	{
		public ColorPickerSliderView()
		{
			InitializeComponent();
		}

		internal ColorPickerSliderView(ColorPickerSlider model) : this()
		{
			new ModelBinder(model).BindByName(this).UpdateNow();
		}
	}
}
