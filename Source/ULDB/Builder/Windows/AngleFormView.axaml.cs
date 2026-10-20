// ULDB: code-behind for AngleFormView.axaml, the window of upstream's AngleForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class AngleFormView : Window
	{
		public AngleFormView()
		{
			InitializeComponent();
		}

		internal AngleFormView(AngleForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
