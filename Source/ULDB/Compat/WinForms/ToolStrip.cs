// ULDB compatibility layer: WinForms' menu and toolbar model (ToolStrip, MenuStrip,
// ToolStripMenuItem, ...). Upstream builds its menus and toolbars from these objects,
// and plugins add their own buttons and menu items through General.Interface. Here they
// hold state only; the Avalonia main window renders them and follows their changes
// through the Changed events (ULDB additions, not part of WinForms).

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms
{
	public class ToolStripItem : IDisposable
	{
		private string text = "";
		private string tooltiptext;
		private Image image;
		private bool enabled = true;
		private bool available = true;
		private ToolStripItemDisplayStyle displaystyle = ToolStripItemDisplayStyle.ImageAndText;
		private ToolStripItemAlignment alignment;

		public ToolStripItem() { }
		public ToolStripItem(string text, Image image, EventHandler onClick) : this(text, image, onClick, null) { }

		public ToolStripItem(string text, Image image, EventHandler onClick, string name)
		{
			this.text = text ?? "";
			this.image = image;
			if(onClick != null) Click += onClick;
			Name = name ?? "";
		}

		public string Name { get; set; } = "";
		public object Tag { get; set; }
		public virtual string Text { get => text; set { if(text == value) return; text = value ?? ""; TextChanged?.Invoke(this, EventArgs.Empty); NotifyChanged(); } }
		public string ToolTipText { get => tooltiptext ?? (AutoToolTip ? text : null); set { tooltiptext = value; NotifyChanged(); } }
		public bool AutoToolTip { get; set; } = true;
		public virtual Image Image { get => image; set { image = value; NotifyChanged(); } }
		public Color ImageTransparentColor { get; set; }
		public ToolStripItemImageScaling ImageScaling { get; set; } = ToolStripItemImageScaling.SizeToFit;
		public ToolStripItemDisplayStyle DisplayStyle { get => displaystyle; set { displaystyle = value; NotifyChanged(); } }
		public TextImageRelation TextImageRelation { get; set; } = TextImageRelation.ImageBeforeText;
		public ContentAlignment ImageAlign { get; set; } = ContentAlignment.MiddleCenter;
		public ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleCenter;
		public ToolStripItemAlignment Alignment { get => alignment; set { alignment = value; NotifyChanged(); } }
		public ToolStripItemOverflow Overflow { get; set; } = ToolStripItemOverflow.AsNeeded;
		public MergeAction MergeAction { get; set; }
		public int MergeIndex { get; set; } = -1;
		public bool AutoSize { get; set; } = true;
		public Size Size { get; set; }
		public int Width { get => Size.Width; set => Size = new Size(value, Size.Height); }
		public int Height { get => Size.Height; set => Size = new Size(Size.Width, value); }
		private Font font;
		/// <summary>Like WinForms: the owner's font unless one is set.</summary>
		public Font Font { get => font ?? Owner?.Font ?? SystemFonts.DefaultFont; set { font = value; NotifyChanged(); } }
		public Color ForeColor { get; set; }
		public Color BackColor { get; set; }
		public Padding Margin { get; set; }
		public Padding Padding { get; set; }
		public bool Selected => false;
		public bool Pressed => false;
		public bool IsDisposed { get; private set; }

		public virtual bool Enabled
		{
			get => enabled;
			set { if(enabled == value) return; enabled = value; EnabledChanged?.Invoke(this, EventArgs.Empty); NotifyChanged(); }
		}

		/// <summary>Whether the item is shown. Unlike WinForms, Visible doesn't depend on the parent being shown.</summary>
		public bool Available
		{
			get => available;
			set { if(available == value) return; available = value; VisibleChanged?.Invoke(this, EventArgs.Empty); NotifyChanged(); }
		}

		public bool Visible { get => Available; set => Available = value; }

		/// <summary>The tool strip (toolbar, menu bar or drop-down) the item is in.</summary>
		public ToolStrip Owner { get; internal set; }

		/// <summary>The menu item whose drop-down holds this item, if any.</summary>
		public ToolStripItem OwnerItem => (Owner as ToolStripDropDown)?.OwnerItem;

		public ToolStrip GetCurrentParent() => Owner;

		public event EventHandler Click;
		public event EventHandler DoubleClick;
		public event MouseEventHandler MouseDown;
		public event MouseEventHandler MouseUp;
		public event EventHandler MouseEnter;
		public event EventHandler MouseLeave;
		public event EventHandler MouseHover;
		public event EventHandler EnabledChanged;
		public event EventHandler VisibleChanged;
		public event EventHandler TextChanged;
		public event EventHandler Disposed;

		/// <summary>ULDB: raised when anything the UI shows (text, image, state) changes.</summary>
		public event EventHandler Changed;

		protected void NotifyChanged() => Changed?.Invoke(this, EventArgs.Empty);

		/// <summary>Clicks the item, as the UI does when the user activates it.</summary>
		public void PerformClick()
		{
			if(Enabled && Available) OnClick(EventArgs.Empty);
		}

		protected virtual void OnClick(EventArgs e) => Click?.Invoke(this, e);
		protected virtual void OnMouseDown(MouseEventArgs e) => MouseDown?.Invoke(this, e);
		protected virtual void OnMouseUp(MouseEventArgs e) => MouseUp?.Invoke(this, e);

		/// <summary>ULDB: lets the UI raise the pointer events upstream code listens to.</summary>
		public void RaiseMouseDown(MouseEventArgs e) => OnMouseDown(e);
		public void RaiseMouseUp(MouseEventArgs e) => OnMouseUp(e);
		public void RaiseMouseEnter() => MouseEnter?.Invoke(this, EventArgs.Empty);
		public void RaiseMouseLeave() => MouseLeave?.Invoke(this, EventArgs.Empty);

		public void Select() { }
		public void Invalidate() { }

		/// <summary>Called when the item's size or position changes (upstream's Mono fixes override it).</summary>
		protected virtual void OnBoundsChanged() { }

		public void Dispose()
		{
			if(IsDisposed) return;
			Dispose(true);
			IsDisposed = true;
			Owner?.Items.Remove(this);
			Disposed?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void Dispose(bool disposing) { }

		public override string ToString() => string.IsNullOrEmpty(Text) ? base.ToString() : Text;
	}

	public class ToolStripButton : ToolStripItem
	{
		private CheckState checkstate;

		public ToolStripButton() { }
		public ToolStripButton(string text) : base(text, null, null) { }
		public ToolStripButton(Image image) : base(null, image, null) { }
		public ToolStripButton(string text, Image image) : base(text, image, null) { }
		public ToolStripButton(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
		public ToolStripButton(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name) { }

		public bool CheckOnClick { get; set; }
		public bool Checked { get => checkstate != CheckState.Unchecked; set => CheckState = value ? CheckState.Checked : CheckState.Unchecked; }

		public CheckState CheckState
		{
			get => checkstate;
			set
			{
				if(checkstate == value) return;
				checkstate = value;
				CheckedChanged?.Invoke(this, EventArgs.Empty);
				CheckStateChanged?.Invoke(this, EventArgs.Empty);
				NotifyChanged();
			}
		}

		public event EventHandler CheckedChanged;
		public event EventHandler CheckStateChanged;

		protected override void OnClick(EventArgs e)
		{
			if(CheckOnClick) Checked = !Checked;
			base.OnClick(e);
		}
	}

	public class ToolStripLabel : ToolStripItem
	{
		public ToolStripLabel() { }
		public ToolStripLabel(string text) : base(text, null, null) { }
		public ToolStripLabel(Image image) : base(null, image, null) { }
		public ToolStripLabel(string text, Image image) : base(text, image, null) { }
		public bool IsLink { get; set; }
	}

	public class ToolStripStatusLabel : ToolStripLabel
	{
		public ToolStripStatusLabel() { }
		public ToolStripStatusLabel(string text) : base(text) { }
		public ToolStripStatusLabel(Image image) : base(image) { }
		public ToolStripStatusLabel(string text, Image image) : base(text, image) { }
		public bool Spring { get; set; }
		public ToolStripStatusLabelBorderSides BorderSides { get; set; }
		public Border3DStyle BorderStyle { get; set; } = Border3DStyle.Flat;
	}

	public class ToolStripSeparator : ToolStripItem
	{
		public ToolStripSeparator() { }
	}

	public abstract class ToolStripDropDownItem : ToolStripItem
	{
		private ToolStripDropDown dropdown;

		protected ToolStripDropDownItem() { }
		protected ToolStripDropDownItem(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
		protected ToolStripDropDownItem(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name) { }

		protected ToolStripDropDownItem(string text, Image image, params ToolStripItem[] dropDownItems) : base(text, image, null)
		{
			if(dropDownItems != null) DropDownItems.AddRange(dropDownItems);
		}

		public ToolStripDropDown DropDown
		{
			get => dropdown ??= CreateDefaultDropDown();
			set
			{
				dropdown = value;
				if(dropdown != null) dropdown.OwnerItem = this;
				NotifyChanged();
			}
		}

		public ToolStripItemCollection DropDownItems => DropDown.Items;
		public bool HasDropDownItems => dropdown != null && dropdown.Items.Count > 0;
		public bool HasDropDown => dropdown != null;
		public ToolStripDropDownDirection DropDownDirection { get; set; } = ToolStripDropDownDirection.Default;
		public bool IsOnDropDown => Owner is ToolStripDropDown;
		public bool DropDownVisible { get; private set; }

		public event EventHandler DropDownOpening;
		public event EventHandler DropDownOpened;
		public event EventHandler DropDownClosed;
		public event ToolStripItemClickedEventHandler DropDownItemClicked;

		protected virtual ToolStripDropDown CreateDefaultDropDown() => new ToolStripDropDownMenu { OwnerItem = this };

		/// <summary>ULDB: the UI calls these when it opens and closes the item's drop-down.</summary>
		public void RaiseDropDownOpening() { DropDownOpening?.Invoke(this, EventArgs.Empty); }
		public void RaiseDropDownOpened() { DropDownVisible = true; DropDownOpened?.Invoke(this, EventArgs.Empty); }
		public void RaiseDropDownClosed() { DropDownVisible = false; DropDownClosed?.Invoke(this, EventArgs.Empty); }
		public void RaiseDropDownItemClicked(ToolStripItem item) => DropDownItemClicked?.Invoke(this, new ToolStripItemClickedEventArgs(item));

		public void ShowDropDown() { }
		public void HideDropDown() { }
	}

	public class ToolStripMenuItem : ToolStripDropDownItem
	{
		private CheckState checkstate;
		private Keys shortcutkeys;
		private string shortcutkeydisplaystring;

		public ToolStripMenuItem() { }
		public ToolStripMenuItem(string text) : base(text, null, (EventHandler)null) { }
		public ToolStripMenuItem(Image image) : base(null, image, (EventHandler)null) { }
		public ToolStripMenuItem(string text, Image image) : base(text, image, (EventHandler)null) { }
		public ToolStripMenuItem(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
		public ToolStripMenuItem(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name) { }
		public ToolStripMenuItem(string text, Image image, params ToolStripItem[] dropDownItems) : base(text, image, dropDownItems) { }
		public ToolStripMenuItem(string text, Image image, EventHandler onClick, Keys shortcutKeys) : base(text, image, onClick) { shortcutkeys = shortcutKeys; }

		public bool CheckOnClick { get; set; }
		public bool Checked { get => checkstate != CheckState.Unchecked; set => CheckState = value ? CheckState.Checked : CheckState.Unchecked; }

		public CheckState CheckState
		{
			get => checkstate;
			set
			{
				if(checkstate == value) return;
				checkstate = value;
				CheckedChanged?.Invoke(this, EventArgs.Empty);
				CheckStateChanged?.Invoke(this, EventArgs.Empty);
				NotifyChanged();
			}
		}

		public Keys ShortcutKeys { get => shortcutkeys; set { shortcutkeys = value; NotifyChanged(); } }
		public string ShortcutKeyDisplayString { get => shortcutkeydisplaystring; set { shortcutkeydisplaystring = value; NotifyChanged(); } }
		public bool ShowShortcutKeys { get; set; } = true;

		public event EventHandler CheckedChanged;
		public event EventHandler CheckStateChanged;

		protected override void OnClick(EventArgs e)
		{
			if(CheckOnClick) Checked = !Checked;
			base.OnClick(e);
		}
	}

	public class ToolStripDropDownButton : ToolStripDropDownItem
	{
		public ToolStripDropDownButton() { }
		public ToolStripDropDownButton(string text) : base(text, null, (EventHandler)null) { }
		public ToolStripDropDownButton(Image image) : base(null, image, (EventHandler)null) { }
		public ToolStripDropDownButton(string text, Image image) : base(text, image, (EventHandler)null) { }
		public ToolStripDropDownButton(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
		public ToolStripDropDownButton(string text, Image image, params ToolStripItem[] dropDownItems) : base(text, image, dropDownItems) { }
		public bool ShowDropDownArrow { get; set; } = true;
	}

	public class ToolStripSplitButton : ToolStripDropDownItem
	{
		private ToolStripItem defaultitem;

		public ToolStripSplitButton() { }
		public ToolStripSplitButton(string text) : base(text, null, (EventHandler)null) { }
		public ToolStripSplitButton(Image image) : base(null, image, (EventHandler)null) { }
		public ToolStripSplitButton(string text, Image image) : base(text, image, (EventHandler)null) { }
		public ToolStripSplitButton(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
		public ToolStripSplitButton(string text, Image image, params ToolStripItem[] dropDownItems) : base(text, image, dropDownItems) { }

		public ToolStripItem DefaultItem
		{
			get => defaultitem;
			set { if(defaultitem == value) return; defaultitem = value; DefaultItemChanged?.Invoke(this, EventArgs.Empty); }
		}

		public event EventHandler ButtonClick;
		public event EventHandler ButtonDoubleClick;
		public event EventHandler DefaultItemChanged;

		/// <summary>Clicks the button part (not the arrow).</summary>
		public void PerformButtonClick()
		{
			if(!Enabled || !Available) return;
			ButtonClick?.Invoke(this, EventArgs.Empty);
			defaultitem?.PerformClick();
		}
	}

	/// <summary>Hosts a control on a tool strip. The UI renders hosted controls it knows about.</summary>
	public class ToolStripControlHost : ToolStripItem
	{
		public ToolStripControlHost(Control c)
		{
			Control = c ?? throw new ArgumentNullException(nameof(c));
			OnSubscribeControlEvents(c);
		}

		// Subclasses forward the hosted control's events here (WinForms calls these when hosting starts and ends).
		protected virtual void OnSubscribeControlEvents(Control control) { }
		protected virtual void OnUnsubscribeControlEvents(Control control) { }
		// Not in WinForms: lets ULDB's generated placeholders derive without a hosted control.
		protected ToolStripControlHost() : this(new Control()) { }
		public ToolStripControlHost(Control c, string name) : this(c) { Name = name ?? ""; }
		public Control Control { get; }
		public override string Text { get => Control.Text; set { Control.Text = value; NotifyChanged(); } }
		public override bool Enabled { get => Control.Enabled; set { Control.Enabled = value; NotifyChanged(); } }
		public bool Focused => false;
		public void Focus() { }
		public event EventHandler GotFocus;
		public event EventHandler LostFocus;
		public event KeyEventHandler KeyDown;
		public event KeyEventHandler KeyUp;
		public event KeyPressEventHandler KeyPress;
	}

	public class ToolStripTextBox : ToolStripControlHost
	{
		public ToolStripTextBox() : base(new Control()) { }
		public ToolStripTextBox(string name) : this() { Name = name ?? ""; }
		public int MaxLength { get; set; } = 32767;
		public BorderStyle BorderStyle { get; set; } = BorderStyle.Fixed3D;
		public bool ReadOnly { get; set; }
		public void SelectAll() { }
		public void Clear() => Text = "";
	}

	public class ToolStripComboBox : ToolStripControlHost
	{
		public ToolStripComboBox() : base(new ComboBox()) { }
		public ToolStripComboBox(string name) : this() { Name = name ?? ""; }
		public ComboBox ComboBox => (ComboBox)Control;
		public ComboBox.ObjectCollection Items => ComboBox.Items;
		public int DropDownWidth { get => ComboBox.DropDownWidth; set => ComboBox.DropDownWidth = value; }
		public ComboBoxStyle DropDownStyle { get => ComboBox.DropDownStyle; set => ComboBox.DropDownStyle = value; }

		public int SelectedIndex
		{
			get => ComboBox.SelectedIndex;
			set
			{
				if(ComboBox.SelectedIndex == value) return;
				ComboBox.SelectedIndex = value;
				SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
				NotifyChanged();
			}
		}

		public object SelectedItem { get => ComboBox.SelectedItem; set => SelectedIndex = Items.IndexOf(value); }
		public event EventHandler SelectedIndexChanged;
		public event EventHandler DropDown;
		public event EventHandler DropDownClosed;
	}

	public class ToolStrip : Control
	{
		public ToolStrip() { Items = new ToolStripItemCollection(this); }
		public ToolStrip(params ToolStripItem[] items) : this() { Items.AddRange(items); }

		public ToolStripItemCollection Items { get; }
		public Size ImageScalingSize { get; set; } = new Size(16, 16);
		public ToolStripGripStyle GripStyle { get; set; } = ToolStripGripStyle.Visible;
		public ToolStripLayoutStyle LayoutStyle { get; set; }
		public ToolStripRenderMode RenderMode { get; set; } = ToolStripRenderMode.ManagerRenderMode;
		public bool ShowItemToolTips { get; set; } = true;
		public bool CanOverflow { get; set; } = true;
		public bool Stretch { get; set; }
		public bool AllowMerge { get; set; } = true;
		public bool AutoSize { get; set; } = true;

		public event ToolStripItemClickedEventHandler ItemClicked;
		public event ToolStripItemEventHandler ItemAdded;
		public event ToolStripItemEventHandler ItemRemoved;

		/// <summary>ULDB: the UI reports a click on one of the strip's items.</summary>
		public void RaiseItemClicked(ToolStripItem item) => ItemClicked?.Invoke(this, new ToolStripItemClickedEventArgs(item));

		internal void OnItemAdded(ToolStripItem item) => ItemAdded?.Invoke(this, new ToolStripItemEventArgs(item));
		internal void OnItemRemoved(ToolStripItem item) => ItemRemoved?.Invoke(this, new ToolStripItemEventArgs(item));

		public ToolStripItem GetItemAt(int x, int y) => null;
	}

	public class MenuStrip : ToolStrip
	{
		public MenuStrip() { GripStyle = ToolStripGripStyle.Hidden; }
		public ToolStripMenuItem MdiWindowListItem { get; set; }
	}

	public class StatusStrip : ToolStrip
	{
		public StatusStrip() { GripStyle = ToolStripGripStyle.Hidden; }
		public bool SizingGrip { get; set; } = true;
	}

	public class ToolStripDropDown : ToolStrip
	{
		private ToolStripItem owneritem;

		public ToolStripItem OwnerItem { get => owneritem; set => owneritem = value; }
		public bool IsDropDown => true;
		public bool AutoClose { get; set; } = true;
		public bool DropShadowEnabled { get; set; } = true;

		public event CancelEventHandler Opening;
		public event EventHandler Opened;
		public event ToolStripDropDownClosingEventHandler Closing;
		public event ToolStripDropDownClosedEventHandler Closed;

		/// <summary>ULDB: the UI calls these around showing the drop-down. Opening can cancel it.</summary>
		public bool RaiseOpening()
		{
			CancelEventArgs e = new CancelEventArgs();
			Opening?.Invoke(this, e);
			return !e.Cancel;
		}

		public void RaiseOpened() => Opened?.Invoke(this, EventArgs.Empty);

		public bool RaiseClosing(ToolStripDropDownCloseReason reason)
		{
			ToolStripDropDownClosingEventArgs e = new ToolStripDropDownClosingEventArgs(reason);
			Closing?.Invoke(this, e);
			return !e.Cancel;
		}

		public void RaiseClosed(ToolStripDropDownCloseReason reason) => Closed?.Invoke(this, new ToolStripDropDownClosedEventArgs(reason));

		/// <summary>Shows the drop-down as a popup menu. Routed to the UI through ShowHandler.</summary>
		public void Show() => ShowHandler?.Invoke(this, null, Cursor.Position);
		public void Show(Point screenLocation) => ShowHandler?.Invoke(this, null, screenLocation);
		public void Show(int x, int y) => ShowHandler?.Invoke(this, null, new Point(x, y));
		public void Show(Control control, Point position) => ShowHandler?.Invoke(this, control, position);
		public void Show(Control control, int x, int y) => ShowHandler?.Invoke(this, control, new Point(x, y));
		public void Close() => CloseHandler?.Invoke(this);
		public void Close(ToolStripDropDownCloseReason reason) => CloseHandler?.Invoke(this);

		/// <summary>ULDB: installed by the UI to show popup menus (the control is null for screen coordinates).</summary>
		public static Action<ToolStripDropDown, Control, Point> ShowHandler { get; set; }
		public static Action<ToolStripDropDown> CloseHandler { get; set; }
	}

	public class ToolStripDropDownMenu : ToolStripDropDown
	{
		public bool ShowImageMargin { get; set; } = true;
		public bool ShowCheckMargin { get; set; }
	}

	public class ContextMenuStrip : ToolStripDropDownMenu
	{
		public ContextMenuStrip() { }
		public ContextMenuStrip(IContainer container) { }
		public Control SourceControl { get; set; }
	}

	/// <summary>A tool strip's items. Adding an item moves it from its previous owner, as in WinForms.</summary>
	public class ToolStripItemCollection : IList, IEnumerable<ToolStripItem>
	{
		private readonly List<ToolStripItem> items = new List<ToolStripItem>();

		public ToolStripItemCollection(ToolStrip owner, ToolStripItem[] value)
		{
			Owner = owner;
			if(value != null) AddRange(value);
		}

		internal ToolStripItemCollection(ToolStrip owner) { Owner = owner; }

		public ToolStrip Owner { get; }
		public int Count => items.Count;
		public bool IsReadOnly => false;

		/// <summary>ULDB: raised whenever items are added, removed or reordered.</summary>
		public event EventHandler Changed;

		public ToolStripItem this[int index] => items[index];

		public ToolStripItem this[string key]
		{
			get
			{
				int i = IndexOfKey(key);
				return i >= 0 ? items[i] : null;
			}
		}

		public int Add(ToolStripItem value)
		{
			Insert(items.Count, value);
			return items.Count - 1;
		}

		public ToolStripItem Add(string text) => Add(text, null, null);
		public ToolStripItem Add(Image image) => Add(null, image, null);
		public ToolStripItem Add(string text, Image image) => Add(text, image, null);

		public ToolStripItem Add(string text, Image image, EventHandler onClick)
		{
			// WinForms creates a menu item on menus and drop-downs, a button elsewhere.
			ToolStripItem item = Owner is MenuStrip || Owner is ToolStripDropDown
				? new ToolStripMenuItem(text, image, onClick)
				: (ToolStripItem)new ToolStripButton(text, image, onClick);
			Add(item);
			return item;
		}

		public void AddRange(ToolStripItem[] toolStripItems)
		{
			foreach(ToolStripItem item in toolStripItems) Add(item);
		}

		public void AddRange(ToolStripItemCollection toolStripItems)
		{
			foreach(ToolStripItem item in new List<ToolStripItem>(toolStripItems.items)) Add(item);
		}

		public void Insert(int index, ToolStripItem value)
		{
			if(value == null) throw new ArgumentNullException(nameof(value));
			if(value.Owner != null && value.Owner.Items != this) value.Owner.Items.Remove(value);
			else if(value.Owner == Owner && items.Contains(value)) { items.Remove(value); index = Math.Min(index, items.Count); }
			items.Insert(Math.Clamp(index, 0, items.Count), value);
			value.Owner = Owner;
			Owner?.OnItemAdded(value);
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void Remove(ToolStripItem value)
		{
			if(value == null || !items.Remove(value)) return;
			value.Owner = null;
			Owner?.OnItemRemoved(value);
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public void RemoveAt(int index) => Remove(items[index]);

		public void RemoveByKey(string key)
		{
			int i = IndexOfKey(key);
			if(i >= 0) RemoveAt(i);
		}

		public void Clear()
		{
			foreach(ToolStripItem item in items) { item.Owner = null; Owner?.OnItemRemoved(item); }
			items.Clear();
			Changed?.Invoke(this, EventArgs.Empty);
		}

		public bool Contains(ToolStripItem value) => items.Contains(value);
		public bool ContainsKey(string key) => IndexOfKey(key) >= 0;
		public int IndexOf(ToolStripItem value) => items.IndexOf(value);

		public int IndexOfKey(string key)
		{
			if(string.IsNullOrEmpty(key)) return -1;
			for(int i = 0; i < items.Count; i++)
				if(string.Equals(items[i].Name, key, StringComparison.OrdinalIgnoreCase)) return i;
			return -1;
		}

		public ToolStripItem[] Find(string key, bool searchAllChildren)
		{
			List<ToolStripItem> found = new List<ToolStripItem>();
			Find(key, searchAllChildren, found);
			return found.ToArray();
		}

		private void Find(string key, bool searchAllChildren, List<ToolStripItem> found)
		{
			foreach(ToolStripItem item in items)
			{
				if(string.Equals(item.Name, key, StringComparison.OrdinalIgnoreCase)) found.Add(item);
				if(searchAllChildren && item is ToolStripDropDownItem dd && dd.HasDropDown) dd.DropDownItems.Find(key, true, found);
			}
		}

		public void CopyTo(ToolStripItem[] array, int index) => items.CopyTo(array, index);
		public IEnumerator<ToolStripItem> GetEnumerator() => items.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();

		// Non-generic IList, so upstream's foreach over ToolStripItem/object and LINQ casts work.
		object IList.this[int index] { get => items[index]; set => throw new NotSupportedException(); }
		bool IList.IsFixedSize => false;
		int IList.Add(object value) => Add((ToolStripItem)value);
		bool IList.Contains(object value) => value is ToolStripItem i && Contains(i);
		int IList.IndexOf(object value) => value is ToolStripItem i ? IndexOf(i) : -1;
		void IList.Insert(int index, object value) => Insert(index, (ToolStripItem)value);
		void IList.Remove(object value) { if(value is ToolStripItem i) Remove(i); }
		bool ICollection.IsSynchronized => false;
		object ICollection.SyncRoot => this;
		void ICollection.CopyTo(Array array, int index) => ((ICollection)items).CopyTo(array, index);
	}

	public class ToolStripItemClickedEventArgs : EventArgs
	{
		public ToolStripItemClickedEventArgs(ToolStripItem clickedItem) { ClickedItem = clickedItem; }
		public ToolStripItem ClickedItem { get; }
	}

	public class ToolStripItemEventArgs : EventArgs
	{
		public ToolStripItemEventArgs(ToolStripItem item) { Item = item; }
		public ToolStripItem Item { get; }
	}

	public class ToolStripDropDownClosingEventArgs : CancelEventArgs
	{
		public ToolStripDropDownClosingEventArgs(ToolStripDropDownCloseReason reason) { CloseReason = reason; }
		public ToolStripDropDownCloseReason CloseReason { get; }
	}

	public class ToolStripDropDownClosedEventArgs : EventArgs
	{
		public ToolStripDropDownClosedEventArgs(ToolStripDropDownCloseReason reason) { CloseReason = reason; }
		public ToolStripDropDownCloseReason CloseReason { get; }
	}

	public delegate void ToolStripItemClickedEventHandler(object sender, ToolStripItemClickedEventArgs e);
	public delegate void ToolStripItemEventHandler(object sender, ToolStripItemEventArgs e);
	public delegate void ToolStripDropDownClosingEventHandler(object sender, ToolStripDropDownClosingEventArgs e);
	public delegate void ToolStripDropDownClosedEventHandler(object sender, ToolStripDropDownClosedEventArgs e);
}
