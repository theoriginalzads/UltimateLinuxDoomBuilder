// ULDB: code-behind for ConfigFormView.axaml, the window of upstream's ConfigForm (model-ported).
// Also the engine presets of the Testing tab: the installed GZDoom/UZDoom list and the launch options
// only edit the model's own controls (application, name, parameters), so upstream's code saves them as usual.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using ULDB.Platform;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ConfigFormView : Window
	{
		private const string CUSTOM = "Custom";

		// The model's controls (the generated fields testapplication etc. are this window's own Avalonia ones)
		private WF.TextBox modelapplication, modelparameters;
		private WF.CheckBox modelcustom;
		private List<EngineInstall> installs = new List<EngineInstall>();
		private string chosenfamily;
		private string shownfamily;
		private bool updating, scanned;

		public ConfigFormView()
		{
			InitializeComponent();
		}

		internal ConfigFormView(ConfigForm form) : this()
		{
			// "Use short paths" is Windows 8.3 path mangling and "Use Linux paths" converts Windows/Wine paths:
			// neither means anything for a native Linux build (issue #19), so both stay off and hidden.
			HideField(form, "shortpaths");
			HideField(form, "linuxpaths");
			ModelBinder binder = new ModelBinder(form);
			binder.BindByName(this);

			modelapplication = binder.Field<WF.TextBox>("testapplication");
			modelparameters = binder.Field<WF.TextBox>("testparameters");
			modelcustom = binder.Field<WF.CheckBox>("customparameters");

			presetfamily.ItemsSource = new[] { CUSTOM }.Concat(EnginePresets.Families).ToList();
			presetfamily.SelectionChanged += (s, e) =>
			{
				if(updating) return;
				chosenfamily = presetfamily.SelectedItem as string == CUSTOM ? null : presetfamily.SelectedItem as string;
				Refresh();
			};
			presetinstall.SelectionChanged += (s, e) =>
			{
				if(updating || !(presetinstall.SelectedItem is EngineInstall install)) return;
				// The model's own handlers store the program and name it (ConfigForm.testapplication_TextChanged).
				if(modelapplication.Text != install.Path) modelapplication.Text = install.Path;
				chosenfamily = install.Family;
				Refresh();
			};
			presetrescan.Click += (s, e) => Scan();
			// The window is built before upstream's code puts the current engine in the model's controls
			binder.Custom(Refresh, modelapplication, modelparameters);
			AttachedToVisualTree += (s, e) => { if(!scanned) Scan(); };
			binder.UpdateNow();
			Refresh();
		}

		private void Scan()
		{
			scanned = true;
			presetstatus.Text = "Looking for installed engines...";
			Task.Run(() => Engines.Detect()).ContinueWith(t => Dispatcher.UIThread.Post(() =>
			{
				installs = t.IsCompletedSuccessfully ? t.Result : new List<EngineInstall>();
				Refresh();
			}));
		}

		// Brings the quick setup in line with the model's program and parameters.
		private void Refresh()
		{
			if(modelapplication == null || updating) return;
			updating = true;
			try
			{
				string path = modelapplication.Text ?? "";

				// The list: installed engines, and the current program if it isn't one of them
				List<EngineInstall> items = installs.ToList();
				if(path.Length > 0 && items.All(i => i.Path != path))
					items.Insert(0, new EngineInstall { Name = Engines.DisplayName(path) + " (current)", Path = path, Family = Engines.FamilyOf(path) });
				if(!SameItems(items)) presetinstall.ItemsSource = items;
				presetinstall.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(EngineInstall.Name));
				presetinstall.SelectedItem = items.FirstOrDefault(i => i.Path == path);

				string family = Engines.FamilyOf(path) ?? chosenfamily;
				presetfamily.SelectedItem = family ?? CUSTOM;

				presetstatus.Text = installs.Count == 0
					? (scanned ? "No GZDoom or UZDoom found on this system. Use the buttons below to pick a program yourself." : "")
					: (Engines.InFlatpak ? "ULDB runs as a Flatpak: engines are started on your system through flatpak-spawn." : "");

				// The launch options of the engine family
				IReadOnlyList<LaunchOption> options = EnginePresets.OptionsFor(family);
				presetoptionspanel.IsVisible = options.Count > 0;
				if(family != shownfamily)
				{
					shownfamily = family;
					presetoptions.Children.Clear();
					foreach(LaunchOption option in options)
					{
						CheckBox box = new CheckBox { Content = option.Label, Margin = new Avalonia.Thickness(0, 0, 18, 0), Tag = option };
						ToolTip.SetTip(box, option.Tip + "  (" + option.Arguments + ")");
						box.IsCheckedChanged += (s, e) =>
						{
							if(updating) return;
							// Editing the parameters means custom parameters (the default ones come from the game configuration)
							if(!modelcustom.Checked) modelcustom.Checked = true;
							modelparameters.Text = EnginePresets.Set(modelparameters.Text, option, box.IsChecked == true);
						};
						presetoptions.Children.Add(box);
					}
				}
				foreach(CheckBox box in presetoptions.Children.OfType<CheckBox>())
					box.IsChecked = EnginePresets.IsSet(modelparameters.Text, (LaunchOption)box.Tag);
			}
			finally { updating = false; }
		}

		private bool SameItems(List<EngineInstall> items)
		{
			IEnumerable<object> current = presetinstall.ItemsSource as IEnumerable<object>;
			return current != null && current.Cast<EngineInstall>().Select(i => i.Path).SequenceEqual(items.Select(i => i.Path));
		}

		private static void HideField(ConfigForm form, string name)
		{
			(typeof(ConfigForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(form) as System.Windows.Forms.Control)?.Hide();
		}
	}
}
