// ULDB: code-behind for LinedefEditFormView.axaml, the window of upstream's LinedefEditForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class LinedefEditFormView : Window
	{
		public LinedefEditFormView()
		{
			InitializeComponent();
		}

		internal LinedefEditFormView(LinedefEditForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
