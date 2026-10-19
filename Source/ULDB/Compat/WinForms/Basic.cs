// ULDB compatibility layer: non-visual System.Windows.Forms types used by the editor core.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace System.Windows.Forms
{
	/// <summary>Marker for "a window that can own dialogs". Avalonia windows in ULDB implement it.</summary>
	public interface IWin32Window
	{
		IntPtr Handle { get; }
	}

	public class MouseEventArgs : EventArgs
	{
		public MouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta)
		{
			Button = button;
			Clicks = clicks;
			X = x;
			Y = y;
			Delta = delta;
		}

		public MouseButtons Button { get; }
		public int Clicks { get; }
		public int X { get; }
		public int Y { get; }
		public int Delta { get; }
		public Point Location => new Point(X, Y);
	}

	public delegate void MouseEventHandler(object sender, MouseEventArgs e);

	public class KeyEventArgs : EventArgs
	{
		public KeyEventArgs(Keys keyData) { KeyData = keyData; }

		public Keys KeyData { get; }
		public Keys KeyCode => KeyData & Keys.KeyCode;
		public Keys Modifiers => KeyData & Keys.Modifiers;
		public int KeyValue => (int)(KeyData & Keys.KeyCode);
		public bool Control => (KeyData & Keys.Control) != 0;
		public bool Shift => (KeyData & Keys.Shift) != 0;
		public bool Alt => (KeyData & Keys.Alt) != 0;
		public bool Handled { get; set; }
		public bool SuppressKeyPress { get; set; }
	}

	public delegate void KeyEventHandler(object sender, KeyEventArgs e);

	public class KeyPressEventArgs : EventArgs
	{
		public KeyPressEventArgs(char keyChar) { KeyChar = keyChar; }
		public char KeyChar { get; set; }
		public bool Handled { get; set; }
	}

	public delegate void KeyPressEventHandler(object sender, KeyPressEventArgs e);

	public struct Padding : IEquatable<Padding>
	{
		public Padding(int all) : this(all, all, all, all) { }

		public Padding(int left, int top, int right, int bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		public int Left { get; set; }
		public int Top { get; set; }
		public int Right { get; set; }
		public int Bottom { get; set; }
		public int Horizontal => Left + Right;
		public int Vertical => Top + Bottom;

		public static readonly Padding Empty = new Padding(0);

		public bool Equals(Padding other) => Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
		public override bool Equals(object obj) => obj is Padding p && Equals(p);
		public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);
		public static bool operator ==(Padding a, Padding b) => a.Equals(b);
		public static bool operator !=(Padding a, Padding b) => !a.Equals(b);
	}

	/// <summary>Converts Keys values to display strings the way WinForms does ("Ctrl+Del", "PgUp", "F5").</summary>
	public class KeysConverter : TypeConverter
	{
		private static readonly Dictionary<Keys, string> NAMES = new Dictionary<Keys, string>
		{
			{ Keys.Return, "Enter" },
			{ Keys.Delete, "Del" },
			{ Keys.Insert, "Ins" },
			{ Keys.Home, "Home" },
			{ Keys.End, "End" },
			{ Keys.Prior, "PgUp" },
			{ Keys.Next, "PgDn" },
			{ Keys.None, "(none)" },
			{ Keys.D0, "0" }, { Keys.D1, "1" }, { Keys.D2, "2" }, { Keys.D3, "3" }, { Keys.D4, "4" },
			{ Keys.D5, "5" }, { Keys.D6, "6" }, { Keys.D7, "7" }, { Keys.D8, "8" }, { Keys.D9, "9" },
			// Several Keys members share a value; pin the names WinForms prints.
			{ Keys.Capital, "Capital" },
			{ Keys.Escape, "Escape" },
			{ Keys.Back, "Back" },
		};

		public string ConvertToString(Keys keys) => ConvertToString((object)keys);
		public new string ConvertToString(object value) => ConvertTo(null, CultureInfo.CurrentCulture, value, typeof(string)) as string;

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(destinationType != typeof(string) || value == null) return base.ConvertTo(context, culture, value, destinationType);

			Keys keys = value is Keys k ? k : (Keys)Convert.ToInt32(value, CultureInfo.InvariantCulture);
			Keys code = keys & Keys.KeyCode;
			StringBuilder sb = new StringBuilder();

			// Same modifier order as UDB's own shortcut descriptions (Action.GetShortcutKeyDesc).
			if((keys & Keys.Control) != 0) sb.Append("Ctrl+");
			if((keys & Keys.Alt) != 0) sb.Append("Alt+");
			if((keys & Keys.Shift) != 0) sb.Append("Shift+");

			if(code == Keys.None && sb.Length > 0) sb.Length--; // Modifiers only: drop the trailing "+"
			else sb.Append(NAMES.TryGetValue(code, out string name) ? name : code.ToString());

			return sb.ToString();
		}
	}

	public static class DataFormats
	{
		public const string Text = "Text";
		public const string UnicodeText = "UnicodeText";
		public const string FileDrop = "FileDrop";
		public const string Bitmap = "Bitmap";
	}

	/// <summary>Clipboard payload: a set of named formats.</summary>
	public class DataObject : IDataObject
	{
		internal readonly Dictionary<string, object> Formats = new Dictionary<string, object>(StringComparer.Ordinal);

		public DataObject() { }
		public DataObject(string format, object data) { SetData(format, data); }

		public void SetData(string format, object data) { Formats[format] = data; }
		public void SetData(string format, bool autoConvert, object data) { Formats[format] = data; }
		public void SetText(string text) { Formats[DataFormats.Text] = text; }
		public object GetData(string format) => Formats.TryGetValue(format, out object data) ? data : null;
		public bool GetDataPresent(string format) => Formats.ContainsKey(format);
		public string[] GetFormats() => new List<string>(Formats.Keys).ToArray();

		// Type-keyed overloads (IDataObject): the type's full name is the format, as in WinForms.
		public object GetData(Type format) => GetData(format.FullName);
		public bool GetDataPresent(Type format) => GetDataPresent(format.FullName);
		public void SetData(object data) { if(data != null) Formats[data.GetType().FullName] = data; }
	}

	public enum HorizontalAlignment
	{
		Left = 0,
		Right = 1,
		Center = 2
	}
}
