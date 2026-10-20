// ULDB: code-behind for ScriptGoToLineFormView.axaml, the window of upstream's ScriptGoToLineForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ScriptGoToLineFormView : Window
	{
		public ScriptGoToLineFormView()
		{
			InitializeComponent();
		}

		internal ScriptGoToLineFormView(ScriptGoToLineForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
