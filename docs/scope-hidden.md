# Hidden in ULDB's UI (GZDoom/UZDoom-only scope)

ULDB targets GZDoom and UZDoom only. Everything below still exists in the code and config files (so upstream merges stay clean) but is hidden in the UI by the scope filter (`Source/Platform/ScopeFilter.cs`, added in Phase 3).

Undo any item by removing it from the filter or allowlist.

## Game configurations

**Shown:** `UZDoom_*` and `GZDoom_*` in all map formats (UDMF, Doom and Hexen).
**Hidden:** everything else, including any new configs that arrive in upstream merges.

| Hidden | Reason |
|---|---|
| *(Filled in when the filter lands in Phase 3.)* | |

## Other UI elements

| Where | What | Reason |
|---|---|---|
| *(Filled in during Phase 3 and the Phase 5 audit pass.)* | | |
