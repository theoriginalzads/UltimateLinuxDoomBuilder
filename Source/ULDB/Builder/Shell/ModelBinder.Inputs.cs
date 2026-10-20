// ULDB: two-way bindings for ModelBinder: input controls in a view drive the model-ported
// WinForms controls of the same name, so upstream's handlers (TextChanged, CheckedChanged,
// SelectedIndexChanged, Click, KeyPress filtering, Validating, …) run exactly as they would in
// WinForms, and whatever upstream sets on the models shows in the view.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ULDB.Input;
using WF = System.Windows.Forms;

namespace ULDB.Shell
{
	internal sealed partial class ModelBinder
	{
		/// <summary>Binds an input control to its model; false when this isn't an input pair.</summary>
		private bool BindInput(Control view, object model)
		{
			switch(view)
			{
				case TextBox tb when model is WF.TextBoxBase m: BindTextBox(tb, m); return true;
				case CheckBox cb when model is WF.CheckBox m: BindCheckBox(cb, m); return true;
				case RadioButton rb when model is WF.RadioButton m: BindRadioButton(rb, m); return true;
				case Button link when model is WF.LinkLabel m: BindLink(link, m); return true;
				case ContentControl links when model is WF.LinkLabel m && !(view is Button): BindLinks(links, m); return true;
				case Slider slider when model is WF.TrackBar m: BindTrackBar(slider, m); return true;
				case ListBox checks when model is WF.CheckedListBox m: BindCheckedListBox(checks, m); return true;
				case ListBox list when model is WF.ListBox m: BindListBox(list, m); return true;
				case ProgressBar bar when model is WF.ProgressBar m: BindProgressBar(bar, m); return true;
				case ComboBox combo when model is WF.ComboBox m: BindComboBox(combo, m); return true;
				case NumericUpDown num when model is WF.NumericUpDown m: BindNumericUpDown(num, m); return true;
				case TabControl tabs when model is WF.TabControl m: BindTabs(tabs, m); return true;
				case TabItem _ when model is WF.TabPage: return true; // bound by BindTabs
				case Button button when model is WF.Button m: BindButton(button, m); return true;
				case ContentControl host when model is WF.Control m && !(view is Button):
					Control content = ControlViews.Create(m);
					if(content == null) return false;
					host.Content = content;
					BindState(host, m);
					return true;
				default:
					return false;
			}
		}

		/// <summary>Visibility, enabled state and tooltip.</summary>
		public ModelBinder BindState(Control view, WF.Control m)
		{
			return Custom(() =>
			{
				view.IsVisible = m.Visible;
				view.IsEnabled = m.Enabled;
				ApplyTip(view, m);
			}, m);
		}

		/// <summary>Shows the tooltip upstream gave the model (ToolTip.SetToolTip), if any.</summary>
		public static void ApplyTip(Control view, WF.Control m)
		{
			if(m.UldbToolTip != null) ToolTip.SetTip(view, m.UldbToolTip);
		}

		private static string Mnemonicless(string text) => (text ?? "").Replace("&&", "\u0001").Replace("&", "").Replace("\u0001", "&");

		private static object ImageOrText(WF.Control m, System.Drawing.Image image, string text)
		{
			if(image != null && string.IsNullOrEmpty(text?.Trim()))
				return Picture(image);
			if(image != null)
			{
				StackPanel both = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 4 };
				both.Children.Add(Picture(image));
				both.Children.Add(new TextBlock { Text = text, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
				return both;
			}
			return text;
		}

		// ULDB's Breeze-style icon when the image is a known resource image, else the bitmap itself.
		private static Control Picture(System.Drawing.Image image)
		{
			return BreezeIcons.ForImage(image, Math.Min(image.Width, image.Height))
				?? new Image { Source = AvaloniaImages.From(image), Width = image.Width, Height = image.Height };
		}

		public ModelBinder BindRadioButton(RadioButton view, WF.RadioButton m)
		{
			// The model checks itself and unchecks its siblings; the view shows the result.
			view.Click += (s, e) => { m.RaiseClick(); Schedule(s, e); };
			bool hascontent = view.Content != null;
			System.Drawing.Image shown = null;
			return Custom(() =>
			{
				if(view.IsChecked != m.Checked) view.IsChecked = m.Checked;
				if(!hascontent && (m.Image != shown || view.Content == null))
				{
					shown = m.Image;
					view.Content = ImageOrText(m, m.Image, Mnemonicless(m.Text));
				}
				view.IsVisible = m.Visible;
				view.IsEnabled = m.Enabled;
				ApplyTip(view, m);
			}, m);
		}

