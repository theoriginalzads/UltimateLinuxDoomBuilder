// ULDB: the OpenGL context UDB's renderer (BuilderNative) draws with, shared with
// Avalonia's so the frame's texture can be shown by a ViewportControl.
//
// UDB renders whenever it likes on the UI thread (redraws on mouse move, texture
// uploads), not only when Avalonia asks for a frame. So it gets its own context,
// made current on demand, and draws into a framebuffer object here. Present() makes
// the viewport copy that framebuffer to the screen on Avalonia's next frame.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.OpenGL;
using Avalonia.Rendering.Composition;
using ULDB.Rendering;

namespace ULDB.Shell
{
	internal static class ViewportContext
	{
		/// <summary>The GPU's renderer string once the viewport's OpenGL context exists (for error reports).</summary>
		public static string Renderer { get; private set; }

		private const int GL_FRAMEBUFFER = 0x8D40;
		private const int GL_RENDERBUFFER = 0x8D41;
		private const int GL_COLOR_ATTACHMENT0 = 0x8CE0;
		private const int GL_DEPTH_STENCIL_ATTACHMENT = 0x821A;
		private const int GL_DEPTH24_STENCIL8 = 0x88F0;
		private const int GL_TEXTURE_2D = 0x0DE1;
		private const int GL_RGBA8 = 0x8058;
		private const int GL_RGBA = 0x1908;
		private const int GL_UNSIGNED_BYTE = 0x1401;
		private const int GL_TEXTURE_MIN_FILTER = 0x2801;
		private const int GL_TEXTURE_MAG_FILTER = 0x2800;
		private const int GL_NEAREST = 0x2600;
		private const int GL_FRAMEBUFFER_COMPLETE = 0x8CD5;

		private static IGlContext context;
		private static GlInterface gl;
		private static Task initializing;
		private static int framebuffer, colortexture, depthbuffer;
		private static unsafe delegate* unmanaged<int, int*, void> glGetIntegerv;
		private static unsafe delegate* unmanaged<int, int, void> glBindBuffer;
		private const int GL_PIXEL_UNPACK_BUFFER = 0x88EC;
		private const int GL_PIXEL_UNPACK_BUFFER_BINDING = 0x88EF;
		private const int GL_TEXTURE_BINDING_2D = 0x8069;
		private const int GL_RENDERBUFFER_BINDING = 0x8CA7;

		/// <summary>True once the context exists and BuilderNative is registered to use it.</summary>
		public static bool IsReady => context != null;

		/// <summary>The texture holding the last presented frame (0 if none), and its size in pixels.</summary>
		public static int FrameTexture => colortexture;
		public static PixelSize FrameSize { get; private set; }

		/// <summary>Raised on the UI thread when UDB presents a frame.</summary>
		public static event Action Presented;

		/// <summary>Creates the context, using the compositor the given visual belongs to. Safe to call repeatedly.</summary>
		public static Task InitializeAsync(Visual visual)
		{
			return initializing ??= Create(visual);
		}

		private static async Task Create(Visual visual)
		{
			Compositor compositor = ElementComposition.GetElementVisual(visual)?.Compositor
				?? throw new InvalidOperationException("The viewport isn't attached to a window yet");
			IOpenGlTextureSharingRenderInterfaceContextFeature sharing =
				await compositor.TryGetRenderInterfaceFeature(typeof(IOpenGlTextureSharingRenderInterfaceContextFeature)) as IOpenGlTextureSharingRenderInterfaceContextFeature;
			if(sharing == null || !sharing.CanCreateSharedContext)
				throw new NotSupportedException("Avalonia's renderer can't share OpenGL textures here (is it running on OpenGL?)");

			// UDB needs OpenGL 3.2 core or newer.
			List<GlVersion> versions = new List<GlVersion>
			{
				new GlVersion(GlProfileType.OpenGL, 4, 6),
				new GlVersion(GlProfileType.OpenGL, 4, 5),
				new GlVersion(GlProfileType.OpenGL, 3, 3),
				new GlVersion(GlProfileType.OpenGL, 3, 2),
			};
			context = sharing.CreateSharedContext(versions)
				?? throw new NotSupportedException("Unable to create an OpenGL 3.2+ context shared with Avalonia's");
			gl = context.GlInterface;
			unsafe
			{
				glGetIntegerv = (delegate* unmanaged<int, int*, void>)gl.GetProcAddress("glGetIntegerv");
				glBindBuffer = (delegate* unmanaged<int, int, void>)gl.GetProcAddress("glBindBuffer");
			}

			HostedGL.Register(name => gl.GetProcAddress(name), MakeCurrent, null, OnPresent);
			CodeImp.DoomBuilder.General.WriteLogLine("Viewport OpenGL context: " + context.Version + ", " + gl.Renderer);
			Renderer = gl.Renderer;
		}

