// ULDB: code-behind for TagSelectorView.axaml, the view of upstream's TagSelector.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class TagSelectorView : UserControl
	{
		public TagSelectorView()
		{
			InitializeComponent();
		}

		internal TagSelectorView(TagSelector model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			spin.Content = new ULDB.Controls.SpinButtons(binder.Field<WF.ScrollBar>("buttons"));
			binder.UpdateNow();
		}
	}
}
