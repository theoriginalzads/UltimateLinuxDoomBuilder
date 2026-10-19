// An off-screen OpenGL 3.3 core context for tests (Mesa's surfaceless EGL platform),
// registered with BuilderNative as its host context. Rendering goes to a framebuffer
// object that tests can read back. No display server or GPU needed (llvmpipe works).

using System.Runtime.InteropServices;
using ULDB.Rendering;

namespace ULDB.Tests
{
	public sealed unsafe class OffscreenGL : IDisposable
	{
		private const int EGL_NONE = 0x3038;
		private const int EGL_RENDERABLE_TYPE = 0x3040;
		private const int EGL_OPENGL_BIT = 0x0008;
		private const int EGL_OPENGL_API = 0x30A2;
		private const int EGL_CONTEXT_MAJOR_VERSION = 0x3098;
		private const int EGL_CONTEXT_MINOR_VERSION = 0x30FB;
		private const int EGL_CONTEXT_OPENGL_PROFILE_MASK = 0x30FD;
		private const int EGL_CONTEXT_OPENGL_CORE_PROFILE_BIT = 0x0001;
		private const int EGL_PLATFORM_SURFACELESS_MESA = 0x31DD;

		private const int GL_FRAMEBUFFER = 0x8D40;
		private const int GL_RENDERBUFFER = 0x8D41;
		private const int GL_RGBA8 = 0x8058;
		private const int GL_DEPTH24_STENCIL8 = 0x88F0;
		private const int GL_COLOR_ATTACHMENT0 = 0x8CE0;
		private const int GL_DEPTH_STENCIL_ATTACHMENT = 0x821A;
		private const int GL_FRAMEBUFFER_COMPLETE = 0x8CD5;
		private const int GL_RGBA = 0x1908;
		private const int GL_UNSIGNED_BYTE = 0x1401;

		private readonly IntPtr display;
		private readonly IntPtr context;
		private uint framebuffer;
		private readonly uint[] renderbuffers = new uint[2];

		public int Width { get; }
		public int Height { get; }
		public string Renderer { get; }

