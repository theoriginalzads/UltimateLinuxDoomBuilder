using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.IO;
using ULDB.Settings;

namespace ULDB.Tests
{
	// Import of existing UDB settings (docs/settings-import.md). Every test builds a fake home
	// folder in a temporary directory with settings files laid out like the real installs.
	public class SettingsImportTests : IDisposable
	{
		private const string FLATPAK_DATA = ".var/app/io.github.ultimatedoombuilder.ultimatedoombuilder/data/Doom Builder";

		private readonly string home;
		private readonly Dictionary<string, string> variables = new Dictionary<string, string>();
		private readonly ImportEnvironment env;

		public SettingsImportTests()
		{
			home = Path.Combine(Path.GetTempPath(), "uldb-import-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(home);
			env = new ImportEnvironment(home, name => variables.TryGetValue(name, out string v) ? v : null);
		}

		public void Dispose()
		{
			try { Directory.Delete(home, true); } catch(IOException) { }
		}

		#region ================== Fixtures

		// Settings options for a fixture file, close to what UDB itself writes
		private sealed class Fixture
		{
			public string Resource = "";
			public string TestProgram = "";
			public string HiddenResource = "";
			public string[] RecentFiles = new string[0];
			public string ScreenshotsPath = null;
			public string AdditionalParameters = "";
			public bool Version = true;
		}

		private static string Q(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

		// Mirrors ProgramConfiguration.Save, ConfigurationInfo.SaveSettings (resources, engines),
		// ActionManager.SaveSettings (shortcuts) and MainForm.SaveRecentFiles
		private static string SettingsText(Fixture f)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("shortcuts\r\n{\r\n");
			sb.Append("\tbuilder_openmap = 131151;\r\n");        // Ctrl+O
			sb.Append("\tbuilder_savemapas = 196691;\r\n");      // Ctrl+Shift+S
			sb.Append("\tbuilder_testmap = 120;\r\n");           // F9
			sb.Append("\tbuilder_zoomin = 65530;\r\n");          // Mouse wheel up (UDB special code)
			sb.Append("\tbuildermodes_visualmode = 87;\r\n");    // W (changed from default Q)
			sb.Append("\tbuilder_deleteitem = 46;\r\n");         // Delete
			sb.Append("}\r\n\r\n");

			sb.Append("recentfiles\r\n{\r\n");
			for(int i = 0; i < f.RecentFiles.Length; i++) sb.Append("\tfile" + i + " = " + Q(f.RecentFiles[i]) + ";\r\n");
			sb.Append("}\r\n\r\n");

			sb.Append("configurations\r\n{\r\n");
			sb.Append("\tuzdoom_doomudmf\r\n\t{\r\n");
			sb.Append("\t\tenabled = true;\r\n");
			sb.Append("\t\tnodebuildersave = \"zdbsp_udmf_normal\";\r\n");
			sb.Append("\t\tnodebuildertest = \"zdbsp_udmf_fast\";\r\n");
			sb.Append("\t\tcurrentengineindex = 0;\r\n");
			sb.Append("\t\tstartmode = \"LinedefsMode\";\r\n");
			sb.Append("\t\tresources\r\n\t\t{\r\n\t\t\tresource0\r\n\t\t\t{\r\n");
			sb.Append("\t\t\t\ttype = 2;\r\n\t\t\t\tlocation = " + Q(f.Resource) + ";\r\n\t\t\t\toption1 = 0;\r\n\t\t\t\toption2 = 0;\r\n\t\t\t\tnotfortesting = 0;\r\n");
			sb.Append("\t\t\t}\r\n\t\t}\r\n");
			sb.Append("\t\tengines\r\n\t\t{\r\n\t\t\tengine0\r\n\t\t\t{\r\n");
			sb.Append("\t\t\t\ttestprogramname = \"UZDoom\";\r\n\t\t\t\ttestprogram = " + Q(f.TestProgram) + ";\r\n");
			sb.Append("\t\t\t\ttestparameters = " + Q("-iwad \"%WP\" -skill \"%S\" -file \"%AP\" \"%F\" +map %L %NM") + ";\r\n");
			sb.Append("\t\t\t\ttestshortpaths = false;\r\n\t\t\t\ttestlinuxpaths = false;\r\n\t\t\t\tcustomparameters = false;\r\n\t\t\t\ttestskill = 3;\r\n");
			sb.Append("\t\t\t\tadditionalparameters = " + Q(f.AdditionalParameters) + ";\r\n");
			sb.Append("\t\t\t}\r\n\t\t}\r\n");
			sb.Append("\t}\r\n");
			// A configuration ULDB hides (Boom); its settings must survive the import
			sb.Append("\tboom_doom2doom\r\n\t{\r\n");
			sb.Append("\t\tenabled = true;\r\n");
			sb.Append("\t\tnodebuildersave = \"zennode_normal\";\r\n");
			sb.Append("\t\tresources\r\n\t\t{\r\n\t\t\tresource0\r\n\t\t\t{\r\n");
			sb.Append("\t\t\t\ttype = 0;\r\n\t\t\t\tlocation = " + Q(f.HiddenResource) + ";\r\n\t\t\t\toption1 = 0;\r\n\t\t\t\toption2 = 0;\r\n\t\t\t\tnotfortesting = 0;\r\n");
			sb.Append("\t\t\t}\r\n\t\t}\r\n");
			sb.Append("\t}\r\n");
			sb.Append("}\r\n\r\n");

			if(f.Version) sb.Append("currentversion = 3000004;\r\n");
			sb.Append("mousespeed = 250;\r\n");
			sb.Append("visualfov = 100;\r\n");
			sb.Append("scriptfontname = \"Consolas\";\r\n");
			sb.Append("scriptfontsize = 12;\r\n");
			sb.Append("scripttabwidth = 2;\r\n");
			sb.Append("scriptusetabs = false;\r\n");
			sb.Append("scriptautoclosebrackets = false;\r\n");
			if(f.ScreenshotsPath != null) sb.Append("screenshotspath = " + Q(f.ScreenshotsPath) + ";\r\n");
			sb.Append("plugins\r\n{\r\n\tbuildermodes\r\n\t{\r\n\t\tchangeheightbysidedef = 1;\r\n\t}\r\n}\r\n");
			return sb.ToString();
		}

		private string Write(string relative, string content)
		{
			string path = Path.Combine(home, relative);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, content);
			return path;
		}

