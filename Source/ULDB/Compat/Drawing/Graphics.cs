// ULDB compatibility layer: System.Drawing.Graphics and friends on SkiaSharp.
// Graphics draws into a premultiplied copy of the target bitmap (Skia can't draw
// into unpremultiplied memory) and writes the result back on Flush/Dispose.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using SkiaSharp;

namespace System.Drawing
{
	public sealed class Graphics : IDisposable
	{
		private Bitmap target;
		private SKBitmap surface;
		private SKCanvas canvas;

		private Graphics(Bitmap target)
		{
			this.target = target;
			surface = Bitmap.ToPremul(target.Native);
			canvas = new SKCanvas(surface);
		}

		public static Graphics FromImage(Image image)
		{
			if(image is Bitmap b) return new Graphics(b);
			throw new ArgumentException("ULDB only supports drawing on Bitmaps", nameof(image));
		}

		/// <summary>There are no window handles: returns a measuring-only Graphics (1x1 bitmap).</summary>
		public static Graphics FromHwnd(IntPtr hwnd) => new Graphics(new Bitmap(1, 1));
		public static Graphics FromHwndInternal(IntPtr hwnd) => FromHwnd(hwnd);

		/// <summary>
		/// Copies screen pixels into this Graphics. Routed to the UI (ScreenCapture), since
		/// only it can read the window's contents; unsupported without one.
		/// </summary>
		public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize)
		{
			Bitmap shot = ScreenCapture?.Invoke(new Rectangle(upperLeftSource, blockRegionSize))
				?? throw new NotSupportedException("Screen capture isn't available");
			using(shot) DrawImage(shot, upperLeftDestination.X, upperLeftDestination.Y, blockRegionSize.Width, blockRegionSize.Height);
		}

		/// <summary>ULDB: installed by the UI. Returns the given screen area as a new bitmap, or null.</summary>
		public static Func<Rectangle, Bitmap> ScreenCapture { get; set; }

		#region Properties

		// GDI+ quality settings, mapped onto the closest Skia behaviour.
		public SmoothingMode SmoothingMode { get; set; } = SmoothingMode.None;
		public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Bilinear;
		public CompositingQuality CompositingQuality { get; set; } = CompositingQuality.Default;
		public CompositingMode CompositingMode { get; set; } = CompositingMode.SourceOver;
		public PixelOffsetMode PixelOffsetMode { get; set; } = PixelOffsetMode.Default;
		public TextRenderingHint TextRenderingHint { get; set; } = TextRenderingHint.SystemDefault;
		public GraphicsUnit PageUnit { get; set; } = GraphicsUnit.Display;
		public float DpiX => target.HorizontalResolution;
		public float DpiY => target.VerticalResolution;

		private bool Antialias => SmoothingMode == SmoothingMode.AntiAlias || SmoothingMode == SmoothingMode.HighQuality;

		private SKSamplingOptions Sampling
		{
			get
			{
				switch(InterpolationMode)
				{
					case InterpolationMode.NearestNeighbor: return new SKSamplingOptions(SKFilterMode.Nearest);
					case InterpolationMode.HighQualityBicubic:
					case InterpolationMode.Bicubic:
					case InterpolationMode.High: return new SKSamplingOptions(SKCubicResampler.Mitchell);
					default: return new SKSamplingOptions(SKFilterMode.Linear);
				}
			}
		}

		private SKBlendMode BlendMode => CompositingMode == CompositingMode.SourceCopy ? SKBlendMode.Src : SKBlendMode.SrcOver;

		#endregion

		#region Images

		public void Clear(Color color) => canvas.Clear(ToSK(color));

		public void DrawImage(Image image, int x, int y) => DrawImage(image, new Rectangle(x, y, image.Width, image.Height));
		public void DrawImage(Image image, float x, float y) => DrawImage(image, new RectangleF(x, y, image.Width, image.Height));
		public void DrawImage(Image image, Point point) => DrawImage(image, point.X, point.Y);
		public void DrawImage(Image image, int x, int y, int width, int height) => DrawImage(image, new Rectangle(x, y, width, height));
		public void DrawImage(Image image, float x, float y, float width, float height) => DrawImage(image, new RectangleF(x, y, width, height));
		public void DrawImage(Image image, Rectangle rect) => DrawImage(image, (RectangleF)rect);

