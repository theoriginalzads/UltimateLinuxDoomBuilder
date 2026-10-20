// ULDB: code-behind for ChangeMapElementIndexFormView.axaml, the window of BuilderModes' ChangeMapElementIndexForm.

using Avalonia.Controls;
using CodeImp.DoomBuilder.BuilderModes.Interface;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
	public partial class ChangeMapElementIndexFormView : Window
	{
		public ChangeMapElementIndexFormView()
		{
			InitializeComponent();
		}

		internal ChangeMapElementIndexFormView(ChangeMapElementIndexForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
