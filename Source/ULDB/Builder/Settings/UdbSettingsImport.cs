// ULDB: imports settings from an existing Ultimate Doom Builder install (Mono build, Flatpak,
// Wine, or a Windows install the user points at) into ULDB's own settings file.
//
// UDB keeps everything in one file, UDBuilder.cfg, in its Configuration text format:
// preferences, keyboard shortcuts ("shortcuts", stored as WinForms Keys values, which ULDB
// uses too), per-game-configuration settings ("configurations.<name>": resources, test
// engines, node builders...), recent files ("recentfiles"), script editor settings and
// plugin settings. The import reads that file with upstream's own Configuration class,
// converts the paths in it (SettingsPathConverter) and writes the result as ULDB's
// UDBuilder.cfg. The source is only ever read.
//
// Settings for game configurations ULDB hides (non-GZDoom/UZDoom, see ScopeFilter) are
// imported like any other; ULDB simply doesn't show those configurations.
//
// Startup: Program.Main calls RunAtStartup before upstream's General.Main loads settings.
// When ULDB has no settings yet and UDB settings are found, it offers to import them.
// "--import-udb-settings[=PATH]" imports on request (replacing ULDB's settings, with a backup).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CodeImp.DoomBuilder.IO;
using ULDB.Scope;

namespace ULDB.Settings
{
	public static class UdbSettingsImport
	{
		#region ================== Constants

		/// <summary>Command-line switch: "--import-udb-settings" (detect) or "--import-udb-settings=PATH".</summary>
		public const string IMPORT_ARGUMENT = "--import-udb-settings";

		/// <summary>The import summary, written to ULDB's settings folder.</summary>
		public const string SUMMARY_FILE = "UDBImport.txt";

		/// <summary>Suffix for the copy of ULDB's own settings made before an import replaces them.</summary>
		public const string BACKUP_SUFFIX = ".before-import";

		// Lines per list in the summary message box (the summary file has everything)
		private const int SUMMARY_MAX_LINES = 12;

		// Settings whose (Linux) values are paths, checked against the file system. Windows paths
		// are recognised in any setting, since "C:\..." can't be anything else.
		private static readonly Regex PATH_SETTING = new Regex(@"(^recentfiles\.file\d+$)|((^|\.)(location|testprogram)$)|((^|\.)[a-z0-9]*(path|folder)$)", RegexOptions.IgnoreCase);

		#endregion

		#region ================== Import

		/// <summary>Imports 'source' into the UDBuilder.cfg in 'targetdir'. An existing file there is backed up first.</summary>
		public static UdbSettingsImportResult Import(UdbSettingsSource source, string targetdir)
		{
			UdbSettingsImportResult result = new UdbSettingsImportResult();
			result.SourceFile = source.SettingsFile;
			result.TargetFile = Path.Combine(targetdir, UdbSettingsSource.SETTINGS_FILE);

			try
			{
				// Already ULDB's own file? (Only when the user points the import at ULDB's settings.)
				if(UdbSettingsSource.FullPath(result.SourceFile) == UdbSettingsSource.FullPath(result.TargetFile))
				{
					result.SameFile = true;
					result.Success = true;
					return result;
				}

				// Read the source with upstream's parser (opens the file read-only)
				Configuration cfg = new Configuration(true);
				if(!File.Exists(source.SettingsFile)) return Fail(result, "the file doesn't exist");
				cfg.LoadConfiguration(source.SettingsFile, true);
				if(cfg.ErrorResult) return Fail(result, "error near line " + cfg.ErrorLine + ": " + cfg.ErrorDescription);

				// Convert paths
				SettingsPathConverter converter = new SettingsPathConverter(source.WinePrefix, source.DriveRoots);
				ConvertPaths(cfg.Root, "", converter, result);

				// Count what's there, for the summary
				CountSettings(cfg, result);

				// Upstream replaces a settings file without a version number with the defaults
				if(!cfg.SettingExists("currentversion"))
				{
					cfg.WriteSetting("currentversion", 0);
					result.Notes.Add("The source had no version number (very old settings file); some settings may not apply.");
				}

				// Write ULDB's settings (backing up any existing file), via a temporary file
				Directory.CreateDirectory(targetdir);
				if(File.Exists(result.TargetFile))
				{
					result.BackupFile = result.TargetFile + BACKUP_SUFFIX;
					File.Copy(result.TargetFile, result.BackupFile, true);
				}
				string tempfile = result.TargetFile + ".import-tmp";
				if(!cfg.SaveConfiguration(tempfile)) return Fail(result, "could not write " + tempfile);
				File.Move(tempfile, result.TargetFile, true);

				result.Success = true;
			}
			catch(Exception e)
			{
				return Fail(result, e.Message);
			}

			// The full summary, for fixing unresolved paths later
			try { File.WriteAllText(Path.Combine(targetdir, SUMMARY_FILE), result.ToSummaryText()); }
			catch(Exception e) when(e is IOException || e is UnauthorizedAccessException) { }

			return result;
		}

