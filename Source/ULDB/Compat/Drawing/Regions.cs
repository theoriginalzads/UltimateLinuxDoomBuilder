// ULDB compatibility layer: System.Drawing.Region and Drawing2D.PathGradientBrush on SkiaSharp
// (ColorPicker's colour wheel uses both).

using System.Drawing.Drawing2D;
using SkiaSharp;

namespace System.Drawing
{
	/// <summary>An area for hit-testing, made from a path or rectangle.</summary>
	public sealed class Region : IDisposable
	{
		internal readonly SKRegion Native = new SKRegion();

		public Region(GraphicsPath path)
		{
			SKRect b = path.Native.Bounds;
			using(SKRegion clip = new SKRegion(SKRectI.Ceiling(b)))
				Native.SetPath(path.Native, clip);
		}

		public Region(Rectangle rect) { Native.SetRect(new SKRectI(rect.Left, rect.Top, rect.Right, rect.Bottom)); }

		public bool IsVisible(Point point) => Native.Contains(point.X, point.Y);
		public bool IsVisible(int x, int y) => Native.Contains(x, y);
		public bool IsVisible(PointF point) => Native.Contains((int)Math.Floor(point.X), (int)Math.Floor(point.Y));

		public void Dispose() => Native.Dispose();
	}
}

namespace System.Drawing.Drawing2D
{
	/// <summary>
	/// GDI+'s path gradient for a polygon: colours blend from CenterColor at CenterPoint to each
	/// point's SurroundColors entry, and along the edges between points (a coloured triangle fan).
	/// </summary>
	public sealed class PathGradientBrush : Brush
	{
		private readonly PointF[] points;

		public PathGradientBrush(Point[] points) : this(Array.ConvertAll(points, p => (PointF)p)) { }

		public PathGradientBrush(PointF[] points)
		{
			this.points = (PointF[])points.Clone();
			float x = 0, y = 0;
			foreach(PointF p in points) { x += p.X; y += p.Y; }
			CenterPoint = points.Length > 0 ? new PointF(x / points.Length, y / points.Length) : PointF.Empty;
			SurroundColors = new[] { Color.White };
		}

		public Color CenterColor { get; set; } = Color.Black;
		public PointF CenterPoint { get; set; }
		public Color[] SurroundColors { get; set; }

		internal override SKPaint CreatePaint(bool antialias, SKBlendMode blend)
		{
			// Draw the fan once into a picture and fill with it; outside the polygon is transparent.
			int n = points.Length;
			SKPoint[] verts = new SKPoint[n * 3];
			SKColor[] colours = new SKColor[n * 3];
			for(int i = 0; i < n; i++)
			{
				int j = (i + 1) % n;
				verts[i * 3] = new SKPoint(CenterPoint.X, CenterPoint.Y);
				verts[i * 3 + 1] = new SKPoint(points[i].X, points[i].Y);
				verts[i * 3 + 2] = new SKPoint(points[j].X, points[j].Y);
				colours[i * 3] = Graphics.ToSK(CenterColor);
				colours[i * 3 + 1] = Graphics.ToSK(Surround(i));
				colours[i * 3 + 2] = Graphics.ToSK(Surround(j));
			}

			SKRect bounds = SKRect.Create(0, 0, 1, 1);
			foreach(SKPoint p in verts) bounds = SKRect.Union(bounds, new SKRect(p.X, p.Y, p.X + 1, p.Y + 1));
			using(SKPictureRecorder recorder = new SKPictureRecorder())
			{
				SKCanvas c = recorder.BeginRecording(bounds);
				using(SKPaint fan = new SKPaint { IsAntialias = antialias, Color = SKColors.White }) // Vertex colours are multiplied by this
					c.DrawVertices(SKVertexMode.Triangles, verts, null, colours, fan);
				using(SKPicture picture = recorder.EndRecording())
				{
					return new SKPaint
					{
						IsAntialias = antialias,
						BlendMode = blend,
						Style = SKPaintStyle.Fill,
						Shader = picture.ToShader(SKShaderTileMode.Decal, SKShaderTileMode.Decal, SKFilterMode.Linear, SKMatrix.Identity, bounds),
					};
				}
			}
		}

		private Color Surround(int i) => SurroundColors == null || SurroundColors.Length == 0 ? Color.White : SurroundColors[Math.Min(i, SurroundColors.Length - 1)];

		public override object Clone() => new PathGradientBrush(points) { CenterColor = CenterColor, CenterPoint = CenterPoint, SurroundColors = (Color[])SurroundColors?.Clone() };
	}
}
