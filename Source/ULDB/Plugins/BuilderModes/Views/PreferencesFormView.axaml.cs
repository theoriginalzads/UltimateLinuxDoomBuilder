// ULDB: code-behind for PreferencesFormView.axaml, Builder Modes's tab in the Preferences dialog.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public partial class PreferencesFormView : UserControl
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
