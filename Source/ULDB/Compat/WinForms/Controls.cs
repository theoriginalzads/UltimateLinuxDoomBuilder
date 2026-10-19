// ULDB compatibility layer: minimal stand-ins for the few WinForms controls that the
// editor core (outside Windows/ and Controls/) refers to. They hold state only and
// draw nothing; ULDB's real UI is Avalonia. Members are added as the core needs them.

using System.Collections.Generic;
using System.Drawing;

namespace System.Windows.Forms
{
	public class Control : IWin32Window, IDisposable
	{
		private int left, top, width, height;

		public virtual IntPtr Handle => IntPtr.Zero;
		public string Name { get; set; } = "";
		public virtual string Text { get; set; } = "";
		public object Tag { get; set; }
		public bool Visible { get; set; } = true;
		public bool Enabled { get; set; } = true;
		public virtual Font Font { get; set; }
		public Color BackColor { get; set; }
		public Color ForeColor { get; set; }
		public Cursor Cursor { get; set; } = Cursors.Default;
		public Padding Margin { get; set; }
		public Padding Padding { get; set; }
		public int Left { get => left; set => left = value; }
		public int Top { get => top; set => top = value; }
		public int Width { get => width; set => width = value; }
		public int Height { get => height; set => height = value; }
		public int Right => left + width;
		public int Bottom => top + height;
		public Point Location { get => new Point(left, top); set { left = value.X; top = value.Y; } }
		public Size Size { get => new Size(width, height); set { width = value.Width; height = value.Height; } }
		public Size ClientSize { get => Size; set => Size = value; }
		public Rectangle Bounds => new Rectangle(left, top, width, height);
		public Rectangle ClientRectangle => new Rectangle(0, 0, width, height);
		public Control Parent { get; set; }
		public ControlCollection Controls { get; }
		public bool IsDisposed { get; private set; }
		public bool InvokeRequired => false;

		public Control() { Controls = new ControlCollection(this); }

		// Standard control events, so upstream code can subscribe. Ports raise their own.
		public event KeyEventHandler KeyDown;
		public event KeyEventHandler KeyUp;
		public event KeyPressEventHandler KeyPress;
		public event MouseEventHandler MouseDown;
		public event MouseEventHandler MouseUp;
		public event MouseEventHandler MouseMove;
		public event MouseEventHandler MouseWheel;
		public event EventHandler Click;
		public event EventHandler TextChanged;
		public event EventHandler Resize;
		public event EventHandler GotFocus;
		public event EventHandler LostFocus;

		protected virtual void OnKeyDown(KeyEventArgs e) => KeyDown?.Invoke(this, e);
		protected virtual void OnKeyUp(KeyEventArgs e) => KeyUp?.Invoke(this, e);
		protected virtual void OnKeyPress(KeyPressEventArgs e) => KeyPress?.Invoke(this, e);
		protected virtual void OnMouseDown(MouseEventArgs e) => MouseDown?.Invoke(this, e);
		protected virtual void OnMouseUp(MouseEventArgs e) => MouseUp?.Invoke(this, e);
		protected virtual void OnMouseMove(MouseEventArgs e) => MouseMove?.Invoke(this, e);
		protected virtual void OnMouseWheel(MouseEventArgs e) => MouseWheel?.Invoke(this, e);
		protected virtual void OnClick(EventArgs e) => Click?.Invoke(this, e);
		protected virtual void OnTextChanged(EventArgs e) => TextChanged?.Invoke(this, e);
		protected virtual void OnResize(EventArgs e) => Resize?.Invoke(this, e);
		protected virtual void OnGotFocus(EventArgs e) => GotFocus?.Invoke(this, e);
		protected virtual void OnLostFocus(EventArgs e) => LostFocus?.Invoke(this, e);

		public virtual bool Focus() => false;
		public virtual void Invalidate() { }
		public virtual void Refresh() { }
		public virtual void Update() { }
		public virtual void Show() { Visible = true; }
		public virtual void Hide() { Visible = false; }
		public void BringToFront() { }
		public void SuspendLayout() { }
		public void ResumeLayout() { }
		public void ResumeLayout(bool performLayout) { }
		public Rectangle RectangleToScreen(Rectangle r) => r;
		public Point PointToScreen(Point p) => p;
		public Point PointToClient(Point p) => p;