		public OffscreenGL(int width = 256, int height = 256)
		{
			Width = width;
			Height = height;

			display = eglGetPlatformDisplay(EGL_PLATFORM_SURFACELESS_MESA, IntPtr.Zero, null);
			if(display == IntPtr.Zero || !eglInitialize(display, out _, out _))
				throw new InvalidOperationException("No surfaceless EGL display (needs Mesa's EGL)");
			if(!eglBindAPI(EGL_OPENGL_API)) throw new InvalidOperationException("eglBindAPI(OpenGL) failed");

			// Surfaceless displays may offer no OpenGL configs; a config isn't needed without
			// surfaces, so fall back to EGL_KHR_no_config_context (EGL_NO_CONFIG_KHR = null).
			int[] configattribs = { EGL_RENDERABLE_TYPE, EGL_OPENGL_BIT, EGL_NONE };
			IntPtr config;
			if(!eglChooseConfig(display, configattribs, &config, 1, out int numconfigs) || numconfigs == 0)
				config = IntPtr.Zero;

			int[] contextattribs = { EGL_CONTEXT_MAJOR_VERSION, 3, EGL_CONTEXT_MINOR_VERSION, 3, EGL_CONTEXT_OPENGL_PROFILE_MASK, EGL_CONTEXT_OPENGL_CORE_PROFILE_BIT, EGL_NONE };
			context = eglCreateContext(display, config, IntPtr.Zero, contextattribs);
			if(context == IntPtr.Zero) throw new InvalidOperationException("eglCreateContext failed: 0x" + eglGetError().ToString("X"));
			if(!MakeCurrent()) throw new InvalidOperationException("eglMakeCurrent failed: 0x" + eglGetError().ToString("X"));

			Renderer = Marshal.PtrToStringUTF8(((delegate* unmanaged<int, IntPtr>)Proc("glGetString"))(0x1F01));

			// The framebuffer BuilderNative renders to when no render target texture is set.
			var genfb = (delegate* unmanaged<int, uint*, void>)Proc("glGenFramebuffers");
			var bindfb = (delegate* unmanaged<int, uint, void>)Proc("glBindFramebuffer");
			var genrb = (delegate* unmanaged<int, uint*, void>)Proc("glGenRenderbuffers");
			var bindrb = (delegate* unmanaged<int, uint, void>)Proc("glBindRenderbuffer");
			var storage = (delegate* unmanaged<int, int, int, int, void>)Proc("glRenderbufferStorage");
			var attach = (delegate* unmanaged<int, int, int, uint, void>)Proc("glFramebufferRenderbuffer");
			var status = (delegate* unmanaged<int, int>)Proc("glCheckFramebufferStatus");

			uint fb;
			genfb(1, &fb);
			framebuffer = fb;
			fixed(uint* rb = renderbuffers) genrb(2, rb);
			bindrb(GL_RENDERBUFFER, renderbuffers[0]);
			storage(GL_RENDERBUFFER, GL_RGBA8, width, height);
			bindrb(GL_RENDERBUFFER, renderbuffers[1]);
			storage(GL_RENDERBUFFER, GL_DEPTH24_STENCIL8, width, height);
			bindfb(GL_FRAMEBUFFER, framebuffer);
			attach(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_RENDERBUFFER, renderbuffers[0]);
			attach(GL_FRAMEBUFFER, GL_DEPTH_STENCIL_ATTACHMENT, GL_RENDERBUFFER, renderbuffers[1]);
			if(status(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE) throw new InvalidOperationException("Test framebuffer is incomplete");

			HostedGL.Register(Proc, MakeCurrent);
			HostedGL.SetFramebuffer(framebuffer, width, height);
		}

		public bool MakeCurrent() => eglMakeCurrent(display, IntPtr.Zero, IntPtr.Zero, context);

		public IntPtr Proc(string name) => eglGetProcAddress(name);

		/// <summary>Reads one RGBA pixel from the framebuffer (origin bottom-left, as in OpenGL).</summary>
		public (byte r, byte g, byte b, byte a) ReadPixel(int x, int y)
		{
			MakeCurrent();
			((delegate* unmanaged<int, uint, void>)Proc("glBindFramebuffer"))(GL_FRAMEBUFFER, framebuffer);
			byte* px = stackalloc byte[4];
			((delegate* unmanaged<int, int, int, int, int, int, void*, void>)Proc("glReadPixels"))(x, y, 1, 1, GL_RGBA, GL_UNSIGNED_BYTE, px);
			return (px[0], px[1], px[2], px[3]);
		}

		public void Dispose()
		{
			HostedGL.Unregister();
			eglMakeCurrent(display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			eglDestroyContext(display, context);
			eglTerminate(display);
		}

		[DllImport("libEGL.so.1")] private static extern IntPtr eglGetPlatformDisplay(int platform, IntPtr nativeDisplay, IntPtr* attribs);
		[DllImport("libEGL.so.1")] private static extern bool eglInitialize(IntPtr display, out int major, out int minor);
		[DllImport("libEGL.so.1")] private static extern bool eglBindAPI(int api);
		[DllImport("libEGL.so.1")] private static extern bool eglChooseConfig(IntPtr display, int[] attribs, IntPtr* configs, int size, out int count);
		[DllImport("libEGL.so.1")] private static extern IntPtr eglCreateContext(IntPtr display, IntPtr config, IntPtr share, int[] attribs);
		[DllImport("libEGL.so.1")] private static extern bool eglMakeCurrent(IntPtr display, IntPtr draw, IntPtr read, IntPtr context);
		[DllImport("libEGL.so.1")] private static extern IntPtr eglGetProcAddress([MarshalAs(UnmanagedType.LPUTF8Str)] string name);
		[DllImport("libEGL.so.1")] private static extern bool eglDestroyContext(IntPtr display, IntPtr context);
		[DllImport("libEGL.so.1")] private static extern bool eglTerminate(IntPtr display);
		[DllImport("libEGL.so.1")] private static extern int eglGetError();
	}
}
