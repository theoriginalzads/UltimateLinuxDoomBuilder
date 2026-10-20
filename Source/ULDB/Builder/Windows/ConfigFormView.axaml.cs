// ULDB: code-behind for ConfigFormView.axaml, the window of upstream's ConfigForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ConfigFormView : Window
	{
		public ConfigFormView()
		{
			InitializeComponent();
		}

		internal ConfigFormView(ConfigForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
