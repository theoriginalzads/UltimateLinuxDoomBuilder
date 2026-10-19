# Edits to upstream files

Every change ULDB makes inside an upstream UDB source file, for checking during merges. Each edit:
- is wrapped in `#if ULDB … #else <original> #endif` (`#ifdef ULDB` in C++; also satisfies the native code's zlib licence requirement to mark altered sources)
- keeps the file's CRLF line endings

**`README.md`** is fully replaced with ULDB's own. Upstream's version is kept as `docs/upstream-README.md`. When merging an upstream release, keep ULDB's `README.md` and copy upstream's new README over `docs/upstream-README.md`.

New ULDB code lives in its own files (`Source/ULDB/`, `tools/`, `docs/`) and isn't listed here.

| File | Where | Why |
|---|---|---|
| `Source/Core/General/General.cs` | `-PORTABLE` argument handling | Windows ACL permission check replaced with a direct write test (`ULDB.Platform.Paths.IsDirectoryWritable`) |
| `Source/Core/General/General.cs` | `Main`: `settingspath` | Settings go to `$XDG_CONFIG_HOME/UltimateLinuxDoomBuilder`, not `~/.local/share/Doom Builder` (which upstream's Mono build uses) |
| `Source/Core/General/General.cs` | `OpenWebsite` | `Process.Start(url)` no longer opens URLs on .NET Core+; use `UseShellExecute = true` (xdg-open on Linux) |
| `Source/Native/OpenGL/OpenGLContext.cpp` | `IOpenGLContext::Create`, `GL_GetProcAddress` | Use the host's OpenGL context and loader when one is registered (`Source/ULDB/Native/HostedContext.cpp`). `ULDBHost.h` is force-included by the CMake build |
| `Source/Native/OpenGL/GLRenderDevice.cpp` | `StartRendering` | Bind the host's framebuffer instead of 0 |
| `Source/Native/OpenGL/GLTexture.cpp` | `GetFramebuffer` | Bind the host's framebuffer instead of 0 |
| `Source/Core/General/MapManager.cs` | `ExecuteExternalCommand` | User pre/post commands run as a `/bin/sh` script (argument as `$1`) instead of a `cmd.exe` batch file |

## Bug fixes

Genuine upstream bugs found while porting. They aren't Linux-specific, so they're fixed directly (no `#if ULDB`). Each is worth offering upstream.

| File | Fix | Symptom |
|---|---|---|
| `Source/Core/GZBuilder/Data/EngineInfo.cs` | `Dispose()`: `icon?.Dispose()` | The icon is created lazily. Disposing an `EngineInfo` whose icon was never requested threw `NullReferenceException`. Disposal runs from `ConfigurationInfo`'s finalizer, so this crashes the process (likely the "can't reproduce" crash reports referenced in `ConfigurationInfo.~ConfigurationInfo`) |
