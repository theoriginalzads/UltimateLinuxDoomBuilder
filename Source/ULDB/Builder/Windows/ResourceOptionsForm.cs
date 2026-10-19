// ULDB: Avalonia port of upstream Windows/ResourceOptionsForm.cs. Upstream's logic and
// handler names over the controls of ResourceOptionsView.axaml. CheckRequiredArchives is
// copied unchanged.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Avalonia.Interactivity;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.ZDoom;
using AvCheckBox = Avalonia.Controls.CheckBox;
using AvTextBox = Avalonia.Controls.TextBox;
using AvButton = Avalonia.Controls.Button;

namespace CodeImp.DoomBuilder.Windows
{
	internal partial class ResourceOptionsForm : DelayedForm
	{
		// Variables
		private DataLocation res;
		private string startPath;
		private List<string> requiredarchives;

		// Controls (ResourceOptionsView.axaml)
		private readonly ResourceOptionsView view;
		private Avalonia.Controls.TabControl tabs;
		private AvTextBox wadlocation, dirlocation, pk3location;
		private AvCheckBox strictpatches, dir_textures, dir_flats, notfortesting;
		private AvButton apply, cancel, browsewad, browsedir, browsepk3;
		private Avalonia.Controls.Control checkingloader;

		// Properties
		public DataLocation ResourceLocation { get { return res; } }
		public GameConfiguration GameConfiguration { get; set; }

		private bool _ischeckingrequiredarchives = false;
		private bool IsCheckingRequiredArchives
		{
			get { return _ischeckingrequiredarchives; }
			set
			{
				apply.IsEnabled = !value;
				cancel.IsEnabled = !value;
				notfortesting.IsEnabled = !value;
				dir_textures.IsEnabled = !value;
				dir_flats.IsEnabled = !value;
				ControlBox = !value;
				checkingloader.IsVisible = value;
				_ischeckingrequiredarchives = value;
			}
		}

		// Constructor
		public ResourceOptionsForm(DataLocation settings, string caption, string startPath) //mxd. added startPath
		{
			// Initialize
			view = new ResourceOptionsView();
			InitializeComponent();

			// Set caption
			this.Text = caption;

			this.requiredarchives = new List<string>();

			// Apply settings from ResourceLocation
			this.res = settings;
			switch(res.type)
			{
				// Setup for WAD File
				case DataLocation.RESOURCE_WAD:
					wadlocation.Text = res.location;
					strictpatches.IsChecked = res.option1;
					break;

				// Setup for Directory
				case DataLocation.RESOURCE_DIRECTORY:
					dirlocation.Text = res.location;
					dir_textures.IsChecked = res.option1;
					dir_flats.IsChecked = res.option2;
					break;

				// Setup for PK3 File
				case DataLocation.RESOURCE_PK3:
					pk3location.Text = res.location;
					break;
			}

			// Select appropriate tab
			tabs.SelectedIndex = res.type;

			// Checkbox
			notfortesting.IsChecked = res.notfortesting;

			this.startPath = startPath;
		}

		// ULDB: takes the controls from the view and connects upstream's handlers.
		private void InitializeComponent()
		{
			tabs = view.tabs;
			wadlocation = view.wadlocation;
			dirlocation = view.dirlocation;
			pk3location = view.pk3location;
			strictpatches = view.strictpatches;
			dir_textures = view.dir_textures;
			dir_flats = view.dir_flats;
			notfortesting = view.notfortesting;
			apply = view.apply;
			cancel = view.cancel;
			browsewad = view.browsewad;
			browsedir = view.browsedir;
			browsepk3 = view.browsepk3;
			checkingloader = view.checkingloader;

			apply.Click += apply_Click;
			cancel.Click += cancel_Click;
			browsewad.Click += browsewad_Click;
			browsedir.Click += browsedir_Click;
			browsepk3.Click += browsepk3_Click;
			view.pk3link.Click += (s, e) => General.OpenWebsite("http://www.zdoom.org/wiki/Using_ZIPs_as_WAD_replacement");
			view.pk3_directorylink.Click += (s, e) => General.OpenWebsite("http://www.zdoom.org/wiki/Using_ZIPs_as_WAD_replacement");
			FormBorderStyle = FormBorderStyle.FixedDialog;
		}

		protected override object CreateView() => view;

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			if(IsCheckingRequiredArchives) e.Cancel = true;
		}

