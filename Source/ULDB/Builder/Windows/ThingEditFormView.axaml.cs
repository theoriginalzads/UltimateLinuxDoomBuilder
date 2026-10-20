// ULDB: code-behind for ThingEditFormView.axaml, the window of upstream's ThingEditForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ThingEditFormView : Window
	{
		public ThingEditFormView()
		{
			InitializeComponent();
		}

		internal ThingEditFormView(ThingEditForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
