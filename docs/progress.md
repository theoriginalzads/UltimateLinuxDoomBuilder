# ULDB progress

**Based on upstream:** UltimateDoomBuilder `master` @ `42c8b37` (17 Oct 2026)
**Current phase:** Phase 3 (branch `phase-3/shell`): **milestone reached on 20 Oct 2026** (user tested: launch, File → Open, 2D/3D, pan, zoom, modes, save). Now porting the main windows to `.axaml`. Phases 0–2 are in PR theoriginalzads/UltimateLinuxDoomBuilder#1. Phase 0 steps marked *(user)* still need the user's machine.

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

**Tests:** 159 passing.

### Remaining Phase 3 work (pick up here)

The user wants this done **on their machine**, with them watching the window. Work in this order and show them each step running:

**State on 20 Oct 2026 (user tested):** open via the Open Map dialog, edit, save; info panels (modern cards), dockers (Help docker; plugin dockers say "not ported"), and the edit dialogs Vertex, Thing (UDMF), Sector (UDMF), Linedef (UDMF), Edit Action and Edit Text all work as model ports with .axaml windows. The user reviewed the dialogs against UDB screenshots and approved the compact, aligned layout (shared label column per tab, 92 px number boxes, previews filling space, Linedef sides grouped by wall part like Sector surfaces, no action help button). First pass of Breeze-style toolbar icons (`Shell/BreezeIcons.cs`). Next, in order:
1. **Texture browser** (`TextureBrowserForm`): clicking texture/flat pickers does nothing until it's ported (its placeholder returns null and the picker keeps its texture).
2. Remaining icons: toolbars, mode buttons, BuilderModes menu items and the status bar use Breeze-style icons (user: "they all look fine"); dialogs' small buttons (reset, link, browse, clear) and other menus' items still show UDB bitmaps. Add specs to `BreezeIcons` (keyed by action tag/name; `ToolStripViews.IconKeyProvider` for state icons).
3. Non-UDMF edit forms (`LinedefEditForm`, `SectorEditForm`, `ThingEditForm`) for GZDoom's Doom/Hexen-format configs; Map Options, game configurations (`ConfigForm`, with `ScopeFilter`), preferences, script editor.
4. BuilderModes' docker panels (Undo/Redo, Draw Settings, Edit Selection, Draw Grid...) register views in `ControlViews` (Phase 4).
5. Window geometry persistence for the main window; startup polish (window shows empty for 1–2 s); resource list drop marker.
6. Modernise the main window further (plan section 4; user wants it): toolbars, status bar, menus.
7. 3D mouselook has no pointer grab for `Cursor.Clip`; add one if users report the pointer escaping.

**How the dialogs are built (read before porting more):** `docs/architecture.md` ("Two ways to port a window", item 3, and "Language version"). Large layouts are generated by `tools/axamlgen` (see its README); check any dialog with `ULDB_SHOW_FORM=linedef:1` (form, tab) or `ULDB_SHOW_FORM=action:buildermodes_sectorsmode`, and add `ULDB_CAPTURE=file.png` to have ULDB render its own window to a PNG (`Shell/DevCapture.cs`). **Never use screen captures**: they can catch the user's other windows. `ULDB_DUMP_TOOLBARS=1` lists toolbar items and their tags (for icons). Headless tests (`EditFormTests`) construct each model-ported form and run its Setup on a real map: add one for every new form.

App icon: **Cacky, the ULDB Cacodemon** (`Source/ULDB/Assets/Icons/`, SVG source plus PNGs; 16 px is text-free), approved by the user. Avalonia sends one icon size, so the title bar shows the 256 px one scaled down; the `.desktop` file with hicolor sizes (packaging) fixes that. `WM_CLASS` is `uldb`; revisit with the `.desktop` file so it matches the app ID. Plugins aren't built yet except BuilderModes (Phase 4).

**Milestone:** launch, open a test map (`tests/maps/*.wad` with Freedoom), see it in 2D and 3D, pan and zoom, switch modes, save. ✅ reached 20 Oct 2026.

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