		public static List<string> CheckRequiredArchives(GameConfiguration config, DataLocation loc, CancellationToken token)
        {
#if DEBUG
			General.WriteLogLine(string.Format("CheckRequiredArchives (Config = {0})", config?.Name));
#endif

			if (config == null)
				return new List<string>();

			try
			{
				DataReader dr = null;
				List<string> requiredarchives = new List<string>();
				HashSet<string> classes = null;

				switch (loc.type)
				{
					case DataLocation.RESOURCE_WAD:
						dr = new WADReader(loc, config, true) { Silent = true };
						break;

					case DataLocation.RESOURCE_DIRECTORY:
						dr = new DirectoryReader(loc, config, true) { Silent = true };
						break;

					case DataLocation.RESOURCE_PK3:
						dr = new PK3Reader(loc, config, true) { Silent = true };
						break;
				}

				token.ThrowIfCancellationRequested();

				foreach (var arc in config.RequiredArchives)
				{
					bool found = true;

					token.ThrowIfCancellationRequested();

					foreach (RequiredArchiveEntry e in arc.Entries)
					{
						token.ThrowIfCancellationRequested();

						if (e.Class != null)
						{
							if (classes == null)
                            {
								classes = new HashSet<string>();

								// load ZScript
								var zscript = new ZScriptParser {
									NoWarnings = true,
									OnInclude = (parser, location) => {
										IEnumerable<TextResourceData> includeStreams = dr.GetZScriptData(location, true);
										foreach (TextResourceData data in includeStreams)
										{
											// Parse this data
											parser.Parse(data, false);

											//mxd. DECORATE lumps are interdepandable. Can't carry on...
											if (parser.HasError)
												break;
										}
									}
								};

								foreach (TextResourceData data in dr.GetZScriptData("ZSCRIPT", false))
								{
									// Parse the data
									data.Stream.Seek(0, SeekOrigin.Begin);
									zscript.Parse(data, true);

									if (zscript.HasError)
										break;

									foreach (string cls in zscript.LastClasses)
										classes.Add(cls.ToLowerInvariant());
								}

#if DEBUG
								if (zscript.HasError)
									General.WriteLogLine(string.Format("CRA({0}): ZScript error: {1}", loc.location, zscript.ErrorDescription));
#endif

								// load DECORATE
								var decorate = new DecorateParser(zscript.AllActorsByClass) {
									NoWarnings = true,
									OnInclude = (parser, location) => {
										IEnumerable<TextResourceData> includeStreams = dr.GetDecorateData(location, true);
										foreach (TextResourceData data in includeStreams)
										{
											// Parse this data
											parser.Parse(data, false);

											//mxd. DECORATE lumps are interdepandable. Can't carry on...
											if (parser.HasError)
												break;
										}
									}
								};

								foreach (TextResourceData data in dr.GetDecorateData("DECORATE", false))
								{
									// Parse the data
									data.Stream.Seek(0, SeekOrigin.Begin);
									decorate.Parse(data, true);

									if (decorate.HasError)
										break;

									foreach (string cls in decorate.LastClasses)
										classes.Add(cls.ToLowerInvariant());
								}

#if DEBUG
								if (decorate.HasError)
									General.WriteLogLine(string.Format("CRA({0}): DECORATE error: {1}", loc.location, decorate.ErrorDescription));
#endif
							}

							if (!classes.Contains(e.Class.ToLowerInvariant()))
                            {
#if DEBUG
								General.WriteLogLine(string.Format("CRA({2}): Does not contain class: {1} <- {0}", string.Join(",", classes), e.Class, loc.location));
#endif
								found = false;
								break;
                            }
						}

						if (e.Lump != null && !dr.FileExists(e.Lump))
						{
#if DEBUG
							General.WriteLogLine(string.Format("CRA({1}): Does not contain lump: {0}", e.Lump, loc.location));
#endif
							found = false;
							break;
						}
					}

					if (found)
                    {
						requiredarchives.Add(arc.ID);
                    }
				}

				dr.Dispose();

				return requiredarchives;
			}
			catch (OperationCanceledException)
            {
				throw;
            }
			catch (Exception e)
			{
				General.WriteLogLine(e.ToString());
				return new List<string>();
			}
		}

		private List<string> RunCheckRequiredArchives(DataLocation location)
		{
			// thanks ms for making this a struct
			CancellationTokenSource dummySource = new CancellationTokenSource();
			List<string> output = CheckRequiredArchives(GameConfiguration, location, dummySource.Token);
			dummySource.Dispose();
			return output;
		}

		private async void StartRequiredArchivesCheck()
		{
			IsCheckingRequiredArchives = true;

			// ULDB: read the controls on the UI thread; the check itself runs in the background.
			DataLocation location = ToDataLocation();
			try
			{
				requiredarchives = await Task.Run(() => RunCheckRequiredArchives(location));
			}
			catch
			{
				requiredarchives = new List<string>();
			}

			ApplyDefaultRequiredArchivesSetting();

			IsCheckingRequiredArchives = false;
		}

		private void ApplyDefaultRequiredArchivesSetting()
		{
			dir_textures.IsChecked = false;
			dir_flats.IsChecked = false;
			notfortesting.IsChecked = false;
			if(GameConfiguration == null) return;

			// if any of the detected required archives implies "not for testing" - disable it by default
			foreach(var arc in GameConfiguration.RequiredArchives)
			{
				if(requiredarchives.Contains(arc.ID) && arc.ExcludeFromTesting)
					notfortesting.IsChecked = true;
			}
		}

