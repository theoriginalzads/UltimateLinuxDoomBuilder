// ULDB: code-behind for AngleControlView.axaml, the view of upstream's AngleControl.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class AngleControlView : UserControl
	{
		public AngleControlView()
		{
			InitializeComponent();
		}

		internal AngleControlView(AngleControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
