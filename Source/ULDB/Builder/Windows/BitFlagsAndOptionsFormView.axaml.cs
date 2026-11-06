// ULDB: code-behind for BitFlagsAndOptionsFormView.axaml, the window of upstream's BitFlagsAndOptionsForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class BitFlagsAndOptionsFormView : Window
	{
		public BitFlagsAndOptionsFormView()
		{
			InitializeComponent();
		}

		internal BitFlagsAndOptionsFormView(BitFlagsAndOptionsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