		/// <summary>Runs a delegate on the UI thread and waits for it.</summary>
		public object Invoke(Delegate method, params object[] args)
		{
			object result = null;
			WinFormsHost.Current.Invoke(() => result = method.DynamicInvoke(args));
			return result;
		}

		public object Invoke(Action method) => Invoke((Delegate)method);

		/// <summary>Queues a delegate to run on the UI thread.</summary>
		public IAsyncResult BeginInvoke(Delegate method, params object[] args)
		{
			WinFormsHost.Current.BeginInvoke(() => method.DynamicInvoke(args));
			return null;
		}

		public virtual void Dispose()
		{
			IsDisposed = true;
		}

		public class ControlCollection : List<Control>
		{
			private readonly Control owner;
			public ControlCollection(Control owner) { this.owner = owner; }
			public new void Add(Control c) { c.Parent = owner; base.Add(c); }
			public new bool Remove(Control c) { if(c.Parent == owner) c.Parent = null; return base.Remove(c); }
			public void SetChildIndex(Control c, int index) { base.Remove(c); Insert(Math.Clamp(index, 0, Count), c); }
		}
	}

	public class Form : Control
	{
		/// <summary>The UI's active window, if it's one of the compat forms.</summary>
		public static Form ActiveForm => WinFormsHost.Current.ActiveForm;

		public DialogResult DialogResult { get; set; }
		public FormWindowState WindowState { get; set; }
		public Form Owner { get; set; }
		public bool TopMost { get; set; }
		public bool ShowInTaskbar { get; set; } = true;
		public Icon Icon { get; set; }

		public event FormClosingEventHandler FormClosing;
		public event FormClosedEventHandler FormClosed;

		public virtual void Close()
		{
			FormClosingEventArgs closing = new FormClosingEventArgs(CloseReason.UserClosing, false);
			FormClosing?.Invoke(this, closing);
			if(closing.Cancel) return;
			Visible = false;
			FormClosed?.Invoke(this, new FormClosedEventArgs(CloseReason.UserClosing));
		}

		public void Activate() { }
		public void Show(IWin32Window owner) => Show();

		public virtual DialogResult ShowDialog() => ShowDialog(null);

		public virtual DialogResult ShowDialog(IWin32Window owner)
		{
			throw new NotSupportedException(GetType().Name + " has no ULDB (Avalonia) implementation yet");
		}
	}

	public enum CloseReason
	{
		None = 0,
		WindowsShutDown = 1,
		MdiFormClosing = 2,
		UserClosing = 3,
		TaskManagerClosing = 4,
		FormOwnerClosing = 5,
		ApplicationExitCall = 6
	}

	public class FormClosingEventArgs : ComponentModel.CancelEventArgs
	{
		public FormClosingEventArgs(CloseReason closeReason, bool cancel) : base(cancel) { CloseReason = closeReason; }
		public CloseReason CloseReason { get; }
	}

	public class FormClosedEventArgs : EventArgs
	{
		public FormClosedEventArgs(CloseReason closeReason) { CloseReason = closeReason; }
		public CloseReason CloseReason { get; }
	}

	public delegate void FormClosingEventHandler(object sender, FormClosingEventArgs e);
	public delegate void FormClosedEventHandler(object sender, FormClosedEventArgs e);

	public class Panel : Control
	{
		public Image BackgroundImage { get; set; }
		public ImageLayout BackgroundImageLayout { get; set; }
	}

	/// <summary>State-only combo box: items, selection and drop-down width. The UI renders it.</summary>
	public class ComboBox : Control
	{
		public ObjectCollection Items { get; } = new ObjectCollection();
		public int SelectedIndex { get; set; } = -1;
		public object SelectedItem
		{
			get => SelectedIndex >= 0 && SelectedIndex < Items.Count ? Items[SelectedIndex] : null;
			set => SelectedIndex = Items.IndexOf(value);
		}
		public int DropDownWidth { get; set; }
		public int ItemHeight { get; set; }
		public void BeginUpdate() { }
		public void EndUpdate() { }

		public class ObjectCollection : List<object>
		{
			public void AddRange(object[] items) => base.AddRange(items);
		}
	}

	public class ListView : Control
	{
		public View View { get; set; }
	}

	public class DataGridView : Control
	{
		public DataGridViewCellStyle RowsDefaultCellStyle { get; set; } = new DataGridViewCellStyle();
	}

	public class DataGridViewCellStyle
	{
		public Padding Padding { get; set; }
	}
}
