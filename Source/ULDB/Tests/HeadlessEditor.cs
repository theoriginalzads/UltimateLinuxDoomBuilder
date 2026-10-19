// Sets up just enough of the editor's static state (CodeImp.DoomBuilder.General) to
// load game configurations and read/write maps, without a UI or a render device.
// Mirrors the start of General.Main. Fields are private upstream, so this uses
// reflection; it only runs in tests.

using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Map;

namespace ULDB.Tests
{
	public static class HeadlessEditor
	{
		private static readonly object initlock = new object();
		private static bool initialized;

		public static string RepoRoot { get; } = FindRepoRoot();
		public static string TempDir { get; } = Path.Combine(Path.GetTempPath(), "uldb-tests-" + Environment.ProcessId);
		public static List<string> HeadlessLog { get; } = new List<string>();

		public static void Initialize()
		{
			lock(initlock)
			{
				if(initialized) return;

				WinFormsHost.Current = new HeadlessWinFormsHost();
				WinFormsHost.Log = line => { lock(HeadlessLog) HeadlessLog.Add(line); };

				string apppath = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
				Directory.CreateDirectory(TempDir);

				SetField("thisasm", typeof(General).Assembly);
				SetField("apppath", apppath);
				SetField("temppath", TempDir);
				SetField("settingspath", TempDir);
				SetField("restorepath", Path.Combine(TempDir, "Restore"));
				SetField("configspath", Path.Combine(apppath, "Configurations"));
				SetField("compilerspath", Path.Combine(apppath, "Compilers"));
				SetField("pluginspath", Path.Combine(apppath, "Plugins"));
				SetField("scriptspath", Path.Combine(apppath, "Scripting"));
				SetField("snippetspath", Path.Combine(apppath, "Snippets"));
				SetField("spritespath", Path.Combine(apppath, "Sprites"));
				SetField("texturespath", Path.Combine(apppath, "Textures"));
				SetField("logfile", Path.Combine(TempDir, "UDBuilder.log"));

				ProgramConfiguration settings = new ProgramConfiguration();
				string defaults = Path.Combine(apppath, "UDBuilder.default.cfg");
				if(!settings.Load(Path.Combine(TempDir, "UDBuilder.cfg"), defaults, ""))
					throw new InvalidOperationException("Unable to load program settings from " + defaults);
				SetField("settings", settings);
				SetField("errorlogger", new ErrorLogger());

				// Actions (loaded from the embedded Actions.cfg); managers bind to them on creation.
				SetField("actions", new CodeImp.DoomBuilder.Actions.ActionManager());
				General.Actions.BindMethods(typeof(General));

				// The generated MainForm placeholder: a main window that does nothing.
				SetField("mainwindow", new CodeImp.DoomBuilder.Windows.MainForm());
				SetField("toastmanager", new ToastManager(new Control()));
				SetField("autosaver", new AutoSaver());

				MapSet.Initialize();

				// Same order as General.Main (minus UI and plugins).
				CallStatic("LoadAllGameConfigurations");
				SetField("plugins", new CodeImp.DoomBuilder.Plugins.PluginManager()); // No plugins loaded
				SetField("editing", new CodeImp.DoomBuilder.Editing.EditingManager());
				CallStatic("LoadAllCompilerConfigurations");
				CallStatic("LoadAllNodebuilderConfigurations");
				CallStatic("LoadAllScriptConfigurations");
				SetField("colors", new ColorCollection(settings.Config));
				SetField("types", new CodeImp.DoomBuilder.Types.TypesManager());

				initialized = true;
			}
		}

		/// <summary>Finds a loaded game configuration by its file name, e.g. "UZDoom_DoomUDMF.cfg".</summary>
		public static ConfigurationInfo GetConfiguration(string filename)
		{
			Initialize();
			ConfigurationInfo info = General.GetConfigurationInfo(filename);
			if(info == null || !string.Equals(info.Filename, filename, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("Game configuration " + filename + " not loaded. Loaded: " + string.Join(", ", General.Configs.Select(c => c.Filename)));
			return info;
		}

		/// <summary>Creates a MapManager with a game configuration loaded, as if a map were being opened.</summary>
		public static MapManager CreateMapManager(string configfile)
		{
			ConfigurationInfo info = GetConfiguration(configfile);
			MapManager manager = new MapManager();
			GameConfiguration config = new GameConfiguration(info.Configuration);
			info.ApplyDefaults(config);
			typeof(MapManager).GetField("config", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, config);
			typeof(MapManager).GetField("configinfo", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(manager, info);

			// An empty map, as for a new map; resource loading updates its things.
			typeof(MapManager).GetField("map", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, new MapSet());

			// Map options, as for a new map using this configuration.
			MapOptions options = new MapOptions { ConfigFile = configfile, CurrentName = "MAP01" };
			typeof(MapManager).GetField("options", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, options);
			SetField("map", manager);

			// The map format interface (UDMF, Hexen or Doom), which MapSet uses for limits.
			// Its constructor reads General.Map, so this comes after installing the manager.
			typeof(MapManager).GetField("io", BindingFlags.NonPublic | BindingFlags.Instance)
				.SetValue(manager, CodeImp.DoomBuilder.IO.MapSetIO.Create(config.FormatInterface, null, manager));
			return manager;
		}

		/// <summary>Path to a generated test map (tests/maps), generating the set if needed.</summary>
		public static string TestMap(string name)
		{
			string dir = Path.Combine(RepoRoot, "tests", "maps");
			string path = Path.Combine(dir, name);
			lock(initlock)
			{
				if(!File.Exists(path))
				{
					ProcessStartInfo psi = new ProcessStartInfo("python3", "\"" + Path.Combine(RepoRoot, "tools", "testmaps", "make_testmaps.py") + "\" \"" + dir + "\"")
					{
						RedirectStandardOutput = true,
						RedirectStandardError = true
					};
					using(Process p = Process.Start(psi))
					{
						string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
						p.WaitForExit();
						if(p.ExitCode != 0) throw new InvalidOperationException("make_testmaps.py failed:\n" + output);
					}
				}
			}
			return path;
		}

		/// <summary>Sets one of General's private static fields (e.g. "map").</summary>
		public static void SetGeneralField(string name, object value) => SetField(name, value);

		private static void SetField(string name, object value)
		{
			FieldInfo f = typeof(General).GetField(name, BindingFlags.NonPublic | BindingFlags.Static)
				?? throw new MissingFieldException(nameof(General), name);
			f.SetValue(null, value);
		}

		private static void CallStatic(string name)
		{
			MethodInfo m = typeof(General).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
				?? throw new MissingMethodException(nameof(General), name);
			m.Invoke(null, null);
		}

		private static string FindRepoRoot()
		{
			for(DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
				if(File.Exists(Path.Combine(d.FullName, "Builder.Linux.sln"))) return d.FullName;
			throw new DirectoryNotFoundException("Can't find the repository root (Builder.Linux.sln) above " + AppContext.BaseDirectory);
		}
	}
}
