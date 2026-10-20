// ULDB: code-behind for SoundEnvironmentPanelView.axaml, the view of Sound Environment mode's docker.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	public partial class SoundEnvironmentPanelView : UserControl
	{
		public SoundEnvironmentPanelView()
		{
			InitializeComponent();
		}

		internal SoundEnvironmentPanelView(SoundEnvironmentPanel model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
