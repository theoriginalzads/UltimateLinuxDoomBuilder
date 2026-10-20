// ULDB: small up/down buttons standing in for the hidden VScrollBar that upstream controls
// (ButtonsNumericTextbox, ArgumentBox, TagSelector) use as arrows: up sets Value - 1, down
// Value + 1, and upstream's ValueChanged handler steps the value and resets it to 0.

using Avalonia.Controls;
using Avalonia.Layout;
using WF = System.Windows.Forms;

namespace ULDB.Controls
{
	internal sealed class SpinButtons : Grid
	{
		public SpinButtons(WF.ScrollBar model)
		{
			RowDefinitions = new RowDefinitions("*,*");
			Width = 18;
			Margin = new Avalonia.Thickness(2, 0, 0, 0);
			RepeatButton up = Arrow("M0,4 L4,0 L8,4 Z");
			RepeatButton down = Arrow("M0,0 L8,0 L4,4 Z");
			SetRow(down, 1);
			Children.Add(up);
			Children.Add(down);
			up.Click += (s, e) => model.RaiseScroll(WF.ScrollEventType.SmallDecrement, model.Value - 1);
			down.Click += (s, e) => model.RaiseScroll(WF.ScrollEventType.SmallIncrement, model.Value + 1);
			Shell.ModelBinder binder = new Shell.ModelBinder(model);
			binder.Custom(() => { IsVisible = model.Visible; IsEnabled = model.Enabled; }, model);
			binder.UpdateNow();
		}

		private static RepeatButton Arrow(string data)
		{
			RepeatButton b = new RepeatButton
			{
				Content = new PathIcon { Data = Avalonia.Media.Geometry.Parse(data), Width = 8, Height = 4 },
				Padding = new Avalonia.Thickness(0), Delay = 400, Interval = 60,
				HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center,
			};
			b.Classes.Add("uldb-spin");
			return b;
		}
	}
}
