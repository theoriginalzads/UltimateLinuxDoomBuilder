# ULDB progress

**Based on upstream:** UltimateDoomBuilder `master` @ `42c8b37` (17 Oct 2026)
**Current phase:** Phase 3 (`phase-3/shell` merged to `master` via PR #2 on 20 Oct 2026; start follow-up work on a new `phase-3/...` branch from `master`). **Early preview v0.1.0 released** (below). Milestone reached on 20 Oct 2026 (user tested: launch, File → Open, 2D/3D, pan, zoom, modes, save). Now porting the main windows to `.axaml`. Phases 0–2 are in PR theoriginalzads/UltimateLinuxDoomBuilder#1. Phase 0 steps marked *(user)* still need the user's machine.

## Phase 0: Fork, toolchain, baseline

| Step | Status | Notes |
|---|---|---|
| Fork, plan committed | ✅ | `docs/ULDB-Plan.md` |
| `CLAUDE.md` and tracking docs | ✅ | `progress.md`, `port-map.md`, `scope-hidden.md`, `ux-changes.md` |
| Toolchain (cloud container) | ✅ | .NET SDK 10.0.112 (Ubuntu apt package), Mono 6.8, cmake, ninja, Xvfb + Mesa |
| Toolchain (user's CachyOS machine) | ⏳ *(user)* | See `docs/setup-cachyos.md` |
| Upstream Mono reference build | ⏳ *(user)* | Not possible in the cloud container: Ubuntu's Mono has no `msbuild`, `dotnet msbuild` fails on the WinForms `.resx` files (MSB3822/MSB3823), and `xbuild` is too old. Arch's `mono-msbuild` should work |
| Native library (upstream Makefile flags) | ✅ | `libBuilderNative.so` compiles cleanly with g++ in about 13 s |
| Avalonia version pinned | ✅ | **Avalonia 12.1.4** (released 9 Oct 2026). AvaloniaEdit 12.0.0. SkiaSharp 4.153.x |
| Avalonia OpenGL on X11 checked | ✅ | See below |
| Test maps | ✅ | `tools/testmaps/make_testmaps.py` → `tests/maps/` (gitignored, generated) |
| Reference screenshots and the user's own maps | ⏳ *(user)* | `docs/setup-cachyos.md` step 5 |

### Avalonia 12 OpenGL prototype (findings)

The prototype is an `OpenGlControlBase` running under Xvfb with Mesa llvmpipe, using the X11 backend with GLX.

- It gets an **OpenGL 4.x core-profile context** (GL 4.5 on Mesa), which is more than UDB's 3.2 requirement.
- `GlInterface.GetProcAddress` works. This is the hook BuilderNative will use to load its GL functions (`gl_load`) from Avalonia's context.
- **Avalonia renders into its own framebuffer object** (`fb` was 1, not 0). Consequences for Phase 2:
  - BuilderNative's "unbind to default framebuffer" calls must bind Avalonia's FBO instead of 0: `GLRenderDevice.cpp:435` and `GLTexture.cpp:224`.
  - `Present()` / `SwapBuffers` becomes a no-op, because Avalonia presents the frame.
  - This fits as a new `IOpenGLContext` implementation next to the existing ones in `OpenGLContext.cpp`, which already has a variant with a no-op `SwapBuffers`.
- Avalonia 12 also ships an official `Avalonia.Wayland` package. This matters for the optional native-Wayland switch later. X11 remains the default, as decided.

## Phase 1: .NET 10 core without WinForms ✅ (branch `phase-1/core`)

See `docs/architecture.md` for how it's built, and the Phase 1 section of the plan for how it differs from the original sketch.

| Item | Status | Notes |
|---|---|---|
| `Builder.Linux.sln` (Compat, Builder, Tests) | ✅ | Upstream `.sln`/`.csproj` untouched |
| `ULDB.Compat`: `System.Drawing` on SkiaSharp 3.119.4 | ✅ | `Bitmap` (GDI+ memory layout), `Graphics`, fonts, brushes, paths |
| `ULDB.Compat`: non-visual WinForms | ✅ | Enums generated from the real assembly; services via `IWinFormsHost` |
| UI placeholders (`tools/uistubgen`) | ✅ | 126 generated files; 87 members pruned until ported |
| Embedded resources and `Properties.Resources` | ✅ | `tools/resxgen` |
| Data files copied next to the program | ✅ | `Assets/Common` and `Assets/Linux`, like upstream's Build folder |
| Platform: settings path, URLs, shell commands, portable check | ✅ | Listed in `docs/upstream-edits.md` |
| Tests | ✅ | 101 passing: upstream's 70, plus configs, UDMF round trips, compat drawing, platform |
| Resource (texture) loading tests | ➡ Phase 2 | They need the renderer (GPU textures). Freedoom is available via apt (`freedoom` package) |

**Upstream changes so far:** four `#if ULDB` edits (three in `General.cs`, one in `MapManager.cs`) and one bug fix (`EngineInfo.Dispose`).

### Known gaps carried forward
- The `Assets/Linux` prebuilt ACC/ZDBSP/ZenNode/ELFBSP binaries are still used. Phase 2b replaces them with source builds.
- `ExecuteExternalCommand` and anything else that opens a window go through placeholders until Phase 3/5. The placeholder `Form.ShowDialog()` throws `NotSupportedException` ("no ULDB implementation yet").
- `General.Main` still contains WinForms startup (`Application.Run`). In Phase 3 it runs through `Application.RunHandler`, which the Avalonia app installs.

## Phase 2: Renderer and build tools ✅ (branch `phase-2/renderer`)

| Item | Status | Notes |
|---|---|---|
| CMake build of BuilderNative | ✅ | `Source/ULDB/Native/CMakeLists.txt`; run by `dotnet build`; `-p:UldbArch=generic\|x86-64-v3\|x86-64-v4\|znver4\|native` |
| Hosted OpenGL context | ✅ | `HostedContext.cpp` and `ULDB.Rendering.HostedGL`; three `#ifdef ULDB` hooks in upstream native code |
| Off-screen renderer for tests | ✅ | `OffscreenGL`: Mesa surfaceless EGL with GL 3.3 core; no display needed |
| Resource loading tests | ✅ | Freedoom 2 plus the test PK3: textures, flats, sprites, ZScript actor, GLDEFS light, MAPINFO sky and skybox, with no warnings |
| ACC, ZDBSP, ZT-BCC, BCC from source | ✅ | Git submodules in `tools/compilers/src`, built by `dotnet build` |
| End-to-end open and save | ✅ | Upstream `InitializeOpenMap` and `SaveMap` headless: ZDBSP nodes plus BEHAVIOR from ACC, ZT-BCC and BCC |
| RawMouse replacement (3D mouselook) | ➡ Phase 3 | Needs the Avalonia window |

**Linux bugs found and fixed:**
- `Process.StartTime` after exit throws on Linux. This broke every compile and node build (`AccCompiler`, `NodesCompiler`, `Launcher`).
- Upstream's BCS setup:
  - ZT-BCC's config didn't list its documented header.
  - BCC was given `.tmp` files without `-x bcs`.
  - Upstream's BCC library files match no BCC release.

  ULDB ships fixed configs and BCC's own `lib/`. Details in `upstream-edits.md`.
- **Placeholders that must do real work:** `MainForm.RunOnUIThread` (image loading) and `MainForm.Display` (renderer). See `uistubs-manual.txt`.

**Tests:** 106 passing.

## Phase 3: Avalonia shell 🚧 (branch `phase-3/shell`)

| Item | Status | Notes |
|---|---|---|
| Branding | ✅ | `ULDB.Branding`; a module initializer brands `Application.ProductName`, so upstream's message boxes say "Ultimate Linux Doom Builder" |
| Scope filter | ✅ | `ULDB.Scope.ScopeFilter`: 14 visible configs, ZDBSP only, ACC/ZT-BCC/BCC; closest-config suggestion. `docs/scope-hidden.md` |
| Key mapping | ✅ | `ULDB.Input.KeyMapping`: Avalonia keys/modifiers/mouse buttons to WinForms codes; every default shortcut in the in-scope Actions.cfg files is reachable |
| Avalonia app entry point | ✅ | `Source/ULDB/Builder/Shell/Program.cs`: `Builder` is now an executable (`uldb` launcher script next to it). Avalonia is set up first (`SetupWithoutStarting`), then upstream's `General.Main` runs unchanged; `Application.Run` runs Avalonia's main loop |
| `IWinFormsHost` | ✅ (untested by hand) | `Shell/AvaloniaHost.cs`: message boxes, portal file dialogs, colour dialog (ColorView), clipboard (text to the desktop, UDB formats in-process), cursors, timers, `Invoke`/`BeginInvoke`, `ActiveForm`. `Shell/FormWindows.cs` links compat `Form`s to Avalonia windows |
| Viewport | ✅ | `ViewportContext` (UDB's own GL context, shared with Avalonia's; renders into an FBO) and `ViewportControl` (blits it). Present callback added to `HostedContext` |
| BuilderModes plugin | ✅ (logic) | Builds with generated placeholders for `Interface/`; upstream `MenusForm` compiles unchanged. UI ports are Phase 4 |
| Main window | 🟨 | **Model port**: upstream `MainForm.cs`/`.Designer.cs` compile unchanged on compat models; `Shell/MainWindow.cs` + `ToolStripViews.cs` show menus, toolbars, modes toolbar, status bar, map view. Info panels and dockers still placeholders |
| Open Map dialog | ✅ | `.axaml` ports of `OpenMapOptionsForm`, `ResourceOptionsForm`, `ResourceListEditor` (user tested). Ported forms show through `Form.CreateView` → `AvaloniaHost.ShowForm` (`docs/architecture.md`) |
| Save | ✅ | Ctrl+S in the UI: ZDBSP runs, `TEXTMAP` + `ZNODES` written (user tested 20 Oct 2026) |
| Input | ✅ | Keys, wheel, mouse buttons into upstream's handlers (via a hit-testable layer over the GL view). `ULDB_TRACE_INPUT=1` logs events and their actions. 3D mouselook: `Shell/X11Pointer.cs` (XQueryPointer/XWarpPointer) + screen mappers; user-tested on KDE Wayland (XWayland): "feels just like the original" |

**Tests:** 188 passing.

### Remaining Phase 3 work (pick up here)

The user wants this done **on their machine**, with them watching the window. Work in this order and show them each step running:

**Session ended 20 Oct 2026 (evening); resume with item 1 below (texture browser).** All work is committed and pushed on `phase-3/shell`; 188 tests pass. Later cloud sessions (no screen) finished item 2 (the icons: show them to the user first) and work from Phases 6 and 7: see "Done ahead of order" below.

**State on 20 Oct 2026 (user tested):** open via the Open Map dialog, edit, save; info panels (modern cards), dockers (Help docker; plugin dockers say "not ported"), and the edit dialogs Vertex, Thing (UDMF), Sector (UDMF), Linedef (UDMF), Edit Action and Edit Text all work as model ports with .axaml windows. The user reviewed the dialogs against UDB screenshots and approved the compact, aligned layout (shared label column per tab, 92 px number boxes, previews filling space, Linedef sides grouped by wall part like Sector surfaces, no action help button). First pass of Breeze-style toolbar icons (`Shell/BreezeIcons.cs`). Next, in order:
1. **Texture browser** (`TextureBrowserForm`): clicking texture/flat pickers does nothing until it's ported (its placeholder returns null and the picker keeps its texture).
2. ~~Remaining icons~~ ✅ done in a cloud session, 20 Oct 2026, **not yet seen on a real screen**: show the user the menus (Edit, View, Tools, Help, the lights/models dropdowns) and the Sector dialog's reset buttons, and adjust any they don't like. `Shell/ResourceImageNames.cs` matches any upstream image to its `Properties.Resources` name by pixel content (this also catches form .resx copies), so `BreezeIcons` has `res:<Name>` specs plus aliases to the action icons. `tools/iconsheet` renders all icons to light/dark preview sheets; `BreezeIconTests` checks the specs and the lookup. BuilderEffects' three menu icons are in place for Phase 6. Not converted on purpose: splash/About artwork, `ErrorLarge*`/`WarningLarge*` (Errors window), `Monster2/3` and engine icons (Test menu), thing/texture images.
3. Non-UDMF edit forms (`LinedefEditForm`, `SectorEditForm`, `ThingEditForm`) for GZDoom's Doom/Hexen-format configs; Map Options, game configurations (`ConfigForm`, with `ScopeFilter`), preferences, script editor.
4. BuilderModes' docker panels (Undo/Redo, Draw Settings, Edit Selection, Draw Grid...) register views in `ControlViews` (Phase 4).
5. Window geometry persistence for the main window; startup polish (window shows empty for 1–2 s); resource list drop marker.
6. Modernise the main window further (plan section 4; user wants it): toolbars, status bar, menus.
7. 3D mouselook has no pointer grab for `Cursor.Clip`; add one if users report the pointer escaping.

**How the dialogs are built (read before porting more):** `docs/architecture.md` ("Two ways to port a window", item 3, and "Language version"). Large layouts are generated by `tools/axamlgen` (see its README); check any dialog with `ULDB_SHOW_FORM=linedef:1` (form, tab) or `ULDB_SHOW_FORM=action:buildermodes_sectorsmode`, and add `ULDB_CAPTURE=file.png` to have ULDB render its own window to a PNG (`Shell/DevCapture.cs`). **Never use screen captures**: they can catch the user's other windows. **In a cloud container**, `tools/capture/capture.sh OUT.png [FORM]` opens a test map with Freedoom under Xvfb (`ULDB_RENDERING=egl`, Mesa llvmpipe) and captures the main window or a dialog; the Errors window is skipped, and ULDB exits after the capture (`ULDB_CAPTURE_EXIT=1`). The map area itself captures white (the GL view isn't part of Avalonia's render-to-bitmap); dialogs capture fully. `ULDB_DUMP_TOOLBARS=1` lists toolbar items and their tags (for icons). Headless tests (`EditFormTests`) construct each model-ported form and run its Setup on a real map: add one for every new form.

App icon: **Cacky, the ULDB Cacodemon** (`Source/ULDB/Assets/Icons/`, SVG source plus PNGs; 16 px is text-free), approved by the user. **About dialog (user request, 20 Oct 2026): when `AboutForm` is ported, show Cacky as its image/icon** (in place of UDB's logo artwork), alongside the UZDoom/GZDoom reference and the AI disclosure from `Branding`. Avalonia sends one icon size, so the title bar shows the 256 px one scaled down; the `.desktop` file with hicolor sizes (packaging) fixes that. `WM_CLASS` is `uldb`; revisit with the `.desktop` file so it matches the app ID. Plugins aren't built yet except BuilderModes (Phase 4).

**Milestone:** launch, open a test map (`tests/maps/*.wad` with Freedoom), see it in 2D and 3D, pan and zoom, switch modes, save. ✅ reached 20 Oct 2026.

## Issue fixes (branch `phase-3/issue-fixes`, 20 Oct 2026)

GitHub issues #6–#15 worked through. #6, #8–#12, #14, #15 fixed and checked with ULDB's own captures, dev steps or a unit test (`PickingALevelInTheUndoPanelUndoesToIt`); the details are in `docs/ux-changes.md`. **#7** (exit on an unknown error) wasn't reproduced; `ExceptionDialog` is now ported, so an unhandled exception should show the real error instead. **#13**: opening a map takes ~3.7 s in Debug for both `small.wad` and `large.wad`, so the time is fixed start-up/resource loading, not map size; the window now shows an "Opening map..." overlay (`ULDB.Platform.Busy`). The map still loads on the UI thread (a real fix would move it off-thread). Release timing wasn't measured. New: `tests/maps/tags.wad`, dev steps in `MainWindow` (see the `ULDB_SHOW_FORM` handling), `~/.cache/uldb-shots/loadtime.sh` (not in the repo). To verify on the real desktop: toolbar » popups, tree expand/rename, Undo/Redo clicking, Find & Replace closing, Add Resource alignment, comment placeholder, link icons, the overlay, and whether #7 recurs.

### Texture browser (branch `phase-3/issue-fixes`, 20 Oct 2026)

`TextureBrowserForm` and `ImageBrowserControl` now compile unchanged (model port): ULDB adds `ImageSelectorPanel` (state) and `ImageBrowserItem` (no drawing) as its own files, and the views `TextureBrowserFormView`, `ImageBrowserControlView` and `ImageTileList` (the tile grid). Compat gained `Control.ProcessTabKey` and `SplitContainer.SplitterRectangle`. Check with `ULDB_SHOW_FORM=texbrowser:textures|flats[:NAME]`; `EditFormTests` has a test. Checked on a ULDB capture only (list, tree, filter row); **not tried by hand**: picking from the Sector/Linedef dialogs, double-click, cursor keys, typing to filter, classic view, folders, used-first, multi-resource trees, resizing the split. The Flat/Texture set editor (`TextureSetForm`) and `FlatBrowserForm` (unused upstream) aren't ported.

### Dialogs and the script editor (branch `phase-3/issue-fixes`, 20 Oct 2026, evening)

Ported (model ports unless noted): Map Options (hand port), Game Configurations, Preferences, Grid Setup, Go To Coordinates, Change Map, Things Filters, tag/thing statistics, Paste Special, pre/post commands, Texture Set, thing/effect browsers, Angle, and the Doom/Hexen-format edit forms. `tools/resxgen/autoresx.py` generates the form `.resx` files a new model port needs; `tools/axamlgen/preferences.py` writes Preferences' layout. Dev steps: `newform:<Type>`, `prefs:<tab>`, `config:<tab>`, `texbrowser:`, `oldsector|oldlinedef|oldthing`.

**Script editor (new engine).** Upstream's Linux build stubs the ScintillaNET API over a plain TextBox (`Controls/Scripting/TextEditorControl.cs`, `#if NO_SCINTILLA`), which is what draws the lines through the text and crashes in the search code. ULDB implements that API for real: `Source/ULDB/Builder/Scripting/` has the `ScintillaNET` types over an AvaloniaEdit document (`Scintilla.cs`, `ScintillaTypes.cs`), a lexer that gives every character upstream's Scintilla style numbers (`ScriptLexer.cs`), and the AvaloniaEdit view (`ScintillaView.cs`: JetBrains Mono, syntax colours from Preferences, line numbers, error markers, brace matching, word highlights, folding, autocompletion, call tips). Upstream's `ScriptEditorControl`, `ScriptHandler`, the document tabs, `ScriptEditorPanel` and `ScriptEditorForm` compile unchanged on top; views: `ScriptEditorControlView`, `ScriptEditorPanelView` (tabs with an unsaved-changes dot), the form, go-to-line and find/replace windows. See `docs/ux-changes.md`.

## Issues #17, #19, #20 (branch `phase-3/issues-17-19-20`, 21 Oct 2026, no screen)

Dialogs lose the minimise button (#17); Game Configurations list/spacing/garbled checkbox (#19: the two Windows-only path options are hidden); Preferences (#20): colour name labels, live script editor preview, eight extra colour presets (`ULDB.Scripting.ScriptColorPresets`, hooked into `PreferencesForm` with `#if ULDB`), plugin tabs (Builder Modes "Editing", 3D Floor Mode) via `TabPageViews`. All checked with container captures; **to verify on the real machine:** the layouts at your window size (the cut-off texts in #20 didn't reproduce at 1040x760, so say if any remain), picking a preset and OK, the 3D Floor Mode tab, the Builder Modes tab's lower groups. Font combo lists are empty in the container (no installed fonts to list?): check yours. #18: 28 new menu icons (see `docs/ux-changes.md`); `ULDB_DUMP_TOOLBARS=1` (with `ULDB_KEEP_LOG=file` on `capture.sh`) now lists every menu item with `icon=True/False`, submenus included. Still open: the plugin windows left (ColorPicker, SoundPropagation), #13.

## ColorPicker and Sound Propagation windows (branch `phase-3/plugin-windows`, 21 Oct 2026, no screen)

Both plugins' remaining windows are ported (see `docs/port-map.md`): ColorPicker's wheel control, sliders and the light and sector pickers; Sound Propagation's colour configuration and reverbs picker. Compat `Control` gained `Invalidated` and `RaisePaint(Graphics, Rectangle)` so owner-drawn controls can be painted into a bitmap by their view. New dev step `plugform:<assembly>:<form type>` opens a plugin form. **To try on the real machine:** dragging in the colour wheel and brightness strip (mouse; only the static drawing was captured), typing hex/float values, OK/Cancel colours, the relative mode and secondary-intensity sliders on spot lights, picking a reverb from a real list. The plugin windows list in "Remaining Phase 3 work" is now done apart from the BuilderModes preferences (done in PR #21).

## Leftover placeholders, window geometry, About centring (branch `phase-3/leftovers`, 21 Oct 2026, no screen)

- **Ported** (captured in the container; see `docs/port-map.md`): Flags, BitFlags, BitFlags and Options, Custom Fields, Linedef Color Presets (with a new coloured checklist binder), Run External Command (error lines in red; `#if ULDB` edit for killing the process tree), the command palette (overlay), and `DebugConsole`'s static logging/profiler. **Not ported on purpose:** the Update window (hidden), `OptimizedListView`/`ResourceListView`/`ButtonsNumericTextboxDesigner` (nothing uses them), `AboutForm` (ULDB's own `AboutView`).
- **Window position and size:** the main window restores what upstream's `DelayedForm` saved (size, place, maximised) and raises `ResizeEnd` half a second after a change so it's remembered; a first start keeps the default size. Checked by closing and restarting under Xvfb; **try on the real machine, incl. a second monitor and Wayland (the compositor may ignore the position)**.
- **Dialogs** with no saved position open centred on the main window (the About window measured centred to within 1 px).
- **Splash:** a placeholder picture at `Source/ULDB/Assets/Overrides/Splash3_trans.png` (725x354); drop your own PNG there to replace it (see `docs/ux-changes.md`).
- **To try by hand:** the command palette (typing, arrows, Enter, Escape, clicking), Run External Command in a real before/after-save command, Linedef Color Presets (add/delete/move, colours, actions), Flags dialogs from Find & Replace and thing args, Custom Fields from the linedef dialog.

## Early preview release (20 Oct 2026)

- `phase-3/shell` merged into `master` (PR #2); CI green on `master`.
- Tag `v0.1.0-preview` pushed; the Flatpak workflow built the bundle on the tag (the build and bundle steps passed). Its release step failed because the release had already been created by hand, so PR #3 changed it to attach to an existing release (and a manual run can take `release_tag`).
- GitHub pre-release **v0.1.0 (Early Preview)** is published with `uldb.flatpak` (about 45.9 MB) attached by hand. Its text is the first version of the notes; the newer wording is `packaging/RELEASE_NOTES.md` (PR #4, merged) and has to be pasted in by hand, since the cloud session can't edit releases.
- Not verified on a real desktop: installing and running the Flatpak, `makepkg -si` for the AUR PKGBUILD, and the dialogs listed under "Remaining Phase 3 work".
- Cloud-session limits found: no tag pushes, no release edits and no PR merges from the container; the user does those.

## Done ahead of order (cloud session, 20 Oct 2026, no screen)

Work the user approved doing without their machine. All on `phase-3/shell`.

| Item | Status | Notes |
|---|---|---|
| Plugins (Phases 4 and 6) | 🟨 logic | All nine in-scope plugins build and load: `Source/ULDB/Plugins/<Name>/` for ThreeDFloorMode, BuilderEffects, StairSectorBuilder, TagRange, SoundPropagationMode, ColorPicker, TagExplorer, CommentsPanel (plus BuilderModes). Same setup as BuilderModes: placeholders for the UI (`tools/uistubgen/prune.py <Name>`), resxgen resources. Model ports (compile unchanged): every plugin's menu holder (`MenusForm`/`ToolsForm`), 3D floors' tooltip list (`ThreeDFloorPanel`, `ThreeDFloorHelperTooltipElementControl`), sound environments' tree (`SoundEnvironmentPanel`). `PluginTests` loads them all and switches into every plugin mode on a real map. **Not yet usable:** modes that open a dialog first (Stair Sector Builder, Slope and Draw Slopes modes ask for a slope data sector, OBJ terrain import) cancel, because the dialog placeholders answer Cancel; the plugins' dockers (Tag Explorer, Comments, 3D floors, sound environments) show the "not ported" note; ColorPicker's windows and BuilderEffects' jitter forms are placeholders. Porting these is Phase 4/6 UI work |
| Settings import (Phase 7) | ✅ (untested by hand) | `Source/ULDB/Builder/Settings/` (`ULDB.Settings.UdbSettingsImport`), user doc `docs/settings-import.md`. Sources: Mono build, Flatpak, Wine prefixes, legacy `GZBuilder.cfg`, or a picked file/folder. Converts Windows/Wine paths, lists unresolved ones in a summary (`UDBImport.txt`), keeps shortcuts as-is (ULDB uses WinForms `Keys` codes too), never touches the source. First start: Yes/No question (`Shell/Program.cs`, before `General.Main`); `--import-udb-settings[=PATH]`. ULDB's own settings: `$XDG_CONFIG_HOME/UltimateLinuxDoomBuilder/`. 22 tests. **To do:** click through it on the user's machine with their real UDB settings; *Preferences → Import UDB settings…* once Preferences is ported (with a drive-letter mapping for mounted Windows installs) |
| CI | ✅ | `.github/workflows/uldb-ci.yml`: build + tests (first run passed, 20 Oct 2026) on ubuntu-24.04 with Freedoom and Mesa. Upstream's workflows are untouched (they may fail on the fork; disable them in the repo's Actions settings if they're noise). Flatpak artifact builds still to do |
| Packaging | 🟨 | `packaging/`: `build.sh` (Release publish + plugins), `install.sh` (PREFIX/DESTDIR; app in `lib/uldb`, hicolor icons from Cacky), `.desktop` (`StartupWMClass=uldb`), AppStream metainfo (both validate), `arch/PKGBUILD` (`uldb-git`, `_arch=znver4` option), Flatpak manifest (runtime 26.08 + dotnet10 extension; **untested**, needs `nuget-sources.json` generated, see `packaging/README.md`). **To do on the user's machine:** `cd packaging/arch && makepkg -si`, then check the KDE menu entry, icon and taskbar grouping. The metainfo release (0.1.0) is a placeholder |

### Cloud session 3 (20 Oct 2026, no screen): where it stopped

**Resume here.** Everything is committed and pushed on `phase-3/shell`; 188 tests pass.

- **Flatpak CI is green**: `.github/workflows/uldb-flatpak.yml` builds `uldb.flatpak` (download it from the run's artifacts in GitHub Actions). Fixes on the way: SDK-first PATH (the extension's `install.sh` runs after the build), ACC's static link on the Freedesktop SDK (`tools/compilers/CMakeLists.txt` gives it an empty `libatomic_asneeded.a`), failed compiler builds now print their errors. **Not yet run on a desktop**: `flatpak install --user uldb.flatpak`, then launch.
- **User's review fixes:** Stair builder height cards on their own row; wider, single-line number boxes in the jitter forms.
- **Ported this session** (model port + view; all captured in the container and look right):
  - Tag Range's form; Tag Explorer docker (tree with Breeze icons); Comments docker (list, click-to-view/select, right-click menu); Sound Environments docker; 3D floors docker (cards per floor: not seen with content, it needs a hovered sector: **check on the user's machine**).
  - BuilderModes: Undo/Redo, Draw Settings, Draw Grid and Edit Selection dockers; the drawing modes' toolbar options (line, curve, rectangle, ellipse, curve linedefs) compile unchanged and show in the toolbar.
- **Shared plumbing added:** context menus (compat `ContextMenuStrip.Show` → Avalonia), `DataGridView.CellLocator`/`HitTest`, any compat `TreeView` gets the shared tree view via `ControlViews`, dockers set only the shown docker's control `Visible` (plugins refresh on `VisibleChanged`), compat ListView `SelectedIndexChanged`/`BeginUpdate`/`EndUpdate`/item `Index`/`EnsureVisible`, `ULDB_SHOW_FORM` step `docker:<name>`.
- **Bug found and fixed:** plugins' `user32.dll`/`BuilderNative.dll` imports didn't resolve (only Builder had the resolver); BuilderModes' Undo/Redo panel would have crashed the app at startup once shown. `Platform/NativeLibraries.cs` now covers assemblies loaded from `Plugins/`.
  - BuilderModes: Map Analysis (`ErrorCheckForm`) and Find and Replace (`FindReplaceForm`) windows, on new compat `ListBox`/`ProgressBar` models (ModelBinder binds them both ways). Not yet run end to end here (an analysis or search needs clicks): **try both on the user's machine**.
  - BuilderModes' smaller windows: Make Door, Bridge options, Fit Textures, Filter Selected Things, Paste Properties Special, Change Map Element Index, Slope Arch, Image Export Settings. Captured and checked: Make Door, Filter Selected Things, Change Index, Image Export. **Not captured** (need a particular selection or Visual mode): Bridge, Fit Textures, Slope Arch, Paste Properties Special: **try them on the user's machine**. The BuilderModes Preferences page (`PreferencesForm`) compiles unchanged but has no view yet: it's a tab of the core Preferences window, so do it with that.
  - **User review fix:** Edit Selection number boxes were cut off; fixed.
  - New shared view `Controls/Inputs/DataGridRowsView.cs`: any plain compat `DataGridView` (header + rows, multi-select) via `ControlViews`; grids with editing have their own views. Dev switch step `selectone:<type>`.
  - 3DFloorMode: the 3D floors editor window (one card per floor), Edit Slope Vertex and Control Sector Area config. Compat gets `CheckedListBox` (+ `ItemCheck`), `Panel.ScrollControlIntoView`; ModelBinder binds a CheckedListBox with a checkbox per row. **Only the editor's empty state was captured** ("There are no 3D floors"); the cards, Edit Slope Vertex and the CSA dialog need real 3D floors/slopes: **try them on the user's machine**. Dev switch step `call:<plugin>:<BuilderPlug method>`. Sizable forms open at UDB's designer size but never below the view's `MinWidth`/`MinHeight`, so set those to what the layout needs.
- **Next:** the colour pickers (need mouse testing, best with the user); then the core windows from the Phase 3 list (texture browser, Map Options, Preferences, ...), which need the user's review. Plugin UI left: ColorPicker windows, SoundPropagation colour configuration/reverbs picker, preferences pages (with the core Preferences window), 3D floor sector settings if any.
- Captures can show colour fringes on grey text (subpixel antialiasing in the render-to-bitmap); not a real colour.

### Cloud session 2 (20 Oct 2026, no screen): where it stopped

**Resume here.** Everything is committed and pushed on `phase-3/shell`; 188 tests pass.

- **Dialogs can be checked in the container now.** `tools/capture/capture.sh OUT.png "STEPS"` opens a test map (Xvfb, `ULDB_RENDERING=egl`) and captures a dialog. Steps are chained with `+`: `action:<name>`, `select:sectors|linedefs|things|vertices`, `sector`, `linedef:1`... e.g. `"action:buildermodes_sectorsmode+select:sectors+action:buildereffects_applyjitter"`. About 13 s per capture.
- **Plugin dialogs ported (model port + `.axaml` in the plugin's `Views/`, registered by `Views/PluginViews.cs`):**
  - ThreeDFloorMode: slope data sector dialog (Slope and Draw Slopes modes now get past it)
  - StairSectorBuilder: the whole Stair Sector Builder window
  - BuilderEffects: all of it (jitter vertices/sectors/things, directional shading, OBJ import settings, `IntControl`)
  - Captured and checked here; **show the user the screenshots for sign-off** (they haven't seen them).
- **Still placeholders:** TagRange's form, 3D floor editor window, control sector area config, slope vertex edit form, 3D floors preferences, ColorPicker windows, SoundPropagation's colour configuration and reverbs picker, and the dockers (Tag Explorer, Comments, 3D floors, sound environments). Next in order: TagRangeForm (small), then the dockers.
- **App-wide style change:** `NumericUpDown` now looks like `ButtonsNumericTextbox` (slim stacked arrows; `Shell/Styles.axaml`).
- **Icons:** plugin edit modes and 3D floors' slope buttons have Breeze icons; plugin resources can have their own spec as `res:<Assembly>/<Name>` (BuilderEffects' `Update` is a re-roll arrow, not check-for-updates). The edit-modes toolbar scrolls.
- **Flatpak CI (`.github/workflows/uldb-flatpak.yml`) is not green yet.** Fixed so far: system-wide SDK, `dir` source (the git source built `master`), NuGet restore with the Flatpak's own .NET SDK (10.0.401) via `tools/flatpak/nuget-sources.py`, a writable home and checkout for the sandbox. The last finished run failed in flatpak-builder while downloading NuGet sources (it was restoring the Tests project too); run 39361072710 (program and plugins only) was still running at the end of the session. Job logs can't be downloaded from the cloud container (Azure blob is blocked), so failures are reported as **annotations**: `gh api repos/theoriginalzads/UltimateLinuxDoomBuilder/check-runs/<job id>/annotations`.
- Main CI (`uldb-ci.yml`) is green.

## Handoff notes (for the next session)

- **Branches:**
  - `phase-1/core` → `phase-2/renderer` → `phase-3/shell` (each builds on the previous).
  - Phases 0–2 are in PR [#1](https://github.com/theoriginalzads/UltimateLinuxDoomBuilder/pull/1) into `master`; it may already be merged when you read this.
  - Continue on `phase-3/shell`. If #1 has been merged, merge `master` into it first.
- **User's machine checklist** (`docs/setup-cachyos.md`). Ask whether these are done:
  - install the toolchain
  - clone with `--recurse-submodules`
  - `dotnet test Builder.Linux.sln`, expecting 149 passing
  - the Mono reference build, reference screenshots in `docs/reference-screenshots/`
  - own maps in `tests/maps/user/` (optional)
- **On the user's machine the window is real:** the Avalonia app runs on their KDE Plasma (X11 backend; XWayland on Wayland sessions). Use `-p:UldbArch=znver4` for their builds if they want it.
- **User's machine (CachyOS):** VS Code runs as a **Flatpak**, so the agent's shell is sandboxed: run `dotnet`, `git push` and the app through `flatpak-spawn --host sh -c "cd '$PWD' && …"`. The sandbox's `/tmp` is private; put files the host must write (screenshots: `spectacle -b -n -a -o ~/.cache/uldb-shots/x.png`) under `$HOME`. Freedoom is in `~/.local/share/doom/`.
- **GitHub from cloud sessions:**
  - `gh` GraphQL is blocked; use `gh api` (REST).
  - The repo is a fork, so always target `theoriginalzads/UltimateLinuxDoomBuilder` explicitly when opening PRs, never upstream.
- **Open decisions:** none. Everything is in `docs/ULDB-Plan.md` sections 0 and 7.
