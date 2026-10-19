using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ULDB.Tests
{
	// Matrix, TextureBrush, ColorMatrix and parallelogram DrawImage, as used by
	// BuilderModes' image exporter. Expected values follow GDI+.
	public class CompatTransformTests
	{
		[Fact]
		public void MatrixPrependsByDefault()
		{
			// GDI+: Rotate then Translate (both prepended) translates first, then rotates.
			using(Matrix m = new Matrix())
			{
				m.Rotate(90);
				m.Translate(10, 0);
				PointF[] p = { new PointF(0, 0) };
				m.TransformPoints(p);
				Assert.Equal(0f, p[0].X, 3);
				Assert.Equal(10f, p[0].Y, 3);
			}
		}

		[Fact]
		public void MatrixAppendAppliesLast()
		{
			using(Matrix m = new Matrix())
			{
				m.Rotate(90);
				m.Translate(10, 0, MatrixOrder.Append);
				PointF[] p = { new PointF(1, 0) };
				m.TransformPoints(p);
				Assert.Equal(10f, p[0].X, 3);
				Assert.Equal(1f, p[0].Y, 3);
			}
		}

		[Fact]
		public void MatrixElementsAreGdiOrder()
		{
			using(Matrix m = new Matrix(1, 2, 3, 4, 5, 6))
				Assert.Equal(new float[] { 1, 2, 3, 4, 5, 6 }, m.Elements);
		}

		[Fact]
		public void TextureBrushTilesWithTransform()
		{
			using(Bitmap tile = new Bitmap(2, 1))
			using(Bitmap dst = new Bitmap(4, 1))
			{
				tile.SetPixel(0, 0, Color.Red);
				tile.SetPixel(1, 0, Color.Blue);
				using(TextureBrush brush = new TextureBrush(tile))
				using(Graphics g = Graphics.FromImage(dst))
				using(GraphicsPath path = new GraphicsPath())
				{
					brush.Transform = new Matrix(1, 0, 0, 1, 1, 0); // Shift the tiling one pixel right
					path.AddRectangle(new RectangleF(0, 0, 4, 1));
					g.FillPath(brush, path);
				}
				Assert.Equal(Color.Blue.ToArgb(), dst.GetPixel(0, 0).ToArgb());
				Assert.Equal(Color.Red.ToArgb(), dst.GetPixel(1, 0).ToArgb());
				Assert.Equal(Color.Blue.ToArgb(), dst.GetPixel(2, 0).ToArgb());
				Assert.Equal(Color.Red.ToArgb(), dst.GetPixel(3, 0).ToArgb());
			}
		}

		[Fact]
		public void ColorMatrixScalesChannels()
		{
			using(Bitmap src = new Bitmap(1, 1))
			using(Bitmap dst = new Bitmap(1, 1))
			{
				src.SetPixel(0, 0, Color.FromArgb(255, 200, 100, 40));
				ColorMatrix cm = new ColorMatrix(new[]
				{
					new float[] { 0.5f, 0, 0, 0, 0 },
					new float[] { 0, 1, 0, 0, 0 },
					new float[] { 0, 0, 2, 0, 0 },
					new float[] { 0, 0, 0, 1, 0 },
					new float[] { 0, 0, 0, 0, 1 },
				});
				ImageAttributes attributes = new ImageAttributes();
				attributes.SetColorMatrix(cm);
				using(Graphics g = Graphics.FromImage(dst))
					g.DrawImage(src, new[] { new Point(0, 0), new Point(1, 0), new Point(0, 1) }, new Rectangle(0, 0, 1, 1), GraphicsUnit.Pixel, attributes);

				Color c = dst.GetPixel(0, 0);
				Assert.InRange(c.R, 99, 101);
				Assert.InRange(c.G, 99, 101);
				Assert.InRange(c.B, 79, 81);
				Assert.Equal(255, c.A);
			}
		}

		[Fact]
		public void ThreePointDrawImageFlipsHorizontally()
		{
			using(Bitmap src = new Bitmap(2, 1))
			using(Bitmap dst = new Bitmap(2, 1))
			{
				src.SetPixel(0, 0, Color.Red);
				src.SetPixel(1, 0, Color.Blue);
				using(Graphics g = Graphics.FromImage(dst))
				{
					g.InterpolationMode = InterpolationMode.NearestNeighbor;
					// Upper-left goes to the right edge, upper-right to the left.
					g.DrawImage(src, new[] { new Point(2, 0), new Point(0, 0), new Point(2, 1) });
				}
				Assert.Equal(Color.Blue.ToArgb(), dst.GetPixel(0, 0).ToArgb());
				Assert.Equal(Color.Red.ToArgb(), dst.GetPixel(1, 0).ToArgb());
			}
		}

		[Fact]
		public void GraphicsPathResetEmptiesThePath()
		{
			using(Bitmap dst = new Bitmap(2, 2))
			using(Graphics g = Graphics.FromImage(dst))
			using(GraphicsPath path = new GraphicsPath())
			{
				path.AddRectangle(new RectangleF(0, 0, 2, 2));
				path.Reset();
				g.FillPath(Brushes.Red, path);
				g.Flush();
				Assert.Equal(0, dst.GetPixel(1, 1).A);
			}
		}
	}
}
