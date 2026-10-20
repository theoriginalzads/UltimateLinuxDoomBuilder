// ULDB: code-behind for SlopeDataSectorDialogView.axaml, the window of 3D Floor Mode's SlopeDataSectorDialog.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ThreeDFloorMode
{
	public partial class SlopeDataSectorDialogView : Window
	{
		public SlopeDataSectorDialogView()
		{
			InitializeComponent();
		}

		internal SlopeDataSectorDialogView(SlopeDataSectorDialog form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
