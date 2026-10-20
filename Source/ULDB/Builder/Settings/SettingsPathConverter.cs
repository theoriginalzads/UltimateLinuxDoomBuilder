// ULDB: converts paths stored in imported UDB settings to Linux paths.
//
// - Windows paths (C:\Games\doom2.wad): the drive letter is looked up in the Wine prefix's
//   dosdevices folder (c: -> drive_c, z: -> /), or in the drive roots the source knows about
//   (a mounted Windows drive). Backslashes become slashes.
// - Every path is then matched against the real file system, ignoring case where the exact
//   case doesn't exist (Windows paths are case-insensitive, Linux file systems aren't).
// - Anything that can't be found is reported as unresolved; the caller keeps the original.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ULDB.Settings
{
	public enum PathConversionStatus
	{
		/// <summary>Not a path (or not one the converter handles); leave it as it is.</summary>
		NotAPath,

		/// <summary>A Linux path that exists exactly as written.</summary>
		Unchanged,

		/// <summary>Converted (Windows to Linux, or case corrected) to a path that exists.</summary>
		Converted,

		/// <summary>No existing Linux path found.</summary>
		Unresolved,
	}

	public struct PathConversion
	{
		public PathConversionStatus Status;

		/// <summary>The Linux path (Converted/Unchanged), or the best guess (Unresolved, may be null).</summary>
		public string Path;

		/// <summary>Why the path couldn't be resolved.</summary>
		public string Reason;
	}

	public sealed class SettingsPathConverter
	{
		#region ================== Constants

		// "C:\..." or "C:/..." (also a bare "C:\")
		private static readonly Regex WINDOWS_DRIVE_PATH = new Regex(@"^(?<drive>[A-Za-z]):[\\/](?<rest>.*)$", RegexOptions.Singleline);

		// A Windows path somewhere inside a longer string (e.g. engine parameters)
		private static readonly Regex EMBEDDED_WINDOWS_PATH = new Regex(@"(?<![A-Za-z0-9])[A-Za-z]:\\");

		#endregion

		#region ================== Variables

		private readonly string wineprefix;
		private readonly Dictionary<char, string> driveroots = new Dictionary<char, string>();

		#endregion

		#region ================== Constructor

		/// <param name="wineprefix">Wine prefix the settings come from, or null.</param>
		/// <param name="driveroots">Known Linux locations of Windows drives (lower- or upper-case letters).</param>
		public SettingsPathConverter(string wineprefix, IDictionary<char, string> driveroots)
		{
			this.wineprefix = wineprefix;
			if(driveroots != null)
				foreach(KeyValuePair<char, string> pair in driveroots) this.driveroots[char.ToLowerInvariant(pair.Key)] = pair.Value;
		}

		#endregion

		#region ================== Methods

		/// <summary>True when the whole value is a Windows path (drive letter or UNC).</summary>
		public static bool IsWindowsPath(string value)
		{
			return !string.IsNullOrEmpty(value) && (WINDOWS_DRIVE_PATH.IsMatch(value) || value.StartsWith(@"\\", StringComparison.Ordinal));
		}

		/// <summary>True when a longer value (not itself a path) contains a Windows path.</summary>
		public static bool ContainsWindowsPath(string value)
		{
			return !string.IsNullOrEmpty(value) && !IsWindowsPath(value) && EMBEDDED_WINDOWS_PATH.IsMatch(value);
		}

		/// <summary>Converts a stored path. Linux paths are only checked (and case-corrected); anything else is NotAPath.</summary>
		public PathConversion Convert(string value)
		{
			if(string.IsNullOrEmpty(value)) return new PathConversion { Status = PathConversionStatus.NotAPath };

			// Windows path
			Match m = WINDOWS_DRIVE_PATH.Match(value);
			if(m.Success)
			{
				char drive = char.ToLowerInvariant(m.Groups["drive"].Value[0]);
				string root = DriveRoot(drive);
				if(root == null)
					return Unresolved(null, "no Linux location is known for drive " + char.ToUpperInvariant(drive) + ":");

				string[] parts = m.Groups["rest"].Value.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
				string guess = parts.Length > 0 ? Path.Combine(root, Path.Combine(parts)) : root;
				string found = ResolveIgnoringCase(root, parts);
				if(found == null) return Unresolved(guess, "not found (looked for " + guess + ")");
				return new PathConversion { Status = PathConversionStatus.Converted, Path = found };
			}

			// Network path
			if(value.StartsWith(@"\\", StringComparison.Ordinal))
				return Unresolved(null, "network (UNC) paths can't be converted");

			// Linux path: check it exists, correct the case if needed
			if(value.StartsWith("/", StringComparison.Ordinal))
			{
				if(Exists(value)) return new PathConversion { Status = PathConversionStatus.Unchanged, Path = value };
				string found = ResolveIgnoringCase("/", value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries));
				if(found == null) return Unresolved(value, "not found");
				return new PathConversion { Status = PathConversionStatus.Converted, Path = found };
			}

			return new PathConversion { Status = PathConversionStatus.NotAPath };
		}

		/// <summary>
		/// Where a Windows drive is on this machine: an explicitly known root first, then the Wine prefix's
		/// dosdevices link (c: -> ../drive_c, z: -> /), then Wine's defaults for C: and Z:.
		/// </summary>
		public string DriveRoot(char drive)
		{
			drive = char.ToLowerInvariant(drive);
			if(driveroots.TryGetValue(drive, out string root)) return root;
			if(wineprefix == null) return null;

			string link = Path.Combine(wineprefix, "dosdevices", drive + ":");
			try
			{
				FileSystemInfo info = new DirectoryInfo(link);
				string target = info.LinkTarget;
				if(target != null)
				{
					// Relative targets ("../drive_c") are relative to the dosdevices folder
					target = Path.GetFullPath(target, Path.Combine(wineprefix, "dosdevices"));
					if(Directory.Exists(target)) return target;
				}
				else if(Directory.Exists(link))
				{
					return link;
				}
			}
			catch(Exception e) when(e is IOException || e is UnauthorizedAccessException) { }

			if(drive == 'c') return Path.Combine(wineprefix, "drive_c");
			if(drive == 'z') return "/";
			return null;
		}

		// Walks 'parts' below 'root', taking each part as written if it exists, otherwise the
		// entry whose name matches ignoring case. Null when a part can't be found.
		internal static string ResolveIgnoringCase(string root, IEnumerable<string> parts)
		{
			string current = root;
			if(!Directory.Exists(current)) return null;

			foreach(string part in parts)
			{
				if(part == ".") continue;
				if(part == "..")
				{
					current = Path.GetDirectoryName(current) ?? current;
					continue;
				}

				string exact = Path.Combine(current, part);
				if(Exists(exact))
				{
					current = exact;
					continue;
				}

				if(!Directory.Exists(current)) return null;
				try
				{
					current = Directory.EnumerateFileSystemEntries(current)
						.Where(e => string.Equals(Path.GetFileName(e), part, StringComparison.OrdinalIgnoreCase))
						.OrderBy(e => e, StringComparer.Ordinal)
						.FirstOrDefault();
				}
				catch(Exception e) when(e is IOException || e is UnauthorizedAccessException)
				{
					return null;
				}
				if(current == null) return null;
			}

			return current;
		}

		private static bool Exists(string path)
		{
			return File.Exists(path) || Directory.Exists(path);
		}

		private static PathConversion Unresolved(string guess, string reason)
		{
			return new PathConversion { Status = PathConversionStatus.Unresolved, Path = guess, Reason = reason };
		}

		#endregion
	}
}
