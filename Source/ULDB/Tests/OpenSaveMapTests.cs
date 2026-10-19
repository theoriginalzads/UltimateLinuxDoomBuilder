using System.Reflection;
using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;

namespace ULDB.Tests
{
	// The editor's real open/save path (MapManager.InitializeOpenMap and SaveMap), headless:
	// loads the game config and resources, reads the map, then saves it, which runs the
	// source-built ZDBSP node builder and the ACC, ZT-BCC or BCC script compiler.
	[Collection("Editor")]
	public class OpenSaveMapTests
	{
		private readonly Xunit.Abstractions.ITestOutputHelper output;

		public OpenSaveMapTests(Xunit.Abstractions.ITestOutputHelper output) { this.output = output; }

		[SkippableTheory]
		[InlineData("acs.wad", "zdoom_acs.cfg")] // ACC
		[InlineData("bcs.wad", "zt_bcs.cfg")]    // ZT-BCC
		[InlineData("bcs.wad", "bcs.cfg")]       // BCC
		public void OpensAndSavesMapWithNodesAndCompiledScripts(string mapfile, string scriptcompiler)
		{
			string iwad = TestResources.FreedoomIwad;
			Skip.If(iwad == null, "Freedoom 2 IWAD not found; install the freedoom package or set ULDB_TEST_IWAD");

			HeadlessEditor.Initialize();
			string source = Path.Combine(HeadlessEditor.TempDir, Path.GetFileNameWithoutExtension(mapfile) + "-" + Guid.NewGuid().ToString("N") + ".wad");
			File.Copy(HeadlessEditor.TestMap(mapfile), source);

			// Drop any pre-compiled BEHAVIOR, so it only exists after saving if the compiler ran.
			using(WAD wad = new WAD(source, false))
			{
				Lump behavior = wad.FindLump("BEHAVIOR");
				if(behavior != null) wad.Remove(behavior);
			}
			string saved = Path.ChangeExtension(source, ".saved.wad");

			using(OffscreenGL gl = new OffscreenGL())
			{
				// As the Open Map dialog fills them in.
				MapOptions options = new MapOptions { ConfigFile = "UZDoom_DoomUDMF.cfg", CurrentName = "MAP01" };
				options.ScriptCompiler = scriptcompiler;
				options.Resources.Add(new DataLocation(DataLocation.RESOURCE_WAD, iwad, false, false, false, null));

				// UZDoom's UDMF config doesn't pick a node builder (GZDoom builds nodes itself);
				// choose ZDBSP for saving, as a user can in the game configuration settings.
				CodeImp.DoomBuilder.Config.ConfigurationInfo configinfo = HeadlessEditor.GetConfiguration("UZDoom_DoomUDMF.cfg");
				string oldnodebuilder = configinfo.NodebuilderSave;
				configinfo.NodebuilderSave = "zdbsp_udmf_normal";

				MapManager manager = new MapManager();
				HeadlessEditor.SetGeneralField("map", manager);
				General.ErrorLogger.Clear();
				try
				{
					Assert.True(manager.InitializeOpenMap(source, options), "InitializeOpenMap failed");
					Assert.Equal(8, manager.Map.Linedefs.Count);

					// Mark the map and its scripts as edited; upstream only rebuilds nodes and
					// recompiles scripts when they changed.
					manager.IsChanged = true;
					typeof(MapManager).GetField("scriptschanged", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, true);

					Assert.True(manager.SaveMap(saved, SavePurpose.AsNewFile), "SaveMap failed");
				}
				finally
				{
					foreach(ErrorItem e in General.ErrorLogger.GetErrors(0)) output.WriteLine(e.Type + ": " + e.Description);
					foreach(var e in manager.Errors) output.WriteLine("Compiler: " + e.description);
					try { manager.Dispose(); } catch(Exception e) { output.WriteLine("Dispose: " + e); }
					HeadlessEditor.SetGeneralField("map", null);
					configinfo.NodebuilderSave = oldnodebuilder;
				}
			}

			using(WAD wad = new WAD(saved, true))
			{
				string[] lumps = wad.Lumps.Select(l => l.Name).ToArray();
				output.WriteLine("Saved lumps: " + string.Join(", ", lumps));
				Assert.Contains("TEXTMAP", lumps);
				Assert.Contains("ZNODES", lumps);   // ZDBSP's UDMF nodes
				Assert.Contains("BEHAVIOR", lumps); // Script compiler output
				Assert.Contains("SCRIPTS", lumps);
				Assert.True(wad.FindLump("BEHAVIOR").Length > 0);
				Assert.True(wad.FindLump("ZNODES").Length > 0);
			}
		}
	}

	internal static class TestResources
	{
		// Debian/Ubuntu and AUR package paths, then ~/.local/share/doom; override with ULDB_TEST_IWAD.
		public static string FreedoomIwad => new[]
		{
			Environment.GetEnvironmentVariable("ULDB_TEST_IWAD") ?? "",
			"/usr/share/games/doom/freedoom2.wad",
			"/usr/share/doom/freedoom2.wad",
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "doom", "freedoom2.wad"),
		}.FirstOrDefault(File.Exists);
	}
}
