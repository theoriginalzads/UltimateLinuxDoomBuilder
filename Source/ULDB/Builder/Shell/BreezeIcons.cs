// ULDB: vector icons for the main window's toolbars, in the style of KDE's Breeze icons:
// a 16×16 grid, 1 px strokes, monochrome in the theme's text colour, with Breeze's accent blue
// (and red/green/orange where the meaning calls for it) as highlights. Icons are keyed by the
// toolbar item's Tag: upstream's action name ("builder_savemap") or, for edit modes, the mode's
// title ("Vertices Mode"). Items without an icon here keep upstream's bitmap.
//
// Spec format, one layer per ';': "<s|f><colour>[opacity] <path data>", s = stroke, f = fill,
// colours: i = ink (text colour), a = accent, r = red, g = green, o = orange, y = yellow.

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
		};

		/// <summary>Whether there's a ULDB icon for this toolbar item tag.</summary>
		public static bool Has(object tag) => tag != null && specs.ContainsKey(tag.ToString());

		/// <summary>A new icon control for the tag, or null.</summary>
		public static Control Create(object tag, double size)
		{
			// Tags are action names, or (edit mode buttons) mode infos whose text is the mode's title.
			if(tag == null || !specs.TryGetValue(tag.ToString(), out string spec)) return null;
			return new IconView(spec) { Width = size, Height = size };
		}

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
