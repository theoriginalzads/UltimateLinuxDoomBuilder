# ULDB architecture

How the fork is put together. The aim throughout is that upstream UDB source files compile **unchanged** wherever possible, so merging new UDB releases stays cheap.

## Projects (`Builder.Linux.sln`)

| Project | Assembly | What it is |
|---|---|---|
| `Source/ULDB/Compat/ULDB.Compat.csproj` | `ULDB.Compat` | Compatibility layer: the parts of `System.Drawing` and `System.Windows.Forms` that UDB uses, reimplemented for .NET 10 on Linux |
| `Source/ULDB/Builder/ULDB.Builder.csproj` | `Builder` | Upstream's `Source/Core/**/*.cs`, included by glob, plus ULDB's own code. Same assembly name as upstream's `Builder.exe`, so plugins reference it the same way. An executable: `ULDB.Shell.Program` sets up Avalonia, then runs upstream's `General.Main` |
| `Source/ULDB/Plugins/<Name>/ULDB.<Name>.csproj` | `<Name>` | An in-scope plugin: upstream's `Source/Plugins/<Name>`, by glob, minus its WinForms UI (placeholders instead). Copied into `Plugins/` next to `Builder`, where upstream's plugin manager loads it |

Upstream's `Builder.sln`, `BuilderMono.sln` and `*.csproj` files are left untouched.

## Why one `Builder` assembly (core and UI together)

Upstream's editor core refers to its windows directly, for example `new AngleForm().ShowDialog()` or `General.MainWindow.DisplayStatus(...)`. Plugins do the same (about 1,500 uses of `General.Interface`).

If ULDB's Avalonia windows live in the same assembly, **with the same names and namespaces** (`CodeImp.DoomBuilder.Windows.AngleForm`), every one of those references keeps working without editing upstream code. That's the "one-to-one port" rule in practice.

## The compatibility layer (`ULDB.Compat`)

On modern .NET, `System.Drawing.Common` is Windows-only and WinForms doesn't exist on Linux. Rather than edit the hundreds of upstream files that use them, ULDB.Compat provides the same types in the same namespaces.

**`System.Drawing`** (`Compat/Drawing`): implemented on SkiaSharp (the same version Avalonia uses).
- `Bitmap` stores unpremultiplied BGRA, which is GDI+'s `Format32bppArgb` memory layout. So `LockBits` returns the image memory directly, and UDB's texture code works unchanged.
- `Graphics` draws into a premultiplied copy and writes it back on `Dispose`/`Flush`.
- Also: `Font`, `FontFamily` (via fontconfig), `Pen`, `SolidBrush`, `LinearGradientBrush`, `GraphicsPath`, `StringFormat`, `SystemFonts`, `Icon`, `ImageFormat`.
- `Color`, `Point`, `Rectangle` and the like come from .NET's own cross-platform `System.Drawing.Primitives`.

