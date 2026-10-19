// ULDB compatibility layer: System.Drawing.Drawing2D.Matrix and TextureBrush on SkiaSharp.

using System.Drawing.Drawing2D;
using SkiaSharp;

namespace System.Drawing.Drawing2D
{
	/// <summary>
	/// A 3x2 affine matrix. GDI+ multiplies row vectors; MatrixOrder.Prepend (the default)
	/// applies the new operation before the existing transform, which is Skia's PreConcat.
	/// </summary>
	public sealed class Matrix : IDisposable
	{
		internal SKMatrix Native;

		public Matrix() { Native = SKMatrix.Identity; }

		public Matrix(float m11, float m12, float m21, float m22, float dx, float dy)
		{
			Native = new SKMatrix(m11, m21, dx, m12, m22, dy, 0, 0, 1);
		}

		private Matrix(SKMatrix m) { Native = m; }

		/// <summary>m11, m12, m21, m22, dx, dy, as in GDI+.</summary>
		public float[] Elements => new[] { Native.ScaleX, Native.SkewY, Native.SkewX, Native.ScaleY, Native.TransX, Native.TransY };
		public float OffsetX => Native.TransX;
		public float OffsetY => Native.TransY;
		public bool IsIdentity => Native.IsIdentity;
		public bool IsInvertible => Native.IsInvertible;

		public void Reset() => Native = SKMatrix.Identity;
		public void Translate(float offsetX, float offsetY) => Translate(offsetX, offsetY, MatrixOrder.Prepend);
		public void Translate(float offsetX, float offsetY, MatrixOrder order) => Apply(SKMatrix.CreateTranslation(offsetX, offsetY), order);
		public void Scale(float scaleX, float scaleY) => Scale(scaleX, scaleY, MatrixOrder.Prepend);
		public void Scale(float scaleX, float scaleY, MatrixOrder order) => Apply(SKMatrix.CreateScale(scaleX, scaleY), order);
		public void Rotate(float angle) => Rotate(angle, MatrixOrder.Prepend);
		public void Rotate(float angle, MatrixOrder order) => Apply(SKMatrix.CreateRotationDegrees(angle), order);
		public void RotateAt(float angle, PointF point) => RotateAt(angle, point, MatrixOrder.Prepend);
		public void RotateAt(float angle, PointF point, MatrixOrder order) => Apply(SKMatrix.CreateRotationDegrees(angle, point.X, point.Y), order);
		public void Shear(float shearX, float shearY) => Shear(shearX, shearY, MatrixOrder.Prepend);
		public void Shear(float shearX, float shearY, MatrixOrder order) => Apply(SKMatrix.CreateSkew(shearX, shearY), order);
		public void Multiply(Matrix matrix) => Multiply(matrix, MatrixOrder.Prepend);
		public void Multiply(Matrix matrix, MatrixOrder order) => Apply(matrix.Native, order);

		public void Invert()
		{
			if(Native.TryInvert(out SKMatrix inverse)) Native = inverse;
			else throw new ArgumentException("Matrix is not invertible");
		}

		public void TransformPoints(PointF[] pts)
		{
			for(int i = 0; i < pts.Length; i++)
			{
				SKPoint p = Native.MapPoint(pts[i].X, pts[i].Y);
				pts[i] = new PointF(p.X, p.Y);
			}
		}

		public void TransformPoints(Point[] pts)
		{
			for(int i = 0; i < pts.Length; i++)
			{
				SKPoint p = Native.MapPoint(pts[i].X, pts[i].Y);
				pts[i] = new Point((int)Math.Round(p.X), (int)Math.Round(p.Y));
			}
		}

		private void Apply(SKMatrix m, MatrixOrder order)
		{
			Native = order == MatrixOrder.Append ? Native.PostConcat(m) : Native.PreConcat(m);
		}

		public Matrix Clone() => new Matrix(Native);
		public void Dispose() { }
	}
}

namespace System.Drawing
{
	/// <summary>Fills with a tiled image, positioned by Transform.</summary>
	public sealed class TextureBrush : Brush
	{
		private readonly Image image;
		private Matrix transform = new Matrix();

		public TextureBrush(Image image) : this(image, WrapMode.Tile) { }

		public TextureBrush(Image image, WrapMode wrapMode)
		{
			this.image = (Image)(image ?? throw new ArgumentNullException(nameof(image))).Clone();
			WrapMode = wrapMode;
		}

		public Image Image => image;
		public WrapMode WrapMode { get; set; }

		public Matrix Transform
		{
			get => transform.Clone();
			set => transform = (value ?? throw new ArgumentNullException(nameof(value))).Clone();
		}

		public void ResetTransform() => transform.Reset();
		public void TranslateTransform(float dx, float dy) => transform.Translate(dx, dy);
		public void TranslateTransform(float dx, float dy, MatrixOrder order) => transform.Translate(dx, dy, order);
		public void RotateTransform(float angle) => transform.Rotate(angle);
		public void RotateTransform(float angle, MatrixOrder order) => transform.Rotate(angle, order);
		public void ScaleTransform(float sx, float sy) => transform.Scale(sx, sy);
		public void ScaleTransform(float sx, float sy, MatrixOrder order) => transform.Scale(sx, sy, order);
		public void MultiplyTransform(Matrix matrix) => transform.Multiply(matrix);
		public void MultiplyTransform(Matrix matrix, MatrixOrder order) => transform.Multiply(matrix, order);

		internal override SKPaint CreatePaint(bool antialias, SKBlendMode blend)
		{
			SKShaderTileMode tx, ty;
			switch(WrapMode)
			{
				case WrapMode.TileFlipX: tx = SKShaderTileMode.Mirror; ty = SKShaderTileMode.Repeat; break;
				case WrapMode.TileFlipY: tx = SKShaderTileMode.Repeat; ty = SKShaderTileMode.Mirror; break;
				case WrapMode.TileFlipXY: tx = ty = SKShaderTileMode.Mirror; break;
				case WrapMode.Clamp: tx = ty = SKShaderTileMode.Decal; break;
				default: tx = ty = SKShaderTileMode.Repeat; break;
			}

			using(SKBitmap premul = Bitmap.ToPremul(image.Native))
			using(SKImage skimage = SKImage.FromBitmap(premul))
			{
				return new SKPaint
				{
					IsAntialias = antialias,
					BlendMode = blend,
					Style = SKPaintStyle.Fill,
					Shader = skimage.ToShader(tx, ty, new SKSamplingOptions(SKFilterMode.Linear), transform.Native),
				};
			}
		}

		public override object Clone()
		{
			TextureBrush b = new TextureBrush(image, WrapMode);
			b.transform = transform.Clone();
			return b;
		}

		public override void Dispose()
		{
			image.Dispose();
			base.Dispose();
		}
	}
}
