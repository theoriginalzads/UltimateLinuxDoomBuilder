// ULDB: upstream's VSTabControl (the script editor's document tabs, drawn by hand upstream) as a
// model: the tab pages and the close button's event; the view is ScriptEditorPanelView's tab strip.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public class VSTabControl : TabControl
	{
		public event EventHandler<TabControlEventArgs> OnCloseTabClicked;

		public bool ShowClosingButton { get; set; }
		public Color ActiveColor { get; set; }
		public Color HighlightColor { get; set; }
		public Color BackTabColor { get; set; }
		public Color BorderColor { get; set; }
		public Color TextColor { get; set; }
		public Color SelectedTextColor { get; set; }

		/// <summary>The view's close button of a tab was clicked.</summary>
		internal void RaiseCloseTabClicked(TabPage page)
		{
			OnCloseTabClicked?.Invoke(this, new TabControlEventArgs(page, TabPages.IndexOf(page), TabControlAction.Selected));
		}
	}
}