		private string Touch(string relative) => Write(relative, "x");

		private string MakeDir(string relative)
		{
			string path = Path.Combine(home, relative);
			Directory.CreateDirectory(path);
			return path;
		}

		private string Target => Path.Combine(home, ".config", "UltimateLinuxDoomBuilder");

		private static Configuration Load(string file)
		{
			Configuration cfg = new Configuration(file, true);
			Assert.False(cfg.ErrorResult, cfg.ErrorDescription);
			return cfg;
		}

		private static byte[] Hash(string file) => SHA256.HashData(File.ReadAllBytes(file));

		// A Wine prefix with the usual dosdevices links (c: -> ../drive_c, z: -> /)
		private string MakeWinePrefix(string relative)
		{
			string prefix = MakeDir(relative);
			MakeDir(Path.Combine(relative, "drive_c", "users", "doomer"));
			string dosdevices = MakeDir(Path.Combine(relative, "dosdevices"));
			File.CreateSymbolicLink(Path.Combine(dosdevices, "c:"), "../drive_c");
			File.CreateSymbolicLink(Path.Combine(dosdevices, "z:"), "/");
			return prefix;
		}

		#endregion

		#region ================== Detection

		[Fact]
		public void NothingIsDetectedInAnEmptyHome()
		{
			Assert.Empty(UdbSettingsSource.Detect(env));
		}

