// Hand-written additions to the generated MainForm placeholder: members upstream's
// MainForm inherits from WinForms' Form, which IMainForm requires. Remove this file
// when MainForm is ported to Avalonia.

using System;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class MainForm
	{
		public bool Focus() => false;
	}
}
