// ULDB compatibility layer: a state-only model of WinForms' TreeView (and ImageList), so upstream
// trees (thing types, texture sets, MultiSelectTreeview's own selection logic) run unchanged.
//
// It keeps nodes, expansion, the selected node and per-node colours, and raises the events
// upstream subscribes to or overrides (OnBeforeSelect, OnMouseDown, …). An Avalonia view shows it
// and installs NodeLocator so GetNodeAt and TreeNode.Bounds answer with the view's layout; it then
// passes mouse input through RaiseMouseDown/RaiseMouseUp, as WinForms' native tree would.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms
{
	#region ================== TreeView

	public class TreeView : Control
	{
		private TreeNode selectednode;
		private int updatecount;

		public TreeView()
		{
			Nodes = new TreeNodeCollection(this, null);
		}

		public TreeNodeCollection Nodes { get; }
		public ImageList ImageList { get; set; }
		public ImageList StateImageList { get; set; }
		public int ImageIndex { get; set; }
		public int SelectedImageIndex { get; set; }
		public string ImageKey { get; set; } = "";
		public string SelectedImageKey { get; set; } = "";
		public bool ShowLines { get; set; } = true;
		public bool ShowRootLines { get; set; } = true;
		public bool ShowPlusMinus { get; set; } = true;
		public bool ShowNodeToolTips { get; set; }
		public bool HideSelection { get; set; } = true;
		public bool FullRowSelect { get; set; }
		public bool HotTracking { get; set; }
		public bool LabelEdit { get; set; }
		public bool CheckBoxes { get; set; }
		public bool Scrollable { get; set; } = true;
		public bool Sorted { get; set; }
		public IComparer TreeViewNodeSorter { get; set; }
		public int Indent { get; set; } = 19;
		public int ItemHeight { get; set; } = 18;
		public Color LineColor { get; set; } = SystemColors.GrayText;
		public string PathSeparator { get; set; } = "\\";
		public BorderStyle BorderStyle { get; set; } = BorderStyle.Fixed3D;
		public TreeViewDrawMode DrawMode { get; set; }
		public TreeNode TopNode { get; set; }
		public int VisibleCount => Math.Max(1, Height / Math.Max(1, ItemHeight));

		/// <summary>ULDB: set by the view: the node at a point (control coordinates), or null.</summary>
		public Func<Point, TreeNode> NodeLocator { get; set; }

		/// <summary>ULDB: set by the view: a node's bounds (its text area, control coordinates).</summary>
		public Func<TreeNode, Rectangle> NodeBoundsProvider { get; set; }

		/// <summary>ULDB: raised when nodes, expansion, selection or node colours change.</summary>
		public event EventHandler TreeChanged;

		/// <summary>The node whose label is being edited (TreeNode.BeginEdit), or null. The view shows an editor for it.</summary>
		public TreeNode EditingNode { get; internal set; }

		internal bool RaiseBeforeLabelEdit(TreeNode node)
		{
			NodeLabelEditEventArgs e = new NodeLabelEditEventArgs(node);
			OnBeforeLabelEdit(e);
			return !e.CancelEdit;
		}

		/// <summary>
		/// ULDB: the view finished editing a label: AfterLabelEdit is raised (label is null when the user
		/// cancelled), and the node takes the new text unless the handler cancelled the edit, as in WinForms.
		/// </summary>
		public void EndLabelEdit(TreeNode node, string label)
		{
			if(EditingNode != node) return;
			EditingNode = null;
			NodeLabelEditEventArgs e = new NodeLabelEditEventArgs(node, label);
			OnAfterLabelEdit(e);
			if(!e.CancelEdit && label != null) node.Text = label;
			OnTreeChanged();
		}

		internal void OnTreeChanged()
		{
			if(updatecount == 0) TreeChanged?.Invoke(this, EventArgs.Empty);
		}

		public TreeNode SelectedNode
		{
			get => selectednode;
			set => Select(value, TreeViewAction.Unknown);
		}

		/// <summary>Selects a node as WinForms does: BeforeSelect (cancelable), then AfterSelect.</summary>
		internal void Select(TreeNode node, TreeViewAction action)
		{
			if(node == selectednode) return;
			TreeViewCancelEventArgs before = new TreeViewCancelEventArgs(node, false, action);
			OnBeforeSelect(before);
			if(before.Cancel) return;
			selectednode = node;
			OnAfterSelect(new TreeViewEventArgs(node, action));
			OnTreeChanged();
		}

		#region Events

		public event TreeViewCancelEventHandler BeforeSelect;
		public event TreeViewEventHandler AfterSelect;
		public event TreeViewCancelEventHandler BeforeExpand;
		public event TreeViewEventHandler AfterExpand;
		public event TreeViewCancelEventHandler BeforeCollapse;
		public event TreeViewEventHandler AfterCollapse;
		public event TreeViewCancelEventHandler BeforeCheck;
		public event TreeViewEventHandler AfterCheck;
		public event TreeNodeMouseClickEventHandler NodeMouseClick;
		public event TreeNodeMouseClickEventHandler NodeMouseDoubleClick;
		public event TreeNodeMouseHoverEventHandler NodeMouseHover;
		public event NodeLabelEditEventHandler BeforeLabelEdit;
		public event NodeLabelEditEventHandler AfterLabelEdit;
		public event ItemDragEventHandler ItemDrag;
		public event DrawTreeNodeEventHandler DrawNode;

		protected virtual void OnBeforeSelect(TreeViewCancelEventArgs e) => BeforeSelect?.Invoke(this, e);
		protected virtual void OnAfterSelect(TreeViewEventArgs e) => AfterSelect?.Invoke(this, e);
		protected virtual void OnBeforeExpand(TreeViewCancelEventArgs e) => BeforeExpand?.Invoke(this, e);
		protected virtual void OnAfterExpand(TreeViewEventArgs e) => AfterExpand?.Invoke(this, e);
		protected virtual void OnBeforeCollapse(TreeViewCancelEventArgs e) => BeforeCollapse?.Invoke(this, e);
		protected virtual void OnAfterCollapse(TreeViewEventArgs e) => AfterCollapse?.Invoke(this, e);
		protected virtual void OnBeforeCheck(TreeViewCancelEventArgs e) => BeforeCheck?.Invoke(this, e);
		protected virtual void OnAfterCheck(TreeViewEventArgs e) => AfterCheck?.Invoke(this, e);
		protected virtual void OnNodeMouseClick(TreeNodeMouseClickEventArgs e) => NodeMouseClick?.Invoke(this, e);
		protected virtual void OnNodeMouseDoubleClick(TreeNodeMouseClickEventArgs e) => NodeMouseDoubleClick?.Invoke(this, e);
		protected virtual void OnNodeMouseHover(TreeNodeMouseHoverEventArgs e) => NodeMouseHover?.Invoke(this, e);
		protected virtual void OnBeforeLabelEdit(NodeLabelEditEventArgs e) => BeforeLabelEdit?.Invoke(this, e);
		protected virtual void OnAfterLabelEdit(NodeLabelEditEventArgs e) => AfterLabelEdit?.Invoke(this, e);
		protected virtual void OnItemDrag(ItemDragEventArgs e) => ItemDrag?.Invoke(this, e);
		protected virtual void OnDrawNode(DrawTreeNodeEventArgs e) => DrawNode?.Invoke(this, e);

		internal bool RaiseBeforeExpand(TreeNode n) { TreeViewCancelEventArgs e = new TreeViewCancelEventArgs(n, false, TreeViewAction.Expand); OnBeforeExpand(e); return !e.Cancel; }
		internal void RaiseAfterExpand(TreeNode n) => OnAfterExpand(new TreeViewEventArgs(n, TreeViewAction.Expand));
		internal bool RaiseBeforeCollapse(TreeNode n) { TreeViewCancelEventArgs e = new TreeViewCancelEventArgs(n, false, TreeViewAction.Collapse); OnBeforeCollapse(e); return !e.Cancel; }
		internal void RaiseAfterCollapse(TreeNode n) => OnAfterCollapse(new TreeViewEventArgs(n, TreeViewAction.Collapse));
		internal bool RaiseBeforeCheck(TreeNode n) { TreeViewCancelEventArgs e = new TreeViewCancelEventArgs(n, false, TreeViewAction.Unknown); OnBeforeCheck(e); return !e.Cancel; }
		internal void RaiseAfterCheck(TreeNode n) => OnAfterCheck(new TreeViewEventArgs(n, TreeViewAction.Unknown));

		#endregion

		#region View input (ULDB)

		/// <summary>
		/// A mouse press on the tree: MouseDown, then (as the native tree does) selection of the node
		/// under the pointer by mouse, and NodeMouseClick on release (see RaiseNodeClick).
		/// </summary>
		public void RaiseTreeMouseDown(MouseEventArgs e)
		{
			RaiseMouseDown(e);
			TreeNode node = GetNodeAt(e.X, e.Y);
			if(node != null && e.Button == MouseButtons.Left) Select(node, TreeViewAction.ByMouse);
		}

		public void RaiseTreeMouseUp(MouseEventArgs e)
		{
			RaiseMouseUp(e);
			TreeNode node = GetNodeAt(e.X, e.Y);
			if(node != null) OnNodeMouseClick(new TreeNodeMouseClickEventArgs(node, e.Button, e.Clicks, e.X, e.Y));
			RaiseClick();
		}

		public void RaiseNodeDoubleClick(TreeNode node, MouseEventArgs e)
		{
			if(node != null) OnNodeMouseDoubleClick(new TreeNodeMouseClickEventArgs(node, e.Button, 2, e.X, e.Y));
			RaiseMouseDoubleClick(e);
		}

		/// <summary>Keyboard selection (arrow keys in the view).</summary>
		public void SelectByKeyboard(TreeNode node) => Select(node, TreeViewAction.ByKeyboard);

		#endregion

		#region Methods

		public TreeNode GetNodeAt(int x, int y) => NodeLocator?.Invoke(new Point(x, y));
		public TreeNode GetNodeAt(Point pt) => NodeLocator?.Invoke(pt);
		public TreeViewHitTestInfo HitTest(Point pt) => new TreeViewHitTestInfo(GetNodeAt(pt), TreeViewHitTestLocations.Label);
		public TreeViewHitTestInfo HitTest(int x, int y) => HitTest(new Point(x, y));

		public int GetNodeCount(bool includeSubTrees) => Nodes.CountNodes(includeSubTrees);

		public void BeginUpdate() => updatecount++;
		public void EndUpdate()
		{
			if(updatecount > 0) updatecount--;
			OnTreeChanged();
		}

		public void ExpandAll() { foreach(TreeNode n in Nodes) n.ExpandAll(); }
		public void CollapseAll() { foreach(TreeNode n in Nodes) n.Collapse(false); }

		public void Sort()
		{
			Nodes.SortRecursive(TreeViewNodeSorter);
			OnTreeChanged();
		}

		public override void Refresh() { OnTreeChanged(); base.Refresh(); }
		public override void Invalidate() { OnTreeChanged(); base.Invalidate(); }

		#endregion
	}

	public class TreeViewHitTestInfo
	{
		public TreeViewHitTestInfo(TreeNode hitNode, TreeViewHitTestLocations hitLocation) { Node = hitNode; Location = hitLocation; }
		public TreeNode Node { get; }
		public TreeViewHitTestLocations Location { get; }
	}

	#endregion

	#region ================== Nodes

	public class TreeNode : ICloneable
	{
		private string text = "";
		private bool expanded, check;
		private Color forecolor = Color.Empty, backcolor = Color.Empty;
		private Font nodefont;
		private int imageindex = -1, selectedimageindex = -1;

		public TreeNode() { Nodes = new TreeNodeCollection(null, this); }
		public TreeNode(string text) : this() { this.text = text ?? ""; }
		public TreeNode(string text, TreeNode[] children) : this(text) { Nodes.AddRange(children); }
		public TreeNode(string text, int imageIndex, int selectedImageIndex) : this(text) { imageindex = imageIndex; selectedimageindex = selectedImageIndex; }
		public TreeNode(string text, int imageIndex, int selectedImageIndex, TreeNode[] children) : this(text, imageIndex, selectedImageIndex) { Nodes.AddRange(children); }

		public TreeNodeCollection Nodes { get; }
		public TreeNode Parent { get; internal set; }
		internal TreeView OwnerTree { get; set; }

		public TreeView TreeView
		{
			get
			{
				TreeNode n = this;
				while(n.Parent != null) n = n.Parent;
				return n.OwnerTree;
			}
		}

		public string Text { get => text; set { text = value ?? ""; Changed(); } }
		public string Name { get; set; } = "";
		public object Tag { get; set; }
		public string ToolTipText { get; set; } = "";
		public int ImageIndex { get => imageindex; set { imageindex = value; Changed(); } }
		public int SelectedImageIndex { get => selectedimageindex; set { selectedimageindex = value; Changed(); } }
		public string ImageKey { get; set; } = "";
		public string SelectedImageKey { get; set; } = "";
		public int StateImageIndex { get; set; } = -1;
		public Color ForeColor { get => forecolor; set { forecolor = value; Changed(); } }
		public Color BackColor { get => backcolor; set { backcolor = value; Changed(); } }
		public Font NodeFont { get => nodefont; set { nodefont = value; Changed(); } }
		public ContextMenuStrip ContextMenuStrip { get; set; }

		public bool Checked
		{
			get => check;
			set
			{
				if(check == value) return;
				TreeView tv = TreeView;
				if(tv != null && !tv.RaiseBeforeCheck(this)) return;
				check = value;
				tv?.RaiseAfterCheck(this);
				Changed();
			}
		}

		public int Index => Parent != null ? Parent.Nodes.IndexOf(this) : TreeView?.Nodes.IndexOf(this) ?? -1;
		public int Level { get { int l = 0; for(TreeNode n = Parent; n != null; n = n.Parent) l++; return l; } }
		public bool IsExpanded => expanded;
		public bool IsSelected => TreeView != null && TreeView.SelectedNode == this;
		public bool IsEditing => TreeView?.EditingNode == this;

		/// <summary>Visible when all ancestors are expanded (scrolling is the view's business).</summary>
		public bool IsVisible
		{
			get { for(TreeNode n = Parent; n != null; n = n.Parent) if(!n.expanded) return false; return TreeView != null; }
		}

		public string FullPath
		{
			get
			{
				string sep = TreeView?.PathSeparator ?? "\\";
				return Parent == null ? text : Parent.FullPath + sep + text;
			}
		}

		/// <summary>The node's text bounds in the view (from the view's NodeBoundsProvider).</summary>
		public Rectangle Bounds => TreeView?.NodeBoundsProvider?.Invoke(this) ?? Rectangle.Empty;

		private TreeNodeCollection Siblings => Parent != null ? Parent.Nodes : TreeView?.Nodes;
		public TreeNode FirstNode => Nodes.Count > 0 ? Nodes[0] : null;
		public TreeNode LastNode => Nodes.Count > 0 ? Nodes[Nodes.Count - 1] : null;
		public TreeNode NextNode { get { TreeNodeCollection s = Siblings; int i = Index; return s != null && i >= 0 && i + 1 < s.Count ? s[i + 1] : null; } }
		public TreeNode PrevNode { get { TreeNodeCollection s = Siblings; int i = Index; return s != null && i > 0 ? s[i - 1] : null; } }

		/// <summary>The next node in display order among visible (expanded) nodes.</summary>
		public TreeNode NextVisibleNode
		{
			get
			{
				if(!IsVisible) return null;
				if(expanded && Nodes.Count > 0) return Nodes[0];
				for(TreeNode n = this; n != null; n = n.Parent)
				{
					TreeNode next = n.NextNode;
					if(next != null) return next;
				}
				return null;
			}
		}

		public TreeNode PrevVisibleNode
		{
			get
			{
				if(!IsVisible) return null;
				TreeNode prev = PrevNode;
				if(prev == null) return Parent;
				while(prev.expanded && prev.Nodes.Count > 0) prev = prev.Nodes[prev.Nodes.Count - 1];
				return prev;
			}
		}

		public TreeNode NextVisibleNodeOrNull => NextVisibleNode;

		public void Expand()
		{
			if(expanded) return;
			TreeView tv = TreeView;
			if(tv != null && !tv.RaiseBeforeExpand(this)) return;
			expanded = true;
			tv?.RaiseAfterExpand(this);
			Changed();
		}

		public void ExpandAll()
		{
			Expand();
			foreach(TreeNode n in Nodes) n.ExpandAll();
		}

		public void Collapse() => Collapse(false);

		public void Collapse(bool ignoreChildren)
		{
			if(!ignoreChildren) foreach(TreeNode n in Nodes) n.Collapse(false);
			if(!expanded) return;
			TreeView tv = TreeView;
			if(tv != null && !tv.RaiseBeforeCollapse(this)) return;
			expanded = false;
			tv?.RaiseAfterCollapse(this);
			Changed();
		}

		public void Toggle() { if(expanded) Collapse(); else Expand(); }

		/// <summary>Expands the parents so the node can be seen; the view scrolls to it.</summary>
		public void EnsureVisible()
		{
			for(TreeNode n = Parent; n != null; n = n.Parent) n.Expand();
			TreeView tv = TreeView;
			if(tv != null) { tv.TopNode = this; tv.OnTreeChanged(); }
		}

		public void Remove()
		{
			TreeNodeCollection s = Siblings;
			s?.Remove(this);
		}

		/// <summary>Starts editing the label (TreeView.LabelEdit must be on; BeforeLabelEdit may refuse).</summary>
		public void BeginEdit()
		{
			TreeView tv = TreeView;
			if(tv == null || !tv.LabelEdit || tv.EditingNode == this) return;
			if(!tv.RaiseBeforeLabelEdit(this)) return;
			tv.EditingNode = this;
			tv.OnTreeChanged();
		}

		/// <summary>Ends editing without going through the view (WinForms: cancel discards the typed text).</summary>
		public void EndEdit(bool cancel)
		{
			TreeView tv = TreeView;
			if(tv == null || tv.EditingNode != this) return;
			tv.EditingNode = null;
			tv.OnTreeChanged();
		}

		public virtual object Clone()
		{
			TreeNode c = new TreeNode(text, imageindex, selectedimageindex)
			{
				Name = Name, Tag = Tag, ToolTipText = ToolTipText, ForeColor = forecolor, BackColor = backcolor, NodeFont = nodefont,
			};
			foreach(TreeNode n in Nodes) c.Nodes.Add((TreeNode)n.Clone());
			return c;
		}

		internal void Changed() => TreeView?.OnTreeChanged();

		public override string ToString() => "TreeNode: " + text;
	}

	public class TreeNodeCollection : IList, IEnumerable<TreeNode>
	{
		private readonly TreeView tree;
		private readonly TreeNode owner;
		private readonly List<TreeNode> nodes = new List<TreeNode>();

		internal TreeNodeCollection(TreeView tree, TreeNode owner)
		{
			this.tree = tree;
			this.owner = owner;
		}

		private TreeView Tree => tree ?? owner?.TreeView;

		public int Count => nodes.Count;
		public TreeNode this[int index] { get => nodes[index]; set { Detach(nodes[index]); Attach(value); nodes[index] = value; Changed(); } }
		public TreeNode this[string key] { get { int i = IndexOfKey(key); return i >= 0 ? nodes[i] : null; } }

		private void Attach(TreeNode node)
		{
			node.Remove(); // a node lives in one collection
			node.Parent = owner;
			node.OwnerTree = tree;
		}

		private static void Detach(TreeNode node)
		{
			node.Parent = null;
			node.OwnerTree = null;
		}

		private void Changed() => Tree?.OnTreeChanged();

		public virtual TreeNode Add(string text) { TreeNode n = new TreeNode(text); Add(n); return n; }
		public virtual TreeNode Add(string key, string text) { TreeNode n = new TreeNode(text) { Name = key }; Add(n); return n; }
		public virtual TreeNode Add(string key, string text, int imageIndex) { TreeNode n = new TreeNode(text, imageIndex, imageIndex) { Name = key }; Add(n); return n; }
		public virtual TreeNode Add(string key, string text, int imageIndex, int selectedImageIndex) { TreeNode n = new TreeNode(text, imageIndex, selectedImageIndex) { Name = key }; Add(n); return n; }
		public virtual TreeNode Add(string key, string text, string imageKey) { TreeNode n = new TreeNode(text) { Name = key, ImageKey = imageKey }; Add(n); return n; }

		public virtual int Add(TreeNode node)
		{
			Attach(node);
			TreeView tv = Tree;
			if(tv != null && tv.Sorted)
			{
				int i = 0;
				while(i < nodes.Count && Compare(tv, nodes[i], node) <= 0) i++;
				nodes.Insert(i, node);
				Changed();
				return i;
			}
			nodes.Add(node);
			Changed();
			return nodes.Count - 1;
		}

		public virtual void AddRange(TreeNode[] newnodes)
		{
			TreeView tv = Tree;
			tv?.BeginUpdate();
			foreach(TreeNode n in newnodes) Add(n);
			tv?.EndUpdate();
		}

		public virtual void Insert(int index, TreeNode node)
		{
			Attach(node);
			nodes.Insert(Math.Max(0, Math.Min(index, nodes.Count)), node);
			Changed();
		}

		public TreeNode Insert(int index, string text) { TreeNode n = new TreeNode(text); Insert(index, n); return n; }
		public TreeNode Insert(int index, string key, string text) { TreeNode n = new TreeNode(text) { Name = key }; Insert(index, n); return n; }

		public void Remove(TreeNode node)
		{
			if(!nodes.Remove(node)) return;
			TreeView tv = Tree;
			if(tv != null && tv.SelectedNode != null && IsSelfOrDescendant(node, tv.SelectedNode)) tv.SelectedNode = null;
			Detach(node);
			Changed();
		}

		private static bool IsSelfOrDescendant(TreeNode ancestor, TreeNode node)
		{
			for(TreeNode n = node; n != null; n = n.Parent) if(n == ancestor) return true;
			return false;
		}

		public virtual void RemoveAt(int index) => Remove(nodes[index]);
		public virtual void RemoveByKey(string key) { int i = IndexOfKey(key); if(i >= 0) RemoveAt(i); }

		public virtual void Clear()
		{
			TreeView tv = Tree;
			if(tv?.SelectedNode != null && (owner == null || IsSelfOrDescendant(owner, tv.SelectedNode)) && tv.SelectedNode != owner) tv.SelectedNode = null;
			foreach(TreeNode n in nodes) Detach(n);
			nodes.Clear();
			Changed();
		}

		public bool Contains(TreeNode node) => nodes.Contains(node);
		public virtual bool ContainsKey(string key) => IndexOfKey(key) >= 0;
		public int IndexOf(TreeNode node) => nodes.IndexOf(node);
		public virtual int IndexOfKey(string key)
		{
			if(string.IsNullOrEmpty(key)) return -1;
			for(int i = 0; i < nodes.Count; i++) if(string.Equals(nodes[i].Name, key, StringComparison.OrdinalIgnoreCase)) return i;
			return -1;
		}

		public TreeNode[] Find(string key, bool searchAllChildren)
		{
			List<TreeNode> found = new List<TreeNode>();
			void Search(TreeNodeCollection c)
			{
				foreach(TreeNode n in c.nodes)
				{
					if(string.Equals(n.Name, key, StringComparison.OrdinalIgnoreCase)) found.Add(n);
					if(searchAllChildren) Search(n.Nodes);
				}
			}
			Search(this);
			return found.ToArray();
		}

		public void CopyTo(Array dest, int index) => ((ICollection)nodes).CopyTo(dest, index);

		internal int CountNodes(bool deep)
		{
			int c = nodes.Count;
			if(deep) foreach(TreeNode n in nodes) c += n.Nodes.CountNodes(true);
			return c;
		}

		internal void SortRecursive(IComparer comparer)
		{
			TreeView tv = Tree;
			nodes.Sort((a, b) => comparer != null ? comparer.Compare(a, b) : Compare(tv, a, b));
			foreach(TreeNode n in nodes) n.Nodes.SortRecursive(comparer);
		}

		private static int Compare(TreeView tv, TreeNode a, TreeNode b)
		{
			if(tv?.TreeViewNodeSorter != null) return tv.TreeViewNodeSorter.Compare(a, b);
			return string.Compare(a.Text, b.Text, StringComparison.CurrentCulture);
		}

		public IEnumerator<TreeNode> GetEnumerator() => nodes.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => nodes.GetEnumerator();

		bool IList.IsFixedSize => false;
		public bool IsReadOnly => false;
		object IList.this[int index] { get => nodes[index]; set => this[index] = (TreeNode)value; }
		int IList.Add(object value) => value is TreeNode n ? Add(n) : Add(value?.ToString()).Index;
		bool IList.Contains(object value) => nodes.Contains(value as TreeNode);
		int IList.IndexOf(object value) => nodes.IndexOf(value as TreeNode);
		void IList.Insert(int index, object value) => Insert(index, (TreeNode)value);
		void IList.Remove(object value) => Remove(value as TreeNode);
		bool ICollection.IsSynchronized => false;
		object ICollection.SyncRoot => this;
	}

	#endregion

	#region ================== Event args

	public delegate void TreeViewEventHandler(object sender, TreeViewEventArgs e);
	public delegate void TreeViewCancelEventHandler(object sender, TreeViewCancelEventArgs e);
	public delegate void TreeNodeMouseClickEventHandler(object sender, TreeNodeMouseClickEventArgs e);
	public delegate void TreeNodeMouseHoverEventHandler(object sender, TreeNodeMouseHoverEventArgs e);
	public delegate void NodeLabelEditEventHandler(object sender, NodeLabelEditEventArgs e);
	public delegate void ItemDragEventHandler(object sender, ItemDragEventArgs e);
	public delegate void DrawTreeNodeEventHandler(object sender, DrawTreeNodeEventArgs e);

	public class TreeViewEventArgs : EventArgs
	{
		public TreeViewEventArgs(TreeNode node) : this(node, TreeViewAction.Unknown) { }
		public TreeViewEventArgs(TreeNode node, TreeViewAction action) { Node = node; Action = action; }
		public TreeNode Node { get; }
		public TreeViewAction Action { get; }
	}

	public class TreeViewCancelEventArgs : CancelEventArgs
	{
		public TreeViewCancelEventArgs(TreeNode node, bool cancel, TreeViewAction action) : base(cancel) { Node = node; Action = action; }
		public TreeNode Node { get; }
		public TreeViewAction Action { get; }
	}

	public class TreeNodeMouseClickEventArgs : MouseEventArgs
	{
		public TreeNodeMouseClickEventArgs(TreeNode node, MouseButtons button, int clicks, int x, int y) : base(button, clicks, x, y, 0) { Node = node; }
		public TreeNode Node { get; }
	}

	public class TreeNodeMouseHoverEventArgs : EventArgs
	{
		public TreeNodeMouseHoverEventArgs(TreeNode node) { Node = node; }
		public TreeNode Node { get; }
	}

	public class NodeLabelEditEventArgs : EventArgs
	{
		public NodeLabelEditEventArgs(TreeNode node) : this(node, null) { }
		public NodeLabelEditEventArgs(TreeNode node, string label) { Node = node; Label = label; }
		public TreeNode Node { get; }
		public string Label { get; }
		public bool CancelEdit { get; set; }
	}

	public class ItemDragEventArgs : EventArgs
	{
		public ItemDragEventArgs(MouseButtons button) : this(button, null) { }
		public ItemDragEventArgs(MouseButtons button, object item) { Button = button; Item = item; }
		public MouseButtons Button { get; }
		public object Item { get; }
	}

	public class DrawTreeNodeEventArgs : EventArgs
	{
		public DrawTreeNodeEventArgs(Graphics graphics, TreeNode node, Rectangle bounds, TreeNodeStates state)
		{
			Graphics = graphics; Node = node; Bounds = bounds; State = state;
		}
		public Graphics Graphics { get; }
		public TreeNode Node { get; }
		public Rectangle Bounds { get; }
		public TreeNodeStates State { get; }
		public bool DrawDefault { get; set; }
	}

	#endregion

	#region ================== ImageList

	/// <summary>Images for trees and lists. Images serialized in a .resx (ImageStream) aren't
	/// loaded: ULDB's views draw their own icons for those indexes.</summary>
	public sealed class ImageList : Component
	{
		public ImageList() { Images = new ImageCollection(); }
		public ImageList(IContainer container) : this() { container?.Add(this); }

		public ImageCollection Images { get; }
		public Size ImageSize { get; set; } = new Size(16, 16);
		public ColorDepth ColorDepth { get; set; } = ColorDepth.Depth8Bit;
		public Color TransparentColor { get; set; } = Color.Transparent;
		public ImageListStreamer ImageStream { get; set; }
		public object Tag { get; set; }

		public sealed class ImageCollection : IEnumerable<Image>
		{
			private readonly List<Image> images = new List<Image>();
			private readonly Dictionary<int, string> keys = new Dictionary<int, string>();

			public int Count => images.Count;
			public bool Empty => images.Count == 0;
			public Image this[int index] { get => index >= 0 && index < images.Count ? images[index] : null; set => images[index] = value; }
			public Image this[string key] { get { int i = IndexOfKey(key); return i >= 0 ? this[i] : null; } }
			public StringCollectionView Keys => new StringCollectionView(keys);

			public void Add(Image image) => images.Add(image);
			public void Add(string key, Image image) { keys[images.Count] = key; images.Add(image); }
			public void Add(Icon icon) => images.Add(icon.ToBitmap());
			public void Add(string key, Icon icon) => Add(key, icon.ToBitmap());
			public void AddRange(Image[] newimages) => images.AddRange(newimages);
			public void Clear() { images.Clear(); keys.Clear(); }
			public void RemoveAt(int index) { images.RemoveAt(index); keys.Remove(index); }
			public bool ContainsKey(string key) => IndexOfKey(key) >= 0;
			public int IndexOfKey(string key)
			{
				foreach(KeyValuePair<int, string> p in keys) if(string.Equals(p.Value, key, StringComparison.OrdinalIgnoreCase)) return p.Key;
				return -1;
			}

			/// <summary>Names an image (the designer does this even when the images weren't loaded).</summary>
			public void SetKeyName(int index, string name) => keys[index] = name;

			public IEnumerator<Image> GetEnumerator() => images.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => images.GetEnumerator();
		}

		public sealed class StringCollectionView : IEnumerable<string>
		{
			private readonly Dictionary<int, string> keys;
			internal StringCollectionView(Dictionary<int, string> keys) { this.keys = keys; }
			public int Count => keys.Count;
			public IEnumerator<string> GetEnumerator() => keys.Values.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => keys.Values.GetEnumerator();
		}
	}

	/// <summary>Stand-in for serialized ImageList data (never loaded on Linux).</summary>
	[Serializable]
	public sealed class ImageListStreamer : IDisposable
	{
		public void Dispose() { }
	}

	#endregion
}
