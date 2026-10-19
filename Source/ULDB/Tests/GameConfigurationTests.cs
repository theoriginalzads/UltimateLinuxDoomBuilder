using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.Config;

namespace ULDB.Tests
{
	// Game configurations must load on Linux. They include each other with Windows-style
	// paths (include("Includes\\UZDoom_common.cfg")), so this exercises path handling.
	[Collection("Editor")]
	public class GameConfigurationTests
	{
		[Theory]
		[InlineData("UZDoom_DoomUDMF.cfg")]
		[InlineData("UZDoom_HereticUDMF.cfg")]
		[InlineData("UZDoom_HexenUDMF.cfg")]
		[InlineData("GZDoom_DoomUDMF.cfg")]
		[InlineData("GZDoom_DoomHexen.cfg")]
		[InlineData("GZDoom_DoomDoom.cfg")]
		public void ConfigurationLoads(string filename)
		{
			ConfigurationInfo info = HeadlessEditor.GetConfiguration(filename);
			GameConfiguration config = new GameConfiguration(info.Configuration);

			Assert.False(string.IsNullOrEmpty(config.Name));
			Assert.NotEmpty(config.LinedefActions);
			Assert.NotEmpty(config.ThingTypes);
			Assert.NotEmpty(config.ThingFlags);
			// UDMF configs override the engine name with the UDMF namespace ("zdoom").
			Assert.Equal(filename.EndsWith("UDMF.cfg") ? "zdoom" : "gzdoom", config.EngineName);
		}

		[Fact]
		public void UdmfConfigurationUsesUniversalFormat()
		{
			GameConfiguration config = new GameConfiguration(HeadlessEditor.GetConfiguration("UZDoom_DoomUDMF.cfg").Configuration);
			Assert.Equal("UniversalMapSetIO", config.FormatInterface);
		}
	}
}
