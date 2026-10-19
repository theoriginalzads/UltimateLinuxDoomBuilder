// ULDB: code-behind for ArgumentsControlView.axaml, the view of upstream's ArgumentsControl.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ArgumentsControlView : UserControl
	{
		public ArgumentsControlView()
		{
			InitializeComponent();
		}

		internal ArgumentsControlView(ArgumentsControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
