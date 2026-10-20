// ULDB: Avalonia port of upstream Windows/MapOptionsForm.cs, over the controls of
// MapOptionsView.axaml. Upstream's logic and handler names are kept; differences:
//  - only GZDoom/UZDoom configurations and their script compilers are offered
//    (ULDB.Scope.ScopeFilter, docs/scope-hidden.md)
//  - the controls are Avalonia's (IsChecked, IsEnabled, ItemCount); the level name filter
//    is applied as the text is typed

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using AvTextBlock = Avalonia.Controls.TextBlock;
using AvTextBox = Avalonia.Controls.TextBox;
using AvRun = Avalonia.Controls.Documents.Run;

namespace CodeImp.DoomBuilder.Windows
{
	internal partial class MapOptionsForm : DelayedForm
	{
		// Variables
		private readonly MapOptions options;
		private readonly bool newmap;
		private ExternalCommandSettings reloadresourceprecommand;
		private ExternalCommandSettings reloadresourcepostcommand;
		private ExternalCommandSettings testprecommand;
		private ExternalCommandSettings testpostcommand;
		private bool prepostcommandsmodified;
		private readonly int initialformheight;

		// Controls (MapOptionsView.axaml)
		private readonly MapOptionsView view;
		private AvComboBox config, scriptcompiler;
		private AvTextBox levelname;
		private AvRun examplelabel;
		private ResourceListEditor datalocations;
		private AvCheckBox strictpatches, longtexturenames;
		private AvButton apply, cancel, prepostcommands;
		private AvTextBlock scriptcompilerlabel;

		// Properties
		public MapOptions Options { get { return options; } }

		// Constructor
		public MapOptionsForm(MapOptions options, bool newmap)
		{
			this.newmap = newmap;

			// Initialize
			view = new MapOptionsView();
			InitializeComponent();
			this.initialformheight = Height;

			// Keep settings
			this.options = options;

			prepostcommandsmodified = false;
			reloadresourceprecommand = options.ReloadResourcePreCommand;
			reloadresourcepostcommand = options.ReloadResourcePostCommand;
			testprecommand = options.TestPreCommand;
			testpostcommand = options.TestPostCommand;

			//mxd. Add script compilers
			foreach(KeyValuePair<string, ScriptConfiguration> group in General.CompiledScriptConfigs)
			{
				if(ScopeFilter.IsScriptCompilerVisible(group.Key)) scriptcompiler.Items.Add(group.Value);
			}

			//mxd. Go for all enabled configurations
			List<ConfigurationInfo> visible = General.Configs.Where(c => ScopeFilter.IsConfigurationVisible(c.Filename)).ToList();
			foreach(ConfigurationInfo info in visible)
			{
				//mxd. No disabled configs here
				if(!info.Enabled) continue;

				// Add config name to list
				int index = AddConfig(info);

				//mxd
				if(newmap && !string.IsNullOrEmpty(General.Settings.LastUsedConfigName) && info.Name == General.Settings.LastUsedConfigName)
				{
					// Select this item
					config.SelectedIndex = index;
				}
				// Is this configuration currently selected?
				else if(string.Compare(info.Filename, options.ConfigFile, true) == 0)
				{
					// Select this item
					config.SelectedIndex = index;
				}
			}

			//mxd. No dice? Check disabled ones
			if(config.SelectedIndex == -1)
			{
				foreach(ConfigurationInfo info in visible)
				{
					//No enabled configs here
					if(info.Enabled) continue;

					if((newmap && !string.IsNullOrEmpty(General.Settings.LastUsedConfigName) && info.Name == General.Settings.LastUsedConfigName) ||
						string.Compare(info.Filename, options.ConfigFile, true) == 0)
					{
						//Add and select this item
						config.SelectedIndex = AddConfig(info);
						break;
					}
				}
			}

			//mxd. Still better than nothing :)
			if(config.SelectedIndex == -1 && config.ItemCount > 0) config.SelectedIndex = 0;

			//mxd
			if(General.Map != null) datalocations.StartPath = General.Map.FilePathName;

			// Set the level name
			if(!string.IsNullOrEmpty(options.CurrentName)) levelname.Text = options.CurrentName;  //mxd

			// Set strict patches loading
			strictpatches.IsChecked = options.StrictPatches;

			// Fill the resources list
			datalocations.IsMapControl = true;
			datalocations.GameConfiguration = GetGameConfiguration();
			datalocations.EditResourceLocationList(options.Resources);
		}