**`System.Windows.Forms`** (`Compat/WinForms`): only what the editor core uses.
- **Enums** (`Keys`, `MouseButtons`, `DialogResult`…) are generated from the real WinForms reference assembly by `tools/compatgen`, so stored values (shortcut keys in settings files) stay compatible.
- **Services** (`MessageBox`, `Cursor`, `Clipboard`, `Timer`, file/colour dialogs, `Application.Run`) all go through `WinFormsHost.Current` (`IWinFormsHost`). The Avalonia UI installs a real host at startup. Tests use the `HeadlessWinFormsHost`, which answers with defaults; its timers never tick, like WinForms timers with no message loop. `Control.InvokeRequired` is "not on the UI thread", so upstream's `RunOnUIThread` marshals background work (image loading) correctly.
- **UI hooks** the Avalonia UI installs as static delegates: `Cursor.PositionProvider/PositionSetter/ClipSetter/VisibilitySetter`, `Control.ModifierKeysProvider`, `Screen.BoundsProvider`, `Graphics.ScreenCapture`, `Form.CloseHandler`, `ToolStripDropDown.ShowHandler`.
- **`Control`, `Form`, `ComboBox`, `Panel`…** are state-only stand-ins. They draw nothing. They exist because upstream core code refers to them.
- **Menus and toolbars** (`ToolStrip`, `MenuStrip`, `ToolStripMenuItem`, `ToolStripButton`, …, in `ToolStrip.cs`) are a state-only model too. Upstream and plugins build their menus with them (plugins through `General.Interface.AddButton`/`AddMenu`); the Avalonia main window renders them and follows their `Changed` events (a ULDB addition). With this model, a designer file that only builds menu items (BuilderModes' `MenusForm`) compiles unchanged.
- **`System.Media.SystemSounds`**: routed to a UI-installed player.

## UI placeholders (`Source/ULDB/Builder/UIStubs`)

Until each window is ported, `tools/uistubgen` generates a **placeholder** for every type in upstream's `Windows/` and `Controls/` (only files upstream's `BuilderMono.csproj` compiles).
- Each placeholder keeps the name, namespace and non-private member signatures. Bodies do nothing and return defaults.
- WinForms base classes map to the compat `Form`/`Control`.
- Members that can't compile yet (they use WinForms types ULDB doesn't provide) are pruned automatically by `tools/uistubgen/prune.py` and listed in `uistubs-prune.txt`.
- `UIStubFixes/` holds small hand-written partial additions:
  - members placeholders inherit from WinForms in upstream (e.g. `MainForm.Focus()`)
  - members that must do real work even before the port. These are listed in `uistubs-manual.txt` so the generator skips them. For example, `MainForm.RunOnUIThread` must run its action, because image loading publishes results through it.

## Rendering (`Source/ULDB/Native`, `Source/ULDB/Builder/Rendering`)

Upstream's BuilderNative creates its own GLX/WGL context on a native window. ULDB adds a **hosted context**: the host registers OpenGL callbacks and a target framebuffer with `ULDB.Rendering.HostedGL`, and BuilderNative renders with them.
- **The Avalonia UI** (`Shell/ViewportContext.cs`, `Shell/ViewportControl.cs`): UDB issues GL calls at any time on the UI thread (texture uploads, redraws), not only inside Avalonia's render callback, so it gets its **own context, shared with Avalonia's** (`IOpenGlTextureSharingRenderInterfaceContextFeature.CreateSharedContext`). It renders into a framebuffer object (colour texture plus depth/stencil). `Present()` calls the host's present callback (`ULDB_SetHostPresent`), which `glFinish`es and asks the `ViewportControl` (an `OpenGlControlBase`) for a frame; the control blits the shared texture into Avalonia's framebuffer. Both run on the UI thread, so no other synchronisation is needed. The view renders in device pixels; upstream's `RenderTargetControl.ClientSize` is kept at that size.
- **Tests** use `OffscreenGL`: Mesa's surfaceless EGL platform, no display needed.

**Build:** `Source/ULDB/Native/CMakeLists.txt`, run automatically by `dotnet build`.
- CPU target: `-p:UldbArch=generic|x86-64-v3|x86-64-v4|znver4|native`.
- Upstream native files get three tiny `#ifdef ULDB` hooks. `ULDBHost.h` is force-included by CMake.

## Two ways to port a window

1. **Model port: upstream's files compile unchanged.** When a window's designer code only builds things compat models (menus, toolbars, labels, panels), upstream's `X.cs` and `X.Designer.cs` are compiled as they are. The WinForms objects become a state-only model, and a ULDB Avalonia view shows it and feeds input back (`Raise…` methods on compat controls, `Changed` events on ToolStrip items). Upstream logic keeps running exactly as written, so merges stay trivial. Used for `MainForm` (about 8,000 lines, zero edits) and BuilderModes' `MenusForm`.
2. **Avalonia port:** for dialogs whose layout is the point (edit forms, preferences), the designer layout is rebuilt as `.axaml`, keeping class and handler names.
   - The form class (`Windows/X.cs`) still derives from the compat `DelayedForm`/`Form`, so callers keep `ShowDialog(owner)` / `DialogResult`. It owns an `XView : Window` (`Windows/XView.axaml`, fields from `x:Name`). `InitializeComponent()` takes the controls from the view and wires upstream's handler names.
   - The form overrides `CreateView()` to return the view. Compat `Form.Show`/`ShowDialog` hand it to `WinFormsHost.ShowForm`. `AvaloniaHost` then shows the window (modal over its owner), applies `DelayedForm`'s saved geometry, and routes closing through `Form.RequestClose`. Setting `DialogResult` on a modal form closes it, as in WinForms.
   - User controls (such as `ResourceListEditor`) are plain Avalonia `UserControl`s with upstream's public API.
