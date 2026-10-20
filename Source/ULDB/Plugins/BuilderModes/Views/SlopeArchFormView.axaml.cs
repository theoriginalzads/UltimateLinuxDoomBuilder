// ULDB: code-behind for SlopeArchFormView.axaml, the window of BuilderModes' SlopeArchForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public partial class SlopeArchFormView : Window
	{
		public SlopeArchFormView()
		{
			InitializeComponent();
		}

		internal SlopeArchFormView(SlopeArchForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
