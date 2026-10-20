// ULDB: code-behind for StairSectorBuilderFormView.axaml, the window of StairSectorBuilderForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.StairSectorBuilderMode
{
	public partial class StairSectorBuilderFormView : Window
	{
		public StairSectorBuilderFormView()
		{
			InitializeComponent();
		}

		internal StairSectorBuilderFormView(StairSectorBuilderForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
