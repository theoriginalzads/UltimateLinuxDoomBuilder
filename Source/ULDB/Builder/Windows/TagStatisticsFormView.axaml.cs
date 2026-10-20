// ULDB: code-behind for TagStatisticsFormView.axaml, the window of upstream's TagStatisticsForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class TagStatisticsFormView : Window
	{
		public TagStatisticsFormView()
		{
			InitializeComponent();
		}

		internal TagStatisticsFormView(TagStatisticsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
