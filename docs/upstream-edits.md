# Edits to upstream files

Every change ULDB makes inside an upstream UDB source file, for checking during merges. Each edit:
- is wrapped in `#if ULDB … #else <original> #endif` (`#ifdef ULDB` in C++; also satisfies the native code's zlib licence requirement to mark altered sources)
- keeps the file's CRLF line endings

**`README.md`** is fully replaced with ULDB's own. Upstream's version is kept as `docs/upstream-README.md`. When merging an upstream release, keep ULDB's `README.md` and copy upstream's new README over `docs/upstream-README.md`.

**ULDB-owned replacements for upstream asset files.** These are copied to the build instead of upstream's version, so re-check them against upstream's file after a merge:

| ULDB file | Replaces | Change |
|---|---|---|
| `Source/ULDB/Assets/Scripting/BCS.cfg` | `Assets/Windows/Scripting/BCS.cfg` | Adds `-x bcs` to BCC's parameters. UDB passes scripts as `.tmp` files, and BCC picks the language from the extension. Probably broken upstream too |
| `Source/ULDB/Assets/Compilers/ZT-BCC/zt-bcc.cfg` | `Assets/Windows/Compilers/ZT-BCC/zt-bcc.cfg` | Linux program name. Lists `zcommon.h.bcs` (the header ZT-BCC's docs say to include) so UDB's include pre-check accepts it |
| `Source/ULDB/Assets/Compilers/BCC/bcc.cfg` | `Assets/Windows/Compilers/BCC/bcc.cfg` | Linux program name. BCC ships with its **own** `lib/`, because upstream's BCC `std.acs`/`zcommon.bcs` use a syntax no BCC release accepts |

New ULDB code lives in its own files (`Source/ULDB/`, `tools/`, `docs/`) and isn't listed here.

| File | Where | Why |
|---|---|---|
| `Source/Core/General/General.cs` | `-PORTABLE` argument handling | Windows ACL permission check replaced with a direct write test (`ULDB.Platform.Paths.IsDirectoryWritable`) |
| `Source/Core/General/General.cs` | `Main`: `settingspath` | Settings go to `$XDG_CONFIG_HOME/UltimateLinuxDoomBuilder`, not `~/.local/share/Doom Builder` (which upstream's Mono build uses) |
| `Source/Core/General/General.cs` | `OpenWebsite` | `Process.Start(url)` no longer opens URLs on .NET Core+; use `UseShellExecute = true` (xdg-open on Linux) |
| `Source/Native/OpenGL/OpenGLContext.cpp` | `IOpenGLContext::Create`, `GL_GetProcAddress` | Use the host's OpenGL context and loader when one is registered (`Source/ULDB/Native/HostedContext.cpp`). `ULDBHost.h` is force-included by the CMake build |
| `Source/Native/OpenGL/GLRenderDevice.cpp` | `StartRendering` | Bind the host's framebuffer instead of 0 |
| `Source/Native/OpenGL/GLTexture.cpp` | `GetFramebuffer` | Bind the host's framebuffer instead of 0 |
| `Source/Core/Compilers/AccCompiler.cs` | `Run` | Process timing via `ULDB.Platform.Processes`: `Process.StartTime` throws on Linux once the process has exited, which broke every ACS/BCS compile |
| `Source/Core/Compilers/NodesCompiler.cs` | `Run` | Same timing fix (broke every node build) |
| `Source/Core/General/Launcher.cs` | Test map start and `TestingFinished` | Same timing fix (would have thrown when the test engine exits) |
| `Source/Core/General/MapManager.cs` | `ExecuteExternalCommand` | User pre/post commands run as a `/bin/sh` script (argument as `$1`) instead of a `cmd.exe` batch file |
| `Source/Core/Windows/ConfigForm.cs` | configuration list, node builder lists, applying changes (`#if ULDB`) | Only GZDoom/UZDoom configurations and ZDBSP node builders are offered (`ScopeFilter`); changes are applied by file name because hidden configurations aren't in the list |
| `Source/Core/Controls/Scripting/ScriptEditorPanel.cs` | Save/Save All enabling and actions, Ctrl+S (`#if ULDB`) | Save and Save All also work on the map's script lumps (upstream only saves them implicitly, so the buttons stayed disabled) |
| `Source/Core/Windows/MainForm.cs` | shortcut reference file and config folder (`#if ULDB`) | `Process.Start(path)` on a file or folder fails on .NET; use `ULDB.Platform.Desktop.Open` (xdg-open) |
| `Source/Core/General/General.cs` | `OpenMapFileWithOptions` (`#if ULDB`) | `ULDB.Platform.Busy.Begin/End` show a "Opening map..." overlay (Avalonia doesn't paint while the UI thread is blocked) |

## Bug fixes

Genuine upstream bugs found while porting. They aren't Linux-specific, so they're fixed directly (no `#if ULDB`). Each is worth offering upstream.

| File | Fix | Symptom |
|---|---|---|
| `Source/Core/GZBuilder/Data/EngineInfo.cs` | `Dispose()`: `icon?.Dispose()` | The icon is created lazily. Disposing an `EngineInfo` whose icon was never requested threw `NullReferenceException`. Disposal runs from `ConfigurationInfo`'s finalizer, so this crashes the process (likely the "can't reproduce" crash reports referenced in `ConfigurationInfo.~ConfigurationInfo`) |
| `Source/Core/Windows/MainForm.cs` | window title: no "(64-bit)"/"(32-bit)" (`#if ULDB`) | User request: there's one build, so it isn't needed |
| `Source/Core/Windows/MainForm.cs` | `itemhelpissues_Click` opens ULDB's repository (`Branding.REPOSITORY_URL`) (`#if ULDB`) | The item pointed at upstream's bug tracker; ULDB problems go to the fork |
| `Source/Core/Windows/RunExternalCommandForm.cs` | `KillProcessAndChildren` uses `Process.Kill(true)`; no `System.Management` (`#if ULDB`) | WMI is Windows-only |
| `Source/Core/General/General.cs` | at startup the previous log is copied to `UDBuilder.previous.log` before it's deleted (`#if ULDB`) | A crash is usually reported after a restart, which deleted the log of the crashed session |
| `Source/Core/Windows/PreferencesForm.cs` | script editor colour presets: extra names added to the combo, a `default:` case in `scriptcolorpresets_SelectedIndexChanged` applies them (`#if ULDB`, `ULDB.Scripting.ScriptColorPresets`) | Issue #20: common editor palettes (Monokai, Dracula, One Dark, Nord, Gruvbox, Solarized, GitHub Light) as choices |

## Native stand-ins (not edits)

- `Source/ULDB/Native/User32Stubs.c` → `libuser32.so`: no-op `SendMessage`, `PostMessage`, `LockWindowUpdate`, `GetWindowLong`, `SetWindowLong`, `MessageBeep` for upstream's direct `user32.dll` P/Invokes (e.g. `ArgumentsControl.BeginUpdate`), which Mono used to emulate. Upstream files are unchanged.