		// ULDB: takes the controls from the view and connects upstream's handlers.
		private void InitializeComponent()
		{
			config = view.config;
			scriptcompiler = view.scriptcompiler;
			scriptcompilerlabel = view.scriptcompilerlabel;
			levelname = view.levelname;
			levelname.Text = "";
			examplelabel = view.examplelabel;
			datalocations = view.datalocations;
			strictpatches = view.strictpatches;
			longtexturenames = view.longtexturenames;
			apply = view.apply;
			cancel = view.cancel;
			prepostcommands = view.prepostcommands;

			apply.Click += apply_Click;
			cancel.Click += cancel_Click;
			prepostcommands.Click += prepostcommands_Click;
			config.SelectionChanged += (s, e) => config_SelectedIndexChanged(s, EventArgs.Empty);
			datalocations.OnWarningsChanged += datalocations_OnWarningsChanged;

			// KeyPress filter: only characters allowed in a map lump name
			levelname.TextChanged += (s, e) =>
			{
				string allowed = Lump.MAP_LUMP_NAME_CHARS + Lump.MAP_LUMP_NAME_CHARS.ToLowerInvariant();
				string filtered = new string(levelname.Text.Where(c => allowed.IndexOf(c) != -1).ToArray());
				if(filtered != levelname.Text) levelname.Text = filtered;
			};
			FormBorderStyle = FormBorderStyle.Sizable;
		}

		protected override object CreateView() => view;

		private int AddConfig(ConfigurationInfo info)
		{
			config.Items.Add(info);
			return config.ItemCount - 1;
		}

		private GameConfiguration GetGameConfiguration()
		{
			if(config.ItemCount == 0 || config.SelectedIndex < 0) return null;

			return new GameConfiguration((config.SelectedItem as ConfigurationInfo).Configuration);
		}

