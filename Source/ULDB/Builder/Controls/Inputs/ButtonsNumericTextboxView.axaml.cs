// ULDB: code-behind for ButtonsNumericTextboxView.axaml. The text box is bound to upstream's
// NumericTextbox (typing filter, relative values, validation); the up/down buttons and the
// mouse wheel move upstream's hidden scroll bar, whose ValueChanged handler steps the value.

using Avalonia.Controls;
using Avalonia.Input;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class ButtonsNumericTextboxView : UserControl
	{
		public ButtonsNumericTextboxView()
		{
			InitializeComponent();
		}

		internal ButtonsNumericTextboxView(ButtonsNumericTextbox model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			WF.ScrollBar scroll = binder.Field<WF.ScrollBar>("buttons");
			NumericTextbox text = binder.Field<NumericTextbox>("textbox");

			// Up = -1, down = +1 (upstream subtracts the scroll value times the step).
			buttonup.Click += (s, e) => scroll.RaiseScroll(WF.ScrollEventType.SmallDecrement, scroll.Value - 1);
			buttondown.Click += (s, e) => scroll.RaiseScroll(WF.ScrollEventType.SmallIncrement, scroll.Value + 1);
			textbox.PointerWheelChanged += (s, e) =>
			{
				int delta = (int)System.Math.Round(e.Delta.Y * 120);
				if(delta == 0) return;
				text.RaiseMouseWheel(new WF.MouseEventArgs(WF.MouseButtons.None, 0, 0, 0, delta));
				e.Handled = true;
			};
			binder.Custom(() =>
			{
				this.IsVisible = model.Visible;
				this.IsEnabled = model.Enabled;
				buttons.IsEnabled = scroll.Enabled;
			}, scroll);
			binder.UpdateNow();
		}
	}
}
