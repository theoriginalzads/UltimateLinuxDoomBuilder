// ULDB: draws ToastManager's toasts over the map view. The manager (upstream, unchanged) adds
// ToastControls to the display control, slides them in and removes them; this mirrors them as cards
// in a canvas over the same area, with the type's icon, a title, the text and a close button.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CodeImp.DoomBuilder;
using CodeImp.DoomBuilder.Controls;
using WF = System.Windows.Forms;

namespace ULDB.Shell
{
	internal sealed class ToastOverlay : Canvas
	{
		private readonly WF.Control display;
		private readonly Dictionary<ToastControl, Control> cards = new Dictionary<ToastControl, Control>();

		public ToastOverlay(WF.Control display)
		{
			this.display = display;
			IsHitTestVisible = true;
			Background = null;
			DispatcherTimer timer = new DispatcherTimer(TimeSpan.FromMilliseconds(30), DispatcherPriority.Render, (s, e) => Sync());
			timer.Start();
		}

		private void Sync()
		{
			List<ToastControl> toasts = display.Controls.OfType<ToastControl>().ToList();
			foreach(ToastControl gone in cards.Keys.Where(t => !toasts.Contains(t)).ToList())
			{
				Children.Remove(cards[gone]);
				cards.Remove(gone);
			}

			foreach(ToastControl t in toasts)
			{
				if(!cards.TryGetValue(t, out Control card)) { card = CreateCard(t); cards[t] = card; Children.Add(card); }
				SetLeft(card, t.Location.X);
				SetTop(card, t.Location.Y);
				card.Width = t.Width;
				card.Height = t.Height;
			}
		}

		// A stroked cross (PathIcon only fills, which would draw nothing for lines)
		private static Control CloseGlyph()
		{
			Path cross = new Path { Width = 10, Height = 10, Data = Geometry.Parse("M1,1 L9,9 M9,1 L1,9"), StrokeThickness = 1.4, StrokeLineCap = PenLineCap.Round };
			cross.Stroke = Brushes.Gray;
			return cross;
		}

		private static Control CreateCard(ToastControl t)
		{
			Color accent = t.Type == ToastType.ERROR ? Color.FromRgb(0xE8, 0x3C, 0x3C) : t.Type == ToastType.WARNING ? Color.FromRgb(0xE8, 0xA3, 0x17) : Color.FromRgb(0x3D, 0xAE, 0xE9);
			string icon = t.Type == ToastType.INFO
				? "M8,1 A7,7 0 1,1 8,15 A7,7 0 1,1 8,1 Z M7.2,7 V12 H8.8 V7 Z M7.2,4 V5.6 H8.8 V4 Z"
				: "M8,1 L15,14 H1 Z M7.2,5.5 V10 H8.8 V5.5 Z M7.2,11 V12.5 H8.8 V11 Z";

			Button close = new Button { Content = CloseGlyph(), Padding = new Thickness(4), MinWidth = 0, MinHeight = 0, Background = Brushes.Transparent, BorderThickness = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
			close.Classes.Add("uldb-tool");
			close.Click += (s, e) => t.RequestRemove();

			Grid content = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Margin = new Thickness(12, 10, 6, 10), ColumnSpacing = 10 };
			content.Children.Add(new PathIcon { Width = 22, Height = 22, Data = Geometry.Parse(icon), Foreground = new SolidColorBrush(accent), VerticalAlignment = VerticalAlignment.Top });
			StackPanel text = new StackPanel { Spacing = 2 };
			text.Children.Add(new TextBlock { Text = t.Title, FontWeight = FontWeight.SemiBold });
			text.Children.Add(new TextBlock { Text = t.Message, TextWrapping = TextWrapping.Wrap, Opacity = 0.85, FontSize = 12 });
			Grid.SetColumn(text, 1);
			Grid.SetColumn(close, 2);
			content.Children.Add(text);
			content.Children.Add(close);

			Border card = new Border
			{
				Child = content,
				CornerRadius = new CornerRadius(6),
				BorderThickness = new Thickness(1, 1, 1, 1),
				BorderBrush = new SolidColorBrush(accent),
				BoxShadow = new BoxShadows(new BoxShadow { Blur = 12, OffsetY = 3, Color = Color.FromArgb(90, 0, 0, 0) }),
				ClipToBounds = true,
			};
			card.Bind(Border.BackgroundProperty, card.GetResourceObservable("UldbCardBrush"));
			card.PointerEntered += (s, e) => t.Hovered = true;
			card.PointerExited += (s, e) => t.Hovered = false;
			return card;
		}
	}
}
