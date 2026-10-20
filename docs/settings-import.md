# Importing your Ultimate Doom Builder settings

ULDB can bring over the settings of an existing Ultimate Doom Builder (UDB) install, so you don't have to set up resources, test engines and shortcuts again.

ULDB keeps its own settings in `~/.config/UltimateLinuxDoomBuilder/UDBuilder.cfg` (`$XDG_CONFIG_HOME/UltimateLinuxDoomBuilder` if you set that variable). That is a different folder from upstream UDB's, so both editors can be installed side by side without touching each other's settings.

## When it happens

- **First start.** If ULDB has no settings yet and finds UDB settings on your computer, it asks whether to import them. Answer *No* and ULDB starts with its default settings; it won't ask again.
- **On request.** Start ULDB with `--import-udb-settings` to import the settings it finds, or with `--import-udb-settings=PATH` to import a particular file or folder:

  ```sh
  uldb --import-udb-settings
  uldb --import-udb-settings="/mnt/windows/Users/Me/AppData/Local/Doom Builder"
  ```

  (In a development build, run `dotnet Builder.dll` with the same switch.) This replaces ULDB's current settings. The old file is kept as `UDBuilder.cfg.before-import`.
- A *Preferences → Import UDB settings…* menu entry will follow once the Preferences window is ported.

Starting ULDB with `-portable` or `-nosettings` skips the first-start question.

## Where ULDB looks

| Install | Settings file |
|---|---|
| UDB's Mono build on Linux | `~/.local/share/Doom Builder/UDBuilder.cfg` (or under `$XDG_DATA_HOME`) |
| Official UDB Flatpak | `~/.var/app/io.github.ultimatedoombuilder.ultimatedoombuilder/data/Doom Builder/UDBuilder.cfg` |
| UDB for Windows under Wine | `<prefix>/drive_c/users/<you>/AppData/Local/Doom Builder/UDBuilder.cfg`, in `$WINEPREFIX` and `~/.wine`. Older (XP-style) prefixes: `<prefix>/drive_c/users/<you>/Local Settings/Application Data/Doom Builder/` |
| GZDoom Builder | `GZBuilder.cfg` in any of the folders above (UDB still reads this older file when it has no `UDBuilder.cfg`) |
| A Windows install, e.g. on a mounted Windows drive | Not found automatically: use `--import-udb-settings=PATH`. `PATH` can be the `UDBuilder.cfg` file, the `Doom Builder` folder, the folder above it, or your Windows user folder (`…/Users/<you>`) |

When several are found, ULDB offers the most recently used `UDBuilder.cfg`.

## What's imported

Everything in UDB's settings file, including:

- **Preferences**: editing, appearance, 3D view, defaults, autosave, paste options, colours.
- **Keyboard shortcuts**, unchanged. ULDB stores shortcuts with the same key codes as UDB, so every shortcut (including mouse buttons and wheel bindings) works as before.
- **Game configuration settings**: resources, test engines and their parameters, node builder choices, start mode, Things filters, texture sets, linedef colour presets.
- **Recent files**.
- **Script editor settings**: font, tab width, tabs or spaces, auto-indent, brackets, line numbers, folding.
- **Plugin settings**.

Settings for game configurations that ULDB doesn't show (anything other than GZDoom and UZDoom, such as vanilla Doom or Boom) are imported too. ULDB just keeps those configurations hidden.

Per-map settings (`.dbs` files) sit next to your maps and are picked up without any import.

**Your UDB settings file is only ever read, never changed.**

## How paths are converted

Settings hold paths to IWADs, resources, test engines and recent maps. ULDB checks each one:

- **Linux paths** (from the Mono build or the Flatpak) are kept as they are.
- **Wine paths** use the prefix's own drive mapping (its `dosdevices` folder): `C:\Games\doom2.wad` becomes `<prefix>/drive_c/Games/doom2.wad`, and `Z:\home\me\doom2.wad` becomes `/home/me/doom2.wad`.
- **Windows paths from a Windows drive**: when you pick a folder inside `…/Users/…` on the mounted drive, that drive is taken to be `C:`. So with the drive mounted at `/mnt/windows`, `C:\Doom\doom2.wad` becomes `/mnt/windows/Doom/doom2.wad`.
- Backslashes become slashes.
- **Upper and lower case**: Windows ignores case in file names and Linux doesn't. If a path doesn't exist exactly as written, ULDB looks for it again ignoring case, so `C:\DOOM\DOOM2.WAD` still finds `drive_c/Doom/doom2.wad`.

## The import summary

After importing, ULDB shows a summary, and saves the full version as `~/.config/UltimateLinuxDoomBuilder/UDBImport.txt`. It lists:

- how many shortcuts, game configurations (and how many of those are hidden in ULDB) and recent files were imported
- how many paths were converted
- **paths that couldn't be found**, with the setting each belongs to and why. For example: a file on a drive ULDB can't map (`D:\…`), a network path (`\\server\…`), or a file that no longer exists. These are **kept exactly as they were**, never dropped, so you can see what they pointed to and fix them in the game configuration or preferences.
- **other things to check**:
  - a test engine that is a Windows program (`.exe`). The path is right, but ULDB runs Linux programs: point it at a Linux GZDoom/UZDoom, or at a script that starts the `.exe` with Wine.
  - Windows paths inside longer settings, such as extra engine parameters (`-file C:\…`). These are left unchanged.
