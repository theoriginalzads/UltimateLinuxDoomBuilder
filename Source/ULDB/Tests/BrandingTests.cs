using System.Windows.Forms;

namespace ULDB.Tests
{
	public class BrandingTests
	{
		[Fact]
		public void MessageBoxCaptionsUseTheForkName()
		{
			// Upstream titles message boxes with Application.ProductName (from its "Ultimate Doom
			// Builder" assembly info); Builder's module initializer brands it.
			System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(typeof(CodeImp.DoomBuilder.General).Module.ModuleHandle);
			Assert.Equal("Ultimate Linux Doom Builder", Application.ProductName);
		}

		[Fact]
		public void AboutTextNamesUpstreamAndDisclosesAi()
		{
			Assert.Contains("Ultimate Doom Builder", Branding.UNOFFICIAL_NOTICE);
			Assert.Contains("AI", Branding.AI_DISCLOSURE);
			Assert.Contains("GZDoom", Branding.TAGLINE);
			Assert.Contains("UZDoom", Branding.TAGLINE);
		}
	}
}
