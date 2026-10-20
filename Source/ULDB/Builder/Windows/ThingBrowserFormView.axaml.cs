// ULDB: code-behind for ThingBrowserFormView.axaml, the window of upstream's ThingBrowserForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ThingBrowserFormView : Window
	{
		public ThingBrowserFormView()
		{
			InitializeComponent();
		}

		internal ThingBrowserFormView(ThingBrowserForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
