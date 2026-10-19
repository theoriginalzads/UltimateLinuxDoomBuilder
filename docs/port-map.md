# Port map: upstream WinForms → ULDB Avalonia

Each upstream window or control and its Avalonia port. Handler names are kept identical, so upstream logic changes can be copied across after a merge.

**Status:** ⬜ not started · 🟨 in progress · ✅ ported · ➖ out of scope

| Upstream file | ULDB file | Status | Notes |
|---|---|---|---|
| `Plugins/BuilderModes/Interface/MenusForm.cs` (+ `.Designer.cs`) | *(upstream files, compiled unchanged)* | ✅ | Never shown: it only holds BuilderModes' menu items and toolbar buttons, which the main window displays. Compiles against the compat ToolStrip model; `MenusForm.resx` (one tooltip) is embedded |
| `Controls/RenderTargetControl.cs` | `Source/ULDB/Builder/Shell/ViewportControl.cs` (+ placeholder) | 🟨 | The placeholder stays as upstream's display object (the renderer reads its `ClientSize`); `ViewportControl` shows it and keeps it sized |
