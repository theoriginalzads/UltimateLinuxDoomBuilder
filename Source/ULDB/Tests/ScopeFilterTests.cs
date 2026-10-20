using CodeImp.DoomBuilder;
using ULDB.Scope;

namespace ULDB.Tests
{
	// The GZDoom/UZDoom-only scope, checked against upstream's real configuration files.
	[Collection("Editor")]
	public class ScopeFilterTests
	{
		private static List<string> AllConfigurations()
		{
			HeadlessEditor.Initialize();
			return General.Configs.Select(c => c.Filename).ToList();
		}

		[Fact]
		public void OnlyGZDoomAndUZDoomConfigurationsAreVisible()
		{
			List<string> visible = AllConfigurations().Where(ScopeFilter.IsConfigurationVisible).OrderBy(f => f).ToList();

			Assert.Equal(new[]
			{
				"GZDoom_DoomDoom.cfg", "GZDoom_DoomHexen.cfg", "GZDoom_DoomUDMF.cfg",
				"GZDoom_HereticDoom.cfg", "GZDoom_HereticHexen.cfg", "GZDoom_HereticUDMF.cfg",
				"GZDoom_HexenHexen.cfg", "GZDoom_HexenUDMF.cfg",
				"GZDoom_StrifeDoom.cfg", "GZDoom_StrifeHexen.cfg", "GZDoom_StrifeUDMF.cfg",
				"UZDoom_DoomUDMF.cfg", "UZDoom_HereticUDMF.cfg", "UZDoom_HexenUDMF.cfg",
			}, visible);
		}

		[Fact]
		public void EveryLoadedConfigurationIsEitherVisibleOrHidden()
		{
			// New upstream configs must not slip through: anything not GZDoom/UZDoom is hidden.
			foreach(string f in AllConfigurations())
				Assert.Equal(f.StartsWith("GZDoom_") || f.StartsWith("UZDoom_"), ScopeFilter.IsConfigurationVisible(f));
		}

		[Fact]
		public void DefaultConfigurationExistsAndIsVisible()
		{
			Assert.Contains(ScopeFilter.DEFAULT_CONFIGURATION, AllConfigurations());
			Assert.True(ScopeFilter.IsConfigurationVisible(ScopeFilter.DEFAULT_CONFIGURATION));
		}

		[Theory]
		[InlineData("Doom_Doom2Doom.cfg", "GZDoom_DoomDoom.cfg")]          // Vanilla Doom 2
		[InlineData("Boom_Doom2Doom.cfg", "GZDoom_DoomDoom.cfg")]          // Boom
		[InlineData("MBF21_Doom2Doom.cfg", "GZDoom_DoomDoom.cfg")]
		[InlineData("ZDoom_DoomHexen.cfg", "GZDoom_DoomHexen.cfg")]        // Hexen format stays Hexen format
		[InlineData("ZDoom_DoomUDMF.cfg", "UZDoom_DoomUDMF.cfg")]          // UDMF prefers UZDoom
		[InlineData("Zandronum_HereticUDMF.cfg", "UZDoom_HereticUDMF.cfg")]
		[InlineData("ZDoom_StrifeUDMF.cfg", "GZDoom_StrifeUDMF.cfg")]      // No UZDoom Strife config: GZDoom
		[InlineData("Hexen_HexenHexen.cfg", "GZDoom_HexenHexen.cfg")]
		[InlineData("GZDoom_DoomHexen.cfg", "GZDoom_DoomHexen.cfg")]       // Already visible: unchanged
		[InlineData("Eternity_DoomUDMF.cfg", "UZDoom_DoomUDMF.cfg")]
		[InlineData("SomethingNew.cfg", "UZDoom_DoomUDMF.cfg")]            // Unknown: the default
		public void HiddenConfigurationsMapToTheClosestVisibleOne(string hidden, string expected)
		{
			Assert.Equal(expected, ScopeFilter.SuggestConfiguration(hidden, AllConfigurations()));
		}

		[Fact]
		public void OnlyZdbspNodebuildersAreVisible()
		{
			HeadlessEditor.Initialize();
			List<string> names = General.Nodebuilders.Select(n => n.Name).ToList();
			Assert.Contains("zdbsp_udmf_normal", names);
			Assert.All(names.Where(ScopeFilter.IsNodebuilderVisible), n => Assert.StartsWith("zdbsp_", n));
			Assert.DoesNotContain(names, n => n.StartsWith("zennode") && ScopeFilter.IsNodebuilderVisible(n));
			Assert.True(ScopeFilter.IsNodebuilderVisible("")); // "None"
		}

		[Fact]
		public void OnlyGZDoomScriptCompilersAreVisible()
		{
			HeadlessEditor.Initialize();
			List<string> visible = General.CompiledScriptConfigs.Keys.Where(ScopeFilter.IsScriptCompilerVisible).OrderBy(k => k).ToList();
			Assert.Equal(new[] { "bcs.cfg", "zdoom_acs.cfg", "zt_bcs.cfg" }, visible);
		}
	}
}
