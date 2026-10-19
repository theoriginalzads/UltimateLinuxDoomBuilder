// ULDB: shows UDB's 2D/3D view. UDB renders into ViewportContext's framebuffer; this
// control copies the last presented frame to the screen, and keeps the RenderTarget
// (upstream's display control, which the renderer measures) at its size in pixels.

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using CodeImp.DoomBuilder.Controls;

namespace ULDB.Shell
{
	internal sealed class ViewportControl : OpenGlControlBase
	{
		private const int GL_FRAMEBUFFER = 0x8D40;
		private const int GL_READ_FRAMEBUFFER = 0x8CA8;
		private const int GL_DRAW_FRAMEBUFFER = 0x8CA9;
		private const int GL_COLOR_ATTACHMENT0 = 0x8CE0;
		private const int GL_TEXTURE_2D = 0x0DE1;
		private const int GL_COLOR_BUFFER_BIT = 0x4000;
		private const int GL_NEAREST = 0x2600;
		private const int GL_LINEAR = 0x2601;

		private int readframebuffer;
		private int attachedtexture;

		public ViewportControl(RenderTargetControl target)
		{
			Target = target;
			Focusable = true;
		}

		/// <summary>Upstream's display control: the renderer reads its ClientSize.</summary>
		public RenderTargetControl Target { get; }

		/// <summary>The control's size in device pixels (what UDB renders at).</summary>
		public PixelSize PixelSize
		{
			get
			{
				double scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
				return new PixelSize(Math.Max(1, (int)Math.Round(Bounds.Width * scale)), Math.Max(1, (int)Math.Round(Bounds.Height * scale)));
			}
		}

		/// <summary>Raised once UDB's OpenGL context is ready (or failed: the argument is the error).</summary>
		public event Action<Exception> ContextReady;

		protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnAttachedToVisualTree(e);
			ViewportContext.Presented += OnPresented;
			try
			{
				await ViewportContext.InitializeAsync(this);
				UpdateSize();
				ContextReady?.Invoke(null);
			}
			catch(Exception ex)
			{
				CodeImp.DoomBuilder.General.ErrorLogger.Add(CodeImp.DoomBuilder.ErrorType.Error, "Unable to set up the OpenGL viewport: " + ex.Message);
				ContextReady?.Invoke(ex);
			}
		}

		protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
		{
			ViewportContext.Presented -= OnPresented;
			base.OnDetachedFromVisualTree(e);
		}

		protected override void OnSizeChanged(SizeChangedEventArgs e)
		{
			base.OnSizeChanged(e);
			UpdateSize();
		}

		private void UpdateSize()
		{
			PixelSize size = PixelSize;
			ViewportContext.Resize(size);
			Target.ClientSize = new System.Drawing.Size(size.Width, size.Height);
			Target.RaiseResize();
		}

		private void OnPresented() => RequestNextFrameRendering();

		protected override void OnOpenGlRender(GlInterface gl, int fb)
		{
			PixelSize dest = PixelSize;
			int texture = ViewportContext.FrameTexture;
			if(texture == 0)
			{
				gl.ClearColor(0, 0, 0, 1);
				gl.Clear(GL_COLOR_BUFFER_BIT);
				return;
			}

			// Framebuffer objects aren't shared between contexts, textures are: wrap the
			// frame texture in a framebuffer of this context to copy from.
			if(readframebuffer == 0) readframebuffer = gl.GenFramebuffer();
			if(attachedtexture != texture)
			{
				gl.BindFramebuffer(GL_FRAMEBUFFER, readframebuffer);
				gl.FramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, texture, 0);
				attachedtexture = texture;
			}

			PixelSize src = ViewportContext.FrameSize;
			gl.BindFramebuffer(GL_READ_FRAMEBUFFER, readframebuffer);
			gl.BindFramebuffer(GL_DRAW_FRAMEBUFFER, fb);
			gl.BlitFramebuffer(0, 0, src.Width, src.Height, 0, 0, dest.Width, dest.Height, GL_COLOR_BUFFER_BIT, src == dest ? GL_NEAREST : GL_LINEAR);
			gl.BindFramebuffer(GL_FRAMEBUFFER, fb);
		}

		protected override void OnOpenGlDeinit(GlInterface gl)
		{
			if(readframebuffer != 0) gl.DeleteFramebuffer(readframebuffer);
			readframebuffer = 0;
			attachedtexture = 0;
		}
	}
}
