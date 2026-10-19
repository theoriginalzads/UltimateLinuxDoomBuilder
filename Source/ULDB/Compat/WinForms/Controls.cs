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
		private string text = "";
		public virtual string Text
		{
			get => text;
			set { if(text == value) return; text = value ?? ""; OnTextChanged(EventArgs.Empty); }
		}
		public object Tag { get; set; }
		private bool visible = true, enabled = true;
		public bool Visible
		{
			get => visible;
			set { if(visible == value) return; visible = value; OnVisibleChanged(EventArgs.Empty); }
		}
		public bool Enabled
		{
			get => enabled;
			set { if(enabled == value) return; enabled = value; OnEnabledChanged(EventArgs.Empty); }
		}
		private Font font;
		/// <summary>Like WinForms: falls back to the default font when none is set.</summary>
		public virtual Font Font { get => font ?? SystemFonts.DefaultFont; set => font = value; }
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
		public bool InvokeRequired => !WinFormsHost.Current.CheckAccess();

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

		public event PaintEventHandler Paint;
		public event PreviewKeyDownEventHandler PreviewKeyDown;
		public event DragEventHandler DragEnter;
		public event DragEventHandler DragOver;
		public event DragEventHandler DragDrop;
		public event EventHandler DragLeave;
		public event EventHandler MouseEnter;
		public event EventHandler MouseLeave;
		public event MouseEventHandler MouseClick;
		public event MouseEventHandler MouseDoubleClick;
		public event EventHandler DoubleClick;
		public event EventHandler SizeChanged;
		public event EventHandler VisibleChanged;
		public event EventHandler EnabledChanged;
		public event EventHandler Enter;
		public event EventHandler Leave;

		protected virtual void OnPaint(PaintEventArgs e) => Paint?.Invoke(this, e);
		protected virtual void OnPreviewKeyDown(PreviewKeyDownEventArgs e) => PreviewKeyDown?.Invoke(this, e);
		protected virtual void OnDragEnter(DragEventArgs e) => DragEnter?.Invoke(this, e);
		protected virtual void OnDragOver(DragEventArgs e) => DragOver?.Invoke(this, e);
		protected virtual void OnDragDrop(DragEventArgs e) => DragDrop?.Invoke(this, e);
		protected virtual void OnDragLeave(EventArgs e) => DragLeave?.Invoke(this, e);
		protected virtual void OnMouseEnter(EventArgs e) => MouseEnter?.Invoke(this, e);
		protected virtual void OnMouseLeave(EventArgs e) => MouseLeave?.Invoke(this, e);
		protected virtual void OnMouseClick(MouseEventArgs e) => MouseClick?.Invoke(this, e);
		protected virtual void OnMouseDoubleClick(MouseEventArgs e) => MouseDoubleClick?.Invoke(this, e);
		protected virtual void OnDoubleClick(EventArgs e) => DoubleClick?.Invoke(this, e);
		protected virtual void OnSizeChanged(EventArgs e) => SizeChanged?.Invoke(this, e);
		protected virtual void OnVisibleChanged(EventArgs e) => VisibleChanged?.Invoke(this, e);
		protected virtual void OnEnabledChanged(EventArgs e) => EnabledChanged?.Invoke(this, e);
		protected virtual void OnEnter(EventArgs e) => Enter?.Invoke(this, e);
		protected virtual void OnLeave(EventArgs e) => Leave?.Invoke(this, e);

		// WinForms keyboard routing hooks. Upstream overrides them to stop focus moving on
		// Tab/arrow keys; the Avalonia port handles that itself, so they're never called.
		protected virtual bool IsInputChar(char charCode) => false;
		protected virtual bool IsInputKey(Keys keyData) => false;
		protected virtual bool ProcessKeyPreview(ref Message m) => false;
		protected virtual bool ProcessDialogKey(Keys keyData) => false;
		protected virtual bool ProcessCmdKey(ref Message msg, Keys keyData) => false;
		protected virtual void WndProc(ref Message m) { }

		public event EventHandler MouseCaptureChanged;
		protected virtual void OnMouseCaptureChanged(EventArgs e) => MouseCaptureChanged?.Invoke(this, e);

		public DockStyle Dock { get; set; }
		public bool CausesValidation { get; set; } = true;
		public bool CanFocus => Visible && Enabled;
		public ContextMenuStrip ContextMenuStrip { get; set; }

		/// <summary>The modifier keys held right now. Reported by the UI.</summary>
		public static Keys ModifierKeys => ModifierKeysProvider?.Invoke() ?? Keys.None;
		public static Func<Keys> ModifierKeysProvider { get; set; }

		/// <summary>ULDB: lets the UI raise the events upstream code subscribes to.</summary>
		public void RaiseMouseDown(MouseEventArgs e) => OnMouseDown(e);
		public void RaiseMouseUp(MouseEventArgs e) => OnMouseUp(e);
		public void RaiseMouseMove(MouseEventArgs e) => OnMouseMove(e);
		public void RaiseMouseWheel(MouseEventArgs e) => OnMouseWheel(e);
		public void RaiseMouseClick(MouseEventArgs e) => OnMouseClick(e);
		public void RaiseMouseDoubleClick(MouseEventArgs e) => OnMouseDoubleClick(e);
		public void RaiseMouseEnter() => OnMouseEnter(EventArgs.Empty);
		public void RaiseMouseLeave() => OnMouseLeave(EventArgs.Empty);
		public void RaiseKeyDown(KeyEventArgs e) => OnKeyDown(e);
		public void RaiseKeyUp(KeyEventArgs e) => OnKeyUp(e);
		public void RaiseKeyPress(KeyPressEventArgs e) => OnKeyPress(e);
		public void RaisePreviewKeyDown(PreviewKeyDownEventArgs e) => OnPreviewKeyDown(e);
		public void RaiseClick() => OnClick(EventArgs.Empty);
		public void RaiseResize() { OnResize(EventArgs.Empty); OnSizeChanged(EventArgs.Empty); }
		public void RaiseGotFocus() => OnGotFocus(EventArgs.Empty);
		public void RaiseLostFocus() => OnLostFocus(EventArgs.Empty);
		public void RaiseMouseCaptureChanged() => OnMouseCaptureChanged(EventArgs.Empty);
		public void RaiseDragEnter(DragEventArgs e) => OnDragEnter(e);
		public void RaiseDragOver(DragEventArgs e) => OnDragOver(e);
		public void RaiseDragDrop(DragEventArgs e) => OnDragDrop(e);
		public void RaiseDragLeave() => OnDragLeave(EventArgs.Empty);

		public bool AllowDrop { get; set; }
		public AnchorStyles Anchor { get; set; } = AnchorStyles.Top | AnchorStyles.Left;
		public bool Capture { get; set; }
		public bool Focused => false;
		public bool ContainsFocus => false;
		public bool Created => true;
		public bool IsHandleCreated => true;
		public Size MinimumSize { get; set; }
		public Size MaximumSize { get; set; }

		public virtual bool Focus() => false;
		public virtual void Invalidate() { }
		public void Invalidate(bool invalidateChildren) => Invalidate();
		public void Invalidate(Rectangle rc) => Invalidate();
		public void SendToBack() { }

		// Painting style flags: kept as state; the Avalonia views decide how to draw.
		private ControlStyles styles;
		protected void SetStyle(ControlStyles flag, bool value) { styles = value ? styles | flag : styles & ~flag; }
		protected bool GetStyle(ControlStyles flag) => (styles & flag) == flag;
		protected void UpdateStyles() { StylesChanged?.Invoke(this, EventArgs.Empty); }
		protected virtual bool DoubleBuffered { get; set; }

		/// <summary>ULDB: raised when SetStyle/UpdateStyles or the background changes, so the view can redraw.</summary>
		public event EventHandler StylesChanged;
		protected void NotifyStylesChanged() => StylesChanged?.Invoke(this, EventArgs.Empty);
		public event EventHandler HandleCreated;
		public event EventHandler HandleDestroyed;
		public virtual void Refresh() { }
		public virtual void Update() { }
		public virtual void Show() { Visible = true; }
		public virtual void Hide() { Visible = false; }
		public void BringToFront() { }
		public void SuspendLayout() { }
		public void ResumeLayout() { }
		public void ResumeLayout(bool performLayout) { }
		public void PerformLayout() { }
		public int TabIndex { get; set; }
		public bool TabStop { get; set; } = true;
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

		public void Dispose()
		{
			if(IsDisposed) return;
			Dispose(true);
			IsDisposed = true;
		}

		protected virtual void Dispose(bool disposing) { }

		public class ControlCollection : List<Control>
		{
			private readonly Control owner;
			public ControlCollection(Control owner) { this.owner = owner; }
			public new void Add(Control c) { c.Parent = owner; base.Add(c); }
			public new bool Remove(Control c) { if(c.Parent == owner) c.Parent = null; return base.Remove(c); }
			public void SetChildIndex(Control c, int index) { base.Remove(c); Insert(Math.Clamp(index, 0, Count), c); }
		}
	}

	public class Form : ContainerControl
	{
		/// <summary>The UI's active window, if it's one of the compat forms.</summary>
		public static Form ActiveForm => WinFormsHost.Current.ActiveForm;

		public DialogResult DialogResult { get; set; }
		public FormWindowState WindowState { get; set; }
		public Form Owner { get; set; }
		public bool TopMost { get; set; }
		public bool ShowInTaskbar { get; set; } = true;
		public Icon Icon { get; set; }

		// Designer settings: kept for upstream's InitializeComponent code; the Avalonia port decides.
		public FormBorderStyle FormBorderStyle { get; set; } = FormBorderStyle.Sizable;
		public FormStartPosition StartPosition { get; set; }
		public SizeGripStyle SizeGripStyle { get; set; }
		public MenuStrip MainMenuStrip { get; set; }
		public bool MaximizeBox { get; set; } = true;
		public bool MinimizeBox { get; set; } = true;
		public bool ShowIcon { get; set; } = true;
		public bool KeyPreview { get; set; }

		public event FormClosingEventHandler FormClosing;
		public event FormClosedEventHandler FormClosed;
		[Obsolete("Use FormClosed")] public event EventHandler Closed;
		public event EventHandler Load;
		public event EventHandler Shown;
		public event EventHandler Activated;
		public event EventHandler Deactivate;

		protected virtual void OnLoad(EventArgs e) => Load?.Invoke(this, e);
		protected virtual void OnShown(EventArgs e) => Shown?.Invoke(this, e);
		protected virtual void OnActivated(EventArgs e) => Activated?.Invoke(this, e);
		protected virtual void OnDeactivate(EventArgs e) => Deactivate?.Invoke(this, e);

		/// <summary>ULDB: the UI raises the form's lifecycle events as its window goes through them.</summary>
		public void RaiseLoad() => OnLoad(EventArgs.Empty);
		public void RaiseShown() => OnShown(EventArgs.Empty);
		public void RaiseActivated() => OnActivated(EventArgs.Empty);
		public void RaiseDeactivate() => OnDeactivate(EventArgs.Empty);

		/// <summary>ULDB: the UI asks to close (title bar, Alt+F4). Returns false if the form cancelled.</summary>
		public bool RequestClose(CloseReason reason)
		{
			FormClosingEventArgs closing = new FormClosingEventArgs(reason, false);
			OnFormClosing(closing);
			if(closing.Cancel) return false;
			Visible = false;
			OnFormClosed(new FormClosedEventArgs(reason));
			return true;
		}

		public virtual void Close()
		{
			// The UI closes the window (CloseHandler), which calls RequestClose; without one, close the model directly.
			if(CloseHandler != null) CloseHandler(this);
			else RequestClose(CloseReason.UserClosing);
		}

		/// <summary>ULDB: installed by the UI to close a form's window.</summary>
		public static Action<Form> CloseHandler { get; set; }

		protected virtual void OnFormClosing(FormClosingEventArgs e) => FormClosing?.Invoke(this, e);

		protected virtual void OnFormClosed(FormClosedEventArgs e)
		{
			FormClosed?.Invoke(this, e);
#pragma warning disable CS0618
			Closed?.Invoke(this, e);
#pragma warning restore CS0618
		}

		public void Activate() { }
		public void Show(IWin32Window owner) => Show();

		public virtual DialogResult ShowDialog() => ShowDialog(null);

		/// <summary>
		/// Windows not ported yet (generated placeholders) say so and answer Cancel, so the
		/// editor keeps running. Ported windows override this.
		/// </summary>
		public virtual DialogResult ShowDialog(IWin32Window owner)
		{
			WinFormsHost.Current.ShowMessageBox(owner, "The \"" + GetType().Name + "\" window isn't available in this version of " + Application.ProductName + " yet.",
				Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
			return DialogResult = DialogResult.Cancel;
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
		private Image backgroundimage;
		private ImageLayout backgroundimagelayout = ImageLayout.Tile;

		public Image BackgroundImage { get => backgroundimage; set { backgroundimage = value; NotifyStylesChanged(); } }
		public ImageLayout BackgroundImageLayout { get => backgroundimagelayout; set { backgroundimagelayout = value; NotifyStylesChanged(); } }
		public BorderStyle BorderStyle { get; set; }
		public bool AutoScroll { get; set; }
	}

	public class FlowLayoutPanel : Panel
	{
		public FlowDirection FlowDirection { get; set; }
		public bool WrapContents { get; set; } = true;
		public bool AutoSize { get; set; }
	}

	public class Label : Control
	{
		public bool AutoSize { get; set; }
		public ContentAlignment TextAlign { get; set; } = ContentAlignment.TopLeft;
		public Image Image { get; set; }
		public BorderStyle BorderStyle { get; set; }
		public FlatStyle FlatStyle { get; set; }
		public bool UseMnemonic { get; set; } = true;
	}

	public class Button : Control, IButtonControl
	{
		public Image Image { get; set; }
		public ContentAlignment ImageAlign { get; set; } = ContentAlignment.MiddleCenter;
		public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleCenter;
		public TextImageRelation TextImageRelation { get; set; }
		public FlatStyle FlatStyle { get; set; } = FlatStyle.Standard;
		public bool UseVisualStyleBackColor { get; set; }
		public bool AutoSize { get; set; }
		public DialogResult DialogResult { get; set; }
		public void NotifyDefault(bool value) { }
		public void PerformClick() { if(Enabled && Visible) OnClick(EventArgs.Empty); }
	}

	public interface IButtonControl
	{
		DialogResult DialogResult { get; set; }
		void NotifyDefault(bool value);
		void PerformClick();
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
		public ComboBoxStyle DropDownStyle { get; set; } = ComboBoxStyle.DropDown;
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
