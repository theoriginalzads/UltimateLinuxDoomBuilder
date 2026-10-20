using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Plugins;

namespace ULDB.Tests
{
	// The in-scope plugins (docs/ULDB-Plan.md, section 0) build with placeholders for their
	// unported UI. This loads them the way General.Main does, then opens a map and switches
	// into every edit mode they add, so a placeholder that breaks a plugin's logic shows up
	// here instead of in the editor.
	[Collection("Editor")]
	public class PluginTests
	{
		private static readonly string[] PLUGINS =
		{
			"BuilderModes", "ThreeDFloorMode", "BuilderEffects", "StairSectorBuilder", "TagRange",
			"SoundPropagationMode", "ColorPicker", "TagExplorer", "CommentsPanel",
		};

		private readonly Xunit.Abstractions.ITestOutputHelper output;

		public PluginTests(Xunit.Abstractions.ITestOutputHelper output) { this.output = output; }

		// Plugins are copied next to the Builder executable (ULDB.<Name>.csproj, CopyToBuilderPlugins).
		private static string PluginsPath => Path.Combine(HeadlessEditor.RepoRoot, "Source", "ULDB", "Builder", "bin",
			new DirectoryInfo(AppContext.BaseDirectory).Parent.Name, "net10.0", "Plugins");

		/// <summary>Runs the test with every plugin loaded and an editing manager that knows their modes.</summary>
		private static void WithPlugins(Action<PluginManager> test)
		{
			HeadlessEditor.Initialize();
			object oldpath = HeadlessEditor.GetGeneralField("pluginspath");
			object oldplugins = HeadlessEditor.GetGeneralField("plugins");
			object oldediting = HeadlessEditor.GetGeneralField("editing");
			object oldactions = HeadlessEditor.GetGeneralField("actions");
			object oldhints = HeadlessEditor.GetGeneralField("hints");
			PluginManager plugins = new PluginManager();
			try
			{
				// Fresh actions each time, so loading the plugins again doesn't redefine theirs.
				HeadlessEditor.SetGeneralField("actions", new CodeImp.DoomBuilder.Actions.ActionManager());
				General.Actions.BindMethods(typeof(General));
				HeadlessEditor.SetGeneralField("hints", new CodeImp.DoomBuilder.Actions.HintsManager());
				HeadlessEditor.SetGeneralField("pluginspath", PluginsPath);
				HeadlessEditor.SetGeneralField("plugins", plugins);
				plugins.LoadAllPlugins();
				HeadlessEditor.SetGeneralField("editing", new EditingManager()); // Collects the plugins' modes
				test(plugins);
			}
			finally
			{
				HeadlessEditor.SetGeneralField("editing", oldediting);
				HeadlessEditor.SetGeneralField("actions", oldactions);
				HeadlessEditor.SetGeneralField("hints", oldhints);
				HeadlessEditor.SetGeneralField("plugins", oldplugins);
				HeadlessEditor.SetGeneralField("pluginspath", oldpath);
			}
		}

		[Fact]
		public void InScopePluginsLoad()
		{
			Assert.True(Directory.Exists(PluginsPath), "No plugins folder at " + PluginsPath);
			WithPlugins(plugins =>
			{
				List<string> loaded = plugins.Plugins.Select(p => p.Assembly.GetName().Name).ToList();
				output.WriteLine("Loaded: " + string.Join(", ", loaded));
				foreach(string name in PLUGINS)
					Assert.Contains(name, loaded);

				// Each plugin's own actions (from its embedded Actions.cfg) are registered.
				foreach(string action in new[] { "threedfloormode_threedfloorhelpermode", "buildereffects_applyjitter", "stairsectorbuilder_stairsectorbuildermode", "tagrange_rangetagselection", "soundpropagationmode_soundpropagationmode", "colorpicker_togglelightpannel" })
					Assert.True(General.Actions.Exists(action), "Action not registered: " + action);
			});
		}

