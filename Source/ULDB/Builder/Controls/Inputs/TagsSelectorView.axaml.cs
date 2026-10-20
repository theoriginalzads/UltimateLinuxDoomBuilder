// ULDB: code-behind for TagsSelectorView.axaml, the view of upstream's TagsSelector.

using Avalonia.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class TagsSelectorView : UserControl
	{
		public TagsSelectorView()
		{
			InitializeComponent();
		}

		internal TagsSelectorView(TagsSelector model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			spin.Content = new ULDB.Controls.SpinButtons(binder.Field<WF.ScrollBar>("buttons"));
			binder.UpdateNow();
		}
	}
}
