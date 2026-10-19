// ULDB compatibility layer: System.Drawing images implemented on SkiaSharp.
// Covers the subset of the GDI+ API used by UDB. Pixels are stored as
// unpremultiplied BGRA, the same memory layout as GDI+ Format32bppArgb, so
// LockBits hands out the image memory directly with no conversion.

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace System.Drawing
{
	public abstract class Image : IDisposable, ICloneable
	{
		internal SKBitmap skbitmap;
		private PixelFormat pixelformat;
		private float hresolution = 96f;
		private float vresolution = 96f;

		internal Image(SKBitmap bitmap, PixelFormat format)
		{
			skbitmap = bitmap;
			pixelformat = format;
		}

		public int Width => Native.Width;
		public int Height => Native.Height;
		public Size Size => new Size(Width, Height);
		public SizeF PhysicalDimension => new SizeF(Width, Height);
		public PixelFormat PixelFormat => pixelformat;
		public float HorizontalResolution => hresolution;
		public float VerticalResolution => vresolution;
		public object Tag { get; set; }

		internal SKBitmap Native
		{
			get
			{
				if(skbitmap == null) throw new ObjectDisposedException(GetType().Name);
				return skbitmap;
			}
		}

		public void SetResolution(float xDpi, float yDpi)
		{
			hresolution = xDpi;
			vresolution = yDpi;
		}

		public static Image FromStream(Stream stream) => Bitmap.Decode(stream);
		public static Image FromStream(Stream stream, bool useEmbeddedColorManagement) => Bitmap.Decode(stream);
		public static Image FromStream(Stream stream, bool useEmbeddedColorManagement, bool validateImageData) => Bitmap.Decode(stream);

		public static Image FromFile(string filename)
		{
			using(FileStream fs = File.OpenRead(filename)) return Bitmap.Decode(fs);
		}

		public void RotateFlip(RotateFlipType type)
		{
			SKBitmap result = Bitmap.RotateFlip(Native, type);
			if(result != skbitmap)
			{
				skbitmap.Dispose();
				skbitmap = result;
			}
		}

		public Image GetThumbnailImage(int thumbWidth, int thumbHeight, GetThumbnailImageAbort callback, IntPtr callbackData)
		{
			return new Bitmap(this, thumbWidth, thumbHeight);
		}

		public delegate bool GetThumbnailImageAbort();

		public void Save(string filename) => Save(filename, ImageFormat.Png);

		public void Save(string filename, ImageFormat format)
		{
			using(FileStream fs = File.Create(filename)) Save(fs, format);
		}

		public void Save(Stream stream, ImageFormat format)
		{
			SKEncodedImageFormat skformat = format.SkiaFormat;
			int quality = (skformat == SKEncodedImageFormat.Jpeg) ? 90 : 100;
			using(SKBitmap premul = Bitmap.ToPremul(Native))
			using(SKData data = premul.Encode(skformat, quality))
			{
				if(data == null) throw new NotSupportedException("Image format " + format + " can't be encoded");
				data.SaveTo(stream);
			}
		}

		public virtual object Clone() => new Bitmap(this);

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			skbitmap?.Dispose();
			skbitmap = null;
		}
	}

	public sealed class Bitmap : Image
	{
		private const SKColorType COLOR_TYPE = SKColorType.Bgra8888;

		#region Constructors

		public Bitmap(int width, int height) : this(width, height, PixelFormat.Format32bppArgb) { }

		public Bitmap(int width, int height, PixelFormat format) : base(CreateNative(width, height), Normalize(format))
		{
			// GDI+ initializes new bitmaps to transparent black; Skia's memory is undefined.
			Native.Erase(SKColors.Transparent);
		}

		// GDI+ wraps the caller's memory here. ULDB copies it instead, which is safe
		// for UDB's usage (the source buffers are temporary) and avoids dangling pointers.
		public Bitmap(int width, int height, int stride, PixelFormat format, IntPtr scan0)
			: base(CreateNative(width, height), Normalize(format))
		{
			CopyIn(scan0, stride, Native, format);
		}

		public Bitmap(Image original) : base(CopyNative(original.Native), original.PixelFormat)
		{
			SetResolution(original.HorizontalResolution, original.VerticalResolution);
		}

		public Bitmap(Image original, int width, int height) : base(ResizeNative(original.Native, width, height), PixelFormat.Format32bppArgb) { }

		public Bitmap(Image original, Size newSize) : this(original, newSize.Width, newSize.Height) { }

		public Bitmap(Stream stream) : base(DecodeNative(stream), PixelFormat.Format32bppArgb) { }

		public Bitmap(string filename) : base(DecodeFile(filename), PixelFormat.Format32bppArgb) { }

		private Bitmap(SKBitmap native, PixelFormat format) : base(native, format) { }

		#endregion

		#region Pixels

		public Color GetPixel(int x, int y)
		{
			CheckBounds(x, y);
			uint argb = ReadPixel(x, y);
			if(PixelFormat == PixelFormat.Format32bppRgb) argb |= 0xFF000000;
			return Color.FromArgb(unchecked((int)argb));
		}

		public void SetPixel(int x, int y, Color color)
		{
			CheckBounds(x, y);
			unsafe
			{
				uint* row = (uint*)((byte*)Native.GetPixels() + (y * Native.RowBytes));
				row[x] = unchecked((uint)color.ToArgb());
			}
		}

		public BitmapData LockBits(Rectangle rect, ImageLockMode flags, PixelFormat format)
		{
			if(!Is32Bit(format)) throw new NotSupportedException("ULDB only supports 32-bit LockBits, requested " + format);
			if(rect.X < 0 || rect.Y < 0 || rect.Right > Width || rect.Bottom > Height) throw new ArgumentException("Rectangle is outside the bitmap", nameof(rect));

			IntPtr scan0 = Native.GetPixels() + (rect.Y * Native.RowBytes) + (rect.X * 4);
			return new BitmapData
			{
				Width = rect.Width,
				Height = rect.Height,
				Stride = Native.RowBytes,
				PixelFormat = format,
				Scan0 = scan0,
				LockMode = flags
			};
		}

		public BitmapData LockBits(Rectangle rect, ImageLockMode flags, PixelFormat format, BitmapData bitmapData)
		{
			BitmapData result = LockBits(rect, flags, format);
			bitmapData.Width = result.Width;
			bitmapData.Height = result.Height;
			bitmapData.Stride = result.Stride;
			bitmapData.PixelFormat = result.PixelFormat;
			bitmapData.Scan0 = result.Scan0;
			bitmapData.LockMode = result.LockMode;
			return bitmapData;
		}

		public void UnlockBits(BitmapData data)
		{
			// Memory is shared with the bitmap, so writes are already in place.
			// Skia caches some state per bitmap; tell it the pixels changed.
			if(data.LockMode != ImageLockMode.ReadOnly) Native.NotifyPixelsChanged();
		}

		public void MakeTransparent(Color transparentColor)
		{
			uint key = unchecked((uint)transparentColor.ToArgb()) | 0xFF000000;
			unsafe
			{
				for(int y = 0; y < Height; y++)
				{
					uint* row = (uint*)((byte*)Native.GetPixels() + (y * Native.RowBytes));
					for(int x = 0; x < Width; x++)
						if((row[x] | 0xFF000000) == key) row[x] = 0;
				}
			}
		}

		public Bitmap Clone(Rectangle rect, PixelFormat format)
		{
			SKBitmap result = CreateNative(rect.Width, rect.Height);
			Native.ExtractSubset(result, new SKRectI(rect.Left, rect.Top, rect.Right, rect.Bottom));
			using(SKBitmap subset = result)
				return new Bitmap(CopyNative(subset), Normalize(format));
		}

		public override object Clone() => new Bitmap(this);

		public IntPtr GetHbitmap() => throw new PlatformNotSupportedException("HBITMAP is a Windows-only concept");

		#endregion

		#region Helpers

		private uint ReadPixel(int x, int y)
		{
			unsafe
			{
				uint* row = (uint*)((byte*)Native.GetPixels() + (y * Native.RowBytes));
				return row[x];
			}
		}

		private void CheckBounds(int x, int y)
		{
			if(x < 0 || y < 0 || x >= Width || y >= Height) throw new ArgumentOutOfRangeException(x < 0 || x >= Width ? nameof(x) : nameof(y));
		}

		private static bool Is32Bit(PixelFormat format)
		{
			return format == PixelFormat.Format32bppArgb || format == PixelFormat.Format32bppRgb || format == PixelFormat.Format32bppPArgb;
		}

		private static PixelFormat Normalize(PixelFormat format)
		{
			// Every ULDB bitmap is 32-bit BGRA in memory. Keep the "no alpha" distinction
			// GDI+ makes for Format32bppRgb, and treat everything else as ARGB.
			return format == PixelFormat.Format32bppRgb ? PixelFormat.Format32bppRgb : PixelFormat.Format32bppArgb;
		}

		internal static SKBitmap CreateNative(int width, int height)
		{
			if(width <= 0 || height <= 0) throw new ArgumentException("Bitmap size must be positive, got " + width + "x" + height);
			SKBitmap bitmap = new SKBitmap(new SKImageInfo(width, height, COLOR_TYPE, SKAlphaType.Unpremul));
			if(bitmap.GetPixels() == IntPtr.Zero) throw new OutOfMemoryException("Unable to allocate a " + width + "x" + height + " bitmap");
			return bitmap;
		}

		internal static SKBitmap CopyNative(SKBitmap source)
		{
			SKBitmap copy = CreateNative(source.Width, source.Height);
			unsafe
			{
				long bytes = (long)source.RowBytes * source.Height;
				if(source.RowBytes == copy.RowBytes)
					Buffer.MemoryCopy((void*)source.GetPixels(), (void*)copy.GetPixels(), bytes, bytes);
				else
					for(int y = 0; y < source.Height; y++)
						Buffer.MemoryCopy((byte*)source.GetPixels() + y * source.RowBytes, (byte*)copy.GetPixels() + y * copy.RowBytes, copy.RowBytes, source.Width * 4);
			}
			return copy;
		}

		private static void CopyIn(IntPtr scan0, int stride, SKBitmap target, PixelFormat format)
		{
			int rowbytes = target.Width * 4;
			unsafe
			{
				for(int y = 0; y < target.Height; y++)
				{
					byte* src = (byte*)scan0 + (long)y * stride;
					byte* dst = (byte*)target.GetPixels() + (long)y * target.RowBytes;
					Buffer.MemoryCopy(src, dst, rowbytes, rowbytes);
				}
			}
			if(format == PixelFormat.Format32bppPArgb)
			{
				using(SKBitmap view = ToPremulView(target))
				using(SKBitmap unpremul = FromPremul(view))
				unsafe
				{
					long bytes = (long)target.RowBytes * target.Height;
					Buffer.MemoryCopy((void*)unpremul.GetPixels(), (void*)target.GetPixels(), bytes, bytes);
				}
			}
		}

		private static SKBitmap ToPremulView(SKBitmap source)
		{
			// Reinterpret already-premultiplied memory as premultiplied.
			SKBitmap view = new SKBitmap();
			view.InstallPixels(new SKImageInfo(source.Width, source.Height, COLOR_TYPE, SKAlphaType.Premul), source.GetPixels(), source.RowBytes);
			return view;
		}

		/// <summary>Returns a premultiplied copy of an unpremultiplied ULDB bitmap, for drawing with Skia.</summary>
		internal static SKBitmap ToPremul(SKBitmap source)
		{
			SKBitmap premul = new SKBitmap(new SKImageInfo(source.Width, source.Height, COLOR_TYPE, SKAlphaType.Premul));
			if(!source.CopyTo(premul, COLOR_TYPE) || premul.AlphaType != SKAlphaType.Premul)
			{
				// Fall back to a pixmap read, which converts alpha types.
				premul.Dispose();
				premul = new SKBitmap(new SKImageInfo(source.Width, source.Height, COLOR_TYPE, SKAlphaType.Premul));
				source.PeekPixels().ReadPixels(premul.Info, premul.GetPixels(), premul.RowBytes, 0, 0);
			}
			return premul;
		}

		/// <summary>Converts a premultiplied Skia bitmap back to ULDB's unpremultiplied storage.</summary>
		internal static SKBitmap FromPremul(SKBitmap premul)
		{
			SKBitmap result = CreateNative(premul.Width, premul.Height);
			premul.PeekPixels().ReadPixels(result.Info, result.GetPixels(), result.RowBytes, 0, 0);
			return result;
		}

		internal static Bitmap Decode(Stream stream) => new Bitmap(DecodeNative(stream), PixelFormat.Format32bppArgb);

		private static SKBitmap DecodeFile(string filename)
		{
			using(FileStream fs = File.OpenRead(filename)) return DecodeNative(fs);
		}

		private static SKBitmap DecodeNative(Stream stream)
		{
			// SKCodec needs a seekable stream; UDB often passes wrapped or partial streams.
			MemoryStream ms = new MemoryStream();
			stream.CopyTo(ms);
			ms.Position = 0;

			using(SKCodec codec = SKCodec.Create(ms))
			{
				if(codec == null) throw new ArgumentException("Parameter is not valid.", nameof(stream)); // Same message as GDI+
				SKImageInfo info = new SKImageInfo(codec.Info.Width, codec.Info.Height, COLOR_TYPE, SKAlphaType.Unpremul);
				SKBitmap bitmap = CreateNative(info.Width, info.Height);
				SKCodecResult result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());
				if(result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
				{
					bitmap.Dispose();
					throw new ArgumentException("Parameter is not valid. (" + result + ")", nameof(stream));
				}
				return bitmap;
			}
		}

		private static SKBitmap ResizeNative(SKBitmap source, int width, int height)
		{
			using(SKBitmap premul = ToPremul(source))
			using(SKBitmap scaled = premul.Resize(new SKImageInfo(width, height, COLOR_TYPE, SKAlphaType.Premul), new SKSamplingOptions(SKCubicResampler.Mitchell)))
				return FromPremul(scaled);
		}

		internal static SKBitmap RotateFlip(SKBitmap source, RotateFlipType type)
		{
			int rotate = 0;
			bool flipx = false;
			switch(type)
			{
				case RotateFlipType.RotateNoneFlipNone: return source;
				case RotateFlipType.Rotate90FlipNone: rotate = 90; break;
				case RotateFlipType.Rotate180FlipNone: rotate = 180; break;
				case RotateFlipType.Rotate270FlipNone: rotate = 270; break;
				case RotateFlipType.RotateNoneFlipX: flipx = true; break;
				case RotateFlipType.Rotate90FlipX: rotate = 90; flipx = true; break;
				case RotateFlipType.Rotate180FlipX: rotate = 180; flipx = true; break; // Same as RotateNoneFlipY
				case RotateFlipType.Rotate270FlipX: rotate = 270; flipx = true; break;
			}

			int w = source.Width, h = source.Height;
			bool swap = (rotate == 90 || rotate == 270);
			SKBitmap result = CreateNative(swap ? h : w, swap ? w : h);
			int rw = result.Width, rh = result.Height;

			unsafe
			{
				for(int y = 0; y < h; y++)
				{
					uint* srcrow = (uint*)((byte*)source.GetPixels() + y * source.RowBytes);
					for(int x = 0; x < w; x++)
					{
						// GDI+ rotates clockwise first, then flips horizontally.
						int tx, ty;
						switch(rotate)
						{
							case 90: tx = h - 1 - y; ty = x; break;
							case 180: tx = w - 1 - x; ty = h - 1 - y; break;
							case 270: tx = y; ty = w - 1 - x; break;
							default: tx = x; ty = y; break;
						}
						if(flipx) tx = rw - 1 - tx;
						uint* dstrow = (uint*)((byte*)result.GetPixels() + ty * result.RowBytes);
						dstrow[tx] = srcrow[x];
					}
				}
			}
			return result;
		}

		#endregion
	}

	public sealed class Icon : IDisposable, ICloneable
	{
		private readonly Bitmap bitmap;

		public Icon(Stream stream) { bitmap = new Bitmap(stream); }
		public Icon(string filename) { bitmap = new Bitmap(filename); }
		internal Icon(Bitmap bitmap) { this.bitmap = bitmap; }

		public int Width => bitmap.Width;
		public int Height => bitmap.Height;
		public Size Size => bitmap.Size;

		public Bitmap ToBitmap() => new Bitmap(bitmap);

		// Windows extracts the icon embedded in an .exe. Linux executables don't carry
		// icons, so return null and callers fall back to their default image.
		public static Icon ExtractAssociatedIcon(string filePath) => null;

		public object Clone() => new Icon(new Bitmap(bitmap));
		public void Dispose() => bitmap.Dispose();
	}
}

