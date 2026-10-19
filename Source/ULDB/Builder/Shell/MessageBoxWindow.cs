// ULDB: the window behind MessageBox.Show. Same buttons, default button and icons as
// the WinForms message box, so upstream's prompts read the same.

using System.Windows.Forms;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using Button = Avalonia.Controls.Button;

namespace ULDB.Shell
{
	internal sealed class MessageBoxWindow : Window
	{
		private DialogResult result;
		private readonly DialogResult cancelresult;

		public MessageBoxWindow(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultbutton)
		{
			Title = string.IsNullOrEmpty(caption) ? Branding.NAME : caption;
			SizeToContent = SizeToContent.WidthAndHeight;
			CanResize = false;
			ShowInTaskbar = false;
			MinWidth = 320;
			MaxWidth = 640;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;

			DialogResult[] choices = Choices(buttons);
			cancelresult = CancelResult(buttons, choices);
			result = cancelresult;

			StackPanel buttonrow = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Right,
				Spacing = 8,
			};

			int defaultindex = defaultbutton == MessageBoxDefaultButton.Button3 ? 2 : defaultbutton == MessageBoxDefaultButton.Button2 ? 1 : 0;
			if(defaultindex >= choices.Length) defaultindex = 0;
			Button defaultcontrol = null;
			for(int i = 0; i < choices.Length; i++)
			{
				DialogResult choice = choices[i];
				Button b = new Button { Content = Label(choice), MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
				b.Click += (s, e) => { result = choice; Close(); };
				if(i == defaultindex) { b.IsDefault = true; b.Classes.Add("accent"); defaultcontrol = b; }
				if(choice == cancelresult) b.IsCancel = true;
				buttonrow.Children.Add(b);
			}

			Grid body = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(20, 20, 20, 12) };
			string glyph = Glyph(icon, out IBrush glyphbrush);
			if(glyph != null)
			{
				body.Children.Add(new TextBlock
				{
					Text = glyph, FontSize = 28, FontWeight = FontWeight.Bold, Foreground = glyphbrush,
					Width = 40, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 0, 12, 0),
					TextAlignment = TextAlignment.Center,
				});
			}
			TextBlock message = new TextBlock { Text = text ?? "", TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
			Grid.SetColumn(message, 1);
			body.Children.Add(message);

			Border footer = new Border
			{
				Padding = new Thickness(20, 12),
				Child = buttonrow,
			};
			footer.Classes.Add("uldb-dialog-footer");

			DockPanel root = new DockPanel();
			DockPanel.SetDock(footer, Dock.Bottom);
			root.Children.Add(footer);
			root.Children.Add(body);
			Content = root;

			Opened += (s, e) => defaultcontrol?.Focus();
			KeyDown += (s, e) =>
			{
				// Ctrl+C copies the message, as in the Windows message box.
				if(e.Key == Key.C && e.KeyModifiers.HasFlag(KeyModifiers.Control))
					Clipboard?.SetTextAsync(Title + "\n\n" + text);
			};
		}

		public DialogResult Result => result;

		private static DialogResult[] Choices(MessageBoxButtons buttons)
		{
			switch(buttons)
			{
				case MessageBoxButtons.OKCancel: return new[] { DialogResult.OK, DialogResult.Cancel };
				case MessageBoxButtons.AbortRetryIgnore: return new[] { DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore };
				case MessageBoxButtons.YesNoCancel: return new[] { DialogResult.Yes, DialogResult.No, DialogResult.Cancel };
				case MessageBoxButtons.YesNo: return new[] { DialogResult.Yes, DialogResult.No };
				case MessageBoxButtons.RetryCancel: return new[] { DialogResult.Retry, DialogResult.Cancel };
				default: return new[] { DialogResult.OK };
			}
		}

		// What closing the window (Escape, the title bar's X) answers. WinForms disables
		// the X for Yes/No and AbortRetryIgnore; here it picks the "don't do it" choice.
		private static DialogResult CancelResult(MessageBoxButtons buttons, DialogResult[] choices)
		{
			switch(buttons)
			{
				case MessageBoxButtons.YesNo: return DialogResult.No;
				case MessageBoxButtons.AbortRetryIgnore: return DialogResult.Abort;
				case MessageBoxButtons.OK: return DialogResult.OK;
				default: return DialogResult.Cancel;
			}
		}

		private static string Label(DialogResult r)
		{
			switch(r)
			{
				case DialogResult.OK: return "OK";
				case DialogResult.Cancel: return "Cancel";
				case DialogResult.Abort: return "Abort";
				case DialogResult.Retry: return "Retry";
				case DialogResult.Ignore: return "Ignore";
				case DialogResult.Yes: return "Yes";
				case DialogResult.No: return "No";
				default: return r.ToString();
			}
		}

		private static string Glyph(MessageBoxIcon icon, out IBrush brush)
		{
			// Hand, Stop and Error share a value, as do Exclamation/Warning and Asterisk/Information.
			switch(icon)
			{
				case MessageBoxIcon.Error: brush = new SolidColorBrush(Color.FromRgb(0xD1, 0x34, 0x38)); return "✕";
				case MessageBoxIcon.Warning: brush = new SolidColorBrush(Color.FromRgb(0xE8, 0xA3, 0x17)); return "!";
				case MessageBoxIcon.Question: brush = new SolidColorBrush(Color.FromRgb(0x2D, 0x7D, 0xD2)); return "?";
				case MessageBoxIcon.Information: brush = new SolidColorBrush(Color.FromRgb(0x2D, 0x7D, 0xD2)); return "i";
				default: brush = null; return null;
			}
		}
	}
}
