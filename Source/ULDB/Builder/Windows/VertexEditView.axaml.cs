// ULDB: code-behind for VertexEditView.axaml, the window of upstream's VertexEditForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class VertexEditView : Window
	{
		public VertexEditView()
		{
			InitializeComponent();
		}

		internal VertexEditView(VertexEditForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
