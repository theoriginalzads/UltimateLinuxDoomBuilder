// ULDB: opens a file or folder with the desktop's default application (xdg-open). Upstream calls
// Process.Start(path), which on .NET Framework went through the shell; on .NET it would try to
// execute the file ("Permission denied" for an .html file) or fail for a folder.

using System.Diagnostics;

namespace ULDB.Platform
{
	public static class Desktop
	{
		/// <summary>Opens a file, folder or URL with the default application.</summary>
		public static void Open(string pathorurl)
		{
			Process.Start(new ProcessStartInfo(pathorurl) { UseShellExecute = true });
		}
	}
}
