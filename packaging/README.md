# Packaging

How to build and install Ultimate Linux Doom Builder (ULDB) as a package. Everything here
is ULDB's own. Upstream's `flatpak/` folder (the Mono build of Ultimate Doom Builder) is left
unchanged.

| File | What it is | Tested? |
|---|---|---|
| `build.sh` | Builds a framework-dependent Release tree | Yes |
| `install.sh` | Installs that tree into `PREFIX`/`DESTDIR` | Yes |
| `io.github.theoriginalzads.UltimateLinuxDoomBuilder.desktop` | Menu entry | Yes, `desktop-file-validate` |
| `io.github.theoriginalzads.UltimateLinuxDoomBuilder.metainfo.xml` | AppStream metadata (software centres) | Yes, `appstreamcli validate` |
| `arch/PKGBUILD` | AUR/CachyOS package `uldb-git` | Syntax only (`bash -n`); `makepkg` not run yet |
| `flatpak/io.github.theoriginalzads.UltimateLinuxDoomBuilder.yml` | Flatpak manifest | **Not tested**; `flatpak-builder` not run yet |

The app ID is `io.github.theoriginalzads.UltimateLinuxDoomBuilder`, and the command is `uldb`.
The window's `WM_CLASS` is `uldb` (set in `Source/ULDB/Builder/Shell/Program.cs`), so the
desktop file's `StartupWMClass=uldb` lets the taskbar match the window to the menu entry.

## What the build needs

- .NET 10 SDK to build. The installed program needs the .NET 10 runtime (it's framework-dependent).
- `cmake`, `ninja` and a C/C++ compiler for `libBuilderNative.so` and the script compilers
  (ACC, ZDBSP, ZT-BCC, BCC).
- X11, Xfixes, OpenGL/EGL and zlib development headers.
- The compiler sources, which are git submodules: `git submodule update --init`.

## build.sh and install.sh

```sh
git submodule update --init
packaging/build.sh                         # ULDB_ARCH=znver4 for a Zen 4 build
sudo packaging/install.sh                  # PREFIX=/usr/local by default
```

`build.sh` builds the plugins, then runs `dotnet publish` for `linux-x64`,
framework-dependent (about 38 MB). The finished tree is
`Source/ULDB/Builder/bin/Release/net10.0/linux-x64/publish`. Extra arguments go to every
`dotnet` command (for example `--source DIR` for an offline restore).

Settings:

- `ULDB_ARCH`: CPU target for the native code: `generic` (default), `x86-64-v3`, `x86-64-v4`,
  `znver4` or `native`. Only use a tuned build on the machine it's for.
- `ULDB_RID`: .NET runtime identifier, `linux-x64` by default.

`install.sh` puts files here:

| Path | Contents |
|---|---|
| `$PREFIX/lib/uldb/` | The program, data files (`Configurations/`, `Compilers/`, `Scripting/`, ...), plugins |
| `$PREFIX/bin/uldb` | Launcher |
| `$PREFIX/share/applications/` | `.desktop` file |
| `$PREFIX/share/metainfo/` | AppStream metadata |
| `$PREFIX/share/icons/hicolor/` | "Cacky, the ULDB Cacodemon" at 16 to 512 px, plus scalable SVG |
| `$PREFIX/share/licenses/uldb/` | `LICENSE.txt` |

Settings: `PREFIX` (default `/usr/local`), `DESTDIR` (staging root for packages) and `APPDIR`
(the built tree, if it's somewhere else). `install.sh` replaces any older `$PREFIX/lib/uldb`
completely. To uninstall, delete the files in the table above.

The program never writes into `$PREFIX/lib/uldb`. Settings and the log file go to
`~/.config/UltimateLinuxDoomBuilder/` (or `$XDG_CONFIG_HOME`).

## Arch Linux and CachyOS (AUR, `uldb-git`)

```sh
cd packaging/arch
makepkg -si                       # generic x86-64
_arch=znver4 makepkg -si          # tuned for Zen 4 (Ryzen 7000/9000)
```

The PKGBUILD clones the repository and the four compiler repositories, then points the
submodules at those clones in `prepare()` (the usual AUR way), so `makepkg` can verify and
cache every source. `build()` runs `build.sh`, and `package()` runs `install.sh` with
`PREFIX=/usr`. NuGet packages are downloaded into `src/nuget-packages`, not `~/.nuget`.

## Flatpak (not tested yet)

The manifest uses `org.freedesktop.Platform` 26.08 (like upstream's `flatpak/` manifest; 24.08
is past its end of life) and the `org.freedesktop.Sdk.Extension.dotnet10` SDK extension, which
exists for 24.08, 25.08 and 26.08. The .NET runtime is copied into the app (`/app/lib/dotnet`).

Permissions: `--socket=x11` (ULDB uses Avalonia's X11 backend on purpose, so not
`fallback-x11`), `--share=ipc`, `--device=dri` for OpenGL, and `--filesystem=home` for maps,
resources and test engines. File dialogs and opening links go through portals, which are
allowed by default.

flatpak-builder has no network access during the build, so NuGet packages must be listed as
sources first. Restore with the Flatpak's own .NET SDK (so implicit package versions match the
offline build), then list the packages with `tools/flatpak/nuget-sources.py`:

```sh
flatpak install flathub org.freedesktop.Sdk//26.08 org.freedesktop.Sdk.Extension.dotnet10//26.08
flatpak run --command=sh --runtime=org.freedesktop.Sdk//26.08 --share=network --filesystem=host \
  org.freedesktop.Sdk.Extension.dotnet10//26.08 \
  -c 'export PATH="$PATH:/usr/lib/sdk/dotnet10/bin"; dotnet restore Builder.Linux.sln -r linux-x64 --packages "$PWD/nuget-restore"'
tools/flatpak/nuget-sources.py nuget-restore packaging/flatpak/nuget-sources.json
rm -rf nuget-restore
```

Generate the file again whenever a `PackageReference` changes. It is not committed
(`packaging/flatpak/.gitignore`). `build.sh` builds the plugins and the program for the same
runtime identifier (`linux-x64`), so this one restore covers both. The ULDB Flatpak workflow
(`.github/workflows/uldb-flatpak.yml`) does exactly this and keeps the bundle as an artifact.

Then build and install:

```sh
flatpak-builder --user --install --force-clean build-dir \
  packaging/flatpak/io.github.theoriginalzads.UltimateLinuxDoomBuilder.yml
flatpak run io.github.theoriginalzads.UltimateLinuxDoomBuilder
```

The manifest builds your checkout as it is (a `dir` source with `path: ../..`), including the
submodules, so run `git submodule update --init` first.

## What has been tested

On Ubuntu 24.04 (the cloud container) with .NET SDK 10.0.112:

- `packaging/build.sh` produced the tree, with `libBuilderNative.so`, the compilers and the
  BuilderModes plugin.
- An offline restore: `build.sh --source <folder of .nupkg files>` with an empty
  `NUGET_PACKAGES`, the same way the Flatpak build restores.
- `install.sh` into a `DESTDIR` and into a `PREFIX`; then the installed `uldb` started under
  Xvfb and the log (`UDBuilder.log`) ended with "Startup done", with the BuilderModes plugin
  loaded. A Release build writes its log only to that file, not to the terminal.
- `desktop-file-validate` and `appstreamcli validate --no-net` (also `validate-tree` on the
  installed tree) pass. `desktop-file-validate` gives one hint: `Development;Game;` puts the
  app in two menu categories, which is intended.

Not tested yet: `makepkg` on Arch/CachyOS, `flatpak-builder`, launching from the KDE menu,
and opening a `.wad` from a file manager.
