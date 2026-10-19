// ULDB: code-behind for ImageSelectorControlView.axaml. Pointer input on the tile goes to
// upstream's picture box (MouseEnter/Leave, MouseDown, Click), whose handlers browse or clear.

using Avalonia.Controls;
using Avalonia.Input;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ImageSelectorControlView : UserControl
	{
		public ImageSelectorControlView()
		{
			InitializeComponent();
		}

		internal ImageSelectorControlView(ImageSelectorControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			WF.Control box = binder.Field<WF.Control>("imagebox");
			tile.PointerEntered += (s, e) => box.RaiseMouseEnter();
			tile.PointerExited += (s, e) => box.RaiseMouseLeave();
			tile.PointerPressed += (s, e) =>
			{
				PointerPointProperties p = e.GetCurrentPoint(tile).Properties;
				WF.MouseButtons button = p.IsRightButtonPressed ? WF.MouseButtons.Right : p.IsMiddleButtonPressed ? WF.MouseButtons.Middle : WF.MouseButtons.Left;
				box.RaiseMouseDown(new WF.MouseEventArgs(button, 1, 0, 0, 0));
			};
			tile.PointerReleased += (s, e) => box.RaiseClick();
			ToolTip.SetTip(tile, "Click to browse; right-click to remove");
			binder.UpdateNow();
		}
	}
}
