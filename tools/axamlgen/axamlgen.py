# Helpers to write edit-form .axaml: groups of "label: control [extras]" rows.
# A control spec is "name" (ContentControl, for upstream user controls), "name:combo",
# "name:check", "name:button" (tool button), "name:text" (value label), "name:textbox".
import re
from xml.sax.saxutils import quoteattr
TEXTS = {}
def load_texts(designer):
    """Static texts from an upstream designer (labels held in locals aren't bound, so the layout carries them)."""
    src = open(designer, encoding="utf-8-sig").read()
    for n, t in re.findall(r'(?:this\.)?(\w+)\.Text = "(.*?)";', src):
        TEXTS[n] = t.strip()

def text(name):
    t = TEXTS.get(name)
    return f" Text={quoteattr(t)}" if t else ""

def ctl(spec, col, row, extra=""):
    name, _, kind = spec.partition(":")
    pos = f'Grid.Row="{row}" Grid.Column="{col}"' if col is not None else ""
    if kind == "combo": return f'<ComboBox x:Name="{name}" {pos} HorizontalAlignment="Left" Width="230" MaxDropDownHeight="400"{extra}/>'
    if kind == "smallcombo": return f'<ComboBox x:Name="{name}" {pos} HorizontalAlignment="Left" Width="160" MaxDropDownHeight="400"{extra}/>'
    if kind == "widecombo": return f'<ComboBox x:Name="{name}" {pos} HorizontalAlignment="Stretch" MaxDropDownHeight="400"{extra}/>'
    if kind == "check": return f'<CheckBox x:Name="{name}" {pos} Margin="6,0,0,0" VerticalAlignment="Center"{extra}/>'
    if kind == "button": return f'<Button x:Name="{name}" {pos} Classes="uldb-tool" VerticalAlignment="Center"{extra}/>'
    if kind == "text": return f'<TextBlock x:Name="{name}" {pos} VerticalAlignment="Center"{extra}/>'
    if kind == "textbox": return f'<TextBox x:Name="{name}" {pos}{extra}/>'
    if kind == "image": return f'<Image x:Name="{name}" {pos} Width="16" Height="16" Margin="4,0" VerticalAlignment="Center"{extra}/>'
    if kind == "wide": return f'<ContentControl x:Name="{name}" {pos}{extra}/>'
    if kind == "pair": return f'<ContentControl x:Name="{name}" {pos} Width="250"{extra}/>'
    # Number boxes and other small controls keep a compact width.
    return f'<ContentControl x:Name="{name}" {pos} Width="130"{extra}/>'

def rows(items, ind, extracols=0):
    """items: list of (label, control, *extras); label may be None. Extras follow the control."""
    # Label columns share one width across the tab (Grid.IsSharedSizeScope on the tab), so fields
    # line up from group to group, like UDB's single label column.
    out = [f'{ind}<Grid RowDefinitions="{",".join(["Auto"] * len(items))}" RowSpacing="4">',
           f'{ind}  <Grid.ColumnDefinitions><ColumnDefinition Width="Auto" SharedSizeGroup="uldblabel"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions>']
    for r, item in enumerate(items):
        label, control, *extras = item
        if label: out.append(f'{ind}  <TextBlock x:Name="{label}"{text(label)} Grid.Row="{r}" Classes="uldb-field" HorizontalAlignment="Right"/>')
        if not control: continue
        if control.endswith((":wide", ":widecombo")) and not extras:
            out.append(f'{ind}  ' + ctl(control, 1, r))
            continue
        out.append(f'{ind}  <StackPanel Grid.Row="{r}" Grid.Column="1" Orientation="Horizontal" Spacing="4">')
        for c in [control] + extras: out.append(f'{ind}    ' + ctl(c, None, None))
        out.append(f'{ind}  </StackPanel>')
    out.append(f'{ind}</Grid>')
    return "\n".join(out)

def group(name, body, ind, title=None, extra=""):
    t = title if title is not None else f'<TextBlock x:Name="{name}Title"{text(name)} Classes="uldb-group-title"/>'
    return f'''{ind}<Border x:Name="{name}" Classes="uldb-group"{extra}>
{ind}  <StackPanel Spacing="6">
{ind}    {t}
{body}
{ind}  </StackPanel>
{ind}</Border>'''

def window(cls, title, w, h, tabs, footer=""):
    return f'''<!-- ULDB: the window of upstream's {cls[:-4]} (model-ported: upstream's code runs unchanged).
     Controls named like upstream's fields are bound to them by ModelBinder.BindByName; tabs,
     groups and field order follow upstream's designer. Generated layout, then edited by hand. -->
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="CodeImp.DoomBuilder.Windows.{cls}"
        Title="{title}" Width="{w}" Height="{h}" MinWidth="{w - 80}" MinHeight="{h - 80}" ShowInTaskbar="False">
  <DockPanel Margin="14">
    <Grid DockPanel.Dock="Bottom" ColumnDefinitions="*,Auto" Margin="0,12,0,0">
{footer}      <StackPanel Grid.Column="1" Orientation="Horizontal" Spacing="8">
        <Button x:Name="apply" IsDefault="True" MinWidth="100" HorizontalContentAlignment="Center" Classes="accent"/>
        <Button x:Name="cancel" IsCancel="True" MinWidth="100" HorizontalContentAlignment="Center"/>
      </StackPanel>
    </Grid>
    <TabControl x:Name="tabs" Padding="0,10,0,0">
{tabs}
    </TabControl>
  </DockPanel>
</Window>
'''

def tab(name, body):
    return f'''      <TabItem x:Name="{name}">
        <ScrollViewer HorizontalScrollBarVisibility="Disabled" Grid.IsSharedSizeScope="True">
{body}
        </ScrollViewer>
      </TabItem>'''

def fixedtab(name, body):
    """A tab that fits without scrolling."""
    return f'''      <TabItem x:Name="{name}">
        <Panel Grid.IsSharedSizeScope="True">
{body}
        </Panel>
      </TabItem>'''

def plaintab(name, body):
    return f'''      <TabItem x:Name="{name}">
{body}
      </TabItem>'''

def grid2(cells, ind, colspec="*,*"):
    """cells: list of (row, col, colspan, text)."""
    nrows = max(r for r, *_ in cells) + 1
    out = [f'{ind}<Grid ColumnDefinitions="{colspec}" RowDefinitions="{",".join(["Auto"] * nrows)}" ColumnSpacing="8" RowSpacing="8">']
    for r, c, span, text in cells:
        text = text.replace("<Border x:Name=", f'<Border Grid.Row="{r}" Grid.Column="{c}" Grid.ColumnSpan="{span}" x:Name=', 1)
        out.append(text)
    out.append(f'{ind}</Grid>')
    return "\n".join(out)
