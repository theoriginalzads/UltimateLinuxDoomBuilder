# axamlgen

Writes the large edit-form layouts (`Source/ULDB/Builder/Windows/*EditFormUDMFView.axaml`) from a
compact description in Python: one group per upstream group box, its fields in upstream's order
as "label: control [extras]" rows. `axamlgen.py` holds the helpers and the layout rules:

- every label column on a tab shares one width (`SharedSizeGroup="uldblabel"`, the tab is the
  scope), so fields line up from group to group, as UDB's single label column does
- number boxes keep a compact width; extras (checkboxes, reset buttons) sit beside their field
- static label texts come from upstream's designer (labels held in designer locals aren't bound)

```sh
python3 tools/axamlgen/linedef.py Source/ULDB/Builder/Windows/LinedefEditFormUDMFView.axaml
python3 tools/axamlgen/sector.py Source/ULDB/Builder/Windows/SectorEditFormUDMFView.axaml
```

Check a layout with `ULDB_SHOW_FORM=linedef:1` (form, tab index), which opens the dialog after a
map loads.
