// ULDB: code-behind for DirectionalShadingFormView.axaml, the window of BuilderEffects' DirectionalShadingForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderEffects
{
	public partial class DirectionalShadingFormView : Window
	{
		public DirectionalShadingFormView()
		{
			InitializeComponent();
		}

		internal DirectionalShadingFormView(DirectionalShadingForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
