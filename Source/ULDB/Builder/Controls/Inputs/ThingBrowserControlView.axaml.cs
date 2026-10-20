// ULDB: code-behind for ThingBrowserControlView.axaml. The type tree is upstream's
// MultiSelectTreeview shown by TreeViewView; its icons (an ImageList stored in the .resx,
// unreadable on Linux) are drawn here: a dot in the thing's colour, a folder in the category's
// colour, or a warning sign, by upstream's image indexes.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CodeImp.DoomBuilder.Rendering;
using ULDB.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ThingBrowserControlView : UserControl
	{
		// Upstream's ImageList layout (ThingBrowserControl.WARNING_ICON_INDEX / FOLDER_ICON_OFFSET).
		private const int WARNING_ICON_INDEX = 20;
		private const int FOLDER_ICON_OFFSET = 21;
		private const int FOLDER_OPEN_ICON_OFFSET = 41;

		public ThingBrowserControlView()
		{
			InitializeComponent();
		}

		internal ThingBrowserControlView(ThingBrowserControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			MultiSelectTreeview tree = binder.Field<MultiSelectTreeview>("typelist");
			typelisthost.Content = new TreeViewView(tree)
			{
				IconProvider = Icon,
				HighlightSelectedNode = false,
				SelectionColor = () => tree.SelectionBackColor,
			};
			binder.UpdateNow();
		}

		private static Control Icon(WF.TreeNode node)
		{
			int index = node.ImageIndex;
			if(index == WARNING_ICON_INDEX)
				return new PathIcon { Width = 14, Height = 14, Data = Avalonia.Media.Geometry.Parse("M8,1 L15,14 H1 Z M7.25,5 V10 H8.75 V5 Z M7.25,11 V12.5 H8.75 V11 Z"), Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xA3, 0x17)) };
			bool open = index >= FOLDER_OPEN_ICON_OFFSET;
			bool folder = index >= FOLDER_ICON_OFFSET;
			int color = open ? index - FOLDER_OPEN_ICON_OFFSET : folder ? index - FOLDER_ICON_OFFSET : index;
			IBrush brush = ThingColor(color);
			if(open)
				return new PathIcon { Width = 15, Height = 13, Data = Avalonia.Media.Geometry.Parse("M1,3 H6 L8,5 H14 V7 H4 L1,14 Z M4.5,8 H16 L13,14 H1.5 Z"), Foreground = brush };
			if(folder)
				return new PathIcon { Width = 15, Height = 13, Data = Avalonia.Media.Geometry.Parse("M1,3 H6 L8,5 H15 V14 H1 Z"), Foreground = brush };
			return new Border { Width = 11, Height = 11, CornerRadius = new CornerRadius(6), Background = brush, BorderBrush = new SolidColorBrush(Color.FromArgb(0x60, 0, 0, 0)), BorderThickness = new Thickness(1), Margin = new Thickness(2, 0, 2, 0) };
		}

		// The editor's thing colours (as the 2D view draws things), or grey when unknown.
		private static IBrush ThingColor(int index)
		{
			if(General.Colors == null || index < 0 || index >= ColorCollection.NUM_THING_COLORS) return Brushes.Gray;
			PixelColor c = General.Colors.Colors[index + ColorCollection.THING_COLORS_OFFSET];
			return new SolidColorBrush(Color.FromRgb(c.r, c.g, c.b));
		}
	}
}
