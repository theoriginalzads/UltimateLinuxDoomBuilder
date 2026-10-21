// ULDB: code-behind for ConfigFormView.axaml, the window of upstream's ConfigForm (model-ported).

using System.Reflection;
using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ConfigFormView : Window
	{
		public ConfigFormView()
		{
			InitializeComponent();
		}

		internal ConfigFormView(ConfigForm form) : this()
		{
			// "Use short paths" is Windows 8.3 path mangling and "Use Linux paths" converts Windows/Wine paths:
			// neither means anything for a native Linux build (issue #19), so both stay off and hidden.
			HideField(form, "shortpaths");
			HideField(form, "linuxpaths");
			new ModelBinder(form).BindByName(this).UpdateNow();
		}

		private static void HideField(ConfigForm form, string name)
		{
			(typeof(ConfigForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(form) as System.Windows.Forms.Control)?.Hide();
		}
	}
}