		private DataLocation ToDataLocation()
		{
			DataLocation res = new DataLocation();
			res.location = "";
			res.requiredarchives = requiredarchives;

			// Apply settings to ResourceLocation
			switch(tabs.SelectedIndex)
			{
				// Setup WAD File
				case DataLocation.RESOURCE_WAD:
					if(string.IsNullOrEmpty(wadlocation.Text) || !File.Exists(wadlocation.Text)) break;
					res.type = DataLocation.RESOURCE_WAD;
					res.location = wadlocation.Text;
					res.option1 = strictpatches.IsChecked == true;
					res.option2 = false;
					res.notfortesting = notfortesting.IsChecked == true;
					break;

				// Setup Directory
				case DataLocation.RESOURCE_DIRECTORY:
					if(string.IsNullOrEmpty(dirlocation.Text) || !Directory.Exists(dirlocation.Text)) break;
					res.type = DataLocation.RESOURCE_DIRECTORY;
					res.location = dirlocation.Text;
					res.option1 = dir_textures.IsChecked == true;
					res.option2 = dir_flats.IsChecked == true;
					res.notfortesting = notfortesting.IsChecked == true;
					break;

				// Setup PK3 File
				case DataLocation.RESOURCE_PK3:
					if(string.IsNullOrEmpty(pk3location.Text) || !File.Exists(pk3location.Text)) break;
					res.type = DataLocation.RESOURCE_PK3;
					res.location = pk3location.Text;
					res.option1 = false;
					res.option2 = false;
					res.notfortesting = notfortesting.IsChecked == true;
					break;
			}

			return res;
		}

		// OK clicked
		private void apply_Click(object sender, RoutedEventArgs e)
		{
			res = ToDataLocation();
			if(res.location == "")
			{
				switch(tabs.SelectedIndex)
				{
					case DataLocation.RESOURCE_WAD:
						MessageBox.Show(this, "Please select a valid WAD File resource.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
						break;

					case DataLocation.RESOURCE_PK3:
						MessageBox.Show(this, "Please select a valid PK3, PKE, or PK7 File resource.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
						break;

					case DataLocation.RESOURCE_DIRECTORY:
						MessageBox.Show(this, "Please select a valid directory resource.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
						break;
				}
			}
			else
			{
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
		}

		// Cancel clicked
		private void cancel_Click(object sender, RoutedEventArgs e)
		{
			// Just hide
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		// The folder the file pickers start in: the current choice, else the map's folder (mxd).
		private string StartDirectory(string current)
		{
			if(!string.IsNullOrEmpty(current))
			{
				string dir = Directory.Exists(current) ? current : Path.GetDirectoryName(current);
				if(!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) return dir;
			}
			if(!string.IsNullOrEmpty(startPath))
			{
				string startDir = Path.GetDirectoryName(startPath);
				if(Directory.Exists(startDir)) return startDir;
			}
			return null;
		}

		// Browse WAD File clicked
		private void browsewad_Click(object sender, RoutedEventArgs e)
		{
			using(OpenFileDialog wadfiledialog = new OpenFileDialog())
			{
				wadfiledialog.Title = "Browse WAD File";
				wadfiledialog.Filter = "Doom WAD Files (*.wad;*.iwad)|*.wad;*.WAD;*.Wad;*.iwad;*.IWAD|All files|*";
				wadfiledialog.InitialDirectory = StartDirectory(wadlocation.Text);
				if(wadfiledialog.ShowDialog(this) == DialogResult.OK)
				{
					// Use this file
					wadlocation.Text = wadfiledialog.FileName;
					StartRequiredArchivesCheck();
				}
			}
		}

		// Browse Directory clicked
		private void browsedir_Click(object sender, RoutedEventArgs e)
		{
			using(FolderBrowserDialog dirdialog = new FolderBrowserDialog())
			{
				dirdialog.Description = "Select Resource Folder";
				dirdialog.SelectedPath = StartDirectory(dirlocation.Text);
				if(dirdialog.ShowDialog(this) == DialogResult.OK)
				{
					// Use this directory
					dirlocation.Text = dirdialog.SelectedPath;
					StartRequiredArchivesCheck();
				}
			}
		}

		// Browse PK3 File clicked
		private void browsepk3_Click(object sender, RoutedEventArgs e)
		{
			using(OpenFileDialog pk3filedialog = new OpenFileDialog())
			{
				pk3filedialog.Title = "Browse PK3, PKE, or PK7 File";
				pk3filedialog.Filter = "Doom PK3/PKE/PK7 Files (*.pk3;*.pke;*.pk7;*.ipk3;*.ipk7)|*.pk3;*.PK3;*.pke;*.PKE;*.pk7;*.PK7;*.ipk3;*.IPK3;*.ipk7;*.IPK7|All files|*";
				pk3filedialog.InitialDirectory = StartDirectory(pk3location.Text);
				if(pk3filedialog.ShowDialog(this) == DialogResult.OK)
				{
					// Use this file
					pk3location.Text = pk3filedialog.FileName;
					StartRequiredArchivesCheck();
				}
			}
		}
	}
}
