// ULDB: code-behind for CenterOnCoordinatesFormView.axaml, the window of upstream's CenterOnCoordinatesForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class CenterOnCoordinatesFormView : Window
	{
		public CenterOnCoordinatesFormView()
		{
			InitializeComponent();
		}

		internal CenterOnCoordinatesFormView(CenterOnCoordinatesForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
