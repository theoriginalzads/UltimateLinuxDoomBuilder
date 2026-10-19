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
| Dockers (side panel) | Same place, tabs and behaviour (auto-hide, pin, resize); tabs are flat buttons with an accent highlight, the selected docker's name is shown as a heading, and the pin button shows a pushpin (upright = stays open, tilted = auto-hide) instead of arrows | Modernisation; pin icon chosen by the user, 20 Oct 2026 |
| Open Map dialog | Larger minimum size (500×700) so the maps list and resources never overlap | Fix after user testing |
| Edit dialogs (Edit Vertex first) | Same tabs, groups, fields and order; groups are rounded outlines with a bold title; number boxes have slim up/down buttons; the custom fields grid has flat headers with a sort arrow and inline editors | Modernisation (plan section 4); approved by the user, 20 Oct 2026 |
| Thing type tree | Thing icons are coloured dots (things) and folders (categories) in the editor's thing colours, instead of UDB's bitmaps; rows are compact | ImageList data in .resx can't be loaded on Linux; user liked the tighter list |
| *(More entries are added as UI work continues in Phase 3.)* | | |
