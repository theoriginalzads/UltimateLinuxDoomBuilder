// ULDB: code-behind for FitTexturesFormView.axaml, the window of BuilderModes' FitTexturesForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public partial class FitTexturesFormView : Window
	{
		public FitTexturesFormView()
		{
			InitializeComponent();
		}

		internal FitTexturesFormView(FitTexturesForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