		[Fact]
		public void DetectsMonoFlatpakWineAndLegacySettings()
		{
			Fixture f = new Fixture();
			string mono = Write(".local/share/Doom Builder/UDBuilder.cfg", SettingsText(f));
			string flatpak = Write(FLATPAK_DATA + "/UDBuilder.cfg", SettingsText(f));
			MakeWinePrefix(".wine");
			string wine = Write(".wine/drive_c/users/doomer/AppData/Local/Doom Builder/UDBuilder.cfg", SettingsText(f));
			MakeWinePrefix("games/udb-prefix");
			string winexp = Write("games/udb-prefix/drive_c/users/doomer/Local Settings/Application Data/Doom Builder/UDBuilder.cfg", SettingsText(f));
			string legacy = Write(".local/share/Doom Builder/GZBuilder.cfg", SettingsText(f));
			variables["WINEPREFIX"] = Path.Combine(home, "games/udb-prefix");

			List<UdbSettingsSource> sources = UdbSettingsSource.Detect(env);

			Assert.Equal(5, sources.Count);
			Assert.Equal(UdbSettingsSourceKind.MonoBuild, sources.Single(s => s.SettingsFile == mono).Kind);
			Assert.Equal(UdbSettingsSourceKind.Flatpak, sources.Single(s => s.SettingsFile == flatpak).Kind);
			Assert.Equal(Path.Combine(home, ".wine"), sources.Single(s => s.SettingsFile == wine).WinePrefix);
			Assert.Equal(Path.Combine(home, "games/udb-prefix"), sources.Single(s => s.SettingsFile == winexp).WinePrefix);
			Assert.True(sources.Single(s => s.SettingsFile == legacy).IsLegacy);

			// UDBuilder.cfg is preferred over GZBuilder.cfg, newest first
			Assert.True(sources.Last().IsLegacy);
			File.SetLastWriteTimeUtc(flatpak, DateTime.UtcNow.AddMinutes(5));
			Assert.Equal(flatpak, UdbSettingsSource.Detect(env)[0].SettingsFile);
		}

		[Fact]
		public void MonoBuildFollowsXdgDataHome()
		{
			string data = MakeDir("xdgdata");
			variables["XDG_DATA_HOME"] = data;
			string file = Write("xdgdata/Doom Builder/UDBuilder.cfg", SettingsText(new Fixture()));

			UdbSettingsSource source = Assert.Single(UdbSettingsSource.Detect(env));
			Assert.Equal(file, source.SettingsFile);
			Assert.Equal(UdbSettingsSourceKind.MonoBuild, source.Kind);
		}

		[Fact]
		public void UserPickedFolderOnAWindowsDrive()
		{
			string file = Write("mnt/win/Users/Doomer/AppData/Local/Doom Builder/UDBuilder.cfg", SettingsText(new Fixture()));

			// The "Doom Builder" folder, the file itself, or the user profile folder all work
			foreach(string picked in new[] { Path.GetDirectoryName(file), file, Path.Combine(home, "mnt/win/Users/Doomer") })
			{
				UdbSettingsSource source = UdbSettingsSource.FromPath(picked);
				Assert.NotNull(source);
				Assert.Equal(file, source.SettingsFile);
				Assert.Equal(UdbSettingsSourceKind.UserPicked, source.Kind);
				Assert.Equal(Path.Combine(home, "mnt/win"), source.DriveRoots['c']);
			}

			Assert.Null(UdbSettingsSource.FromPath(Path.Combine(home, "mnt")));
		}

		#endregion

		#region ================== Import

