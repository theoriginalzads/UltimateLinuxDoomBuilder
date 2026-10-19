# What's different from Ultimate Doom Builder

A user-facing list of every intentional UI or behaviour difference between ULDB and upstream UDB, with the reason for each. If something moved, it's listed here.

| Area | Change | Reason |
|---|---|---|
| Resource list (Open Map, Map Options) | Icons are simple vector glyphs instead of upstream's bitmaps; missing resources show red text instead of a pink row | Upstream's ImageList can't be loaded on Linux; red reads better in dark mode |
| Resource list | Reorder by dragging an item onto another (no insertion marker yet) | Avalonia's ListBox has no built-in reorder; a marker is to come |
| Open Map, Add Resource | Native look (Avalonia Fluent theme, follows the desktop's light/dark setting); same controls, labels and order | New toolkit |
| Errors and Warnings | A list of icon + text rows instead of a grid; says "No errors or warnings." when empty; text is selectable; window slightly wider so the bottom row fits | Clearer, less dense (plan section 4) |
| Info panel (bottom of the main window) | Groups are rounded cards instead of captioned grey boxes; field names dimmed, values in normal text; textures/flats/sprites as 64 px tiles with a size badge and the name below; flags as a check-marked list; expand/collapse is a chevron. Same fields, order and grouping. Panel is 126 px tall (UDB: 106) | Main-window modernisation (plan section 4); approved by the user with screenshots, 20 Oct 2026 |
| Info panel, nothing highlighted | Shows the mode name and map statistics also in Debug builds (upstream Debug builds show a debug console there) | ULDB doesn't show the debug console |
| *(More entries are added as UI work continues in Phase 3.)* | | |
