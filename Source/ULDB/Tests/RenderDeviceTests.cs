using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Rendering;

namespace ULDB.Tests
{
	// BuilderNative running on a host-provided OpenGL context (here an off-screen EGL one).
	[Collection("Editor")]
	public class RenderDeviceTests
	{
		[Fact]
		public void RenderDeviceCreatesOnHostContextAndClears()
		{
			HeadlessEditor.Initialize();
			using(OffscreenGL gl = new OffscreenGL(64, 32))
			{
				RenderDevice device = new RenderDevice(new RenderTargetControl());
				try
				{
					// Shaders compiled in the constructor; now clear the host framebuffer to a known colour.
					device.StartRendering(true, new Color4(0.0f, 1.0f, 0.0f, 1.0f));
					device.FinishRendering();

					(byte r, byte g, byte b, byte a) = gl.ReadPixel(10, 10);
					Assert.Equal((0, 255, 0, 255), ((int)r, (int)g, (int)b, (int)a));
				}
				finally
				{
					device.Dispose();
				}
			}
		}
	}
}