		[Fact]
		public void MonoImportKeepsEverythingAndLeavesTheSourceUntouched()
		{
			string wad = Touch("doom/freedoom2.wad");
			string engine = Touch("bin/uzdoom");
			string map = Touch("maps/mymap.wad");
			Fixture f = new Fixture { Resource = wad, TestProgram = engine, RecentFiles = new[] { map, "/nowhere/gone.wad" }, HiddenResource = wad };
			string file = Write(".local/share/Doom Builder/UDBuilder.cfg", SettingsText(f));
			byte[] before = Hash(file);
			DateTime modified = File.GetLastWriteTimeUtc(file);

			UdbSettingsImportResult result = UdbSettingsImport.Import(UdbSettingsSource.Detect(env)[0], Target);

			Assert.True(result.Success, result.Error);
			Assert.Equal(before, Hash(file));
			Assert.Equal(modified, File.GetLastWriteTimeUtc(file));

			Configuration cfg = Load(result.TargetFile);
			Assert.Equal(wad, cfg.ReadSetting("configurations.uzdoom_doomudmf.resources.resource0.location", ""));
			Assert.Equal(engine, cfg.ReadSetting("configurations.uzdoom_doomudmf.engines.engine0.testprogram", ""));
			Assert.Equal("zdbsp_udmf_normal", cfg.ReadSetting("configurations.uzdoom_doomudmf.nodebuildersave", ""));
			Assert.Equal("zdbsp_udmf_fast", cfg.ReadSetting("configurations.uzdoom_doomudmf.nodebuildertest", ""));
			Assert.Equal("LinedefsMode", cfg.ReadSetting("configurations.uzdoom_doomudmf.startmode", ""));
			Assert.Equal(250, cfg.ReadSetting("mousespeed", 0));
			Assert.Equal(1, cfg.ReadSetting("plugins.buildermodes.changeheightbysidedef", 0));

			// Recent files: kept in order; the missing one is kept and reported
			Assert.Equal(map, cfg.ReadSetting("recentfiles.file0", ""));
			Assert.Equal("/nowhere/gone.wad", cfg.ReadSetting("recentfiles.file1", ""));
			Assert.Equal(2, result.RecentFileCount);
			ImportedPath gone = Assert.Single(result.UnresolvedPaths);
			Assert.Equal("recentfiles.file1", gone.Setting);
			Assert.Contains("/nowhere/gone.wad", result.ToSummaryText());

			Assert.Empty(result.ConvertedPaths);
			Assert.True(File.Exists(Path.Combine(Target, UdbSettingsImport.SUMMARY_FILE)));
		}

		[Fact]
		public void ScriptEditorSettingsAreImported()
		{
			Write(".local/share/Doom Builder/UDBuilder.cfg", SettingsText(new Fixture()));
			UdbSettingsImportResult result = UdbSettingsImport.Import(UdbSettingsSource.Detect(env)[0], Target);

			Configuration cfg = Load(result.TargetFile);
			Assert.Equal("Consolas", cfg.ReadSetting("scriptfontname", ""));
			Assert.Equal(12, cfg.ReadSetting("scriptfontsize", 0));
			Assert.Equal(2, cfg.ReadSetting("scripttabwidth", 0));
			Assert.False(cfg.ReadSetting("scriptusetabs", true));
			Assert.False(cfg.ReadSetting("scriptautoclosebrackets", true));
		}

		[Fact]
		public void ShortcutsImportUnchanged()
		{
			Write(".local/share/Doom Builder/UDBuilder.cfg", SettingsText(new Fixture()));
			UdbSettingsImportResult result = UdbSettingsImport.Import(UdbSettingsSource.Detect(env)[0], Target);

			// UDB stores WinForms Keys values; ULDB uses the same codes (ULDB.Input.KeyMapping), so no conversion
			Configuration cfg = Load(result.TargetFile);
			Assert.Equal((int)(Keys.Control | Keys.O), cfg.ReadSetting("shortcuts.builder_openmap", 0));
			Assert.Equal((int)(Keys.Control | Keys.Shift | Keys.S), cfg.ReadSetting("shortcuts.builder_savemapas", 0));
			Assert.Equal((int)Keys.F9, cfg.ReadSetting("shortcuts.builder_testmap", 0));
			Assert.Equal((int)Keys.W, cfg.ReadSetting("shortcuts.buildermodes_visualmode", 0));
			Assert.Equal((int)Keys.Delete, cfg.ReadSetting("shortcuts.builder_deleteitem", 0));
			Assert.Equal(65530, cfg.ReadSetting("shortcuts.builder_zoomin", 0)); // Mouse wheel code, not a Keys value
			Assert.Equal(6, result.ShortcutCount);
		}

