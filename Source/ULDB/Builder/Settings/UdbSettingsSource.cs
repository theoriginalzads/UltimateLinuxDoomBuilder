// ULDB: an existing Ultimate Doom Builder settings file ULDB can import from, and the
// detection of the usual places one lives on a Linux machine (Mono build, Flatpak, Wine
// prefixes) or below a folder the user picks (e.g. a mounted Windows drive).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ULDB.Settings
{
	public enum UdbSettingsSourceKind
	{
		/// <summary>Upstream's Mono build on Linux (~/.local/share/Doom Builder).</summary>
		MonoBuild,

		/// <summary>The official UDB Flatpak (~/.var/app/io.github.ultimatedoombuilder.ultimatedoombuilder/data/Doom Builder).</summary>
		Flatpak,

		/// <summary>The Windows build running in a Wine prefix.</summary>
		Wine,

		/// <summary>A file or folder the user picked, e.g. on a mounted Windows drive.</summary>
		UserPicked,
	}

	public sealed class UdbSettingsSource
	{
		#region ================== Constants

		/// <summary>Upstream's settings file.</summary>
		public const string SETTINGS_FILE = "UDBuilder.cfg";

		/// <summary>GZDoom Builder's settings file, which upstream still reads when UDBuilder.cfg is missing.</summary>
		public const string LEGACY_SETTINGS_FILE = "GZBuilder.cfg";

		/// <summary>Upstream's settings folder name (under LocalApplicationData).</summary>
		public const string SETTINGS_DIR = "Doom Builder";

		public const string FLATPAK_APP_ID = "io.github.ultimatedoombuilder.ultimatedoombuilder";

		#endregion

		#region ================== Properties

		public UdbSettingsSourceKind Kind { get; }

		/// <summary>The settings file (UDBuilder.cfg or GZBuilder.cfg).</summary>
		public string SettingsFile { get; }

		/// <summary>True for a GZDoom Builder GZBuilder.cfg.</summary>
		public bool IsLegacy => string.Equals(Path.GetFileName(SettingsFile), LEGACY_SETTINGS_FILE, StringComparison.OrdinalIgnoreCase);

		/// <summary>The Wine prefix the file is in, or null.</summary>
		public string WinePrefix { get; }

		/// <summary>
		/// Where each Windows drive letter is on this machine, for converting Windows paths in the settings
		/// (e.g. 'c' -> "/mnt/windows"). Filled in by detection; callers may add drives the user names.
		/// Wine prefixes are resolved through their dosdevices folder instead, so this only needs extra drives there.
		/// </summary>
		public IDictionary<char, string> DriveRoots { get; } = new Dictionary<char, string>();

		public DateTime LastModifiedUtc => File.Exists(SettingsFile) ? File.GetLastWriteTimeUtc(SettingsFile) : DateTime.MinValue;

		/// <summary>A short, user-facing description, e.g. "Ultimate Doom Builder (Flatpak)".</summary>
		public string Description
		{
			get
			{
				string name = IsLegacy ? "GZDoom Builder" : Branding.UPSTREAM_NAME;
				switch(Kind)
				{
					case UdbSettingsSourceKind.MonoBuild: return name + " (Mono build)";
					case UdbSettingsSourceKind.Flatpak: return name + " (Flatpak)";
					case UdbSettingsSourceKind.Wine: return name + " (Wine prefix " + WinePrefix + ")";
					default: return name;
				}
			}
		}

		#endregion

		#region ================== Constructor

		public UdbSettingsSource(UdbSettingsSourceKind kind, string settingsfile, string wineprefix = null)
		{
			Kind = kind;
			SettingsFile = settingsfile;
			WinePrefix = wineprefix;
		}

		#endregion

		#region ================== Detection

		/// <summary>
		/// Finds UDB settings in the usual places, best candidate first: UDBuilder.cfg before GZBuilder.cfg,
		/// then the most recently changed. Folders that are ULDB's own settings folder are skipped.
		/// </summary>
		public static List<UdbSettingsSource> Detect(ImportEnvironment env)
		{
			List<UdbSettingsSource> found = new List<UdbSettingsSource>();
			HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
			string ownsettings = FullPath(env.SettingsDirectory);

			void AddFolder(UdbSettingsSourceKind kind, string folder, string prefix)
			{
				if(string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
				if(FullPath(folder) == ownsettings) return;
				foreach(string name in new[] { SETTINGS_FILE, LEGACY_SETTINGS_FILE })
				{
					string file = FindEntry(folder, name, false);
					if(file == null || !seen.Add(FullPath(file))) continue;
					found.Add(new UdbSettingsSource(kind, file, prefix));
				}
			}

			// Upstream's Mono build: LocalApplicationData is $XDG_DATA_HOME (default ~/.local/share)
			AddFolder(UdbSettingsSourceKind.MonoBuild, Path.Combine(env.DataHome, SETTINGS_DIR), null);
			AddFolder(UdbSettingsSourceKind.MonoBuild, Path.Combine(env.Home, ".local", "share", SETTINGS_DIR), null);

			// Official Flatpak (the same Mono build, with XDG_DATA_HOME inside ~/.var/app)
			AddFolder(UdbSettingsSourceKind.Flatpak, Path.Combine(env.Home, ".var", "app", FLATPAK_APP_ID, "data", SETTINGS_DIR), null);

			// Wine prefixes: $WINEPREFIX and the default ~/.wine
			List<string> prefixes = new List<string>();
			string wineprefix = env.GetVariable("WINEPREFIX");
			if(!string.IsNullOrEmpty(wineprefix) && Path.IsPathRooted(wineprefix)) prefixes.Add(wineprefix);
			prefixes.Add(Path.Combine(env.Home, ".wine"));
			foreach(string prefix in prefixes.Select(FullPath).Distinct())
			{
				foreach(string folder in WineSettingsFolders(prefix))
					AddFolder(UdbSettingsSourceKind.Wine, folder, prefix);
			}

			return found.OrderBy(s => s.IsLegacy).ThenByDescending(s => s.LastModifiedUtc).ToList();
		}

		/// <summary>
		/// The settings in a file or folder the user picked: UDBuilder.cfg/GZBuilder.cfg itself, the "Doom Builder"
		/// folder, the folder above it, or a Windows user profile folder (AppData/Local/Doom Builder below it).
		/// For a file on a Windows drive (a "Users" folder above it), drive C: is mapped to that drive's root.
		/// Returns null when no settings file is found.
		/// </summary>
		public static UdbSettingsSource FromPath(string path)
		{
			if(string.IsNullOrEmpty(path)) return null;
			path = FullPath(path);

			string file = null;
			if(File.Exists(path))
			{
				file = path;
			}
			else if(Directory.Exists(path))
			{
				foreach(string folder in new[] { path, CombineCaseInsensitive(path, SETTINGS_DIR), CombineCaseInsensitive(path, "AppData", "Local", SETTINGS_DIR), CombineCaseInsensitive(path, "Local Settings", "Application Data", SETTINGS_DIR) })
				{
					if(folder == null) continue;
					file = FindEntry(folder, SETTINGS_FILE, false) ?? FindEntry(folder, LEGACY_SETTINGS_FILE, false);
					if(file != null) break;
				}
			}
			if(file == null) return null;

			// Inside a Wine prefix?
			string prefix = FindAncestor(file, "drive_c");
			if(prefix != null)
				return new UdbSettingsSource(UdbSettingsSourceKind.Wine, file, Path.GetDirectoryName(prefix));

			UdbSettingsSource source = new UdbSettingsSource(UdbSettingsSourceKind.UserPicked, file);

			// On a Windows drive, settings live in <drive>\Users\<name>\AppData\Local\Doom Builder
			// (XP: <drive>\Documents and Settings\...), and that drive is the system drive, C:
			string users = FindAncestor(file, "Users") ?? FindAncestor(file, "Documents and Settings");
			if(users != null) source.DriveRoots['c'] = Path.GetDirectoryName(users);

			return source;
		}

		// Settings folders for every user in a Wine prefix (Vista+ and XP-style layouts)
		private static IEnumerable<string> WineSettingsFolders(string prefix)
		{
			string users = Path.Combine(prefix, "drive_c", "users");
			if(!Directory.Exists(users)) yield break;

			string[] userdirs;
			try { userdirs = Directory.GetDirectories(users); }
			catch(Exception e) when(e is IOException || e is UnauthorizedAccessException) { yield break; }

			foreach(string user in userdirs.OrderBy(d => d, StringComparer.Ordinal))
			{
				yield return CombineCaseInsensitive(user, "AppData", "Local", SETTINGS_DIR);
				yield return CombineCaseInsensitive(user, "Local Settings", "Application Data", SETTINGS_DIR);
			}
		}

		#endregion

		#region ================== Helpers

		internal static string FullPath(string path)
		{
			string full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
			return (full.Length > 0 ? full : Path.DirectorySeparatorChar.ToString());
		}

		// An entry (file or folder) in a folder, matched by exact name first, then ignoring case
		internal static string FindEntry(string folder, string name, bool directory)
		{
			string exact = Path.Combine(folder, name);
			if(directory ? Directory.Exists(exact) : File.Exists(exact)) return exact;
			if(!Directory.Exists(folder)) return null;

			try
			{
				IEnumerable<string> entries = directory ? Directory.EnumerateDirectories(folder) : Directory.EnumerateFiles(folder);
				return entries.Where(e => string.Equals(Path.GetFileName(e), name, StringComparison.OrdinalIgnoreCase)).OrderBy(e => e, StringComparer.Ordinal).FirstOrDefault();
			}
			catch(Exception e) when(e is IOException || e is UnauthorizedAccessException)
			{
				return null;
			}
		}

		// Folder path below 'folder', each part matched ignoring case. Null when a part is missing.
		private static string CombineCaseInsensitive(string folder, params string[] parts)
		{
			string current = folder;
			foreach(string part in parts)
			{
				current = FindEntry(current, part, true);
				if(current == null) return null;
			}
			return current;
		}

		// The closest ancestor folder of 'path' named 'name' (ignoring case), or null
		private static string FindAncestor(string path, string name)
		{
			for(string dir = Path.GetDirectoryName(path); !string.IsNullOrEmpty(dir); dir = Path.GetDirectoryName(dir))
			{
				if(string.Equals(Path.GetFileName(dir), name, StringComparison.OrdinalIgnoreCase)) return dir;
			}
			return null;
		}

		#endregion
	}
}
