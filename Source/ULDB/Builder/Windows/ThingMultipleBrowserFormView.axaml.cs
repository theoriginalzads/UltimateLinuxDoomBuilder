// ULDB: code-behind for ThingMultipleBrowserFormView.axaml, the window of upstream's ThingMultipleBrowserForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ThingMultipleBrowserFormView : Window
	{
		public ThingMultipleBrowserFormView()
		{
			InitializeComponent();
		}

		internal ThingMultipleBrowserFormView(ThingMultipleBrowserForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
