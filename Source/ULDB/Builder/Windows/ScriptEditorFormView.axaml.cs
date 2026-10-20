// ULDB: code-behind for ScriptEditorFormView.axaml, the window of upstream's ScriptEditorForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ScriptEditorFormView : Window
	{
		public ScriptEditorFormView()
		{
			InitializeComponent();
		}

		internal ScriptEditorFormView(ScriptEditorForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
