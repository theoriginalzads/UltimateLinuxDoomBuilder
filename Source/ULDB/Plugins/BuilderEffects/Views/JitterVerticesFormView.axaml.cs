// ULDB: code-behind for JitterVerticesFormView.axaml, the window of BuilderEffects' JitterVerticesForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderEffects
{
	public partial class JitterVerticesFormView : Window
	{
		public JitterVerticesFormView()
		{
			InitializeComponent();
		}

		internal JitterVerticesFormView(JitterVerticesForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
