// ULDB compatibility layer: WinForms event arguments, window messages and data
// transfer types that upstream window code declares handlers and overrides with.
// The Avalonia UI raises the events; Message never carries a real Win32 message.

using System.Drawing;

namespace System.Windows.Forms
{
	/// <summary>A Win32 window message. ULDB has none; overrides taking one are never called with real data.</summary>
	public struct Message
	{
		public IntPtr HWnd { get; set; }
		public int Msg { get; set; }
		public IntPtr WParam { get; set; }
		public IntPtr LParam { get; set; }
		public IntPtr Result { get; set; }

		public static Message Create(IntPtr hWnd, int msg, IntPtr wparam, IntPtr lparam) =>
			new Message { HWnd = hWnd, Msg = msg, WParam = wparam, LParam = lparam };

		public object GetLParam(Type cls) => null;
	}

	public class PaintEventArgs : EventArgs, IDisposable
	{
		public PaintEventArgs(Graphics graphics, Rectangle clipRect)
		{
			Graphics = graphics;
			ClipRectangle = clipRect;
		}

		public Graphics Graphics { get; }
		public Rectangle ClipRectangle { get; }
		public void Dispose() { }
	}

	public delegate void PaintEventHandler(object sender, PaintEventArgs e);

	public class PreviewKeyDownEventArgs : EventArgs
	{
		public PreviewKeyDownEventArgs(Keys keyData) { KeyData = keyData; }
		public Keys KeyData { get; }
		public Keys KeyCode => KeyData & Keys.KeyCode;
		public Keys Modifiers => KeyData & Keys.Modifiers;
		public int KeyValue => (int)(KeyData & Keys.KeyCode);
		public bool Alt => (KeyData & Keys.Alt) == Keys.Alt;
		public bool Control => (KeyData & Keys.Control) == Keys.Control;
		public bool Shift => (KeyData & Keys.Shift) == Keys.Shift;
		public bool IsInputKey { get; set; }
	}

	public delegate void PreviewKeyDownEventHandler(object sender, PreviewKeyDownEventArgs e);

	public interface IDataObject
	{
		object GetData(string format);
		object GetData(Type format);
		bool GetDataPresent(string format);
		bool GetDataPresent(Type format);
		string[] GetFormats();
		void SetData(string format, object data);
		void SetData(object data);
	}

	public class DragEventArgs : EventArgs
	{
		public DragEventArgs(IDataObject data, int keyState, int x, int y, DragDropEffects allowedEffect, DragDropEffects effect)
		{
			Data = data;
			KeyState = keyState;
			X = x;
			Y = y;
			AllowedEffect = allowedEffect;
			Effect = effect;
		}

		public IDataObject Data { get; }
		public int KeyState { get; }
		public int X { get; }
		public int Y { get; }
		public DragDropEffects AllowedEffect { get; }
		public DragDropEffects Effect { get; set; }
	}

	public delegate void DragEventHandler(object sender, DragEventArgs e);
}
