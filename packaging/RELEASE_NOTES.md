# Ultimate Linux Doom Builder: early preview

An **unofficial**, native Linux fork of [Ultimate Doom Builder](https://github.com/UltimateDoomBuilder/UltimateDoomBuilder) (.NET 10 + Avalonia), for GZDoom/UZDoom maps. The port is **AI-coded** (Claude), and that is disclosed publicly. It is not endorsed by the UDB team.

## Install (Flatpak)

```sh
flatpak install --user ./ultimate-linux-doom-builder-*.flatpak
flatpak run io.github.theoriginalzads.UltimateLinuxDoomBuilder
```

The runtime (`org.freedesktop.Platform//26.08`) comes from Flathub. Arch users can build from `packaging/arch/PKGBUILD`.

## Status

This is a preview. It was developed and tested in a headless container, so expect rough edges:

- Several dialogs are still placeholders (texture browser, Map Options, Preferences, script editor).
- Some ported dialogs have not been checked by eye on a real desktop (Bridge, Fit Textures, Slope Arch, 3D floor editors, Map Analysis, Find & Replace).
- Save your work often and keep backups of your maps.

See `docs/progress.md` for details. Bug reports are welcome on the issue tracker.
