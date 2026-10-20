// ULDB: code-behind for ColorFieldsControlView.axaml, the view of upstream's ColorFieldsControl.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ColorFieldsControlView : UserControl
	{
		public ColorFieldsControlView()
		{
			InitializeComponent();
		}

		internal ColorFieldsControlView(ColorFieldsControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
