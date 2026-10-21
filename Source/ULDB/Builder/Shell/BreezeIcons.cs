// ULDB: vector icons for the main window's toolbars, in the style of KDE's Breeze icons:
// a 16×16 grid, 1 px strokes, monochrome in the theme's text colour, with Breeze's accent blue
// (and red/green/orange where the meaning calls for it) as highlights. Icons are keyed by the
// toolbar item's Tag: upstream's action name ("builder_savemap") or, for edit modes, the mode's
// title ("Vertices Mode"), or by the upstream resource image they show. Items without an icon keep upstream's bitmap.
//
// Spec format, one layer per ';': "<s|f><colour>[opacity] <path data>", s = stroke, f = fill,
// colours: i = ink (text colour), a = accent, r = red, g = green, o = orange, y = yellow, w = white.
// Upstream resource images are keyed "res:<name>" (ResourceImageNames), some as aliases of action icons;
// "res:<assembly>/<name>" picks a plugin's own picture where its name means something else in the core.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace ULDB.Shell
{
	internal static class BreezeIcons
	{
		// Circle helper for specs: C(x, y, r) → path data.
		private static string C(double x, double y, double r) => string.Format(CultureInfo.InvariantCulture,
			"M{0},{1} A{2},{2} 0 1 0 {3},{1} A{2},{2} 0 1 0 {0},{1} Z", x - r, y, r, x + r);

		private const string Doc = "M3.5,1.5 H9.5 L12.5,4.5 V14.5 H3.5 Z M9.5,1.5 V4.5 H12.5";
		private const string Poly = "M3.5,13.5 L2.5,5.5 L8,2.5 L13.5,6 L12,13.5 Z";
		private const string Cube = "M2.5,5 L8,2.5 L13.5,5 V11 L8,13.5 L2.5,11 Z M2.5,5 L8,7.5 L13.5,5 M8,7.5 V13.5";
		private const string Gear = "M12.7,7.0 L14.4,7.2 L14.4,8.8 L12.7,9.0 L12.1,10.6 L13.2,12.0 L12.0,13.2 L10.6,12.1 L9.0,12.7 L8.8,14.4 L7.2,14.4 L7.0,12.7 L5.4,12.1 L4.0,13.2 L2.8,12.0 L3.9,10.6 L3.3,9.0 L1.6,8.8 L1.6,7.2 L3.3,7.0 L3.9,5.4 L2.8,4.0 L4.0,2.8 L5.4,3.9 L7.0,3.3 L7.2,1.6 L8.8,1.6 L9.0,3.3 L10.6,3.9 L12.0,2.8 L13.2,4.0 L12.1,5.4 Z";
		private const string Bulb = "M8,1.5 A4.5,4.5 0 0 1 10.5,9.8 V12 H5.5 V9.8 A4.5,4.5 0 0 1 8,1.5 Z M6,14.5 H10";
		private const string BulbGlow = "M8,2.5 A3.5,3.5 0 0 1 9.5,9 H6.5 A3.5,3.5 0 0 1 8,2.5 Z";
		private const string Folder = "M1.5,13.5 V2.5 H6 L7.5,4 H14.5 V13.5 Z M1.5,6.5 H14.5";
		private const string Eye = "M1.5,8 Q8,1 14.5,8 Q8,15 1.5,8 Z";
		private const string Group = "M3.5,3.5 H7 V7 H3.5 Z M6,6 H9.5 V9.5 H6 Z";
		private const string Paste = "M5.5,3.5 H3.5 V14.5 H12.5 V3.5 H10.5 M5.5,2.5 H10.5 V4.5 H5.5 Z";

		private static readonly Dictionary<string, string> specs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			// File
			["builder_newmap"] = "si " + Doc + ";sa M11.5,9 V15 M8.5,12 H14.5",
			["builder_openmap"] = "si M1.5,13.5 V3.5 H6 L7.5,5 H13.5 V7.5 M1.5,13.5 L4,7.5 H15 L12.5,13.5 Z",
			["builder_savemap"] = "si M2.5,2.5 H11.5 L13.5,4.5 V13.5 H2.5 Z M5.5,2.5 V6.5 H10.5 V2.5;sa M4.5,13.5 V9.5 H11.5 V13.5",
			["builder_openscripteditor"] = "si " + Doc + ";sa M6.5,7.5 L5,9.5 L6.5,11.5 M9.5,7.5 L11,9.5 L9.5,11.5",

			// Edit
			["builder_undo"] = "si M5.5,3.5 L2.5,6.5 L5.5,9.5 M3,6.5 H10 A3.5,3.5 0 0 1 10,13.5 H7",
			["builder_redo"] = "si M10.5,3.5 L13.5,6.5 L10.5,9.5 M13,6.5 H6 A3.5,3.5 0 0 0 6,13.5 H9",
			["builder_cutselection"] = "si " + C(4.5, 12, 2) + " " + C(11.5, 12, 2) + " M5.8,10.4 L11,1.5 M10.2,10.4 L5,1.5",
			["builder_copyselection"] = "si M2.5,11.5 V1.5 H10.5 V3.5;si M5.5,4.5 H13.5 V14.5 H5.5 Z;sa M7.5,8 H11.5 M7.5,11 H11.5",
			["builder_pasteselection"] = "si M5.5,3.5 H3.5 V14.5 H12.5 V3.5 H10.5 M5.5,2.5 H10.5 V4.5 H5.5 Z;sa M6,8.5 H10 M6,11.5 H10",
			["builder_insertprefabfile"] = "si " + Cube,
			["builder_insertpreviousprefab"] = "si " + Cube + ";fa.6 M2.5,5 L8,2.5 L13.5,5 L8,7.5 Z",

			// Things and linedefs
			["builder_thingsfilterssetup"] = "si M1.5,2.5 H14.5 L9.5,8.5 V14 L6.5,12.5 V8.5 Z",
			["thingfilters"] = "si M1.5,2.5 H14.5 L9.5,8.5 V14 L6.5,12.5 V8.5 Z;fa.5 M1.5,2.5 H14.5 L9.5,8.5 H6.5 Z",
			["builder_linedefcolorssetup"] = "sr M1.5,13.5 L6.5,2.5;sg M5.5,13.5 L10.5,2.5;sa M9.5,13.5 L14.5,2.5",

			// View
			["builder_togglebrightness"] = "si " + C(8, 8, 3) + " M8,0.5 V2.5 M8,13.5 V15.5 M0.5,8 H2.5 M13.5,8 H15.5 M2.7,2.7 L4.1,4.1 M11.9,11.9 L13.3,13.3 M2.7,13.3 L4.1,11.9 M11.9,4.1 L13.3,2.7;fy.8 " + C(8, 8, 2.5),
			["builder_togglegrid"] = "si M1.5,1.5 H14.5 V14.5 H1.5 Z M5.8,1.5 V14.5 M10.2,1.5 V14.5 M1.5,5.8 H14.5 M1.5,10.2 H14.5",
			["builder_togglecomments"] = "si M2.5,2.5 H13.5 V10.5 H7.5 L4.5,13.5 V10.5 H2.5 Z;sa M5,5.5 H11 M5,7.5 H9",
			["builder_togglefixedthingsscale"] = "si " + C(8, 8, 3.5) + " M8,8 L10.5,5.5;sa M1,1.5 V4 M15,1.5 V4 M1,2.75 H4 M12,2.75 H15",
			["builder_viewmodenormal"] = "si " + Poly,
			["builder_viewmodebrightness"] = "si " + Poly + ";fi.25 M3.5,13.5 L2.5,5.5 L8,2.5 L8,13.5 Z;fi.55 M8,2.5 L13.5,6 L12,13.5 H8 Z",
			["builder_viewmodefloors"] = "si " + Poly + ";fa.45 " + Poly + ";sa.9 M5,10 L11,10 M4.5,7.5 L12,7.5",
			["builder_viewmodeceilings"] = "si " + Poly + ";fo.45 " + Poly + ";so.9 M5,10 L11,10 M4.5,7.5 L12,7.5",

			// Menu-only items (issue #18)
			["builder_openmapincurrentwad"] = "si " + Folder + ";sa M5.5,10 H10.5 M8,7.5 V12.5",
			["builder_closemap"] = "si " + Doc + ";sr M6,8 L10,12 M10,8 L6,12",
			["builder_savemapas"] = "si M2.5,2.5 H11.5 L13.5,4.5 V13.5 H2.5 Z M5.5,2.5 V6.5 H10.5 V2.5;sa M5,13.5 V10 H8;sa M8.5,14.5 L14.5,8.5 L15.5,9.5 L9.5,15.5 L8,15.5 Z",
			["builder_savemapinto"] = "si M2.5,2.5 H11.5 L13.5,4.5 V13.5 H2.5 Z M5.5,2.5 V6.5 H10.5 V2.5;sa M8,8 V14 M5.5,11.5 L8,14 L10.5,11.5",
			["itemimport"] = "si " + Doc + ";sa M8,6 V12 M5.5,9.5 L8,12 L10.5,9.5",
			["itemexport"] = "si " + Doc + ";sa M8,12 V6 M5.5,8.5 L8,6 L10.5,8.5",
			["itemexit"] = "si M6.5,2.5 H2.5 V13.5 H6.5;sr M6.5,8 H14 M11.5,5.5 L14,8 L11.5,10.5",
			["buildermodes_exporttoobj"] = "si " + Cube + ";sa M12,14.5 V9.5 M9.8,11.7 L12,9.5 L14.2,11.7",
			["buildermodes_exporttoimage"] = "si M1.5,2.5 H14.5 V13.5 H1.5 Z;sa M2,12.5 L6,7.5 L9,10.5 L11,8.5 L14,12.5;fy " + C(11.5, 5.5, 1.3),
			["buildermodes_exporttoidstudio"] = "si " + Poly + ";sa M12,14.5 V9.5 M9.8,11.7 L12,9.5 L14.2,11.7",
			["builder_aligngridtolinedef"] = "si M1.5,1.5 H14.5 V14.5 H1.5 Z M1.5,8 H14.5 M8,1.5 V14.5;sa M2.5,13.5 L13.5,2.5",
			["builder_setgridorigintovertex"] = "si M1.5,8 H14.5 M8,1.5 V14.5;fa " + C(8, 8, 2.5),
			["builder_resetgrid"] = "si M1.5,1.5 H14.5 V14.5 H1.5 Z M5.8,1.5 V14.5 M10.2,1.5 V14.5 M1.5,5.8 H14.5 M1.5,10.2 H14.5;sr M5,4 L3,6 L5,8",
			["builder_togglealwaysshowvertices"] = "si M3.5,12.5 L8,3.5 L12.5,12.5;fa M2,11 H5 V14 H2 Z M6.5,2 H9.5 V5 H6.5 Z M11,11 H14 V14 H11 Z",
			["builder_centeroncoordinates"] = "si " + C(8, 8, 4.5) + " M8,1 V4.5 M8,11.5 V15 M1,8 H4.5 M11.5,8 H15;fa " + C(8, 8, 1.2),
			["builder_centerinscreen"] = "si M2.5,5.5 V2.5 H5.5 M10.5,2.5 H13.5 V5.5 M13.5,10.5 V13.5 H10.5 M5.5,13.5 H2.5 V10.5;sa M5.5,5.5 H10.5 V10.5 H5.5 Z",
			["builder_toggleinfopanel"] = "si M1.5,2.5 H14.5 V13.5 H1.5 Z;fa.5 M1.5,9.5 H14.5 V13.5 H1.5 Z;si M1.5,9.5 H14.5",
			["builder_createprefab"] = "si " + Cube + ";sa M11.5,9 V15 M8.5,12 H14.5",
			["buildermodes_selectsinglesided"] = "si M2.5,13.5 L13.5,2.5;sa M5.5,8 L8,10.5 M8,5.5 L10.5,8 M3,11 L5.5,13.5",
			["buildermodes_selectdoublesided"] = "si M2.5,13.5 L13.5,2.5;sa M5.5,8 L8,10.5 M8,5.5 L10.5,8 M3,11 L5.5,13.5 M8,5.5 L5.5,3 M10.5,8 L13,10.5 M5.5,8 L3,5.5",
			["buildermodes_splitlinedefs"] = "si M2.5,13.5 L6.5,9.5 M9.5,6.5 L13.5,2.5;fa " + C(8, 8, 2),
			["buildermodes_changemapelementindex"] = "si M5.5,2.5 L4.5,13.5 M11.5,2.5 L10.5,13.5 M2.5,6 H13.5 M2.5,10 H13.5",
			["buildermodes_applylightfogflag"] = "si M4,11.5 A2.5,2.5 0 0 1 4.5,6.5 A3.5,3.5 0 0 1 11.5,6 A2.8,2.8 0 0 1 12,11.5 Z;sa M3,14 H8 M7,14.5 H13",
			["aligntexturesitem"] = "si M2.5,2.5 H13.5 V13.5 H2.5 Z M2.5,8 H13.5 M8,2.5 V13.5;sa M5,5 H11",
			["buildermodes_alignfloortofront"] = "si M2.5,2.5 H13.5 V13.5 H2.5 Z M2.5,8 H13.5;fa.5 M2.5,8 H13.5 V13.5 H2.5 Z;sa M7,5 L4.5,5 M6,4 L4.5,5 L6,6",
			["buildermodes_alignfloortoback"] = "si M2.5,2.5 H13.5 V13.5 H2.5 Z M2.5,8 H13.5;fa.5 M2.5,8 H13.5 V13.5 H2.5 Z;sa M9,5 L11.5,5 M10,4 L11.5,5 L10,6",
			["buildermodes_alignceilingtofront"] = "si M2.5,2.5 H13.5 V13.5 H2.5 Z M2.5,8 H13.5;fo.5 M2.5,2.5 H13.5 V8 H2.5 Z;sa M7,11 L4.5,11 M6,10 L4.5,11 L6,12",
			["buildermodes_alignceilingtoback"] = "si M2.5,2.5 H13.5 V13.5 H2.5 Z M2.5,8 H13.5;fo.5 M2.5,2.5 H13.5 V8 H2.5 Z;sa M9,11 L11.5,11 M10,10 L11.5,11 L10,12",

			// Geometry merging and snapping
			["builder_geomergeclassic"] = "si M1.5,12.5 L6,6 M14.5,12.5 L10,6;fa " + C(8, 5, 2),
			["builder_geomerge"] = "si M1.5,5.5 H8.5 V12.5 H1.5 Z M7.5,3.5 H14.5 V10.5 H7.5 Z;fa.6 M7.5,5.5 H8.5 V10.5 H7.5 Z",
			["builder_georeplace"] = "si M1.5,5.5 H8.5 V12.5 H1.5 Z;sa M7.5,3.5 H14.5 V10.5 H7.5 Z",
			["builder_togglesnap"] = "si M3.5,2.5 V8.5 A4.5,4.5 0 0 0 12.5,8.5 V2.5 H9.5 V8.5 A1.5,1.5 0 0 1 6.5,8.5 V2.5 Z;fr M3.5,2.5 H6.5 V5 H3.5 Z M9.5,2.5 H12.5 V5 H9.5 Z",
			["builder_toggledynamicgrid"] = "si M1.5,1.5 H14.5 V14.5 H1.5 Z M8,1.5 V14.5 M1.5,8 H14.5 M4.75,8 V14.5 M1.5,11.25 H8;sa M10,6 L13,3 M10.5,3 H13 V5.5",
			["builder_toggleautomerge"] = "si M2.5,13.5 L8,3.5 L13.5,13.5;fa " + C(8, 3.5, 2) + ";sa M10.5,1.5 L12.5,1.5 M11.5,0.5 V2.5",
			["builder_togglejoinedsectorssplitting"] = "si M1.5,3.5 H14.5 V12.5 H1.5 Z;sa M8,1 V15",
			["builder_toggleautoclearsidetextures"] = "si M2.5,10.5 L8.5,4.5 L12.5,8.5 L8,13 H5 Z M5,13 H14;fa.6 M8.5,4.5 L12.5,8.5 L10.5,10.5 L6.5,6.5 Z",

			// Visual mode rendering
			["builder_gztogglelights"] = "si M8,1.5 A4.5,4.5 0 0 1 10.5,9.8 V12 H5.5 V9.8 A4.5,4.5 0 0 1 8,1.5 Z M6,14.5 H10;fy.7 M8,2.5 A3.5,3.5 0 0 1 9.5,9 H6.5 A3.5,3.5 0 0 1 8,2.5 Z",
			["builder_gztogglemodels"] = "si " + Cube + ";fa.35 M8,7.5 L13.5,5 V11 L8,13.5 Z",
			["builder_gztogglefog"] = "si M1.5,5 Q4,3 6.5,5 T11.5,5 T15,4.5 M1.5,8.5 Q4,6.5 6.5,8.5 T11.5,8.5 T15,8 M1.5,12 Q4,10 6.5,12 T11.5,12 T15,11.5",
			["builder_gztogglesky"] = "si M4.5,12.5 A3,3 0 0 1 4.5,6.5 A4,4 0 0 1 12,6 A3.25,3.25 0 0 1 12,12.5 Z;fa.35 M4.5,12.5 A3,3 0 0 1 4.5,6.5 A4,4 0 0 1 12,6 A3.25,3.25 0 0 1 12,12.5 Z",
			["builder_toggleclassicrendering"] = "fi M2,2 H6 V6 H2 Z M10,2 H14 V6 H10 Z M6,6 H10 V10 H6 Z M2,10 H6 V14 H2 Z M10,10 H14 V14 H10 Z;fa.6 M6,2 H10 V6 H6 Z M2,6 H6 V10 H2 Z",
			["builder_gztoggleeventlines"] = "fi " + C(3, 12.5, 1.75) + " " + C(13, 3.5, 1.75) + ";sa M4.5,11.5 L6.5,10 M8,9 L10,7.5 M11.5,6.5 L12,6",
			["builder_gztogglevisualvertices"] = "si M2.5,13.5 L8,2.5 L13.5,13.5 Z;fa " + C(2.5, 13.5, 1.75) + " " + C(8, 2.5, 1.75) + " " + C(13.5, 13.5, 1.75),
			["builder_testmap"] = "fg M4,2 L14,8 L4,14 Z",

			// Edit modes (by title)
			["Draw Lines Mode"] = "si M1.5,14.5 L6,10 L10,12 L14.5,6.5;fa " + C(6, 10, 1.5) + " " + C(10, 12, 1.5) + ";si M10.5,1.5 L14.5,5.5 L8.5,11.5 H4.5 V7.5 Z",
			["Draw Curve Mode"] = "si M1.5,13.5 C4,1 12,1 14.5,13.5;fa " + C(1.5, 13.5, 1.5) + " " + C(14.5, 13.5, 1.5) + " " + C(8, 4.2, 1.5),
			["Draw Rectangle Mode"] = "si M2.5,3.5 H13.5 V12.5 H2.5 Z;fa " + C(2.5, 3.5, 1.5) + " " + C(13.5, 12.5, 1.5),
			["Draw Ellipse Mode"] = "si M1.5,8 A6.5,4.5 0 1 0 14.5,8 A6.5,4.5 0 1 0 1.5,8 Z;fa " + C(14.5, 8, 1.5),
			["Draw Grid Mode"] = "si M1.5,1.5 H14.5 V14.5 H1.5 Z;sa M5.8,1.5 V14.5 M10.2,1.5 V14.5 M1.5,5.8 H14.5 M1.5,10.2 H14.5",
			["Vertices Mode"] = "si.6 " + Poly + ";fa " + C(3.5, 13.5, 1.75) + " " + C(2.5, 5.5, 1.75) + " " + C(8, 2.5, 1.75) + " " + C(13.5, 6, 1.75) + " " + C(12, 13.5, 1.75),
			["Linedefs Mode"] = "si.6 " + Poly + ";sa M2.5,5.5 L8,2.5 L13.5,6;fi " + C(2.5, 5.5, 1.25) + " " + C(13.5, 6, 1.25),
			["Sectors Mode"] = "si " + Poly + ";fa.6 " + Poly,
			["Things Mode"] = "si " + C(8, 8, 5.5) + ";fa.6 " + C(8, 8, 5.5) + ";si M8,8 L12.5,8 M10.5,6 L12.5,8 L10.5,10",
			["Make Sectors Mode"] = "si " + Poly + ";sa M8,5.5 V11.5 M5,8.5 H11",
			["Floor Align Mode"] = "si M1.5,9.5 L8,6.5 L14.5,9.5 L8,12.5 Z;fa.45 M1.5,9.5 L8,6.5 L14.5,9.5 L8,12.5 Z;sa M8,1.5 V5 M6.5,3.5 L8,5 L9.5,3.5",
			["Ceiling Align Mode"] = "si M1.5,6.5 L8,3.5 L14.5,6.5 L8,9.5 Z;fo.45 M1.5,6.5 L8,3.5 L14.5,6.5 L8,9.5 Z;sa M8,14.5 V11 M6.5,12.5 L8,11 L9.5,12.5",
			["Visual Mode"] = "si M1.5,2.5 L5.5,5.5 H10.5 L14.5,2.5 M1.5,13.5 L5.5,10.5 H10.5 L14.5,13.5 M5.5,5.5 V10.5 M10.5,5.5 V10.5;fa.45 M5.5,5.5 H10.5 V10.5 H5.5 Z",
			["Edit Selection Mode"] = "si M3.5,3.5 H12.5 V12.5 H3.5 Z;fa M1.5,1.5 H5.5 V5.5 H1.5 Z M10.5,1.5 H14.5 V5.5 H10.5 Z M1.5,10.5 H5.5 V14.5 H1.5 Z M10.5,10.5 H14.5 V14.5 H10.5 Z",
			["Bridge Mode"] = "si M1.5,6.5 H14.5 M1.5,6.5 V13.5 M14.5,6.5 V13.5 M4.5,13.5 V10.5 A3.5,3.5 0 0 1 11.5,10.5 V13.5;sa M1.5,4 H14.5",
			["Find and Replace Mode"] = "si " + C(6.5, 6.5, 4.5) + " M10,10 L14.5,14.5;fa.4 " + C(6.5, 6.5, 3.5),
			["Map Analysis Mode"] = "si " + C(8, 8, 6.5) + ";sg M4.5,8.5 L7,11 L11.5,5.5",

			// BuilderModes: mode buttons and menu items (tags get the plugin's "buildermodes_" prefix)
			["buildermodes_thingaligntowall"] = "si M13.5,1.5 V14.5;si " + C(6, 8, 3.5) + ";sa M9.5,8 H13",
			["buildermodes_makedoor"] = "si M3.5,14.5 V1.5 H12.5 V14.5 M1.5,14.5 H14.5;fa.5 M5.5,3.5 H10.5 V14.5 H5.5 Z;fi " + C(9.25, 9, 0.75),
			["buttonMarqueSelectTouching"] = "si M2.5,2.5 H13.5 V13.5 H2.5 Z;sa M6,7 L10,11 M10,7 L6,11",
			["buttonTextureOffsetLock"] = "si M4.5,7.5 H11.5 V14.5 H4.5 Z M5.5,7.5 V5 A2.5,2.5 0 0 1 10.5,5 V7.5;fa " + C(8, 11, 1.25),
			["buttonTextureOffset3DFloorLock"] = "si M4.5,8.5 H11.5 V14.5 H4.5 Z M5.5,8.5 V6 A2.5,2.5 0 0 1 10.5,6 V8.5;sa M1.5,3.5 L8,0.5 L14.5,3.5",
			["buildermodes_gradientbrightness"] = "fi.2 M1.5,10 H4.5 V14.5 H1.5 Z;fi.45 M5,10 H8 V14.5 H5 Z;fi.7 M8.5,10 H11.5 V14.5 H8.5 Z;fi M12,10 H15 V14.5 H12 Z;fy " + C(8, 4.5, 2.5),
			["buildermodes_gradientfloors"] = "si M1.5,14.5 V11.5 H5 V8.5 H8.5 V5.5 H12 V2.5 H14.5;fa.5 M1.5,14.5 V11.5 H5 V8.5 H8.5 V5.5 H12 V2.5 H14.5 V14.5 Z",
			["buildermodes_gradientceilings"] = "si M1.5,1.5 V4.5 H5 V7.5 H8.5 V10.5 H12 V13.5 H14.5;fo.5 M1.5,1.5 V4.5 H5 V7.5 H8.5 V10.5 H12 V13.5 H14.5 V1.5 Z",
			["buildermodes_curvelinesmode"] = "si.5 M1.5,12.5 H14.5;si M1.5,12.5 Q8,0 14.5,12.5;fa " + C(1.5, 12.5, 1.5) + " " + C(14.5, 12.5, 1.5),
			["buildermodes_flipselectionh"] = "sa M8,1 V15;si M6,3.5 L1.5,8 L6,12.5 Z M10,3.5 L14.5,8 L10,12.5 Z",
			["buildermodes_flipselectionv"] = "sa M1,8 H15;si M3.5,6 L8,1.5 L12.5,6 Z M3.5,10 L8,14.5 L12.5,10 Z",
			["buttonlightradii"] = "si.5 " + C(8, 8, 7) + ";si M8,3.5 A3,3 0 0 1 9.5,9 V11 H6.5 V9 A3,3 0 0 1 8,3.5 Z M7,12.5 H9;fy.7 M8,4.5 A2,2 0 0 1 9,8.5 H7 A2,2 0 0 1 8,4.5 Z",
			["buttonsoundradii"] = "si M1.5,6 H4 L7.5,3 V13 L4,10 H1.5 Z;sa M10,5.5 A3.5,3.5 0 0 1 10,10.5 M12,3.5 A6,6 0 0 1 12,12.5",
			["buttonselectioneffects"] = "si M1.5,3.5 H9.5 L14.5,8 L9.5,12.5 H1.5 Z;fa " + C(4.5, 8, 1.25),
			["buttonselectionnumbers"] = "si M5,2 L4,14 M10,2 L9,14 M2,5.5 H13.5 M1.5,10.5 H13;fa.5 M4.6,5.5 H9.6 L9.2,10.5 H4.2 Z",
			["buildermodes_syncedthingedit"] = "si " + C(5, 10, 3) + " " + C(11, 6, 3) + ";sa M7,8 L9,8",
			["buildermodes_alignlinedefs"] = "si M1.5,4.5 H14.5 M1.5,11.5 H14.5;sa M8,6 V10 M6.5,8.5 L8,10 L9.5,8.5",
			["buildermodes_fliplinedefs"] = "si M2.5,13.5 L13.5,2.5;sa M5,3.5 H9 M7,1.5 L9,3.5 L7,5.5 M11,12.5 H7 M9,10.5 L7,12.5 L9,14.5",
			["buildermodes_flipsidedefs"] = "si M8,1.5 V14.5;fa.6 M2.5,4.5 H6.5 V11.5 H2.5 Z;fo.6 M9.5,4.5 H13.5 V11.5 H9.5 Z",
			["buildermodes_joinsectors"] = "si M1.5,3.5 H14.5 V12.5 H1.5 Z;sa.6 M8,3.5 V12.5;fa.4 M1.5,3.5 H14.5 V12.5 H1.5 Z",
			["buildermodes_mergesectors"] = "si M1.5,3.5 H14.5 V12.5 H1.5 Z;fa.6 M1.5,3.5 H14.5 V12.5 H1.5 Z",
			["buildermodes_placethings"] = "si " + C(5, 11, 2.5) + " " + C(11, 11, 2.5) + ";sa M8,1.5 V6.5 M5.5,4 H10.5",
			["buildermodes_selectsimilar"] = "si M1.5,1.5 H6.5 V6.5 H1.5 Z M9.5,9.5 H14.5 V14.5 H9.5 Z;fa.6 M9.5,1.5 H14.5 V6.5 H9.5 Z M1.5,9.5 H6.5 V14.5 H1.5 Z",
			["buildermodes_thinglookatcursor"] = "si " + C(6, 10, 3.5) + ";sa M8.5,7.5 L14.5,1.5 M10.5,1.5 H14.5 V5.5",
			["buildermodes_thingsselectinsectors"] = "si " + Poly + ";fa " + C(6.5, 8.5, 1.75) + " " + C(10, 9.5, 1.75),
			["buildermodes_filterselectedthings"] = "si M1.5,2.5 H14.5 L9.5,8.5 V14 L6.5,12.5 V8.5 Z;fa " + C(12.5, 12.5, 2),

			// Plugin edit modes (by title)
			["3D Floor Mode"] = "si M1.5,4 L5.5,1.5 H14.5 L10.5,4 Z M1.5,14.5 L5.5,12 H14.5 L10.5,14.5 Z;sa M1.5,9.25 L5.5,6.75 H14.5 L10.5,9.25 Z;fa.45 M1.5,9.25 L5.5,6.75 H14.5 L10.5,9.25 Z",
			["Slope Mode"] = "si M1.5,13.5 L14.5,5.5 V13.5 Z;fa.45 M1.5,13.5 L14.5,5.5 V13.5 Z;fa " + C(1.5, 13.5, 1.5) + " " + C(14.5, 5.5, 1.5),
			["Draw Slopes Mode"] = "si.6 M1.5,14.5 H14.5;sa M1.5,14.5 L9,9.5;fa " + C(1.5, 14.5, 1.5) + " " + C(9, 9.5, 1.5) + ";si M11,1.5 L14.5,5 L10,9.5 H7 V6.5 Z",
			["Stair Sector Builder Mode"] = "si M1.5,14.5 H14.5 V2.5 H11 V6 H7.5 V9.5 H4 V13 H1.5 Z;fa.45 M1.5,14.5 H14.5 V2.5 H11 V6 H7.5 V9.5 H4 V13 H1.5 Z",
			["Sound Propagation Mode"] = "si M1.5,6 H4 L7,3.5 V12.5 L4,10 H1.5 Z;sa M9.5,5.5 A3.5,3.5 0 0 1 9.5,10.5 M11.5,3.5 A6,6 0 0 1 11.5,12.5 M13.5,1.5 A8.5,8.5 0 0 1 13.5,14.5",
			["Sound Environment Mode"] = "si M1.5,1.5 H14.5 V14.5 H1.5 Z M3.5,7 H5.5 L8,5 V11 L5.5,9 H3.5 Z;sa M10,6 A2.5,2.5 0 0 1 10,10 M11.75,4.5 A4.5,4.5 0 0 1 11.75,11.5",

			// Plugin buttons (tags get the plugin's assembly name as prefix)
			["threedfloormode_drawfloorslope"] = "si M1.5,13.5 L14.5,7.5 V13.5 Z;fa.45 M1.5,13.5 L14.5,7.5 V13.5 Z",
			["threedfloormode_drawceilingslope"] = "si M1.5,2.5 H14.5 L1.5,8.5 Z;fo.45 M1.5,2.5 H14.5 L1.5,8.5 Z",
			["threedfloormode_drawfloorandceilingslope"] = "si M1.5,2.5 H14.5 L1.5,7 Z M1.5,13.5 L14.5,9 V13.5 Z;fo.45 M1.5,2.5 H14.5 L1.5,7 Z;fa.45 M1.5,13.5 L14.5,9 V13.5 Z",
			["threedfloormode_relocate3dfloorcontrolsectors"] = "si M1.5,1.5 H6.5 V6.5 H1.5 Z M4,6.5 V12 H8.5;sa M9.5,9.5 H14.5 V14.5 H9.5 Z;fa.45 M9.5,9.5 H14.5 V14.5 H9.5 Z;si M7,10.5 L8.5,12 L7,13.5",

			// BuilderEffects (tags get the plugin's "buildereffects_" prefix)
			["buildereffects_applyjitter"] = "si.6 " + Poly + ";fa " + C(4, 12, 1.5) + " " + C(3.5, 6, 1.5) + " " + C(8.5, 3.5, 1.5) + " " + C(13, 6.5, 1.5) + " " + C(11, 13, 1.5),
			["buildereffects_applydirectionalshading"] = "fi.2 M1.5,9 H5.5 V14.5 H1.5 Z;fi.45 M6,9 H10 V14.5 H6 Z;fi.7 M10.5,9 H14.5 V14.5 H10.5 Z;fy " + C(4, 4, 2) + ";sa M7,4 H14 M11.5,2 L14,4 L11.5,6",
			["buildereffects_importobjasterrain"] = "si M1.5,13.5 L5.5,6.5 L8.5,10.5 L11,7.5 L14.5,13.5 Z;fa.45 M1.5,13.5 L5.5,6.5 L8.5,10.5 L11,7.5 L14.5,13.5 Z;sa M11.5,1.5 V5.5 M9.5,3.5 L11.5,5.5 L13.5,3.5",

			// Core menu items and status bar
			["builder_gridinc"] = "si M1.5,1.5 H14.5 V14.5 H1.5 Z M8,1.5 V14.5 M1.5,8 H14.5",
			["builder_griddec"] = "si M1.5,1.5 H14.5 V14.5 H1.5 Z M4.75,1.5 V14.5 M8,1.5 V14.5 M11.25,1.5 V14.5 M1.5,4.75 H14.5 M1.5,8 H14.5 M1.5,11.25 H14.5",
			["builder_gridsetup"] = "si M1.5,1.5 H14.5 V14.5 H1.5 Z M5.8,1.5 V14.5 M10.2,1.5 V14.5 M1.5,5.8 H14.5 M1.5,10.2 H14.5;fa " + C(12, 12, 3),
			["builder_snapvertstogrid"] = "si.6 M1.5,5.5 H14.5 M1.5,10.5 H14.5 M5.5,1.5 V14.5 M10.5,1.5 V14.5;fa " + C(5.5, 5.5, 1.75) + " " + C(10.5, 10.5, 1.75),
			["buttonzoom"] = "si " + C(6.5, 6.5, 4.5) + " M10,10 L14.5,14.5 M4.5,6.5 H8.5 M6.5,4.5 V8.5",
			["buttongrid"] = "si M1.5,1.5 H14.5 V14.5 H1.5 Z M5.8,1.5 V14.5 M10.2,1.5 V14.5 M1.5,5.8 H14.5 M1.5,10.2 H14.5",
			["status:ready"] = "fg " + C(8, 8, 4.5),
			["status:loading"] = "fa " + C(8, 8, 4.5),
			["status:busy"] = "fo " + C(8, 8, 4.5),
			["status:warning"] = "fo M8,1.5 L15,14 H1 Z;fi M7.25,5.5 H8.75 V10 H7.25 Z M7.25,11 H8.75 V12.5 H7.25 Z",
			["status:ready:dim"] = "fg.35 " + C(8, 8, 4.5),
			["status:loading:dim"] = "fa.35 " + C(8, 8, 4.5),
			["status:busy:dim"] = "fo.35 " + C(8, 8, 4.5),
			["status:warning:dim"] = "fo.35 M8,1.5 L15,14 H1 Z",
			["warns:on"] = "fo M8,1.5 L15,14 H1 Z;fi M7.25,5.5 H8.75 V10 H7.25 Z M7.25,11 H8.75 V12.5 H7.25 Z",
			["warns:off"] = "si.4 M8,1.5 L15,14 H1 Z",

			// Mode-specific buttons
			["buildermodes_classiccopyproperties"] = "si M10.5,1.5 L14.5,5.5 L12.5,7.5 L8.5,3.5 Z M9.5,6.5 L3,13 L2,15 L4,14 L10.5,7.5;fa M2.8,13.2 L4.5,11.5 L5.5,12.5 L3.8,14.2 Z",
			["buildermodes_classicpasteproperties"] = "si M9.5,1.5 H14.5 V5.5 H9.5 Z M12,5.5 V8 L6,10 M6,10 V14.5;fa M4.5,10.5 H7.5 V15 H4.5 Z",
			["buildermodes_classicpastepropertieswithoptions"] = "si M9.5,1.5 H14.5 V5.5 H9.5 Z M12,5.5 V8 L6,10 M6,10 V14.5;fa M4.5,10.5 H7.5 V15 H4.5 Z;fi " + C(11, 12.5, 1) + " " + C(14, 12.5, 1),

			// Upstream resource images ("res:" + Properties.Resources name, see ResourceImageNames): menu items
			// without an action, items whose image shows a state, and dialog buttons
			["res:List"] = "sa M1.5,3.5 H2.5 M1.5,8 H2.5 M1.5,12.5 H2.5;si M5.5,3.5 H14.5 M5.5,8 H14.5 M5.5,12.5 H14.5",
			["res:List_Images"] = "si M1.5,2.5 H6.5 V7.5 H1.5 Z M1.5,9.5 H6.5 V14.5 H1.5 Z M8.5,4 H14.5 M8.5,11 H14.5;sa M8.5,6 H12.5 M8.5,13 H12.5;fa.45 M1.5,2.5 H6.5 V7.5 H1.5 Z M1.5,9.5 H6.5 V14.5 H1.5 Z",
			["res:treeview"] = "si M3.5,4 V12.5 H7.5 M3.5,8 H7.5;fi M1.5,1 H5.5 V4 H1.5 Z;sa M9.5,8 H14.5 M9.5,12.5 H14.5",
			// Linked values (offset/scale pairs): two joined links when linked, apart when not.
			["res:Link"] = "sa M7.5,4.5 H4.5 A3.5,3.5 0 0 0 4.5,11.5 H7.5 M8.5,4.5 H11.5 A3.5,3.5 0 0 1 11.5,11.5 H8.5 M6,8 H10",
			["res:Unlink"] = "si M6.5,4.5 H4.5 A3.5,3.5 0 0 0 4.5,11.5 H6.5 M9.5,4.5 H11.5 A3.5,3.5 0 0 1 11.5,11.5 H9.5",
			// Comment types (the Comment tab's radio buttons)
			["res:CommentRegular"] = "si M2.5,2.5 H13.5 V10.5 H8 L5,13.5 V10.5 H2.5 Z;si M5,5.5 H11 M5,8 H9",
			["res:CommentInfo"] = "sa M2.5,2.5 H13.5 V10.5 H8 L5,13.5 V10.5 H2.5 Z;fa " + C(8, 4.6, 0.9) + ";sa M8,6.6 V8.8",
			["res:CommentQuestion"] = "so M2.5,2.5 H13.5 V10.5 H8 L5,13.5 V10.5 H2.5 Z;so M6.6,5 A1.5,1.5 0 1 1 8.3,6.5 C8,6.8 8,7 8,7.8;fo " + C(8, 9.2, 0.6),
			["res:CommentProblem"] = "sr M2.5,2.5 H13.5 V10.5 H8 L5,13.5 V10.5 H2.5 Z;sr M8,4.3 V7.2;fr " + C(8, 8.9, 0.7),
			["res:CommentSmile"] = "sy M2.5,2.5 H13.5 V10.5 H8 L5,13.5 V10.5 H2.5 Z;fy " + C(6.2, 5.3, 0.7) + " " + C(9.8, 5.3, 0.7) + ";sy M5.8,7.3 Q8,9.2 10.2,7.3",
			["res:Reset"] = "si M4.5,4.5 A5,5 0 1 1 3,8 M1.5,4.5 H4.5 V1.5",
			["res:Reload"] = "si M13.5,8 A5.5,5.5 0 0 1 3.5,11 M2.5,8 A5.5,5.5 0 0 1 12.5,5;sa M12.5,1.5 V5 H9 M3.5,14.5 V11 H7",
			["res:Repeat"] = "si M2.5,9 V6.5 A2,2 0 0 1 4.5,4.5 H13 M13.5,7 V9.5 A2,2 0 0 1 11.5,11.5 H3;sa M11,2.5 L13,4.5 L11,6.5 M5,9.5 L3,11.5 L5,13.5",
			["res:SearchClear"] = "sr M3.5,3.5 L12.5,12.5 M12.5,3.5 L3.5,12.5",
			["res:Close"] = "si M3.5,3.5 L12.5,12.5 M12.5,3.5 L3.5,12.5",
			["res:Clear"] = "si M2.5,10.5 L8.5,4.5 L12.5,8.5 L8,13 H5 Z M9,14.5 H14.5;fr.6 M8.5,4.5 L12.5,8.5 L10.5,10.5 L6.5,6.5 Z",
			["res:Failed"] = "fr " + C(8, 8, 6.5) + ";sw M5.5,5.5 L10.5,10.5 M10.5,5.5 L5.5,10.5",
			["res:Check"] = "sa M2.5,8.5 L6,12 L13.5,4",
			["res:Add"] = "sg M8,2.5 V13.5 M2.5,8 H13.5",
			["res:Folder"] = "si " + Folder + ";fa.35 M1.5,6.5 H14.5 V13.5 H1.5 Z",
			["res:FolderExplore"] = "si M1.5,13.5 V2.5 H6 L7.5,4 H14.5 V7.5 M1.5,6.5 H8 M1.5,13.5 H7.5;sa " + C(11, 11, 2.5) + " M12.8,12.8 L15,15",
			["res:File"] = "si " + Doc,
			["res:Text"] = "si " + Doc + ";sa M5.5,7.5 H10.5 M5.5,10 H10.5 M5.5,12.5 H8.5",
			["res:Search"] = "si " + C(6.5, 6.5, 4.5) + " M10,10 L14.5,14.5",
			["res:SearchMatch"] = "si " + C(10, 6, 3.5) + " M12.5,8.5 L15,11;sa M1.5,6.5 H5 M1.5,9.5 H7 M1.5,12.5 H10",
			["res:SearchMatchCase"] = "si M1.5,13.5 L5,2.5 L8.5,13.5 M2.8,9.5 H7.2;sa " + C(12, 11, 2.5) + " M14.5,8.5 V13.5",
			["res:SearchPrev"] = "si M1.5,3 V13;sa M14.5,8 H4 M7.5,4.5 L4,8 L7.5,11.5",
			["res:SearchNext"] = "si M14.5,3 V13;sa M1.5,8 H12 M8.5,4.5 L12,8 L8.5,11.5",
			["res:Replace"] = "si M1.5,2.5 H7.5 V8.5 H1.5 Z;sa M8.5,7.5 H14.5 V13.5 H8.5 Z;si M10,2.5 A3.5,3.5 0 0 1 13.5,6 M12,4.5 L13.5,6 L15,4.5",
			["res:WordWrap"] = "si M1.5,3.5 H14.5 M1.5,7.5 H12 A2.5,2.5 0 0 1 12,12.5 H8.5 M1.5,11.5 H5.5;sa M10,11 L8.5,12.5 L10,14",
			["res:TextIndent"] = "si M7.5,3.5 H14.5 M7.5,6.5 H14.5 M7.5,9.5 H14.5 M1.5,12.5 H14.5;sa M1.5,4.5 L4,6.5 L1.5,8.5",
			["res:TextUnindent"] = "si M7.5,3.5 H14.5 M7.5,6.5 H14.5 M7.5,9.5 H14.5 M1.5,12.5 H14.5;sa M4,4.5 L1.5,6.5 L4,8.5",
			["res:TextWhitespace"] = "si M9.5,2.5 V14.5 M12.5,2.5 V14.5 M9.5,2.5 H14;fi M9.5,2.5 H7.5 A3,3 0 0 0 7.5,8.5 H9.5 Z",
			["res:ScriptCompile"] = "si " + Doc + ";fo M8.5,5.5 L5.5,10 H8 L7,13.5 L10.5,8.5 H8 Z",
			["res:SaveAll"] = "si M2.5,12 V2.5 H10;si M4.5,4.5 H11.5 L13.5,6.5 V14.5 H4.5 Z M7,4.5 V7.5 H11 V4.5;sa M6.5,14.5 V11 H11.5 V14.5",
			["res:PasteSpecial"] = "si " + Paste + ";fo " + C(11.5, 11.5, 2.5),
			["res:Lightbulb"] = "si " + Bulb,
			["res:LightDisabled"] = "si.45 " + Bulb + ";sr M2.5,13.5 L13.5,2.5",
			["res:Light_animate"] = "si " + Bulb + ";fy.7 " + BulbGlow + ";fg M11,10.5 L15.5,13 L11,15.5 Z",
			["res:ModelDisabled"] = "si.45 " + Cube + ";sr M2.5,13.5 L13.5,2.5",
			["res:Model_selected"] = "so " + Cube + ";fo.45 M8,7.5 L13.5,5 V11 L8,13.5 Z",
			["res:ModelFiltered"] = "si " + Cube + ";fa M8.5,9 H15.5 L13,11.75 V15.5 L11,14.5 V11.75 Z",
			["res:Guidelines"] = "sa.7 M8,1.5 V14.5 M1.5,8 H14.5;fi " + C(8, 8, 1.5),
			["res:Angle"] = "si M1.5,13.5 H14.5 M1.5,13.5 L11,4;sa M7.5,13.5 A6,6 0 0 0 5.8,9.3",
			["res:Radial"] = "si M5.5,5.5 L2.5,2.5 M10.5,5.5 L13.5,2.5 M5.5,10.5 L2.5,13.5 M10.5,10.5 L13.5,13.5;sa M2.5,5.5 V2.5 H5.5 M10.5,2.5 H13.5 V5.5 M2.5,10.5 V13.5 H5.5 M10.5,13.5 H13.5 V10.5;fi " + C(8, 8, 1.5),
			["res:QuarterCircleTopLeft"] = "si M2.5,14 A11.5,11.5 0 0 1 14,2.5",
			["res:QuarterCircleTopRight"] = "si M2,2.5 A11.5,11.5 0 0 1 13.5,14",
			["res:HalfCircle"] = "si M1.5,13 A6.5,6.5 0 0 1 14.5,13",
			["res:Question"] = "sa " + C(8, 8, 6.5) + ";si M6,6 A2,2 0 1 1 8,8 V9.5;fi " + C(8, 11.75, 0.75),
			["res:Help"] = "si M2.5,2.5 H6.5 A1.5,1.5 0 0 1 8,4 A1.5,1.5 0 0 1 9.5,2.5 H13.5 V12.5 H9.5 A1.5,1.5 0 0 0 8,14 A1.5,1.5 0 0 0 6.5,12.5 H2.5 Z M8,4 V14;sa M4.5,5.5 H6 M10,5.5 H11.5",
			["res:About"] = "sa " + C(8, 8, 6.5) + ";si M8,7 V11.5;fi " + C(8, 4.75, 0.75),
			["res:Update"] = "sa " + C(8, 8, 6.5) + ";si M8,4 V11.5 M5,8.5 L8,11.5 L11,8.5",
			["res:Github"] = "si M4.5,4 V12 M4.5,10 C4.5,7.5 11.5,8.5 11.5,6;fi " + C(4.5, 2.75, 1.75) + " " + C(4.5, 13.25, 1.75) + ";fa " + C(11.5, 4.25, 1.75),
			["res:Keyboard"] = "si M1.5,4.5 H14.5 V12.5 H1.5 Z M4.5,10 H11.5;fi M3.5,6.5 H4.5 V7.5 H3.5 Z M6,6.5 H7 V7.5 H6 Z M8.5,6.5 H9.5 V7.5 H8.5 Z M11,6.5 H12 V7.5 H11 Z",
			["res:Configuration"] = "si " + Gear + " " + C(8, 8, 2),
			["res:Preferences"] = "si M1.5,4 H14.5 M1.5,8 H14.5 M1.5,12 H14.5;fa " + C(5, 4, 1.75) + " " + C(11, 8, 1.75) + " " + C(7, 12, 1.75),
			["res:Properties"] = "si M1.5,2.5 H14.5 V13.5 H1.5 Z M9.5,6.5 H12.5 M9.5,10.5 H12.5;sa M4,6.5 L5.5,8 L8,5 M4,10.5 L5.5,12 L8,9",
			["res:Screenshot"] = "si M1.5,4.5 H4.5 L6,2.5 H10 L11.5,4.5 H14.5 V13.5 H1.5 Z;sa " + C(8, 8.5, 2.75),
			["res:ScreenshotActiveWindow"] = "si M1.5,2.5 H14.5 V13.5 H1.5 Z M1.5,5 H14.5;sa " + C(8, 9.25, 2.5),
			["res:ThingStatistics"] = "si " + C(5.5, 10, 3.5) + ";sa M10,4 A2,2 0 1 1 12,6 V7.5;fa " + C(12, 9.75, 0.75),
			["res:TagStatistics"] = "si M1.5,7.5 V1.5 H7.5 L14,8 L8,14 Z;fa " + C(4.5, 4.5, 1),
			["res:ImageStack"] = "si M4.5,4.5 H14.5 V13.5 H4.5 Z M1.5,10.5 V1.5 H11.5;sa M4.5,11.5 L8,8 L10.5,10.5 L12,9 L14.5,11.5",
			["res:PuzzlePiece"] = "si M2.5,4.5 H6 A1.5,1.5 0 1 1 9,4.5 H12.5 V8 A1.5,1.5 0 1 1 12.5,11 V14.5 H2.5 Z;fg.4 M2.5,4.5 H6 A1.5,1.5 0 1 1 9,4.5 H12.5 V8 A1.5,1.5 0 1 1 12.5,11 V14.5 H2.5 Z",
			["res:Pin"] = "si M6,1.5 H10 M7,1.5 V6 L4.5,8.5 H11.5 L9,6 V1.5 M8,8.5 V14.5",
			["res:Group"] = "si.5 M1.5,1.5 H11.5 V11.5 H1.5 Z;si " + Group,
			["res:GroupAdd"] = "si.5 M1.5,1.5 H11.5 V11.5 H1.5 Z;si " + Group + ";sg M12.5,10 V15 M10,12.5 H15",
			["res:GroupRemove"] = "si.5 M1.5,1.5 H11.5 V11.5 H1.5 Z;si " + Group + ";sr M10.5,10.5 L14.5,14.5 M14.5,10.5 L10.5,14.5",
			["res:ColorPick"] = "fr M8,8 L8,1.5 A6.5,6.5 0 0 1 14.5,8 Z;fy M8,8 L14.5,8 A6.5,6.5 0 0 1 8,14.5 Z;fg M8,8 L8,14.5 A6.5,6.5 0 0 1 1.5,8 Z;fa M8,8 L1.5,8 A6.5,6.5 0 0 1 8,1.5 Z",
			["res:ArrowUp"] = "si M8,13.5 V2.5 M3.5,7 L8,2.5 L12.5,7",
			["res:ArrowDown"] = "si M8,2.5 V13.5 M3.5,9 L8,13.5 L12.5,9",
			["res:Collapse"] = "si M8,1.5 V14.5;sa M2,5 L5.5,8 L2,11 M14,5 L10.5,8 L14,11",
			["res:Expand"] = "si M8,1.5 V14.5;sa M5.5,5 L2,8 L5.5,11 M10.5,5 L14,8 L10.5,11",
			["res:DockerCollapse"] = "si M3,4 L7,8 L3,12 M8.5,4 L12.5,8 L8.5,12",
			["res:DockerExpand"] = "si M13,4 L9,8 L13,12 M7.5,4 L3.5,8 L7.5,12",
			["res:InfoPanelCollapse"] = "si M3.5,6 L8,10.5 L12.5,6",
			["res:InfoPanelExpand"] = "si M3.5,10 L8,5.5 L12.5,10",
			["res:Show"] = "si " + Eye + ";fi " + C(8, 8, 2),
			["res:Show2"] = "si " + Eye + ";fa " + C(8, 8, 2),
			["res:Show3"] = "si " + Eye + ";fa " + C(8, 8, 2) + ";sg M12.5,10 V15 M10,12.5 H15",
			["res:Hide"] = "si M1.5,7 Q8,13 14.5,7 M4,9.5 L3,11.5 M8,10.5 V12.5 M12,9.5 L13,11.5",
			["res:HideAll"] = "si M1.5,5 Q8,11 14.5,5 M4,7.5 L3,9.5 M8,8.5 V10.5;sg M12.5,10 V15 M10,12.5 H15",
			["res:MixedThings"] = "fa.7 " + C(4.5, 11, 3) + ";fr.7 M8,1.5 L12,5.5 L8,9.5 L4,5.5 Z;fg.7 M10,9.5 H14.5 V14 H10 Z",
		};

		// Resource images that show the same thing as an action icon above.
		private static readonly Dictionary<string, string> aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["res:NewMap"] = "builder_newmap", ["res:OpenMap"] = "builder_openmap", ["res:SaveMap"] = "builder_savemap", ["res:Save"] = "builder_savemap",
			["res:Script"] = "builder_openscripteditor", ["res:Script2"] = "builder_openscripteditor",
			["res:Undo"] = "builder_undo", ["res:Redo"] = "builder_redo", ["res:Cut"] = "builder_cutselection", ["res:Copy"] = "builder_copyselection",
			["res:Paste"] = "builder_pasteselection", ["res:Prefab"] = "builder_insertprefabfile", ["res:Prefab2"] = "builder_insertpreviousprefab",
			["res:Filter"] = "builder_thingsfilterssetup", ["res:LinedefColorPresets"] = "builder_linedefcolorssetup",
			["res:Brightness"] = "builder_togglebrightness", ["res:Grid2"] = "builder_togglegrid", ["res:Grid4"] = "builder_togglesnap",
			["res:GridDynamic"] = "builder_toggledynamicgrid", ["res:GridIncrease"] = "builder_gridinc", ["res:GridDecrease"] = "builder_griddec",
			["res:SnapVerts"] = "builder_snapvertstogrid", ["res:Comment"] = "builder_togglecomments", ["res:FixedThingsScale"] = "builder_togglefixedthingsscale",
			["res:ViewNormal"] = "builder_viewmodenormal", ["res:ViewBrightness"] = "builder_viewmodebrightness",
			["res:ViewTextureFloor"] = "builder_viewmodefloors", ["res:ViewTextureCeiling"] = "builder_viewmodeceilings",
			["res:MergeGeoClassic"] = "builder_geomergeclassic", ["res:MergeGeo"] = "builder_geomerge", ["res:MergeGeoRemoveLines"] = "builder_georeplace",
			["res:mergegeometry2"] = "builder_toggleautomerge", ["res:SplitSectors"] = "builder_togglejoinedsectorssplitting",
			["res:ClearTextures"] = "builder_toggleautoclearsidetextures",
			["res:Light"] = "builder_gztogglelights", ["res:Model"] = "builder_gztogglemodels", ["res:fog"] = "builder_gztogglefog", ["res:Sky"] = "builder_gztogglesky",
			["res:ClassicRendering"] = "builder_toggleclassicrendering", ["res:InfoLine"] = "builder_gztoggleeventlines",
			["res:VisualVertices"] = "builder_gztogglevisualvertices", ["res:Test"] = "builder_testmap",
			["res:Zoom"] = "buttonzoom", ["res:Zoom_arrowup"] = "buttonzoom", ["res:Grid2_arrowup"] = "buttongrid",
			["res:Warning"] = "warns:on", ["res:WarningOff"] = "warns:off", ["res:KnownTextureSet"] = "res:ImageStack",
			["res:ScriptHelp"] = "res:Question",
			// BuilderModes
			["res:Similar"] = "buildermodes_selectsimilar", ["res:PlaceThings"] = "buildermodes_placethings",
			["res:SelectThingsInSectors"] = "buildermodes_thingsselectinsectors", ["res:Flip"] = "buildermodes_fliplinedefs",
			["res:FlipSelectionH"] = "buildermodes_flipselectionh", ["res:FlipSelectionV"] = "buildermodes_flipselectionv",
			["res:CurveLines"] = "buildermodes_curvelinesmode", ["res:Door"] = "buildermodes_makedoor", ["res:AlignThings"] = "buildermodes_thingaligntowall",
			["res:ThingPointAtCursor"] = "buildermodes_thinglookatcursor", ["res:FilterThings"] = "buildermodes_filterselectedthings",
			["res:BrightnessGradient"] = "buildermodes_gradientbrightness", ["res:FloorsGradient"] = "buildermodes_gradientfloors",
			["res:CeilsGradient"] = "buildermodes_gradientceilings", ["res:TextureLock"] = "buttonTextureOffsetLock",
			["res:TextureLock3Dfloor"] = "buttonTextureOffset3DFloorLock", ["res:SelectTouching"] = "buttonMarqueSelectTouching",
			["res:LightRadius"] = "buttonlightradii", ["res:SoundRadius"] = "buttonsoundradii",
			["res:ViewSelectionEffects"] = "buttonselectioneffects", ["res:ViewSelectionIndex"] = "buttonselectionnumbers",
			["res:CopyProperties"] = "buildermodes_classiccopyproperties", ["res:PasteProperties"] = "buildermodes_classicpasteproperties",
			["res:PastePropertiesOptions"] = "buildermodes_classicpastepropertieswithoptions",
			["res:Join"] = "buildermodes_joinsectors", ["res:Merge"] = "buildermodes_mergesectors",
			["res:NewSector2"] = "Make Sectors Mode", ["res:DrawCurveMode"] = "Draw Curve Mode",
			// Other plugins
			["res:BuilderEffects/Update"] = "res:Reload",
			// Comments and Tag Explorer list icons (element types)
			["res:VerticesMode"] = "Vertices Mode", ["res:LinesMode"] = "Linedefs Mode", ["res:SectorsMode"] = "Sectors Mode", ["res:ThingsMode"] = "Things Mode",
			["colorpicker_togglelightpannel"] = "res:ColorPick", ["updateslopes"] = "res:Reload",
		};

		/// <summary>Whether there's a ULDB icon for this toolbar item tag.</summary>
		public static bool Has(object tag) => Spec(tag) != null;

		private static string Spec(object tag)
		{
			if(tag == null) return null;
			string key = tag.ToString();
			if(aliases.TryGetValue(key, out string alias)) key = alias;
			return specs.TryGetValue(key, out string spec) ? spec : null;
		}

		/// <summary>A new icon control for the tag, or null.</summary>
		public static Control Create(object tag, double size)
		{
			// Tags are action names, or (edit mode buttons) mode infos whose text is the mode's title.
			string spec = Spec(tag);
			if(spec == null) return null;
			return new IconView(spec) { Width = size, Height = size };
		}

		/// <summary>A new icon control for an upstream resource image (see ResourceImageNames), or null.</summary>
		public static Control ForImage(System.Drawing.Image image, double size) => Create(ResourceImageNames.KeyFor(image), size);

		private sealed class IconView : Control
		{
			private readonly List<(bool fill, char colour, double opacity, Geometry geometry)> layers = new List<(bool, char, double, Geometry)>();

			public IconView(string spec)
			{
				foreach(string part in spec.Split(';'))
				{
					string p = part.Trim();
					int space = p.IndexOf(' ');
					string head = p.Substring(0, space);
					double opacity = head.Length > 2 ? double.Parse("0" + head.Substring(2), CultureInfo.InvariantCulture) : 1.0;
					layers.Add((head[0] == 'f', head[1], opacity, Geometry.Parse(p.Substring(space + 1))));
				}
				ActualThemeVariantChanged += (s, e) => InvalidateVisual();
			}

			private Color Colour(char c)
			{
				bool dark = ActualThemeVariant == ThemeVariant.Dark;
				switch(c)
				{
					case 'a': return Color.FromRgb(0x3d, 0xae, 0xe9); // Breeze accent blue
					case 'r': return Color.FromRgb(0xda, 0x44, 0x53);
					case 'g': return Color.FromRgb(0x27, 0xae, 0x60);
					case 'o': return Color.FromRgb(0xf6, 0x74, 0x00);
					case 'y': return Color.FromRgb(0xfd, 0xbc, 0x4b);
					case 'w': return Color.FromRgb(0xfc, 0xfc, 0xfc);
					default: return dark ? Color.FromRgb(0xfc, 0xfc, 0xfc) : Color.FromRgb(0x23, 0x26, 0x29); // Breeze text
				}
			}

			public override void Render(DrawingContext context)
			{
				double scale = Math.Min(Bounds.Width, Bounds.Height) / 16.0;
				using(context.PushTransform(Matrix.CreateScale(scale, scale)))
				{
					foreach(var (fill, colour, opacity, geometry) in layers)
					{
						Color c = Colour(colour);
						SolidColorBrush brush = new SolidColorBrush(c, opacity);
						if(fill) context.DrawGeometry(brush, null, geometry);
						else context.DrawGeometry(null, new Pen(brush, 1.0, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round), geometry);
					}
				}
			}
		}
	}
}
