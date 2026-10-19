// ULDB: links upstream's Form objects (compat stand-ins, or ported windows) to the
// Avalonia windows that show them. Dialog owners, Form.ActiveForm and Cursor.Current
// are resolved through it.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Avalonia.Controls;

namespace ULDB.Shell
{
	public static class FormWindows
	{
		private static readonly ConditionalWeakTable<Form, Window> windows = new ConditionalWeakTable<Form, Window>();
		private static readonly ConditionalWeakTable<Window, Form> forms = new ConditionalWeakTable<Window, Form>();
		private static readonly List<Window> open = new List<Window>();

		/// <summary>Associates a form with the window that shows it.</summary>
		public static void Register(Form form, Window window)
		{
			windows.AddOrUpdate(form, window);
			forms.AddOrUpdate(window, form);
		}

		public static Window WindowFor(IWin32Window owner)
		{
			return owner is Form f && windows.TryGetValue(f, out Window w) ? w : null;
		}

		public static Form FormFor(Window window)
		{
			return window != null && forms.TryGetValue(window, out Form f) ? f : null;
		}

		/// <summary>Every open Avalonia window, oldest first.</summary>
		public static IReadOnlyList<Window> Open => open;

		/// <summary>The active window, or the most recently opened one.</summary>
		public static Window Active
		{
			get
			{
				foreach(Window w in open) if(w.IsActive) return w;
				return open.Count > 0 ? open[open.Count - 1] : null;
			}
		}

		internal static void TrackOpenWindows()
		{
			Window.WindowOpenedEvent.AddClassHandler<Window>((w, e) =>
			{
				if(!open.Contains(w)) open.Add(w);
				if(w.Icon == null) w.Icon = AppIcon.Window;
			});
			Window.WindowClosedEvent.AddClassHandler<Window>((w, e) => open.Remove(w));
		}
	}
}
