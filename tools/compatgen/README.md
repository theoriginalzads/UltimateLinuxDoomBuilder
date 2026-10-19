# compatgen

Extracts public enums, with their exact values, from .NET Framework reference
assemblies. ULDB uses it to generate the enum part of its WinForms and
System.Drawing compatibility layer (`Source/ULDB/Compat`). Upstream code can
then compile unchanged, and stored values (for example keyboard shortcuts in
settings files) stay compatible with upstream UDB.

```sh
REF=/usr/lib/mono/4.7.2-api   # from the mono package
C=Source/ULDB/Compat
dotnet run --project tools/compatgen -- $REF/System.Windows.Forms.dll \
  Keys MouseButtons DialogResult MessageBoxButtons MessageBoxIcon MessageBoxDefaultButton \
  MessageBoxOptions FormWindowState View ImageLayout Shortcut HelpNavigator DockStyle AnchorStyles \
  CheckState ToolStripItemDisplayStyle ToolStripItemImageScaling ToolStripItemAlignment ToolStripGripStyle \
  ToolStripDropDownDirection ToolStripDropDownCloseReason ToolStripStatusLabelBorderSides Border3DStyle \
  TextImageRelation ToolStripItemOverflow ToolStripLayoutStyle ToolStripRenderMode MergeAction \
  AutoScaleMode FormBorderStyle ComboBoxStyle FormStartPosition SizeGripStyle \
  DragDropEffects FlatStyle FlowDirection BorderStyle ScrollBars RightToLeft \
  > $C/WinForms/Enums.g.cs
dotnet run --project tools/compatgen -- $REF/System.Drawing.dll \
  RotateFlipType GraphicsUnit FontStyle PixelFormat ImageLockMode SmoothingMode InterpolationMode \
  CompositingQuality CompositingMode PixelOffsetMode LinearGradientMode TextRenderingHint WrapMode FillMode \
  StringAlignment StringFormatFlags StringTrimming ContentAlignment MatrixOrder ColorMatrixFlag ColorAdjustType \
  > $C/Drawing/Enums.g.cs
```
