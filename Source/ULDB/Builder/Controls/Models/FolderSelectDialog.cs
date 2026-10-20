// ULDB: upstream's FolderSelectDialog (a Windows Vista COM dialog reached by reflection) on the
// desktop's folder picker, with the same members.

using System;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public class FolderSelectDialog
	{
		private string initialdirectory, title, filename = "";

		public string InitialDirectory { get { return initialdirectory; } set { initialdirectory = value; } }
		public string Title { get { return title; } set { title = value; } }
		public string FileName { get { return filename; } }

		public bool ShowDialog() { return ShowDialog(IntPtr.Zero); }

		public bool ShowDialog(IntPtr hWndOwner)
		{
			FolderBrowserDialog dialog = new FolderBrowserDialog { Description = title ?? "", SelectedPath = initialdirectory ?? "" };
			if(dialog.ShowDialog(Form.ActiveForm) != DialogResult.OK) return false;
			filename = dialog.SelectedPath;
			return true;
		}
	}
}
