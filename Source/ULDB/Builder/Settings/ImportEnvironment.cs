// ULDB: the parts of the user's environment the UDB settings import looks at (home folder
// and environment variables). Injectable so tests can point it at a fake home.

using System;
using System.IO;
using ULDB.Platform;

namespace ULDB.Settings
{
	public sealed class ImportEnvironment
	{
		#region ================== Properties

		/// <summary>The user's home folder.</summary>
		public string Home { get; }

		/// <summary>Reads an environment variable (null when unset).</summary>
		public Func<string, string> GetVariable { get; }

		/// <summary>ULDB's settings folder in this environment ($XDG_CONFIG_HOME/UltimateLinuxDoomBuilder), the import target.</summary>
		public string SettingsDirectory => Path.Combine(XdgDirectory("XDG_CONFIG_HOME", ".config"), Paths.APP_DIR);

		/// <summary>$XDG_DATA_HOME (default ~/.local/share). Mono's LocalApplicationData, where upstream's Mono build keeps "Doom Builder".</summary>
		public string DataHome => XdgDirectory("XDG_DATA_HOME", Path.Combine(".local", "share"));

		/// <summary>The real environment of this process.</summary>
		public static ImportEnvironment Current => new ImportEnvironment(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), Environment.GetEnvironmentVariable);

		#endregion

		#region ================== Constructor

		public ImportEnvironment(string home, Func<string, string> getvariable)
		{
			Home = home ?? "";
			GetVariable = getvariable ?? (name => null);
		}

		#endregion

		#region ================== Methods

		// Same rules as ULDB.Platform.Paths.XdgDirectory: unset or relative values fall back to ~/fallback
		private string XdgDirectory(string variable, string fallback)
		{
			string value = GetVariable(variable);
			if(!string.IsNullOrEmpty(value) && Path.IsPathRooted(value)) return value;
			return Path.Combine(Home, fallback);
		}

		#endregion
	}
}
