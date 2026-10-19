// ULDB compatibility layer: state-only stand-ins for WinForms' common controls, so
// upstream designer code (InitializeComponent) and logic compile and run unchanged.
// The Avalonia views read and write their state and raise their events.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms
{
	/// <summary>A control that holds other controls and tracks which one is focused (Form, UserControl).</summary>
	public class ContainerControl : Control
	{
		public Control ActiveControl { get; set; }
		public SizeF AutoScaleDimensions { get; set; }
		public AutoScaleMode AutoScaleMode { get; set; }
		public bool AutoScroll { get; set; }
		public Form ParentForm
		{
			get
			{
				for(Control c = Parent; c != null; c = c.Parent) if(c is Form f) return f;
				return null;
			}
		}
		public bool Validate() => true;
		public void PerformAutoScale() { }
	}

	public class UserControl : ContainerControl
	{
		public bool AutoSize { get; set; }
		public AutoSizeMode AutoSizeMode { get; set; }
		public BorderStyle BorderStyle { get; set; }
		public event EventHandler Load;
		protected virtual void OnLoad(EventArgs e) => Load?.Invoke(this, e);

		/// <summary>ULDB: the view raises Load when it first shows the control.</summary>
		public void RaiseLoad() => OnLoad(EventArgs.Empty);
	}

	public enum AutoSizeMode
	{
		GrowAndShrink = 0,
		GrowOnly = 1
	}

	public class GroupBox : Control
	{
		public FlatStyle FlatStyle { get; set; } = FlatStyle.Standard;
		public bool AutoSize { get; set; }
	}

	public class CheckBox : Control
	{
		private CheckState checkstate;

		public bool AutoCheck { get; set; } = true;
		public bool AutoSize { get; set; }
		public bool ThreeState { get; set; }
		public Appearance Appearance { get; set; }
		public ContentAlignment CheckAlign { get; set; } = ContentAlignment.MiddleLeft;
		public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleLeft;
		public FlatStyle FlatStyle { get; set; } = FlatStyle.Standard;
		public Image Image { get; set; }
		public bool UseVisualStyleBackColor { get; set; }

		public bool Checked { get => checkstate != CheckState.Unchecked; set => CheckState = value ? CheckState.Checked : CheckState.Unchecked; }

		public CheckState CheckState
		{
			get => checkstate;
			set
			{
				if(checkstate == value) return;
				bool waschecked = Checked;
				checkstate = value;
				if(waschecked != Checked) CheckedChanged?.Invoke(this, EventArgs.Empty);
				CheckStateChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public event EventHandler CheckedChanged;
		public event EventHandler CheckStateChanged;

		protected override void OnClick(EventArgs e)
		{
			if(AutoCheck) CheckState = ThreeState
				? (CheckState)(((int)checkstate + 1) % 3)
				: (Checked ? CheckState.Unchecked : CheckState.Checked);
			base.OnClick(e);
		}
	}

	public class NumericUpDown : Control, ISupportInitialize
	{
		private decimal value, minimum, maximum = 100m;

		public decimal Minimum { get => minimum; set { minimum = value; if(maximum < minimum) maximum = minimum; Value = Value; } }
		public decimal Maximum { get => maximum; set { maximum = value; if(minimum > maximum) minimum = maximum; Value = Value; } }
		public decimal Increment { get; set; } = 1m;
		public int DecimalPlaces { get; set; }
		public bool Hexadecimal { get; set; }
		public bool ThousandsSeparator { get; set; }
		public bool ReadOnly { get; set; }
		public bool InterceptArrowKeys { get; set; } = true;
		public HorizontalAlignment TextAlign { get; set; }
		public LeftRightAlignment UpDownAlign { get; set; } = LeftRightAlignment.Right;
		public BorderStyle BorderStyle { get; set; } = BorderStyle.Fixed3D;

		public decimal Value
		{
			get => value;
			set
			{
				if(value < minimum || value > maximum) throw new ArgumentOutOfRangeException(nameof(value), "Value must be between Minimum and Maximum");
				if(this.value == value) return;
				this.value = value;
				ValueChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		public event EventHandler ValueChanged;

		public void UpButton() => Value = Math.Min(maximum, value + Increment);
		public void DownButton() => Value = Math.Max(minimum, value - Increment);
		public void Select(int start, int length) { }
		public void BeginInit() { }
		public void EndInit() { }
	}

	public class TabControl : Control
	{
		private int selectedindex = -1;

		public TabControl() { TabPages = new TabPageCollection(this); }

		public TabPageCollection TabPages { get; }
		public TabAlignment Alignment { get; set; }
		public TabAppearance Appearance { get; set; }
		public TabSizeMode SizeMode { get; set; }
		public Size ItemSize { get; set; }
		public Point Padding { get; set; }
		public bool Multiline { get; set; }
		public bool ShowToolTips { get; set; }
		public int TabCount => TabPages.Count;

		public int SelectedIndex
		{
			get => selectedindex;
			set
			{
				if(value == selectedindex || value < -1 || value >= TabPages.Count) return;
				TabControlCancelEventArgs selecting = new TabControlCancelEventArgs(value >= 0 ? TabPages[value] : null, value, false, TabControlAction.Selecting);
				Selecting?.Invoke(this, selecting);
				if(selecting.Cancel) return;
				selectedindex = value;
				SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
				Selected?.Invoke(this, new TabControlEventArgs(SelectedTab, value, TabControlAction.Selected));
			}
		}

		public TabPage SelectedTab
		{
			get => selectedindex >= 0 && selectedindex < TabPages.Count ? TabPages[selectedindex] : null;
			set => SelectedIndex = TabPages.IndexOf(value);
		}

		public void SelectTab(int index) => SelectedIndex = index;
		public void SelectTab(TabPage tabPage) => SelectedTab = tabPage;
		public void SelectTab(string tabPageName) => SelectedIndex = TabPages.FindIndex(p => p.Name == tabPageName);

		public event EventHandler SelectedIndexChanged;
		public event TabControlCancelEventHandler Selecting;
		public event TabControlEventHandler Selected;
		public event TabControlCancelEventHandler Deselecting;
		public event TabControlEventHandler Deselected;

		public class TabPageCollection : List<TabPage>
		{
			private readonly TabControl owner;
			internal TabPageCollection(TabControl owner) { this.owner = owner; }

			public new void Add(TabPage page)
			{
				page.Parent = owner;
				base.Add(page);
				if(owner.selectedindex < 0) owner.selectedindex = 0;
			}

			public void Add(string text) => Add(new TabPage(text));
			public void AddRange(TabPage[] pages) { foreach(TabPage p in pages) Add(p); }

			public new bool Remove(TabPage page)
			{
				bool removed = base.Remove(page);
				if(owner.selectedindex >= Count) owner.selectedindex = Count - 1;
				return removed;
			}
		}
	}

	public class TabPage : Panel
	{
		public TabPage() { }
		public TabPage(string text) { Text = text; }
		public string ToolTipText { get; set; }
		public bool UseVisualStyleBackColor { get; set; }
		public int ImageIndex { get; set; } = -1;
	}

	public class TabControlEventArgs : EventArgs
	{
		public TabControlEventArgs(TabPage tabPage, int tabPageIndex, TabControlAction action)
		{
			TabPage = tabPage;
			TabPageIndex = tabPageIndex;
			Action = action;
		}

		public TabPage TabPage { get; }
		public int TabPageIndex { get; }
		public TabControlAction Action { get; }
	}

	public class TabControlCancelEventArgs : CancelEventArgs
	{
		public TabControlCancelEventArgs(TabPage tabPage, int tabPageIndex, bool cancel, TabControlAction action) : base(cancel)
		{
			TabPage = tabPage;
			TabPageIndex = tabPageIndex;
			Action = action;
		}

		public TabPage TabPage { get; }
		public int TabPageIndex { get; }
		public TabControlAction Action { get; }
	}

	public delegate void TabControlEventHandler(object sender, TabControlEventArgs e);
	public delegate void TabControlCancelEventHandler(object sender, TabControlCancelEventArgs e);

	/// <summary>Tooltips for controls. Show/Hide pop up a tooltip right away; routed to the UI.</summary>
	public class ToolTip : IDisposable
	{
		private readonly Dictionary<Control, string> tips = new Dictionary<Control, string>();

		public ToolTip() { }
		public ToolTip(IContainer cont) { }

		public bool Active { get; set; } = true;
		public int AutomaticDelay { get; set; } = 500;
		public int AutoPopDelay { get; set; } = 5000;
		public int InitialDelay { get; set; } = 500;
		public int ReshowDelay { get; set; } = 100;
		public bool ShowAlways { get; set; }
		public bool IsBalloon { get; set; }
		public bool UseAnimation { get; set; } = true;
		public bool UseFading { get; set; } = true;
		public string ToolTipTitle { get; set; } = "";
		public ToolTipIcon ToolTipIcon { get; set; }
		public object Tag { get; set; }

		public void SetToolTip(Control control, string caption)
		{
			if(string.IsNullOrEmpty(caption)) tips.Remove(control);
			else tips[control] = caption;
			TipChanged?.Invoke(control, caption);
		}

		public string GetToolTip(Control control) => tips.TryGetValue(control, out string t) ? t : "";
		public void RemoveAll() { foreach(Control c in new List<Control>(tips.Keys)) SetToolTip(c, null); }

		public void Show(string text, IWin32Window window) => ShowHandler?.Invoke(this, text, window as Control, null);
		public void Show(string text, IWin32Window window, int duration) => ShowHandler?.Invoke(this, text, window as Control, null);
		public void Show(string text, IWin32Window window, Point point) => ShowHandler?.Invoke(this, text, window as Control, point);
		public void Show(string text, IWin32Window window, int x, int y) => ShowHandler?.Invoke(this, text, window as Control, new Point(x, y));
		public void Show(string text, IWin32Window window, int x, int y, int duration) => ShowHandler?.Invoke(this, text, window as Control, new Point(x, y));
		public void Hide(IWin32Window win) => HideHandler?.Invoke(this, win as Control);

		/// <summary>ULDB: installed by the UI. Point is relative to the control (null: at the pointer).</summary>
		public static Action<ToolTip, string, Control, Point?> ShowHandler { get; set; }
		public static Action<ToolTip, Control> HideHandler { get; set; }

		/// <summary>ULDB: a control's hover tooltip changed (null or empty: removed).</summary>
		public static event Action<Control, string> TipChanged;

		public void Dispose() { }
	}
}

namespace System.Windows.Forms.Design
{
	/// <summary>Where the Visual Studio designer offers a ToolStripItem type. No effect at runtime.</summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ToolStripItemDesignerAvailabilityAttribute : Attribute
	{
		public ToolStripItemDesignerAvailabilityAttribute() { }
		public ToolStripItemDesignerAvailabilityAttribute(ToolStripItemDesignerAvailability visibility) { ItemAdditionVisibility = visibility; }
		public ToolStripItemDesignerAvailability ItemAdditionVisibility { get; }
	}
}

namespace System.Drawing
{
	/// <summary>Visual Studio toolbox icon for a control. No effect at runtime.</summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class ToolboxBitmapAttribute : Attribute
	{
		public ToolboxBitmapAttribute(string imageFile) { }
		public ToolboxBitmapAttribute(Type t) { }
		public ToolboxBitmapAttribute(Type t, string name) { }
	}
}