		[SkippableFact]
		public void PluginModesEngageOnAMap()
		{
			string iwad = TestResources.FreedoomIwad;
			Skip.If(iwad == null, "Freedoom 2 IWAD not found; install the freedoom package or set ULDB_TEST_IWAD");
			// Modes run background workers whose progress callbacks redraw the map: in the editor
			// they run on the UI thread, so here they're queued and run on the test's thread.
			UIThread ui = new UIThread();
			SynchronizationContext oldcontext = SynchronizationContext.Current;
			SynchronizationContext.SetSynchronizationContext(ui);
			try { WithPlugins(plugins => EngageModes(plugins, iwad, ui)); }
			finally { SynchronizationContext.SetSynchronizationContext(oldcontext); }
		}

		private void EngageModes(PluginManager plugins, string iwad, UIThread ui)
		{
			{
				string source = Path.Combine(HeadlessEditor.TempDir, "plugins-" + Guid.NewGuid().ToString("N") + ".wad");
				File.Copy(HeadlessEditor.TestMap("3dfloors_slopes.wad"), source);
				using(OffscreenGL gl = new OffscreenGL())
				{
					MapOptions options = new MapOptions { ConfigFile = "UZDoom_DoomUDMF.cfg", CurrentName = "MAP01", ScriptCompiler = "zdoom_acs.cfg" };
					options.Resources.Add(new DataLocation(DataLocation.RESOURCE_WAD, iwad, false, false, false, null));
					MapManager manager = new MapManager();
					HeadlessEditor.SetGeneralField("map", manager);
					try
					{
						Assert.True(manager.InitializeOpenMap(source, options), "InitializeOpenMap failed");
						plugins.OnMapOpenEnd();

						// Every mode a plugin other than BuilderModes adds, from a classic mode and back.
						List<EditModeInfo> modes = General.Editing.ModesInfo.Where(m => m.Plugin != null && m.Plugin.Assembly.GetName().Name != "BuilderModes").ToList();
						output.WriteLine("Plugin modes: " + string.Join(", ", modes.Select(m => m.Type.Name)));
						Assert.NotEmpty(modes);
						List<string> failures = new List<string>();
						foreach(EditModeInfo mode in modes)
						{
							try
							{
								General.Editing.ChangeMode("SectorsMode");
								General.Editing.ChangeMode(mode.Type.Name);
								ui.RunUntilIdle(); // Background work the mode started (sound environments)
								output.WriteLine(mode.Type.Name + " → " + General.Editing.Mode?.GetType().Name);
								General.Editing.ChangeMode("SectorsMode");
								ui.RunUntilIdle();
							}
							catch(Exception e)
							{
								failures.Add(mode.Type.Name + ": " + e);
								try { General.Map.Renderer2D.Finish(); } catch(Exception) { } // A failed redraw leaves a layer open
							}
						}
						Assert.True(failures.Count == 0, string.Join("\n\n", failures));
						Assert.Equal("SectorsMode", General.Editing.Mode?.GetType().Name);
					}
					finally
					{
						try { General.Editing.ChangeMode((EditMode)null); } catch(Exception e) { output.WriteLine("Leaving mode: " + e.Message); }
						try { manager.Dispose(); } catch(Exception e) { output.WriteLine("Dispose: " + e); }
						HeadlessEditor.SetGeneralField("map", null);
					}
				}
			}
		}

		/// <summary>Stands in for the UI thread: posted callbacks queue up and run on the caller's thread.</summary>
		private sealed class UIThread : SynchronizationContext
		{
			private readonly BlockingCollection<(SendOrPostCallback, object)> queue = new BlockingCollection<(SendOrPostCallback, object)>();

			public override void Post(SendOrPostCallback d, object state) => queue.Add((d, state));
			public override void Send(SendOrPostCallback d, object state) => d(state);

			/// <summary>Runs queued callbacks until none arrive for a while (or a time limit passes).</summary>
			public void RunUntilIdle()
			{
				Stopwatch limit = Stopwatch.StartNew();
				while(limit.Elapsed < TimeSpan.FromSeconds(30) && queue.TryTake(out var item, 500))
					item.Item1(item.Item2);
			}
		}
	}
}