		public void DrawImage(Image image, RectangleF rect)
		{
			DrawImageCore(image, new RectangleF(0, 0, image.Width, image.Height), rect, null);
		}

		public void DrawImage(Image image, int x, int y, Rectangle srcRect, GraphicsUnit srcUnit)
		{
			DrawImageCore(image, srcRect, new RectangleF(x, y, srcRect.Width, srcRect.Height), null);
		}

		public void DrawImage(Image image, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit)
		{
			DrawImageCore(image, srcRect, destRect, null);
		}

		public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit)
		{
			DrawImageCore(image, srcRect, destRect, null);
		}

		public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit)
		{
			DrawImageCore(image, new RectangleF(srcX, srcY, srcWidth, srcHeight), destRect, null);
		}

		public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttr)
		{
			DrawImageCore(image, new RectangleF(srcX, srcY, srcWidth, srcHeight), destRect, imageAttr);
		}

		// destPoints: where the source's upper-left, upper-right and lower-left corners go
		// (a parallelogram: rotation, skew, flips).
		public void DrawImage(Image image, Point[] destPoints) => DrawImage(image, Array.ConvertAll(destPoints, p => (PointF)p), new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel, null);
		public void DrawImage(Image image, PointF[] destPoints) => DrawImage(image, destPoints, new RectangleF(0, 0, image.Width, image.Height), GraphicsUnit.Pixel, null);
		public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit) => DrawImage(image, Array.ConvertAll(destPoints, p => (PointF)p), srcRect, srcUnit, null);
		public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr) => DrawImage(image, Array.ConvertAll(destPoints, p => (PointF)p), srcRect, srcUnit, imageAttr);

		public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr)
		{
			if(destPoints == null || destPoints.Length != 3) throw new ArgumentException("Expected three destination points", nameof(destPoints));
			PointF p0 = destPoints[0], p1 = destPoints[1], p2 = destPoints[2];
			float w = srcRect.Width, h = srcRect.Height;
			SKMatrix map = new SKMatrix(
				(p1.X - p0.X) / w, (p2.X - p0.X) / h, p0.X,
				(p1.Y - p0.Y) / w, (p2.Y - p0.Y) / h, p0.Y,
				0, 0, 1);
			canvas.Save();
			canvas.Concat(in map);
			DrawImageCore(image, srcRect, new RectangleF(0, 0, w, h), imageAttr);
			canvas.Restore();
		}

		public void DrawImageUnscaled(Image image, int x, int y) => DrawImage(image, x, y);
		public void DrawImageUnscaled(Image image, Point point) => DrawImage(image, point.X, point.Y);
		public void DrawImageUnscaled(Image image, Rectangle rect) => DrawImage(image, rect.X, rect.Y); // GDI+ ignores the size here

		public void DrawImageUnscaledAndClipped(Image image, Rectangle rect)
		{
			int w = Math.Min(rect.Width, image.Width);
			int h = Math.Min(rect.Height, image.Height);
			DrawImageCore(image, new RectangleF(0, 0, w, h), new RectangleF(rect.X, rect.Y, w, h), null);
		}

		private void DrawImageCore(Image image, RectangleF src, RectangleF dest, ImageAttributes attributes)
		{
			using(SKBitmap source = Bitmap.ToPremul(image.Native))
			using(SKImage skimage = SKImage.FromBitmap(source))
			using(SKColorFilter filter = attributes?.CreateColorFilter())
			using(SKPaint paint = new SKPaint { BlendMode = BlendMode, IsAntialias = Antialias, ColorFilter = filter })
			{
				// GDI+ samples edge pixels from outside the source rect unless told to
				// wrap (UDB uses TileFlipXY to avoid dark edges when scaling skies).
				// Clamping to the source rect gives the same result for that case.
				canvas.DrawImage(skimage, ToSK(src), ToSK(dest), Sampling, paint);
			}
		}

		#endregion

		#region Shapes

		public void FillRectangle(Brush brush, int x, int y, int width, int height) => FillRectangle(brush, new RectangleF(x, y, width, height));
		public void FillRectangle(Brush brush, float x, float y, float width, float height) => FillRectangle(brush, new RectangleF(x, y, width, height));
		public void FillRectangle(Brush brush, Rectangle rect) => FillRectangle(brush, (RectangleF)rect);

		public void FillRectangle(Brush brush, RectangleF rect)
		{
			using(SKPaint paint = brush.CreatePaint(Antialias, BlendMode))
				canvas.DrawRect(ToSK(rect), paint);
		}

		public void DrawRectangle(Pen pen, int x, int y, int width, int height) => DrawRectangle(pen, new RectangleF(x, y, width, height));
		public void DrawRectangle(Pen pen, Rectangle rect) => DrawRectangle(pen, (RectangleF)rect);

		public void DrawRectangle(Pen pen, RectangleF rect)
		{
			using(SKPaint paint = pen.CreatePaint(Antialias))
				canvas.DrawRect(ToSK(rect), paint);
		}

		public void DrawLine(Pen pen, int x1, int y1, int x2, int y2) => DrawLine(pen, (float)x1, y1, x2, y2);
		public void DrawLine(Pen pen, Point p1, Point p2) => DrawLine(pen, (float)p1.X, p1.Y, p2.X, p2.Y);
		public void DrawLine(Pen pen, PointF p1, PointF p2) => DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);

		public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
		{
			using(SKPaint paint = pen.CreatePaint(Antialias))
				canvas.DrawLine(x1, y1, x2, y2, paint);
		}

		public void DrawLines(Pen pen, Point[] points)
		{
			using(SKPath path = new SKPath())
			{
				path.MoveTo(points[0].X, points[0].Y);
				for(int i = 1; i < points.Length; i++) path.LineTo(points[i].X, points[i].Y);
				using(SKPaint paint = pen.CreatePaint(Antialias)) canvas.DrawPath(path, paint);
			}
		}

		public void DrawLines(Pen pen, PointF[] points)
		{
			using(SKPath path = new SKPath())
			{
				path.MoveTo(points[0].X, points[0].Y);
				for(int i = 1; i < points.Length; i++) path.LineTo(points[i].X, points[i].Y);
				using(SKPaint paint = pen.CreatePaint(Antialias)) canvas.DrawPath(path, paint);
			}
		}

		public void FillPath(Brush brush, GraphicsPath path)
		{
			using(SKPaint paint = brush.CreatePaint(Antialias, BlendMode))
				canvas.DrawPath(path.Native, paint);
		}

		public void DrawPath(Pen pen, GraphicsPath path)
		{
			using(SKPaint paint = pen.CreatePaint(Antialias))
				canvas.DrawPath(path.Native, paint);
		}

		public void FillEllipse(Brush brush, float x, float y, float width, float height)
		{
			using(SKPaint paint = brush.CreatePaint(Antialias, BlendMode))
				canvas.DrawOval(new SKRect(x, y, x + width, y + height), paint);
		}

		public void FillEllipse(Brush brush, Rectangle rect) => FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);

		public void FillPolygon(Brush brush, Point[] points) => FillPolygon(brush, Array.ConvertAll(points, p => (PointF)p));

		public void FillPolygon(Brush brush, PointF[] points)
		{
			using(SKPath path = new SKPath())
			using(SKPaint paint = brush.CreatePaint(Antialias, BlendMode))
			{
				path.AddPoly(Array.ConvertAll(points, p => new SKPoint(p.X, p.Y)), true);
				canvas.DrawPath(path, paint);
			}
		}

		public void DrawEllipse(Pen pen, float x, float y, float width, float height)
		{
			using(SKPaint paint = pen.CreatePaint(Antialias))
				canvas.DrawOval(new SKRect(x, y, x + width, y + height), paint);
		}

		public void DrawEllipse(Pen pen, Rectangle rect) => DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);

		#endregion

		#region Text

		public SizeF MeasureString(string text, Font font) => font.Measure(text, DpiY);
		public SizeF MeasureString(string text, Font font, int width) => font.Measure(text, DpiY);
		public SizeF MeasureString(string text, Font font, int width, StringFormat format) => font.Measure(text, DpiY);
		public SizeF MeasureString(string text, Font font, SizeF layoutArea) => font.Measure(text, DpiY);
		public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat format) => font.Measure(text, DpiY);

		public void DrawString(string text, Font font, Brush brush, float x, float y)
		{
			DrawString(text, font, brush, new RectangleF(x, y, 0, 0), null);
		}

		public void DrawString(string text, Font font, Brush brush, PointF point) => DrawString(text, font, brush, point.X, point.Y);
		public void DrawString(string text, Font font, Brush brush, RectangleF layout) => DrawString(text, font, brush, layout, null);

		public void DrawString(string text, Font font, Brush brush, RectangleF layout, StringFormat format)
		{
			if(string.IsNullOrEmpty(text)) return;

			using(SKFont skfont = font.CreateSkFont(DpiY))
			using(SKPaint paint = brush.CreatePaint(TextRenderingHint != TextRenderingHint.SingleBitPerPixel && TextRenderingHint != TextRenderingHint.SingleBitPerPixelGridFit, BlendMode))
			{
				skfont.Edging = (TextRenderingHint == TextRenderingHint.SingleBitPerPixel || TextRenderingHint == TextRenderingHint.SingleBitPerPixelGridFit)
					? SKFontEdging.Alias : SKFontEdging.Antialias;

				SKFontMetrics metrics = skfont.Metrics;
				float textwidth = skfont.MeasureText(text);
				float lineheight = metrics.Descent - metrics.Ascent;

				float x = layout.X, y = layout.Y;
				StringAlignment halign = format?.Alignment ?? StringAlignment.Near;
				StringAlignment valign = format?.LineAlignment ?? StringAlignment.Near;
				if(layout.Width > 0)
				{
					if(halign == StringAlignment.Center) x += (layout.Width - textwidth) / 2f;
					else if(halign == StringAlignment.Far) x += layout.Width - textwidth;
				}
				if(layout.Height > 0)
				{
					if(valign == StringAlignment.Center) y += (layout.Height - lineheight) / 2f;
					else if(valign == StringAlignment.Far) y += layout.Height - lineheight;
				}

				bool clip = layout.Width > 0 && layout.Height > 0 && (format == null || (format.FormatFlags & StringFormatFlags.NoClip) == 0);
				if(clip)
				{
					canvas.Save();
					canvas.ClipRect(ToSK(layout));
				}

				canvas.DrawText(text, x, y - metrics.Ascent, SKTextAlign.Left, skfont, paint);

				if(clip) canvas.Restore();
			}
		}

		#endregion

		#region Lifetime

		public void Flush()
		{
			canvas.Flush();
			using(SKBitmap result = Bitmap.FromPremul(surface))
			{
				unsafe
				{
					long bytes = (long)result.RowBytes * result.Height;
					Buffer.MemoryCopy((void*)result.GetPixels(), (void*)target.Native.GetPixels(), bytes, bytes);
				}
			}
			target.Native.NotifyPixelsChanged();
		}

		public void Dispose()
		{
			if(canvas == null) return;
			Flush();
			canvas.Dispose();
			surface.Dispose();
			canvas = null;
			surface = null;
			target = null;
		}

		#endregion

		internal static SKColor ToSK(Color c) => new SKColor(c.R, c.G, c.B, c.A);
		private static SKRect ToSK(RectangleF r) => new SKRect(r.Left, r.Top, r.Right, r.Bottom);
	}

	public abstract class Brush : IDisposable, ICloneable
	{
		internal abstract SKPaint CreatePaint(bool antialias, SKBlendMode blend);
		public abstract object Clone();
		public virtual void Dispose() { }
	}

	public sealed class SolidBrush : Brush
	{
		public SolidBrush(Color color) { Color = color; }

		public Color Color { get; set; }

		internal override SKPaint CreatePaint(bool antialias, SKBlendMode blend)
		{
			return new SKPaint { Color = Graphics.ToSK(Color), IsAntialias = antialias, Style = SKPaintStyle.Fill, BlendMode = blend };
		}

		public override object Clone() => new SolidBrush(Color);
	}

	public static class Pens
	{
		public static Pen Black => new Pen(Color.Black);
		public static Pen White => new Pen(Color.White);
		public static Pen Red => new Pen(Color.Red);
		public static Pen Green => new Pen(Color.Green);
		public static Pen Blue => new Pen(Color.Blue);
		public static Pen Gray => new Pen(Color.Gray);
		public static Pen DarkGray => new Pen(Color.DarkGray);
		public static Pen LightGray => new Pen(Color.LightGray);
		public static Pen Yellow => new Pen(Color.Yellow);
		public static Pen Transparent => new Pen(Color.Transparent);
	}

	/// <summary>Brushes in the system colours (WinForms' SystemBrushes).</summary>
	public static class SystemBrushes
	{
		public static Brush ActiveBorder => new SolidBrush(SystemColors.ActiveBorder);
		public static Brush ActiveCaption => new SolidBrush(SystemColors.ActiveCaption);
		public static Brush ActiveCaptionText => new SolidBrush(SystemColors.ActiveCaptionText);
		public static Brush AppWorkspace => new SolidBrush(SystemColors.AppWorkspace);
		public static Brush ButtonFace => new SolidBrush(SystemColors.ButtonFace);
		public static Brush ButtonHighlight => new SolidBrush(SystemColors.ButtonHighlight);
		public static Brush ButtonShadow => new SolidBrush(SystemColors.ButtonShadow);
		public static Brush Control => new SolidBrush(SystemColors.Control);
		public static Brush ControlDark => new SolidBrush(SystemColors.ControlDark);
		public static Brush ControlDarkDark => new SolidBrush(SystemColors.ControlDarkDark);
		public static Brush ControlLight => new SolidBrush(SystemColors.ControlLight);
		public static Brush ControlLightLight => new SolidBrush(SystemColors.ControlLightLight);
		public static Brush ControlText => new SolidBrush(SystemColors.ControlText);
		public static Brush Desktop => new SolidBrush(SystemColors.Desktop);
		public static Brush GradientActiveCaption => new SolidBrush(SystemColors.GradientActiveCaption);
		public static Brush GradientInactiveCaption => new SolidBrush(SystemColors.GradientInactiveCaption);
		public static Brush GrayText => new SolidBrush(SystemColors.GrayText);
		public static Brush Highlight => new SolidBrush(SystemColors.Highlight);
		public static Brush HighlightText => new SolidBrush(SystemColors.HighlightText);
		public static Brush HotTrack => new SolidBrush(SystemColors.HotTrack);
		public static Brush InactiveBorder => new SolidBrush(SystemColors.InactiveBorder);
		public static Brush InactiveCaption => new SolidBrush(SystemColors.InactiveCaption);
		public static Brush InactiveCaptionText => new SolidBrush(SystemColors.InactiveCaptionText);
		public static Brush Info => new SolidBrush(SystemColors.Info);
		public static Brush InfoText => new SolidBrush(SystemColors.InfoText);
		public static Brush Menu => new SolidBrush(SystemColors.Menu);
		public static Brush MenuBar => new SolidBrush(SystemColors.MenuBar);
		public static Brush MenuHighlight => new SolidBrush(SystemColors.MenuHighlight);
		public static Brush MenuText => new SolidBrush(SystemColors.MenuText);
		public static Brush ScrollBar => new SolidBrush(SystemColors.ScrollBar);
		public static Brush Window => new SolidBrush(SystemColors.Window);
		public static Brush WindowFrame => new SolidBrush(SystemColors.WindowFrame);
		public static Brush WindowText => new SolidBrush(SystemColors.WindowText);
		public static Brush FromSystemColor(Color c) => new SolidBrush(c);
	}

	/// <summary>Pens in the system colours (WinForms' SystemPens).</summary>
	public static class SystemPens
	{
		public static Pen ActiveBorder => new Pen(SystemColors.ActiveBorder);
		public static Pen ActiveCaption => new Pen(SystemColors.ActiveCaption);
		public static Pen ActiveCaptionText => new Pen(SystemColors.ActiveCaptionText);
		public static Pen AppWorkspace => new Pen(SystemColors.AppWorkspace);
		public static Pen ButtonFace => new Pen(SystemColors.ButtonFace);
		public static Pen ButtonHighlight => new Pen(SystemColors.ButtonHighlight);
		public static Pen ButtonShadow => new Pen(SystemColors.ButtonShadow);
		public static Pen Control => new Pen(SystemColors.Control);
		public static Pen ControlDark => new Pen(SystemColors.ControlDark);
		public static Pen ControlDarkDark => new Pen(SystemColors.ControlDarkDark);
		public static Pen ControlLight => new Pen(SystemColors.ControlLight);
		public static Pen ControlLightLight => new Pen(SystemColors.ControlLightLight);
		public static Pen ControlText => new Pen(SystemColors.ControlText);
		public static Pen Desktop => new Pen(SystemColors.Desktop);
		public static Pen GradientActiveCaption => new Pen(SystemColors.GradientActiveCaption);
		public static Pen GradientInactiveCaption => new Pen(SystemColors.GradientInactiveCaption);
		public static Pen GrayText => new Pen(SystemColors.GrayText);
		public static Pen Highlight => new Pen(SystemColors.Highlight);
		public static Pen HighlightText => new Pen(SystemColors.HighlightText);
		public static Pen HotTrack => new Pen(SystemColors.HotTrack);
		public static Pen InactiveBorder => new Pen(SystemColors.InactiveBorder);
		public static Pen InactiveCaption => new Pen(SystemColors.InactiveCaption);
		public static Pen InactiveCaptionText => new Pen(SystemColors.InactiveCaptionText);
		public static Pen Info => new Pen(SystemColors.Info);
		public static Pen InfoText => new Pen(SystemColors.InfoText);
		public static Pen Menu => new Pen(SystemColors.Menu);
		public static Pen MenuBar => new Pen(SystemColors.MenuBar);
		public static Pen MenuHighlight => new Pen(SystemColors.MenuHighlight);
		public static Pen MenuText => new Pen(SystemColors.MenuText);
		public static Pen ScrollBar => new Pen(SystemColors.ScrollBar);
		public static Pen Window => new Pen(SystemColors.Window);
		public static Pen WindowFrame => new Pen(SystemColors.WindowFrame);
		public static Pen WindowText => new Pen(SystemColors.WindowText);
		public static Pen FromSystemColor(Color c) => new Pen(c);
	}

	public static class Brushes
	{
		public static Brush Black => new SolidBrush(Color.Black);
		public static Brush White => new SolidBrush(Color.White);
		public static Brush Transparent => new SolidBrush(Color.Transparent);
		public static Brush Red => new SolidBrush(Color.Red);
		public static Brush Green => new SolidBrush(Color.Green);
		public static Brush Blue => new SolidBrush(Color.Blue);
		public static Brush Yellow => new SolidBrush(Color.Yellow);
		public static Brush Gray => new SolidBrush(Color.Gray);
		public static Brush DarkGray => new SolidBrush(Color.DarkGray);
		public static Brush LightGray => new SolidBrush(Color.LightGray);
		public static Brush Orange => new SolidBrush(Color.Orange);
	}

	public sealed class Pen : IDisposable, ICloneable
	{
		public Pen(Color color) : this(color, 1f) { }

		public Pen(Color color, float width)
		{
			Color = color;
			Width = width;
		}

		public Pen(Brush brush, float width = 1f)
		{
			Color = (brush as SolidBrush)?.Color ?? Color.Black;
			Width = width;
		}

		public Color Color { get; set; }
		public float Width { get; set; }
		public DashStyle DashStyle { get; set; } = DashStyle.Solid;

		internal SKPaint CreatePaint(bool antialias)
		{
			SKPaint paint = new SKPaint { Color = Graphics.ToSK(Color), StrokeWidth = Width, IsAntialias = antialias, Style = SKPaintStyle.Stroke };
			if(DashStyle == DashStyle.Dash) paint.PathEffect = SKPathEffect.CreateDash(new[] { Width * 3, Width }, 0);
			else if(DashStyle == DashStyle.Dot) paint.PathEffect = SKPathEffect.CreateDash(new[] { Width, Width }, 0);
			return paint;
		}

		public object Clone() => new Pen(Color, Width) { DashStyle = DashStyle };
		public void Dispose() { }
	}
}

