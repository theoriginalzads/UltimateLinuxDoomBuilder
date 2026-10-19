# Ultimate Linux Doom Builder — Native Linux Port of UDB (Avalonia)

**Project name:** *Ultimate Linux Doom Builder* (ULDB). It's a fork of Ultimate Doom Builder, deliberately named differently so nobody confuses it with the official UDB.

**Goal:** a native Linux fork of [UltimateDoomBuilder](https://github.com/UltimateDoomBuilder/UltimateDoomBuilder) on modern .NET with an Avalonia UI. It should follow KDE Plasma's dark/light mode, accent colour and scaling (and work on GNOME and others), with no Mono or WinForms. It also needs to stay easy to merge upstream changes into.

**Primary target:** CachyOS, KDE Plasma, x86-64 (Ryzen 9 9950X / Zen 4).
**Not in scope (yet):** macOS. Keep code platform-neutral so it stays possible.

---

## 0. Context for a fresh Claude session

Read this first if you're picking up the work without the original conversation.

- **Today's Linux build** uses Mono + Mono's WinForms (`Makefile`, `BuilderMono.sln`, `builder.sh`, Flatpak in `flatpak/`). It is not Wine. Mono's WinForms is the cause of the poor performance and lack of desktop integration.
- **Codebase size (Oct 2026):** about 1,050 C# files and 318k lines, including 165 `*.Designer.cs` files (51k lines). About 158 Form/UserControl classes. 481 files use `System.Windows.Forms` (229 of them in plugins). 18 plugins, about 125k lines.
- **Renderer:** `Source/Native` is about 17.6k lines of C++/OpenGL 3.2. It's called from C# through about 63 `DllImport("BuilderNative")` calls. It already has a `UDB_LINUX` X11 path for creating the OpenGL context.
- **Points where the UI connects to the rest of the code:** `IMainForm` (`Source/Core/Windows/IMainForm.cs`) and `General.Interface` (about 1,500 uses).
- **Windows-only bits to replace:**
  - `user32`, `uxtheme` and `rstrtmgr` P/Invokes
  - `Microsoft.Win32` registry use (`General.cs`, `PreferencesForm.cs`, …)
  - `cmd.exe` in `MapManager.ExecuteExternalCommand`
  - `explorer.exe` in `ExceptionDialog`
  - `Updater.exe` / `UpdateChecker`
  - The `WINEPREFIX` path hacks in `General.cs`
  - ScintillaNET and TabControlEX
- **Decisions already made:**
  - **Windowing: Avalonia's X11 backend**, running natively on X11 and through XWayland on Wayland sessions. One build for everyone. No native Wayland backend for now.
  - **Import of existing UDB settings** (Mono, Flatpak, Wine, Windows) is supported. See Phase 7.
  - **ACS and BCS** (ZT-BCC and BCC) are both supported.
  - **The fork is public and clearly labelled as AI-coded.** See section 6.
  - **The project is called "Ultimate Linux Doom Builder" (ULDB)** in all UI and packaging. The About box says it's for GZDoom/UZDoom and credits upstream UDB. Internal code names stay unchanged. See the branding section in section 3.
  - Use Avalonia, not Qt, because merging upstream is easier.
  - **Mapping target is GZDoom and UZDoom only** (UDMF, ZDoom-style ACS). UDB already ships `UZDoom_*UDMF.cfg` game configurations. Testing focuses on these. Other game configs stay in the tree but aren't a priority.
  - **Plugins in scope:** BuilderModes, ColorPicker, TagExplorer, CommentsPanel, 3DFloorMode, StairSectorBuilder, BuilderEffects, TagRange, SoundPropagationMode.
  - **Plugins out of scope** (left in the tree, not built): UDBScript, VisplaneExplorer, AutomapMode, NodesViewer, BlockmapExplorer, RejectExplorer, USDF, WadAuthorMode, ImageDrawingExample.
  - **Upstream sync happens only on major UDB releases**, not continuously.
  - **This is a GZDoom/UZDoom map editor, not a vanilla, Boom or other-port editor.** Every option in the UI for creating or targeting other ports is hidden. This is UI only: upstream code and config files stay in the tree to keep merges easy. See section 3.
  - **UI style:** familiar to existing UDB users, but modern and responsive. See section 4.
  - Port windows one-to-one: same class names and event handlers. Replace each `*.Designer.cs` with an `.axaml` file. Don't do an MVVM rewrite.
  - Linux first, x64 only.
  - Offer a Zen 4 build option for the native code, off by default.

---

## 1. How we stay mergeable with upstream

This shapes every phase, so it comes first.

- **Git layout:**
  - `origin` is the fork and `upstream` is the original repo.
  - `main` holds the port.
  - Merge upstream **only at major UDB releases** (merge the release tag, not `master`).
  - Rare merges are bigger merges, so the rules below matter even more. Each merge gets its own branch (`sync/udb-<version>`) and a checklist run through the round-trip tests and the UI-change script.
  - Record the upstream commit the fork is based on in `docs/progress.md`.
- **Don't move or rename upstream files** unless there's no other way. Leave WinForms files where they are and exclude them from the new project files with globs. Then upstream diffs keep landing on the same paths.
- **Add new code in new places:**
  - `Source/Avalonia/…` for UI
  - `Source/Platform/…` for OS abstractions
  - `Source/Native/CMakeLists.txt` for the native build
  - `tools/` for external tools
- **Keep window ports mirrored:**
  - `Windows/LinedefEditForm.cs` → `Source/Avalonia/Windows/LinedefEditForm.axaml(.cs)`.
  - Event handler names stay identical, so upstream logic changes can be copied across.
  - Keep a table (`docs/port-map.md`) mapping each original file to its port.
- **Prefer conditional compilation or partial classes** over editing shared files where possible (`#if UDB_AVALONIA` only when unavoidable).
- **Flag upstream UI changes after each merge:** a script lists upstream commits that touched `*.Designer.cs`, `Windows/` or `Controls/`, so they can be re-applied to the matching `.axaml`.

---

## 2. Phases

Each phase ends with a milestone you can test. Work happens on a `phase-N/...` branch and is merged into `main` once the milestone passes.

### Phase 0 — Fork, toolchain, baseline

1. Fork on GitHub and clone it on the CachyOS machine. Add the `upstream` remote.
2. Install the toolchain:
   - .NET SDK, **.NET 10 (LTS)**
   - `cmake`, `ninja`, `gcc`/`clang`
   - Mesa / GL headers
   - Optional: `mono` + `mono-msbuild`, to build the current version as a reference
3. Build and run the existing Mono version (`make && Build/builder`). Record reference screenshots and test maps for later comparison. All use the GZDoom/UZDoom UDMF configs:
   - a small UDMF map
   - a large UDMF map
   - a map with ACS scripts
   - a map using 3D floors and slopes
   - a map using a PK3 resource with GZDoom extras: MODELDEF, GLDEFS, dynamic lights, ZScript actors
   - Ideally, some of your own maps
4. Check what the current Avalonia release supports:
   - Confirm the X11 backend works cleanly on Plasma (XWayland), GNOME (XWayland) and a plain X11 session.
   - How do OpenGL controls work in that release?
   - Pin the version.
5. Add `CLAUDE.md` with build and run commands and a pointer to this plan.

**Milestone:** the reference build runs, the toolchain is installed, and the Avalonia version is chosen.

### Phase 1 — Move to .NET 10 and separate the editor logic from the UI ✅

**Done.** The approach changed from the original sketch, to keep upstream files untouched. Details are in [`architecture.md`](architecture.md).

| Originally planned | What was built instead, and why |
|---|---|
| A UI-free `UDB.Core` library, plus an `IUserInterface` service replacing `General.Interface` | One `Builder` assembly (upstream core and, later, the Avalonia UI), like upstream's `Builder.exe`. Ported windows keep their upstream names, so the core's direct references (`new AngleForm()`, `General.MainWindow.…`) and plugins work **without editing upstream code**. Until a window is ported, a generated **placeholder** stands in (`tools/uistubgen`) |
| Rewrite `System.Drawing` usage to SkiaSharp | A **compatibility layer** (`ULDB.Compat`) provides `System.Drawing` (on SkiaSharp) and the non-visual WinForms types, so upstream code compiles unchanged. WinForms services (message boxes, dialogs, clipboard, cursors, timers) route through `IWinFormsHost`, which the Avalonia UI implements |
| `Source/Platform` | `Source/ULDB/Builder/Platform`, called from small `#if ULDB` edits listed in [`upstream-edits.md`](upstream-edits.md) |

**What Phase 1 delivered:**
1. `Builder.Linux.sln` with `ULDB.Compat`, `ULDB.Builder` (assembly `Builder`) and `ULDB.Tests` (assembly `Builder.Tests`). Upstream `.sln` and `.csproj` files are untouched.
2. The upstream editor core compiles on .NET 10 for Linux. There are only four small `#if ULDB` edits, plus one upstream bug fix.
3. Upstream resources are embedded under the same names. `Properties.Resources` is regenerated so it loads the same images (`tools/resxgen`).
4. Platform:

   | Windows behaviour | Linux replacement |
   |---|---|
   | Settings folder | `$XDG_CONFIG_HOME/UltimateLinuxDoomBuilder` |
   | `-PORTABLE` ACL check | Direct write test |
   | `cmd.exe` pre/post commands | `/bin/sh` scripts |
   | Opening URLs | `xdg-open` |

   - Already compiled out by upstream's `NO_WIN32`/`NO_UPDATER`: `user32`/`rstrtmgr` calls, the updater.
   - Registry and `explorer.exe` uses are only in UI code (Preferences, exception dialog) and get handled when those windows are ported.
5. Tests (`dotnet test Builder.Linux.sln`, 101 passing):
   - upstream's own 70 tests
   - UZDoom/GZDoom game configurations load
   - UDMF map round trips are byte-stable and keep GZDoom data
   - the compat drawing layer behaves like GDI+
   - the platform helpers work

**Moved to Phase 2:** resource loading tests (WAD/PK3 textures, using Freedoom). Loading textures creates GPU textures, so these need the renderer's off-screen mode (Phase 2a).

### Phase 2 — Native renderer and external tools

**2a. BuilderNative**

1. Add `Source/Native/CMakeLists.txt` that builds `libBuilderNative.so`. Keep the old Makefile target working as well.
2. Remove the renderer's own window and context creation on Linux. It renders into the OpenGL context **Avalonia provides** (`OpenGlControlBase`), so the renderer itself has no windowing-system code. It works on X11 and XWayland now, and would work on native Wayland later without renderer changes.
3. Replace RawMouse (Windows raw input) with Avalonia pointer capture plus relative motion, for 3D mouselook.
   - `Actions/MouseInput.cs` (upstream) warps the cursor with `Cursor.Position`. The compat `Cursor.PositionSetter` hook is where the UI implements that.
4. **Off-screen context for tests:** a surfaceless or pbuffer EGL context (Mesa), so headless tests can create `RenderDevice` and exercise resource loading. Then add resource loading tests (Freedoom IWAD plus `tests/maps/pk3_resource.pk3`: textures, flats, sprites, GLDEFS, ZScript, MAPINFO).
5. Build options:
   - `-DUDB_ARCH=generic` (default, x86-64-v2)
   - `x86-64-v3`
   - `x86-64-v4`
   - `znver4`
   - `native`

**2b. Node builders and ACS/BCS compilers.** The repo currently ships prebuilt Linux binaries as opaque files in `Assets/Linux/Compilers`. Replace them with **source builds**: git submodules or pinned tarballs under `tools/`, built with CMake and installed into `Build/Compilers/...`.

Priority follows the GZDoom/UZDoom target:

| Tool | Used for | Windows today | Linux today | Priority | Plan |
|---|---|---|---|---|---|
| ACC (ZDoom) | ACS scripts for GZDoom/UZDoom | ✔ | prebuilt binary | **Essential** | Build from the ZDoom `acc` source. Keep the `.acs` include files as they are |
| ZDBSP | Nodes, including the UDMF/ZDoom node formats | ✔ | prebuilt binary | **Essential** | Build from source. GZDoom can build nodes itself, but UDB uses a node builder when saving and test-launching |
| ACC (Zandronum, ZDaemon, Hexen) | Other ports' ACS | ✔ | prebuilt binary | Cheap | Same source as above, only different include files, so it comes free |
| ZT-BCC and BCC | BCS scripts, an alternative to ACS that also targets GZDoom | ✔ | ✘ | **Essential** | Build both from source (they support Linux). Add Linux `.cfg` files, ship their libraries (`ZT-BCC/lib/*.bcs`, BCC's `zcommon.bcs`/`std.acs`), and move the BCS syntax definitions (`BCS.cfg`, `ZT_BCS.cfg`) out of `Assets/Windows/Scripting`, where only the Windows build gets them today. Test compiling, error reporting and highlighting |
| ZenNode, ELFBSP | Nodes (mainly vanilla/Boom) | ✔ | prebuilt binary | Low | Build from source if trivial, otherwise keep the existing binaries |
| glBSP, BSP (`bsp-w32`) | Nodes (vanilla/legacy) | ✔ | ✘ | Skip | Not needed for GZDoom/UZDoom |
| DeePBSP | Nodes (vanilla) | ✔ | ✘ | Drop | Source probably isn't public, and not needed for GZDoom/UZDoom |

Extra tasks:
- Check that the compiler interfaces (`Source/Core/Compilers`) handle Linux paths and case-sensitive filenames, for example `#include "ZCOMMON.ACS"` against `zcommon.acs`.
- Check that error output parsing still works with each tool's Linux build.
- These tools also get the `UDB_ARCH` option.

**Milestone:** a script compiles nodes for a UDMF test map with ZDBSP and compiles a ZDoom ACS test script and a BCS test script (both ZT-BCC and BCC), including error reporting. The native library builds through CMake. The VPO (visplane) code is excluded from the build because VisplaneExplorer is out of scope.

### Phase 3 — Avalonia app shell

1. Create the `UDB.Avalonia` app project, which implements `IUserInterface`. Use the Fluent theme following the system theme (it reads dark/light and accent colour through the freedesktop portal).
2. Main window:
   - **Menus and toolbars** are generated from UDB's existing action and menu definitions, including plugin-added buttons.
   - **Docking panels**, the side panels where plugins add tools.
   - **Status bar.**
   - **Bottom info panel:** the linedef, sector and thing info panels in `Controls/` are ported here.
3. **Viewport control:** an `OpenGlControlBase` that hosts BuilderNative, with resizing, redraw and vsync.
4. **Input:** keyboard and mouse go into UDB's action and shortcut system:
   - Map Avalonia key codes to UDB's key codes. This mapping matters because existing configs store WinForms key values.
   - Mouse wheel, focus handling, and capture for 3D mode.
5. Native file dialogs through the portal; message boxes.

**Milestone:** launch, open a map, see it in 2D and 3D, pan, zoom, switch modes, save.

### Phase 4 — Editing modes (BuilderModes and core editing)

1. Port the UI parts of the four everyday plugins:
   - **BuilderModes:** mode toolbars, option panels, docking windows
   - **ColorPicker:** light and fog colour dialogs
   - **TagExplorer:** side panel
   - **CommentsPanel:** UDMF comments
2. Check every core mode works:
   - vertices, linedefs, sectors and things
   - drawing, drag, paste, undo/redo
   - visual (3D) mode with texture alignment, including GZDoom rendering features: dynamic lights, models, fog, sector colours
3. Copy/paste between UDB instances, as on Windows (clipboard).

**Milestone:** you can build and edit a GZDoom/UZDoom UDMF map end to end in the port, including ACS and test-launching UZDoom.

### Phase 5 — Dialogs (one-to-one port)

For each window:
- Write an `.axaml` file matching the original layout.
- The code-behind keeps the original handler names and logic.
- Tick it off in `docs/port-map.md`.

**Shared controls come first**, because the dialogs depend on them (`Source/Core/Controls`, about 100 files):

| WinForms control | Avalonia replacement |
|---|---|
| Field and argument editors, angle and colour pickers | Port as Avalonia equivalents |
| Image and texture selectors | Port as Avalonia equivalents |
| Tag selectors | Port as Avalonia equivalents |
| Flags checkboxes | Port as Avalonia equivalents |
| Docker panels | Port as Avalonia equivalents |
| TabControlEX | Standard `TabControl` |
| ScintillaNET script editor | **AvaloniaEdit** with UDB's syntax definitions, autocomplete, snippets and error markers |

**Dialogs, in priority order:**
1. Linedef, sector, thing and vertex edit forms (classic and UDMF)
2. Texture and flat browser (needs fast, virtualised thumbnail loading)
3. Map options, open map options, change map
4. Preferences, game configurations, resource editing
5. Script editor window, errors window
6. Find/replace, tag explorer, test map, the remaining dialogs

**Milestone:** every core dialog is ported, and no WinForms is left in `UDB.Avalonia`.

### Phase 6 — Remaining plugins

Each plugin gets the same treatment: logic stays, UI files are ported. In order:

1. **3DFloorMode:** 3D floors, Slope mode, Draw Slopes mode. This is the most important one for GZDoom mapping.
2. **StairSectorBuilder**
3. **BuilderEffects:** jitter, random placement, terrain importer
4. **TagRange**
5. **SoundPropagationMode**

**Out of scope:** UDBScript, VisplaneExplorer, AutomapMode, NodesViewer, BlockmapExplorer, RejectExplorer, USDF, WadAuthorMode, ImageDrawingExample.
- They stay in the source tree (no deletions, so upstream merges stay clean) but are left out of `Builder.Linux.sln`.
- Any can be ported later with the same approach.

**Milestone:** all nine in-scope plugins work.

### Phase 7 — Desktop integration and packaging

1. **Paths:**
   - XDG folders for config, data and cache.
   - **Settings import** (useful for anyone switching over, not only you):
     - On first run, if no ULDB settings exist, look for existing UDB settings and offer to import them. It can also be run any time from *Preferences → Import UDB settings…*.
     - Sources to detect:
       - Mono build: `~/.local/share/Doom Builder/UDBuilder.cfg`
       - Official Flatpak: `~/.var/app/io.github.ultimatedoombuilder.ultimatedoombuilder/data/Doom Builder/`
       - Wine prefixes: `$WINEPREFIX` or `~/.wine/drive_c/users/*/AppData/Local/Doom Builder/`
       - A Windows install: the user picks the `Doom Builder` folder or `UDBuilder.cfg`, for example from a mounted Windows drive
       - Legacy `GZBuilder.cfg`
       - Exact paths are to be confirmed in Phase 1 against the code (`General.cs` settings path logic).
     - What's imported:
       - preferences
       - keyboard shortcuts (converted to Avalonia key codes)
       - game configuration settings: resources, test engines, node builder choices
       - recent files
       - script editor settings
       - per-map `.dbs` settings (these sit next to the maps and already work)
     - Path conversion:
       - Windows paths (`C:\...`) and Wine paths are converted to Linux paths where possible.
       - Paths that can't be resolved are listed in an import summary for the user to fix, never silently dropped.
       - Settings that point at hidden configs (vanilla, Boom, other ports) are imported but stay hidden.
     - The original files are only ever read, never changed.
     - Tests: import fixtures for each source, including Windows paths and old WinForms key codes.
   - Case-insensitive resource lookup where Doom data expects it.
2. **Dark mode:**
   - Switches live when the system changes.
   - The 2D and 3D viewport colours stay UDB's own; offer a "follow system" preset only if wanted.
3. **HiDPI:**
   - Test at 100%, 125%, 150% and 200%.
   - Test on a native X11 session (with `Xft.dpi` / KDE scaling) and under XWayland on Plasma and GNOME.
   - Document the Plasma Wayland setting: *Display → Legacy applications (X11) → Apply scaling themselves*. Without it the app is upscaled and looks blurry.
   - On GNOME Wayland with fractional scaling, document the expected behaviour (XWayland apps may be upscaled) and any workaround found in testing.
4. **Packaging:**
   - `.desktop` file, icons, AppStream metadata (reuse what's in `flatpak/`).
   - Updated Flatpak manifest (freedesktop runtime, .NET SDK extension).
   - An **AUR/CachyOS `PKGBUILD`**.
   - Optionally an AppImage.
5. **CI:** a GitHub Actions job for the Linux build and tests, plus Flatpak artifact builds.

**Milestone:** install from the package, launch from the KDE menu, everything works.

### Phase 8 — Zen 4 build option (personal builds)

- Applies to the native code only: BuilderNative and the tools from Phase 2b.
- Set it with `make UDB_ARCH=znver4` or a CMake preset.
- The C# code needs nothing: .NET already compiles for the CPU it runs on, including AVX-512.
- Measure before and after on a large map and in visplane explorer. Keep the option only if it measurably helps.

---

## 3. Scope: GZDoom/UZDoom only (hidden in the UI, code kept)

**Goal:** nobody should mistake this fork for a vanilla, Boom or PrBoom map builder.

**Rule:** hide, don't delete.
- Upstream code and `.cfg` files stay in place, so merges stay clean.
- The filtering lives in **one small, fork-owned place** (`Source/Platform/ScopeFilter.cs`, plus an allowlist file shipped in `Build/`). It's not scattered through upstream code.

**Game configurations.** UDB ships 51 game configs (`Assets/Common/Configurations`). Only these are offered:

| Shown | Hidden |
|---|---|
| `UZDoom_*UDMF.cfg` (Doom, Heretic, Hexen) | Vanilla: `Doom_*`, `Heretic_*`, `Hexen_*`, `Strife_*` |
| `GZDoom_*` in all formats: UDMF, plus Doom-format and Hexen-format for older GZDoom projects | Boom family: `Boom_*`, `MBF21_*`, `DSDADoom_*` |
| | Other ports: `ZDoom_*`, `Zandronum_*`, `ZDaemon_*`, `Eternity_*`, `EdgeC_*`, `Helion_*` |

**Everywhere this applies:**
1. **Game configuration lists:**
   - Preferences / Game Configurations
   - New Map
   - Open Map options
   - Map Options / change config
2. **New maps:** default to UZDoom: Doom 2 (UDMF).
3. **Opening a WAD made for a hidden config:**
   - Offer the closest GZDoom/UZDoom config. GZDoom plays vanilla and Boom maps, so the map still opens and edits.
   - Show a one-time notice that this editor targets GZDoom/UZDoom only.
   - Never fall back to a hidden config.
4. **Node builder dropdowns** (Game Configurations and test/save settings): show only the shipped GZDoom-relevant builders (ZDBSP, plus ZenNode/ELFBSP if built). Hide vanilla-only presets.
5. **Script compiler lists:** ZDoom ACC, ZT-BCC and BCC. Hide ACC entries for Hexen, ZDaemon and Zandronum.
6. **Test-map engine presets:** UZDoom and GZDoom only. Auto-detect `uzdoom` / `gzdoom` on `PATH` and the Flatpak IDs.
7. **Branding:** see the section below.
8. **Audit pass:** grep the UI for vanilla- or port-specific features and hide what only makes sense for hidden targets, for example:
   - Map Analysis checks that only matter for vanilla limits
   - Boom generalized-action editors where they can't apply
   - menu items for Doom/Hexen-format-only tools

   Record each hidden item in `docs/scope-hidden.md` so it can be undone if ever needed.

**Branding: "Ultimate Linux Doom Builder"**

The name is final for now. It's set in one place (a `Branding` constants class owned by the fork) so a rename later is a one-line change.

| Where | What it says |
|---|---|
| Window title | `Ultimate Linux Doom Builder – <map name>` |
| About box | **Ultimate Linux Doom Builder**, "A map editor for GZDoom and UZDoom". Version, plus the upstream UDB version it's based on |
| About box credits | "Based on Ultimate Doom Builder" with links, and the upstream credits (UDB, GZDoom Builder, Doom Builder 2 authors). UDB is GPL-3.0, so the licence and original copyright notices must stay. Required, not optional |
| Splash and first-run text, dialog captions, message boxes, error dialogs | "Ultimate Linux Doom Builder", or "ULDB" where space is short |
| Executable / launcher | `uldb` |
| App ID | `io.github.theoriginalzads.UltimateLinuxDoomBuilder` |
| `.desktop`, AppStream, icons | Own app ID and name, so it installs next to upstream UDB without clashing |
| Config, data, cache folders | `~/.config/UltimateLinuxDoomBuilder` etc. |
| X11 window class (`WM_CLASS`) | Matches the app ID, so KDE groups it correctly in the taskbar |
| Icon | A variant of the UDB icon, for example with a small Linux/penguin badge or a colour shift. Similar but distinguishable |

What **doesn't** change:
- Internal namespaces, assembly names and upstream source files. Renaming those would break every upstream merge.
- Branding is only applied in UI strings and packaging metadata.
- Upstream file paths stay as they are.
- Strings in upstream code that say "Ultimate Doom Builder" go through the `Branding` class rather than being edited in each file, where practical.

**Where it fits in the phases:**
- The filter and allowlist land in Phase 3, alongside the first config UI.
- The audit pass runs at the end of Phase 5.
- After every upstream merge, new configs are checked against the allowlist. Anything new is hidden by default.

## 4. UI/UX guidelines: familiar, but modern

**Goal:** an existing UDB user sits down and immediately knows where everything is. It just feels faster, cleaner and more polished.

**The split that keeps both goals:**
- **Logic** (code-behind and handlers) is ported one-to-one, so upstream fixes keep merging.
- **Layout** (`.axaml`) is rebuilt by hand anyway, so modernising it costs nothing extra in mergeability.

**Keep (familiarity):**
- Overall window structure:
  - menu bar
  - main and mode toolbars
  - 2D/3D viewport
  - bottom info panel
  - side docker panels
- Menu names, menu order and action names, so the wiki and tutorials still match.
- **Default keyboard shortcuts**, plus importing existing shortcut configs.
- The same dialogs, with the same tabs and grouping. Fields in the same order, so muscle memory works.
- Editing behaviour, mouse controls and mode logic exactly as upstream.

**Modernise:**
- **Responsiveness:**
  - Resource loading, texture and thumbnail decoding, node building and script compiling run in the background with progress.
  - The UI never freezes.
  - Dialogs open instantly; long lists are virtualised.
- **Visual design:**
  - Consistent spacing, alignment and typography.
  - System font and scaling.
  - Follows dark/light mode and accent colour.
  - Clear visual hierarchy instead of dense grey grids.
- **Icons:**
  - Upstream icons are small PNGs that look blurry when scaled up.
  - Phase 3 uses them as they are, scaled cleanly.
  - Replace them over time with scalable SVG equivalents in the same visual style, keeping recognisability.
- **Search and filter:**
  - Live filter boxes in the texture/flat browser, thing-type picker, action/effect browsers, preferences and the shortcut editor.
  - Type-to-search in long dropdowns.
- **Command palette (Ctrl+Shift+P):** search every action by name and see its shortcut. It's an addition only, so nothing is replaced.
- **Less modal friction:**
  - Inline validation (highlight the bad field, explain why) instead of error popups.
  - Toast notifications (UDB already has a `ToastManager`) for non-blocking messages.
  - Confirmations only for destructive actions.
- **Window behaviour:**
  - Dialogs can be resized.
  - Sizes, positions, splitters and docker layout are remembered.
  - Sensible minimum sizes.
- **Keyboard and accessibility:**
  - Full keyboard navigation and visible focus.
  - Logical tab order.
  - Tooltips on every icon-only button.
  - Text contrast that meets WCAG AA in both themes.
  - Avalonia accessibility labels on controls.
- **Desktop integration:**
  - Portal file dialogs.
  - Recent files in the KDE/GNOME recent-documents list.
  - Single-instance file opening.

**Guardrails:**
- No feature is removed and no menu item moves without a reason recorded in `docs/ux-changes.md`. That file doubles as a "what's different from upstream UDB" guide for users.
- When unsure, match upstream's behaviour and only modernise how it looks.
- Major layout departures (anything beyond spacing, theming and search additions) get a screenshot to you for sign-off before going further.

## 5. How we'll work (VS Code session on the CachyOS machine)

- **Claude does:**
  - all code changes
  - builds (`dotnet build`, CMake)
  - tests
  - running the app and taking screenshots for comparison
  - upstream merges
  - small, regular commits, with a short progress note per phase in `docs/progress.md`
- **You do:**
  - look at and approve each phase's milestone
  - say where it feels wrong (input, rendering, look)
  - answer the open questions below
- **Each session starts** by reading `CLAUDE.md`, this plan, `docs/progress.md` and `docs/port-map.md`. That way work can continue across sessions without the original chat.

## 6. Public release and AI disclosure

The fork will be **public on GitHub** so others can use it. It must be **clearly and honestly labelled as AI-coded**:

- **README:** a notice at the very top, before anything else. For example: *"Ultimate Linux Doom Builder is an unofficial Linux fork of Ultimate Doom Builder. The port was written largely by an AI coding assistant (Claude, by Anthropic) under the direction of the maintainer. Review it accordingly, and please report ULDB bugs here, not to the UDB team."*
- **About box:** a line under the credits: *"Linux port developed with AI assistance (Claude by Anthropic)."*
- **Repo metadata:** the repo description mentions it's AI-assisted. Topics include `ai-assisted`.
- **Commits:** every AI-authored commit carries a `Co-Authored-By: Claude` trailer, so it's visible commit by commit.
- **Release notes and Flatpak/AUR descriptions:** repeat the short disclosure.
- **Upstream etiquette:**
  - Make clear this is not affiliated with or endorsed by the UDB team.
  - Point issues at the ULDB repo.
  - Don't file ULDB-specific bugs upstream.
  - If something is ever offered back upstream, label it as AI-assisted in that PR too.
- **Licence:** GPL-3.0, the same as upstream. Keep `LICENSE.txt` and all upstream copyright notices. ULDB's own changes are under GPL-3.0 as well.
- **Before going public:**
  - a short `CONTRIBUTING.md` (how to report issues, the AI disclosure, build instructions)
  - a check that no personal paths, test data or credentials are in the history

## 7. Open questions

None. All decisions are settled; GitHub repo: `theoriginalzads/UltimateLinuxDoomBuilder`.

*Settled:* settings import from existing UDB installs (Phase 7), public repo with AI disclosure (section 6), BCS support with ZT-BCC and BCC (Phase 2b), plugin scope (section 0), dropping DeePBSP and the vanilla-only node builders, GZDoom/UZDoom-only target with other targets hidden in the UI (section 3), UI style (section 4), syncing only on major releases, GZDoom Doom-format and Hexen-format configs stay visible (new maps default to UDMF), project name "Ultimate Linux Doom Builder" (section 3), **windowing uses Avalonia's X11 backend**: native on X11 sessions, XWayland on Wayland sessions, so X11 and Wayland users get the same supported build. Native Wayland may be offered as an optional, experimental switch later, never as the default.

## 8. Main risks

| Risk | What we do about it |
|---|---|
| OpenGL integration with Avalonia (context sharing, resizing, vsync) | Prototype first thing in Phase 2–3, with an off-screen rendering fallback |
| Hidden WinForms dependencies in the core (`Control.Invoke`, timers, `Keys`, `Cursor`) | Phase 1 compiles the core with WinForms removed, so the compiler finds them all |
| Converting key codes in existing shortcut configs | Explicit mapping table and tests |
| Texture browser performance with large resource sets | Virtualised lists, background thumbnail decoding |
| Big, infrequent upstream merges (major releases only) | Untouched upstream file paths, `docs/port-map.md`, the UI-change detection script and round-trip tests make each merge a checklist |
| Scale (about 160 windows) | Strict priority order. The app is usable by the end of Phase 4, before every dialog is done |
