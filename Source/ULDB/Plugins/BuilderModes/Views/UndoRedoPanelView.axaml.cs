// ULDB: code-behind for UndoRedoPanelView.axaml, the view of BuilderModes' Undo/Redo docker.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.BuilderModes
{
	public partial class UndoRedoPanelView : UserControl
	{
		public UndoRedoPanelView()
		{
			InitializeComponent();
		}

		internal UndoRedoPanelView(UndoRedoPanel model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			binder.BindState(this, model);
			binder.UpdateNow();
		}
	}
}
