using System.Text;
using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;

namespace ULDB.Tests
{
	// Reads the generated GZDoom/UZDoom UDMF test maps with upstream's UDMF reader,
	// writes them back with upstream's writer, and checks nothing is lost or changed.
	[Collection("Editor")] // General.Map is global state; don't run these in parallel
	public class MapRoundTripTests
	{
		private const string CONFIG = "UZDoom_DoomUDMF.cfg";

		[Theory]
		// Counts as written by tools/testmaps/make_testmaps.py.
		[InlineData("small.wad", 8, 9, 10, 2, 2)]
		[InlineData("large.wad", 2601, 5100, 10000, 2500, 11)]
		[InlineData("acs.wad", 8, 8, 12, 2, 1)]
		[InlineData("bcs.wad", 8, 8, 12, 2, 1)]
		[InlineData("3dfloors_slopes.wad", 18, 20, 26, 5, 2)]
		public void ReadsExpectedGeometry(string file, int vertices, int linedefs, int sidedefs, int sectors, int things)
		{
			MapManager manager = HeadlessEditor.CreateMapManager(CONFIG);
			MapSet map = Read(manager, HeadlessEditor.TestMap(file));

			Assert.Equal(vertices, map.Vertices.Count);
			Assert.Equal(linedefs, map.Linedefs.Count);
			Assert.Equal(sidedefs, map.Sidedefs.Count);
			Assert.Equal(sectors, map.Sectors.Count);
			Assert.Equal(things, map.Things.Count);
		}

		[Theory]
		[InlineData("small.wad")]
		[InlineData("large.wad")]
		[InlineData("acs.wad")]
		[InlineData("bcs.wad")]
		[InlineData("3dfloors_slopes.wad")]
		public void WriteReadWriteIsStable(string file)
		{
			MapManager manager = HeadlessEditor.CreateMapManager(CONFIG);
			MapSet original = Read(manager, HeadlessEditor.TestMap(file));

			// Write, read back, write again: the two TEXTMAPs must be identical,
			// and the reread map must match the original element by element.
			string first = WriteTextmap(manager, original, out string firstwad);
			MapSet reread = Read(manager, firstwad);
			string second = WriteTextmap(manager, reread, out _);

			Assert.Equal(first, second);
			AssertSameMap(original, reread);
		}

		[Fact]
		public void KeepsGZDoomSpecificData()
		{
			MapManager manager = HeadlessEditor.CreateMapManager(CONFIG);
			MapSet map = Read(manager, HeadlessEditor.TestMap("3dfloors_slopes.wad"));
			map = Read(manager, WriteWad(manager, map));

			// Sector_Set3dFloor (160) on the control sector line, targeting tag 10.
			Linedef floor3d = map.Linedefs.Single(l => l.Action == 160);
			Assert.Equal(new[] { 10, 1, 0, 255, 0 }, floor3d.Args);
			Assert.Contains(map.Sectors, s => s.Tags.Contains(10));

			// Plane_Align (181) slope.
			Assert.Single(map.Linedefs, l => l.Action == 181);

			// Vertex-height slope (UDMF zfloor).
			Assert.Contains(map.Vertices, v => !double.IsNaN(v.ZFloor) && v.ZFloor == 48.0);

			// Dynamic light thing with its colour args.
			Thing light = map.Things.Single(t => t.Type == 9800);
			Assert.Equal(new[] { 255, 200, 150, 160, 0 }, light.Args);
		}

		[Fact]
		public void KeepsAcsSwitch()
		{
			MapManager manager = HeadlessEditor.CreateMapManager(CONFIG);
			MapSet map = Read(manager, WriteWad(manager, Read(manager, HeadlessEditor.TestMap("acs.wad"))));

			Linedef sw = map.Linedefs.Single(l => l.Action == 80); // ACS_Execute
			Assert.Equal(2, sw.Args[0]);
			Assert.True(sw.IsFlagSet("playeruse"));
			Assert.True(sw.IsFlagSet("repeatspecial"));
		}

		#region Helpers

		private static MapSet Read(MapManager manager, string path)
		{
			using(WAD wad = new WAD(path, true))
			{
				UniversalMapSetIO io = new UniversalMapSetIO(wad, manager);
				string mapname = wad.Lumps.Any(l => l.Name == MapManager.TEMP_MAP_HEADER) ? MapManager.TEMP_MAP_HEADER : "MAP01";
				// Same calls as MapManager when it loads a map.
				MapSet map = new MapSet();
				map.BeginAddRemove();
				map = io.Read(map, mapname);
				map.EndAddRemove();
				return map;
			}
		}

