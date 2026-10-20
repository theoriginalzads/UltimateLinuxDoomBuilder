// ULDB: code-behind for TagRangeFormView.axaml, the window of Tag Range's TagRangeForm.

using Avalonia.Controls;
using ULDB.Shell;

namespace CodeImp.DoomBuilder.TagRange
{
	public partial class TagRangeFormView : Window
	{
		public TagRangeFormView()
		{
			InitializeComponent();
		}

		internal TagRangeFormView(TagRangeForm form) : this()
		{
			ModelBinder binder = new ModelBinder(form).BindByName(this);
			// The note box shows only while something in it is visible (upstream toggles the labels).
			binder.Updated += () => warningnote.IsVisible = doubletagwarning.IsVisible || outoftagswarning.IsVisible || skipdoubletags.IsVisible;
			binder.UpdateNow();
		}
	}
}