		// Converts every path setting in the structure, recording each one in the result
		private static void ConvertPaths(IDictionary dict, string prefix, SettingsPathConverter converter, UdbSettingsImportResult result)
		{
			List<KeyValuePair<object, string>> changes = new List<KeyValuePair<object, string>>();

			foreach(DictionaryEntry de in dict)
			{
				string key = prefix + de.Key;
				if(de.Value is IDictionary sub)
				{
					ConvertPaths(sub, key + ".", converter, result);
					continue;
				}

				string value = de.Value as string;
				if(string.IsNullOrEmpty(value)) continue;

				if(SettingsPathConverter.IsWindowsPath(value) || (PATH_SETTING.IsMatch(key) && value.StartsWith("/", StringComparison.Ordinal)))
				{
					PathConversion pc = converter.Convert(value);
					if(pc.Status == PathConversionStatus.NotAPath) continue;

					ImportedPath p = new ImportedPath { Setting = key, Original = value, Status = pc.Status, Reason = pc.Reason };
					p.Value = (pc.Status == PathConversionStatus.Unresolved ? value : pc.Path);
					result.Paths.Add(p);
					if(p.Value != value) changes.Add(new KeyValuePair<object, string>(de.Key, p.Value));

					// A Windows engine found in a Wine prefix: the path is right, but it's a Windows program
					if(key.EndsWith("testprogram", StringComparison.OrdinalIgnoreCase) && p.Value.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
						result.Notes.Add(key + ": " + p.Value + " is a Windows program. Use a Linux GZDoom/UZDoom, or a Wine wrapper script.");
				}
				else if(SettingsPathConverter.ContainsWindowsPath(value))
				{
					result.Notes.Add(key + " contains a Windows path, left unchanged: " + value);
				}
			}

			foreach(KeyValuePair<object, string> change in changes) dict[change.Key] = change.Value;
		}

		private static void CountSettings(Configuration cfg, UdbSettingsImportResult result)
		{
			result.ShortcutCount = cfg.ReadSetting("shortcuts", new ListDictionary()).Count;

			foreach(DictionaryEntry de in cfg.ReadSetting("recentfiles", new ListDictionary()))
				if(de.Value is string s && s.Length > 0) result.RecentFileCount++;

			foreach(DictionaryEntry de in cfg.ReadSetting("configurations", new ListDictionary()))
			{
				if(!(de.Value is IDictionary)) continue;
				string name = de.Key.ToString();
				result.Configurations.Add(name);
				if(!ScopeFilter.IsConfigurationVisible(name + ".cfg")) result.HiddenConfigurations.Add(name);
			}
		}

		private static UdbSettingsImportResult Fail(UdbSettingsImportResult result, string error)
		{
			result.Success = false;
			result.Error = error;
			return result;
		}

		#endregion

		#region ================== Startup

		/// <summary>True when the folder already has settings ULDB would load (UDBuilder.cfg, or upstream's legacy GZBuilder.cfg).</summary>
		public static bool SettingsExist(string settingsdir)
		{
			return File.Exists(Path.Combine(settingsdir, UdbSettingsSource.SETTINGS_FILE)) || File.Exists(Path.Combine(settingsdir, UdbSettingsSource.LEGACY_SETTINGS_FILE));
		}

		/// <summary>
		/// First run: when ULDB has no settings yet (and isn't started with -portable or -nosettings) and UDB settings
		/// are found, asks whether to import the best one. Returns the import result, or null when nothing was imported.
		/// 'ask' and 'report' default to message boxes.
		/// </summary>
		public static UdbSettingsImportResult OfferImportOnFirstRun(string[] args, ImportEnvironment env, Func<UdbSettingsSource, bool> ask = null, Action<UdbSettingsImportResult> report = null)
		{
			if(HasArgument(args, "-PORTABLE") || HasArgument(args, "-NOSETTINGS")) return null;

			string settingsdir = env.SettingsDirectory;
			if(SettingsExist(settingsdir)) return null;

			UdbSettingsSource source = UdbSettingsSource.Detect(env).FirstOrDefault();
			if(source == null) return null;
			if(!(ask ?? AskToImport)(source)) return null;

			UdbSettingsImportResult result = Import(source, settingsdir);
			(report ?? ShowSummary)(result);
			return result;
		}

		/// <summary>
		/// Called by Program.Main before upstream's startup. Handles "--import-udb-settings[=PATH]", or else offers the
		/// first-run import. Returns the arguments with ULDB's own switch removed, for General.Main.
		/// Never stops startup: problems are reported and startup carries on with whatever settings there are.
		/// </summary>
		public static string[] RunAtStartup(string[] args, ImportEnvironment env = null)
		{
			env = env ?? ImportEnvironment.Current;
			List<string> remaining = new List<string>();
			bool requested = false;
			string requestedpath = null;
			foreach(string arg in args)
			{
				if(string.Equals(arg, IMPORT_ARGUMENT, StringComparison.Ordinal))
				{
					requested = true;
				}
				else if(arg.StartsWith(IMPORT_ARGUMENT + "=", StringComparison.Ordinal))
				{
					requested = true;
					requestedpath = arg.Substring(IMPORT_ARGUMENT.Length + 1);
				}
				else
				{
					remaining.Add(arg);
				}
			}

			try
			{
				if(requested)
				{
					UdbSettingsSource source = (requestedpath != null ? UdbSettingsSource.FromPath(requestedpath) : UdbSettingsSource.Detect(env).FirstOrDefault());
					if(source == null)
						MessageBox.Show("No " + Branding.UPSTREAM_NAME + " settings were found" + (requestedpath != null ? " at \"" + requestedpath + "\"" : "") + ".", Branding.NAME, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					else
						ShowSummary(Import(source, env.SettingsDirectory));
				}
				else
				{
					OfferImportOnFirstRun(remaining.ToArray(), env);
				}
			}
			catch(Exception e)
			{
				Console.Error.WriteLine("UDB settings import failed: " + e);
			}

			return remaining.ToArray();
		}

		private static bool AskToImport(UdbSettingsSource source)
		{
			string text = "Settings from " + source.Description + " were found:\n" + source.SettingsFile + "\n\n" +
				"Import them into " + Branding.NAME + "? This brings over your preferences, keyboard shortcuts, " +
				"game configuration settings (resources, test engines, node builders) and recent files. " +
				"The original file is not changed.";
			return MessageBox.Show(text, Branding.NAME, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
		}

		private static void ShowSummary(UdbSettingsImportResult result)
		{
			bool problems = !result.Success || result.UnresolvedPaths.Any();
			MessageBox.Show(result.ToSummaryText(SUMMARY_MAX_LINES), Branding.NAME, MessageBoxButtons.OK, problems ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
		}

		private static bool HasArgument(string[] args, string name)
		{
			return args != null && args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
		}

		#endregion
	}
}
