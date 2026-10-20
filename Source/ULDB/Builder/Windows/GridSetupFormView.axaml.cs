// ULDB: code-behind for GridSetupFormView.axaml, the window of upstream's GridSetupForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class GridSetupFormView : Window
	{
		public GridSetupFormView()
		{
			InitializeComponent();
		}

		internal GridSetupFormView(GridSetupForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