		// The returned scope would restore the previous context; UDB just leaves its own
		// current. Avalonia makes its context current itself whenever it renders.
		private static bool MakeCurrent()
		{
			context.MakeCurrent();
			return true;
		}

		/// <summary>Sizes the framebuffer UDB renders into. Call on the UI thread when the viewport resizes.</summary>
		public static void Resize(PixelSize size)
		{
			if(!IsReady || size.Width <= 0 || size.Height <= 0 || size == FrameSize) return;
			MakeCurrent();

			// UDB's renderer may leave a pixel unpack buffer bound (texture uploads); with one
			// bound, TexImage2D reads from it and fails. Unbind it here and restore UDB's
			// bindings afterwards, so its cached GL state stays true.
			int unpackbuffer = GetInteger(GL_PIXEL_UNPACK_BUFFER_BINDING);
			int boundtexture = GetInteger(GL_TEXTURE_BINDING_2D);
			int boundrenderbuffer = GetInteger(GL_RENDERBUFFER_BINDING);
			if(unpackbuffer != 0) BindBuffer(GL_PIXEL_UNPACK_BUFFER, 0);

			if(framebuffer == 0)
			{
				framebuffer = gl.GenFramebuffer();
				colortexture = gl.GenTexture();
				depthbuffer = gl.GenRenderbuffer();
			}

			gl.BindTexture(GL_TEXTURE_2D, colortexture);
			gl.TexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, size.Width, size.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, IntPtr.Zero);
			gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_NEAREST);
			gl.TexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_NEAREST);
			gl.BindTexture(GL_TEXTURE_2D, 0);

			gl.BindRenderbuffer(GL_RENDERBUFFER, depthbuffer);
			gl.RenderbufferStorage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, size.Width, size.Height);
			gl.BindRenderbuffer(GL_RENDERBUFFER, 0);

			gl.BindFramebuffer(GL_FRAMEBUFFER, framebuffer);
			gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, colortexture, 0);
			gl.FramebufferRenderbuffer(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, depthbuffer);
			int status = gl.CheckFramebufferStatus(GL_FRAMEBUFFER);
			gl.BindTexture(GL_TEXTURE_2D, boundtexture);
			gl.BindRenderbuffer(GL_RENDERBUFFER, boundrenderbuffer);
			if(unpackbuffer != 0) BindBuffer(GL_PIXEL_UNPACK_BUFFER, unpackbuffer);
			gl.BindFramebuffer(GL_FRAMEBUFFER, 0);
			if(status != GL_FRAMEBUFFER_COMPLETE)
				throw new InvalidOperationException("Viewport framebuffer is incomplete (0x" + status.ToString("X") + ")");

			FrameSize = size;
			HostedGL.SetFramebuffer((uint)framebuffer, size.Width, size.Height);
		}

		private static unsafe int GetInteger(int name)
		{
			int value = 0;
			glGetIntegerv(name, &value);
			return value;
		}

		private static unsafe void BindBuffer(int target, int buffer) => glBindBuffer(target, buffer);

		private static void OnPresent()
		{
			// The viewport reads the texture from another context: make sure it's complete.
			gl.Finish();
			Presented?.Invoke();
		}
	}
}