		private static string WriteWad(MapManager manager, MapSet map)
		{
			WriteTextmap(manager, map, out string path);
			return path;
		}

		private static string WriteTextmap(MapManager manager, MapSet map, out string path)
		{
			path = Path.Combine(HeadlessEditor.TempDir, Guid.NewGuid().ToString("N") + ".wad");
			using(WAD wad = new WAD(path, false))
			{
				// Same steps as MapManager when it writes the temporary map file.
				wad.Insert(MapManager.TEMP_MAP_HEADER, 0, 0);
				new UniversalMapSetIO(wad, manager).Write(map, MapManager.TEMP_MAP_HEADER, 1);
				wad.WriteHeaders();

				Lump textmap = wad.FindLump("TEXTMAP");
				textmap.Stream.Seek(0, SeekOrigin.Begin);
				return Encoding.ASCII.GetString(textmap.Stream.ReadAllBytes());
			}
		}

		private static void AssertSameMap(MapSet a, MapSet b)
		{
			Assert.Equal(a.Vertices.Count, b.Vertices.Count);
			Assert.Equal(a.Linedefs.Count, b.Linedefs.Count);
			Assert.Equal(a.Sidedefs.Count, b.Sidedefs.Count);
			Assert.Equal(a.Sectors.Count, b.Sectors.Count);
			Assert.Equal(a.Things.Count, b.Things.Count);

			foreach(var (x, y) in a.Vertices.Zip(b.Vertices))
			{
				Assert.Equal(x.Position, y.Position);
				Assert.Equal(x.ZFloor, y.ZFloor);
				Assert.Equal(x.ZCeiling, y.ZCeiling);
			}
			foreach(var (x, y) in a.Linedefs.Zip(b.Linedefs))
			{
				Assert.Equal(x.Start.Index, y.Start.Index);
				Assert.Equal(x.End.Index, y.End.Index);
				Assert.Equal(x.Action, y.Action);
				Assert.Equal(x.Args, y.Args);
				Assert.Equal(x.Tags, y.Tags);
				Assert.Equal(x.GetFlags().OrderBy(f => f.Key), y.GetFlags().OrderBy(f => f.Key));
				Assert.Equal(x.Front?.Index, y.Front?.Index);
				Assert.Equal(x.Back?.Index, y.Back?.Index);
			}
			foreach(var (x, y) in a.Sidedefs.Zip(b.Sidedefs))
			{
				Assert.Equal(x.Sector.Index, y.Sector.Index);
				Assert.Equal(x.HighTexture, y.HighTexture);
				Assert.Equal(x.MiddleTexture, y.MiddleTexture);
				Assert.Equal(x.LowTexture, y.LowTexture);
				Assert.Equal(x.OffsetX, y.OffsetX);
				Assert.Equal(x.OffsetY, y.OffsetY);
			}
			foreach(var (x, y) in a.Sectors.Zip(b.Sectors))
			{
				Assert.Equal(x.FloorHeight, y.FloorHeight);
				Assert.Equal(x.CeilHeight, y.CeilHeight);
				Assert.Equal(x.FloorTexture, y.FloorTexture);
				Assert.Equal(x.CeilTexture, y.CeilTexture);
				Assert.Equal(x.Brightness, y.Brightness);
				Assert.Equal(x.Tags, y.Tags);
			}
			foreach(var (x, y) in a.Things.Zip(b.Things))
			{
				Assert.Equal(x.Type, y.Type);
				Assert.Equal(x.Position, y.Position);
				Assert.Equal(x.AngleDoom, y.AngleDoom);
				Assert.Equal(x.Action, y.Action);
				Assert.Equal(x.Args, y.Args);
				Assert.Equal(x.GetFlags().OrderBy(f => f.Key), y.GetFlags().OrderBy(f => f.Key));
			}
		}

		#endregion
	}

	internal static class StreamExtensions
	{
		public static byte[] ReadAllBytes(this Stream s)
		{
			using(MemoryStream ms = new MemoryStream())
			{
				s.CopyTo(ms);
				return ms.ToArray();
			}
		}
	}
}
