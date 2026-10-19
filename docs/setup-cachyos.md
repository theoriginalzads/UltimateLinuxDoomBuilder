# Setting up a CachyOS / Arch machine for ULDB

## 1. Install the toolchain

```sh
sudo pacman -S --needed base-devel git cmake ninja python \
    dotnet-sdk mono mono-msbuild \
    libx11 libxfixes mesa freedoom
```

Then check the .NET version:

```sh
dotnet --version
```

It should print `10.x`. If it prints an older version, install the versioned package instead (`dotnet-sdk-10.0`, if your repos have it).

| Package | Why it's needed |
|---|---|
| `dotnet-sdk` | .NET 10 SDK, for the new ULDB build |
| `mono`, `mono-msbuild` | Only for the upstream reference build (step 3). Can be removed later |
| `cmake`, `ninja`, `base-devel` | Native renderer and the node builders/compilers (Phase 2) |
| `libx11`, `libxfixes`, `mesa` | Needed by the upstream native renderer |
| `freedoom` | Free IWADs (`/usr/share/doom/freedoom2.wad`), so test maps show real textures. Optional if you have your own Doom 2 IWAD |

## 2. Clone

```sh
git clone https://github.com/theoriginalzads/UltimateLinuxDoomBuilder.git
cd UltimateLinuxDoomBuilder
```

## 3. Upstream reference build (Mono)

This is the "before" version, used to compare against while porting.

```sh
make
cd Build && ./builder
```

If `make` fails, copy the error into the session. Don't spend time fixing it yourself.

## 4. Generate the test maps

```sh
python3 tools/testmaps/make_testmaps.py
```

This writes to `tests/maps/`:

| File | What it covers |
|---|---|
| `small.wad` | Two-sector map, player start, one monster |
| `large.wad` | 2,500 sectors and 5,100 lines, for performance |
| `acs.wad` | ACS script (source plus compiled BEHAVIOR) and a switch that runs it |
| `bcs.wad` | BCS script source only. Compiling it needs ZT-BCC, which comes in Phase 2b |
| `3dfloors_slopes.wad` | Solid 3D floor, Plane_Align slope, vertex-height slope, a GZDoom dynamic light |
| `pk3_resource.pk3` | PK3 with MAPINFO, DoomEdNums, GLDEFS, a ZScript actor and an embedded map |

## 5. Reference screenshots (with the Mono build)

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

## 6. KDE setting for Wayland sessions

ULDB runs through XWayland on Wayland sessions. For sharp text at fractional scaling, set:

*System Settings → Display & Monitor → Legacy Applications (X11) → **Apply scaling themselves***
