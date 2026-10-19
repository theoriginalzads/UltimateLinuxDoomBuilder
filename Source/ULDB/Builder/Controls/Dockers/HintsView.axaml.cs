// ULDB: code-behind for HintsView.axaml. Upstream's HintsPanel (model-ported) receives the hints
// as RTF from HintsManager, which uses only {\b ...} and \par; this renders that subset.

using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	public partial class HintsView : UserControl
	{
		public HintsView()
		{
			InitializeComponent();
		}

		internal HintsView(HintsPanel model) : this()
		{
			ModelBinder binder = new ModelBinder(model);
			WF.RichTextBox text = binder.Field<WF.RichTextBox>("hints");
			binder.Custom(() => Render(text.Rtf, hints.Inlines), text);
			binder.UpdateNow();
		}

		/// <summary>Renders simple RTF: groups, \b / \b0, \par and \line; other control words are skipped.</summary>
		internal static void Render(string rtf, InlineCollection target)
		{
			target.Clear();
			if(string.IsNullOrEmpty(rtf)) return;

			StringBuilder run = new StringBuilder();
			System.Collections.Generic.Stack<bool> boldstack = new System.Collections.Generic.Stack<bool>();
			bool bold = false;

			void Flush()
			{
				if(run.Length == 0) return;
				target.Add(new Run(run.ToString()) { FontWeight = bold ? FontWeight.Bold : FontWeight.Normal });
				run.Clear();
			}

			for(int i = 0; i < rtf.Length; i++)
			{
				char c = rtf[i];
				if(c == '{') { Flush(); boldstack.Push(bold); }
				else if(c == '}') { Flush(); if(boldstack.Count > 0) bold = boldstack.Pop(); }
				else if(c == '\\' && i + 1 < rtf.Length && !char.IsLetter(rtf[i + 1]))
				{
					run.Append(rtf[++i]); // escaped \ { }
				}
				else if(c == '\\')
				{
					int start = ++i;
					while(i < rtf.Length && char.IsLetter(rtf[i])) i++;
					string word = rtf.Substring(start, i - start);
					int numstart = i;
					while(i < rtf.Length && (char.IsDigit(rtf[i]) || rtf[i] == '-')) i++;
					string num = rtf.Substring(numstart, i - numstart);
					if(i < rtf.Length && rtf[i] == ' ') { } else i--; // a space ends the word and is consumed

					switch(word)
					{
						case "b": Flush(); bold = num != "0"; break;
						case "par":
						case "line": Flush(); target.Add(new LineBreak()); break;
					}
				}
				else if(c != '\r' && c != '\n') run.Append(c);
			}
			Flush();
		}
	}
}
