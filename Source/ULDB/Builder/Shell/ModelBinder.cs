// ULDB: shows the state of model-ported WinForms controls (upstream code running on ULDB.Compat
// models) in Avalonia controls. Upstream fills labels and picture boxes as it always has; the
// binder copies text, visibility, enabled state, colours and images across whenever one of the
// watched controls changes, at most once per dispatcher turn.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using SD = System.Drawing;
using WF = System.Windows.Forms;

namespace ULDB.Shell
{
	internal sealed class ModelBinder
	{
		private readonly object model;
		private readonly List<Action> updates = new List<Action>();
		private readonly HashSet<WF.Control> watched = new HashSet<WF.Control>();
		private bool pending;
		private static readonly bool trace = Environment.GetEnvironmentVariable("ULDB_TRACE_BINDER") == "1";

		/// <summary>Binds controls of <paramref name="model"/> (fields found by their upstream names).</summary>
		public ModelBinder(object model)
		{
			this.model = model;
		}

		/// <summary>One of the model's fields, by its upstream (designer) name.</summary>
		public T Field<T>(string name) where T : class
		{
			for(Type t = model.GetType(); t != null; t = t.BaseType)
			{
				FieldInfo f = t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
				if(f != null) return (T)f.GetValue(model);
			}
			throw new MissingFieldException(model.GetType().Name, name);
		}

		/// <summary>Raised after the bound controls were updated.</summary>
		public event Action Updated;

		/// <summary>Watches a model control: any change to it schedules an update.</summary>
		public WF.Control Watch(WF.Control c)
		{
			if(c != null && watched.Add(c))
			{
				c.TextChanged += Schedule;
				c.VisibleChanged += Schedule;
				c.EnabledChanged += Schedule;
				c.StylesChanged += Schedule;
				c.Refreshed += Schedule;
			}
			return c;
		}

		/// <summary>Runs <paramref name="update"/> on every update, after watching the given controls.</summary>
		public ModelBinder Custom(Action update, params WF.Control[] watch)
		{
			foreach(WF.Control c in watch) Watch(c);
			updates.Add(update);
			return this;
		}

		/// <summary>Text, visibility, enabled state and colour of a label.</summary>
		public ModelBinder Text(TextBlock target, string field) => Text(target, Field<WF.Control>(field));

		public ModelBinder Text(TextBlock target, WF.Control c)
		{
			return Custom(() =>
			{
				target.Text = c.Text.Trim();
				target.IsVisible = c.Visible;
				target.Classes.Set("uldb-disabled", !c.Enabled); // dimmed by Styles.axaml
				SetForeground(target, c.ForeColor);
			}, c);
		}

		/// <summary>Shows <paramref name="target"/> only while the model control is visible.</summary>
		public ModelBinder Visible(Control target, string field) => Visible(target, Field<WF.Control>(field));

		public ModelBinder Visible(Control target, WF.Control c) => Custom(() => target.IsVisible = c.Visible, c);

		/// <summary>Dims <paramref name="target"/> while the model control is disabled.</summary>
		public ModelBinder Enabled(Control target, string field)
		{
			WF.Control c = Field<WF.Control>(field);
			return Custom(() => target.Opacity = c.Enabled ? 1.0 : 0.45, c);
		}

		/// <summary>A picture box's image (or a control's background image).</summary>
		public ModelBinder Image(Image target, string field)
		{
			WF.Control c = Field<WF.Control>(field);
			return Custom(() =>
			{
				SD.Image image = (c as WF.PictureBox)?.Image ?? c.BackgroundImage;
				target.Source = AvaloniaImages.From(image);
			}, c);
		}

		/// <summary>A control's background colour as a swatch.</summary>
		public ModelBinder Swatch(Border target, string field)
		{
			WF.Control c = Field<WF.Control>(field);
			return Custom(() =>
			{
				SD.Color col = c.BackColor;
				target.Background = new SolidColorBrush(Color.FromArgb(col.A == 0 ? (byte)255 : col.A, col.R, col.G, col.B));
				target.IsVisible = c.Visible;
			}, c);
		}

