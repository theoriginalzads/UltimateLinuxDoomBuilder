// ULDB: code-behind for PreAndPostCommandsFormView.axaml, the window of upstream's PreAndPostCommandsForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class PreAndPostCommandsFormView : Window
	{
		public PreAndPostCommandsFormView()
		{
			InitializeComponent();
		}

		internal PreAndPostCommandsFormView(PreAndPostCommandsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