namespace System.Drawing.Imaging
{
	public sealed class BitmapData
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public int Stride { get; set; }
		public PixelFormat PixelFormat { get; set; }
		public IntPtr Scan0 { get; set; }
		public int Reserved { get; set; }
		internal ImageLockMode LockMode { get; set; }
	}

	public sealed class ImageFormat
	{
		internal readonly SKEncodedImageFormat SkiaFormat;
		private readonly string name;

		private ImageFormat(string name, SKEncodedImageFormat format)
		{
			this.name = name;
			SkiaFormat = format;
		}

		public static ImageFormat Png { get; } = new ImageFormat("Png", SKEncodedImageFormat.Png);
		public static ImageFormat Jpeg { get; } = new ImageFormat("Jpeg", SKEncodedImageFormat.Jpeg);
		public static ImageFormat Bmp { get; } = new ImageFormat("Bmp", SKEncodedImageFormat.Bmp);
		public static ImageFormat Gif { get; } = new ImageFormat("Gif", SKEncodedImageFormat.Gif);
		public static ImageFormat Webp { get; } = new ImageFormat("Webp", SKEncodedImageFormat.Webp);

		public override string ToString() => name;
	}

	public sealed class ImageAttributes : IDisposable
	{
		internal WrapMode WrapMode { get; private set; } = WrapMode.Clamp;

		public void SetWrapMode(WrapMode mode) { WrapMode = mode; }
		public void SetWrapMode(WrapMode mode, Color color) { WrapMode = mode; }
		public void Dispose() { }
	}
}
