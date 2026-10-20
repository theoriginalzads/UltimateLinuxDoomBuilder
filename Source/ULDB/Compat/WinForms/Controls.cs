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
		private Color backcolor, forecolor;
		public Color BackColor { get => backcolor; set { if(backcolor == value) return; backcolor = value; NotifyStylesChanged(); } }
		public Color ForeColor { get => forecolor; set { if(forecolor == value) return; forecolor = value; NotifyStylesChanged(); } }
		public Cursor Cursor { get; set; } = Cursors.Default;
		public RightToLeft RightToLeft { get; set; } = RightToLeft.No;
		public static Point MousePosition => Cursor.Position;
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
		protected virtual bool ProcessTabKey(bool forward) => false;
		/// <summary>ULDB: offers a key to the control as WinForms does before the key reaches the focused child (shortcuts).</summary>
		public bool RaiseProcessCmdKey(Keys keyData) { Message m = default; return ProcessCmdKey(ref m, keyData); }
		protected virtual bool ProcessDialogKey(Keys keyData) => false;
		protected virtual bool ProcessCmdKey(ref Message msg, Keys keyData) => false;
		protected virtual void WndProc(ref Message m) { }

		public event EventHandler MouseCaptureChanged;
		public event ComponentModel.CancelEventHandler Validating;
		public event EventHandler Validated;
		public event LayoutEventHandler Layout;
		public event EventHandler ClientSizeChanged;
		public event HelpEventHandler HelpRequested;
		protected virtual void OnValidating(ComponentModel.CancelEventArgs e) => Validating?.Invoke(this, e);
		protected virtual void OnValidated(EventArgs e) => Validated?.Invoke(this, e);
		protected virtual void OnLayout(LayoutEventArgs e) => Layout?.Invoke(this, e);
		protected virtual void OnClientSizeChanged(EventArgs e) => ClientSizeChanged?.Invoke(this, e);
		protected virtual void OnHelpRequested(HelpEventArgs e) => HelpRequested?.Invoke(this, e);

		/// <summary>ULDB: focus left the control in the view; runs Validating (unless cancelled) and Validated.</summary>
		public bool RaiseValidation()
		{
			if(!CausesValidation) return true;
			ComponentModel.CancelEventArgs e = new ComponentModel.CancelEventArgs();
			OnValidating(e);
			if(e.Cancel) return false;
			OnValidated(EventArgs.Empty);
			return true;
		}

		public void RaiseLayout() => OnLayout(new LayoutEventArgs(this, ""));
		public void RaiseHelpRequested() => OnHelpRequested(new HelpEventArgs(Cursor.Position));
		protected virtual void OnMouseCaptureChanged(EventArgs e) => MouseCaptureChanged?.Invoke(this, e);

		public DockStyle Dock { get; set; }

		/// <summary>The form this control is on (following Parent), or null.</summary>
		public Form FindForm()
		{
			for(Control c = Parent; c != null; c = c.Parent) if(c is Form f) return f;
			return null;
		}
		public ImeMode ImeMode { get; set; }
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
		public void RaiseDoubleClick() => OnDoubleClick(EventArgs.Empty);
		public void RaiseResize() { OnResize(EventArgs.Empty); OnSizeChanged(EventArgs.Empty); }
		// WinForms raises Enter before GotFocus and Leave before LostFocus.
		public void RaiseEnter() => OnEnter(EventArgs.Empty);
		public void RaiseLeave() => OnLeave(EventArgs.Empty);
		public void RaiseGotFocus() => OnGotFocus(EventArgs.Empty);
		public void RaiseLostFocus() => OnLostFocus(EventArgs.Empty);
		public void RaiseMouseCaptureChanged() => OnMouseCaptureChanged(EventArgs.Empty);
		public void RaiseDragEnter(DragEventArgs e) => OnDragEnter(e);
		public void RaiseDragOver(DragEventArgs e) => OnDragOver(e);
		public void RaiseDragDrop(DragEventArgs e) => OnDragDrop(e);
		public void RaiseDragLeave() => OnDragLeave(EventArgs.Empty);

		protected bool DesignMode => false;
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
		public void Invalidate(Rectangle rc, bool invalidateChildren) => Invalidate();

		/// <summary>ULDB: views do the drawing, so this draws onto an off-screen surface of the control's size.</summary>
		public Graphics CreateGraphics() => Graphics.FromImage(new Bitmap(Math.Max(1, Width), Math.Max(1, Height)));
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
		// ULDB: there are no native window handles, so these are never raised (upstream uses them
		// only for Win32 tweaks such as BufferedTreeView's double buffering).
		protected virtual void OnHandleCreated(EventArgs e) => HandleCreated?.Invoke(this, e);
		protected virtual void OnHandleDestroyed(EventArgs e) => HandleDestroyed?.Invoke(this, e);
		protected virtual void OnCreateControl() { }
		public virtual void Refresh() { Refreshed?.Invoke(this, EventArgs.Empty); }
		public virtual void Update() { }
		public virtual void Show() { Visible = true; Refreshed?.Invoke(this, EventArgs.Empty); }

		/// <summary>ULDB: the tooltip a ToolTip component gave this control (views show it).</summary>
		public string UldbToolTip { get => uldbtooltip; set { if(uldbtooltip == value) return; uldbtooltip = value; NotifyStylesChanged(); } }
		private string uldbtooltip;

		/// <summary>ULDB: raised by Show() and Refresh(), which upstream calls after filling a
		/// control; views re-read the model's state then.</summary>
		public event EventHandler Refreshed;

		// Background image (WinForms has these on Control).
		private Image backgroundimage;
		private ImageLayout backgroundimagelayout = ImageLayout.Tile;
		public Image BackgroundImage { get => backgroundimage; set { backgroundimage = value; NotifyStylesChanged(); } }
		public ImageLayout BackgroundImageLayout { get => backgroundimagelayout; set { backgroundimagelayout = value; NotifyStylesChanged(); } }

		// Window creation parameters: kept as state only.
		protected virtual CreateParams CreateParams => new CreateParams();
		protected virtual void OnPaintBackground(PaintEventArgs e) { }
		public virtual void Hide() { Visible = false; }
		public void BringToFront() { }
		public void SuspendLayout() { }
		public void ResumeLayout() { }
		public void ResumeLayout(bool performLayout) { }
		public void PerformLayout() { }
		public int TabIndex { get; set; }
		public bool TabStop { get; set; } = true;
		// Client <-> screen coordinates. The UI installs a ScreenMapper on controls it shows;
		// without one, client and screen coordinates are the same.
		public Rectangle RectangleToScreen(Rectangle r) => new Rectangle(PointToScreen(r.Location), r.Size);
		public Rectangle RectangleToClient(Rectangle r) => new Rectangle(PointToClient(r.Location), r.Size);
		public Point PointToScreen(Point p) => ScreenMapper?.ToScreen(p) ?? p;
		public Point PointToClient(Point p) => ScreenMapper?.ToClient(p) ?? p;

		/// <summary>ULDB: set by the UI to map this control's coordinates (device pixels) to the screen.</summary>
		public IScreenMapper ScreenMapper { get; set; }

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
			public new void Add(Control c)
			{
				c.Parent = owner;
				base.Add(c);
				// WinForms: adding a TabPage to a TabControl's Controls adds it to TabPages (designers do this).
				if(owner is TabControl tabs && c is TabPage page && !tabs.TabPages.Contains(page)) tabs.TabPages.Add(page);
			}

			public new bool Remove(Control c)
			{
				if(c.Parent == owner) c.Parent = null;
				if(owner is TabControl tabs && c is TabPage page) tabs.TabPages.Remove(page);
				return base.Remove(c);
			}
			public void SetChildIndex(Control c, int index) { base.Remove(c); Insert(Math.Clamp(index, 0, Count), c); }
		}
	}

	public class Form : ContainerControl
	{
		/// <summary>The UI's active window, if it's one of the compat forms.</summary>
		public static Form ActiveForm => WinFormsHost.Current.ActiveForm;

		private DialogResult dialogresult;

		/// <summary>Like WinForms, a new form isn't visible until it's shown (callers test Visible before Show).</summary>
		public Form() { Visible = false; }

		/// <summary>Like WinForms: setting a result on a form shown with ShowDialog closes it.</summary>
		public DialogResult DialogResult
		{
			get => dialogresult;
			set
			{
				dialogresult = value;
				if(Modal && value != DialogResult.None) Close();
			}
		}

		/// <summary>True while the form is shown with ShowDialog.</summary>
		public bool Modal { get; private set; }
		public bool ControlBox { get; set; } = true;
		public IButtonControl AcceptButton { get; set; }
		public IButtonControl CancelButton { get; set; }
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
		public double Opacity { get; set; } = 1.0;
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
			// WinForms raises the old Closing event first; FormClosing sees its decision.
			ComponentModel.CancelEventArgs old = new ComponentModel.CancelEventArgs();
			OnClosing(old);
			FormClosingEventArgs closing = new FormClosingEventArgs(reason, old.Cancel);
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

		[Obsolete("Use FormClosing")] public event ComponentModel.CancelEventHandler Closing;
		public event EventHandler ResizeEnd;
#pragma warning disable CS0618
		protected virtual void OnClosing(ComponentModel.CancelEventArgs e) => Closing?.Invoke(this, e);
#pragma warning restore CS0618
		protected virtual void OnResizeEnd(EventArgs e) => ResizeEnd?.Invoke(this, e);

		/// <summary>ULDB: the UI raises ResizeEnd when the user finishes moving or resizing the window.</summary>
		public void RaiseResizeEnd() => OnResizeEnd(EventArgs.Empty);

		protected virtual void OnFormClosing(FormClosingEventArgs e) => FormClosing?.Invoke(this, e);

		protected virtual void OnFormClosed(FormClosedEventArgs e)
		{
			FormClosed?.Invoke(this, e);
#pragma warning disable CS0618
			Closed?.Invoke(this, e);
#pragma warning restore CS0618
		}

		public void Activate() { }
		public void Show(IWin32Window owner) { if(owner is Form f) Owner = f; Show(); }

		public virtual DialogResult ShowDialog() => ShowDialog(null);

		/// <summary>
		/// Windows not ported yet (generated placeholders) say so and answer Cancel, so the
		/// editor keeps running. Ported windows override this.
		/// </summary>
		public virtual DialogResult ShowDialog(IWin32Window owner)
		{
			object view = GetView();
			if(view == null)
			{
				WinFormsHost.Current.ShowMessageBox(owner, "The \"" + GetType().Name + "\" window isn't available in this version of " + Application.ProductName + " yet.",
					Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
				return dialogresult = DialogResult.Cancel;
			}

			dialogresult = DialogResult.None;
			Modal = true;
			Visible = true;
			try { WinFormsHost.Current.ShowForm(this, view, owner, true); }
			finally { Modal = false; Visible = false; }
			if(dialogresult == DialogResult.None) dialogresult = DialogResult.Cancel; // Closed with the title bar
			return dialogresult;
		}

		public override void Show()
		{
			object view = GetView();
			if(view != null) WinFormsHost.Current.ShowForm(this, view, Owner, false);
			base.Show();
		}

		/// <summary>
		/// ULDB: an Avalonia port of this form returns its window (an Avalonia Window) here.
		/// Forms without one (generated placeholders) say they aren't available yet.
		/// </summary>
		protected virtual object CreateView() => null;

		/// <summary>
		/// ULDB: creates views for model-ported forms (upstream's form code compiled unchanged, with a
		/// separate Avalonia view). Installed by the UI; returns null for forms it has no view for.
		/// </summary>
		public static Func<Form, object> ViewFactory { get; set; }

		private object view;
		private object GetView() => view ??= CreateView() ?? ViewFactory?.Invoke(this);
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

	/// <summary>ULDB: maps a control's client coordinates to screen coordinates and back.</summary>
	public interface IScreenMapper
	{
		Point ToScreen(Point client);
		Point ToClient(Point screen);
	}

	public class Panel : Control
	{
		/// <summary>ULDB: the child the view should scroll to (ScrollControlIntoView).</summary>
		public Control ScrollTarget { get; set; }
		public void ScrollControlIntoView(Control activeControl) { ScrollTarget = activeControl; NotifyStylesChanged(); }
		public BorderStyle BorderStyle { get; set; }
		public bool AutoScroll { get; set; }
		public AutoSizeMode AutoSizeMode { get; set; } = AutoSizeMode.GrowOnly;
	}

	public static class ControlPaint
	{
		/// <summary>Draws a border of the given widths and colours inside bounds (only Solid is drawn).</summary>
		public static void DrawBorder(Graphics graphics, Rectangle bounds,
			Color leftColor, int leftWidth, ButtonBorderStyle leftStyle,
			Color topColor, int topWidth, ButtonBorderStyle topStyle,
			Color rightColor, int rightWidth, ButtonBorderStyle rightStyle,
			Color bottomColor, int bottomWidth, ButtonBorderStyle bottomStyle)
		{
			void Fill(Color c, ButtonBorderStyle style, int x, int y, int w, int h)
			{
				if(style == ButtonBorderStyle.None || w <= 0 || h <= 0) return;
				using(SolidBrush b = new SolidBrush(c)) graphics.FillRectangle(b, x, y, w, h);
			}
			Fill(leftColor, leftStyle, bounds.Left, bounds.Top, leftWidth, bounds.Height);
			Fill(topColor, topStyle, bounds.Left, bounds.Top, bounds.Width, topWidth);
			Fill(rightColor, rightStyle, bounds.Right - rightWidth, bounds.Top, rightWidth, bounds.Height);
			Fill(bottomColor, bottomStyle, bounds.Left, bounds.Bottom - bottomWidth, bounds.Width, bottomWidth);
		}
	}

	public class AutoCompleteStringCollection : List<string>
	{
		public void AddRange(string[] value) => base.AddRange(value);
	}

	public delegate void DrawItemEventHandler(object sender, DrawItemEventArgs e);
	public class DrawItemEventArgs : EventArgs
	{
		public DrawItemEventArgs(Graphics graphics, Font font, Rectangle rect, int index, DrawItemState state)
		{
			Graphics = graphics; Font = font; Bounds = rect; Index = index; State = state;
		}
		public Graphics Graphics { get; }
		public Font Font { get; }
		public Rectangle Bounds { get; }
		public int Index { get; }
		public DrawItemState State { get; }
		public Color ForeColor => (State & DrawItemState.Selected) != 0 ? SystemColors.HighlightText : SystemColors.WindowText;
		public Color BackColor => (State & DrawItemState.Selected) != 0 ? SystemColors.Highlight : SystemColors.Window;
		public void DrawBackground() { }
		public void DrawFocusRectangle() { }
	}

	public delegate void MeasureItemEventHandler(object sender, MeasureItemEventArgs e);
	public class MeasureItemEventArgs : EventArgs
	{
		public MeasureItemEventArgs(Graphics graphics, int index, int itemHeight) { Graphics = graphics; Index = index; ItemHeight = itemHeight; }
		public Graphics Graphics { get; }
		public int Index { get; }
		public int ItemHeight { get; set; }
		public int ItemWidth { get; set; }
	}

	public delegate void MethodInvoker();

	public delegate void LayoutEventHandler(object sender, LayoutEventArgs e);
	public class LayoutEventArgs : EventArgs
	{
		public LayoutEventArgs(Control affectedControl, string affectedProperty) { AffectedControl = affectedControl; AffectedProperty = affectedProperty; }
		public Control AffectedControl { get; }
		public string AffectedProperty { get; }
	}

	public delegate void HelpEventHandler(object sender, HelpEventArgs hlpevent);
	public class HelpEventArgs : EventArgs
	{
		public HelpEventArgs(Point mousePos) { MousePos = mousePos; }
		public Point MousePos { get; }
		public bool Handled { get; set; }
	}

	/// <summary>Scroll bar state (value and range). ButtonsNumericTextbox uses one as up/down buttons.</summary>
	public class ScrollBar : Control
	{
		private int value;
		public int Minimum { get; set; }
		public int Maximum { get; set; } = 100;
		public int SmallChange { get; set; } = 1;
		public int LargeChange { get; set; } = 10;
		public int Value
		{
			get => value;
			set
			{
				if(this.value == value) return;
				if(value < Minimum || value > Maximum) throw new ArgumentOutOfRangeException(nameof(value));
				this.value = value;
				ValueChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		public event EventHandler ValueChanged;
		public event ScrollEventHandler Scroll;

		/// <summary>ULDB: the view scrolled (or clicked an arrow): raises Scroll, then sets Value.</summary>
		public void RaiseScroll(ScrollEventType type, int newValue)
		{
			newValue = Math.Max(Minimum, Math.Min(Maximum, newValue));
			ScrollEventArgs e = new ScrollEventArgs(type, value, newValue, this is VScrollBar ? ScrollOrientation.VerticalScroll : ScrollOrientation.HorizontalScroll);
			Scroll?.Invoke(this, e);
			Value = Math.Max(Minimum, Math.Min(Maximum, e.NewValue));
		}
	}

	public class VScrollBar : ScrollBar { }

	/// <summary>A slider's state (value, range, ticks); views drag it and set Value.</summary>
	public class TrackBar : Control, ComponentModel.ISupportInitialize
	{
		private int value;
		public int Minimum { get; set; }
		public int Maximum { get; set; } = 10;
		public int SmallChange { get; set; } = 1;
		public int LargeChange { get; set; } = 5;
		public int TickFrequency { get; set; } = 1;
		public TickStyle TickStyle { get; set; } = TickStyle.BottomRight;
		public Orientation Orientation { get; set; } = Orientation.Horizontal;
		public bool AutoSize { get; set; } = true;
		public int Value
		{
			get => value;
			set
			{
				if(this.value == value) return;
				if(value < Minimum || value > Maximum) throw new ArgumentOutOfRangeException(nameof(value));
				this.value = value;
				OnValueChanged(EventArgs.Empty);
			}
		}
		public event EventHandler ValueChanged;
		public event EventHandler Scroll;
		protected virtual void OnValueChanged(EventArgs e) => ValueChanged?.Invoke(this, e);
		protected virtual void OnScroll(EventArgs e) => Scroll?.Invoke(this, e);

		/// <summary>ULDB: the user moved the slider in the view.</summary>
		public void RaiseScroll(int newValue)
		{
			Value = Math.Max(Minimum, Math.Min(Maximum, newValue));
			OnScroll(EventArgs.Empty);
		}

		public void BeginInit() { }
		public void EndInit() { }
	}
	public class HScrollBar : ScrollBar { }

	public class CreateParams
	{
		public string ClassName { get; set; }
		public string Caption { get; set; }
		public int Style { get; set; }
		public int ExStyle { get; set; }
		public int ClassStyle { get; set; }
		public object Parent { get; set; }
	}

	public class PictureBox : Control, ComponentModel.ISupportInitialize
	{
		public void BeginInit() { }
		public void EndInit() { }
		private Image image;
		public Image Image { get => image; set { image = value; NotifyStylesChanged(); } }
		public PictureBoxSizeMode SizeMode { get; set; }
		public BorderStyle BorderStyle { get; set; }
		public Rectangle DisplayRectangle => new Rectangle(Point.Empty, ClientSize);
	}

	public class FlowLayoutPanel : Panel
	{
		public FlowDirection FlowDirection { get; set; }
		public bool WrapContents { get; set; } = true;
		public bool AutoSize { get; set; }
	}

	/// <summary>
	/// State only: upstream uses it to show a few lines of formatted help text (3D Floor Mode's slope
	/// data sector dialog). ULDB's views show that text natively; DocumentText keeps what was set.
	/// </summary>
	public class WebBrowser : Control
	{
		private string documenttext = "";
		public string DocumentText { get => documenttext; set { documenttext = value ?? ""; NotifyStylesChanged(); } }
		public bool ScrollBarsEnabled { get; set; } = true;
		public bool IsWebBrowserContextMenuEnabled { get; set; } = true;
		public bool AllowWebBrowserDrop { get; set; } = true;
		public bool WebBrowserShortcutsEnabled { get; set; } = true;
	}

	public class Label : Control
	{
		public bool AutoSize { get; set; }
		public ContentAlignment TextAlign { get; set; } = ContentAlignment.TopLeft;
		public Image Image { get; set; }
		public BorderStyle BorderStyle { get; set; }
		public FlatStyle FlatStyle { get; set; }
		public bool UseMnemonic { get; set; } = true;
		public bool AutoEllipsis { get; set; }
	}

	public class Button : Control, IButtonControl
	{
		private Image image;
		public Image Image { get => image; set { if(image == value) return; image = value; NotifyStylesChanged(); } }
		public ContentAlignment ImageAlign { get; set; } = ContentAlignment.MiddleCenter;
		public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleCenter;
		public TextImageRelation TextImageRelation { get; set; }
		public FlatStyle FlatStyle { get; set; } = FlatStyle.Standard;
		public bool UseVisualStyleBackColor { get; set; }
		public bool AutoSize { get; set; }
		public DialogResult DialogResult { get; set; }
		public void NotifyDefault(bool value) { }
		public void PerformClick() { if(Enabled && Visible) OnClick(EventArgs.Empty); }

		// WinForms: a button with a DialogResult closes its (modal) form with that result.
		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);
			if(DialogResult != DialogResult.None && FindForm() is Form f) f.DialogResult = DialogResult;
		}
	}

	public interface IButtonControl
	{
		DialogResult DialogResult { get; set; }
		void NotifyDefault(bool value);
		void PerformClick();
	}

	/// <summary>State-only combo box: items, selection and drop-down width. The UI renders it.</summary>
	/// <summary>
	/// Combo box state: items, selection and text. Like WinForms, selecting an item sets Text to it
	/// (and a DropDownList's Text is always its selected item); typing in a DropDown sets Text and
	/// selects a matching item. Views call RaiseDropDown/RaiseDropDownClosed/RaiseSelectionChangeCommitted.
	/// </summary>
	public class ComboBox : Control
	{
		private int selectedindex = -1;
		private bool settingtext;

		public ComboBox()
		{
			Items = new ObjectCollection(this);
		}

		public ObjectCollection Items { get; }

		public int SelectedIndex
		{
			get => selectedindex;
			set
			{
				if(value < -1 || value >= Items.Count) throw new ArgumentOutOfRangeException(nameof(value));
				if(selectedindex == value) return;
				selectedindex = value;
				settingtext = true;
				try { Text = value >= 0 ? GetItemText(Items[value]) : (DropDownStyle == ComboBoxStyle.DropDownList ? "" : Text); }
				finally { settingtext = false; }
				OnSelectedIndexChanged(EventArgs.Empty);
				OnSelectedValueChanged(EventArgs.Empty);
				SelectedItemChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public object SelectedItem
		{
			get => selectedindex >= 0 && selectedindex < Items.Count ? Items[selectedindex] : null;
			set => SelectedIndex = value == null ? -1 : Items.IndexOf(value);
		}

		public object SelectedValue { get => SelectedItem; set => SelectedItem = value; }

		public override string Text
		{
			get => base.Text;
			set
			{
				base.Text = value;
				if(settingtext) return;
				// Typing (or code) selects the item with that text, if any.
				int index = FindStringExact(value ?? "");
				if(index != selectedindex && (index >= 0 || DropDownStyle != ComboBoxStyle.DropDownList))
				{
					if(index >= 0) SelectedIndex = index;
					else if(selectedindex >= 0) { selectedindex = -1; OnSelectedIndexChanged(EventArgs.Empty); }
				}
			}
		}

		public string GetItemText(object item)
		{
			if(item == null) return "";
			if(!string.IsNullOrEmpty(DisplayMember))
			{
				Reflection.PropertyInfo p = item.GetType().GetProperty(DisplayMember);
				if(p != null) return p.GetValue(item)?.ToString() ?? "";
			}
			return item.ToString();
		}

		public int FindStringExact(string s)
		{
			for(int i = 0; i < Items.Count; i++) if(string.Equals(GetItemText(Items[i]), s, StringComparison.OrdinalIgnoreCase)) return i;
			return -1;
		}

		public int FindString(string s)
		{
			for(int i = 0; i < Items.Count; i++) if(GetItemText(Items[i]).StartsWith(s ?? "", StringComparison.OrdinalIgnoreCase)) return i;
			return -1;
		}

		public int DropDownWidth { get; set; }
		public int DropDownHeight { get; set; } = 106;
		public int ItemHeight { get; set; } = 13;
		public int MaxDropDownItems { get; set; } = 8;
		public int MaxLength { get; set; }
		public bool IntegralHeight { get; set; } = true;
		public bool FormattingEnabled { get; set; }
		public bool Sorted { get; set; }
		public bool DroppedDown { get; set; }
		public string DisplayMember { get; set; } = "";
		public string ValueMember { get; set; } = "";
		public object DataSource { get; set; }
		public FlatStyle FlatStyle { get; set; } = FlatStyle.Standard;
		public DrawMode DrawMode { get; set; }
		public AutoCompleteMode AutoCompleteMode { get; set; }
		public AutoCompleteSource AutoCompleteSource { get; set; } = AutoCompleteSource.None;
		public AutoCompleteStringCollection AutoCompleteCustomSource { get; set; } = new AutoCompleteStringCollection();
		public ComboBoxStyle DropDownStyle { get; set; } = ComboBoxStyle.DropDown;
		public int SelectionStart { get; set; }
		public int SelectionLength { get; set; }
		public string SelectedText { get; set; } = "";
		public void SelectAll() { SelectionStart = 0; SelectionLength = Text.Length; }
		public void Select(int start, int length) { SelectionStart = start; SelectionLength = length; }
		public void BeginUpdate() { }
		public void EndUpdate() { ItemsChanged?.Invoke(this, EventArgs.Empty); }

		public event EventHandler SelectedIndexChanged;
		public event EventHandler SelectedValueChanged;
		public event EventHandler SelectionChangeCommitted;
		public event EventHandler DropDown;
		public event EventHandler DropDownClosed;
		public event EventHandler TextUpdate;
		public event DrawItemEventHandler DrawItem;
		public event MeasureItemEventHandler MeasureItem;

		/// <summary>ULDB: raised when the item list changes, so views refresh it.</summary>
		public event EventHandler ItemsChanged;
		/// <summary>ULDB: raised after SelectedIndex changes (with SelectedIndexChanged).</summary>
		public event EventHandler SelectedItemChanged;

		protected virtual void OnSelectedIndexChanged(EventArgs e) => SelectedIndexChanged?.Invoke(this, e);
		protected virtual void OnSelectedValueChanged(EventArgs e) => SelectedValueChanged?.Invoke(this, e);
		protected virtual void OnSelectionChangeCommitted(EventArgs e) => SelectionChangeCommitted?.Invoke(this, e);
		protected virtual void OnDropDown(EventArgs e) => DropDown?.Invoke(this, e);
		protected virtual void OnDropDownClosed(EventArgs e) => DropDownClosed?.Invoke(this, e);
		protected virtual void OnDrawItem(DrawItemEventArgs e) => DrawItem?.Invoke(this, e);
		protected virtual void OnMeasureItem(MeasureItemEventArgs e) => MeasureItem?.Invoke(this, e);

		/// <summary>ULDB: the user picked an item in the view.</summary>
		public void RaiseSelectionChangeCommitted(int index)
		{
			SelectedIndex = index;
			OnSelectionChangeCommitted(EventArgs.Empty);
		}

		/// <summary>ULDB: the user typed in the view's text box.</summary>
		public void RaiseTextUpdate(string text)
		{
			Text = text;
			TextUpdate?.Invoke(this, EventArgs.Empty);
		}

		public void RaiseDropDown() { DroppedDown = true; OnDropDown(EventArgs.Empty); }
		public void RaiseDropDownClosed() { DroppedDown = false; OnDropDownClosed(EventArgs.Empty); }

		internal void OnItemsChanged()
		{
			if(selectedindex >= Items.Count) { selectedindex = -1; OnSelectedIndexChanged(EventArgs.Empty); }
			ItemsChanged?.Invoke(this, EventArgs.Empty);
		}

		public class ObjectCollection : List<object>
		{
			private readonly ComboBox owner;

			public ObjectCollection() { }
			public ObjectCollection(ComboBox owner) { this.owner = owner; }

			public new int Add(object item)
			{
				if(owner != null && owner.Sorted)
				{
					string text = owner.GetItemText(item);
					int i = 0;
					while(i < Count && string.Compare(owner.GetItemText(this[i]), text, StringComparison.CurrentCulture) <= 0) i++;
					base.Insert(i, item);
					owner.OnItemsChanged();
					return i;
				}
				base.Add(item);
				owner?.OnItemsChanged();
				return Count - 1;
			}

			public void AddRange(object[] items)
			{
				if(owner != null && owner.Sorted) { foreach(object o in items) Add(o); return; }
				base.AddRange(items);
				owner?.OnItemsChanged();
			}

			public new void AddRange(IEnumerable<object> items) => AddRange(new List<object>(items).ToArray());
			public new void Insert(int index, object item) { base.Insert(index, item); owner?.OnItemsChanged(); }
			public new void Remove(object item) { base.Remove(item); owner?.OnItemsChanged(); }
			public new void RemoveAt(int index) { base.RemoveAt(index); owner?.OnItemsChanged(); }
			public new void Clear()
			{
				base.Clear();
				if(owner != null) { owner.selectedindex = -1; owner.OnItemsChanged(); }
			}
		}
	}

	public class ListView : Control
	{
		public View View { get; set; }
		public bool CheckBoxes { get; set; }
		public bool FullRowSelect { get; set; }
		public bool MultiSelect { get; set; } = true;
		public bool HideSelection { get; set; } = true;
		public bool Scrollable { get; set; } = true;
		public bool ShowGroups { get; set; } = true;
		public bool UseCompatibleStateImageBehavior { get; set; }
		public BorderStyle BorderStyle { get; set; }
		public ColumnHeaderStyle HeaderStyle { get; set; }
		public ListViewItemCollection Items { get; }
		public ImageList SmallImageList { get; set; }
		public ImageList LargeImageList { get; set; }
		public SortOrder Sorting { get; set; }
		public bool LabelWrap { get; set; }
		public bool LabelEdit { get; set; }
		public bool GridLines { get; set; }
		/// <summary>ULDB: the item whose label the view should edit (BeginEdit).</summary>
		public ListViewItem EditingItem { get; set; }
		public event LabelEditEventHandler BeforeLabelEdit;
		public event LabelEditEventHandler AfterLabelEdit;
		/// <summary>ULDB: the view finished editing an item's label (label null when cancelled).</summary>
		public void EndLabelEdit(ListViewItem item, string label)
		{
			LabelEditEventArgs e = new LabelEditEventArgs(item.Index, label);
			AfterLabelEdit?.Invoke(this, e);
			if(!e.CancelEdit && label != null) item.Text = label;
		}
		public bool RaiseBeforeLabelEdit(ListViewItem item)
		{
			LabelEditEventArgs e = new LabelEditEventArgs(item.Index);
			BeforeLabelEdit?.Invoke(this, e);
			return !e.CancelEdit;
		}
		public ListViewGroupCollection Groups { get; } = new ListViewGroupCollection();
		public void AutoResizeColumns(ColumnHeaderAutoResizeStyle headerAutoResize) { }
		public void EnsureVisible(int index) { if(index >= 0 && index < Items.Count) Items[index].EnsureVisible(); }
		/// <summary>The item with the focus; the views keep selection and focus together, so the first selected.</summary>
		public ListViewItem FocusedItem { get { foreach(ListViewItem i in Items) if(i.Selected) return i; return null; } set { if(value != null && !value.Selected) { value.Selected = true; OnSelectionSetByCode(); } } }

		/// <summary>Sorts the items by text (in the order of Sorting), as WinForms does.</summary>
		public void Sort()
		{
			if(Sorting == SortOrder.None) return;
			List<ListViewItem> sorted = new List<ListViewItem>(Items);
			sorted.Sort((a, b) => (Sorting == SortOrder.Descending ? -1 : 1) * string.Compare(a.Text, b.Text, StringComparison.CurrentCultureIgnoreCase));
			Items.Clear();
			foreach(ListViewItem i in sorted) Items.Add(i);
			NotifyStylesChanged();
		}

		public ListView() { Items = new ListViewItemCollection(this); }

		public event EventHandler SelectedIndexChanged;
		public event ItemCheckedEventHandler ItemChecked;
		public event ListViewItemSelectionChangedEventHandler ItemSelectionChanged;
		internal void OnItemChecked(ListViewItem item) => ItemChecked?.Invoke(this, new ItemCheckedEventArgs(item));
		internal void OnItemSelectionChanged(ListViewItem item) => ItemSelectionChanged?.Invoke(this, new ListViewItemSelectionChangedEventArgs(item, item.Index, item.Selected));

		/// <summary>ULDB: the view calls this after changing the items' Selected, as WinForms raises it.</summary>
		public void RaiseSelectedIndexChanged() => SelectedIndexChanged?.Invoke(this, EventArgs.Empty);

		// Batch updates: the view refreshes when they end.
		private int updating;
		internal void OnItemsChanged() { if(updating == 0) NotifyStylesChanged(); }
		public void BeginUpdate() => updating++;
		public void EndUpdate() { if(updating > 0 && --updating == 0) NotifyStylesChanged(); }

		/// <summary>ULDB: set to the item the view should scroll to (EnsureVisible).</summary>
		public ListViewItem EnsureVisibleItem { get; set; }
		public List<ColumnHeader> Columns { get; } = new List<ColumnHeader>();

		/// <summary>Item layout is up to the view; this reports one column of 16 px rows.</summary>
		public Rectangle GetItemRect(int index) => new Rectangle(0, index * 16, Math.Max(1, Width), 16);

		/// <summary>Indices of the selected items, in item order. A snapshot for reading; Add and Clear
		/// change the real selection (as WinForms' live collection does).</summary>
		public SelectedIndexCollection SelectedIndices
		{
			get
			{
				SelectedIndexCollection result = new SelectedIndexCollection(this);
				for(int i = 0; i < Items.Count; i++) if(Items[i].Selected) result.AddSnapshot(i);
				return result;
			}
		}

		/// <summary>The selected items, in item order (same: a snapshot whose Clear changes the selection).</summary>
		public SelectedListViewItemCollection SelectedItems
		{
			get
			{
				SelectedListViewItemCollection result = new SelectedListViewItemCollection(this);
				foreach(ListViewItem i in Items) if(i.Selected) result.AddSnapshot(i);
				return result;
			}
		}

		/// <summary>ULDB: the selection changed from code; views refresh their selection.</summary>
		internal void OnSelectionSetByCode() => NotifyStylesChanged();

		public class SelectedIndexCollection : List<int>
		{
			private readonly ListView owner;
			internal SelectedIndexCollection(ListView owner) { this.owner = owner; }
			internal void AddSnapshot(int index) => base.Add(index);
			public new int Add(int index) { if(index >= 0 && index < owner.Items.Count) { owner.Items[index].Selected = true; owner.OnSelectionSetByCode(); } return index; }
			public new void Clear() { foreach(ListViewItem i in owner.Items) i.Selected = false; owner.OnSelectionSetByCode(); }
			public new void Remove(int index) { if(index >= 0 && index < owner.Items.Count) { owner.Items[index].Selected = false; owner.OnSelectionSetByCode(); } }
		}

		public class SelectedListViewItemCollection : List<ListViewItem>
		{
			private readonly ListView owner;
			internal SelectedListViewItemCollection(ListView owner) { this.owner = owner; }
			internal void AddSnapshot(ListViewItem item) => base.Add(item);
			public new void Clear() { foreach(ListViewItem i in owner.Items) i.Selected = false; owner.OnSelectionSetByCode(); }
		}

		public class ListViewItemCollection : List<ListViewItem>
		{
			private readonly ListView owner;
			public ListViewItemCollection() { }
			internal ListViewItemCollection(ListView owner) { this.owner = owner; }
			public ListViewItem Add(string text) { ListViewItem item = new ListViewItem(text); Add(item); return item; }
			public ListViewItem Add(string key, string text, int imageIndex) { ListViewItem item = new ListViewItem(text) { Name = key, ImageIndex = imageIndex }; Add(item); return item; }
			public new ListViewItem Add(ListViewItem item) { item.ListView = owner; base.Add(item); Changed(); return item; }
			public new void Insert(int index, ListViewItem item) { item.ListView = owner; base.Insert(index, item); Changed(); }
			public void AddRange(ListViewItem[] items) { foreach(ListViewItem i in items) Add(i); }
			public new void Clear() { base.Clear(); Changed(); }
			public new bool Remove(ListViewItem item) { bool removed = base.Remove(item); Changed(); return removed; }
			public new void RemoveAt(int index) { base.RemoveAt(index); Changed(); }

			// The view shows the items again (not inside BeginUpdate/EndUpdate, which refresh once at the end).
			private void Changed() { owner?.OnItemsChanged(); }
		}
	}

	public class ListViewItem
	{
		public ListViewItem() { }
		public ListViewItem(string text) { Text = text; }
		public string Text { get; set; }
		public ListView ListView { get; internal set; }
		public int Index => ListView?.Items.IndexOf(this) ?? -1;
		public void EnsureVisible() { if(ListView != null) ListView.EnsureVisibleItem = this; }

		/// <summary>The item's columns. As in WinForms, SubItems[0] is the item itself (its Text).</summary>
		public ListViewSubItemCollection SubItems => subitems ??= new ListViewSubItemCollection(this);
		private ListViewSubItemCollection subitems;

		public class ListViewSubItem
		{
			public ListViewSubItem() { }
			public ListViewSubItem(ListViewItem owner, string text) { Text = text; }
			public virtual string Text { get; set; }
			public object Tag { get; set; }
			public string Name { get; set; }
		}

		private sealed class OwnerText : ListViewSubItem
		{
			private readonly ListViewItem owner;
			public OwnerText(ListViewItem owner) { this.owner = owner; }
			public override string Text { get => owner.Text; set => owner.Text = value; }
		}

		public class ListViewSubItemCollection : List<ListViewSubItem>
		{
			private readonly ListViewItem owner;
			internal ListViewSubItemCollection(ListViewItem owner) { this.owner = owner; base.Add(new OwnerText(owner)); }
			public ListViewSubItem Add(string text) { ListViewSubItem item = new ListViewSubItem(owner, text); base.Add(item); return item; }
			public new ListViewSubItem Add(ListViewSubItem item) { base.Add(item); return item; }
		}
		private bool itemchecked, selected;
		public bool Checked { get => itemchecked; set { if(itemchecked == value) return; itemchecked = value; ListView?.OnItemChecked(this); } }
		public bool Selected { get => selected; set { if(selected == value) return; selected = value; ListView?.OnItemSelectionChanged(this); } }
		public Color ForeColor { get; set; } = SystemColors.WindowText;
		public Color BackColor { get; set; } = SystemColors.Window;
		public object Tag { get; set; }
		public string ToolTipText { get; set; }
		public string Name { get; set; }
		public int ImageIndex { get; set; } = -1;
		public bool UseItemStyleForSubItems { get; set; } = true;
		public ListViewGroup Group { get; set; }
		public void Remove() { ListView?.Items.Remove(this); }
		public void BeginEdit() { if(ListView != null) ListView.EditingItem = this; }
	}

	public class ListViewGroup
	{
		public ListViewGroup() { }
		public ListViewGroup(string header) { Header = header; }
		public ListViewGroup(string key, string header) { Name = key; Header = header; }
		public string Header { get; set; }
		public string Name { get; set; }
		public object Tag { get; set; }
	}

	public class ListViewGroupCollection : List<ListViewGroup>
	{
		public ListViewGroup Add(string key, string headerText) { ListViewGroup g = new ListViewGroup(key, headerText); base.Add(g); return g; }
		public new int Add(ListViewGroup group) { base.Add(group); return Count - 1; }
		public ListViewGroup this[string key] { get { foreach(ListViewGroup g in this) if(g.Name == key) return g; return null; } }
	}

	public enum ColumnHeaderAutoResizeStyle { None, HeaderSize, ColumnContent }

	public delegate void LabelEditEventHandler(object sender, LabelEditEventArgs e);
	public class LabelEditEventArgs : EventArgs
	{
		public LabelEditEventArgs(int item) { Item = item; }
		public LabelEditEventArgs(int item, string label) { Item = item; Label = label; }
		public int Item { get; }
		public string Label { get; }
		public bool CancelEdit { get; set; }
	}

	public delegate void ItemCheckedEventHandler(object sender, ItemCheckedEventArgs e);
	public delegate void ListViewItemSelectionChangedEventHandler(object sender, ListViewItemSelectionChangedEventArgs e);

	public class ItemCheckedEventArgs : EventArgs
	{
		public ItemCheckedEventArgs(ListViewItem item) { Item = item; }
		public ListViewItem Item { get; }
	}

	public class ListViewItemSelectionChangedEventArgs : EventArgs
	{
		public ListViewItemSelectionChangedEventArgs(ListViewItem item, int itemIndex, bool isSelected) { Item = item; ItemIndex = itemIndex; IsSelected = isSelected; }
		public ListViewItem Item { get; }
		public int ItemIndex { get; }
		public bool IsSelected { get; }
	}

	public class ColumnHeader
	{
		public string Text { get; set; }
		public int Width { get; set; }
	}

}
