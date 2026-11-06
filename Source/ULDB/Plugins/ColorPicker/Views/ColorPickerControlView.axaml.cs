// ULDB: code-behind for ColorPickerControlView.axaml (see there).

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using ULDB.Shell;
using SD = System.Drawing;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.ColorPicker.Controls
{
	public partial class ColorPickerControlView : UserControl
	{
		private readonly ColorPickerControl model;
		private bool attached, drawing;

		public ColorPickerControlView()
		{
			InitializeComponent();
		}

		internal ColorPickerControlView(ColorPickerControl model) : this()
		{
			this.model = model;
			ModelBinder binder = new ModelBinder(model);
			binder.BindByName(this);

			// The OK and Cancel buttons show the new and the original colour (upstream sets their BackColor).
			Button ok = this.FindControl<Button>("btnOK"), cancel = this.FindControl<Button>("btnCancel");
			binder.Custom(() =>
			{
				Tint(ok, model.OkButton);
				Tint(cancel, model.CancelButton);
			}, model);

			Image surface = this.FindControl<Image>("surface");
			surface.PointerPressed += (s, e) => Mouse(e, true, false);
			surface.PointerMoved += (s, e) => Mouse(e, false, false);
			surface.PointerReleased += (s, e) => Mouse(e, false, true);
			surface.PointerExited += (s, e) => model.RaiseMouseLeave();

			model.Invalidated += (s, e) => Dispatcher.UIThread.Post(Redraw, DispatcherPriority.Background);
			// The control's Load (colour wheel creation) is raised when it's attached; draw after that.
			AttachedToVisualTree += (s, e) =>
			{
				attached = true;
				Dispatcher.UIThread.Post(Redraw, DispatcherPriority.Background);
			};
			binder.UpdateNow();
		}

		private static void Tint(Button target, WF.Button source)
		{
			if(source.BackColor.IsEmpty) return;
			Avalonia.Media.Color back = Avalonia.Media.Color.FromRgb(source.BackColor.R, source.BackColor.G, source.BackColor.B);
			Avalonia.Media.IBrush brush = new Avalonia.Media.SolidColorBrush(back);
			target.Background = brush;
			target.BorderBrush = Avalonia.Media.Brushes.Gray;
			target.BorderThickness = new Avalonia.Thickness(1);
			target.Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(source.ForeColor.R, source.ForeColor.G, source.ForeColor.B));
			// Fluent's hover/pressed states replace the background; keep the colour so the button still shows it.
			target.Resources["ButtonBackgroundPointerOver"] = brush;
			target.Resources["ButtonBackgroundPressed"] = brush;
		}

		private void Mouse(PointerEventArgs e, bool down, bool up)
		{
			if(!attached) return;
			Avalonia.Point p = e.GetPosition(this.FindControl<Image>("surface"));
			bool left = e.GetCurrentPoint(null).Properties.IsLeftButtonPressed;
			WF.MouseButtons button = left || up ? WF.MouseButtons.Left : WF.MouseButtons.None;
			WF.MouseEventArgs args = new WF.MouseEventArgs(button, 1, (int)p.X, (int)p.Y, 0);
			if(down) { e.Pointer.Capture(this.FindControl<Image>("surface")); model.RaiseMouseDown(args); }
			else if(up) { e.Pointer.Capture(null); model.RaiseMouseUp(args); }
			else model.RaiseMouseMove(args);
		}

		private void Redraw()
		{
			if(!attached || drawing) return;
			drawing = true;
			try
			{
				Image surface = this.FindControl<Image>("surface");
				using(SD.Bitmap bitmap = new SD.Bitmap((int)surface.Width, (int)surface.Height))
				{
					using(SD.Graphics g = SD.Graphics.FromImage(bitmap))
						model.RaisePaint(g, new SD.Rectangle(0, 0, bitmap.Width, bitmap.Height));
					surface.Source = AvaloniaImages.From(bitmap);
				}
			}
			finally { drawing = false; }
		}
	}
}
