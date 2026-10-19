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

		// The 2D/3D view. Opening a map creates the renderer on it; with a hosted OpenGL
		// context (ULDB.Rendering.HostedGL) its window handle isn't used.
		public CodeImp.DoomBuilder.Controls.RenderTargetControl Display { get; } = new CodeImp.DoomBuilder.Controls.RenderTargetControl();

		// Upstream runs the action now on the UI thread, otherwise queues it for the UI
		// thread. Image loading depends on this to publish loaded images.
		public void RunOnUIThread(Action action)
		{
			if(WinFormsHost.Current.CheckAccess()) action();
			else WinFormsHost.Current.BeginInvoke(action);
		}
	}
}
