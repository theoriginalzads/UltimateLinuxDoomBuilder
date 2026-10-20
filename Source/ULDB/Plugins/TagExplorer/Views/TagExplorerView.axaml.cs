// ULDB: code-behind for TagExplorerView.axaml, the view of Tag Explorer's docker control.

using Avalonia.Controls;
using ULDB.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.TagExplorer
{
	public partial class TagExplorerView : UserControl
	{
		public TagExplorerView()
		{
			InitializeComponent();
		}

		internal TagExplorerView(TagExplorer model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();

			// Upstream's node icons are an ImageList in the .resx, which can't be read on Linux:
			// ImageIndex 0/1 things (group/item), 2/3 sectors, 4/5 linedefs.
			if(treeView.Content is TreeViewView tree)
				tree.IconProvider = node => BreezeIcons.Create(IconKey(node.ImageIndex), 16);
		}

		private static string IconKey(int index)
		{
			switch(index / 2)
			{
				case 0: return "Things Mode";
				case 1: return "Sectors Mode";
				default: return "Linedefs Mode";
			}
		}
	}
}
