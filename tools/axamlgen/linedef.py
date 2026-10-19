import sys; sys.path.insert(0, ".")
from axamlgen import *
import os
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
load_texts(ROOT + '/Source/Core/Windows/LinedefEditFormUDMF.Designer.cs')
I = "          "
G = I + "  "
B = G + "    "
LBL = '<Grid.ColumnDefinitions><ColumnDefinition Width="Auto" SharedSizeGroup="uldblabel"/>'

# Properties: everything fits (compact flag rows); labels share one column with the arguments'.
props = f'''{I}<StackPanel Spacing="6">
''' + "\n".join([
    group("flagsgroup", f'{B}<ContentControl x:Name="flags"/>', G),
    group("groupsettings", f'''{B}<Grid>
{B}  {LBL}<ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
{B}  <TextBlock x:Name="labelrenderstyle"{text("labelrenderstyle")} Classes="uldb-field" HorizontalAlignment="Right"/>
{B}  <ComboBox x:Name="renderStyle" Grid.Column="1" Width="170"/>
{B}  <TextBlock x:Name="label6"{text("label6")} Grid.Column="2" Classes="uldb-field" Margin="16,0,8,0"/>
{B}  <ContentControl x:Name="alpha" Grid.Column="3" Width="100"/>
{B}  <Button x:Name="resetalpha" Grid.Column="4" Classes="uldb-tool" VerticalAlignment="Center"/>
{B}  <TextBlock x:Name="labellockpick"{text("labellockpick")} Grid.Column="6" Classes="uldb-field"/>
{B}  <ComboBox x:Name="lockpick" Grid.Column="7" Width="200"/>
{B}</Grid>''', G),
    group("actiongroup", f'''{B}<Grid>
{B}  {LBL}<ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
{B}  <TextBlock x:Name="label2"{text("label2")} Classes="uldb-field" HorizontalAlignment="Right"/>
{B}  <ContentControl x:Name="action" Grid.Column="1"/>
{B}  <Button x:Name="browseaction" Grid.Column="2" Classes="uldb-tool" Margin="4,0,0,0" ToolTip.Tip="Browse actions"/>
{B}</Grid>
{B}<ContentControl x:Name="argscontrol"/>''', G),
    group("activationGroup", f'{B}<ContentControl x:Name="udmfactivates"/>', G,
        title=f'<StackPanel Orientation="Horizontal" Spacing="6"><TextBlock x:Name="activationGroupTitle"{text("activationGroup")} Classes="uldb-group-title"/><Image x:Name="missingactivation" Width="16" Height="16"/></StackPanel>'),
    group("idgroup", f'{B}<ContentControl x:Name="tagsselector"/>', G),
]) + f"\n{I}</StackPanel>"

def side(S, s, box, sector, seclabel, light, lightreset, lightabs, lightlabel, custombtn, offgroup, scalegroup, flagsgroup, flags):
    C = I + "    "
    lights = "\n".join(f'{C}      <ContentControl x:Name="light{s}{p}" HorizontalAlignment="Left"/>' for p in ["upper", "middle", "lower"])
    left = "\n".join([
        group(box, rows([(seclabel, sector), (lightlabel, light, f"{lightabs}:check", f"{lightreset}:button")], C + "    ", 0) +
              f'\n{lights}\n{C}    <Button x:Name="{custombtn}" HorizontalAlignment="Left"/>',
              C, title='<TextBlock Text="Sector and light" Classes="uldb-group-title"/>'),
        group(offgroup, rows([(f"label{S}TextureOffset", f"{s}TextureOffset:pair"), (f"label{S}OffsetTop", f"pfc{S}OffsetTop:pair"), (f"label{S}OffsetMid", f"pfc{S}OffsetMid:pair"), (f"label{S}OffsetBottom", f"pfc{S}OffsetBottom:pair")], C + "    ", 0), C),
        group(scalegroup, rows([(f"label{S}ScaleTop", f"pfc{S}ScaleTop:pair"), (f"label{S}ScaleMid", f"pfc{S}ScaleMid:pair"), (f"label{S}ScaleBottom", f"pfc{S}ScaleBottom:pair")], C + "    ", 0), C),
        group(flagsgroup, f'{C}    <ContentControl x:Name="{flags}"/>', C),
    ])
    textures = "\n".join(f'''{C}  <DockPanel Grid.Row="{r}" Margin="0,0,0,{0 if r == 2 else 8}">
{C}    <TextBlock DockPanel.Dock="Top" Text="{t}" Opacity="0.7" FontSize="12" Margin="2,0,0,2"/>
{C}    <ContentControl x:Name="{s}{n}"/>
{C}  </DockPanel>''' for r, (t, n) in enumerate([("Upper", "high"), ("Middle", "mid"), ("Lower", "low")]))
    return f'''{I}<DockPanel>
{I}  <CheckBox x:Name="{s}side" DockPanel.Dock="Top" FontWeight="SemiBold" Margin="0,0,0,6"/>
{I}  <Grid x:Name="{s}group" ColumnDefinitions="*,250" ColumnSpacing="12">
{I}    <StackPanel Spacing="6">
{left}
{I}    </StackPanel>
{I}    <Grid Grid.Column="1" RowDefinitions="*,*,*">
{textures}
{I}    </Grid>
{I}  </Grid>
{I}</DockPanel>'''

tabs = "\n".join([
    fixedtab("tabproperties", props),
    fixedtab("tabfront", side("Front", "front", "groupBox5", "frontsector", "label11", "lightFront", "resetfrontlight", "cbLightAbsoluteFront", "labelLightFront", "customfrontbutton", "groupBox6", "frontscalegroup", "frontflagsgroup", "flagsFront")),
    fixedtab("tabback", side("Back", "back", "groupBox4", "backsector", "label12", "lightBack", "resetbacklight", "cbLightAbsoluteBack", "labelLightBack", "custombackbutton", "groupBox1", "backscalegroup", "backflagsgroup", "flagsBack")),
    plaintab("tabcomment", '        <ContentControl x:Name="commenteditor"/>'),
    plaintab("tabcustom", '        <ContentControl x:Name="fieldslist"/>'),
])
open(sys.argv[1], "w").write(window("LinedefEditFormUDMFView", "Edit Linedef", 880, 920, tabs))
