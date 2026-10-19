// ULDB compatibility layer: the bridge between WinForms-style calls in upstream code
// (MessageBox.Show, Cursor.Current, Clipboard, file dialogs, ...) and the real UI.
//
// The Avalonia UI installs its own IWinFormsHost at startup. Until then (and in
// tests) the headless host is used: no UI, sensible defaults, everything logged.

using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace System.Windows.Forms
{
	public interface IWinFormsHost
	{
		DialogResult ShowMessageBox(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton);

		/// <summary>Shows a file dialog. Returns the chosen paths, or null when cancelled.</summary>
		string[] ShowFileDialog(IWin32Window owner, FileDialogRequest request);

		/// <summary>Shows a color picker. Returns the chosen color, or null when cancelled.</summary>
		Color? ShowColorDialog(IWin32Window owner, Color initial, int[] customColors);

		void SetCursor(Cursor cursor);
		void SetClipboard(DataObject data);
		DataObject GetClipboard();
		void ShowHelp(IWin32Window owner, string url, string topic);
		void DoEvents();

		/// <summary>Runs an action on the UI thread and waits for it to finish.</summary>
		void Invoke(Action action);

		/// <summary>Queues an action to run on the UI thread.</summary>
		void BeginInvoke(Action action);

		/// <summary>Runs a callback on the UI thread every interval until the returned handle is disposed.</summary>
		IDisposable StartTimer(int interval, Action tick);

		Form ActiveForm { get; }
	}

	public sealed class FileDialogRequest
	{
		public bool Save { get; set; }
		public bool SelectFolder { get; set; }
		public string Title { get; set; }
		public string Filter { get; set; }
		public int FilterIndex { get; set; } = 1;
		public string InitialDirectory { get; set; }
		public string FileName { get; set; }
		public string DefaultExt { get; set; }
		public bool Multiselect { get; set; }
		public bool OverwritePrompt { get; set; } = true;
		public bool CheckFileExists { get; set; }

		/// <summary>Parses a WinForms filter string ("Text files|*.txt|All files|*.*") into name/patterns pairs.</summary>
		public IReadOnlyList<KeyValuePair<string, string[]>> ParseFilter()
		{
			List<KeyValuePair<string, string[]>> result = new List<KeyValuePair<string, string[]>>();
			if(string.IsNullOrEmpty(Filter)) return result;
			string[] parts = Filter.Split('|');
			for(int i = 0; i + 1 < parts.Length; i += 2)
				result.Add(new KeyValuePair<string, string[]>(parts[i], parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)));
			return result;
		}
	}

	public static class WinFormsHost
	{
		private static IWinFormsHost current = new HeadlessWinFormsHost();

		public static IWinFormsHost Current
		{
			get => current;
			set => current = value ?? new HeadlessWinFormsHost();
		}

		/// <summary>Receives a line for every UI request the headless host answers with a default.</summary>
		public static Action<string> Log { get; set; }
	}

	/// <summary>No-UI host for tests and early startup. Answers every request with the default choice.</summary>
	public sealed class HeadlessWinFormsHost : IWinFormsHost
	{
		private DataObject clipboard = new DataObject();

		public DialogResult ShowMessageBox(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
		{
			DialogResult result = DefaultResult(buttons, defaultButton);
			WinFormsHost.Log?.Invoke("[headless] MessageBox '" + caption + "': " + text + " -> " + result);
			return result;
		}

		public string[] ShowFileDialog(IWin32Window owner, FileDialogRequest request)
		{
			WinFormsHost.Log?.Invoke("[headless] File dialog '" + request.Title + "' cancelled");
			return null;
		}

		public Color? ShowColorDialog(IWin32Window owner, Color initial, int[] customColors) => null;
		public void SetCursor(Cursor cursor) { }
		public void SetClipboard(DataObject data) { clipboard = data ?? new DataObject(); }
		public DataObject GetClipboard() => clipboard;
		public void ShowHelp(IWin32Window owner, string url, string topic) { }
		public void DoEvents() { }
		public void Invoke(Action action) => action();
		public void BeginInvoke(Action action) => Threading.ThreadPool.QueueUserWorkItem(_ => action());

		public IDisposable StartTimer(int interval, Action tick)
		{
			// No UI thread: run on the thread pool. Ticks never overlap.
			object gate = new object();
			Threading.Timer timer = new Threading.Timer(_ =>
			{
				if(!Threading.Monitor.TryEnter(gate)) return;
				try { tick(); }
				finally { Threading.Monitor.Exit(gate); }
			}, null, interval, interval);
			return timer;
		}

		public Form ActiveForm => null;

		private static DialogResult DefaultResult(MessageBoxButtons buttons, MessageBoxDefaultButton defaultButton)
		{
			DialogResult[] choices;
			switch(buttons)
			{
				case MessageBoxButtons.OKCancel: choices = new[] { DialogResult.OK, DialogResult.Cancel }; break;
				case MessageBoxButtons.AbortRetryIgnore: choices = new[] { DialogResult.Abort, DialogResult.Retry, DialogResult.Ignore }; break;
				case MessageBoxButtons.YesNoCancel: choices = new[] { DialogResult.Yes, DialogResult.No, DialogResult.Cancel }; break;
				case MessageBoxButtons.YesNo: choices = new[] { DialogResult.Yes, DialogResult.No }; break;
				case MessageBoxButtons.RetryCancel: choices = new[] { DialogResult.Retry, DialogResult.Cancel }; break;
				default: choices = new[] { DialogResult.OK }; break;
			}
			int index = defaultButton == MessageBoxDefaultButton.Button3 ? 2 : defaultButton == MessageBoxDefaultButton.Button2 ? 1 : 0;
			return choices[Math.Min(index, choices.Length - 1)];
		}
	}
}
