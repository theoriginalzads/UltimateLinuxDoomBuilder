// ULDB: code-behind for PreferencesFormView.axaml, the window of upstream's PreferencesForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class PreferencesFormView : Window
	{
		public PreferencesFormView()
		{
			InitializeComponent();
		}

		internal PreferencesFormView(PreferencesForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
