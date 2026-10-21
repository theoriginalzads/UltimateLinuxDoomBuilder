// ULDB: the command palette (upstream's CommandPaletteControl): a search box over a list of the
// editor's actions, matched like Visual Studio Code does, shown at the top middle of the map view.
// Up/Down/PageUp/PageDown move, Enter runs, Escape or clicking elsewhere closes. Recently used
// actions come first when the search is empty; actions that can't run in the current mode are dimmed.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using CodeImp.DoomBuilder;
using Actions = CodeImp.DoomBuilder.Actions;

namespace ULDB.Shell
{
	internal sealed class CommandPaletteOverlay : Border
	{
		private const int MAX_ITEMS = 16;
		private const int MAX_RECENT_ACTIONS = 5;

		private sealed class Row
		{
			public string Header;
			public Actions.Action Action;
			public bool Usable;
			public string Category, Shortcut;
		}

		private static CommandPaletteOverlay instance;
		private static readonly List<Actions.Action> recent = new List<Actions.Action>();

		private readonly TextBox search;
		private readonly ListBox list;
		private readonly TextBlock noresults;
		private readonly Action restoreFocus;
		private Action closed;
		private List<Row> rows = new List<Row>();

		/// <summary>Creates the overlay for the main window; <paramref name="restoreFocus"/> gives the focus back to the map view.</summary>
		public CommandPaletteOverlay(Action restoreFocus)
		{
			this.restoreFocus = restoreFocus;
			instance = this;
			IsVisible = false;
			HorizontalAlignment = HorizontalAlignment.Center;
			VerticalAlignment = VerticalAlignment.Top;
			Margin = new Thickness(0, 6, 0, 0);
			Width = 680;
			CornerRadius = new CornerRadius(6);
			BorderThickness = new Thickness(1);
			Padding = new Thickness(6);
			this.Bind(BackgroundProperty, this.GetResourceObservable("UldbCardBrush"));
			this.Bind(BorderBrushProperty, this.GetResourceObservable("UldbLineBrush"));
			BoxShadow = new BoxShadows(new BoxShadow { Blur = 18, Color = Color.FromArgb(90, 0, 0, 0), OffsetY = 4 });

			search = new TextBox { Watermark = "Type the name of a command...", Margin = new Thickness(0, 0, 0, 6) };
			noresults = new TextBlock { Text = "No results found", Opacity = 0.7, Margin = new Thickness(6, 4) };
			list = new ListBox { Focusable = false, MaxHeight = 22 * MAX_ITEMS + 8, SelectionMode = SelectionMode.Single };
			list.Styles.Add(new Avalonia.Styling.Style(x => x.OfType<ListBoxItem>())
			{
				Setters = { new Avalonia.Styling.Setter(MinHeightProperty, 0.0), new Avalonia.Styling.Setter(HeightProperty, 22.0), new Avalonia.Styling.Setter(PaddingProperty, new Thickness(6, 0)), new Avalonia.Styling.Setter(FocusableProperty, false) }
			});
			list.ItemTemplate = new FuncDataTemplate<Row>((row, _) => CreateRow(row), true);
			list.AddHandler(PointerReleasedEvent, (s, e) =>
			{
				if(e.InitialPressMouseButton == MouseButton.Left && list.SelectedItem is Row r && r.Action != null) RunSelected();
			}, Avalonia.Interactivity.RoutingStrategies.Bubble, true);
			Child = new StackPanel { Children = { search, list, noresults } };

			search.TextChanged += (s, e) => Fill(search.Text ?? "");
			search.LostFocus += (s, e) => Dispatcher.UIThread.Post(() => { if(IsVisible && !search.IsKeyboardFocusWithin) Close(false); }, DispatcherPriority.Background);
			search.AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
		}

		/// <summary>Opens the palette on the main window (does nothing before the window exists).</summary>
		public static void Open(Action onClosed)
		{
			Dispatcher.UIThread.Post(() => instance?.Show(onClosed), DispatcherPriority.Input);
		}

		private void Show(Action onClosed)
		{
			closed = onClosed;
			IsVisible = true;
			search.Text = "";
			Fill("");
			search.Focus();
		}

		private void Close(bool restore)
		{
			if(!IsVisible) return;
			IsVisible = false;
			closed?.Invoke();
			closed = null;
			if(restore) restoreFocus();
		}

