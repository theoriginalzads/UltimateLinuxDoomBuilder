// ULDB: code-behind for LinedefInfoView.axaml. Upstream's LinedefInfoPanel (model-ported) fills
// its labels as always; the controls here are bound to them by name.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class LinedefInfoView : UserControl
	{
		public LinedefInfoView()
		{
			InitializeComponent();
		}

		internal LinedefInfoView(LinedefInfoPanel model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			WF.Control arg1 = binder.Field<WF.Control>("arg1");
			binder.Custom(() => args.IsVisible = arg1.Visible, arg1); // Doom format: no arguments
			binder.UpdateNow();
		}
	}
}
