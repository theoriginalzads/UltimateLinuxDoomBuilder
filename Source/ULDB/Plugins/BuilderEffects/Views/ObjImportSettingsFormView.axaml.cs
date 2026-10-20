// ULDB: code-behind for ObjImportSettingsFormView.axaml, the window of BuilderEffects' ObjImportSettingsForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderEffects
{
	public partial class ObjImportSettingsFormView : Window
	{
		public ObjImportSettingsFormView()
		{
			InitializeComponent();
		}

		internal ObjImportSettingsFormView(ObjImportSettingsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
