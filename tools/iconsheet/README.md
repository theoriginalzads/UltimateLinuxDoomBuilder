# iconsheet

Preview sheets of ULDB's Breeze-style icons (`Source/ULDB/Builder/Shell/BreezeIcons.cs`), in light and dark, at 16 and 32 px. Use it to review icon changes without starting the editor.

```sh
python3 tools/iconsheet/specs.py Source/ULDB/Builder/Shell/BreezeIcons.cs /tmp/icons.tsv
dotnet run --project tools/iconsheet -- /tmp/icons.tsv /tmp/icons          # all icons
dotnet run --project tools/iconsheet -- /tmp/icons.tsv /tmp/res "res:"     # only keys starting with "res:"
```

It writes `<prefix>-light.png` and `<prefix>-dark.png`. Aliases (resource images that reuse an action's icon) aren't listed separately.
