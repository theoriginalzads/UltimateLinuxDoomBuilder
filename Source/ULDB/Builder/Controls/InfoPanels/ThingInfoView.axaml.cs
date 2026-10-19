// ULDB: code-behind for ThingInfoView.axaml. Upstream's ThingInfoPanel (model-ported) fills its labels as
// always; the controls here are bound to them by name.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ThingInfoView : UserControl
	{
		public ThingInfoView()
		{
			InitializeComponent();
		}

		internal ThingInfoView(ThingInfoPanel model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			WF.Control arg1 = binder.Field<WF.Control>("arg1");
			binder.Custom(() => args.IsVisible = arg1.Visible, arg1); // Doom format: no arguments
			AngleControlEx dial = binder.Field<AngleControlEx>("anglecontrol");
			binder.Custom(() => anglecontrol.Angle = dial.Angle, dial);
			binder.UpdateNow();
		}
	}
}
