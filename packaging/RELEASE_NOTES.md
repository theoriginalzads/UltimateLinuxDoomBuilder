# Ultimate Linux Doom Builder v0.6.2-beta

An **unofficial** native Linux fork of [Ultimate Doom Builder](https://github.com/UltimateDoomBuilder/UltimateDoomBuilder), built on .NET 10 and Avalonia instead of Mono and WinForms. It targets **GZDoom / UZDoom** maps only. The port is **AI-coded** (Claude), and that is disclosed publicly. It isn't endorsed by the UDB team.

This is a **beta**: every window the editor opens is ported, and the work is fixing and polishing. It has had little real-world use, so **back up your maps and save often.** v0.6.2 brings ZScript editing into the script editor and a Dialogue Builder for Strife-style conversations.

## Why use ULDB instead of UDB on Linux

UDB is a Windows program; on Linux it runs on Mono's WinForms. ULDB is the same editor (UDB's own map, mode and plugin code, unchanged where possible) with a new native front end. What that gets you:

- **A native Linux application.** .NET 10 and Avalonia, no Mono or Wine. Settings live in `~/.config`, it installs as a Flatpak or an Arch package, and the compilers (ACC, ZDBSP, ZT-BCC, BCC) are built from source and included.
- **It looks like it belongs on your desktop.** It follows the system's light or dark theme, has Breeze-style icons, scales cleanly on high-resolution displays, and has its own interface scale option.
- **A real script editor.** Syntax colours, folding, autocompletion, call tips, a find bar, hover help, error underlines, zoom, and line editing shortcuts (comment, move, duplicate, delete lines).
- **ZScript and dialogue without another program.** Edit the WAD's ZSCRIPT next to your ACS, with colours, help and error checking, and build conversations with forms instead of hand-writing them.
- **Testing maps without typing command lines.** ULDB finds the GZDoom and UZDoom you have installed, natively or as Flatpaks, and offers their launch options as tick boxes. It works from inside ULDB's own Flatpak too.
- **Dialogs that fit your screen.** The big edit dialogs scroll and keep OK and Cancel in reach on small screens; upstream's are cut off at 1280x720.
- **Long-standing UDB requests, done:**
  - new maps are autosaved before their first save;
  - Select Connected Linedefs;
  - a choice of background in the texture browser, including a chequerboard for transparency;
  - an adjustable thing rotation step;
  - a choice of how tags are treated when inserting prefabs;
  - a filter box in long lists.
- **Your UDB setup carries over.** ULDB can import the settings of an existing UDB installation on first start.

**What you give up:** ULDB only edits for GZDoom and UZDoom (other game configurations are hidden), it doesn't include UDBScript or the update window, and it is new, so it has had far less use than UDB. On Windows, use UDB.

## New since v0.6.1-beta

**ZScript in the script editor**
- A **ZSCRIPT** tab next to SCRIPTS and DIALOGUE, with a tab for each ZSCRIPT lump in the map's WAD and for each lump they `#include`. When the WAD has none, the tab starts empty so you can begin one.
- **Ctrl+S saves it straight into the map's WAD**, so Test Map runs what you saved. If another program (SLADE) changed the lump meanwhile, you're asked first. A new map has to be saved once before its ZScript can be. Reload resources (F8) to see your actors in the thing list.
- **Colours, autocompletion, call tips and hover help** from GZDoom's and UZDoom's own ZScript: about 2,400 functions with their signatures, plus keywords, types, classes and constants.
- **Errors as you type**: unmatched brackets, unclosed strings and comments, code outside a class, a missing `;` in States and Default blocks, a state without a duration. The engine still has the last word on types and names.

**Dialogue Builder**
- A **Dialogue Builder** button in the script editor's toolbar on the DIALOGUE tab. Conversations and pages on the left; the page (speaker, dialogue, portrait, voice, goodbye, drop, skip conditions) and its choices (next page, costs, require/exclude, messages, item given, log, action with the action browser and named arguments) on the right.
- Shows which things on the map use a conversation, and gives a conversation to the selected things.
- Points out links to pages that don't exist, missing text, repeated IDs and page names, and unknown class names.
- "Apply to script" writes the dialogue into the text (one undo step). Comments in the text are lost when it does; you're warned once.

**ACS and dialogue**
- 45 more ACS specials and functions with signatures (the Eternity-style floor, ceiling and stairs specials GZDoom supports, Line_/Sector_SetHealth, ScriptCall, StrArg, Round, GetSectorHealth and more), 4 keywords and 27 constants.
- The ZDoom and GZDoom dialogue fields (namespace, class, goodbye, userstring, pagename, specialname, require, exclude).

**Fixes**
- In case-insensitive languages (DECORATE, ZScript) function and class names were only coloured when typed in lowercase.
- After switching tabs in the script editor, the toolbar and status bar showed the previous tab's state.
- **Pasting** no longer pastes twice when Ctrl+V repeats or the mouse moves while the paste is being made (#46).
- **Menus and drop-down lists** open next to their button on KDE/Wayland at a scaled resolution (they appeared far away, for example the Test menu and the Engine list in Game Configurations).
- **Several monitors:** the main window opens where you left it, also when maximised, and dialogs open on the main window's monitor.
- **Recent files** in the File menu keep the whole file name ("…/tests/maps/small.wad", your home folder as `~`); the full path is the tooltip.
- Script editor **fold markers** are readable on the dark theme.
- **Preferences → Script Editor** shows the font in use (JetBrains Mono unless you chose another) and the colour preset in use, instead of a blank font and "Current colors".

## What works

- Opening, editing and saving GZDoom/UZDoom maps: the editing modes, the 2D and 3D views, undo/redo and all the map element dialogs.
- The built-in compilers (ACC, ZDBSP, ZT-BCC, BCC), and Test Map with GZDoom or UZDoom, installed natively or as a Flatpak, with launch options you tick instead of type.
- The bundled plugins: Builder Modes, 3D Floor Mode, Builder Effects, Stair Sector Builder, Tag Range, Sound Propagation, Color Picker, Tag Explorer and the Comments Panel.
- Texture, thing, action and effect browsers, Map Options, Game Configurations, Preferences, the command palette and the script editor.
- Importing settings from an existing UDB installation.
- Follows the system's light or dark theme, with Breeze-style icons and a layout UDB users will recognise. Dialogs fit small screens, and there is an interface scale option for high-resolution displays.

## Known limits

- Much of this release was checked in a headless test environment and not yet by eye on a real desktop. In particular: saving ZScript into the map's WAD, the Dialogue Builder, the Test menu, finding an AppImage engine, Test Map with a Flatpak engine, dialogs on small or scaled screens, and the interface scale option. Reports on these are especially welcome.
- Only GZDoom and UZDoom game configurations are shown, on purpose.
- The update window is not included: update through Flatpak or your package manager.
- The splash picture is a placeholder, and the Help menu has no manual yet.

## Install (Flatpak)

Download `ultimate-linux-doom-builder-v0.6.2-beta.flatpak` below (it appears once the build finishes), then:

```sh
flatpak install --user ./ultimate-linux-doom-builder-v0.6.2-beta.flatpak
flatpak run io.github.theoriginalzads.UltimateLinuxDoomBuilder
```

The runtime (`org.freedesktop.Platform//26.08`) comes from Flathub. Arch users can instead build from `packaging/arch/PKGBUILD`.

## Feedback

Bug reports are very welcome on the [issue tracker](https://github.com/theoriginalzads/UltimateLinuxDoomBuilder/issues). Please include what you were doing and, if possible, the text of the Errors window. Current status is tracked in `docs/progress.md`.
