// ULDB: Linux file-system helpers used by #if ULDB blocks in upstream code.

using System;
using System.IO;

namespace ULDB.Platform
{
	public static class Paths
	{
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
