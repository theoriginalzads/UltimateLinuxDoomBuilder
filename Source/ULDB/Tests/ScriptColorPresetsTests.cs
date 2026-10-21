// ULDB: the extra script editor colour presets (Preferences → Script Editor).

using System.Linq;
using ULDB.Scripting;
using Xunit;

namespace ULDB.Tests
{
	public class ScriptColorPresetsTests
	{
		[Fact]
		public void EveryPresetHasAllEighteenColours()
		{
			Assert.NotEmpty(ScriptColorPresets.Names);
			foreach(string name in ScriptColorPresets.Names.ToList())
			{
				Assert.True(ScriptColorPresets.TryGet(name, out var colors), name);
				Assert.Equal(ScriptColorPresets.Fields.Length, colors.Length);
			}
		}

		[Fact]
		public void UnknownNamesAreNotPresets() => Assert.False(ScriptColorPresets.TryGet("Use current settings", out _));
	}
}
