// ULDB: code-behind for IntControlView.axaml, the view of BuilderEffects' IntControl.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderEffects
{
	public partial class IntControlView : UserControl
	{
		public IntControlView()
		{
			InitializeComponent();
		}

		internal IntControlView(IntControl model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
