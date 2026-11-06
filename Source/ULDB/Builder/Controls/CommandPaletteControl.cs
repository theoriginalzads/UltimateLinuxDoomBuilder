// ULDB: the model of upstream's CommandPaletteControl (the Ctrl+Shift+P style command list). Upstream's
// control is a WinForms list view with auto-sized columns; ULDB shows the palette as an Avalonia
// overlay (Shell/CommandPaletteOverlay.cs) and keeps this class so MainForm's code runs unchanged:
// it adds the control to the form, calls MakeVisible, and reads Visible to keep the map view unfocused.

using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public class CommandPaletteControl : UserControl
	{
		public CommandPaletteControl()
		{
			Visible = false;
		}

		public void MakeVisible()
		{
			Visible = true;
			ULDB.Shell.CommandPaletteOverlay.Open(() => Visible = false);
		}
	}
}