		[Fact]
		public void WinePathsAreConverted()
		{
			string prefix = MakeWinePrefix(".wine");
			string wad = Touch(".wine/drive_c/Games/Doom/DOOM2.WAD");
			string engine = Touch(".wine/drive_c/Games/UZDoom/uzdoom.exe");
			string linuxwad = Touch("wads/MyResource.pk3");
			string map = Touch("maps/test.wad");
			Fixture f = new Fixture
			{
				Resource = @"c:\games\doom\doom2.wad",               // different case than on disk
				TestProgram = @"C:\Games\UZDoom\uzdoom.exe",
				HiddenResource = "Z:" + linuxwad.Replace('/', '\\'), // Z: is the Linux root in Wine
				RecentFiles = new[] { "Z:" + map.Replace('/', '\\'), @"D:\Maps\other.wad", @"C:\Games\Doom\missing.wad" },
				AdditionalParameters = @"-file C:\Games\Doom\extra.pk3",
			};
			string file = Write(".wine/drive_c/users/doomer/AppData/Local/Doom Builder/UDBuilder.cfg", SettingsText(f));
			byte[] before = Hash(file);

			UdbSettingsSource source = Assert.Single(UdbSettingsSource.Detect(env));
			Assert.Equal(prefix, source.WinePrefix);
			UdbSettingsImportResult result = UdbSettingsImport.Import(source, Target);
			Assert.True(result.Success, result.Error);
			Assert.Equal(before, Hash(file));

			Configuration cfg = Load(result.TargetFile);
			Assert.Equal(wad, cfg.ReadSetting("configurations.uzdoom_doomudmf.resources.resource0.location", ""));
			Assert.Equal(engine, cfg.ReadSetting("configurations.uzdoom_doomudmf.engines.engine0.testprogram", ""));
			Assert.Equal(linuxwad, cfg.ReadSetting("configurations.boom_doom2doom.resources.resource0.location", ""));
			Assert.Equal(map, cfg.ReadSetting("recentfiles.file0", ""));

			// Unresolved: kept exactly as they were and listed
			Assert.Equal(@"D:\Maps\other.wad", cfg.ReadSetting("recentfiles.file1", ""));
			Assert.Equal(@"C:\Games\Doom\missing.wad", cfg.ReadSetting("recentfiles.file2", ""));
			List<ImportedPath> unresolved = result.UnresolvedPaths.ToList();
			Assert.Equal(new[] { "recentfiles.file1", "recentfiles.file2" }, unresolved.Select(p => p.Setting).OrderBy(s => s));
			Assert.Contains("drive D:", unresolved.Single(p => p.Setting == "recentfiles.file1").Reason);
			Assert.Equal(4, result.ConvertedPaths.Count());

			// Things to review: the Windows engine, and the Windows path inside the extra parameters (left alone)
			Assert.Contains(result.Notes, n => n.Contains("uzdoom.exe") && n.Contains("Windows program"));
			Assert.Contains(result.Notes, n => n.Contains("additionalparameters"));
			Assert.Equal(@"-file C:\Games\Doom\extra.pk3", cfg.ReadSetting("configurations.uzdoom_doomudmf.engines.engine0.additionalparameters", ""));

			string summary = result.ToSummaryText();
			Assert.Contains(@"D:\Maps\other.wad", summary);
			Assert.Contains("2 paths could not be found", summary);
		}

