# What's different from Ultimate Doom Builder

A user-facing list of every intentional UI or behaviour difference between ULDB and upstream UDB, with the reason for each. If something moved, it's listed here.

| Area | Change | Reason |
|---|---|---|
| Resource list (Open Map, Map Options) | Icons are simple vector glyphs instead of upstream's bitmaps; missing resources show red text instead of a pink row | Upstream's ImageList can't be loaded on Linux; red reads better in dark mode |
| Resource list | Reorder by dragging an item onto another (no insertion marker yet) | Avalonia's ListBox has no built-in reorder; a marker is to come |
| Open Map, Add Resource | Native look (Avalonia Fluent theme, follows the desktop's light/dark setting); same controls, labels and order | New toolkit |
| *(More entries are added as UI work continues in Phase 3.)* | | |
