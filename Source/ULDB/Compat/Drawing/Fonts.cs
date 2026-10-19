// ULDB compatibility layer: System.Drawing fonts on SkiaSharp (fonts come from fontconfig).

using SkiaSharp;

namespace System.Drawing
{
	public sealed class FontFamily : IDisposable
	{
		private static readonly string[] SANS_FALLBACKS = { "Noto Sans", "DejaVu Sans", "Liberation Sans", "sans-serif" };

		public FontFamily(string name)
		{
			if(string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Font family name can't be empty", nameof(name));

			// GDI+ throws for fonts that aren't installed; callers rely on that to fall back.
			using(SKTypeface face = SKFontManager.Default.MatchFamily(name))
			{
				if(face == null) throw new ArgumentException("Font '" + name + "' cannot be found.", nameof(name));
				Name = face.FamilyName;
			}
		}

		private FontFamily(string name, bool trusted) { Name = name; }

		public string Name { get; }

		public static FontFamily GenericSansSerif => new FontFamily(ResolveFirst(SANS_FALLBACKS), true);
		public static FontFamily GenericSerif => new FontFamily(ResolveFirst(new[] { "Noto Serif", "DejaVu Serif", "Liberation Serif", "serif" }), true);
		public static FontFamily GenericMonospace => new FontFamily(ResolveFirst(new[] { "Noto Sans Mono", "DejaVu Sans Mono", "Liberation Mono", "monospace" }), true);

		public static FontFamily[] Families
		{
			get
			{
				string[] names = SKFontManager.Default.GetFontFamilies();
				Array.Sort(names, StringComparer.OrdinalIgnoreCase);
				return Array.ConvertAll(names, n => new FontFamily(n, true));
			}
		}

		public bool IsStyleAvailable(FontStyle style) => true;

		private static string ResolveFirst(string[] names)
		{
			foreach(string name in names)
			{
				using(SKTypeface face = SKFontManager.Default.MatchFamily(name))
					if(face != null) return face.FamilyName;
			}
			return SKTypeface.Default.FamilyName;
		}

		public override string ToString() => "[FontFamily: Name=" + Name + "]";
		public override bool Equals(object obj) => obj is FontFamily f && string.Equals(f.Name, Name, StringComparison.OrdinalIgnoreCase);
		public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
		public void Dispose() { }
	}

	public sealed class Font : IDisposable, ICloneable
	{
		public Font(FontFamily family, float emSize) : this(family, emSize, FontStyle.Regular, GraphicsUnit.Point) { }
		public Font(FontFamily family, float emSize, FontStyle style) : this(family, emSize, style, GraphicsUnit.Point) { }
		public Font(FontFamily family, float emSize, GraphicsUnit unit) : this(family, emSize, FontStyle.Regular, unit) { }

		public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit)
		{
			FontFamily = family ?? throw new ArgumentNullException(nameof(family));
			Size = emSize;
			Style = style;
			Unit = unit;
		}

		public Font(string familyName, float emSize) : this(MakeFamily(familyName), emSize) { }
		public Font(string familyName, float emSize, FontStyle style) : this(MakeFamily(familyName), emSize, style) { }
		public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit) : this(MakeFamily(familyName), emSize, style, unit) { }
		public Font(Font prototype, FontStyle newStyle) : this(prototype.FontFamily, prototype.Size, newStyle, prototype.Unit) { }
		// gdiCharSet is a Windows code page hint; fontconfig picks glyphs by Unicode coverage instead.
		public Font(string familyName, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet) : this(familyName, emSize, style, unit) { }
		public Font(FontFamily family, float emSize, FontStyle style, GraphicsUnit unit, byte gdiCharSet) : this(family, emSize, style, unit) { }

