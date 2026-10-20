// ULDB: code-behind for ScriptFindReplaceFormView.axaml, the window of upstream's ScriptFindReplaceForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ScriptFindReplaceFormView : Window
	{
		public ScriptFindReplaceFormView()
		{
			InitializeComponent();
		}

		internal ScriptFindReplaceFormView(ScriptFindReplaceForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
