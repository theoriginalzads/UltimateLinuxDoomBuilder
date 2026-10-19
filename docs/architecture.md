# ULDB architecture

How the fork is put together. The aim throughout is that upstream UDB source files compile **unchanged** wherever possible, so merging new UDB releases stays cheap.

## Projects (`Builder.Linux.sln`)

| Project | Assembly | What it is |
|---|---|---|
| `Source/ULDB/Compat/ULDB.Compat.csproj` | `ULDB.Compat` | Compatibility layer: the parts of `System.Drawing` and `System.Windows.Forms` that UDB uses, reimplemented for .NET 10 on Linux |
| `Source/ULDB/Builder/ULDB.Builder.csproj` | `Builder` | Upstream's `Source/Core/**/*.cs`, included by glob, plus ULDB's own code. Same assembly name as upstream's `Builder.exe`, so plugins reference it the same way |

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
- **Services** (`MessageBox`, `Cursor`, `Clipboard`, `Timer`, file/colour dialogs, `Application.Run`) all go through `WinFormsHost.Current` (`IWinFormsHost`). The Avalonia UI installs a real host at startup. Tests use the `HeadlessWinFormsHost`, which answers with defaults.
- **`Control`, `Form`, `ComboBox`, `Panel`…** are state-only stand-ins. They draw nothing. They exist because upstream core code refers to them.
- **`System.Media.SystemSounds`**: routed to a UI-installed player.

## UI placeholders (`Source/ULDB/Builder/UIStubs`)

Until each window is ported, `tools/uistubgen` generates a **placeholder** for every type in upstream's `Windows/` and `Controls/` (only files upstream's `BuilderMono.csproj` compiles).
- Each placeholder keeps the name, namespace and non-private member signatures. Bodies do nothing and return defaults.
- WinForms base classes map to the compat `Form`/`Control`.
- Members that can't compile yet (they use WinForms types ULDB doesn't provide) are pruned automatically by `tools/uistubgen/prune.py` and listed in `uistubs-prune.txt`.
- `UIStubFixes/` holds small hand-written partial additions, for members placeholders inherit from WinForms in upstream (e.g. `MainForm.Focus()`).

**When a type gets its real Avalonia port:**
1. Add its full name to `uistubs-ported.txt`.
2. Rerun `tools/uistubgen/prune.py`. The placeholder disappears and the port takes its place.

**To regenerate everything** (after an upstream merge, for example):

```sh
python3 tools/uistubgen/prune.py
```

It regenerates the placeholders, prunes what doesn't compile, and prints the errors left in upstream files.

## Edits to upstream files

Kept to a minimum, always wrapped in `#if ULDB … #else <original> #endif` so the original code stays visible and merges conflict as little as possible. Each one is listed in [`upstream-edits.md`](upstream-edits.md).

Upstream files use **CRLF line endings**. Edits must preserve them, or the whole file shows as changed.

## Platform helpers (`Source/ULDB/Builder/Platform`)

Linux replacements for Windows-only logic, called from the `#if ULDB` blocks: XDG paths, write-permission checks and so on.
