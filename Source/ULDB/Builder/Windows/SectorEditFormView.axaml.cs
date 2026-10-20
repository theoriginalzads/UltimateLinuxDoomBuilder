// ULDB: code-behind for SectorEditFormView.axaml, the window of upstream's SectorEditForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class SectorEditFormView : Window
	{
		public SectorEditFormView()
		{
			InitializeComponent();
		}

		internal SectorEditFormView(SectorEditForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