		// OK clicked
		private void apply_Click(object sender, EventArgs e)
		{
			// Configuration selected?
			if(config.SelectedIndex == -1)
			{
				// Select a configuration!
				MessageBox.Show(this, "Please select a game configuration to use for editing your map.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				config.Focus();
				return;
			}

			//mxd. Script configuration selected?
			if(scriptcompiler.IsEnabled && scriptcompiler.SelectedIndex == -1)
			{
				// Select a configuration!
				MessageBox.Show(this, "Please select a script type to use for editing your map.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				scriptcompiler.Focus();
				return;
			}
			
			// Level name empty?
			if(levelname.Text.Length == 0)
			{
				// Enter a level name!
				MessageBox.Show(this, "Please enter a level name for your map.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				levelname.Focus();
				return;
			}

			// Collect information
			ConfigurationInfo configinfo = config.SelectedItem as ConfigurationInfo; //mxd
			DataLocationList locations = datalocations.GetResources();

			//mxd. Level name will fuck things up horribly?
			if(!configinfo.ValidateMapName(levelname.Text.ToUpperInvariant())) 
			{
				// Enter a different level name!
				MessageBox.Show(this, "Chosen map name conflicts with a lump name defined for current map format.\n", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				levelname.Focus();
				return;
			}

			// Resources are valid? (mxd)
			if(!datalocations.ResourcesAreValid()) 
			{
				MessageBox.Show(this, "Cannot " + (newmap ? "create map" : "change map settings") + ": at least one resource doesn't exist!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				datalocations.Focus();
				return;
			}

			// Save the last used config when creating a new map
			if (newmap)
			{
				General.Settings.LastUsedConfigName = configinfo.Name;
			}
			
			// Next checks are only for maps that are already opened
			if(!newmap)
			{
				// Now we check if the map name the user has given does already exist in the source WAD file
				// We have to warn the user about that, because it would create a level name conflict in the WAD

				// Level name changed and the map exists in a source wad?
				if((levelname.Text != options.CurrentName) && (General.Map != null) &&
				   (!string.IsNullOrEmpty(General.Map.FilePathName)) && File.Exists(General.Map.FilePathName))
				{
					// Open the source wad file to check for conflicting name
					WAD sourcewad = new WAD(General.Map.FilePathName, true);
					bool conflictingname = (sourcewad.FindLumpIndex(levelname.Text) > -1);
					sourcewad.Dispose();

					// Names conflict?
					if(conflictingname)
					{
						// Show warning!
						if(General.ShowWarningMessage("The map name \"" + levelname.Text + "\" is already in use by another map or data lump in the source WAD file. Saving your map with this name will cause conflicting data lumps in the WAD file. Do you want to continue?", MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2) == DialogResult.No)
						{
							return;
						}
					}
				}

				//mxd. If the map was never saved and it's name was changed, update filename
				if((levelname.Text != options.CurrentName) && (General.Map != null) && (string.IsNullOrEmpty(General.Map.FilePathName)))
				{
					General.Map.FileTitle = levelname.Text + ".wad";
				}

				// When the user changed the configuration to one that has a different read/write interface,
				// we have to warn the user that the map may not be compatible.
				
				// Configuration changed?
				if((options.ConfigFile != "") && (configinfo.Filename != options.ConfigFile))
				{
					// Check if the config uses a different IO interface
					if(configinfo.Configuration.ReadSetting("formatinterface", "") != General.Map.Config.FormatInterface)
					{
						// Warn the user about IO interface change
						if(General.ShowWarningMessage("The game configuration you selected uses a different file format than your current map. Because your map was not designed for this format it may cause the map to work incorrectly in the game. Do you want to continue?", MessageBoxButtons.YesNo, MessageBoxDefaultButton.Button2) == DialogResult.No)
						{
							// Reset to old configuration
							for(int i = 0; i < config.ItemCount; i++)
							{
								// Is this configuration the old config?
								if(string.Compare((config.Items[i] as ConfigurationInfo).Filename, options.ConfigFile, true) == 0)
								{
									// Select this item
									config.SelectedIndex = i;
									datalocations.GameConfiguration = GetGameConfiguration();
								}
							}
							return;
						} 

						//mxd. Otherwise map data won't be saved if a user decides to save the map right after converting to new map format
						General.Map.IsChanged = true;
					}
				}
			}
			
			// Apply changes
			options.ClearResources();
			options.ConfigFile = (config.SelectedItem as ConfigurationInfo).Filename; //mxd
			options.CurrentName = levelname.Text.Trim().ToUpper();
			options.StrictPatches = strictpatches.IsChecked == true;
			options.CopyResources(datalocations.GetResources());

			// Only store the pre and post commands in the map options if they were actually changed (i.e. the user pressed the OK button the the dialog)
			if (prepostcommandsmodified)
			{
				options.ReloadResourcePreCommand = reloadresourceprecommand;
				options.ReloadResourcePostCommand = reloadresourcepostcommand;
				options.TestPreCommand = testprecommand;
				options.TestPostCommand = testpostcommand;
			}

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

			//options.ReloadResourcePreCommand = reloadresourceprecmd.Text;

			// Hide window
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		// Cancel clicked
		private void cancel_Click(object sender, EventArgs e)
		{
			// Just hide window
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		// Game configuration chosen
		private void config_SelectedIndexChanged(object sender, EventArgs e)
		{
			// Anything selected?
			if(config.SelectedIndex < 0) return;
				
			// Get the info
			ConfigurationInfo info = config.SelectedItem as ConfigurationInfo;
			if(info == null) return; //mxd. Some boilerplate

			// No lump name in the name field?
			if(newmap || levelname.Text.Trim().Length == 0) 
			{
				// Get default lump name from configuration
				levelname.Text = info.DefaultLumpName;
			}
			examplelabel.Text = info.DefaultLumpName; //mxd

			//mxd. Select script compiler
			string scriptconfig = string.Empty;
			if(!string.IsNullOrEmpty(options.ScriptCompiler) && General.CompiledScriptConfigs.ContainsKey(options.ScriptCompiler))
			{
				scriptconfig = options.ScriptCompiler;
			}
			else if(!string.IsNullOrEmpty(info.DefaultScriptCompiler) && General.CompiledScriptConfigs.ContainsKey(info.DefaultScriptCompiler))
			{
				scriptconfig = info.DefaultScriptCompiler;
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

			// Show resources
			datalocations.GameConfiguration = GetGameConfiguration();
			datalocations.FixedResourceLocationList(info.Resources);

			// Update long texture names checkbox (mxd)
			longtexturenames.IsEnabled = info.Configuration.ReadSetting("longtexturenames", false);
			longtexturenames.IsChecked = longtexturenames.IsEnabled && options.UseLongTextureNames;
		}

		private void prepostcommands_Click(object sender, EventArgs e)
		{
			PreAndPostCommandsForm papcf = new PreAndPostCommandsForm(reloadresourceprecommand, reloadresourcepostcommand, testprecommand, testpostcommand);
			papcf.ShowDialog();

			if (papcf.DialogResult == DialogResult.OK)
			{
				reloadresourceprecommand = papcf.GetReloadResourcePreCommand();
				reloadresourcepostcommand = papcf.GetReloadResourcePostCommand();
				testprecommand = papcf.GetTestPreCommand();
				testpostcommand = papcf.GetTestPostCommand();

				prepostcommandsmodified = true;
			}
		}

        private void datalocations_OnWarningsChanged(int size)
        {
			Height = initialformheight + size;
			Refresh();
		}
    }
}