# ULDB progress

**Based on upstream:** UltimateDoomBuilder `master` @ `42c8b37` (17 Oct 2026)
**Current phase:** Phase 0, done in the cloud session. The steps marked *(user)* need the user's machine.

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

## Next: Phase 1 (cloud-friendly, no screen needed)

1. Add `Builder.Linux.sln` and SDK-style `net10.0` project files for a UI-free `UDB.Core`. WinForms files are excluded with globs, and upstream `.csproj` files are untouched.
2. Fix compile errors from removing WinForms and System.Drawing from the core. This gives the real list of UI couplings.
3. Load/save round-trip tests against `tests/maps/`.
