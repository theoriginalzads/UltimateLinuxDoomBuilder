// Hand-written members of the MainForm placeholder (see uistubs-manual.txt and
// docs/architecture.md). Remove this file when MainForm is ported to Avalonia.

using System;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class MainForm
	{
		// Inherited from WinForms' Form upstream; IMainForm requires it.
		public bool Focus() => false;

		// Upstream runs the action now on the UI thread, otherwise queues it for the UI
		// thread. Image loading depends on this to publish loaded images.
		public void RunOnUIThread(Action action)
		{
			if(WinFormsHost.Current.CheckAccess()) action();
			else WinFormsHost.Current.BeginInvoke(action);
		}
	}
}
