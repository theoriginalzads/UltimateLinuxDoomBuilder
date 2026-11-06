// ULDB: code-behind for LinedefColorPresetsFormView.axaml, the window of upstream's LinedefColorPresetsForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class LinedefColorPresetsFormView : Window
	{
		public LinedefColorPresetsFormView()
		{
			InitializeComponent();
		}

		internal LinedefColorPresetsFormView(LinedefColorPresetsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
