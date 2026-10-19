// ULDB: a small read-only angle indicator (a circle with a needle), the view of upstream's
// AngleControlEx where it only shows an angle (thing info panel).

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ULDB.Controls
{
	public class AngleDial : Control
	{
		/// <summary>Angle in degrees, counter-clockwise from east (Doom's convention);
		/// int.MinValue (AngleControlEx.NO_ANGLE) draws no needle.</summary>
		public static readonly StyledProperty<int> AngleProperty = AvaloniaProperty.Register<AngleDial, int>(nameof(Angle), int.MinValue);

		static AngleDial()
		{
			AffectsRender<AngleDial>(AngleProperty);
		}

		public int Angle { get => GetValue(AngleProperty); set => SetValue(AngleProperty, value); }

		/// <summary>Draws marks every 45° (the editor's dials, as UDB's AngleControlEx).</summary>
		public bool ShowMarks { get; set; }

		public override void Render(DrawingContext context)
		{
			double size = Math.Min(Bounds.Width, Bounds.Height);
			if(size <= 2) return;
			Point center = new Point(Bounds.Width / 2, Bounds.Height / 2);
			double radius = size / 2 - 1;

			IBrush ink = TryBrush("TextFillColorPrimaryBrush") ?? Brushes.Gray;
			// Transparent fill so the whole dial takes pointer input (AngleEditor).
			context.DrawEllipse(Brushes.Transparent, new Pen(ink, 1) { Brush = ink }, center, radius, radius);

			if(ShowMarks)
			{
				Pen marks = new Pen(ink, 1) { Brush = ink };
				double inner = radius * 0.8;
				for(int a = 0; a < 360; a += 45)
				{
					double r = a * Math.PI / 180.0;
					context.DrawLine(marks, new Point(center.X + Math.Cos(r) * inner, center.Y - Math.Sin(r) * inner), new Point(center.X + Math.Cos(r) * (radius - 2), center.Y - Math.Sin(r) * (radius - 2)));
				}
				context.DrawEllipse(ink, null, center, 2, 2);
			}

			if(Angle != int.MinValue)
			{
				double rad = Angle * Math.PI / 180.0;
				Point tip = new Point(center.X + Math.Cos(rad) * (radius - 1), center.Y - Math.Sin(rad) * (radius - 1));
				context.DrawLine(new Pen(ink, 1.5, lineCap: PenLineCap.Round), center, tip);
			}
		}

		private IBrush TryBrush(string key)
		{
			return this.TryFindResource(key, ActualThemeVariant, out object value) ? value as IBrush : null;
		}
	}
}