		/// <summary>
		/// A LinkLabel with links inside its text (upstream's TagsSelector lists tags this way):
		/// plain runs and link buttons in a wrap panel; clicking one raises LinkClicked for it.
		/// </summary>
		public ModelBinder BindLinks(ContentControl view, WF.LinkLabel m)
		{
			string shown = null;
			return Custom(() =>
			{
				string sig = m.Text + "|" + string.Join(",", m.Links.Select(l => l.Start + ":" + l.Length));
				if(sig != shown)
				{
					shown = sig;
					WrapPanel panel = new WrapPanel { Orientation = Avalonia.Layout.Orientation.Horizontal };
					string text = m.Text ?? "";
					List<WF.LinkLabel.Link> links = m.Links.Count > 0 ? m.Links.OrderBy(l => l.Start).ToList() : new List<WF.LinkLabel.Link> { new WF.LinkLabel.Link(0, text.Length) };
					int pos = 0;
					void Plain(string t) { if(t.Length > 0) panel.Children.Add(new TextBlock { Text = t, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center }); }
					foreach(WF.LinkLabel.Link link in links)
					{
						int start = Math.Clamp(link.Start, pos, text.Length);
						int end = Math.Clamp(link.Start + link.Length, start, text.Length);
						Plain(text.Substring(pos, start - pos));
						Button b = new Button { Content = text.Substring(start, end - start) };
						b.Classes.Add("uldb-link");
						WF.LinkLabel.Link target = m.Links.Count > 0 ? link : null;
						b.Click += (s, e) => m.RaiseLinkClicked(target);
						panel.Children.Add(b);
						pos = end;
					}
					Plain(text.Substring(Math.Min(pos, text.Length)));
					view.Content = panel;
				}
				view.IsVisible = m.Visible;
				view.IsEnabled = m.Enabled;
				ApplyTip(view, m);
			}, m);
		}

		public ModelBinder BindTrackBar(Slider view, WF.TrackBar m)
		{
			bool updating = false;
			view.ValueChanged += (s, e) =>
			{
				if(updating) return;
				int v = (int)Math.Round(view.Value);
				if(v != m.Value) m.RaiseScroll(v);
			};
			return Custom(() =>
			{
				updating = true;
				try
				{
					view.Minimum = m.Minimum;
					view.Maximum = m.Maximum;
					view.SmallChange = m.SmallChange;
					view.LargeChange = m.LargeChange;
					view.TickFrequency = m.TickFrequency;
					view.IsSnapToTickEnabled = true;
					if((int)Math.Round(view.Value) != m.Value) view.Value = m.Value;
					view.IsVisible = m.Visible;
					view.IsEnabled = m.Enabled;
					ApplyTip(view, m);
				}
				finally { updating = false; }
			}, m);
		}

		/// <summary>A LinkLabel as a link-styled button.</summary>
		public ModelBinder BindLink(Button view, WF.LinkLabel m)
		{
			view.Classes.Add("uldb-link");
			view.Click += (s, e) => m.RaiseLinkClicked();
			return Custom(() =>
			{
				view.Content = m.Text;
				view.IsVisible = m.Visible;
				view.IsEnabled = m.Enabled;
				ApplyTip(view, m);
			}, m);
		}

		#region ================== Lists and progress

		private const int ListRowHeight = 16; // Model coordinates only: item i is at y = i * ListRowHeight