namespace System.Drawing.Drawing2D
{
	public enum DashStyle
	{
		Solid = 0,
		Dash = 1,
		Dot = 2,
		DashDot = 3,
		DashDotDot = 4,
		Custom = 5
	}

	public sealed class LinearGradientBrush : Brush
	{
		private readonly RectangleF rect;
		private readonly Color color1;
		private readonly Color color2;
		private readonly float angle;

		public LinearGradientBrush(Rectangle rect, Color color1, Color color2, float angle) : this((RectangleF)rect, color1, color2, angle) { }

		public LinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle)
		{
			this.rect = rect;
			this.color1 = color1;
			this.color2 = color2;
			this.angle = angle;
		}

		public LinearGradientBrush(Rectangle rect, Color color1, Color color2, LinearGradientMode mode)
			: this(rect, color1, color2, mode == LinearGradientMode.Horizontal ? 0f : mode == LinearGradientMode.Vertical ? 90f : mode == LinearGradientMode.ForwardDiagonal ? 45f : 135f) { }

		internal override SKPaint CreatePaint(bool antialias, SKBlendMode blend)
		{
			// GDI+ measures the angle clockwise from the x axis, across the rectangle.
			float radians = angle * (float)Math.PI / 180f;
			float cx = rect.X + rect.Width / 2f, cy = rect.Y + rect.Height / 2f;
			float dx = (float)Math.Cos(radians), dy = (float)Math.Sin(radians);
			float half = Math.Abs(dx) * rect.Width / 2f + Math.Abs(dy) * rect.Height / 2f;
			SKPoint start = new SKPoint(cx - dx * half, cy - dy * half);
			SKPoint end = new SKPoint(cx + dx * half, cy + dy * half);

			return new SKPaint
			{
				IsAntialias = antialias,
				BlendMode = blend,
				Style = SKPaintStyle.Fill,
				Shader = SKShader.CreateLinearGradient(start, end, new[] { Graphics.ToSK(color1), Graphics.ToSK(color2) }, SKShaderTileMode.Clamp)
			};
		}

