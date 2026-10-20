# Ultimate Linux Doom Builder: early preview (v0.2.0)

An **unofficial** native Linux fork of [Ultimate Doom Builder](https://github.com/UltimateDoomBuilder/UltimateDoomBuilder), built on .NET 10 and Avalonia instead of Mono and WinForms. It targets **GZDoom / UZDoom** (UDMF) maps only. The port is **AI-coded** (Claude), and that is disclosed publicly. It isn't endorsed by the UDB team.

## What to expect

This is a very early preview, meant for testers who are comfortable with rough edges. **Back up your maps and save often.**

**Working (as far as it has been tested):**
- Opening, editing and saving GZDoom/UZDoom UDMF maps, with the editing modes, 2D and 3D views, undo/redo, and the UDB map-element dialogs (sector, linedef, thing, vertex, and so on).
- The built-in compilers (ACC, ZDBSP, ZT-BCC, BCC) for building and testing maps.
- Most bundled plugins (3D Floor Mode, Builder Effects, Stair Sector Builder, Tag Range, Sound Propagation, Color Picker, Tag Explorer, Comments Panel, Builder Modes) with their main dialogs.
- New since 0.1.0: texture browser, Map Options, Preferences, thing/effect browsers, and a rewritten script editor (syntax colours, JetBrains Mono, unsaved-change dots, find/replace, autocompletion, error list) that doesn't have upstream's Linux search crash. Toast notifications, and fixes for issues #6–#15.
- Importing settings from an existing UDB install.
- Breeze-style icons, with a layout that should feel familiar to UDB users.

**Not done yet / expect placeholders:**
- Still placeholders: linedef colour presets, the command palette and the debug console. The update window is skipped on purpose (update through Flatpak/your package manager).
- Opening a map while another is open can freeze the window for a few seconds (#13).
- A few ported dialogs were only checked in a headless test environment, not by eye on a real desktop: Bridge, Fit Textures, Slope Arch, the 3D floor editors, Map Analysis, and Find & Replace.
- Non-GZDoom/UZDoom game targets are hidden on purpose.
- The splash screen and some artwork are placeholders. The Help menu has no manual yet.

## Install (Flatpak)

Download `ultimate-linux-doom-builder-v0.2.0-preview.flatpak` below (it appears once the build finishes), then:

```sh
flatpak install --user ./ultimate-linux-doom-builder-v0.2.0-preview.flatpak
flatpak run io.github.theoriginalzads.UltimateLinuxDoomBuilder
```

The runtime (`org.freedesktop.Platform//26.08`) comes from Flathub. Arch users can instead build from `packaging/arch/PKGBUILD`.

## Feedback

Bug reports are very welcome on the [issue tracker](https://github.com/theoriginalzads/UltimateLinuxDoomBuilder/issues). Please include what you were doing and, if possible, the Errors window text. Current status is tracked in `docs/progress.md`.
