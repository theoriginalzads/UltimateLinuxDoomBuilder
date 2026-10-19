// ULDB compatibility layer: WinForms common dialogs, shown by the UI through WinFormsHost.

using System.Drawing;

namespace System.Windows.Forms
{
	public abstract class CommonDialog : IDisposable
	{
		public object Tag { get; set; }

		public DialogResult ShowDialog() => ShowDialog(null);
		public abstract DialogResult ShowDialog(IWin32Window owner);
		public virtual void Reset() { }
		public void Dispose() { }
	}

	public abstract class FileDialog : CommonDialog
	{
		private string[] filenames = new string[0];

		public string Title { get; set; } = "";
		public string Filter { get; set; } = "";
		public int FilterIndex { get; set; } = 1;
		public string InitialDirectory { get; set; } = "";
		public string DefaultExt { get; set; } = "";
		public bool AddExtension { get; set; } = true;
		public bool CheckFileExists { get; set; }
		public bool CheckPathExists { get; set; } = true;
		public bool RestoreDirectory { get; set; }
		public bool SupportMultiDottedExtensions { get; set; }
		public bool ValidateNames { get; set; } = true;
		public bool AutoUpgradeEnabled { get; set; } = true;

		public string FileName
		{
			get => filenames.Length > 0 ? filenames[0] : "";
			set => filenames = string.IsNullOrEmpty(value) ? new string[0] : new[] { value };
		}

		public string[] FileNames => (string[])filenames.Clone();

		protected abstract FileDialogRequest CreateRequest();

		public override DialogResult ShowDialog(IWin32Window owner)
		{
			FileDialogRequest request = CreateRequest();
			request.Title = Title;
			request.Filter = Filter;
			request.FilterIndex = FilterIndex;
			request.InitialDirectory = InitialDirectory;
			request.FileName = FileName;
			request.DefaultExt = DefaultExt;
			request.CheckFileExists = CheckFileExists;

			string[] result = WinFormsHost.Current.ShowFileDialog(owner, request);
			if(result == null || result.Length == 0) return DialogResult.Cancel;

			filenames = result;
			FilterIndex = request.FilterIndex;
			return DialogResult.OK;
		}
	}

	public sealed class OpenFileDialog : FileDialog
	{
		public bool Multiselect { get; set; }
		public bool ReadOnlyChecked { get; set; }
		public bool ShowReadOnly { get; set; }

		public OpenFileDialog() { CheckFileExists = true; }

		protected override FileDialogRequest CreateRequest() => new FileDialogRequest { Save = false, Multiselect = Multiselect };
	}

	public sealed class SaveFileDialog : FileDialog
	{
		public bool OverwritePrompt { get; set; } = true;
		public bool CreatePrompt { get; set; }

		protected override FileDialogRequest CreateRequest() => new FileDialogRequest { Save = true, OverwritePrompt = OverwritePrompt };
	}

	public sealed class ColorDialog : CommonDialog
	{
		public Color Color { get; set; } = Color.Black;
		public int[] CustomColors { get; set; } = new int[16];
		public bool AllowFullOpen { get; set; } = true;
		public bool FullOpen { get; set; }
		public bool AnyColor { get; set; }
		public bool SolidColorOnly { get; set; }

		public override DialogResult ShowDialog(IWin32Window owner)
		{
			Color? result = WinFormsHost.Current.ShowColorDialog(owner, Color, CustomColors);
			if(result == null) return DialogResult.Cancel;
			Color = result.Value;
			return DialogResult.OK;
		}
	}

	public sealed class FolderBrowserDialog : CommonDialog
	{
		public string SelectedPath { get; set; } = "";
		public string Description { get; set; } = "";
		public bool ShowNewFolderButton { get; set; } = true;

		public override DialogResult ShowDialog(IWin32Window owner)
		{
			FileDialogRequest request = new FileDialogRequest { SelectFolder = true, Title = Description, InitialDirectory = SelectedPath };
			string[] result = WinFormsHost.Current.ShowFileDialog(owner, request);
			if(result == null || result.Length == 0) return DialogResult.Cancel;
			SelectedPath = result[0];
			return DialogResult.OK;
		}
	}
}
