// ULDB: the app icon (Cacky, the ULDB Cacodemon), from Icons/ next to the program.

using System;
using System.IO;
using Avalonia.Controls;

namespace ULDB.Shell
{
	internal static class AppIcon
	{
		private static WindowIcon window;
		private static bool loaded;

		/// <summary>The title bar and taskbar icon, or null if the file is missing.</summary>
		public static WindowIcon Window
		{
			get
			{
				if(!loaded)
				{
					loaded = true;
					string path = Path.Combine(AppContext.BaseDirectory, "Icons", "uldb-256.png");
					if(File.Exists(path)) window = new WindowIcon(path);
				}
				return window;
			}
		}
	}
}
