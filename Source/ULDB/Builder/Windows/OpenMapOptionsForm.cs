// ULDB: Avalonia port of upstream Windows/OpenMapOptionsForm.cs, over the controls of
// OpenMapOptionsView.axaml. Upstream's logic and handler names are kept; differences:
//  - the maps list is a ListBox of names (upstream: ListView)
//  - only GZDoom/UZDoom configurations are listed (ULDB.Scope.ScopeFilter); a map whose
//    saved configuration is hidden gets the closest visible one (docs/scope-hidden.md)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Avalonia.Interactivity;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using ULDB.Scope;
using AvComboBox = Avalonia.Controls.ComboBox;
using AvCheckBox = Avalonia.Controls.CheckBox;
using AvButton = Avalonia.Controls.Button;
using AvListBox = Avalonia.Controls.ListBox;
using AvTextBlock = Avalonia.Controls.TextBlock;

namespace CodeImp.DoomBuilder.Windows
{
	internal partial class OpenMapOptionsForm : DelayedForm
	{
		// Variables
		private Configuration mapsettings;
		private MapOptions options;
		private WAD wadfile;
		private readonly string filepathname;
		private string selectedmapname;
		private static readonly Regex episodemapregex = new Regex("^E[1-9]M[1-9]$");
		private static readonly Regex noepisodemapregex = new Regex("^MAP[0-9][0-9]$");

		// Controls (OpenMapOptionsView.axaml)
		private readonly OpenMapOptionsView view;
		private AvComboBox config, scriptcompiler;
		private AvListBox mapslist;
		private readonly ObservableCollection<string> mapnames = new ObservableCollection<string>();
		private ResourceListEditor datalocations;
		private AvCheckBox strictpatches, longtexturenames;
		private AvButton apply, cancel;
		private AvTextBlock scriptcompilerlabel;
		private bool updatingmaps;

		// Properties
		public MapOptions Options { get { return options; } }

		// Constructor
		public OpenMapOptionsForm(string filepathname) : this(filepathname, null) { }

		// Constructor
		public OpenMapOptionsForm(string filepathname, MapOptions options)
		{
			// Initialize
			view = new OpenMapOptionsView();
			InitializeComponent();
			this.Text = "Open Map from " + Path.GetFileName(filepathname);
			this.filepathname = filepathname;
			this.options = options;
			datalocations.StartPath = filepathname; //mxd
			datalocations.IsMapControl = true;
		}

		// ULDB: takes the controls from the view and connects upstream's handlers.
		private void InitializeComponent()
		{
			config = view.config;
			scriptcompiler = view.scriptcompiler;
			scriptcompilerlabel = view.scriptcompilerlabel;
			mapslist = view.mapslist;
			mapslist.ItemsSource = mapnames;
			datalocations = view.datalocations;
			strictpatches = view.strictpatches;
			longtexturenames = view.longtexturenames;
			apply = view.apply;
			cancel = view.cancel;

			apply.Click += apply_Click;
			cancel.Click += cancel_Click;
			config.SelectionChanged += (s, e) => config_SelectedIndexChanged(s, EventArgs.Empty);
			mapslist.SelectionChanged += (s, e) => { if(!updatingmaps) mapslist_ItemSelectionChanged(); };
			mapslist.DoubleTapped += (s, e) => mapslist_DoubleClick(s, EventArgs.Empty);
			this.Shown += OpenMapOptionsForm_Shown;
			FormBorderStyle = FormBorderStyle.Sizable;
		}

		protected override object CreateView() => view;

		private string SelectedMapName => mapslist.SelectedItem as string;

		private GameConfiguration GetGameConfiguration()
		{
			if(config.ItemCount == 0 || config.SelectedIndex < 0) return null;
			return new GameConfiguration((config.SelectedItem as ConfigurationInfo).Configuration);
		}

		// ULDB: the configurations offered (GZDoom/UZDoom only).
		private static IEnumerable<ConfigurationInfo> VisibleConfigs => General.Configs.Where(c => ScopeFilter.IsConfigurationVisible(c.Filename));

