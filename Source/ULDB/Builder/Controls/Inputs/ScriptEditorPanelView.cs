// ULDB: the view of upstream's ScriptEditorPanel (model-ported: upstream's tab, save, compile and
// find code runs unchanged). Menu, toolbar and status bar are the model's ToolStrips shown by
// ToolStripViews; the document tabs show a dot for unsaved changes and a close button; compile
// errors are listed under the editor and jump to the line on double click.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CodeImp.DoomBuilder.Compilers;
using CodeImp.DoomBuilder.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace ULDB.Controls
{
	internal sealed class ScriptEditorPanelView : UserControl
	{
		private readonly ScriptEditorPanel model;
		private readonly VSTabControl tabsmodel;
		private readonly TabControl tabs = new TabControl { Padding = new Thickness(0, 4, 0, 0) };
		private readonly Dictionary<WF.TabPage, TabItem> items = new Dictionary<WF.TabPage, TabItem>();
		private bool syncing;

		public ScriptEditorPanelView(ScriptEditorPanel model)
		{
			this.model = model;
			ModelBinder binder = new ModelBinder(model);
			tabsmodel = binder.Field<VSTabControl>("tabs");

			Menu menu = ToolStripViews.CreateMenuBar(binder.Field<WF.MenuStrip>("menustrip"));
			Control toolbar = ToolStripViews.CreateToolBar(binder.Field<WF.ToolStrip>("toolbar"), Orientation.Horizontal);
			Control status = ToolStripViews.CreateToolBar(binder.Field<WF.StatusStrip>("statusbar"), Orientation.Horizontal);
			Border menuborder = new Border { Child = menu };
			menuborder.Classes.Add("uldb-bar");
			Border toolborder = new Border { Child = toolbar, Padding = new Thickness(2, 1) };
			toolborder.Classes.Add("uldb-bar");
			Border statusborder = new Border { Child = status, Padding = new Thickness(2, 1), MinHeight = 24 };
			statusborder.Classes.Add("uldb-bar");

			// Documents and, below them, the compile errors
			ListBox errors = new ListBox { SelectionMode = SelectionMode.Single };
			errors.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<ModelBinder.ListItem>((item, _) =>
			{
				Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("32,*,Auto"), Margin = new Thickness(2, 1) };
				row.Children.Add(new PathIcon { Width = 12, Height = 12, Foreground = new SolidColorBrush(Color.FromRgb(232, 60, 60)), Data = Geometry.Parse("M8,1 A7,7 0 1,1 8,15 A7,7 0 1,1 8,1 Z M7.2,4 V9 H8.8 V4 Z M7.2,10.3 V11.8 H8.8 V10.3 Z"), HorizontalAlignment = HorizontalAlignment.Left });
				TextBlock text = new TextBlock { Text = item?.Sub1, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
				Grid.SetColumn(text, 1);
				TextBlock file = new TextBlock { Text = item?.Sub2, Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center };
				Grid.SetColumn(file, 2);
				row.Children.Add(text);
				row.Children.Add(file);
				return row;
			}, true);
			WF.ListView errorlist = binder.Field<WF.ListView>("errorlist");
			binder.Items(errors, errorlist);
			errors.DoubleTapped += (s, e) => GoToError(errors.SelectedItem as ModelBinder.ListItem);
			Border errorsborder = new Border { Child = errors, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), ClipToBounds = true, Height = 170 };
			errorsborder.Classes.Add("uldb-grid");
			Grid errorspanel = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
			errorspanel.Children.Add(new TextBlock { Text = "Errors", FontWeight = FontWeight.SemiBold, Margin = new Thickness(2, 6, 0, 4) });
			Grid.SetRow(errorsborder, 1);
			errorspanel.Children.Add(errorsborder);

			// The errors panel follows the model's splitter (upstream shows it when there are errors)
			WF.SplitContainer splitter = binder.Field<WF.SplitContainer>("scriptsplitter");
			void UpdateErrorsVisible() => errorspanel.IsVisible = splitter.Panel2.Visible && errorlist.Items.Count > 0;
			UpdateErrorsVisible();
			splitter.Panel2.VisibleChanged += (s, e) => Dispatcher.UIThread.Post(UpdateErrorsVisible);
			binder.Custom(UpdateErrorsVisible, errorlist);

			DockPanel root = new DockPanel();
			DockPanel.SetDock(menuborder, Dock.Top);
			DockPanel.SetDock(toolborder, Dock.Top);
			DockPanel.SetDock(statusborder, Dock.Bottom);
			DockPanel.SetDock(errorspanel, Dock.Bottom);
			root.Children.Add(menuborder);
			root.Children.Add(toolborder);
			root.Children.Add(statusborder);
			root.Children.Add(errorspanel);
			root.Children.Add(new Border { Child = tabs, Padding = new Thickness(6, 0, 6, 0) });
			Content = root;

			// Tabs follow the model
			tabsmodel.TabPagesChanged += (s, e) => Dispatcher.UIThread.Post(RebuildTabs);
			tabsmodel.SelectedIndexChanged += (s, e) => SyncSelection();
			tabs.SelectionChanged += OnViewSelectionChanged;
			RebuildTabs();

			// The close button: upstream's handler (not wired by its designer: the control drew the buttons itself)
			System.Reflection.MethodInfo close = typeof(ScriptEditorPanel).GetMethod("tabs_OnCloseTabClicked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if(close != null) tabsmodel.OnCloseTabClicked += (s, e) => close.Invoke(model, new object[] { s, e });

			binder.UpdateNow();
		}

		// A stroked cross (PathIcon only fills, which would draw nothing for lines)
		private static Control CloseGlyph()
		{
			Path cross = new Path { Width = 10, Height = 10, Data = Geometry.Parse("M1,1 L9,9 M9,1 L1,9"), StrokeThickness = 1.4, StrokeLineCap = PenLineCap.Round };
			cross.Stroke = Brushes.Gray;
			return cross;
		}

		#region ================== Tabs

		private void RebuildTabs()
		{
			syncing = true;
			try
			{
				foreach(WF.TabPage gone in items.Keys.Where(p => !tabsmodel.TabPages.Contains(p)).ToList()) items.Remove(gone);
				List<TabItem> list = new List<TabItem>();
				foreach(WF.TabPage page in tabsmodel.TabPages)
				{
					if(!items.TryGetValue(page, out TabItem item)) items[page] = item = CreateItem(page);
					list.Add(item);
				}
				tabs.ItemsSource = list;
				SyncSelection();
			}
			finally { syncing = false; }
		}

		private TabItem CreateItem(WF.TabPage page)
		{
			TabItem item = new TabItem { Tag = page };
			UpdateHeader(item, page);
			page.TextChanged += (s, e) => Dispatcher.UIThread.Post(() => UpdateHeader(item, page));
			return item;
		}

		private void UpdateHeader(TabItem item, WF.TabPage page)
		{
			string title = page.Text ?? "";
			bool modified = title.StartsWith("● ");
			if(modified) title = title.Substring(2);

			StackPanel header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
			header.Children.Add(new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.FromRgb(0xE8, 0xA3, 0x17)), VerticalAlignment = VerticalAlignment.Center, IsVisible = modified });
			ToolTip.SetTip(header, modified ? "Unsaved changes" : null);
			header.Children.Add(new TextBlock { Text = title, VerticalAlignment = VerticalAlignment.Center });
			bool closable = !(page is ScriptDocumentTab t) || t.IsClosable;
			if(tabsmodel.ShowClosingButton && closable)
			{
				Button close = new Button { Content = CloseGlyph(), Padding = new Thickness(3), MinWidth = 0, MinHeight = 0, Background = Brushes.Transparent, BorderThickness = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
				close.Classes.Add("uldb-tool");
				ToolTip.SetTip(close, "Close");
				close.Click += (s, e) => tabsmodel.RaiseCloseTabClicked(page);
				header.Children.Add(close);
			}
			item.Header = header;
		}

		private void EnsureContent(TabItem item)
		{
			if(item.Content != null || !(item.Tag is WF.TabPage page)) return;
			foreach(WF.Control child in page.Controls)
			{
				Control view = ControlViews.Create(child);
				if(view != null) { item.Content = view; return; }
			}
		}

		private void SyncSelection()
		{
			bool was = syncing;
			syncing = true;
			try
			{
				int index = tabsmodel.SelectedIndex;
				if(index >= 0 && index < tabs.ItemCount)
				{
					tabs.SelectedIndex = index;
					if(tabs.SelectedItem is TabItem item) EnsureContent(item);
				}
			}
			finally { syncing = was; }
		}

		private void OnViewSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(syncing) return;
			if(tabs.SelectedItem is TabItem item) EnsureContent(item);
			if(tabs.SelectedIndex != tabsmodel.SelectedIndex)
			{
				tabsmodel.SelectedIndex = tabs.SelectedIndex;
				// Upstream may refuse to leave a tab
				if(tabs.SelectedIndex != tabsmodel.SelectedIndex) SyncSelection();
			}
			if(tabs.SelectedItem is TabItem sel && sel.Content is Control c) Dispatcher.UIThread.Post(() => FocusEditor(c), DispatcherPriority.Input);
		}

		private static void FocusEditor(Control view)
		{
			foreach(Avalonia.Visual v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(view))
				if(v is AvaloniaEdit.Editing.TextArea ta) { ta.Focus(); return; }
		}

		#endregion

		#region ================== Errors

		private void GoToError(ModelBinder.ListItem item)
		{
			if(!(item?.Tag is CompilerError error)) return;
			foreach(WF.TabPage page in tabsmodel.TabPages)
			{
				if(page is ScriptDocumentTab tab && tab.VerifyErrorForScript(error))
				{
					tabsmodel.SelectedTab = tab;
					if(error.linenumber != CompilerError.NO_LINE_NUMBER) tab.MoveToLine(error.linenumber);
					return;
				}
			}
		}

		#endregion
	}
}
