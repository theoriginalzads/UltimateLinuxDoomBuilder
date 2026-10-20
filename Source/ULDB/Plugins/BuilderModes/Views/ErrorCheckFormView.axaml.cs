// ULDB: code-behind for ErrorCheckFormView.axaml, the window of Map Analysis mode.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public partial class ErrorCheckFormView : Window
	{
		public ErrorCheckFormView()
		{
			InitializeComponent();
		}

		internal ErrorCheckFormView(ErrorCheckForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
