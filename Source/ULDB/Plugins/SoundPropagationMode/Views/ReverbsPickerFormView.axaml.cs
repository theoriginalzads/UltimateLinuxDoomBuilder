// ULDB: code-behind for ReverbsPickerFormView.axaml, the window of Sound Propagation mode's ReverbsPickerForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	public partial class ReverbsPickerFormView : Window
	{
		public ReverbsPickerFormView()
		{
			InitializeComponent();
		}

		internal ReverbsPickerFormView(ReverbsPickerForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
