using System;
using System.Collections.Generic;
using System.IO;
using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Windows;

namespace ULDB.Tests
{
	// The edit forms are model ports: upstream's form code runs unchanged on ULDB.Compat. This
	// constructs them and runs their Setup on a real map, headless, as MainForm does before
	// showing them, so missing compat behaviour shows up here instead of in the UI.
	[Collection("Editor")]
	public class EditFormTests
	{
		private readonly Xunit.Abstractions.ITestOutputHelper output;

		public EditFormTests(Xunit.Abstractions.ITestOutputHelper output) { this.output = output; }

		private void WithMap(Action<MapManager> test)
		{
			string iwad = TestResources.FreedoomIwad;
			Skip.If(iwad == null, "Freedoom 2 IWAD not found; install the freedoom package or set ULDB_TEST_IWAD");
			HeadlessEditor.Initialize();
			string source = Path.Combine(HeadlessEditor.TempDir, "editforms-" + Guid.NewGuid().ToString("N") + ".wad");
			File.Copy(HeadlessEditor.TestMap("small.wad"), source);

			using(OffscreenGL gl = new OffscreenGL())
			{
				MapOptions options = new MapOptions { ConfigFile = "UZDoom_DoomUDMF.cfg", CurrentName = "MAP01", ScriptCompiler = "zdoom_acs.cfg" };
				options.Resources.Add(new DataLocation(DataLocation.RESOURCE_WAD, iwad, false, false, false, null));
				MapManager manager = new MapManager();
				HeadlessEditor.SetGeneralField("map", manager);
				try
				{
					Assert.True(manager.InitializeOpenMap(source, options), "InitializeOpenMap failed");
					test(manager);
				}
				finally
				{
					try { manager.Dispose(); } catch(Exception e) { output.WriteLine("Dispose: " + e); }
					HeadlessEditor.SetGeneralField("map", null);
				}
			}
		}

		[SkippableFact]
		public void TextureBrowserFormListsTexturesAndSelectsTheCurrentOne()
		{
			WithMap(manager =>
			{
				using(TextureBrowserForm f = new TextureBrowserForm("STARTAN3", false))
				{
					f.RaiseShown();
					CodeImp.DoomBuilder.Controls.ImageBrowserControl browser = (CodeImp.DoomBuilder.Controls.ImageBrowserControl)typeof(TextureBrowserForm)
						.GetField("browser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(f);
					Assert.NotNull(browser.SelectedItem);
					Assert.Equal("STARTAN3", browser.SelectedItem.TextureName);
				}
			});
		}

		[SkippableFact]
		public void VertexEditFormSetsUp()
		{
			WithMap(manager =>
			{
				using(VertexEditForm f = new VertexEditForm())
					f.Setup(new List<Vertex> { System.Linq.Enumerable.First(manager.Map.Vertices) }, false);
			});
		}

		[SkippableFact]
		public void ThingEditFormUDMFSetsUp()
		{
			WithMap(manager =>
			{
				using(ThingEditFormUDMF f = new ThingEditFormUDMF())
					f.Setup(new List<Thing> { System.Linq.Enumerable.First(manager.Map.Things) });
			});
		}

		[SkippableFact]
		public void SectorEditFormUDMFSetsUp()
		{
			WithMap(manager =>
			{
				using(SectorEditFormUDMF f = new SectorEditFormUDMF())
					f.Setup(new List<Sector> { System.Linq.Enumerable.First(manager.Map.Sectors) });
			});
		}

		[SkippableFact]
		public void LinedefEditFormUDMFSetsUp()
		{
			WithMap(manager =>
			{
				// A two-sided line, so both sides' controls are filled.
				Linedef line = System.Linq.Enumerable.First(manager.Map.Linedefs, l => l.Back != null);
				using(LinedefEditFormUDMF f = new LinedefEditFormUDMF(false, false))
					f.Setup(new List<Linedef> { line }, false, false);
			});
		}

		[SkippableFact]
		public void ActionBrowserFormBuildsItsTree()
		{
			WithMap(manager =>
			{
				using(ActionBrowserForm f = new ActionBrowserForm(80, false))
				{
					var tree = (System.Windows.Forms.TreeView)typeof(ActionBrowserForm).GetField("actions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(f);
					Assert.NotEmpty(tree.Nodes);
				}
			});
		}
	}
}
