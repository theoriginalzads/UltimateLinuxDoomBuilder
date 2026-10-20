// ULDB: code-behind for PasteOptionsFormView.axaml, the window of upstream's PasteOptionsForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class PasteOptionsFormView : Window
	{
		public PasteOptionsFormView()
		{
			InitializeComponent();
		}

		internal PasteOptionsFormView(PasteOptionsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
