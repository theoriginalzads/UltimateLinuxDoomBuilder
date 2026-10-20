// ULDB: code-behind for ImageExportSettingsFormView.axaml, the window of BuilderModes' ImageExportSettingsForm.

using Avalonia.Controls;
using CodeImp.DoomBuilder.BuilderModes.Interface;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes.Interface
{
	public partial class ImageExportSettingsFormView : Window
	{
		public ImageExportSettingsFormView()
		{
			InitializeComponent();
		}

		internal ImageExportSettingsFormView(ImageExportSettingsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
