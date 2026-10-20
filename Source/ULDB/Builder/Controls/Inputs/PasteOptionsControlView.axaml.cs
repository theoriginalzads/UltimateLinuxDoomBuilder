// ULDB: code-behind for PasteOptionsControlView.axaml, the view of upstream's PasteOptionsControl.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class PasteOptionsControlView : UserControl
	{
		public PasteOptionsControlView()
		{
			InitializeComponent();
		}

		internal PasteOptionsControlView(PasteOptionsControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
