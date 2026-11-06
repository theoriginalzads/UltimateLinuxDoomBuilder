// ULDB: code-behind for FlagsFormView.axaml, the window of upstream's FlagsForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class FlagsFormView : Window
	{
		public FlagsFormView()
		{
			InitializeComponent();
		}

		internal FlagsFormView(FlagsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
