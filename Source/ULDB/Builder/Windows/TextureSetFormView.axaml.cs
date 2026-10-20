// ULDB: code-behind for TextureSetFormView.axaml, the window of upstream's TextureSetForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class TextureSetFormView : Window
	{
		public TextureSetFormView()
		{
			InitializeComponent();
		}

		internal TextureSetFormView(TextureSetForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
