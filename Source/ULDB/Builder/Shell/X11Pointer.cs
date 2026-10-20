// ULDB: reads and moves the mouse pointer on X11 (also XWayland). Upstream's visual
// (3D) mode reads Cursor.Position every frame and warps the pointer back to the middle
// of the view; Avalonia has no API for warping, so this talks to Xlib directly, on a
// connection of its own.

using System;
using System.Runtime.InteropServices;

namespace ULDB.Shell
{
	internal static class X11Pointer
	{
		private static IntPtr display;
		private static IntPtr root;
		private static bool unavailable;

		private static bool Open()
		{
			if(display != IntPtr.Zero) return true;
			if(unavailable) return false;
			try
			{
				display = XOpenDisplay(IntPtr.Zero);
				if(display != IntPtr.Zero) root = XDefaultRootWindow(display);
			}
			catch(DllNotFoundException) { }
			unavailable = display == IntPtr.Zero;
			return !unavailable;
		}

		/// <summary>The pointer position in screen pixels, or null when X11 isn't available.</summary>
		public static (int x, int y)? GetPosition()
		{
			if(!Open()) return null;
			if(!XQueryPointer(display, root, out _, out _, out int rx, out int ry, out _, out _, out _)) return null;
			return (rx, ry);
		}

		/// <summary>Moves the pointer to a screen position (pixels).</summary>
		public static void SetPosition(int x, int y)
		{
			if(!Open()) return;
			XWarpPointer(display, IntPtr.Zero, root, 0, 0, 0, 0, x, y);
			XFlush(display);
		}

		private const string LIBX11 = "libX11.so.6";

		[DllImport(LIBX11)] private static extern IntPtr XOpenDisplay(IntPtr name);
		[DllImport(LIBX11)] private static extern IntPtr XDefaultRootWindow(IntPtr display);
		[DllImport(LIBX11)] private static extern int XFlush(IntPtr display);

		[DllImport(LIBX11)]
		private static extern int XWarpPointer(IntPtr display, IntPtr srcwindow, IntPtr destwindow,
			int srcx, int srcy, uint srcwidth, uint srcheight, int destx, int desty);

		[DllImport(LIBX11)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr rootreturn, out IntPtr childreturn,
			out int rootx, out int rooty, out int winx, out int winy, out uint mask);
	}
}
