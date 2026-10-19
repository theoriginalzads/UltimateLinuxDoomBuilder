# Setting up a CachyOS / Arch machine for ULDB

## 1. Install the toolchain

```sh
sudo pacman -S --needed base-devel git cmake ninja python zlib \
    dotnet-sdk mono mono-msbuild \
    libx11 libxfixes mesa
```

Freedoom isn't in the Arch or CachyOS repos (only the AUR), so it's left out above: if any package is missing, pacman installs nothing. Get it either from the AUR (`paru -S freedoom`) or by downloading the release zip:

```sh
mkdir -p ~/.local/share/doom
curl -L -o /tmp/freedoom.zip https://github.com/freedoom/freedoom/releases/download/v0.13.0/freedoom-0.13.0.zip
unzip -j /tmp/freedoom.zip '*/freedoom2.wad' '*/freedoom1.wad' -d ~/.local/share/doom
```

The tests look in `/usr/share/doom`, `/usr/share/games/doom` and `~/.local/share/doom`, or wherever `ULDB_TEST_IWAD` points.

Then check the .NET version:

```sh
dotnet --version
```

It should print `10.x`. If it prints an older version, install the versioned package instead (`dotnet-sdk-10.0`, if your repos have it).

| Package | Why it's needed |
|---|---|
| `dotnet-sdk` | .NET 10 SDK, for the new ULDB build |
| `mono`, `mono-msbuild` | Only for the upstream reference build (step 3). Can be removed later |
| `cmake`, `ninja`, `base-devel` | Native renderer, node builder and script compilers (built automatically by `dotnet build`) |
| `libx11`, `libxfixes`, `mesa` | Needed by the upstream native renderer |
| Freedoom (AUR or zip, see above) | Free IWADs, so test maps show real textures. Optional if you have your own Doom 2 IWAD |

## 2. Clone

```sh
git clone --recurse-submodules https://github.com/theoriginalzads/UltimateLinuxDoomBuilder.git
cd UltimateLinuxDoomBuilder
# Already cloned without submodules? Run: git submodule update --init
```

## 3. Upstream reference build (Mono)

This is the "before" version, used to compare against while porting.

```sh
make
cd Build && ./builder
```

If `make` fails, copy the error into the session. Don't spend time fixing it yourself.

## 4. Build and test ULDB so far

```sh
dotnet build Builder.Linux.sln
dotnet test Builder.Linux.sln     # renderer and resource tests use Freedoom + Mesa (no window needed)
```

On your machine, for a Zen 4 build of the native parts:

```sh
dotnet build Builder.Linux.sln -p:UldbArch=znver4
```

## 5. Generate the test maps

```sh
python3 tools/testmaps/make_testmaps.py
```

This writes to `tests/maps/`:

| File | What it covers |
|---|---|
| `small.wad` | Two-sector map, player start, one monster |
| `large.wad` | 2,500 sectors and 5,100 lines, for performance |
| `acs.wad` | ACS script (source plus compiled BEHAVIOR) and a switch that runs it |
| `bcs.wad` | BCS script source (ZT-BCC and BCC) |
| `3dfloors_slopes.wad` | Solid 3D floor, Plane_Align slope, vertex-height slope, a GZDoom dynamic light |
| `pk3_resource.pk3` | PK3 with MAPINFO, DoomEdNums, GLDEFS, a ZScript actor and an embedded map |

## 6. Reference screenshots (with the Mono build)

1. In the Mono build, set up the **UZDoom: Doom 2 (UDMF)** game configuration with `freedoom2.wad` as its resource.
2. Open each test map and take screenshots of:
   - the main window in 2D (Sectors mode) with `small.wad`
   - Visual mode (3D) in `3dfloors_slopes.wad`, looking at the 3D floor and the slopes
   - the linedef, sector and thing edit dialogs
   - the texture browser
   - Preferences (each tab)
   - the script editor with `acs.wad` open
3. Save them to `docs/reference-screenshots/` (create the folder) and commit them.
4. Optional but very useful: copy a few of **your own maps** into `tests/maps/user/`. That folder isn't committed unless you want it to be.

## 7. KDE setting for Wayland sessions

ULDB runs through XWayland on Wayland sessions. For sharp text at fractional scaling, set:

*System Settings → Display & Monitor → Legacy Applications (X11) → **Apply scaling themselves***
