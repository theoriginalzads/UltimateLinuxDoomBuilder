// ULDB: code-behind for TextEditView.axaml, the window of upstream's TextEditForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class TextEditView : Window
	{
		public TextEditView()
		{
			InitializeComponent();
		}

		internal TextEditView(TextEditForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
			Opened += (s, e) => { textbox.Focus(); textbox.SelectAll(); };
		}
	}
}
