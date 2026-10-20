// ULDB: what a UDB settings import did, for the summary shown to the user
// (and written next to ULDB's settings as UDBImport.txt).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ULDB.Settings
{
	/// <summary>One path setting the import looked at.</summary>
	public sealed class ImportedPath
	{
		/// <summary>The setting's key, e.g. "configurations.uzdoom_doomudmf.resources.resource0.location".</summary>
		public string Setting { get; internal set; }

		/// <summary>The value in the source settings.</summary>
		public string Original { get; internal set; }

		/// <summary>The value written to ULDB's settings (the original when unresolved).</summary>
		public string Value { get; internal set; }

		public PathConversionStatus Status { get; internal set; }

		/// <summary>Why an unresolved path couldn't be converted.</summary>
		public string Reason { get; internal set; }

		public override string ToString() => Setting + " = " + Original + (Status == PathConversionStatus.Converted ? " -> " + Value : "") + (Reason != null ? " (" + Reason + ")" : "");
	}

	public sealed class UdbSettingsImportResult
	{
		#region ================== Properties

		public bool Success { get; internal set; }

		/// <summary>True when the source already is ULDB's settings file, so there was nothing to do.</summary>
		public bool SameFile { get; internal set; }

		/// <summary>Why the import failed.</summary>
		public string Error { get; internal set; }

		public string SourceFile { get; internal set; }
		public string TargetFile { get; internal set; }

		/// <summary>The copy of ULDB's previous settings file, when there was one; otherwise null.</summary>
		public string BackupFile { get; internal set; }

		public int ShortcutCount { get; internal set; }
		public int RecentFileCount { get; internal set; }

		/// <summary>Settings keys of the game configurations imported (e.g. "uzdoom_doomudmf").</summary>
		public List<string> Configurations { get; } = new List<string>();

		/// <summary>Imported configurations ULDB doesn't show (non-GZDoom/UZDoom). Their settings are kept.</summary>
		public List<string> HiddenConfigurations { get; } = new List<string>();

		/// <summary>Every path setting looked at.</summary>
		public List<ImportedPath> Paths { get; } = new List<ImportedPath>();

		public IEnumerable<ImportedPath> ConvertedPaths => Paths.Where(p => p.Status == PathConversionStatus.Converted);

		/// <summary>Paths that couldn't be found on this machine. They're kept as they were, for the user to fix.</summary>
		public IEnumerable<ImportedPath> UnresolvedPaths => Paths.Where(p => p.Status == PathConversionStatus.Unresolved);

		/// <summary>Other things worth checking (Windows programs as test engines, Windows paths inside parameters...).</summary>
		public List<string> Notes { get; } = new List<string>();

		#endregion

		#region ================== Methods

		/// <summary>The summary as plain text. 'maxlines' limits each list (0 = no limit).</summary>
		public string ToSummaryText(int maxlines = 0)
		{
			StringBuilder sb = new StringBuilder();
			if(!Success)
			{
				sb.AppendLine("Importing settings from " + SourceFile + " failed: " + Error);
				return sb.ToString();
			}
			if(SameFile)
			{
				sb.AppendLine(SourceFile + " already is " + Branding.SHORT_NAME + "'s settings file. Nothing to import.");
				return sb.ToString();
			}

			sb.AppendLine("Imported settings from " + SourceFile);
			sb.AppendLine("into " + TargetFile);
			if(BackupFile != null) sb.AppendLine("(previous settings saved as " + BackupFile + ")");
			sb.AppendLine();
			sb.AppendLine(ShortcutCount + " keyboard shortcuts, " + Configurations.Count + " game configurations, " + RecentFileCount + " recent files.");
			if(HiddenConfigurations.Count > 0)
				sb.AppendLine(HiddenConfigurations.Count + " of the game configurations are not shown in " + Branding.SHORT_NAME + " (not GZDoom/UZDoom); their settings were kept.");

			List<ImportedPath> converted = ConvertedPaths.ToList();
			List<ImportedPath> unresolved = UnresolvedPaths.ToList();
			if(converted.Count > 0) sb.AppendLine(converted.Count + " paths were converted to Linux paths.");

			if(unresolved.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine(unresolved.Count + " paths could not be found on this computer. They were kept as they were; please fix them in Game Configurations or Preferences:");
				AppendList(sb, unresolved.Select(p => p.Setting + ": " + p.Original + (p.Reason != null ? " (" + p.Reason + ")" : "")), maxlines);
			}

			if(Notes.Count > 0)
			{
				sb.AppendLine();
				sb.AppendLine("Also check:");
				AppendList(sb, Notes, maxlines);
			}

			return sb.ToString();
		}

		private static void AppendList(StringBuilder sb, IEnumerable<string> lines, int maxlines)
		{
			List<string> list = lines.ToList();
			int shown = (maxlines > 0 ? Math.Min(maxlines, list.Count) : list.Count);
			for(int i = 0; i < shown; i++) sb.AppendLine("  " + list[i]);
			if(shown < list.Count) sb.AppendLine("  ...and " + (list.Count - shown) + " more (see " + UdbSettingsImport.SUMMARY_FILE + ")");
		}

		#endregion
	}
}
