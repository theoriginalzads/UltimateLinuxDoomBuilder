// ULDB: code-behind for PreferencesFormView.axaml, 3D Floor Mode's tab in the Preferences dialog.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ThreeDFloorMode
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
