using ULDB.Platform;

namespace ULDB.Tests
{
	public class PlatformTests
	{
		[Fact]
		public void SettingsDirectoryFollowsXdgConfigHome()
		{
			string old = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
			try
			{
				Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", "/tmp/uldb-xdg-test");
				Assert.Equal("/tmp/uldb-xdg-test/UltimateLinuxDoomBuilder", Paths.SettingsDirectory);

				// Relative values are invalid per the XDG spec and must be ignored.
				Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", "relative/path");
				Assert.Equal(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "UltimateLinuxDoomBuilder"), Paths.SettingsDirectory);
			}
			finally
			{
				Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", old);
			}
		}

		[Fact]
		public void DirectoryWritabilityIsDetected()
		{
			string dir = Path.Combine(Path.GetTempPath(), "uldb-writable-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(dir);
			try
			{
				Assert.True(Paths.IsDirectoryWritable(dir));
				Assert.Empty(Directory.GetFiles(dir)); // The probe file cleans itself up
				Assert.False(Paths.IsDirectoryWritable(Path.Combine(dir, "does-not-exist")));
			}
			finally
			{
				Directory.Delete(dir, true);
			}
		}
	}
}
