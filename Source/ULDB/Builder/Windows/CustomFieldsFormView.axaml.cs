// ULDB: code-behind for CustomFieldsFormView.axaml, the window of upstream's CustomFieldsForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class CustomFieldsFormView : Window
	{
		public CustomFieldsFormView()
		{
			InitializeComponent();
		}

		internal CustomFieldsFormView(CustomFieldsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
