// ULDB: code-behind for ThingStatisticsFormView.axaml, the window of upstream's ThingStatisticsForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ThingStatisticsFormView : Window
	{
		public ThingStatisticsFormView()
		{
			InitializeComponent();
		}

		internal ThingStatisticsFormView(ThingStatisticsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
