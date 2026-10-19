# ULDB progress

**Based on upstream:** UltimateDoomBuilder `master` @ `42c8b37` (17 Oct 2026)
**Current phase:** Phase 3 started (branch `phase-3/shell`): the non-visual groundwork is done; the Avalonia window itself is next, best done with the user at their machine. Phases 0–2 are in PR theoriginalzads/UltimateLinuxDoomBuilder#1. Phase 0 steps marked *(user)* still need the user's machine.

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
| Avalonia app, `IWinFormsHost`, main window, viewport | ⬜ | Next (below) |

**Tests:** 137 passing.

### Remaining Phase 3 work

1. `UDB.Avalonia` app (or the UI in the `Builder` assembly) with Avalonia 12.1.4. It installs an `IWinFormsHost`, `Application.RunHandler` and a `HostedGL` context shared with Avalonia's, whose FBO it displays.
2. `MainForm` port: menus and toolbars from UDB's action and menu configs, status bar, info panel, dockers.
3. Viewport: render UDB into the shared context's FBO and show it in an `OpenGlControlBase`. Input goes into UDB's action system, with key code mapping.
4. Scope filter (GZDoom/UZDoom only) and branding, as the first config UI lands.
