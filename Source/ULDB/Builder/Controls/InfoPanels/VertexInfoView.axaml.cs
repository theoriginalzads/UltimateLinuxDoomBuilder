// ULDB: code-behind for VertexInfoView.axaml. Upstream's VertexInfoPanel (model-ported) fills its labels as
// always; the controls here are bound to them by name.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class VertexInfoView : UserControl
	{
		public VertexInfoView()
		{
			InitializeComponent();
		}

		internal VertexInfoView(VertexInfoPanel model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.UpdateNow();
		}
	}
}