		private int IndexOfConfig(ConfigurationInfo info)
		{
			for(int i = 0; i < config.ItemCount; i++) if(config.Items[i] == info) return i;
			return -1;
		}

		private int AddConfig(ConfigurationInfo info)
		{
			config.Items.Add(info);
			return config.ItemCount - 1;
		}

		// This loads the settings and attempt to find a suitable config
		private void LoadSettings()
		{
			string gameconfig;
			int index;

			// Busy
			Cursor.Current = Cursors.WaitCursor;

			// Check if the file exists
			if(!File.Exists(filepathname))
			{
				// WAD file does not exist
				MessageBox.Show(this, "Could not open the WAD file: The file does not exist.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				this.DialogResult = DialogResult.Cancel;
				this.Close();
				return;
			}

			try
			{
				// Open the WAD file
				wadfile = new WAD(filepathname, true);
			}
			catch(Exception)
			{
				// Unable to open WAD file (or its config)
				MessageBox.Show(this, "Could not open the WAD file for reading. Please make sure the file you selected is valid and is not in use by any other application.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
				if(wadfile != null) wadfile.Dispose();
				this.DialogResult = DialogResult.Cancel;
				this.Close();
				return;
			}

			// Open the Map Settings configuration
			string dbsfile = filepathname.Substring(0, filepathname.Length - 4) + ".dbs";
			if(File.Exists(dbsfile))
				try { mapsettings = new Configuration(dbsfile, true); }
				catch(Exception) { mapsettings = new Configuration(true); }
			else
				mapsettings = new Configuration(true);

			// Check strict patches box, check what game configuration is preferred
			if(options != null)
			{
				strictpatches.IsChecked = options.StrictPatches;
				gameconfig = options.ConfigFile;
			}
			else
			{
				strictpatches.IsChecked = mapsettings.ReadSetting("strictpatches", false);
				gameconfig = mapsettings.ReadSetting("gameconfig", "");
			}

			// ULDB: a map saved with a hidden (non-GZDoom/UZDoom) configuration opens with the closest visible one.
			if(!string.IsNullOrEmpty(gameconfig) && !ScopeFilter.IsConfigurationVisible(gameconfig))
				gameconfig = ScopeFilter.SuggestConfiguration(gameconfig, VisibleConfigs.Select(c => c.Filename));

			//mxd. Fill script compilers list
			foreach(KeyValuePair<string, ScriptConfiguration> group in General.CompiledScriptConfigs)
			{
				if(ScopeFilter.IsScriptCompilerVisible(group.Key)) scriptcompiler.Items.Add(group.Value);
			}

			// Go for all configurations
			foreach(ConfigurationInfo info in VisibleConfigs)
			{
				// Add config name to list
				index = AddConfig(info);

				// Select this item
				if(info.Filename == gameconfig) config.SelectedIndex = index;
			}

			// Still no configuration selected? Find a suitable one: enabled with resources, then
			// disabled with resources, then enabled without, then disabled without (mxd).
			foreach(bool withresources in new[] { true, false })
			{
				foreach(bool enabled in new[] { true, false })
				{
					if(config.SelectedIndex != -1) break;
					foreach(ConfigurationInfo info in VisibleConfigs)
					{
						if(info.Enabled == enabled && (info.Resources.Count > 0) == withresources && MatchConfiguration(info.Configuration, wadfile))
						{
							index = IndexOfConfig(info);
							config.SelectedIndex = (index != -1 ? index : AddConfig(info));
							break;
						}
					}
				}
			}

			// [ZZ] dispose of wadfile
			wadfile.Dispose();

			//mxd. Bail out if still no dice...
			if(config.SelectedIndex == -1 || mapnames.Count == 0)
			{
				General.ShowWarningMessage("Unable to find maps using any game configuration.\nDoes this wad contain any maps at all?..", MessageBoxButtons.OK);
				cancel_Click(this, null);
			}
			else
			{
				datalocations.GameConfiguration = GetGameConfiguration();
			}

			// Done
			Cursor.Current = Cursors.Default;
		}

		// mxd. This matches a WAD file with the specified game configuration
		// by checking if the specific lumps are detected
		private static bool MatchConfiguration(Configuration cfg, WAD wadfile)
		{
			string mapnameformat = cfg.ReadSetting("mapnameformat", "");
			int lumpsrequired = 0;

			// Get the map lump names
			IDictionary maplumpnames = cfg.ReadSetting("maplumpnames", new Hashtable());

			// Count how many required lumps we have to find
			foreach(DictionaryEntry ml in maplumpnames)
			{
				// Ignore the map header (it will not be found because the name is different)
				if(ml.Key.ToString() != MapManager.CONFIG_MAP_HEADER)
				{
					// Read lump setting and count it
					if(cfg.ReadSetting("maplumpnames." + ml.Key + ".required", false))
						lumpsrequired++;
				}
			}

			// Go for all the lumps in the wad
			for(int scanindex = 0; scanindex < (wadfile.Lumps.Count - 1); scanindex++)
			{
				if(MapNameFormatMismatch(mapnameformat, wadfile.Lumps[scanindex].Name)) return false;

				// Make sure this lump is not part of the map.
				if(!maplumpnames.Contains(wadfile.Lumps[scanindex].Name))
				{
					// Reset check
					int lumpsfound = 0;
					int checkoffset = 1;

					// Continue while still within bounds and lumps are still recognized
					while(((scanindex + checkoffset) < wadfile.Lumps.Count) &&
						  maplumpnames.Contains(wadfile.Lumps[scanindex + checkoffset].Name))
					{
						string lumpname = wadfile.Lumps[scanindex + checkoffset].Name;
						//mxd. Lump cannot present in current map format, fail this check
						if(cfg.ReadSetting("maplumpnames." + lumpname + ".forbidden", false))
						{
							lumpsfound = -1;
							break;
						}

						// Count the lump when it is marked as required
						if(cfg.ReadSetting("maplumpnames." + lumpname + ".required", false))
							lumpsfound++;

						// Check the next lump
						checkoffset++;
					}

					// Map found? Let's call it a day :)
					if(lumpsfound >= lumpsrequired) return true;
				}
			}

			return false;
		}

		// Determine if the given map lump name is not accepted by the given game configuration setting.
		private static bool MapNameFormatMismatch(string mapnameformat, string lumpname)
		{
			return (
				mapnameformat == MapManager.CONFIG_MAP_NAME_FORMAT_NO_EPISODE && episodemapregex.IsMatch(lumpname)
			) || (
				mapnameformat == MapManager.CONFIG_MAP_NAME_FORMAT_EPISODE && noepisodemapregex.IsMatch(lumpname)
			);
		}

		// Configuration is selected
		private void config_SelectedIndexChanged(object sender, EventArgs e)
		{
			datalocations.GameConfiguration = GetGameConfiguration();

			// Anything selected?
			if(config.SelectedIndex < 0 || wadfile == null) return;

			// Keep selected name, if any
			string selectedname = SelectedMapName ?? "";

			// [ZZ] This should be a string list so we can do mapnames.Contains
			List<string> foundnames = new List<string>();

			// Get selected configuration info
			ConfigurationInfo ci = (config.SelectedItem as ConfigurationInfo);

			//mxd. Get configuration
			Configuration cfg = ci.Configuration;

			// Get the map lump names
			IDictionary maplumpnames = cfg.ReadSetting("maplumpnames", new Hashtable());

			// Count how many required lumps we have to find
			int lumpsrequired = 0;
			foreach(DictionaryEntry ml in maplumpnames)
			{
				// Ignore the map header (it will not be found because the name is different)
				if(ml.Key.ToString() != MapManager.CONFIG_MAP_HEADER)
				{
					// Read lump setting and count it
					if(cfg.ReadSetting("maplumpnames." + ml.Key + ".required", false))
						lumpsrequired++;
				}
			}

			// Go for all the lumps in the wad
			for(int scanindex = 0; scanindex < (wadfile.Lumps.Count - 1); scanindex++)
			{
				// Make sure this lump is not part of the map.
				if(!maplumpnames.Contains(wadfile.Lumps[scanindex].Name))
				{
					// Reset check
					int lumpsfound = 0;
					int checkoffset = 1;

					// Continue while still within bounds and lumps are still recognized
					while(((scanindex + checkoffset) < wadfile.Lumps.Count) &&
						  maplumpnames.Contains(wadfile.Lumps[scanindex + checkoffset].Name))
					{
						string lumpname = wadfile.Lumps[scanindex + checkoffset].Name;
						//mxd. Lump cannot present in current map format, fail this check
						if(cfg.ReadSetting("maplumpnames." + lumpname + ".forbidden", false))
						{
							lumpsfound = -1;
							break;
						}

						// Count the lump when it is marked as required
						if(cfg.ReadSetting("maplumpnames." + lumpname + ".required", false))
							lumpsfound++;

						// Check the next lump
						checkoffset++;
					}

					// Map found? Then add it to the list
					string mapname = wadfile.Lumps[scanindex].Name;
					if((lumpsfound >= lumpsrequired) && !foundnames.Contains(mapname))
						foundnames.Add(mapname);
				}
			}

			// Clear the list and add the new map names (sorted, as upstream's ListView)
			foundnames.Sort(StringComparer.Ordinal);
			updatingmaps = true;
			mapnames.Clear();
			foreach(string mapname in foundnames) mapnames.Add(mapname);
			updatingmaps = false;

			// Select the previously selected map again, or the first
			if(mapnames.Contains(selectedname)) mapslist.SelectedItem = selectedname;
			else if(mapnames.Count > 0) mapslist.SelectedIndex = 0;

			//mxd. Disable script compiler selector when there are no maps detected using current configuration
			if(mapnames.Count == 0)
			{
				scriptcompiler.IsEnabled = false;
				scriptcompiler.SelectedIndex = -1;
				scriptcompilerlabel.IsEnabled = false;
			}

			// Show configuration resources
			datalocations.FixedResourceLocationList(ci.Resources);

			// Update long texture names checkbox (mxd)
			longtexturenames.IsEnabled = cfg.ReadSetting("longtexturenames", false);
			longtexturenames.IsChecked = (longtexturenames.IsEnabled && options != null && options.UseLongTextureNames);
		}

		// OK clicked
		private void apply_Click(object sender, RoutedEventArgs e)
		{
			// Configuration selected?
			if(config.SelectedIndex == -1)
			{
				MessageBox.Show(this, "Please select a game configuration to use for editing your map.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				config.Focus();
				return;
			}

			//mxd. Script configuration selected?
			if(scriptcompiler.IsEnabled && scriptcompiler.SelectedIndex == -1)
			{
				MessageBox.Show(this, "Please select a script type to use for editing your map.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				scriptcompiler.Focus();
				return;
			}

			// Collect information
			ConfigurationInfo configinfo = (config.SelectedItem as ConfigurationInfo); //mxd
			DataLocationList locations = datalocations.GetResources();

			// Resources are valid? (mxd)
			if(!datalocations.ResourcesAreValid())
			{
				MessageBox.Show(this, "Cannot open map: at least one resource doesn't exist!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				datalocations.Focus();
				return;
			}

			// No map selected?
			if(SelectedMapName == null)
			{
				MessageBox.Show(this, "Please select a map to load for editing.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				mapslist.Focus();
				return;
			}

			//mxd. We cant't deal with this... We just can't...
			if(!configinfo.ValidateMapName(SelectedMapName.ToUpperInvariant()))
			{
				MessageBox.Show(this, "Selected map name conflicts with a lump name defined for current map format.\nPlease rename the map and try again.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				mapslist.Focus();
				return;
			}

			// Check if we should warn the user for missing resources
			if((!wadfile.IsIWAD) && (locations.Count == 0) && (configinfo.Resources.Count == 0))
			{
				if(MessageBox.Show(this, "You are about to load a map without selecting any resources. Textures, flats and " +
										 "sprites may not be shown correctly or may not show up at all. Do you want to continue?", Application.ProductName,
										 MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.No)
				{
					return;
				}
			}

			// Apply changes
			options.ClearResources();
			options.ConfigFile = configinfo.Filename;
			options.CurrentName = SelectedMapName;
			options.StrictPatches = strictpatches.IsChecked == true;
			options.CopyResources(locations);

			//mxd. Store script compiler
			if(scriptcompiler.IsEnabled)
			{
				ScriptConfiguration scriptcfg = scriptcompiler.SelectedItem as ScriptConfiguration;
				foreach(KeyValuePair<string, ScriptConfiguration> group in General.CompiledScriptConfigs)
				{
					if(group.Value == scriptcfg)
					{
						options.ScriptCompiler = group.Key;
						break;
					}
				}
			}

			//mxd. Use long texture names?
			if(longtexturenames.IsEnabled) options.UseLongTextureNames = longtexturenames.IsChecked == true;

			// Hide window
			wadfile.Dispose();
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		// Cancel clicked
		private void cancel_Click(object sender, RoutedEventArgs e)
		{
			// Just hide window
			wadfile?.Dispose();
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		// Window is shown
		private void OpenMapOptionsForm_Shown(object sender, EventArgs e)
		{
			// Load settings
			LoadSettings();

			// Show
			if(options != null)
				datalocations.EditResourceLocationList(options.Resources);
		}

		// Map name doubleclicked
		private void mapslist_DoubleClick(object sender, EventArgs e)
		{
			// Click OK
			if(SelectedMapName != null) apply_Click(apply, null);
		}

		// Map name selected (upstream gets one event per item; this handles deselect and select together)
		private void mapslist_ItemSelectionChanged()
		{
			datalocations.GameConfiguration = GetGameConfiguration();

			DataLocationList locations;
			DataLocationList listedlocations;
			string scriptconfig = string.Empty; //mxd

			// Map previously selected?
			if(!string.IsNullOrEmpty(selectedmapname))
			{
				// Get locations from previous selected map settings
				locations = new DataLocationList(mapsettings, "maps." + selectedmapname + ".resources");
				listedlocations = datalocations.GetResources();

				// Remove data locations that this map has in its config
				foreach(DataLocation dl in locations)
					listedlocations.Remove(dl);

				// Set new data locations
				datalocations.EditResourceLocationList(listedlocations);

				// Done
				selectedmapname = null;
			}

			// Anything selected?
			if(SelectedMapName != null)
			{
				// Get the map name
				selectedmapname = SelectedMapName;
				options = new MapOptions(mapsettings, selectedmapname, longtexturenames.IsEnabled);

				// Get locations from previous selected map settings
				locations = new DataLocationList(mapsettings, "maps." + selectedmapname + ".resources");
				listedlocations = datalocations.GetResources();

				// Add data locations that this map has in its config
				foreach(DataLocation dl in locations)
					if(!listedlocations.Contains(dl)) listedlocations.Add(dl);

				// Set new data locations
				datalocations.EditResourceLocationList(listedlocations);

				//mxd. Select script compiler
				ConfigurationInfo info = config.SelectedItem as ConfigurationInfo;
				if(info != null)
				{
					if(!string.IsNullOrEmpty(options.ScriptCompiler) && General.CompiledScriptConfigs.ContainsKey(options.ScriptCompiler))
						scriptconfig = options.ScriptCompiler;
					else if(!string.IsNullOrEmpty(info.DefaultScriptCompiler) && General.CompiledScriptConfigs.ContainsKey(info.DefaultScriptCompiler))
						scriptconfig = info.DefaultScriptCompiler;
				}
			}

			//mxd. Select proper script compiler
			if(!string.IsNullOrEmpty(scriptconfig))
			{
				scriptcompiler.IsEnabled = true;
				scriptcompiler.SelectedItem = General.CompiledScriptConfigs[scriptconfig];
				scriptcompilerlabel.IsEnabled = true;
			}
			else
			{
				scriptcompiler.IsEnabled = false;
				scriptcompiler.SelectedIndex = -1;
				scriptcompilerlabel.IsEnabled = false;
			}
		}
	}
}
