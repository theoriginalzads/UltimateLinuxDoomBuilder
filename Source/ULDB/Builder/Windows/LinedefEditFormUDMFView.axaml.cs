// ULDB: code-behind for LinedefEditFormUDMFView.axaml, the window of upstream's LinedefEditFormUDMF.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class LinedefEditFormUDMFView : Window
	{
		public LinedefEditFormUDMFView()
		{
			InitializeComponent();
		}

		internal LinedefEditFormUDMFView(LinedefEditFormUDMF form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
