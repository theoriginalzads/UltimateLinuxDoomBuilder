#!/bin/sh
# Regenerates ULDB.Compat's enum files from Mono's .NET Framework reference assemblies.
# Run from the repository root. Add an enum's name to the list when compat needs it.
set -e
REF=${REF:-/usr/lib/mono/4.7.2-api}   # from the mono package
C=Source/ULDB/Compat
dotnet run --project tools/compatgen -- $REF/System.Windows.Forms.dll \
  Keys MouseButtons DialogResult MessageBoxButtons MessageBoxIcon MessageBoxDefaultButton \
  MessageBoxOptions FormWindowState View ImageLayout Shortcut HelpNavigator DockStyle AnchorStyles \
  CheckState ToolStripItemDisplayStyle ToolStripItemImageScaling ToolStripItemAlignment ToolStripGripStyle \
  ToolStripDropDownDirection ToolStripDropDownCloseReason ToolStripStatusLabelBorderSides Border3DStyle \
  TextImageRelation ToolStripItemOverflow ToolStripLayoutStyle ToolStripRenderMode MergeAction \
  AutoScaleMode FormBorderStyle ComboBoxStyle FormStartPosition SizeGripStyle \
  DragDropEffects FlatStyle FlowDirection BorderStyle ScrollBars RightToLeft \
  TabControlAction Appearance ToolStripItemDesignerAvailability LeftRightAlignment ToolTipIcon TabAlignment TabAppearance TabSizeMode \
  ControlStyles PictureBoxSizeMode ColumnHeaderStyle ButtonBorderStyle AutoSizeMode RichTextBoxScrollBars CharacterCasing \
  DataGridViewCellBorderStyle DataGridViewClipboardCopyMode DataGridViewHeaderBorderStyle DataGridViewColumnHeadersHeightSizeMode \
  DataGridViewContentAlignment DataGridViewTriState DataGridViewEditMode DataGridViewSelectionMode DataGridViewComboBoxDisplayStyle \
  DataGridViewColumnSortMode DataGridViewAutoSizeColumnsMode DataGridViewAutoSizeRowsMode DataGridViewAutoSizeColumnMode SortOrder \
  ScrollEventType ScrollOrientation DataGridViewDataErrorContexts DataGridViewRowHeadersWidthSizeMode DataGridViewImageCellLayout \
  DataGridViewElementStates DataGridViewHitTestType DataGridViewPaintParts \
  DrawMode AutoCompleteMode AutoCompleteSource ImeMode DrawItemState \
  TreeViewAction TreeViewDrawMode TreeViewHitTestLocations TreeNodeStates ColorDepth \
  LinkBehavior FixedPanel Orientation TickStyle \
  > $C/WinForms/Enums.g.cs.new && mv $C/WinForms/Enums.g.cs.new $C/WinForms/Enums.g.cs
dotnet run --project tools/compatgen -- $REF/System.Drawing.dll \
  RotateFlipType GraphicsUnit FontStyle PixelFormat ImageLockMode SmoothingMode InterpolationMode \
  CompositingQuality CompositingMode PixelOffsetMode LinearGradientMode TextRenderingHint WrapMode FillMode \
  StringAlignment StringFormatFlags StringTrimming ContentAlignment MatrixOrder ColorMatrixFlag ColorAdjustType \
  > $C/Drawing/Enums.g.cs.new && mv $C/Drawing/Enums.g.cs.new $C/Drawing/Enums.g.cs
