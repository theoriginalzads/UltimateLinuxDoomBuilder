// ULDB: "busy" notice for long synchronous work on the UI thread (opening a map). WinForms
// repainted the status bar and showed the wait cursor while upstream code ran; Avalonia only paints
// when the UI thread returns to its loop, so Begin shows a "Loading…" overlay on the main window and
// renders one frame before the work starts. It is removed again when the wait cursor is reset.

using System;

namespace ULDB.Platform
{
	public static class Busy
	{
		internal static Action<string> Show;
		internal static Action Hide;

		/// <summary>Shows the overlay and renders it once; call before blocking work.</summary>
		public static void Begin(string message) { Show?.Invoke(message); }

		/// <summary>Removes the overlay (also done when the cursor returns to default).</summary>
		public static void End() { Hide?.Invoke(); }
	}
}
