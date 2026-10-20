// Renders ULDB's Breeze-style icon specs (from specs.py) to light and dark preview sheets,
// each icon at 16 and 32 px, drawn the way Shell/BreezeIcons draws them.
// Usage: iconsheet SPECS_TSV OUT_PREFIX [KEY_PREFIX]

using System.Globalization;
using SkiaSharp;

var icons = File.ReadAllLines(args[0]).Where(l => l.Length > 0).Select(l => l.Split('\t'))
	.Where(p => args.Length < 3 || p[0].StartsWith(args[2], StringComparison.OrdinalIgnoreCase)).ToList();
const int COLS = 8, CW = 150, CH = 64;
int rows = (icons.Count + COLS - 1) / COLS;

foreach(bool dark in new[] { false, true })
{
	using SKSurface surface = SKSurface.Create(new SKImageInfo(COLS * CW, Math.Max(rows, 1) * CH));
	SKCanvas c = surface.Canvas;
	c.Clear(dark ? new SKColor(0x2a, 0x2e, 0x32) : new SKColor(0xef, 0xf0, 0xf1));
	SKColor ink = dark ? new SKColor(0xfc, 0xfc, 0xfc) : new SKColor(0x23, 0x26, 0x29);
	using SKFont font = new SKFont(SKTypeface.Default, 10);
	using SKPaint label = new SKPaint { Color = ink, IsAntialias = true };

	for(int i = 0; i < icons.Count; i++)
	{
		float x = (i % COLS) * CW, y = (i / COLS) * CH;
		foreach(int size in new[] { 16, 32 })
		{
			c.Save();
			c.Translate(x + 4 + (size == 16 ? 0 : 24), y + 4);
			c.Scale(size / 16f);
			foreach(string layer in icons[i][1].Split(';'))
			{
				string l = layer.Trim();
				int space = l.IndexOf(' ');
				string head = l[..space];
				double opacity = head.Length > 2 ? double.Parse("0" + head[2..], CultureInfo.InvariantCulture) : 1.0;
				SKColor colour = head[1] switch
				{
					'a' => new SKColor(0x3d, 0xae, 0xe9),
					'r' => new SKColor(0xda, 0x44, 0x53),
					'g' => new SKColor(0x27, 0xae, 0x60),
					'o' => new SKColor(0xf6, 0x74, 0x00),
					'y' => new SKColor(0xfd, 0xbc, 0x4b),
					'w' => new SKColor(0xfc, 0xfc, 0xfc),
					_ => ink,
				};
				using SKPath path = SKPath.ParseSvgPathData(l[(space + 1)..]);
				if(path == null) { Console.WriteLine("Bad path data: " + icons[i][0]); continue; }
				using SKPaint paint = new SKPaint
				{
					IsAntialias = true, Color = colour.WithAlpha((byte)(opacity * 255)), StrokeWidth = 1,
					Style = head[0] == 'f' ? SKPaintStyle.Fill : SKPaintStyle.Stroke, StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round,
				};
				c.DrawPath(path, paint);
			}
			c.Restore();
		}
		c.DrawText(icons[i][0].Replace("buildermodes_", "bm_").Replace("builder_", ""), x + 2, y + 52, font, label);
	}

	using SKImage image = surface.Snapshot();
	using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
	string file = args[1] + (dark ? "-dark.png" : "-light.png");
	File.WriteAllBytes(file, data.ToArray());
	Console.WriteLine("Wrote " + file);
}
