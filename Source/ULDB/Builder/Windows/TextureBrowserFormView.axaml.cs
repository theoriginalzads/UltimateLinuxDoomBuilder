// ULDB: code-behind for TextureBrowserFormView.axaml, the window of upstream's TextureBrowserForm.
// The texture set tree is upstream's MultiSelectTreeview shown by TreeViewView; its icons (an
// ImageList stored in the .resx, unreadable on Linux) are drawn here by upstream's image indexes.
// The splitter position is kept in upstream's CollapsibleSplitContainer ("splitter").

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CodeImp.DoomBuilder.Controls;
using ULDB.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class TextureBrowserFormView : Window
	{
		private static readonly Avalonia.Media.Geometry Folder = Avalonia.Media.Geometry.Parse("M1,3 H6 L8,5 H15 V14 H1 Z");
		private static readonly Avalonia.Media.Geometry File = Avalonia.Media.Geometry.Parse("M3,1 H10 L13,4 V15 H3 Z M9,1.5 V5 H12.5");
		private static readonly Avalonia.Media.Geometry Archive = Avalonia.Media.Geometry.Parse("M2,3 H14 V6 H2 Z M3,6 H13 V14 H3 Z M6.5,8.5 H9.5");
		private static readonly Avalonia.Media.Geometry All = Avalonia.Media.Geometry.Parse("M2,2 H7 V7 H2 Z M9,2 H14 V7 H9 Z M2,9 H7 V14 H2 Z M9,9 H14 V14 H9 Z");
		private static readonly Avalonia.Media.Geometry Known = Avalonia.Media.Geometry.Parse("M3,1.5 H13 V14.5 L8,11 L3,14.5 Z");

		public TextureBrowserFormView()
		{
			InitializeComponent();
		}

		internal TextureBrowserFormView(TextureBrowserForm form) : this()
		{
			// A roomier first-time size than the designer's 784x434 (DelayedForm restores a saved one on load).
			form.Size = new System.Drawing.Size(1000, 640);
			ModelBinder binder = new ModelBinder(form).BindByName(this);
			MultiSelectTreeview tree = binder.Field<MultiSelectTreeview>("tvTextureSets");
			treehost.Content = new TreeViewView(tree)
			{
				IconProvider = Icon,
				HighlightSelectedNode = false,
				SelectionColor = () => tree.SelectionBackColor,
			};

			// Splitter: the tree's width is the model's split position (as upstream stores it).
			WF.SplitContainer splitter = binder.Field<WF.SplitContainer>("splitter");
			CollapsibleSplitContainer collapsible = splitter as CollapsibleSplitContainer;
			if(collapsible != null)
			{
				splitgrid.ColumnDefinitions[2].Width = new GridLength(System.Math.Max(120, collapsible.SplitPosition));
				splitgrid.ColumnDefinitions[2].MinWidth = 100;
				splitgrid.ColumnDefinitions[0].MinWidth = 300;
				gridsplitter.DragCompleted += (s, e) =>
				{
					splitter.Width = (int)splitgrid.Bounds.Width;
					collapsible.SplitPosition = (int)splitgrid.ColumnDefinitions[2].ActualWidth;
				};
				// The model can't know the window's size; give it the full width so SplitPosition reads back.
				Opened += (s, e) => { splitter.Width = (int)splitgrid.Bounds.Width; collapsible.SplitPosition = (int)splitgrid.ColumnDefinitions[2].ActualWidth; };
			}
			binder.UpdateNow();
		}

		private static Control Icon(WF.TreeNode node)
		{
			Avalonia.Media.Geometry g;
			switch(node.ImageIndex)
			{
				case 0: g = Known; break;
				case 1: g = All; break;
				case 2: case 5: case 8: g = File; break;
				case 4: case 7: g = Archive; break;
				default: g = Folder; break;
			}
			return new PathIcon { Width = 14, Height = 14, Data = g };
		}
	}
}
