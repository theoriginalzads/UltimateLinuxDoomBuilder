// ULDB: code-behind for RunExternalCommandFormView.axaml, the window of upstream's RunExternalCommandForm (model-ported).
// The output is the model's RichTextBox text with its coloured runs (SelectionColor), shown as inlines.

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class RunExternalCommandFormView : Window
	{
		public RunExternalCommandFormView()
		{
			InitializeComponent();
		}

		internal RunExternalCommandFormView(RunExternalCommandForm form) : this()
		{
			ModelBinder binder = new ModelBinder(form);
			binder.BindByName(this);
			WF.RichTextBox output = binder.Field<WF.RichTextBox>("rtbOutput");
			SelectableTextBlock text = this.FindControl<SelectableTextBlock>("outputtext");
			ScrollViewer scroll = this.FindControl<ScrollViewer>("outputscroll");
			string shown = null;
			output.CaretScrollRequested += (s, e) => Dispatcher.UIThread.Post(scroll.ScrollToEnd, DispatcherPriority.Background);
			binder.Custom(() =>
			{
				string sig = output.Text + "|" + output.ColorRuns.Count;
				if(sig == shown) return;
				shown = sig;
				text.Inlines.Clear();
				int pos = 0;
				string all = output.Text ?? "";
				foreach((int start, int length, System.Drawing.Color color) in output.ColorRuns)
				{
					if(start < pos || start + length > all.Length) continue;
					if(start > pos) text.Inlines.Add(new Run(all.Substring(pos, start - pos)));
					text.Inlines.Add(new Run(all.Substring(start, length)) { Foreground = new SolidColorBrush(Color.FromRgb(color.R, color.G, color.B)) });
					pos = start + length;
				}
				if(pos < all.Length) text.Inlines.Add(new Run(all.Substring(pos)));
			}, output);
			binder.UpdateNow();
		}
	}
}
