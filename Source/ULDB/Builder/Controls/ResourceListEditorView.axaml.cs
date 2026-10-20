// ULDB: Avalonia port of upstream Controls/ResourceListEditorView.cs. Same public API and
// method names as upstream, so callers (and future upstream changes) map across.
// Differences: items are ResourceListEditorView.Item objects instead of ListViewItems; icons
// are vector glyphs (upstream's ImageList can't be loaded on Linux); reordering is by
// dragging an item onto another.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Windows;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ResourceListEditorView : UserControl
	{
		#region ================== Item

		/// <summary>One row: a resource location; fixed rows come from the game configuration.</summary>
		public sealed class Item
		{
			private static readonly Avalonia.Media.Geometry FolderIcon = Avalonia.Media.Geometry.Parse("M1,3 H6 L8,5 H15 V14 H1 Z");
			private static readonly Avalonia.Media.Geometry FileIcon = Avalonia.Media.Geometry.Parse("M3,1 H10 L13,4 V15 H3 Z");
			private static readonly Avalonia.Media.Geometry ArchiveIcon = Avalonia.Media.Geometry.Parse("M2,2 H14 V14 H2 Z M7,3 V4 H9 V3 Z M7,5 V6 H9 V5 Z M7,7 V9 H9 V7 Z");
			private static readonly Avalonia.Media.Geometry LoaderIcon = Avalonia.Media.Geometry.Parse("M8,2 A6,6 0 1 1 2,8 H4 A4,4 0 1 0 8,4 Z");

			public Item(DataLocation location, bool isfixed, bool loading)
			{
				Tag = location;
				Fixed = isfixed;
				Loading = loading;
			}

			public DataLocation Tag { get; }
			public bool Fixed { get; }
			public bool Loading { get; }
			public string Location => Tag.location;
			public bool Valid => Tag.IsValid();

			public Avalonia.Media.Geometry Icon
			{
				get
				{
					if(Loading) return LoaderIcon;
					switch(Tag.type)
					{
						case DataLocation.RESOURCE_DIRECTORY: return FolderIcon;
						case DataLocation.RESOURCE_PK3: return ArchiveIcon;
						default: return FileIcon;
					}
				}
			}

			public double IconOpacity => Fixed ? 0.45 : 0.85;
			public double TextOpacity => Fixed ? 0.55 : 1.0;
			public string Tooltip => !Valid ? "This resource doesn't exist" : Fixed ? "From the game configuration (can't be changed here)" : null;

			public Item With(DataLocation location) => new Item(location, Fixed, Loading);
			public Item WithLoading(bool loading) => new Item(Tag, Fixed, loading);
		}

		#endregion

		#region ================== Delegates / Events

		public delegate void ContentChanged();
		public delegate void WarningsChanged(int size);
		public event ContentChanged OnContentChanged;
		public event WarningsChanged OnWarningsChanged;
		public string StartPath; //mxd
		public bool IsMapControl = false;

		#endregion

		#region ================== Variables

		private readonly ObservableCollection<Item> items = new ObservableCollection<Item>();
		private readonly DataLocationList copiedresources; //mxd
		private readonly Dictionary<string, CancellationTokenSource> loadingrequiredarchives;
		private readonly List<string> warnings;

		// Reordering by drag
		private Item dragitem;
		private Point dragstart;
		private bool dragging;

		#endregion

		#region ================== Properties

		public System.Drawing.Point DialogOffset { get; set; } = new System.Drawing.Point(40, 20);
		public GameConfiguration GameConfiguration { get; set; }

		#endregion

		#region ================== Constructor

		public ResourceListEditorView()
		{
			InitializeComponent();

			loadingrequiredarchives = new Dictionary<string, CancellationTokenSource>();
			warnings = new List<string>();
			copiedresources = new DataLocationList(); //mxd
			resourceitems.ItemsSource = items;

			// Handlers (upstream names)
			addresource.Click += addresource_Click;
			editresource.Click += editresource_Click;
			deleteresources.Click += deleteresources_Click;
			resourceitems.SelectionChanged += resourceitems_ItemSelectionChanged;
			resourceitems.DoubleTapped += resourceitems_DoubleClick;
			resourceitems.KeyUp += resourceitems_KeyUp;
			copyresources.Click += copyresources_Click;
			cutresources.Click += cutresources_Click;
			pasteresources.Click += pasteresources_Click;
			replaceresources.Click += replaceresources_Click;
			removeresources.Click += removeresources_Click;
			copypastemenu.Opening += copypastemenu_Opening;
			DragDrop.AddDropHandler(resourceitems, resourceitems_DragDrop);
			DragDrop.AddDragOverHandler(resourceitems, resourceitems_DragOver);

			// Reorder by dragging items within the list.
			resourceitems.AddHandler(PointerPressedEvent, resourceitems_PointerPressed, RoutingStrategies.Tunnel);
			resourceitems.AddHandler(PointerMovedEvent, resourceitems_PointerMoved, RoutingStrategies.Tunnel);
			resourceitems.AddHandler(PointerReleasedEvent, resourceitems_PointerReleased, RoutingStrategies.Tunnel);

			// Shortcuts shown in the context menu, as upstream (mxd)
			if(General.Actions != null)
			{
				copyresources.InputGesture = Gesture("builder_copyselection");
				cutresources.InputGesture = Gesture("builder_cutselection");
				pasteresources.InputGesture = Gesture("builder_pasteselection");
				replaceresources.InputGesture = Gesture("builder_pasteselectionspecial");
				removeresources.InputGesture = Gesture("builder_deleteitem");
			}
		}

		private static KeyGesture Gesture(string action)
		{
			string desc = Actions.Action.GetShortcutKeyDesc(General.Actions.GetActionByName(action).ShortcutKey);
			try { return string.IsNullOrEmpty(desc) ? null : KeyGesture.Parse(desc.Replace(" ", "")); }
			catch(Exception) { return null; }
		}

		private int ShortcutKey(string action) => General.Actions?.GetActionByName(action)?.ShortcutKey ?? 0;

		#endregion

		#region ================== Methods

		private List<string> RunCheckRequiredArchives(DataLocation loc, CancellationToken token)
		{
			return ResourceOptionsForm.CheckRequiredArchives(GameConfiguration, loc, token);
		}

		private async void StartRequiredArchivesCheck(string location)
		{
			if(GameConfiguration == null) return;

			DataLocation loc = new DataLocation();
			bool found = false;
			foreach(Item item in items)
			{
				if(item.Tag.location == location)
				{
					loc = item.Tag;
					found = true;
					break;
				}
			}
			if(!found) return;

			CancellationTokenSource cancellation = new CancellationTokenSource();
			General.WriteLogLine(string.Format("Resource check started for: {0}", loc.location));
			loadingrequiredarchives.Add(location, cancellation);
			RefreshLoading();

			try
			{
				loc.requiredarchives = await Task.Run(() => RunCheckRequiredArchives(loc, cancellation.Token));

				// in case of dir, option1/2 should be erased
				if(loc.type == DataLocation.RESOURCE_DIRECTORY)
					loc.option1 = loc.option2 = false;
				// check if it has to be force-excluded from testing
				foreach(var arc in GameConfiguration.RequiredArchives)
				{
					if(loc.requiredarchives.Contains(arc.ID) && arc.ExcludeFromTesting)
						loc.notfortesting = true;
				}

				for(int i = 0; i < items.Count; i++)
				{
					if(items[i].Tag.location == location)
					{
						items[i] = items[i].With(loc);
						OnContentChanged?.Invoke();
						break;
					}
				}
			}
			catch(Exception e)
			{
				loc.requiredarchives = new List<string>();
				General.WriteLogLine(e.ToString());
			}

			cancellation.Dispose();

			// Loading might have been canceled outside of here, so make sure that the location is still in the list.
			if(loadingrequiredarchives.ContainsKey(location) && loadingrequiredarchives[location] == cancellation)
			{
				General.WriteLogLine(string.Format("Resource check completed for: {0} (RequiredArchives = {1})", location, string.Join(",", loc.requiredarchives)));
				loadingrequiredarchives.Remove(location);
				RefreshLoading();
			}

			// if nothing is loading, update warnings if any
			if(loadingrequiredarchives.Count == 0)
				UpdateWarnings();
		}

		private void ShowWarning(string text, bool loading)
		{
			TextBlock message = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
			PathIcon icon = new PathIcon
			{
				Data = Avalonia.Media.Geometry.Parse("M8,1 L15,14 H1 Z M7.25,5 V10 H8.75 V5 Z M7.25,11 V12.5 H8.75 V11 Z"),
				Width = 16, Height = 16, Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xA3, 0x17)),
				VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 1, 8, 0),
			};
			DockPanel row = new DockPanel();
			DockPanel.SetDock(icon, Dock.Left);
			row.Children.Add(icon);
			row.Children.Add(message);
			Border box = new Border
			{
				Child = row,
				Padding = new Thickness(8, 6),
				CornerRadius = new CornerRadius(3),
				BorderThickness = new Thickness(1),
				BorderBrush = new SolidColorBrush(Color.FromArgb(0x99, 0xE8, 0xA3, 0x17)),
				Background = new SolidColorBrush(Color.FromArgb(0x22, 0xE8, 0xA3, 0x17)),
			};
			warningspanel.Children.Add(box);
			warnings.Add(text);
		}

		private int GetWarningsHeight() => warnings.Count;

		private void UpdateWarnings()
		{
			int lastH = GetWarningsHeight();
			warningspanel.Children.Clear();
			warnings.Clear();

			List<string> requiredarchives = new List<string>();
			foreach(Item item in items)
			{
				if(item.Tag.requiredarchives != null)
					requiredarchives.AddRange(item.Tag.requiredarchives);
			}

			General.WriteLogLine(string.Format("Archive check: RequiredArchives = {0}", string.Join(",", requiredarchives)));

			// warning 1: you do not have a required file
			if(GameConfiguration != null)
			{
				foreach(RequiredArchive arc in GameConfiguration.RequiredArchives)
				{
					if(!requiredarchives.Contains(arc.ID))
						ShowWarning(string.Format("A resource archive is required for this game configuration, but not present:\n  \"{0}\"\nWithout it, {1} will have severely limited capabilities.", arc.FileName, ULDB.Branding.SHORT_NAME), false);
				}
			}

			// warning 2: map without any resources. this makes sense only on map open dialog and not game configurations dialog
			if(IsMapControl && items.Count == 0)
				ShowWarning("You are about to edit a map without any resources.\nTextures, flats and sprites may not be shown correctly or may not show up at all.", false);

			// warning 3: multiple instances of the same required file
			if(GameConfiguration != null)
			{
				for(int i = 0; i < requiredarchives.Count; i++)
				{
					if(requiredarchives.IndexOf(requiredarchives[i]) != i)
					{
						foreach(RequiredArchive arc in GameConfiguration.RequiredArchives)
						{
							if(arc.ID == requiredarchives[i])
								ShowWarning(string.Format("Required archive was added more than once:\n  \"{0}\"\nThis will most likely not work.", arc.FileName), false);
						}
					}
				}
			}

			int h = GetWarningsHeight();
			if(lastH != h) OnWarningsChanged?.Invoke(h);
		}

		private void RefreshLoading()
		{
			for(int i = 0; i < items.Count; i++)
			{
				bool loading = IsLoading(items[i].Location);
				if(loading != items[i].Loading) items[i] = items[i].WithLoading(loading);
			}
		}

		private void CancelLoading(string location)
		{
			General.WriteLogLine(string.Format("Resource check cancelled for: {0}", location));
			if(loadingrequiredarchives.ContainsKey(location))
			{
				loadingrequiredarchives[location].Cancel();
				loadingrequiredarchives.Remove(location);
			}
		}

		private bool IsLoading(string location) => loadingrequiredarchives.ContainsKey(location);

		// This will show a fixed list
		public void FixedResourceLocationList(DataLocationList list)
		{
			HashSet<DataLocation> currentitems = new HashSet<DataLocation>(); //mxd

			// Remove items unless fixed
			for(int i = items.Count - 1; i >= 0; i--)
			{
				if(items[i].Fixed)
				{
					CancelLoading(items[i].Location);
					items.RemoveAt(i);
				}
				else currentitems.Add(items[i].Tag); //mxd
			}

			// Add the fixed items at the top
			for(int i = list.Count - 1; i >= 0; i--)
			{
				if(currentitems.Contains(list[i])) continue; //mxd
				currentitems.Add(list[i]); //mxd
				items.Insert(0, new Item(list[i], true, IsLoading(list[i].location)));

				// Check if resource has no info about membership in Game configuration's requiredarchives
				if(list[i].requiredarchives == null)
					StartRequiredArchivesCheck(list[i].location);
			}

			if(loadingrequiredarchives.Count == 0)
				UpdateWarnings();
		}

		// This will edit the given list
		public void EditResourceLocationList(DataLocationList list)
		{
			// Remove items unless fixed
			for(int i = items.Count - 1; i >= 0; i--)
			{
				if(!items[i].Fixed)
				{
					CancelLoading(items[i].Location);
					items.RemoveAt(i);
				}
			}

			foreach(DataLocation dl in list) AddItem(dl);

			if(loadingrequiredarchives.Count == 0)
				UpdateWarnings();

			OnContentChanged?.Invoke();
		}

		// This adds a normal item
		private bool AddItem(DataLocation rl)
		{
			//mxd. No duplicates ples
			foreach(Item item in items)
				if(item.Tag.location == rl.location) return false;

			items.Add(new Item(rl, false, IsLoading(rl.location)));

			// Check if resource has no info about membership in Game configuration's requiredarchives
			if(rl.requiredarchives == null)
				StartRequiredArchivesCheck(rl.location);

			return true;
		}

		// Returns a list of the resources
		public DataLocationList GetResources()
		{
			DataLocationList list = new DataLocationList();
			foreach(Item item in items)
			{
				if(!item.Fixed && !list.Contains(item.Tag)) list.Add(item.Tag); //mxd. Duplicates check
			}
			return list;
		}

		//mxd
		public bool ResourcesAreValid()
		{
			foreach(Item item in items)
				if(!item.Tag.IsValid()) return false;
			return true;
		}

		private List<Item> SelectedItems
		{
			get
			{
				List<Item> selected = new List<Item>();
				if(resourceitems.SelectedItems != null) foreach(object o in resourceitems.SelectedItems) if(o is Item i) selected.Add(i);
				return selected;
			}
		}

		private ResourceOptionsForm CreateOptionsForm(DataLocation location, string caption)
		{
			ResourceOptionsForm resoptions = new ResourceOptionsForm(location, caption, StartPath);
			resoptions.GameConfiguration = GameConfiguration;
			return resoptions;
		}

		#endregion

		#region ================== Events

		// Add a resource
		private void addresource_Click(object sender, RoutedEventArgs e)
		{
			ResourceOptionsForm resoptions = CreateOptionsForm(new DataLocation(), "Add Resource");
			if(resoptions.ShowDialog(OwnerForm) == System.Windows.Forms.DialogResult.OK)
			{
				if(!AddItem(resoptions.ResourceLocation))
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Resource already added!"); //mxd
					return; //mxd
				}

				if(loadingrequiredarchives.Count == 0)
					UpdateWarnings();
			}

			OnContentChanged?.Invoke();
		}

		// Edit resource
		private void editresource_Click(object sender, RoutedEventArgs e)
		{
			List<Item> selected = SelectedItems;
			if(selected.Count != 1) return;
			Item selecteditem = selected[0];

			ResourceOptionsForm resoptions = CreateOptionsForm(selecteditem.Tag, "Resource Options");
			if(resoptions.ShowDialog(OwnerForm) == System.Windows.Forms.DialogResult.OK)
			{
				DataLocation rl = resoptions.ResourceLocation;
				int index = items.IndexOf(selecteditem);
				if(index >= 0) items[index] = new Item(rl, false, IsLoading(rl.location));

				if(rl.requiredarchives == null)
					StartRequiredArchivesCheck(rl.location);
				else if(loadingrequiredarchives.Count == 0)
					UpdateWarnings();

				OnContentChanged?.Invoke();
			}
		}

		// Remove resource
		private void deleteresources_Click(object sender, RoutedEventArgs e)
		{
			DeleteSelectedResources(); //mxd
		}

		// Item selected
		private void resourceitems_ItemSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			// Fixed (grayed) items can't be selected
			foreach(Item item in SelectedItems)
				if(item.Fixed) resourceitems.SelectedItems.Remove(item);

			int count = SelectedItems.Count;
			editresource.IsEnabled = (count == 1);
			deleteresources.IsEnabled = (count > 0);
		}

		// When an item is double clicked
		private void resourceitems_DoubleClick(object sender, TappedEventArgs e)
		{
			if(editresource.IsEnabled) editresource_Click(sender, null);
		}

		private void resourceitems_DragOver(object sender, DragEventArgs e)
		{
			e.DragEffects = e.DataTransfer.TryGetFiles() != null ? DragDropEffects.Copy : DragDropEffects.None;
		}

		// Files or folders dropped from the desktop
		private void resourceitems_DragDrop(object sender, DragEventArgs e)
		{
			IStorageItem[] dropped = e.DataTransfer.TryGetFiles();
			if(dropped == null) return;

			int addedfiles = 0;
			foreach(IStorageItem storageitem in dropped)
			{
				string path = storageitem.TryGetLocalPath();
				if(path == null) continue;
				if(File.Exists(path))
				{
					string ext = Path.GetExtension(path);
					if(string.IsNullOrEmpty(ext)) continue;
					switch(ext.ToLowerInvariant())
					{
						case ".wad":
						case ".iwad":
							if(AddItem(new DataLocation(DataLocation.RESOURCE_WAD, path, false, false, false, null))) addedfiles++;
							break;
						case ".pk7":
						case ".pk3":
						case ".pke":
						case ".ipk3":
						case ".ipk7":
							if(AddItem(new DataLocation(DataLocation.RESOURCE_PK3, path, false, false, false, null))) addedfiles++;
							break;
					}
				}
				else if(Directory.Exists(path))
				{
					if(AddItem(new DataLocation(DataLocation.RESOURCE_DIRECTORY, path, false, false, false, null))) addedfiles++;
				}
			}

			if(addedfiles == 0)
			{
				General.Interface.DisplayStatus(StatusType.Warning, "Invalid or duplicate resources!");
				return;
			}

			if(loadingrequiredarchives.Count == 0)
				UpdateWarnings();

			OnContentChanged?.Invoke();
		}

		// Reordering: drag an item onto another (lower items override higher items).
		private void resourceitems_PointerPressed(object sender, PointerPressedEventArgs e)
		{
			dragitem = ItemAt(e.Source);
			dragstart = e.GetPosition(resourceitems);
			dragging = false;
		}

		private void resourceitems_PointerMoved(object sender, PointerEventArgs e)
		{
			if(dragitem == null || dragitem.Fixed || !e.GetCurrentPoint(resourceitems).Properties.IsLeftButtonPressed) return;
			Point p = e.GetPosition(resourceitems);
			if(!dragging && Math.Abs(p.Y - dragstart.Y) > 6) dragging = true;
			if(dragging) resourceitems.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
		}

		private void resourceitems_PointerReleased(object sender, PointerReleasedEventArgs e)
		{
			if(dragging && dragitem != null)
			{
				Item target = ItemAt(resourceitems.InputHitTest(e.GetPosition(resourceitems)));
				int from = items.IndexOf(dragitem);
				int to = target != null ? items.IndexOf(target) : items.Count - 1;
				int firstmovable = 0;
				while(firstmovable < items.Count && items[firstmovable].Fixed) firstmovable++;
				to = Math.Max(to, firstmovable);
				if(from >= 0 && to >= 0 && from != to)
				{
					items.Move(from, to);
					resourceitems.SelectedItem = dragitem;
					OnContentChanged?.Invoke();
				}
			}
			dragitem = null;
			dragging = false;
			resourceitems.Cursor = null;
		}

		private static Item ItemAt(object source)
		{
			for(StyledElement el = source as StyledElement; el != null; el = el.Parent)
				if(el.DataContext is Item item) return item;
			return null;
		}

		#endregion

		#region ================== Copy / Paste (mxd)

		private void CopySelectedResources()
		{
			List<Item> selected = SelectedItems;
			if(selected.Count == 0) return;

			copiedresources.Clear();
			foreach(Item item in selected) copiedresources.Add(item.Tag);

			General.Interface.DisplayStatus(StatusType.Info, copiedresources.Count + " Resource" + (copiedresources.Count > 1 ? "s" : "") + " Copied to Clipboard");
		}

		private void PasteResources()
		{
			if(copiedresources.Count == 0) return;

			int pastedcount = 0;
			foreach(DataLocation dl in copiedresources)
				if(AddItem(dl)) pastedcount++;

			if(pastedcount > 0)
			{
				General.Interface.DisplayStatus(StatusType.Info, pastedcount + " Resource" + (pastedcount > 1 ? "s" : "") + " Pasted");
				if(loadingrequiredarchives.Count == 0) UpdateWarnings();
				OnContentChanged?.Invoke();
			}
		}

		private void ReplaceResources()
		{
			if(copiedresources.Count == 0) return;

			int pastedcount = 0;

			// Delete non-fixed resources
			for(int i = items.Count - 1; i > -1; i--)
			{
				if(items[i].Fixed) break;
				items.RemoveAt(i);
				pastedcount++;
			}

			// Paste new resources
			foreach(DataLocation dl in copiedresources)
				if(AddItem(dl)) pastedcount++;

			if(pastedcount > 0)
			{
				General.Interface.DisplayStatus(StatusType.Info, pastedcount + " Resource" + (pastedcount > 1 ? "s" : "") + " Replaced");
				if(loadingrequiredarchives.Count == 0) UpdateWarnings();
				OnContentChanged?.Invoke();
			}
		}

		private void DeleteSelectedResources()
		{
			List<Item> selected = SelectedItems;
			if(selected.Count == 0) return;

			foreach(Item item in selected)
				if(!item.Fixed) items.Remove(item);

			if(loadingrequiredarchives.Count == 0) UpdateWarnings();
			OnContentChanged?.Invoke();
		}

		private void copyresources_Click(object sender, RoutedEventArgs e) => CopySelectedResources();

		private void cutresources_Click(object sender, RoutedEventArgs e)
		{
			CopySelectedResources();
			DeleteSelectedResources();
		}

		private void pasteresources_Click(object sender, RoutedEventArgs e) => PasteResources();
		private void replaceresources_Click(object sender, RoutedEventArgs e) => ReplaceResources();
		private void removeresources_Click(object sender, RoutedEventArgs e) => DeleteSelectedResources();

		// Update copy/paste menu buttons
		private void copypastemenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
		{
			int count = SelectedItems.Count;
			copyresources.IsEnabled = count > 0;
			cutresources.IsEnabled = count > 0;
			pasteresources.IsEnabled = copiedresources.Count > 0;
			replaceresources.IsEnabled = copiedresources.Count > 0;
			removeresources.IsEnabled = count > 0;
		}

		private void resourceitems_KeyUp(object sender, KeyEventArgs e)
		{
			int key = (int)ULDB.Input.KeyMapping.ToKeys(e.Key, e.KeyModifiers);
			if(key == ShortcutKey("builder_copyselection")) CopySelectedResources();
			else if(key == ShortcutKey("builder_pasteselection")) PasteResources();
			else if(key == ShortcutKey("builder_pasteselectionspecial")) ReplaceResources();
			else if(key == ShortcutKey("builder_deleteitem")) DeleteSelectedResources();
			else if(key == ShortcutKey("builder_cutselection"))
			{
				CopySelectedResources();
				DeleteSelectedResources();
			}
		}

		// The form owning this control, for dialog ownership.
		private System.Windows.Forms.Form OwnerForm => ULDB.Shell.FormWindows.FormFor(TopLevel.GetTopLevel(this) as Window);

		#endregion
	}
}
