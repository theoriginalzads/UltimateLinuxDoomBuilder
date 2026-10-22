// ULDB: code-behind for ColorConfigurationView.axaml, the window of Sound Propagation mode's ColorConfiguration.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	public partial class ColorConfigurationView : Window
	{
		public ColorConfigurationView()
		{
			InitializeComponent();
		}

		internal ColorConfigurationView(ColorConfiguration form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
