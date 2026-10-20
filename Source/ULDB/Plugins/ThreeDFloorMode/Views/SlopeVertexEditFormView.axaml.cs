// ULDB: code-behind for SlopeVertexEditFormView.axaml.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.ThreeDFloorMode
{
	public partial class SlopeVertexEditFormView : Window
	{
		public SlopeVertexEditFormView()
		{
			InitializeComponent();
		}

		internal SlopeVertexEditFormView(SlopeVertexEditForm model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.UpdateNow();
		}
	}
}
