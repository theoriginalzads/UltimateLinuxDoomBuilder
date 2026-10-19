import sys; sys.path.insert(0, ".")
from axamlgen import *
import os
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
load_texts(ROOT + '/Source/Core/Windows/SectorEditFormUDMF.Designer.cs')
I = "            "
G = I + "    "
B = G + "    "

props = grid2([
    (0, 0, 2, group("groupBox3", f'{B}<ContentControl x:Name="flags"/>', G)),
    (1, 0, 1, group("groupfloorceiling", rows([("label6", "ceilingheight"), ("label5", "floorheight"), ("label15", "heightoffset"), ("sectorheightlabel", "sectorheight:text")], B, 0), G)),
    (1, 1, 1, group("groupdamage", rows([("label16", "damagetype:combo", "resetdamagetype:button"), ("label17", "damageamount"), ("label18", "damageinterval"), ("label19", "leakiness")], B, 1), G)),
    (2, 0, 2, group("groupeffect", f'''{B}<Grid ColumnDefinitions="Auto,*,Auto">
{B}  <TextBlock x:Name="label8" Text="Special:" Classes="uldb-field" HorizontalAlignment="Right"/>
{B}  <ContentControl x:Name="effect" Grid.Column="1"/>
{B}  <Button x:Name="browseeffect" Grid.Column="2" Classes="uldb-tool" Margin="4,0,0,0"/>
{B}</Grid>
{B}<Grid ColumnDefinitions="Auto,*">
''' + rows([("label9", "brightness"), ("label2", "gravity")], B + "  ", 0) + "\n" +
        rows([("label14", "soundsequence:combo", "resetsoundsequence:button"), ("label4", "fogdensity")], B + "  ", 1).replace("<Grid ", '<Grid Grid.Column="1" ', 1) + f"\n{B}</Grid>", G)),
    (3, 0, 2, group("groupaction", f'{B}<ContentControl x:Name="tagsselector"/>', G)),
], I)

glow = lambda pre, Pre, enabled: rows([(None, f"{pre}glowcolor:pair"), (f"{pre}glowheightlabel", f"{pre}glowheight", f"reset{pre}glowheight:button", f"{pre}glowheightrequired:image")], B, 2)
colors = grid2([
    (0, 0, 1, group("groupBox8", f'{B}<ContentControl x:Name="lightColor"/>\n{B}<ContentControl x:Name="fadeColor"/>\n' + rows([("label13", "desaturation")], B, 0), G)),
    (0, 1, 1, group("groupBox9", "\n".join(f'{B}<ContentControl x:Name="{n}"/>' for n in ["ceilingColor", "upperWallColor", "thingsColor", "lowerWallColor", "floorColor"]), G)),
    (1, 0, 1, group("groupBox10", glow("ceiling", "Ceiling", "ceilingGlowEnabled"), G, title='<CheckBox x:Name="ceilingGlowEnabled" FontWeight="SemiBold"/>')),
    (1, 1, 1, group("groupBox11", glow("floor", "Floor", "floorGlowEnabled"), G, title='<CheckBox x:Name="floorGlowEnabled" FontWeight="SemiBold"/>')),
], I)

def surface(gname, p, P, offs, scale, rs, rslabel, terrain, tlabel, bright, blabel, babs, angle, rot, uselines, rotlabel, refl, reflreset, refllabel, tex, offlabel, scalelabel, lightreset):
    fields = rows([(offlabel, offs + ":pair"), (scalelabel, scale + ":pair"), (rslabel, f"{rs}:combo"), (tlabel, f"{terrain}:combo", f"reset{p}terrain:button"),
                   (blabel, bright, f"{babs}:check", f"{lightreset}:button"), (rotlabel, rot, f"{uselines}:check"), (refllabel, refl, f"{reflreset}:button")], B + "    ", 2)
    body = f'''{B}<Grid ColumnDefinitions="Auto,*,Auto" ColumnSpacing="12">
{B}  <ContentControl x:Name="{angle}" VerticalAlignment="Bottom" Margin="0,0,0,40"/>
{B}  <Border Grid.Column="1">
{fields}
{B}  </Border>
{B}  <ContentControl x:Name="{tex}" Grid.Column="2" VerticalAlignment="Top"/>
{B}</Grid>'''
    return group(gname, body, G)

surfaces = grid2([
    (0, 0, 1, surface("groupBox1", "ceil", "Ceil", "ceilOffsets", "ceilScale", "ceilRenderStyle", "labelceilrenderstyle", "ceilterrain", "label7", "ceilBrightness", "labelLightFront", "ceilLightAbsolute", "ceilAngleControl", "ceilRotation", "cbUseCeilLineAngles", "label1", "ceiling_reflect", "reset_ceiling_reflect", "label20", "ceilingtex", "labelCeilOffsets", "labelCeilScale", "resetceillight")),
    (1, 0, 1, surface("groupBox2", "floor", "Floor", "floorOffsets", "floorScale", "floorRenderStyle", "labelfloorrenderstyle", "floorterrain", "label3", "floorBrightness", "label12", "floorLightAbsolute", "floorAngleControl", "floorRotation", "cbUseFloorLineAngles", "label11", "floor_reflect", "reset_floor_reflect", "label23", "floortex", "labelFloorOffsets", "labelFloorScale", "resetfloorlight")),
], I, "*")

portal = lambda g, p, flags: group(g, rows([(f"{p}portalrenderstylelabel", f"{p}portalrenderstyle:combo"), ("label21" if p == "ceil" else "label22", f"alpha{p if p=='floor' else 'ceiling'}", f"resetalpha{p if p=='floor' else 'ceiling'}:button")], B, 1) + f'\n{B}<ContentControl x:Name="{flags}"/>', G)
slopes = grid2([
    (0, 0, 1, group("gbCeilingSLope", f'{B}<ContentControl x:Name="ceilingslopecontrol"/>', G)),
    (0, 1, 1, portal("groupBox6", "ceil", "ceilportalflags")),
    (1, 0, 1, group("gbFloorSlope", f'{B}<ContentControl x:Name="floorslopecontrol"/>', G)),
    (1, 1, 1, portal("groupBox7", "floor", "floorportalflags")),
], I)

tabs = "\n".join([
    tab("tabproperties", props), tab("tabColors", colors), tab("tabSurfaces", surfaces), tab("tabslopes", slopes),
    plaintab("tabcomment", '        <ContentControl x:Name="commenteditor"/>'),
    plaintab("tabcustom", '        <ContentControl x:Name="fieldslist"/>'),
])
open(sys.argv[1], "w").write(window("SectorEditFormUDMFView", "Edit Sector", 860, 720, tabs))
