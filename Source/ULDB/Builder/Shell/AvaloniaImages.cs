// ULDB: converts upstream's System.Drawing images (ULDB.Compat bitmaps) to Avalonia
// bitmaps for menus, toolbars and labels. Each image is converted once.

using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SD = System.Drawing;
using SDI = System.Drawing.Imaging;

namespace ULDB.Shell
{
	internal static class AvaloniaImages
	{
		private static readonly ConditionalWeakTable<SD.Image, Bitmap> cache = new ConditionalWeakTable<SD.Image, Bitmap>();

		/// <summary>The image as an Avalonia bitmap, or null for no image.</summary>
		public static Bitmap From(SD.Image image)
		{
			if(image == null || image.Width <= 0 || image.Height <= 0) return null;
			if(cache.TryGetValue(image, out Bitmap converted)) return converted;
			converted = Convert((SD.Bitmap)image);
			cache.AddOrUpdate(image, converted);
			return converted;
		}

		public static Bitmap From(SD.Icon icon) => icon == null ? null : From(icon.ToBitmap());

		private static unsafe Bitmap Convert(SD.Bitmap source)
		{
			// Compat bitmaps hold GDI+'s Format32bppArgb: unpremultiplied BGRA, which Avalonia takes as is.
			SD.Rectangle rect = new SD.Rectangle(0, 0, source.Width, source.Height);
			SDI.BitmapData data = source.LockBits(rect, SDI.ImageLockMode.ReadOnly, SDI.PixelFormat.Format32bppArgb);
			try
			{
				WriteableBitmap result = new WriteableBitmap(new PixelSize(source.Width, source.Height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);
				using(ILockedFramebuffer fb = result.Lock())
				{
					int rowbytes = source.Width * 4;
					for(int y = 0; y < source.Height; y++)
						System.Buffer.MemoryCopy((byte*)data.Scan0 + y * data.Stride, (byte*)fb.Address + y * fb.RowBytes, fb.RowBytes, rowbytes);
				}
				return result;
			}
			finally
			{
				source.UnlockBits(data);
			}
		}
	}
}
