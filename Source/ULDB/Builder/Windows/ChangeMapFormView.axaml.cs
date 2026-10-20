// ULDB: code-behind for ChangeMapFormView.axaml, the window of upstream's ChangeMapForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ChangeMapFormView : Window
	{
		public ChangeMapFormView()
		{
			InitializeComponent();
		}

		internal ChangeMapFormView(ChangeMapForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
