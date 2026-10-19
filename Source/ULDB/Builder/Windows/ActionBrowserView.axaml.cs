// ULDB: code-behind for ActionBrowserView.axaml, the window of upstream's ActionBrowserForm.
// The action tree is upstream's BufferedTreeView, shown by TreeViewView.

using Avalonia.Controls;
using CodeImp.DoomBuilder.Controls;
using ULDB.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ActionBrowserView : Window
	{
		public ActionBrowserView()
		{
			InitializeComponent();
		}

		internal ActionBrowserView(ActionBrowserForm form) : this()
		{
			ModelBinder binder = new ModelBinder(form).BindByName(this);
			actionshost.Content = new TreeViewView(binder.Field<BufferedTreeView>("actions"));
			binder.UpdateNow();
		}
	}
}