		/// <summary>
		/// A WinForms ListBox: its items (as text), selection both ways, and mouse and keys at item
		/// positions (IndexFromPoint answers through the model's ItemLocator), so upstream's handlers run.
		/// </summary>
		public ModelBinder BindListBox(ListBox view, WF.ListBox m)
		{
			bool updating = false;
			string shown = null;
			m.ItemLocator = p => p.Y >= 0 && p.Y / ListRowHeight < m.Items.Count ? p.Y / ListRowHeight : -1;
			view.SelectionChanged += (s, e) =>
			{
				if(updating) return;
				m.SetSelectionFromView(Enumerable.Range(0, m.Items.Count).Where(i => view.Selection.IsSelected(i)).ToList());
				Schedule(s, e);
			};

			WF.MouseEventArgs At(int index, WF.MouseButtons button, int clicks) =>
				new WF.MouseEventArgs(button, clicks, 4, index * ListRowHeight + ListRowHeight / 2, 0);
			view.DoubleTapped += (s, e) =>
			{
				if(view.SelectedIndex < 0) return;
				m.RaiseMouseDoubleClick(At(view.SelectedIndex, WF.MouseButtons.Left, 2));
				m.RaiseDoubleClick();
				Schedule(s, e);
			};
			view.AddHandler(InputElement.PointerReleasedEvent, (s, e) =>
			{
				WF.MouseButtons button = e.InitialPressMouseButton == MouseButton.Right ? WF.MouseButtons.Right : WF.MouseButtons.Left;
				m.RaiseMouseUp(At(Math.Max(0, view.SelectedIndex), button, 1));
				Schedule(s, e);
			}, RoutingStrategies.Bubble, true);
			RouteKeyboard(view, m);

			return Custom(() =>
			{
				view.SelectionMode = m.SelectionMode == WF.SelectionMode.One ? SelectionMode.Single : SelectionMode.Multiple;
				List<string> items = new List<string>(m.Items.Count);
				foreach(object o in m.Items) items.Add(o?.ToString() ?? "");
				string sig = string.Join("\n", items);
				updating = true;
				if(sig != shown) { shown = sig; view.ItemsSource = items; }
				view.Selection.Clear();
				foreach(int i in m.SelectedIndices) view.Selection.Select(i);
				updating = false;
				view.IsVisible = m.Visible;
				view.IsEnabled = m.Enabled;
				ApplyTip(view, m);
			}, m);
		}

		/// <summary>A CheckedListBox: a checkbox (three-state) and text per row; a click goes to the model, whose ItemCheck may change it.</summary>
		public ModelBinder BindCheckedListBox(ListBox view, WF.CheckedListBox m)
		{
			string shown = null;
			view.SelectionMode = SelectionMode.Single;
			view.ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<(int index, string text, bool? check)>((row, _) =>
			{
				CheckBox box = new CheckBox { Content = row.text, IsThreeState = true, IsChecked = row.check };
				// The model decides the new state (ItemCheck); the view then redraws from it.
				box.AddHandler(InputElement.PointerPressedEvent, (s, e) =>
				{
					e.Handled = true;
					m.ToggleFromView(row.index);
					Schedule(s, e);
				}, RoutingStrategies.Tunnel);
				return box;
			}, true);
			return Custom(() =>
			{
				List<(int, string, bool?)> rows = new List<(int, string, bool?)>();
				for(int i = 0; i < m.Items.Count; i++)
				{
					WF.CheckState st = m.GetItemCheckState(i);
					rows.Add((i, m.Items[i]?.ToString() ?? "", st == WF.CheckState.Indeterminate ? (bool?)null : st == WF.CheckState.Checked));
				}
				string sig = string.Join("\n", rows.Select(r => r.Item2 + "|" + r.Item3));
				if(sig != shown) { shown = sig; view.ItemsSource = rows; }
				view.IsVisible = m.Visible;
				view.IsEnabled = m.Enabled;
				ApplyTip(view, m);
			}, m);
		}

		public ModelBinder BindProgressBar(ProgressBar view, WF.ProgressBar m)
		{
			return Custom(() =>
			{
				view.Minimum = m.Minimum;
				view.Maximum = m.Maximum;
				view.Value = m.Value;
				view.IsIndeterminate = m.Style == WF.ProgressBarStyle.Marquee;
				view.IsVisible = m.Visible;
			}, m);
		}

		#endregion

		#region ================== Keyboard and focus

		/// <summary>Routes keys, typed characters and focus to the model, like WinForms would.</summary>
		public static void RouteKeyboard(Control view, WF.Control m)
		{
			view.AddHandler(InputElement.KeyDownEvent, (s, e) =>
			{
				WF.KeyEventArgs args = new WF.KeyEventArgs(KeyMapping.ToKeys(e.Key, e.KeyModifiers));
				m.RaiseKeyDown(args);
				if(args.Handled || args.SuppressKeyPress) e.Handled = true;
			}, RoutingStrategies.Tunnel);
			view.AddHandler(InputElement.KeyUpEvent, (s, e) =>
			{
				WF.KeyEventArgs args = new WF.KeyEventArgs(KeyMapping.ToKeys(e.Key, e.KeyModifiers));
				m.RaiseKeyUp(args);
				if(args.Handled) e.Handled = true;
			}, RoutingStrategies.Tunnel);
			// Upstream filters typed characters in KeyPress (NumericTextbox); rejected ones never arrive.
			view.AddHandler(InputElement.TextInputEvent, (s, e) =>
			{
				if(string.IsNullOrEmpty(e.Text)) return;
				string accepted = "";
				foreach(char ch in e.Text)
				{
					WF.KeyPressEventArgs args = new WF.KeyPressEventArgs(ch);
					m.RaiseKeyPress(args);
					if(!args.Handled) accepted += args.KeyChar;
				}
				if(accepted.Length == 0) e.Handled = true;
				else e.Text = accepted;
			}, RoutingStrategies.Tunnel);
			view.GotFocus += (s, e) => m.RaiseGotFocus();
			view.LostFocus += (s, e) =>
			{
				m.RaiseLostFocus();
				m.RaiseValidation();
			};
		}

