// ULDB: code-behind for BitFlagsFormView.axaml, the window of upstream's BitFlagsForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class BitFlagsFormView : Window
	{
		public BitFlagsFormView()
		{
			InitializeComponent();
		}

		internal BitFlagsFormView(BitFlagsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
