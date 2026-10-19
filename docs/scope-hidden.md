# Hidden in ULDB's UI (GZDoom/UZDoom-only scope)

ULDB targets GZDoom and UZDoom only. Everything below still exists in the code and config files (so upstream merges stay clean) but is hidden in the UI by the scope filter (`Source/ULDB/Builder/Scope/ScopeFilter.cs`).

Undo any item by changing the rules in `ScopeFilter` (and its tests).

## Game configurations

**Shown (14):** `UZDoom_*` (Doom, Heretic, Hexen; UDMF) and `GZDoom_*` (Doom, Heretic, Hexen, Strife; UDMF, Doom and Hexen formats).
**Default for new maps:** `UZDoom_DoomUDMF.cfg`.
**Hidden:** everything else (37 configs), including any new configs that arrive in upstream merges. Rule: `ScopeFilter.IsConfigurationVisible`, tested in `ScopeFilterTests`.

A map whose saved settings name a hidden config opens with the closest visible one: same game and map format, UZDoom where it has the config, otherwise GZDoom (`ScopeFilter.SuggestConfiguration`).

| Engine | Hidden configurations | Reason |
|---|---|---|
| Boom | `Boom_Doom2Doom.cfg`, `Boom_DoomDoom.cfg` | Boom family |
| DSDADoom | `DSDADoom_DoomUDMF.cfg` | Boom family |
| Doom | `Doom_Doom2Doom.cfg`, `Doom_DoomDoom.cfg` | Vanilla |
| EdgeC | `EdgeC_Doom2Doom.cfg`, `EdgeC_DoomUDMF.cfg` | Other port |
| Eternity | `Eternity_Doom2Doom.cfg`, `Eternity_DoomUDMF.cfg` | Other port |
| Helion | `Helion_DoomUDMF.cfg` | Other port |
| Heretic | `Heretic_HereticDoom.cfg` | Vanilla |
| Hexen | `Hexen_HexenHexen.cfg` | Vanilla |
| MBF21 | `MBF21_Doom2Doom.cfg` | Boom family |
| Strife | `Strife_StrifeDoom.cfg` | Vanilla |
| ZDaemon | `ZDaemon_DoomHexen.cfg` | Other port |
| ZDoom | `ZDoom_DoomDoom.cfg`, `ZDoom_DoomHexen.cfg`, `ZDoom_DoomUDMF.cfg`, `ZDoom_HereticDoom.cfg`, `ZDoom_HereticHexen.cfg`, `ZDoom_HereticUDMF.cfg`, `ZDoom_HexenHexen.cfg`, `ZDoom_HexenUDMF.cfg`, `ZDoom_StrifeDoom.cfg`, `ZDoom_StrifeHexen.cfg`, `ZDoom_StrifeUDMF.cfg` | Other port (ZDoom-family; GZDoom/UZDoom configs cover these maps) |
| Zandronum | `Zandronum_DoomDoom.cfg`, `Zandronum_DoomHexen.cfg`, `Zandronum_DoomUDMF.cfg`, `Zandronum_HereticDoom.cfg`, `Zandronum_HereticHexen.cfg`, `Zandronum_HereticUDMF.cfg`, `Zandronum_HexenHexen.cfg`, `Zandronum_HexenUDMF.cfg`, `Zandronum_StrifeDoom.cfg`, `Zandronum_StrifeHexen.cfg`, `Zandronum_StrifeUDMF.cfg` | Other port |

## Node builders

**Shown:** ZDBSP (`zdbsp_*`) and "none". **Hidden:** `elfbsp_fast`, `elfbsp_fast_hexen`, `elfbsp_normal`, `elfbsp_normal_hexen`, `zennode_fast`, `zennode_normal`. ZenNode and ELFBSP are vanilla/Boom-oriented node builders.

## Script compilers

**Shown:** `zdoom_acs.cfg` (ACC), `zt_bcs.cfg` (ZT-BCC), `bcs.cfg` (BCC). **Hidden:** `hexen_acs.cfg`, `zandronum_acs.cfg`, `zdaemon_acs.cfg` (other ports' ACS variants).

## Other UI elements

| Where | What | Reason |
|---|---|---|
| *(Filled in as UI lands in Phase 3, and in the Phase 5 audit pass.)* | | |