3. **Model port + `.axaml` view:** for controls whose logic manipulates WinForms labels directly (the info panels), upstream's `.cs` and `.Designer.cs` compile unchanged and fill compat labels; a ULDB `.axaml` view with the new layout names its controls like upstream's fields, and `Shell/ModelBinder.BindByName` copies text, visibility, enabled state, colours, images and list items across whenever a watched model control changes (or upstream calls `Show()`/`Refresh()`, which raise compat's `Refreshed`). Upstream logic stays untouched; the layout is free.
   - **Inputs are two-way** (`Shell/ModelBinder.Inputs.cs`): Avalonia TextBox/CheckBox/ComboBox/NumericUpDown/Button/TabControl named like an upstream field drive that model control (setting Text, CheckState, SelectedIndex, raising Click), and keys, typed characters (upstream's KeyPress filters), focus and validation are routed to it. A `ContentControl` named like an upstream user control shows that control's registered view (`Shell/ControlViews`).
   - **Model-ported forms** get their window from `Shell/FormViews` (installed as compat's `Form.ViewFactory`); `AvaloniaHost.ShowForm` shows it like any ported form.

Designer code that loads values through `ComponentResourceManager` (`resources.GetObject("$this.Icon")`) gets a ULDB `.resx` from `tools/resxgen/formresx.py` (in `FormResources/`). It keeps only the entries the designer reads, typed as compat's `Bitmap`/`Icon`; the project compiles it as preserialized resources, and `System.Resources.Extensions` creates the objects from a stream at runtime.

**When a type gets its real Avalonia port:**
1. Add its full name to `uistubs-ported.txt`.
2. Rerun `tools/uistubgen/prune.py`. The placeholder disappears and the port takes its place.

**To regenerate everything** (after an upstream merge, for example):

```sh
python3 tools/uistubgen/prune.py               # every target
python3 tools/uistubgen/prune.py BuilderModes  # one target (see TARGETS in the script)
```

Plugins work the same way: each has its own `UIStubs/`, `uistubs-*.txt` and `UIStubFixes/` under `Source/ULDB/Plugins/<Name>/`, and a `TARGETS` entry in `prune.py`. Its `Properties.Resources` comes from `tools/resxgen`, like the core's.

It regenerates the placeholders, prunes what doesn't compile, and prints the errors left in upstream files.

## Edits to upstream files

Kept to a minimum, always wrapped in `#if ULDB … #else <original> #endif` so the original code stays visible and merges conflict as little as possible. Each one is listed in [`upstream-edits.md`](upstream-edits.md).

Upstream files use **CRLF line endings**. Edits must preserve them, or the whole file shows as changed.

## Platform helpers (`Source/ULDB/Builder/Platform`)

Linux replacements for Windows-only logic, called from the `#if ULDB` blocks: XDG paths, write-permission checks and so on.

## Script compilers and node builder (`tools/compilers`)

ACC 1.60, ZDBSP 1.19, ZT-BCC 0.10.0-alpha.8 and BCC (master) are **git submodules** in `tools/compilers/src`, pinned to those versions. `tools/compilers/CMakeLists.txt` builds them out of tree, so the submodules stay clean. `dotnet build` runs it and copies the binaries into `Compilers/…` in the output, where upstream's game configurations expect them. These replace upstream's prebuilt Linux ACC and ZDBSP binaries; ZenNode and ELFBSP are still the prebuilt ones (low priority, since they're vanilla node builders).

BCS (ZT-BCC and BCC) support was Windows-only upstream. ULDB adds Linux compiler configs and fixes found while testing; see `upstream-edits.md`.

`OpenSaveMapTests` checks the whole chain through upstream's own `MapManager.InitializeOpenMap` and `SaveMap`: it opens a UZDoom map with Freedoom, saves it, and verifies the output has `ZNODES` from ZDBSP and `BEHAVIOR` from each compiler.
