// ULDB: code-behind for JitterSectorsFormView.axaml, the window of BuilderEffects' JitterSectorsForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderEffects
{
	public partial class JitterSectorsFormView : Window
	{
		public JitterSectorsFormView()
		{
			InitializeComponent();
		}

		internal JitterSectorsFormView(JitterSectorsForm form) : this()
		{
			new ModelBinder(form).BindByName(this).UpdateNow();
		}
	}
}
