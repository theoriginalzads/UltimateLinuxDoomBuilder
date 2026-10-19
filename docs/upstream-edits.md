# Edits to upstream files

Every change ULDB makes inside an upstream UDB source file, for checking during merges. Each edit:
- is wrapped in `#if ULDB … #else <original> #endif`
- keeps the file's CRLF line endings

New ULDB code lives in its own files (`Source/ULDB/`, `tools/`, `docs/`) and isn't listed here.

| File | Where | Why |
|---|---|---|
| `Source/Core/General/General.cs` | `-PORTABLE` argument handling | Windows ACL permission check replaced with a direct write test (`ULDB.Platform.Paths.IsDirectoryWritable`) |