		#endregion

		#region ================== Inputs

		public ModelBinder BindTextBox(TextBox view, WF.TextBoxBase m)
		{
			bool fromview = false;
			WF.CharacterCasing Casing() => (m as WF.TextBox)?.CharacterCasing ?? WF.CharacterCasing.Normal;
			view.IsReadOnly = m.ReadOnly;
			view.AcceptsReturn = m.Multiline;
			view.TextWrapping = m.Multiline && m.WordWrap ? Avalonia.Media.TextWrapping.Wrap : Avalonia.Media.TextWrapping.NoWrap;
			view.TextChanged += (s, e) =>
			{
				string text = view.Text ?? "";
				// WinForms' CharacterCasing changes typed text (upstream upper-cases short texture names).
				if(Casing() == WF.CharacterCasing.Upper) text = text.ToUpperInvariant();
				else if(Casing() == WF.CharacterCasing.Lower) text = text.ToLowerInvariant();
				if(text != (view.Text ?? "")) { int caret = view.CaretIndex; view.Text = text; view.CaretIndex = caret; }
				if(text == m.Text) return;
				fromview = true;
				try { m.Text = text; }
				finally { fromview = false; }
			};
			view.GotFocus += (s, e) =>
			{
				// Upstream's AutoSelectTextbox selects everything on focus; show the model's selection.
				if(m.SelectionLength > 0) { view.SelectionStart = m.SelectionStart; view.SelectionEnd = m.SelectionStart + m.SelectionLength; }
			};
			RouteKeyboard(view, m);
			return Custom(() =>
			{
				if(!fromview && (view.Text ?? "") != m.Text) view.Text = m.Text;
				view.IsVisible = m.Visible;
				view.IsEnabled = m.Enabled;
				view.IsReadOnly = m.ReadOnly;
				SetForeground(view, m.ForeColor);
			}, m);
		}

		public ModelBinder BindCheckBox(CheckBox view, WF.CheckBox m)
		{
			// The model toggles (WinForms' AutoCheck, including three-state order); the view shows the result.
			view.Click += (s, e) =>
			{
				m.RaiseClick();
				Schedule(s, e);
			};
			return Custom(() =>
			{
				bool? want = m.CheckState == WF.CheckState.Indeterminate ? null : m.Checked;
				if(view.IsChecked != want) view.IsChecked = want;
				view.IsThreeState = m.ThreeState || m.CheckState == WF.CheckState.Indeterminate;
				if(!string.IsNullOrEmpty(m.Text)) view.Content = Mnemonicless(m.Text);
				view.IsVisible = m.Visible;
				view.IsEnabled = m.Enabled;
				ApplyTip(view, m);
			}, m);
		}

		public ModelBinder BindComboBox(ComboBox view, WF.ComboBox m)
		{
			bool updating = false;
			List<string> shown = null;
			m.ItemsChanged += (s, e) => Schedule(s, e);
			m.SelectedItemChanged += (s, e) => Schedule(s, e);
			view.IsTextSearchEnabled = true;
			view.SelectionChanged += (s, e) =>
			{
				if(updating || view.SelectedIndex == m.SelectedIndex || view.SelectedIndex >= m.Items.Count) return;
				m.RaiseSelectionChangeCommitted(view.SelectedIndex);
			};
			view.PropertyChanged += (s, e) =>
			{
				if(updating || e.Property != ComboBox.TextProperty || !view.IsEditable) return;
				string text = view.Text ?? "";
				if(text != m.Text) m.RaiseTextUpdate(text);
			};
			view.DropDownOpened += (s, e) => m.RaiseDropDown();
			view.DropDownClosed += (s, e) => m.RaiseDropDownClosed();
			RouteKeyboard(view, m);
			return Custom(() =>
			{
				updating = true;
				try
				{
					List<string> items = m.Items.Cast<object>().Select(m.GetItemText).ToList();
					if(shown == null || !shown.SequenceEqual(items))
					{
						shown = items;
						view.ItemsSource = items;
					}
					view.IsEditable = m.DropDownStyle != WF.ComboBoxStyle.DropDownList;
					if(view.SelectedIndex != m.SelectedIndex) view.SelectedIndex = m.SelectedIndex;
					if(view.IsEditable && (view.Text ?? "") != m.Text) view.Text = m.Text;
					view.IsVisible = m.Visible;
					view.IsEnabled = m.Enabled;
				}
				finally { updating = false; }
			}, m);
		}

