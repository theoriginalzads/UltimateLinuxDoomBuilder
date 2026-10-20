// ULDB: code-behind for EffectBrowserFormView.axaml, the window of upstream's EffectBrowserForm (model-ported).

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class EffectBrowserFormView : Window
	{
		public EffectBrowserFormView()
		{
			InitializeComponent();
		}

		internal EffectBrowserFormView(EffectBrowserForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