		public FontFamily FontFamily { get; }
		public string Name => FontFamily.Name;
		public float Size { get; }
		public FontStyle Style { get; }
		public GraphicsUnit Unit { get; }
		public bool Bold => (Style & FontStyle.Bold) != 0;
		public bool Italic => (Style & FontStyle.Italic) != 0;
		public bool Underline => (Style & FontStyle.Underline) != 0;
		public bool Strikeout => (Style & FontStyle.Strikeout) != 0;
		public float SizeInPoints => Unit == GraphicsUnit.Pixel ? Size * 72f / 96f : Size;

		/// <summary>Line spacing in pixels at 96 DPI, as GDI+ reports it.</summary>
		public int Height
		{
			get
			{
				using(SKFont f = CreateSkFont(96f)) return (int)Math.Ceiling(f.Spacing);
			}
		}

		public float GetHeight() => Height;
		public float GetHeight(float dpi)
		{
			using(SKFont f = CreateSkFont(dpi)) return f.Spacing;
		}

		internal float PixelSize(float dpi)
		{
			switch(Unit)
			{
				case GraphicsUnit.Pixel: return Size;
				case GraphicsUnit.Inch: return Size * dpi;
				case GraphicsUnit.Document: return Size * dpi / 300f;
				case GraphicsUnit.Millimeter: return Size * dpi / 25.4f;
				default: return Size * dpi / 72f; // Point, World, Display
			}
		}

		internal SKFont CreateSkFont(float dpi)
		{
			SKFontStyle style = new SKFontStyle(Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
				SKFontStyleWidth.Normal, Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
			SKTypeface face = SKFontManager.Default.MatchFamily(FontFamily.Name, style) ?? SKTypeface.Default;
			return new SKFont(face, PixelSize(dpi)) { Subpixel = true };
		}

		internal SizeF Measure(string text, float dpi)
		{
			if(string.IsNullOrEmpty(text)) text = " ";
			using(SKFont f = CreateSkFont(dpi))
			{
				// GDI+ MeasureString pads the width slightly; UDB sizes label textures from it.
				float width = f.MeasureText(text) + f.Size / 6f;
				return new SizeF(width, f.Spacing);
			}
		}

		private static FontFamily MakeFamily(string name)
		{
			try { return new FontFamily(name); }
			catch(ArgumentException) { return FontFamily.GenericSansSerif; }
		}

		public object Clone() => new Font(FontFamily, Size, Style, Unit);
		public void Dispose() { }
		public override string ToString() => "[Font: Name=" + Name + ", Size=" + Size + ", Units=" + (int)Unit + "]";
	}

	/// <summary>Desktop fonts. The UI replaces these with the desktop's actual fonts at startup.</summary>
	public static class SystemFonts
	{
		private static Font defaultfont;

		public static Font DefaultFont
		{
			get => defaultfont ??= new Font(FontFamily.GenericSansSerif, 9f);
			set => defaultfont = value;
		}

		public static Font MessageBoxFont => DefaultFont;
		public static Font DialogFont => DefaultFont;
		public static Font MenuFont => DefaultFont;
		public static Font StatusFont => DefaultFont;
		public static Font IconTitleFont => DefaultFont;
		public static Font SmallCaptionFont => DefaultFont;
		public static Font CaptionFont => new Font(DefaultFont, FontStyle.Bold);
	}

	public sealed class StringFormat : IDisposable, ICloneable
	{
		public StringFormat() { }
		public StringFormat(StringFormatFlags options) { FormatFlags = options; }

		public StringFormatFlags FormatFlags { get; set; }
		public StringAlignment Alignment { get; set; } = StringAlignment.Near;
		public StringAlignment LineAlignment { get; set; } = StringAlignment.Near;
		public StringTrimming Trimming { get; set; } = StringTrimming.Character;

		public static StringFormat GenericDefault => new StringFormat();
		public static StringFormat GenericTypographic => new StringFormat(StringFormatFlags.FitBlackBox | StringFormatFlags.LineLimit | StringFormatFlags.NoClip);

		public object Clone() => new StringFormat(FormatFlags) { Alignment = Alignment, LineAlignment = LineAlignment, Trimming = Trimming };
		public void Dispose() { }
	}
}
