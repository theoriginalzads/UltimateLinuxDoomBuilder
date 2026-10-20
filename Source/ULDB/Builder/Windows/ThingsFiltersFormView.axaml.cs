// ULDB: code-behind for ThingsFiltersFormView.axaml, the window of upstream's ThingsFiltersForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ThingsFiltersFormView : Window
	{
		public ThingsFiltersFormView()
		{
			InitializeComponent();
		}

		internal ThingsFiltersFormView(ThingsFiltersForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
