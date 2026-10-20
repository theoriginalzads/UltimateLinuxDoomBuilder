// ULDB: code-behind for BridgeModeFormView.axaml, the window of BuilderModes' BridgeModeForm.

using Avalonia.Controls;
using CodeImp.DoomBuilder.BuilderModes.Interface;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
	public partial class BridgeModeFormView : Window
	{
		public BridgeModeFormView()
		{
			InitializeComponent();
		}

		internal BridgeModeFormView(BridgeModeForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
