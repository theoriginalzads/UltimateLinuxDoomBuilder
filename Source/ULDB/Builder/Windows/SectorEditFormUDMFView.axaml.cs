// ULDB: code-behind for SectorEditFormUDMFView.axaml, the window of upstream's SectorEditFormUDMF.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class SectorEditFormUDMFView : Window
	{
		public SectorEditFormUDMFView()
		{
			InitializeComponent();
		}

		internal SectorEditFormUDMFView(SectorEditFormUDMF form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
