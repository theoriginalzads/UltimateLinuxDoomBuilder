// ULDB: the dialogue builder. A form view of the DIALOGUE lump (Strife dialogue, USDF) next to the script
// editor's text: conversations and their pages on the left, the selected page and its choices on the right.
// It edits a UsdfDocument read from the tab's text, and "Apply" writes the text back into the tab (one undo
// step), so the text stays what is saved with the map. Comments in the text don't survive that; the builder
// says so once before replacing text that has them.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Map;
using ULDB.Shell;

namespace ULDB.Dialogue
{
	internal sealed class DialogueBuilderWindow : Window
	{
		#region ================== Variables

		private readonly UsdfDocument doc;
		private readonly Func<string, bool> apply; // writes the text into the script tab
		private readonly bool hadcomments;
		private readonly List<string> classnames;

		private readonly ListBox tree = new ListBox { SelectionMode = SelectionMode.Single }; // conversations, and their pages indented under them
		private readonly ContentControl editor = new ContentControl();
		private readonly TextBlock problems = new TextBlock { TextWrapping = TextWrapping.Wrap, MaxHeight = 60 };
		private readonly ComboBox nsbox = new ComboBox { Width = 120 };
		private object selected; // UsdfConversation or UsdfPage
		private bool loading, changed;
		private DispatcherTimer checktimer;

		private static readonly string[] Namespaces = { "GZDoom", "ZDoom", "Strife" };

		#endregion

		#region ================== Constructor