		[Fact]
		public void WineWithoutDosdevicesUsesWineDefaults()
		{
			string prefix = MakeDir("oldprefix");
			string wad = Touch("oldprefix/drive_c/doom/doom.wad");
			SettingsPathConverter converter = new SettingsPathConverter(prefix, null);

			Assert.Equal(Path.Combine(prefix, "drive_c"), converter.DriveRoot('C'));
			Assert.Equal("/", converter.DriveRoot('z'));
			Assert.Null(converter.DriveRoot('e'));

			PathConversion pc = converter.Convert(@"C:\DOOM\DOOM.WAD");
			Assert.Equal(PathConversionStatus.Converted, pc.Status);
			Assert.Equal(wad, pc.Path);
		}

		[Fact]
		public void WindowsDriveImportFromUserPickedFolder()
		{
			string wad = Touch("mnt/win/Doom/IWADs/doom2.wad");
			string engine = Touch("mnt/win/Program Files/GZDoom/gzdoom.exe");
			Fixture f = new Fixture { Resource = @"C:\Doom\IWADs\DOOM2.wad", TestProgram = @"C:\Program Files\GZDoom\gzdoom.exe", HiddenResource = @"\\server\share\doom.wad" };
			string file = Write("mnt/win/Users/Doomer/AppData/Local/Doom Builder/UDBuilder.cfg", SettingsText(f));
			byte[] before = Hash(file);

			UdbSettingsImportResult result = UdbSettingsImport.Import(UdbSettingsSource.FromPath(Path.GetDirectoryName(file)), Target);
			Assert.True(result.Success, result.Error);
			Assert.Equal(before, Hash(file));

			Configuration cfg = Load(result.TargetFile);
			Assert.Equal(wad, cfg.ReadSetting("configurations.uzdoom_doomudmf.resources.resource0.location", ""));
			Assert.Equal(engine, cfg.ReadSetting("configurations.uzdoom_doomudmf.engines.engine0.testprogram", ""));
			Assert.Equal(@"\\server\share\doom.wad", cfg.ReadSetting("configurations.boom_doom2doom.resources.resource0.location", ""));
			Assert.Contains("UNC", Assert.Single(result.UnresolvedPaths).Reason);
		}

		[Fact]
		public void LinuxPathsAreMatchedIgnoringCase()
		{
			string wad = Touch("Doom/IWADs/Doom2.WAD");
			SettingsPathConverter converter = new SettingsPathConverter(null, null);

			PathConversion exact = converter.Convert(wad);
			Assert.Equal(PathConversionStatus.Unchanged, exact.Status);

			PathConversion wrongcase = converter.Convert(Path.Combine(home, "doom/iwads/doom2.wad"));
			Assert.Equal(PathConversionStatus.Converted, wrongcase.Status);
			Assert.Equal(wad, wrongcase.Path);

			Assert.Equal(PathConversionStatus.Unresolved, converter.Convert(Path.Combine(home, "doom/iwads/doom.wad")).Status);
			Assert.Equal(PathConversionStatus.NotAPath, converter.Convert("Consolas").Status);
			Assert.Equal(PathConversionStatus.Unresolved, converter.Convert(@"C:\Doom\doom2.wad").Status); // No drive known
		}

		[Fact]
		public void HiddenConfigurationSettingsAreKept()
		{
			string wad = Touch("doom/doom2.wad");
			Write(".local/share/Doom Builder/UDBuilder.cfg", SettingsText(new Fixture { Resource = wad, HiddenResource = wad }));

			UdbSettingsImportResult result = UdbSettingsImport.Import(UdbSettingsSource.Detect(env)[0], Target);

			Assert.Equal(new[] { "boom_doom2doom", "uzdoom_doomudmf" }, result.Configurations.OrderBy(s => s));
			Assert.Equal(new[] { "boom_doom2doom" }, result.HiddenConfigurations);
			Configuration cfg = Load(result.TargetFile);
			Assert.Equal("zennode_normal", cfg.ReadSetting("configurations.boom_doom2doom.nodebuildersave", ""));
			Assert.Equal(wad, cfg.ReadSetting("configurations.boom_doom2doom.resources.resource0.location", ""));
		}

