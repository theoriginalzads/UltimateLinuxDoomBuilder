// ULDB: the interactive view of upstream's AngleControlEx (thing angle, pitch and roll): a dial
// that passes the pointer to the model in its own coordinates, so upstream's handlers turn it
// into an angle (left button snaps to 45°, right button is free) and raise AngleChanged.

using System;
using Avalonia;
using Avalonia.Input;
using CodeImp.DoomBuilder.Controls;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace ULDB.Controls
{
	internal sealed class AngleEditor : AngleDial
	{
		private readonly AngleControlEx model;
		private WF.MouseButtons pressed;

		public AngleEditor(AngleControlEx model)
		{
			this.model = model;
			Width = 72;
			Height = 72;
			ShowMarks = true;
			Cursor = new Cursor(StandardCursorType.Hand);
			Avalonia.Controls.ToolTip.SetTip(this, "Left-click or drag: 45° steps. Right-click or drag: any angle.");
			ModelBinder binder = new ModelBinder(model);
			binder.Custom(() =>
			{
				Angle = model.Angle == AngleControlEx.NO_ANGLE ? int.MinValue : model.Angle + model.AngleOffset;
				IsVisible = model.Visible;
				IsEnabled = model.Enabled;
				Opacity = model.Enabled ? 1.0 : 0.45;
			}, model);
			binder.UpdateNow();
		}

		private WF.MouseEventArgs Args(PointerEventArgs e, WF.MouseButtons button)
		{
			Point p = e.GetPosition(this);
			double sx = Bounds.Width > 0 ? model.Width / Bounds.Width : 1;
			double sy = Bounds.Height > 0 ? model.Height / Bounds.Height : 1;
			return new WF.MouseEventArgs(button, 1, (int)Math.Round(p.X * sx), (int)Math.Round(p.Y * sy), 0);
		}

		protected override void OnPointerPressed(PointerPressedEventArgs e)
		{
			base.OnPointerPressed(e);
			PointerPointProperties p = e.GetCurrentPoint(this).Properties;
			pressed = p.IsRightButtonPressed ? WF.MouseButtons.Right : WF.MouseButtons.Left;
			e.Pointer.Capture(this);
			model.RaiseMouseDown(Args(e, pressed));
			e.Handled = true;
		}

		protected override void OnPointerMoved(PointerEventArgs e)
		{
			base.OnPointerMoved(e);
			if(e.Pointer.Captured != this) return;
			model.RaiseMouseMove(Args(e, pressed));
		}

		protected override void OnPointerReleased(PointerReleasedEventArgs e)
		{
			base.OnPointerReleased(e);
			if(e.Pointer.Captured == this) e.Pointer.Capture(null);
			model.RaiseMouseUp(Args(e, pressed));
		}
	}
}
