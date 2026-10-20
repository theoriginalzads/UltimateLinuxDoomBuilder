// ULDB: code-behind for ImageBrowserControlView.axaml. The tiles are upstream's ImageSelectorPanel
// ("list") shown by ImageTileList; the other controls are bound by name.

using Avalonia.Controls;
using ULDB.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ImageBrowserControlView : UserControl
	{
		public ImageBrowserControlView()
		{
			InitializeComponent();
		}

		internal ImageBrowserControlView(ImageBrowserControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			scroller.Content = new ImageTileList(binder.Field<ImageSelectorPanel>("list"));

			// HideInputBox collapses the splitter's second panel, which holds the filter controls.
			WF.SplitContainer splitter = binder.Field<WF.SplitContainer>("splitter");
			splitter.StylesChanged += (s, e) => inputpanel.IsVisible = !splitter.Panel2Collapsed;
			inputpanel.IsVisible = !splitter.Panel2Collapsed;
			binder.UpdateNow();
		}
	}
}