		private void RunSelected()
		{
			if(!(list.SelectedItem is Row row) || row.Action == null) return;
			Actions.Action action = row.Action;
			Close(true);
			recent.Remove(action);
			recent.Insert(0, action);
			if(recent.Count > MAX_RECENT_ACTIONS) recent.RemoveRange(MAX_RECENT_ACTIONS, recent.Count - MAX_RECENT_ACTIONS);
			Dispatcher.UIThread.Post(() => General.Actions.InvokeAction(action.Name), DispatcherPriority.Background);
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			switch(e.Key)
			{
				case Key.Escape: e.Handled = true; Close(true); break;
				case Key.Down: e.Handled = true; Move(1, true); break;
				case Key.Up: e.Handled = true; Move(-1, true); break;
				case Key.PageDown: e.Handled = true; Move(MAX_ITEMS - 1, false); break;
				case Key.PageUp: e.Handled = true; Move(-(MAX_ITEMS - 1), false); break;
				case Key.Enter: e.Handled = true; RunSelected(); break;
			}
		}

		// Headers aren't selectable; moving skips them.
		private void Move(int by, bool wrap)
		{
			List<int> selectable = Enumerable.Range(0, rows.Count).Where(i => rows[i].Action != null).ToList();
			if(selectable.Count == 0) return;
			int at = Math.Max(0, selectable.IndexOf(list.SelectedIndex)) + by;
			if(at >= selectable.Count) at = wrap ? 0 : selectable.Count - 1;
			else if(at < 0) at = wrap ? selectable.Count - 1 : 0;
			list.SelectedIndex = selectable[at];
			list.ScrollIntoView(rows[selectable[at]]);
		}

		private void Fill(string text)
		{
			text = text.Trim();
			bool withrecent = text.Length == 0;
			List<Actions.Action> usable = new List<Actions.Action>(), unusable = new List<Actions.Action>();
			foreach(Actions.Action a in General.Actions.GetAllActions())
				if(MatchText(a.Title, text)) (a.BeginBound || a.EndBound ? usable : unusable).Add(a);

			List<Row> built = new List<Row>();
			if(withrecent && recent.Count > 0)
			{
				built.Add(new Row { Header = "Recently used" });
				built.AddRange(recent.Select(a => MakeRow(a, true)));
			}
			if(usable.Count > 0) { built.Add(new Row { Header = "Commands" }); built.AddRange(usable.OrderBy(a => a.Title).Select(a => MakeRow(a, true))); }
			if(unusable.Count > 0) { built.Add(new Row { Header = "Not available in the current mode" }); built.AddRange(unusable.OrderBy(a => a.Title).Select(a => MakeRow(a, false))); }

			rows = built;
			list.ItemsSource = rows;
			bool any = rows.Any(r => r.Action != null);
			list.IsVisible = any;
			noresults.IsVisible = !any;
			if(any) Move(0, false);
		}

		private static Row MakeRow(Actions.Action a, bool usable)
		{
			string category = General.Actions.Categories.ContainsKey(a.Category) ? General.Actions.Categories[a.Category] : "";
			return new Row { Action = a, Usable = usable, Category = category, Shortcut = Actions.Action.GetShortcutKeyDesc(a.ShortcutKey) };
		}

		private static Control CreateRow(Row row)
		{
			if(row.Action == null)
				return new TextBlock { Text = row.Header, FontWeight = FontWeight.SemiBold, FontSize = 11, Opacity = 0.65, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
			Grid g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,150,130"), Opacity = row.Usable ? 1.0 : 0.5 };
			g.Children.Add(new TextBlock { Text = row.Action.Title, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
			TextBlock cat = new TextBlock { Text = row.Category, Opacity = 0.65, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
			Grid.SetColumn(cat, 1);
			g.Children.Add(cat);
			TextBlock key = new TextBlock { Text = row.Shortcut, Opacity = 0.65, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
			Grid.SetColumn(key, 2);
			g.Children.Add(key);
			return g;
		}

		// Matches like Visual Studio Code (upstream's CommandPaletteControl.MatchText): the whole search text
		// inside the title, or each search word's start (and then as much of it as can be found) at the start of a title word.
		internal static bool MatchText(string text, string search)
		{
			text = Regex.Replace(text.ToLowerInvariant().Trim(), @"\s+", " ");
			search = Regex.Replace(search.ToLowerInvariant().Trim(), @"\s+", " ");
			if(string.IsNullOrWhiteSpace(search) || text.Contains(search)) return true;

			List<string> textitems = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
			string[] searchitems = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			for(int i = 0; i < searchitems.Length; i++)
			{
				string si = searchitems[i];
				if(string.IsNullOrEmpty(si)) continue;
				string result = null;
				while(si.Length > 0)
				{
					result = textitems.FirstOrDefault(ti => ti.StartsWith(si));
					if(result != null) { searchitems[i] = searchitems[i].Remove(0, si.Length); i--; break; }
					si = si.Remove(si.Length - 1);
				}
				if(result == null) return false;
				textitems.RemoveRange(0, textitems.IndexOf(result) + 1);
			}
			return true;
		}
	}
}
