// ULDB: Linux file-system helpers used by #if ULDB blocks in upstream code.

using System;
using System.IO;

namespace ULDB.Platform
{
	public static class Paths
	{
		/// <summary>Folder name under the XDG base directories.</summary>
		public const string APP_DIR = "UltimateLinuxDoomBuilder";

		/// <summary>$XDG_CONFIG_HOME/UltimateLinuxDoomBuilder (default ~/.config/UltimateLinuxDoomBuilder): settings, log, map restore data.</summary>
		public static string SettingsDirectory => Path.Combine(XdgDirectory("XDG_CONFIG_HOME", ".config"), APP_DIR);

		/// <summary>Returns an XDG base directory, falling back to ~/fallback when the variable is unset or not absolute (per the XDG spec).</summary>
		public static string XdgDirectory(string variable, string fallback)
		{
			string value = Environment.GetEnvironmentVariable(variable);
			if(!string.IsNullOrEmpty(value) && Path.IsPathRooted(value)) return value;
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), fallback);
		}

		/// <summary>True when files can be created in the directory (replaces upstream's Windows ACL check).</summary>
		public static bool IsDirectoryWritable(string path)
		{
			try
			{
				string probe = Path.Combine(path, ".uldb-write-test-" + Guid.NewGuid().ToString("N"));
				using(File.Create(probe, 1, FileOptions.DeleteOnClose)) { }
				return true;
			}
			catch(Exception e) when(e is IOException || e is UnauthorizedAccessException)
			{
				return false;
			}
		}
	}
}
