// ULDB: code-behind for FilterSelectedThingsFormView.axaml, the window of BuilderModes' FilterSelectedThingsForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public partial class FilterSelectedThingsFormView : Window
	{
		public FilterSelectedThingsFormView()
		{
			InitializeComponent();
		}

		internal FilterSelectedThingsFormView(FilterSelectedThingsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
