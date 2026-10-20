// ULDB: code-behind for FindReplaceFormView.axaml, the window of Find and Replace mode.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public partial class FindReplaceFormView : Window
	{
		public FindReplaceFormView()
		{
			InitializeComponent();
		}

		internal FindReplaceFormView(FindReplaceForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
