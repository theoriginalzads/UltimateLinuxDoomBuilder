// ULDB: code-behind for MakeDoorFormView.axaml, the window of BuilderModes' MakeDoorForm.

using Avalonia.Controls;
using CodeImp.DoomBuilder.BuilderModes.Interface;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
	public partial class MakeDoorFormView : Window
	{
		public MakeDoorFormView()
		{
			InitializeComponent();
		}

		internal MakeDoorFormView(MakeDoorForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