		public ModelBinder BindNumericUpDown(NumericUpDown view, WF.NumericUpDown m)
		{
			bool updating = false;
			view.ValueChanged += (s, e) =>
			{
				if(updating || view.Value == null || view.Value.Value == m.Value) return;
				m.Value = Math.Max(m.Minimum, Math.Min(m.Maximum, view.Value.Value));
			};
			RouteKeyboard(view, m);
			return Custom(() =>
			{
				updating = true;
				try
				{
					view.Minimum = m.Minimum;
					view.Maximum = m.Maximum;
					view.Increment = m.Increment;
					view.FormatString = "F" + m.DecimalPlaces;
					if(view.Value != m.Value) view.Value = m.Value;
					view.IsVisible = m.Visible;
					view.IsEnabled = m.Enabled;
				}
				finally { updating = false; }
			}, m);
		}

		public ModelBinder BindButton(Button view, WF.Button m)
		{
			view.Click += (s, e) => m.PerformClick();
			bool hascontent = view.Content != null;
			System.Drawing.Image shownimage = null;
			string showntext = null;
			return Custom(() =>
			{
				string text = Mnemonicless(m.Text);
				if(!hascontent && (m.Image != shownimage || text != showntext))
				{
					shownimage = m.Image;
					showntext = text;
					view.Content = ImageOrText(m, m.Image, text);
				}
				view.IsVisible = m.Visible;
				view.IsEnabled = m.Enabled;
				ApplyTip(view, m);
			}, m);
		}

		/// <summary>
		/// Tabs: each TabItem named like an upstream TabPage shows while that page is in the model's
		/// TabPages (upstream removes pages it doesn't need); selection is two-way; headers follow the
		/// pages' texts.
		/// </summary>
		public ModelBinder BindTabs(TabControl view, WF.TabControl m)
		{
			bool updating = false;
			List<(TabItem item, WF.TabPage page)> pairs = new List<(TabItem, WF.TabPage)>();
			foreach(TabItem item in view.Items.OfType<TabItem>())
				if(!string.IsNullOrEmpty(item.Name) && HasField(item.Name) && Field<object>(item.Name) is WF.TabPage page)
					pairs.Add((item, page));

			m.TabPagesChanged += (s, e) => Schedule(s, e);
			m.SelectedIndexChanged += (s, e) => Schedule(s, e);
			view.SelectionChanged += (s, e) =>
			{
				if(updating || !(view.SelectedItem is TabItem item)) return;
				(TabItem item, WF.TabPage page) pair = pairs.FirstOrDefault(p => p.item == item);
				int index = pair.page != null ? m.TabPages.IndexOf(pair.page) : -1;
				if(index >= 0 && index != m.SelectedIndex) m.SelectedIndex = index;
			};
			return Custom(() =>
			{
				updating = true;
				try
				{
					foreach((TabItem item, WF.TabPage page) in pairs)
					{
						item.IsVisible = m.TabPages.Contains(page);
						if(!string.IsNullOrEmpty(page.Text)) item.Header = page.Text.Trim();
					}
					WF.TabPage selected = m.SelectedTab;
					TabItem want = pairs.FirstOrDefault(p => p.page == selected).item;
					if(want != null && view.SelectedItem != want) view.SelectedItem = want;
					view.IsVisible = m.Visible;
					view.IsEnabled = m.Enabled;
				}
				finally { updating = false; }
			}, m);
		}

		#endregion

		/// <summary>Colours for input text (TextBox): like SetForeground for labels.</summary>
		private static void SetForeground(Avalonia.Controls.Primitives.TemplatedControl target, System.Drawing.Color c)
		{
			bool red = !c.IsEmpty && !c.IsKnownColor && c.R > 0x7F && c.G < 0x60 && c.B < 0x60 || c == System.Drawing.Color.Red || c == System.Drawing.Color.DarkRed;
			target.Classes.Set("uldb-red", red);
		}
	}
}
