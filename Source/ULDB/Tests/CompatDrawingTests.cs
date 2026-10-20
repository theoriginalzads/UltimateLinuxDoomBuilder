using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ULDB.Tests
{
	// ULDB.Compat's System.Drawing must behave like GDI+ where UDB depends on it:
	// pixel memory layout, LockBits, RotateFlip and resource image loading.
	public class CompatDrawingTests
	{
		[Fact]
		public void NewBitmapIsTransparentBlack()
		{
			using(Bitmap b = new Bitmap(4, 4))
				Assert.Equal(Color.FromArgb(0, 0, 0, 0).ToArgb(), b.GetPixel(2, 2).ToArgb());
		}

		[Fact]
		public void LockBitsUsesGdiArgbLayout()
		{
			using(Bitmap b = new Bitmap(2, 1))
			{
				// Format32bppArgb is little-endian ARGB: bytes B, G, R, A in memory.
				BitmapData data = b.LockBits(new Rectangle(0, 0, 2, 1), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
				Marshal.Copy(new byte[] { 10, 20, 30, 128, 1, 2, 3, 255 }, 0, data.Scan0, 8);
				b.UnlockBits(data);

				Assert.Equal(Color.FromArgb(128, 30, 20, 10), b.GetPixel(0, 0));
				Assert.Equal(Color.FromArgb(255, 3, 2, 1), b.GetPixel(1, 0));
			}
		}

		[Fact]
		public void OpaquePixelsSurviveDrawingExactly()
		{
			// Graphics draws through a premultiplied copy; opaque pixels must come back unchanged.
			using(Bitmap src = new Bitmap(1, 1))
			using(Bitmap dst = new Bitmap(1, 1))
			{
				src.SetPixel(0, 0, Color.FromArgb(255, 200, 100, 50));
				using(Graphics g = Graphics.FromImage(dst)) g.DrawImage(src, 0, 0);
				Assert.Equal(Color.FromArgb(255, 200, 100, 50), dst.GetPixel(0, 0));
			}
		}

		[Theory]
		[InlineData(RotateFlipType.Rotate90FlipNone, 2, 0)]   // GDI+ rotates clockwise; the result is 3x2
		[InlineData(RotateFlipType.Rotate180FlipNone, 1, 2)]
		[InlineData(RotateFlipType.Rotate270FlipNone, 0, 1)]
		[InlineData(RotateFlipType.RotateNoneFlipX, 1, 0)]
		[InlineData(RotateFlipType.RotateNoneFlipY, 0, 2)]
		public void RotateFlipMatchesGdi(RotateFlipType type, int x, int y)
		{
			// A 2x3 image with one red pixel at the top-left corner.
			using(Bitmap b = new Bitmap(2, 3))
			{
				b.SetPixel(0, 0, Color.Red);
				b.RotateFlip(type);
				Assert.Equal(Color.Red.ToArgb(), b.GetPixel(x, y).ToArgb());
			}
		}

		[Fact]
		public void BuiltInResourceImagesLoad()
		{
			// Generated Properties.Resources replacement (tools/resxgen).
			using(Bitmap unknown = CodeImp.DoomBuilder.Properties.Resources.UnknownImage)
			{
				Assert.True(unknown.Width > 0 && unknown.Height > 0);
			}
		}

		[Fact]
		public void TextRendersPixels()
		{
			// Map text labels are drawn this way (Rendering/TextLabel.cs).
			using(Bitmap b = new Bitmap(64, 24))
			using(Font font = new Font(FontFamily.GenericSansSerif, 12f, FontStyle.Bold))
			using(SolidBrush brush = new SolidBrush(Color.White))
			{
				using(Graphics g = Graphics.FromImage(b))
				{
					StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
					g.DrawString("UZD", font, brush, new RectangleF(0, 0, 64, 24), sf);
				}

				int lit = 0;
				for(int y = 0; y < b.Height; y++)
					for(int x = 0; x < b.Width; x++)
						if(b.GetPixel(x, y).A > 128) lit++;
				Assert.True(lit > 20, "Expected the text to cover some pixels, got " + lit);
			}
		}
			[Fact]
		public void PathGradientBrushBlendsFromCentreToEdges()
		{
			// ColorPicker's colour wheel: white in the middle, each surround colour at its point.
			Point[] square = { new Point(0, 0), new Point(20, 0), new Point(20, 20), new Point(0, 20) };
			using(Bitmap b = new Bitmap(20, 20))
			using(System.Drawing.Drawing2D.PathGradientBrush brush = new System.Drawing.Drawing2D.PathGradientBrush(square))
			{
				brush.CenterColor = Color.White;
				brush.CenterPoint = new PointF(10, 10);
				brush.SurroundColors = new[] { Color.Red, Color.Red, Color.Blue, Color.Blue };
				using(Graphics g = Graphics.FromImage(b))
					g.FillEllipse(brush, 0, 0, 20, 20);

				Color centre = b.GetPixel(10, 10), top = b.GetPixel(10, 1), bottom = b.GetPixel(10, 18), corner = b.GetPixel(0, 0);
				Assert.True(centre.R > 230 && centre.G > 230 && centre.B > 230, "centre " + centre);
				Assert.True(top.R > 200 && top.B < 60, "top " + top);
				Assert.True(bottom.B > 200 && bottom.R < 60, "bottom " + bottom);
				Assert.Equal(0, corner.A); // Outside the ellipse
			}
		}

		[Fact]
		public void RegionHitTestsItsPath()
		{
			using(System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
			{
				path.AddEllipse(new Rectangle(0, 0, 20, 20));
				using(Region r = new Region(path))
				{
					Assert.True(r.IsVisible(new Point(10, 10)));
					Assert.False(r.IsVisible(new Point(1, 1)));
					Assert.False(r.IsVisible(new Point(30, 10)));
				}
			}
		}
	}
}