		[Fact]
		public void LegacyGZBuilderSettingsImport()
		{
			string wad = Touch("doom/doom2.wad");
			Write(".local/share/Doom Builder/GZBuilder.cfg", SettingsText(new Fixture { Resource = wad, Version = false }));

			UdbSettingsSource source = Assert.Single(UdbSettingsSource.Detect(env));
			Assert.True(source.IsLegacy);
			UdbSettingsImportResult result = UdbSettingsImport.Import(source, Target);

			Assert.True(result.Success, result.Error);
			Assert.Equal("UDBuilder.cfg", Path.GetFileName(result.TargetFile));
			Configuration cfg = Load(result.TargetFile);
			Assert.Equal(wad, cfg.ReadSetting("configurations.uzdoom_doomudmf.resources.resource0.location", ""));

			// Upstream throws away settings without a version number; the import adds one
			Assert.True(cfg.SettingExists("currentversion"));
			Assert.Contains(result.Notes, n => n.Contains("version number"));
		}

		[Fact]
		public void ExistingSettingsAreBackedUp()
		{
			Write(".local/share/Doom Builder/UDBuilder.cfg", SettingsText(new Fixture()));
			string old = Write(".config/UltimateLinuxDoomBuilder/UDBuilder.cfg", "currentversion = 1;\r\nmousespeed = 7;\r\n");

			UdbSettingsImportResult result = UdbSettingsImport.Import(UdbSettingsSource.Detect(env)[0], Target);

			Assert.Equal(old + UdbSettingsImport.BACKUP_SUFFIX, result.BackupFile);
			Assert.Equal(7, Load(result.BackupFile).ReadSetting("mousespeed", 0));
			Assert.Equal(250, Load(result.TargetFile).ReadSetting("mousespeed", 0));
		}

		[Fact]
		public void ImportingUlDbsOwnFileDoesNothing()
		{
			string own = Write(".config/UltimateLinuxDoomBuilder/UDBuilder.cfg", SettingsText(new Fixture()));
			byte[] before = Hash(own);

			UdbSettingsImportResult result = UdbSettingsImport.Import(UdbSettingsSource.FromPath(own), Target);

			Assert.True(result.Success);
			Assert.True(result.SameFile);
			Assert.Equal(before, Hash(own));
			Assert.False(File.Exists(own + UdbSettingsImport.BACKUP_SUFFIX));
		}

		[Fact]
		public void BrokenSourceFailsCleanly()
		{
			Write(".local/share/Doom Builder/UDBuilder.cfg", "currentversion = 1;\r\nmousespeed = bogus;\r\n");
			UdbSettingsImportResult result = UdbSettingsImport.Import(UdbSettingsSource.Detect(env)[0], Target);

			Assert.False(result.Success);
			Assert.NotNull(result.Error);
			Assert.False(File.Exists(Path.Combine(Target, "UDBuilder.cfg")));
		}

		#endregion

		#region ================== First run

		[Fact]
		public void FirstRunOffersImportWhenNoSettingsExist()
		{
			Write(".local/share/Doom Builder/UDBuilder.cfg", SettingsText(new Fixture()));
			UdbSettingsSource asked = null;
			UdbSettingsImportResult reported = null;

			UdbSettingsImportResult result = UdbSettingsImport.OfferImportOnFirstRun(new string[0], env, s => { asked = s; return true; }, r => reported = r);

			Assert.NotNull(asked);
			Assert.Same(result, reported);
			Assert.True(result.Success);
			Assert.True(UdbSettingsImport.SettingsExist(Target));
		}

		[Fact]
		public void FirstRunDeclinedImportsNothing()
		{
			Write(".local/share/Doom Builder/UDBuilder.cfg", SettingsText(new Fixture()));
			Assert.Null(UdbSettingsImport.OfferImportOnFirstRun(new string[0], env, s => false, r => Assert.Fail("No summary expected")));
			Assert.False(UdbSettingsImport.SettingsExist(Target));
		}

