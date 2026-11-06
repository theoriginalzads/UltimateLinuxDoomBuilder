// ULDB: Avalonia port of upstream Windows/ExceptionDialog.cs, over the controls of ExceptionView.axaml.
// Same class, constructors, Setup() and handler names; differences:
//  - the system information is Linux's (OS, kernel, GPU, runtime), not Windows' WMI queries
//  - the crash report is shown in the file manager with xdg-open (upstream: explorer.exe)
//  - issues go to ULDB's tracker (Branding.ISSUES_URL)
//  - the window title is a plain "Unexpected error" (upstream picks a joke title)
// Continue answers OK (the editor keeps running); Quit answers Cancel, which makes upstream's
// exception handlers terminate the program after saving a backup of the map.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Avalonia.Interactivity;
using ULDB;
using AvButton = Avalonia.Controls.Button;
using AvTextBlock = Avalonia.Controls.TextBlock;
using AvTextBox = Avalonia.Controls.TextBox;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ExceptionDialog : Form
	{
		#region ================== Variables

		private const string CRASH_DUMP_FILE = "UDBCrash.txt";
		private const int KEPT_REPORTS = 10;
		private const int REPORT_LOG_LINES = 300;
		private readonly bool isterminating;
		private readonly string logpath;
		private string report = "";

		// Controls (ExceptionView.axaml)
		private readonly ExceptionView view;
		private AvTextBlock errorDescription;
		private AvTextBox errorMessage;
		private AvButton bToClipboard, bContinue, bQuit, reportlink, newissue;

		#endregion

		#region ================== Constructor

		public ExceptionDialog(UnhandledExceptionEventArgs e)
		{
			view = new ExceptionView();
			InitializeComponent();

			logpath = Path.Combine(General.SettingsPath, CRASH_DUMP_FILE);

			Exception ex = (Exception)e.ExceptionObject;
			errorDescription.Text = "Error in " + ex.Source + ":";
			string sysinfo = GetSystemInfo();
			WriteReport(sysinfo + GetExceptionDescription(ex));

			// ToString includes inner exceptions, which the stack trace alone leaves out
			errorMessage.Text = ex.ToString();
			isterminating = e.IsTerminating;  // Recoverable?
		}

		public ExceptionDialog(ThreadExceptionEventArgs e)
		{
			view = new ExceptionView();
			InitializeComponent();

			logpath = Path.Combine(General.SettingsPath, CRASH_DUMP_FILE);

			errorDescription.Text = "Error in " + e.Exception.Source + ":";
			string sysinfo = GetSystemInfo();
			WriteReport(sysinfo + GetExceptionDescription(e.Exception));

			errorMessage.Text = sysinfo + "********EXCEPTION DETAILS********" + Environment.NewLine + e.Exception;
		}

		// ULDB: takes the controls from the view and connects upstream's handlers.
		private void InitializeComponent()
		{
			errorDescription = view.errorDescription;
			errorMessage = view.errorMessage;
			bToClipboard = view.bToClipboard;
			bContinue = view.bContinue;
			bQuit = view.bQuit;
			reportlink = view.reportlink;
			newissue = view.newissue;

			bToClipboard.Click += bToClipboard_Click;
			bContinue.Click += bContinue_Click;
			bQuit.Click += bQuit_Click;
			reportlink.Click += (s, e) => reportlink_LinkClicked(s, null);
			newissue.Click += (s, e) => newissue_LinkClicked(s, null);
			FormBorderStyle = FormBorderStyle.FixedDialog;
		}

		protected override object CreateView() => view;

		#endregion

		#region ================== Methods

		public void Setup()
		{
			this.Text = "Unexpected error";
			bContinue.IsEnabled = !isterminating;
		}

		// The report is best effort: failing to write it mustn't hide the error itself. It goes to
		// UDBCrash.txt (the latest) and to a dated copy, of which the last few are kept, so a second
		// crash doesn't overwrite the first. The clipboard gets the same text.
		private void WriteReport(string text)
		{
			report = text;
			try
			{
				File.WriteAllText(logpath, text);
				string dated = Path.Combine(Path.GetDirectoryName(logpath), "UDBCrash-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");
				File.WriteAllText(dated, text);
				string[] old = Directory.GetFiles(Path.GetDirectoryName(logpath), "UDBCrash-*.txt");
				Array.Sort(old, StringComparer.Ordinal);
				for(int i = 0; i < old.Length - KEPT_REPORTS; i++) File.Delete(old[i]);
			}
			catch(Exception) { }
		}

		// ULDB: Linux system information (upstream asks Windows' WMI).
		private static string GetSystemInfo()
		{
			StringBuilder result = new StringBuilder("***********SYSTEM INFO***********" + Environment.NewLine);

			result.AppendLine("OS: " + RuntimeInformation.OSDescription + " (" + RuntimeInformation.OSArchitecture + ")");
			string desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
			string session = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
			if(!string.IsNullOrEmpty(desktop) || !string.IsNullOrEmpty(session))
				result.AppendLine("Desktop: " + desktop + " (" + session + ")");
			if(!string.IsNullOrEmpty(ULDB.Shell.ViewportContext.Renderer)) result.AppendLine("GPU: " + ULDB.Shell.ViewportContext.Renderer);
			string version = System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(General.ThisAssembly)?.InformationalVersion;
			result.AppendLine(Branding.SHORT_NAME + ": " + (string.IsNullOrEmpty(version) ? "development build" : version));
			result.AppendLine("Runtime: " + RuntimeInformation.FrameworkDescription);
			result.AppendLine("Platform: " + (Environment.Is64BitProcess ? "x64" : "x86"));
			result.AppendLine();
			return result.ToString();
		}

		private static string GetExceptionDescription(Exception ex)
		{
			// Add to error logger
			General.WriteLogLine("***********************************************************");
			General.ErrorLogger.Add(ErrorType.Error, ex.Source + ": " + ex.Message);
			General.WriteLogLine("***********************************************************");

			string message = "********EXCEPTION DETAILS********"
							 + Environment.NewLine + ex.Source + ": " + ex.Message + Environment.NewLine + ex;

			if(File.Exists(General.LogFile))
			{
				try
				{
					string[] lines = File.ReadAllLines(General.LogFile);
					message += Environment.NewLine + Environment.NewLine + "***********ACTIONS LOG***********";
					for(int i = lines.Length - 1; i > -1 && i >= lines.Length - REPORT_LOG_LINES; i--)
						message += Environment.NewLine + lines[i];
				}
				catch(Exception) { }
			}

			return message;
		}

		#endregion

		#region ================== Events

		private void reportlink_LinkClicked(object sender, EventArgs e)
		{
			if(!File.Exists(logpath)) return;
			// The folder holding the report, in the file manager.
			try { ULDB.Platform.Desktop.Open(Path.GetDirectoryName(logpath)); }
			catch { MessageBox.Show("Unable to show the error report location..."); }
		}

		private void newissue_LinkClicked(object sender, EventArgs e)
		{
			try { General.OpenWebsite(Branding.ISSUES_URL); }
			catch { MessageBox.Show("Unable to open URL..."); }
		}

		private void bContinue_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void bQuit_Click(object sender, RoutedEventArgs e)
		{
			if(General.Map != null) General.Map.SaveMapBackup();
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void bToClipboard_Click(object sender, RoutedEventArgs e)
		{
			// The whole report (system info, details and the latest log lines), as in the report file
			Clipboard.SetDataObject(string.IsNullOrEmpty(report) ? errorMessage.Text ?? "" : report, true);
		}

		#endregion
	}
}
