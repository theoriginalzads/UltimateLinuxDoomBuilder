# Edits to upstream files

Every change ULDB makes inside an upstream UDB source file, for checking during merges. Each edit:
- is wrapped in `#if ULDB … #else <original> #endif`
- keeps the file's CRLF line endings

New ULDB code lives in its own files (`Source/ULDB/`, `tools/`, `docs/`) and isn't listed here.

| File | Where | Why |
|---|---|---|
| `Source/Core/General/General.cs` | `-PORTABLE` argument handling | Windows ACL permission check replaced with a direct write test (`ULDB.Platform.Paths.IsDirectoryWritable`) |

## Bug fixes

Genuine upstream bugs found while porting. They aren't Linux-specific, so they're fixed directly (no `#if ULDB`). Each is worth offering upstream.

| File | Fix | Symptom |
|---|---|---|
| `Source/Core/GZBuilder/Data/EngineInfo.cs` | `Dispose()`: `icon?.Dispose()` | The icon is created lazily. Disposing an `EngineInfo` whose icon was never requested threw `NullReferenceException`. Disposal runs from `ConfigurationInfo`'s finalizer, so this crashes the process (likely the "can't reproduce" crash reports referenced in `ConfigurationInfo.~ConfigurationInfo`) |
