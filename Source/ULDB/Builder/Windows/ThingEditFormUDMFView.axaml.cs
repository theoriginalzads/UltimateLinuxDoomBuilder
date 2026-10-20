// ULDB: code-behind for ThingEditFormUDMFView.axaml, the window of upstream's ThingEditFormUDMF.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ThingEditFormUDMFView : Window
	{
		public ThingEditFormUDMFView()
		{
			InitializeComponent();
		}

		internal ThingEditFormUDMFView(ThingEditFormUDMF form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
