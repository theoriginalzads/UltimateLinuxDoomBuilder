// ULDB: the model of upstream's CheckedColoredListBox (Linedef Color Presets: a checkable list whose
// rows show a colour swatch). Upstream's version is owner-drawn (DrawMode, CheckBoxRenderer);
// here the control is just a CheckedListBox with a warning icon, and ModelBinder draws the rows
// from IColoredListBoxItem.

using System.Drawing;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public class CheckedColoredListBox : CheckedListBox
	{
		public Image WarningIcon { get; set; }
	}

	interface IColoredListBoxItem
	{
		bool ShowWarning { get; }
		Color Color { get; }
	}
}
