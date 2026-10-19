# Port map: upstream WinForms → ULDB Avalonia

Each upstream window or control and its Avalonia port. Handler names are kept identical, so upstream logic changes can be copied across after a merge.

**Status:** ⬜ not started · 🟨 in progress · ✅ ported · ➖ out of scope

| Upstream file | ULDB file | Status | Notes |
|---|---|---|---|
| `Windows/MainForm.cs` (+ `.Designer.cs`) | *(upstream files, compiled unchanged)* + `Shell/MainWindow.cs`, `Shell/ToolStripViews.cs` | 🟨 | Model port: upstream logic runs as-is on compat models; resources via `FormResources/MainForm.resx`. The view shows menus, toolbars, modes toolbar, status bar and the map view, and routes input. Info panels and dockers are still placeholders |
| `Controls/ToolStripActionButton.cs`, `ToolStripNumericUpDown.cs`, `ToolStripCheckBox.cs`, `PlaceholderToolStripTextBox.cs`, `RenderTargetControl.cs` | *(upstream files, compiled unchanged)* | ✅ | Model ports, rendered by `ToolStripViews` / `MainWindow` |
| `Windows/StatusInfo.cs`, `MenuSection.cs`, `ToolbarSection.cs`, `MessageBeepType.cs`, `FindReplaceOptions.cs`, `PluginToolbarButton.cs`, `IMainForm.cs`, `Controls/Docker.cs`, `ControlExtensionMethods.cs` | *(upstream files, compiled unchanged)* | ✅ | Not UI: data types and interfaces |
| `Plugins/BuilderModes/Interface/MenusForm.cs` (+ `.Designer.cs`) | *(upstream files, compiled unchanged)* | ✅ | Never shown: it only holds BuilderModes' menu items and toolbar buttons, which the main window displays. Compiles against the compat ToolStrip model; `MenusForm.resx` (one tooltip) is embedded |
| `Controls/RenderTargetControl.cs` | `Source/ULDB/Builder/Shell/ViewportControl.cs` (+ placeholder) | 🟨 | The placeholder stays as upstream's display object (the renderer reads its `ClientSize`); `ViewportControl` shows it and keeps it sized |