		public DialogueBuilderWindow(UsdfDocument doc, bool hadcomments, Func<string, bool> apply)
		{
			this.doc = doc;
			this.hadcomments = hadcomments;
			this.apply = apply;
			classnames = KnownClassNames();

			Title = "Dialogue Builder";
			Width = 1180; Height = 820; MinWidth = 900; MinHeight = 560;
			ShowInTaskbar = false;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			// Top: the dialogue's namespace
			nsbox.ItemsSource = Namespaces;
			nsbox.SelectedIndex = Math.Max(0, Array.FindIndex(Namespaces, n => n.Equals(doc.Namespace ?? "", StringComparison.OrdinalIgnoreCase)));
			if(string.IsNullOrEmpty(doc.Namespace)) nsbox.SelectedIndex = 0;
			nsbox.SelectionChanged += (s, e) =>
			{
				doc.Namespace = (string)nsbox.SelectedItem;
				Changed();
				ShowSelected();
			};
			ToolTip.SetTip(nsbox, "GZDoom: class names and named pages. ZDoom: class names. Strife: numbers, as in Strife's own dialogues.");
			StackPanel top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 0, 0, 10) };
			top.Children.Add(new TextBlock { Text = "Namespace:", VerticalAlignment = VerticalAlignment.Center });
			top.Children.Add(nsbox);
			if(doc.Includes.Count > 0) top.Children.Add(new TextBlock { Text = "Includes: " + string.Join(", ", doc.Includes), Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0) });

			// Left: conversations and pages
			tree.SelectionChanged += (s, e) =>
			{
				if(loading) return;
				selected = (tree.SelectedItem as ListBoxItem)?.Tag;
				ShowSelected();
			};
			Border treeborder = new Border { Child = tree, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), ClipToBounds = true };
			treeborder.Classes.Add("uldb-grid");
			WrapPanel treebuttons = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
			treebuttons.Children.Add(SmallButton("+ Conversation", "Add a conversation", AddConversation));
			treebuttons.Children.Add(SmallButton("+ Page", "Add a page to the selected conversation", AddPage));
			treebuttons.Children.Add(SmallButton("Duplicate", "Copy the selected page or conversation", Duplicate));
			treebuttons.Children.Add(SmallButton("↑", "Move up", () => Move(-1)));
			treebuttons.Children.Add(SmallButton("↓", "Move down", () => Move(1)));
			treebuttons.Children.Add(SmallButton("Delete", "Delete the selected page or conversation", Delete));
			DockPanel left = new DockPanel { Width = 280, Margin = new Thickness(0, 0, 12, 0) };
			DockPanel.SetDock(treebuttons, Dock.Bottom);
			left.Children.Add(treebuttons);
			left.Children.Add(treeborder);

			// Bottom: problems, Apply/Cancel
			Button applybutton = new Button { Content = "Apply to script", MinWidth = 120, IsDefault = true, HorizontalContentAlignment = HorizontalAlignment.Center };
			applybutton.Classes.Add("accent");
			applybutton.Click += (s, e) => { if(Apply()) Close(); };
			Button cancelbutton = new Button { Content = "Cancel", MinWidth = 100, IsCancel = true, HorizontalContentAlignment = HorizontalAlignment.Center };
			cancelbutton.Click += (s, e) => Close();
			StackPanel buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
			buttons.Children.Add(applybutton);
			buttons.Children.Add(cancelbutton);
			DockPanel bottom = new DockPanel { Margin = new Thickness(0, 10, 0, 0) };
			DockPanel.SetDock(buttons, Dock.Right);
			bottom.Children.Add(buttons);
			bottom.Children.Add(problems);
			problems.Classes.Add("uldb-red");

			DockPanel root = new DockPanel { Margin = new Thickness(14) };
			DockPanel.SetDock(top, Dock.Top);
			DockPanel.SetDock(bottom, Dock.Bottom);
			DockPanel.SetDock(left, Dock.Left);
			root.Children.Add(top);
			root.Children.Add(bottom);
			root.Children.Add(left);
			root.Children.Add(editor);
			Content = root;

			Closing += (s, e) =>
			{
				if(!changed || appliedclose) return;
				e.Cancel = true;
				Dispatcher.UIThread.Post(async () =>
				{
					MessageBoxWindow box = new MessageBoxWindow("Apply the changes to the DIALOGUE script?", "Dialogue Builder",
						System.Windows.Forms.MessageBoxButtons.YesNoCancel, System.Windows.Forms.MessageBoxIcon.Question, System.Windows.Forms.MessageBoxDefaultButton.Button1);
					await box.ShowDialog(this);
					if(box.Result == System.Windows.Forms.DialogResult.Yes) { if(Apply()) { appliedclose = true; Close(); } }
					else if(box.Result == System.Windows.Forms.DialogResult.No) { appliedclose = true; Close(); }
				});
			};

			Opened += (s, e) => DevCapture.Schedule(this, TimeSpan.FromSeconds(1.5)); // development aid (ULDB_CAPTURE)
			// Development aid: ULDB_DIALOGUE_SELECT=<row> selects a row of the list, ULDB_DIALOGUE_SCROLL=1 scrolls the choice editor down
			if(int.TryParse(Environment.GetEnvironmentVariable("ULDB_DIALOGUE_SELECT"), out int devrow))
				Opened += (s, e) => Dispatcher.UIThread.Post(() => tree.SelectedIndex = devrow, DispatcherPriority.Background);
			// ULDB_DIALOGUE_CHOICE=<n> selects that choice of the page; ULDB_DIALOGUE_APPLY=1 applies and closes
			if(int.TryParse(Environment.GetEnvironmentVariable("ULDB_DIALOGUE_CHOICE"), out int devchoice))
				Opened += (s, e) => DispatcherTimer.RunOnce(() =>
				{
					ListBox choicelist = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(editor).OfType<ListBox>().FirstOrDefault();
					if(choicelist != null) choicelist.SelectedIndex = devchoice;
				}, TimeSpan.FromMilliseconds(300));
			if(Environment.GetEnvironmentVariable("ULDB_DIALOGUE_APPLY") == "1")
				Opened += (s, e) => DispatcherTimer.RunOnce(() => { changed = true; if(Apply()) Close(); }, TimeSpan.FromMilliseconds(800));
			if(Environment.GetEnvironmentVariable("ULDB_DIALOGUE_SCROLL") == "1")
				Opened += (s, e) => DispatcherTimer.RunOnce(() =>
				{
					foreach(ScrollViewer sv in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(this).OfType<ScrollViewer>()) sv.ScrollToEnd();
				}, TimeSpan.FromMilliseconds(600));

			selected = doc.Conversations.Count > 0 ? (doc.Conversations[0].Pages.Count > 0 ? doc.Conversations[0].Pages[0] : (object)doc.Conversations[0]) : null;
			RebuildTree();
			ShowSelected();
			CheckSoon();
		}

		private bool appliedclose;

		#endregion

		#region ================== Applying

		private bool Apply()
		{
			if(hadcomments && !warnedcomments)
			{
				warnedcomments = true;
				MessageBoxWindow box = new MessageBoxWindow("The DIALOGUE script has comments. The builder writes the whole script again in its own layout, so the comments will be lost.\n\nReplace the script?",
					"Dialogue Builder", System.Windows.Forms.MessageBoxButtons.OKCancel, System.Windows.Forms.MessageBoxIcon.Warning, System.Windows.Forms.MessageBoxDefaultButton.Button2);
				Modal.ShowDialog(box, this);
				if(box.Result != System.Windows.Forms.DialogResult.OK) return false;
			}
			if(string.IsNullOrEmpty(doc.Namespace)) doc.Namespace = Namespaces[0];
			bool ok = apply(doc.Write());
			if(ok) { changed = false; appliedclose = true; }
			return ok;
		}

		private static bool warnedcomments;

		#endregion

		#region ================== Tree

		private string ConversationTitle(UsdfConversation c)
		{
			string actor = c.Actor != null && c.Actor.Text.Length > 0 ? c.Actor.Text : "(no actor)";
			return actor + (c.Id.HasValue ? "  ·  ID " + c.Id.Value : "");
		}

		private string PageTitle(UsdfConversation c, UsdfPage p)
		{
			int index = c.Pages.IndexOf(p) + 1;
			string text = (p.Dialog ?? "").Replace("\n", " ").Trim();
			if(text.Length > 34) text = text.Substring(0, 34) + "…";
			string name = !string.IsNullOrEmpty(p.PageName) ? "[" + p.PageName + "] " : "";
			return index + ". " + name + (text.Length > 0 ? text : "(empty page)");
		}

		private void RebuildTree()
		{
			loading = true;
			try
			{
				List<ListBoxItem> items = new List<ListBoxItem>();
				ListBoxItem select = null;
				foreach(UsdfConversation c in doc.Conversations)
				{
					ListBoxItem ci = new ListBoxItem { Content = ConversationTitle(c), Tag = c, FontWeight = FontWeight.SemiBold, Padding = new Thickness(8, 6, 8, 4) };
					if(c == selected) select = ci;
					items.Add(ci);
					foreach(UsdfPage p in c.Pages)
					{
						ListBoxItem pi = new ListBoxItem { Content = PageTitle(c, p), Tag = p, Padding = new Thickness(24, 4, 8, 4) };
						if(p == selected) select = pi;
						items.Add(pi);
					}
				}
				tree.ItemsSource = items;
				tree.SelectedItem = select;
			}
			finally { loading = false; }
		}

		// Titles only (text changed), keeping the list as it is
		private void RefreshTitles()
		{
			if(!(tree.ItemsSource is List<ListBoxItem> items)) return;
			UsdfConversation current = null;
			foreach(ListBoxItem item in items)
			{
				if(item.Tag is UsdfConversation c) { current = c; item.Content = ConversationTitle(c); }
				else if(item.Tag is UsdfPage p && current != null) item.Content = PageTitle(current, p);
			}
		}

		private UsdfConversation SelectedConversation()
		{
			if(selected is UsdfConversation c) return c;
			if(selected is UsdfPage p) return doc.Conversations.FirstOrDefault(x => x.Pages.Contains(p));
			return null;
		}

		private void AddConversation()
		{
			UsdfConversation c = new UsdfConversation();
			if(doc.UsesClassNames)
			{
				c.Actor = UsdfValue.Of("");
				c.Id = doc.Conversations.Where(x => x.Id.HasValue).Select(x => x.Id.Value).DefaultIfEmpty(0).Max() + 1;
			}
			else c.Actor = UsdfValue.Of(1);
			UsdfPage p = new UsdfPage { Name = "", Dialog = "" };
			c.Pages.Add(p);
			doc.Conversations.Add(c);
			selected = c;
			Changed(); RebuildTree(); ShowSelected();
		}

		private void AddPage()
		{
			UsdfConversation c = SelectedConversation();
			if(c == null) { AddConversation(); return; }
			UsdfPage p = new UsdfPage { Name = c.Pages.Count > 0 ? c.Pages[c.Pages.Count - 1].Name : "", Dialog = "" };
			int at = selected is UsdfPage sp ? c.Pages.IndexOf(sp) + 1 : c.Pages.Count;
			InsertPage(c, at, p);
			selected = p;
			Changed(); RebuildTree(); ShowSelected();
		}

		private void Duplicate()
		{
			if(selected is UsdfPage p)
			{
				UsdfConversation c = SelectedConversation();
				UsdfPage copy = UsdfDocument.Parse("conversation { page {" + PageText(p) + "} }").Conversations[0].Pages[0];
				if(!string.IsNullOrEmpty(copy.PageName)) copy.PageName += "_copy";
				InsertPage(c, c.Pages.IndexOf(p) + 1, copy);
				selected = copy;
			}
			else if(selected is UsdfConversation c)
			{
				UsdfDocument one = new UsdfDocument { Namespace = doc.Namespace };
				one.Conversations.Add(c);
				UsdfConversation copy = UsdfDocument.Parse(one.Write()).Conversations[0];
				if(copy.Id.HasValue) copy.Id = doc.Conversations.Where(x => x.Id.HasValue).Select(x => x.Id.Value).DefaultIfEmpty(0).Max() + 1;
				doc.Conversations.Insert(doc.Conversations.IndexOf(c) + 1, copy);
				selected = copy;
			}
			else return;
			Changed(); RebuildTree(); ShowSelected();
		}

		// The page's fields as USDF (for copying)
		private static string PageText(UsdfPage p)
		{
			UsdfDocument one = new UsdfDocument();
			UsdfConversation c = new UsdfConversation();
			c.Pages.Add(p);
			one.Conversations.Add(c);
			string text = one.Write();
			int open = text.IndexOf("page", StringComparison.Ordinal);
			open = text.IndexOf('{', open);
			int close = text.LastIndexOf('}', text.LastIndexOf('}') - 1);
			return text.Substring(open + 1, close - open - 1);
		}

		private void Move(int direction)
		{
			if(selected is UsdfPage p)
			{
				UsdfConversation c = SelectedConversation();
				int from = c.Pages.IndexOf(p), to = from + direction;
				if(to < 0 || to >= c.Pages.Count) return;
				c.Pages.RemoveAt(from);
				c.Pages.Insert(to, p);
				// Page numbers in links follow the pages: from+1 <-> to+1
				Renumber(c, n => n == from + 1 ? to + 1 : n == to + 1 ? from + 1 : n);
			}
			else if(selected is UsdfConversation c)
			{
				int from = doc.Conversations.IndexOf(c), to = from + direction;
				if(to < 0 || to >= doc.Conversations.Count) return;
				doc.Conversations.RemoveAt(from);
				doc.Conversations.Insert(to, c);
			}
			else return;
			Changed(); RebuildTree(); ShowSelected();
		}

		private void Delete()
		{
			if(selected is UsdfPage p)
			{
				UsdfConversation c = SelectedConversation();
				int index = c.Pages.IndexOf(p);
				c.Pages.RemoveAt(index);
				// Links to the deleted page go nowhere (the check reports them); later pages move up one
				Renumber(c, n => n == index + 1 ? (int?)null : n > index + 1 ? n - 1 : n);
				selected = c.Pages.Count > 0 ? c.Pages[Math.Min(index, c.Pages.Count - 1)] : (object)c;
			}
			else if(selected is UsdfConversation c)
			{
				int index = doc.Conversations.IndexOf(c);
				doc.Conversations.RemoveAt(index);
				selected = doc.Conversations.Count > 0 ? doc.Conversations[Math.Min(index, doc.Conversations.Count - 1)] : null;
			}
			else return;
			Changed(); RebuildTree(); ShowSelected();
		}

		private void InsertPage(UsdfConversation c, int at, UsdfPage p)
		{
			c.Pages.Insert(at, p);
			Renumber(c, n => n > at ? n + 1 : n); // pages after it move down one
		}

		// Numbered links (link, nextpage) follow pages that moved; a null result removes the link
		private static void Renumber(UsdfConversation c, Func<int, int?> map)
		{
			UsdfValue Fix(UsdfValue v)
			{
				if(v == null || v.Kind != UsdfValue.ValueKind.Integer) return v;
				int? n = map(v.AsInt());
				return n.HasValue ? UsdfValue.Of(n.Value) : null;
			}
			foreach(UsdfPage p in c.Pages)
			{
				p.Link = Fix(p.Link);
				foreach(UsdfChoice ch in p.Choices) ch.NextPage = Fix(ch.NextPage);
			}
		}

		#endregion

		#region ================== Editors

		private void ShowSelected()
		{
			loading = true;
			try
			{
				if(selected is UsdfPage p) editor.Content = PageEditor(SelectedConversation(), p);
				else if(selected is UsdfConversation c) editor.Content = ConversationEditor(c);
				else editor.Content = new TextBlock { Text = "No conversations yet. Add one with \"+ Conversation\".", Opacity = 0.7, Margin = new Thickness(8) };
			}
			finally { loading = false; }
		}

		private Control ConversationEditor(UsdfConversation c)
		{
			StackPanel panel = new StackPanel { Spacing = 8 };
			Grid grid = FieldGrid();
			AutoCompleteBox actor = ClassBox(c.Actor?.Text ?? "");
			actor.TextChanged += (s, e) => { if(loading) return; c.Actor = ItemValue(actor.Text); Changed(); RefreshTitles(); };
			AddRow(grid, doc.UsesClassNames ? "Actor class:" : "Actor (conversation ID):", actor, doc.UsesClassNames ? "The class of the actor this conversation belongs to." : "Strife's conversation ID of the actor.");

			NumericUpDown id = new NumericUpDown { Minimum = 0, Maximum = 65535, Increment = 1, FormatString = "0", Value = c.Id ?? 0, Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
			id.ValueChanged += (s, e) => { if(loading) return; c.Id = id.Value > 0 ? (int?)(int)id.Value : null; Changed(); RefreshTitles(); ShowSelectedSoon(); };
			AddRow(grid, "ID:", id, "Things on the map use this conversation through their Conversation ID (Edit Thing, Action / Tag / Misc.). 0: none.");

			TextBox menuclass = new TextBox { Text = c.Class ?? "", Watermark = "(the default menu)" };
			menuclass.TextChanged += (s, e) => { if(loading) return; c.Class = menuclass.Text; Changed(); };
			AddRow(grid, "Menu class:", menuclass, "A custom conversation menu class (ZScript). Usually left empty.");
			panel.Children.Add(Group("Conversation", grid));

			// Things that use it
			StackPanel things = new StackPanel { Spacing = 6 };
			List<Thing> users = c.Id.HasValue ? ThingsUsing(c.Id.Value) : new List<Thing>();
			int selectedthings = General.Map?.Map?.GetSelectedThings(true).Count ?? 0;
			things.Children.Add(new TextBlock { Text = !c.Id.HasValue ? "Give the conversation an ID to assign it to things." : users.Count == 0 ? "No thing on this map uses conversation " + c.Id.Value + "." : users.Count + (users.Count == 1 ? " thing uses" : " things use") + " conversation " + c.Id.Value + ".", TextWrapping = TextWrapping.Wrap });
			StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
			Button selectbutton = new Button { Content = "Select them on the map", IsEnabled = users.Count > 0 };
			selectbutton.Click += (s, e) =>
			{
				General.Map.Map.ClearSelectedThings();
				foreach(Thing t in users) t.Selected = true;
				General.Interface.RedrawDisplay();
			};
			Button assign = new Button { Content = "Give it to the " + selectedthings + " selected thing" + (selectedthings == 1 ? "" : "s"), IsEnabled = c.Id.HasValue && selectedthings > 0 && General.Map.UDMF };
			assign.Click += (s, e) =>
			{
				General.Map.UndoRedo.CreateUndo("Assign conversation " + c.Id.Value);
				foreach(Thing t in General.Map.Map.GetSelectedThings(true)) UniFields.SetInteger(t.Fields, "conversation", c.Id.Value, 0);
				General.Map.IsChanged = true;
				ShowSelected();
			};
			row.Children.Add(selectbutton);
			row.Children.Add(assign);
			things.Children.Add(row);
			panel.Children.Add(Group("On this map", things));
			panel.Children.Add(new TextBlock { Text = c.Pages.Count + (c.Pages.Count == 1 ? " page" : " pages") + ". Select a page on the left to edit it.", Opacity = 0.7, Margin = new Thickness(2, 4) });
			return new ScrollViewer { Content = panel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
		}

		private Control PageEditor(UsdfConversation c, UsdfPage p)
		{
			// Left: the page
			StackPanel pagepanel = new StackPanel { Spacing = 8 };
			Grid grid = FieldGrid();
			TextBox name = new TextBox { Text = p.Name ?? "", Watermark = "Shown over the text" };
			name.TextChanged += (s, e) => { if(loading) return; p.Name = name.Text; Changed(); };
			AddRow(grid, "Speaker:", name, "The name shown in the corner of the dialogue.");
			TextBox pagename = new TextBox { Text = p.PageName ?? "", Watermark = doc.IsGZDoom ? "(optional)" : "GZDoom namespace only", IsEnabled = doc.IsGZDoom };
			pagename.TextChanged += (s, e) => { if(loading) return; p.PageName = pagename.Text; Changed(); RefreshTitles(); };
			AddRow(grid, "Page name:", pagename, "A name other pages and choices can link to (GZDoom namespace).");
			pagepanel.Children.Add(Group("Page " + (c.Pages.IndexOf(p) + 1), grid));

			TextBox dialog = new TextBox { Text = p.Dialog ?? "", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 150, Watermark = "What the actor says" };
			dialog.TextChanged += (s, e) => { if(loading) return; p.Dialog = dialog.Text; Changed(); RefreshTitles(); };
			pagepanel.Children.Add(Group("Dialogue", dialog));

			Grid extra = FieldGrid();
			TextBox panelbox = new TextBox { Text = p.Panel ?? "", Watermark = "Graphic lump" };
			panelbox.TextChanged += (s, e) => { if(loading) return; p.Panel = panelbox.Text; Changed(); };
			AddRow(extra, "Portrait:", panelbox, "A graphic shown behind the dialogue (Strife: the speaker's portrait).");
			TextBox voice = new TextBox { Text = p.Voice ?? "", Watermark = "Sound" };
			voice.TextChanged += (s, e) => { if(loading) return; p.Voice = voice.Text; Changed(); };
			AddRow(extra, "Voice:", voice, "A sound played when the page opens.");
			TextBox goodbye = new TextBox { Text = p.Goodbye ?? "", Watermark = doc.UsesClassNames ? "(a generic goodbye)" : "ZDoom namespace only", IsEnabled = doc.UsesClassNames };
			goodbye.TextChanged += (s, e) => { if(loading) return; p.Goodbye = goodbye.Text; Changed(); };
			AddRow(extra, "Goodbye:", goodbye, "The text of the choice that leaves the conversation.");
			AutoCompleteBox drop = ClassBox(p.Drop?.Text ?? "");
			drop.TextChanged += (s, e) => { if(loading) return; p.Drop = ItemValue(drop.Text); Changed(); };
			AddRow(extra, "Drops:", drop, "What the actor drops when it's killed.");
			pagepanel.Children.Add(Group("Look and sound", extra));

			StackPanel jump = new StackPanel { Spacing = 6 };
			ComboBox link = PageCombo(c, p.Link, "(don't jump)");
			link.SelectionChanged += (s, e) => { if(loading) return; p.Link = PageValue(c, link); Changed(); };
			jump.Children.Add(Labelled("Go to page:", link));
			jump.Children.Add(new TextBlock { Text = "when the player has all of:", Opacity = 0.75 });
			jump.Children.Add(new ItemChecksEditor(p.IfItems, classnames, ItemValue, Changed));
			pagepanel.Children.Add(Group("Skip this page", jump));

			// Right: the choices
			StackPanel choicepanel = new StackPanel { Spacing = 8 };
			ListBox choices = new ListBox { MaxHeight = 160, MinHeight = 60 };
			ContentControl choiceeditor = new ContentControl();
			void FillChoices(UsdfChoice select)
			{
				loading = true;
				choices.ItemsSource = p.Choices.Select((ch, i) => new ListBoxItem { Content = ChoiceTitle(c, ch, i), Tag = ch }).ToList();
				choices.SelectedIndex = select != null ? p.Choices.IndexOf(select) : -1;
				loading = false;
				choiceeditor.Content = select != null ? ChoiceEditor(c, p, select, () => RefreshChoiceTitles(c, p, choices)) : new TextBlock { Text = p.Choices.Count == 0 ? "No choices: the page only has the goodbye." : "Select a choice to edit it.", Opacity = 0.7, Margin = new Thickness(4) };
			}
			choices.SelectionChanged += (s, e) =>
			{
				if(loading) return;
				UsdfChoice ch = (choices.SelectedItem as ListBoxItem)?.Tag as UsdfChoice;
				choiceeditor.Content = ch != null ? ChoiceEditor(c, p, ch, () => RefreshChoiceTitles(c, p, choices)) : null;
			};
			WrapPanel choicebuttons = new WrapPanel();
			choicebuttons.Children.Add(SmallButton("+ Choice", "Add a choice", () =>
			{
				UsdfChoice ch = new UsdfChoice { Text = "" };
				p.Choices.Add(ch);
				Changed(); FillChoices(ch);
			}));
			choicebuttons.Children.Add(SmallButton("↑", "Move up", () => MoveChoice(p, choices, -1, FillChoices)));
			choicebuttons.Children.Add(SmallButton("↓", "Move down", () => MoveChoice(p, choices, 1, FillChoices)));
			choicebuttons.Children.Add(SmallButton("Delete", "Delete the selected choice", () =>
			{
				if(!((choices.SelectedItem as ListBoxItem)?.Tag is UsdfChoice ch)) return;
				int i = p.Choices.IndexOf(ch);
				p.Choices.Remove(ch);
				Changed(); FillChoices(p.Choices.Count > 0 ? p.Choices[Math.Min(i, p.Choices.Count - 1)] : null);
			}));
			Border listborder = new Border { Child = choices, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3), ClipToBounds = true };
			listborder.Classes.Add("uldb-grid");
			StackPanel list = new StackPanel { Spacing = 6 };
			list.Children.Add(listborder);
			list.Children.Add(choicebuttons);
			choicepanel.Children.Add(Group("Choices", list));
			choicepanel.Children.Add(choiceeditor);
			FillChoices(p.Choices.Count > 0 ? p.Choices[0] : null);

			Grid columns = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,*") };
			ScrollViewer leftscroll = new ScrollViewer { Content = pagepanel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 10, 0) };
			ScrollViewer rightscroll = new ScrollViewer { Content = choicepanel, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Padding = new Thickness(0, 0, 10, 0) };
			Grid.SetColumn(rightscroll, 2);
			columns.Children.Add(leftscroll);
			columns.Children.Add(rightscroll);
			return columns;
		}

		private void MoveChoice(UsdfPage p, ListBox choices, int direction, Action<UsdfChoice> fill)
		{
			if(!((choices.SelectedItem as ListBoxItem)?.Tag is UsdfChoice ch)) return;
			int from = p.Choices.IndexOf(ch), to = from + direction;
			if(to < 0 || to >= p.Choices.Count) return;
			p.Choices.RemoveAt(from);
			p.Choices.Insert(to, ch);
			Changed(); fill(ch);
		}

		private string ChoiceTitle(UsdfConversation c, UsdfChoice ch, int index)
		{
			string text = string.IsNullOrEmpty(ch.Text) ? "(no text)" : ch.Text;
			string next = ch.CloseDialog ? "closes" : ch.NextPage != null ? "→ " + PageLabel(c, ch.NextPage) : "";
			return (index + 1) + ". " + text + (next.Length > 0 ? "   " + next : "");
		}

		private void RefreshChoiceTitles(UsdfConversation c, UsdfPage p, ListBox choices)
		{
			if(!(choices.ItemsSource is List<ListBoxItem> items)) return;
			for(int i = 0; i < items.Count && i < p.Choices.Count; i++) items[i].Content = ChoiceTitle(c, p.Choices[i], i);
		}

		private Control ChoiceEditor(UsdfConversation c, UsdfPage p, UsdfChoice ch, Action titles)
		{
			StackPanel panel = new StackPanel { Spacing = 8 };

			Grid grid = FieldGrid();
			TextBox text = new TextBox { Text = ch.Text ?? "", Watermark = "What the player says" };
			text.TextChanged += (s, e) => { if(loading) return; ch.Text = text.Text; Changed(); titles(); };
			AddRow(grid, "Text:", text, "The choice as the player sees it.");
			ComboBox next = PageCombo(c, ch.NextPage, "(stay on this page)");
			next.SelectionChanged += (s, e) => { if(loading) return; ch.NextPage = PageValue(c, next); Changed(); titles(); };
			AddRow(grid, "Then go to:", next, "The page shown after this choice succeeds.");
			CheckBox close = new CheckBox { Content = "Close the conversation", IsChecked = ch.CloseDialog };
			close.IsCheckedChanged += (s, e) => { if(loading) return; ch.CloseDialog = close.IsChecked == true; Changed(); titles(); };
			AddRow(grid, "", close, null);
			panel.Children.Add(Group("Choice", grid));

			// What it costs and who can pick it
			StackPanel cost = new StackPanel { Spacing = 6 };
			cost.Children.Add(new ItemChecksEditor(ch.Costs, classnames, ItemValue, Changed));
			CheckBox display = new CheckBox { Content = "Show the cost with the choice", IsChecked = ch.DisplayCost };
			display.IsCheckedChanged += (s, e) => { if(loading) return; ch.DisplayCost = display.IsChecked == true; Changed(); };
			cost.Children.Add(display);
			Grid messages = FieldGrid();
			TextBox yes = new TextBox { Text = ch.YesMessage ?? "", Watermark = "When the player can pay" };
			yes.TextChanged += (s, e) => { if(loading) return; ch.YesMessage = yes.Text; Changed(); };
			AddRow(messages, "Yes message:", yes, "Printed when the choice succeeds.");
			TextBox no = new TextBox { Text = ch.NoMessage ?? "", Watermark = "When the player can't" };
			no.TextChanged += (s, e) => { if(loading) return; ch.NoMessage = no.Text; Changed(); };
			AddRow(messages, "No message:", no, "Printed when the player doesn't have what it costs.");
			cost.Children.Add(messages);
			panel.Children.Add(Group("Costs (taken from the player)", cost));

			if(doc.UsesClassNames)
			{
				StackPanel shown = new StackPanel { Spacing = 6 };
				shown.Children.Add(new TextBlock { Text = "Only shown when the player has:", Opacity = 0.75 });
				shown.Children.Add(new ItemChecksEditor(ch.Requires, classnames, ItemValue, Changed));
				shown.Children.Add(new TextBlock { Text = "and doesn't have:", Opacity = 0.75 });
				shown.Children.Add(new ItemChecksEditor(ch.Excludes, classnames, ItemValue, Changed));
				panel.Children.Add(Group("Shown when", shown));
			}

			// What it does
			Grid does = FieldGrid();
			AutoCompleteBox give = ClassBox(ch.GiveItem?.Text ?? "");
			give.TextChanged += (s, e) => { if(loading) return; ch.GiveItem = ItemValue(give.Text); Changed(); };
			AddRow(does, "Gives:", give, "An item given to the player.");
			TextBox log = new TextBox { Text = ch.Log ?? "", Watermark = "Log entry" };
			log.TextChanged += (s, e) => { if(loading) return; ch.Log = log.Text; Changed(); };
			AddRow(does, "Log:", log, "A LOG entry (Strife's mission log).");
			panel.Children.Add(Group("Rewards", does));

			panel.Children.Add(Group("Action", ActionEditor(ch)));
			return panel;
		}

		// special + args, as on a linedef, with the action browser and the arguments' names
		private Control ActionEditor(UsdfChoice ch)
		{
			StackPanel panel = new StackPanel { Spacing = 6 };
			NumericUpDown number = new NumericUpDown { Minimum = 0, Maximum = 65535, Increment = 1, FormatString = "0", Value = ch.Special, Width = 110 };
			TextBlock title = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
			Button browse = new Button { Content = "…", MinWidth = 32, HorizontalContentAlignment = HorizontalAlignment.Center };
			ToolTip.SetTip(browse, "Pick an action");
			Grid args = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowSpacing = 4, ColumnSpacing = 10 };
			void ShowArgs()
			{
				args.Children.Clear();
				args.RowDefinitions.Clear();
				LinedefActionInfo info = ch.Special > 0 && General.Map?.Config?.LinedefActions != null && General.Map.Config.LinedefActions.TryGetValue(ch.Special, out LinedefActionInfo a) ? a : null;
				title.Text = ch.Special == 0 ? "None" : info != null ? info.Title : "Unknown action";
				for(int i = 0; i < 5; i++)
				{
					bool used = info == null ? ch.Special != 0 : info.Args[i].Used;
					if(!used && (ch.Args[i] == null || ch.Args[i].Text.Length == 0)) continue;
					args.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
					int row = args.RowDefinitions.Count - 1, index = i;
					TextBlock label = new TextBlock { Text = (info != null && info.Args[i].Used ? info.Args[i].Title : "Argument " + (i + 1)) + ":", VerticalAlignment = VerticalAlignment.Center, MinWidth = 120 };
					TextBox value = new TextBox { Text = ch.Args[i]?.Text ?? "", Width = 120, HorizontalAlignment = HorizontalAlignment.Left };
					value.TextChanged += (s, e) =>
					{
						if(loading) return;
						string v = (value.Text ?? "").Trim();
						ch.Args[index] = v.Length == 0 ? null : int.TryParse(v, out int n) ? UsdfValue.Of(n) : UsdfValue.Of(v);
						Changed();
					};
					Grid.SetRow(label, row); Grid.SetRow(value, row); Grid.SetColumn(value, 1);
					args.Children.Add(label); args.Children.Add(value);
				}
			}
			number.ValueChanged += (s, e) => { if(loading) return; ch.Special = (int)(number.Value ?? 0); Changed(); ShowArgs(); };
			browse.Click += (s, e) =>
			{
				int picked = CodeImp.DoomBuilder.Windows.ActionBrowserForm.BrowseAction(null, ch.Special);
				if(picked != ch.Special) { ch.Special = picked; number.Value = picked; Changed(); ShowArgs(); }
			};
			StackPanel top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
			top.Children.Add(number);
			top.Children.Add(browse);
			top.Children.Add(title);
			panel.Children.Add(top);
			if(doc.UsesClassNames)
			{
				TextBox specialname = new TextBox { Text = ch.SpecialName ?? "", Watermark = "or an action by name (e.g. ACS_NamedExecute)" };
				specialname.TextChanged += (s, e) => { if(loading) return; ch.SpecialName = specialname.Text; Changed(); };
				panel.Children.Add(specialname);
			}
			panel.Children.Add(args);
			ShowArgs();
			return panel;
		}

		#endregion

		#region ================== Pages in combo boxes

		private string PageLabel(UsdfConversation c, UsdfValue v)
		{
			if(v == null) return "";
			if(v.Kind == UsdfValue.ValueKind.Integer)
			{
				int n = v.AsInt();
				if(n >= 1 && n <= c.Pages.Count) return PageTitle(c, c.Pages[n - 1]);
				return "page " + n + " (missing)";
			}
			UsdfPage named = c.Pages.FirstOrDefault(x => string.Equals(x.PageName, v.Text, StringComparison.OrdinalIgnoreCase));
			return named != null ? PageTitle(c, named) : "\"" + v.Text + "\" (missing)";
		}

		private sealed class PageItem
		{
			public UsdfValue Value;
			public string Label;
			public override string ToString() => Label;
		}

		private ComboBox PageCombo(UsdfConversation c, UsdfValue current, string none)
		{
			List<PageItem> items = new List<PageItem> { new PageItem { Value = null, Label = none } };
			for(int i = 0; i < c.Pages.Count; i++)
			{
				UsdfPage p = c.Pages[i];
				// GZDoom: link by name when the page has one (stays right when pages move)
				UsdfValue v = doc.IsGZDoom && !string.IsNullOrEmpty(p.PageName) ? UsdfValue.Of(p.PageName) : UsdfValue.Of(i + 1);
				items.Add(new PageItem { Value = v, Label = PageTitle(c, p) });
			}
			int selectedindex = 0;
			if(current != null && current.Text.Length > 0)
			{
				int found = items.FindIndex(it => it.Value != null && Same(c, it.Value, current));
				if(found < 0) { items.Add(new PageItem { Value = current, Label = PageLabel(c, current) }); found = items.Count - 1; }
				selectedindex = found;
			}
			ComboBox box = new ComboBox { ItemsSource = items, SelectedIndex = selectedindex, HorizontalAlignment = HorizontalAlignment.Stretch };
			return box;
		}

		// Two page references to the same page (a number and a name can be)
		private static bool Same(UsdfConversation c, UsdfValue a, UsdfValue b)
		{
			int Index(UsdfValue v) => v.Kind == UsdfValue.ValueKind.Integer ? v.AsInt() : c.Pages.FindIndex(p => string.Equals(p.PageName, v.Text, StringComparison.OrdinalIgnoreCase)) + 1;
			return Index(a) > 0 && Index(a) == Index(b);
		}

		private static UsdfValue PageValue(UsdfConversation c, ComboBox box) => (box.SelectedItem as PageItem)?.Value;

		#endregion

		#region ================== Checks

		private void Changed()
		{
			if(loading) return;
			changed = true;
			CheckSoon();
		}

		private void CheckSoon()
		{
			if(checktimer == null)
			{
				checktimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
				checktimer.Tick += (s, e) => { checktimer.Stop(); ShowProblems(); };
			}
			checktimer.Stop();
			checktimer.Start();
		}

		private void ShowSelectedSoon() { Dispatcher.UIThread.Post(ShowSelected, DispatcherPriority.Background); }

		private void ShowProblems()
		{
			List<string> found = DialogueChecks.Check(doc, classnames);
			problems.Text = found.Count == 0 ? "" : (found.Count == 1 ? "1 problem: " : found.Count + " problems: ") + string.Join("  ·  ", found.Take(4)) + (found.Count > 4 ? "  ·  …" : "");
			problems.IsVisible = found.Count > 0;
		}

		#endregion

		#region ================== Helpers

		// Class names the map knows: thing types and the actors of its DECORATE/ZScript
		private static List<string> KnownClassNames()
		{
			HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			try
			{
				if(General.Map?.Data != null)
				{
					foreach(ThingTypeInfo t in General.Map.Data.ThingTypes) if(!string.IsNullOrEmpty(t.ClassName)) names.Add(t.ClassName);
					System.Reflection.FieldInfo field = typeof(CodeImp.DoomBuilder.Data.DataManager).GetField("zdoomclasses", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					if(field?.GetValue(General.Map.Data) is Dictionary<string, CodeImp.DoomBuilder.ZDoom.ActorStructure> actors)
						foreach(CodeImp.DoomBuilder.ZDoom.ActorStructure a in actors.Values) if(!string.IsNullOrEmpty(a.ClassName)) names.Add(a.ClassName);
				}
			}
			catch(Exception) { }
			return names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
		}

		private static List<Thing> ThingsUsing(int id)
		{
			List<Thing> result = new List<Thing>();
			if(General.Map?.Map == null) return result;
			foreach(Thing t in General.Map.Map.Things) if(t.Fields.GetValue("conversation", 0) == id) result.Add(t);
			return result;
		}

		// An actor or item: a class name in the ZDoom/GZDoom namespaces, a number in Strife's
		private UsdfValue ItemValue(string text)
		{
			text = (text ?? "").Trim();
			if(text.Length == 0) return null;
			if(!doc.UsesClassNames && int.TryParse(text, out int n)) return UsdfValue.Of(n);
			return UsdfValue.Of(text);
		}

		private AutoCompleteBox ClassBox(string text)
		{
			return new AutoCompleteBox
			{
				Text = text,
				ItemsSource = classnames,
				FilterMode = AutoCompleteFilterMode.ContainsOrdinal,
				MinimumPrefixLength = 1,
				MaxDropDownHeight = 240,
				Watermark = doc.UsesClassNames ? "Class name" : "Conversation ID",
				HorizontalAlignment = HorizontalAlignment.Stretch,
			};
		}

		private static Grid FieldGrid() => new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowSpacing = 6, ColumnSpacing = 10 };

		private static void AddRow(Grid grid, string label, Control control, string tip)
		{
			grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			int row = grid.RowDefinitions.Count - 1;
			TextBlock text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, MinWidth = 96 };
			text.Classes.Add("uldb-field");
			Grid.SetRow(text, row);
			Grid.SetRow(control, row);
			Grid.SetColumn(control, 1);
			if(!string.IsNullOrEmpty(tip)) { ToolTip.SetTip(text, tip); ToolTip.SetTip(control, tip); }
			grid.Children.Add(text);
			grid.Children.Add(control);
		}

		private static Control Labelled(string label, Control control)
		{
			DockPanel row = new DockPanel();
			TextBlock text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
			DockPanel.SetDock(text, Dock.Left);
			row.Children.Add(text);
			row.Children.Add(control);
			return row;
		}

		private static Border Group(string title, Control content)
		{
			StackPanel stack = new StackPanel { Spacing = 8 };
			TextBlock t = new TextBlock { Text = title };
			t.Classes.Add("uldb-group-title");
			stack.Children.Add(t);
			stack.Children.Add(content);
			Border b = new Border { Child = stack };
			b.Classes.Add("uldb-group");
			return b;
		}

		private static Button SmallButton(string text, string tip, Action click)
		{
			Button b = new Button { Content = text, Margin = new Thickness(0, 0, 6, 6), Padding = new Thickness(10, 4) };
			ToolTip.SetTip(b, tip);
			b.Click += (s, e) => click();
			return b;
		}

		#endregion
	}

	/// <summary>Rows of item + amount (ifitem, cost, require, exclude), with add and remove.</summary>
	internal sealed class ItemChecksEditor : UserControl
	{
		private readonly List<UsdfItemCheck> checks;
		private readonly List<string> classnames;
		private readonly Func<string, UsdfValue> tovalue;
		private readonly Action changed;
		private readonly StackPanel rows = new StackPanel { Spacing = 4 };

		public ItemChecksEditor(List<UsdfItemCheck> checks, List<string> classnames, Func<string, UsdfValue> tovalue, Action changed)
		{
			this.checks = checks;
			this.classnames = classnames;
			this.tovalue = tovalue;
			this.changed = changed;
			StackPanel panel = new StackPanel { Spacing = 4 };
			panel.Children.Add(rows);
			Button add = new Button { Content = "+ Item", Padding = new Thickness(10, 3), HorizontalAlignment = HorizontalAlignment.Left };
			add.Click += (s, e) => { checks.Add(new UsdfItemCheck()); changed(); Fill(); };
			panel.Children.Add(add);
			Content = panel;
			Fill();
		}

		private void Fill()
		{
			rows.Children.Clear();
			foreach(UsdfItemCheck check in checks)
			{
				UsdfItemCheck c = check;
				Grid row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 6 };
				AutoCompleteBox item = new AutoCompleteBox { Text = c.Item?.Text ?? "", ItemsSource = classnames, FilterMode = AutoCompleteFilterMode.ContainsOrdinal, MinimumPrefixLength = 1, MaxDropDownHeight = 240, Watermark = "Item" };
				item.TextChanged += (s, e) => { c.Item = tovalue(item.Text) ?? UsdfValue.Of(""); changed(); };
				NumericUpDown amount = new NumericUpDown { Minimum = 0, Maximum = 999999, Increment = 1, FormatString = "0", Value = c.Amount, Width = 110 };
				amount.ValueChanged += (s, e) => { c.Amount = (int)(amount.Value ?? 0); changed(); };
				ToolTip.SetTip(amount, "Amount");
				Button remove = new Button { Content = "✕", Padding = new Thickness(8, 3) };
				ToolTip.SetTip(remove, "Remove");
				remove.Click += (s, e) => { checks.Remove(c); changed(); Fill(); };
				Grid.SetColumn(amount, 1);
				Grid.SetColumn(remove, 2);
				row.Children.Add(item); row.Children.Add(amount); row.Children.Add(remove);
				rows.Children.Add(row);
			}
			if(checks.Count == 0) rows.Children.Add(new TextBlock { Text = "None", Opacity = 0.6 });
		}
	}
}