		/// <summary>
		/// Binds every named control under <paramref name="root"/> to the model field of the same name:
		/// TextBlock → label text, state and colour; "<i>name</i>Title" TextBlock → a group's text;
		/// Image → picture box image; Border with class uldb-swatch → background colour;
		/// ItemsControl → a ListView's items (as <see cref="ListItem"/>s); anything else → visibility.
		/// Also watches the model itself, so Show()/Refresh() on it updates the view.
		/// </summary>
		public ModelBinder BindByName(Control root)
		{
			if(model is WF.Control self) Watch(self);
			foreach(ILogical logical in root.GetLogicalDescendants().Prepend(root))
			{
				if(!(logical is Control c) || string.IsNullOrEmpty(c.Name)) continue;
				string name = c.Name;
				if(trace) Console.WriteLine($"[binder] {model.GetType().Name}.{name} ({c.GetType().Name}) field={HasField(name)}");
				if(c is TextBlock title && name.EndsWith("Title") && HasField(name.Substring(0, name.Length - 5)))
					Text(title, Field<WF.Control>(name.Substring(0, name.Length - 5)));
				else if(!HasField(name)) continue;
				else if(c is TextBlock tb) Text(tb, name);
				else if(c is Image img) Image(img, name);
				else if(c is Border sw && sw.Classes.Contains("uldb-swatch")) Swatch(sw, name);
				else if(c is ItemsControl items && Field<object>(name) is WF.ListView list) Items(items, list);
				else Visible(c, name);
			}
			return this;
		}

		/// <summary>One ListView item, as shown by a view.</summary>
		public sealed class ListItem
		{
			public string Text { get; init; }
			public bool Checked { get; init; }
			public bool Hot { get; init; }
		}

		public ModelBinder Items(ItemsControl target, WF.ListView list)
		{
			return Custom(() =>
			{
				target.ItemsSource = list.Items.Select(i => new ListItem
				{
					Text = i.Text,
					Checked = i.Checked,
					Hot = i.ForeColor.IsKnownColor && i.ForeColor.ToKnownColor() == SD.KnownColor.HotTrack,
				}).ToList();
				target.IsVisible = list.Visible;
			}, list);
		}

		private bool HasField(string name)
		{
			for(Type t = model.GetType(); t != null; t = t.BaseType)
				if(t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly) != null) return true;
			return false;
		}

		/// <summary>Applies every binding now.</summary>
		public void UpdateNow()
		{
			pending = false;
			foreach(Action update in updates) update();
			Updated?.Invoke();
		}

		private void Schedule(object sender, EventArgs e)
		{
			if(pending) return;
			pending = true;
			Dispatcher.UIThread.Post(UpdateNow, DispatcherPriority.Render);
		}

		/// <summary>Upstream's text colours, mapped to the theme: normal text keeps the theme's
		/// colour, HotTrack (highlighted) gets class uldb-hot, reds get uldb-red (Styles.axaml).</summary>
		public static void SetForeground(TextBlock target, SD.Color c)
		{
			bool hot = c.IsKnownColor && c.ToKnownColor() == SD.KnownColor.HotTrack;
			bool red = !c.IsEmpty && !c.IsKnownColor && c.R > 0x7F && c.G < 0x60 && c.B < 0x60 || c == SD.Color.Red || c == SD.Color.DarkRed;
			target.Classes.Set("uldb-hot", hot);
			target.Classes.Set("uldb-red", red);
			if(hot || red || c.IsEmpty || IsThemeText(c)) target.ClearValue(TextBlock.ForegroundProperty);
			else target.Foreground = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
		}

		private static bool IsThemeText(SD.Color c)
		{
			if(!c.IsKnownColor) return false;
			switch(c.ToKnownColor())
			{
				case SD.KnownColor.ControlText:
				case SD.KnownColor.WindowText:
				case SD.KnownColor.GrayText:
				case SD.KnownColor.InfoText:
				case SD.KnownColor.MenuText:
					return true;
				default:
					return false;
			}
		}
	}
}
