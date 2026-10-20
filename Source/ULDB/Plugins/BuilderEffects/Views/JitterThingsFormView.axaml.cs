// ULDB: code-behind for JitterThingsFormView.axaml, the window of BuilderEffects' JitterThingsForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderEffects
{
	public partial class JitterThingsFormView : Window
	{
		public JitterThingsFormView()
		{
			InitializeComponent();
		}

		internal JitterThingsFormView(JitterThingsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
