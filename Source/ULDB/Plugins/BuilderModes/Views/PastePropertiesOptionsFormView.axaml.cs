// ULDB: code-behind for PastePropertiesOptionsFormView.axaml, the window of BuilderModes' PastePropertiesOptionsForm.

using Avalonia.Controls;
using CodeImp.DoomBuilder.BuilderModes.Interface;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
	public partial class PastePropertiesOptionsFormView : Window
	{
		public PastePropertiesOptionsFormView()
		{
			InitializeComponent();
		}

		internal PastePropertiesOptionsFormView(PastePropertiesOptionsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
