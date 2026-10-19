// Hand-written members of the RenderTargetControl placeholder (see docs/architecture.md).
// The Avalonia viewport (ULDB.Shell.ViewportControl) drives this control's size.

using System;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class RenderTargetControl
	{
		/// <summary>Raises Resize after the viewport changed ClientSize.</summary>
		internal void RaiseResize() => OnResize(EventArgs.Empty);
	}
}
