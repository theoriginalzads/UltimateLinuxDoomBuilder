using System.Reflection;
using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Rendering;

namespace ULDB.Tests
{
	// Loads real resources through upstream's DataManager on Linux: the Freedoom 2 IWAD
	// (Doom 2 compatible textures, flats and sprites) and the generated test PK3 (MAPINFO,
	// DoomEdNums, ZScript actor, GLDEFS light). Texture loading exercises ULDB.Compat's
	// System.Drawing and uploads to the off-screen renderer.
	[Collection("Editor")]
	public class ResourceLoadingTests
	{
		private readonly Xunit.Abstractions.ITestOutputHelper output;

		public ResourceLoadingTests(Xunit.Abstractions.ITestOutputHelper output) { this.output = output; }

		// Debian/Ubuntu and Arch package paths; override with ULDB_TEST_IWAD.
		private static readonly string[] IWAD_PATHS =
		{
			Environment.GetEnvironmentVariable("ULDB_TEST_IWAD") ?? "",
			"/usr/share/games/doom/freedoom2.wad",
			"/usr/share/doom/freedoom2.wad",
		};

		[SkippableFact]
		public void LoadsFreedoomAndPk3Resources()
		{
			string iwad = IWAD_PATHS.FirstOrDefault(File.Exists);
			Skip.If(iwad == null, "Freedoom 2 IWAD not found; install the freedoom package or set ULDB_TEST_IWAD");

			MapManager manager = HeadlessEditor.CreateMapManager("UZDoom_DoomUDMF.cfg");
			using(OffscreenGL gl = new OffscreenGL())
			{
				RenderDevice device = new RenderDevice(new RenderTargetControl());
				SetField(manager, "graphics", device);
				DataManager data = new DataManager();
				SetField(manager, "data", data);
				try
				{
					DataLocationList configresources = new DataLocationList();
					configresources.Add(new DataLocation(DataLocation.RESOURCE_WAD, iwad, false, false, false, null));
					DataLocationList mapresources = new DataLocationList();
					mapresources.Add(new DataLocation(DataLocation.RESOURCE_PK3, HeadlessEditor.TestMap("pk3_resource.pk3"), false, false, false, null));

					General.ErrorLogger.Clear();
					data.Load(configresources, mapresources);
					List<string> problems = General.ErrorLogger.GetErrors(0).Select(e => e.Type + ": " + e.Description).ToList();
					foreach(string p in problems) output.WriteLine(p);
					Assert.Empty(problems); // Includes building the MAPINFO sky (SKY1) and its skybox

					// Textures (TEXTURE1/PNAMES composites), flats and sprites from the IWAD.
					Assert.Contains("STARTAN2", data.TextureNames);
					Assert.Contains("FLOOR4_8", data.FlatNames);
					AssertImageLoads(data.GetTextureImage("STARTAN2"), 128, 128);
					AssertImageLoads(data.GetFlatImage("FLOOR4_8"), 64, 64);
					ImageData imp = data.GetSpriteImage("TROOA1");
					imp.LoadImageNow();
					Assert.True(imp.IsImageLoaded && imp.Width > 0, "Imp sprite didn't load");

					// From the PK3: MAPINFO DoomEdNums + ZScript actor with editor title/category.
					ThingTypeInfo lamp = data.GetThingInfoEx(30000);
					Assert.NotNull(lamp);
					Assert.Equal("ULDB test lamp", lamp.Title);

					// GLDEFS light bound to that actor.
					Assert.True(data.GldefsEntries.ContainsKey(30000), "GLDEFS light for the test lamp wasn't loaded");
				}
				finally
				{
					data.Dispose();
					SetField(manager, "data", null);
					device.Dispose();
					SetField(manager, "graphics", null);
				}
			}
		}

		private static void AssertImageLoads(ImageData image, int width, int height)
		{
			Assert.NotNull(image);
			image.LoadImageNow();
			Assert.True(image.IsImageLoaded, image.Name + " didn't load");
			Assert.False(image.LoadFailed, image.Name + " failed to load");
			Assert.Equal(width, image.Width);
			Assert.Equal(height, image.Height);
		}

		private static void SetField(MapManager manager, string name, object value)
		{
			typeof(MapManager).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, value);
		}
	}
}