		public override object Clone() => new LinearGradientBrush(rect, color1, color2, angle);
	}

	public sealed class GraphicsPath : IDisposable
	{
		internal readonly SKPath Native = new SKPath();
		private bool started;

		public void AddLine(float x1, float y1, float x2, float y2)
		{
			// GDI+ connects consecutive figures' segments; mirror that.
			if(!started) { Native.MoveTo(x1, y1); started = true; }
			else Native.LineTo(x1, y1);
			Native.LineTo(x2, y2);
		}

		public void AddArc(float x, float y, float width, float height, float startAngle, float sweepAngle)
		{
			Native.ArcTo(new SKRect(x, y, x + width, y + height), startAngle, sweepAngle, !started);
			started = true;
		}

		public void AddRectangle(RectangleF rect)
		{
			Native.AddRect(new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom));
		}

		public void AddEllipse(float x, float y, float width, float height)
		{
			Native.AddOval(new SKRect(x, y, x + width, y + height));
		}

		public void AddEllipse(RectangleF rect) => AddEllipse(rect.X, rect.Y, rect.Width, rect.Height);
		public void AddEllipse(Rectangle rect) => AddEllipse(rect.X, rect.Y, rect.Width, rect.Height);
		public void AddRectangle(Rectangle rect) => AddRectangle((RectangleF)rect);

		public void CloseFigure()
		{
			Native.Close();
			started = false;
		}

		public void Reset()
		{
			Native.Reset();
			started = false;
		}

		public void Dispose() => Native.Dispose();
	}
}
