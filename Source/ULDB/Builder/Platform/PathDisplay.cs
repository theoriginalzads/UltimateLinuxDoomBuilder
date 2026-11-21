// ULDB: file paths for menus (the recent files list). Upstream chops the middle out of a long path by measuring
// pixels, which doesn't match what the menu draws, so the end of the name (".wad") could be cut off. This keeps
// the file name and as many of the folders next to it as fit in a number of characters.

using System;
using System.IO;

namespace ULDB.Platform
{
	public static class PathDisplay
	{
		public static string Short(string path, int maxlength, string home = null)
		{
			if(string.IsNullOrEmpty(path)) return "";
			try { path = Path.GetFullPath(path); } catch(Exception) { } // "tests/../../x" becomes a real path
			home = (home ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))?.TrimEnd('/');
			string prefix = "";
			if(!string.IsNullOrEmpty(home) && path.StartsWith(home + "/", StringComparison.Ordinal)) { path = path.Substring(home.Length); prefix = "~"; }
			if(prefix.Length + path.Length <= maxlength) return prefix + path;

			string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
			string result = parts[parts.Length - 1];
			for(int i = parts.Length - 2; i >= 0; i--)
			{
				string next = parts[i] + "/" + result;
				if(("…/" + next).Length > maxlength) break;
				result = next;
			}
			return result == parts[parts.Length - 1] && result.Length > maxlength ? "…" + result.Substring(result.Length - maxlength + 1) : "…/" + result;
		}
	}
}