		[Fact]
		public void FirstRunDoesNotAskWhenUlDbSettingsExist()
		{
			Write(".local/share/Doom Builder/UDBuilder.cfg", SettingsText(new Fixture()));
			string own = Write(".config/UltimateLinuxDoomBuilder/UDBuilder.cfg", "currentversion = 1;\r\n");
			byte[] before = Hash(own);

			Assert.Null(UdbSettingsImport.OfferImportOnFirstRun(new string[0], env, s => { Assert.Fail("Should not ask"); return true; }));
			Assert.Equal(before, Hash(own));
		}

		[Fact]
		public void FirstRunDoesNotAskWithoutSourceOrInPortableMode()
		{
			Func<UdbSettingsSource, bool> fail = s => { Assert.Fail("Should not ask"); return true; };
			Assert.Null(UdbSettingsImport.OfferImportOnFirstRun(new string[0], env, fail));

			Write(".local/share/Doom Builder/UDBuilder.cfg", SettingsText(new Fixture()));
			Assert.Null(UdbSettingsImport.OfferImportOnFirstRun(new[] { "-portable" }, env, fail));
			Assert.Null(UdbSettingsImport.OfferImportOnFirstRun(new[] { "-nosettings" }, env, fail));
		}

		[Fact]
		public void StartupHandlesTheImportSwitchAndRemovesIt()
		{
			// Message boxes go to the headless host here, which answers OK
			string file = Write("elsewhere/UDBuilder.cfg", SettingsText(new Fixture()));
			Write(".config/UltimateLinuxDoomBuilder/UDBuilder.cfg", "currentversion = 1;\r\nmousespeed = 7;\r\n");

			string[] rest = UdbSettingsImport.RunAtStartup(new[] { "-map", "MAP01", UdbSettingsImport.IMPORT_ARGUMENT + "=" + file, "level.wad" }, env);

			Assert.Equal(new[] { "-map", "MAP01", "level.wad" }, rest);
			Assert.Equal(250, Load(Path.Combine(Target, "UDBuilder.cfg")).ReadSetting("mousespeed", 0));
			Assert.True(File.Exists(Path.Combine(Target, "UDBuilder.cfg" + UdbSettingsImport.BACKUP_SUFFIX)));
		}

		#endregion
	}

	// The imported file must load through upstream's own ProgramConfiguration
	[Collection("Editor")]
	public class SettingsImportLoadTests
	{
		[Fact]
		public void ImportedSettingsLoadInUpstreamProgramConfiguration()
		{
			HeadlessEditor.Initialize();
			string home = Path.Combine(Path.GetTempPath(), "uldb-import-load-" + Guid.NewGuid().ToString("N"));
			try
			{
				string source = Path.Combine(home, "UDBuilder.cfg");
				Directory.CreateDirectory(home);
				File.WriteAllText(source, "currentversion = 3000004;\r\nmousespeed = 250;\r\nscripttabwidth = 2;\r\nshortcuts\r\n{\r\n\tbuilder_openmap = 131151;\r\n}\r\n");
				string target = Path.Combine(home, "uldb");

				UdbSettingsImportResult result = UdbSettingsImport.Import(UdbSettingsSource.FromPath(source), target);
				Assert.True(result.Success, result.Error);

				ProgramConfiguration settings = new ProgramConfiguration();
				Assert.True(settings.Load(result.TargetFile, Path.Combine(AppContext.BaseDirectory, "UDBuilder.default.cfg"), ""));
				Assert.Equal(250, settings.MouseSpeed);
				Assert.Equal(2, settings.ScriptTabWidth);
				Assert.Equal((int)(Keys.Control | Keys.O), settings.ReadSetting("shortcuts.builder_openmap", 0));
			}
			finally
			{
				Directory.Delete(home, true);
			}
		}
	}
}
