// ULDB: Avalonia port of upstream Windows/ErrorsForm.cs, over the controls of ErrorsView.axaml.
// Upstream's logic and handler names are kept; differences:
//  - the DataGridView is a ListBox of ErrorsForm.Row (icon + wrapped text)
//  - an empty list says so instead of showing an empty grid

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvButton = Avalonia.Controls.Button;
using AvCheckBox = Avalonia.Controls.CheckBox;
using AvListBox = Avalonia.Controls.ListBox;
using AvTextBlock = Avalonia.Controls.TextBlock;

namespace CodeImp.DoomBuilder.Windows
{
	public partial class ErrorsForm : DelayedForm
	{
		#region ================== Row

		/// <summary>One error or warning in the list (upstream: a grid row).</summary>
		public sealed class Row
		{
			public Row(ErrorItem item)
			{
				Item = item;
				Icon = ULDB.Shell.AvaloniaImages.From(item.Icon);
			}

			public ErrorItem Item { get; }
			public Avalonia.Media.Imaging.Bitmap Icon { get; }
			public string Description => Item.Description;
		}

		#endregion

		#region ================== Variables

		private readonly ErrorsView view;
		private readonly ObservableCollection<Row> rows = new ObservableCollection<Row>();
		private AvListBox grid;
		private AvButton copyselected, showsource, clearlist, close;
		private AvCheckBox checkshow;
		private AvTextBlock emptylabel;
		private Timer checkerrors;

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public ErrorsForm()
		{
			view = new ErrorsView();
			InitializeComponent();

			FillList();
			checkerrors.Start();
			checkshow.IsChecked = General.Settings.ShowErrorsWindow;
			grid.Focus(); //mxd
		}

		// ULDB: takes the controls from the view and connects upstream's handlers.
		private void InitializeComponent()
		{
			grid = view.grid;
			grid.ItemsSource = rows;
			copyselected = view.copyselected;
			showsource = view.showsource;
			clearlist = view.clearlist;
			close = view.close;
			checkshow = view.checkshow;
			emptylabel = view.emptylabel;
			checkerrors = new Timer { Interval = 1000 };

			copyselected.Click += copyselected_Click;
			showsource.Click += showsource_Click;
			clearlist.Click += clearlist_Click;
			close.Click += close_Click;
			checkerrors.Tick += checkerrors_Tick;
			grid.SelectionChanged += grid_CellContentClick;
			grid.DoubleTapped += grid_CellContentDoubleClick;
			view.KeyDown += ErrorsForm_HelpRequested;
			this.FormClosing += ErrorsForm_FormClosing;
			this.Shown += ErrorsForm_Shown;
			FormBorderStyle = FormBorderStyle.Sizable;
		}

		protected override object CreateView() => view;

		#endregion

		#region ================== Methods

		// This sets up the list
		private void FillList()
		{
			// Fill the list with the items we don't have yet
			General.ErrorLogger.HasChanged = false;

			//mxd. Rewritten to get only the new items from the ErrorLogger
			int startindex = rows.Count;
			IEnumerable<ErrorItem> errors = General.ErrorLogger.GetErrors(startindex);
			foreach(ErrorItem e in errors) rows.Add(new Row(e));

			//mxd
			clearlist.IsEnabled = (rows.Count > 0);
			emptylabel.IsVisible = (rows.Count == 0);
		}

		//mxd
		private ErrorItem GetErrorItem()
		{
			if(rows.Count == 0 || grid.SelectedItems == null || grid.SelectedItems.Count != 1) return null;
			return (grid.SelectedItem as Row)?.Item;
		}

		//mxd
		private void ShowErrorSource()
		{
			ErrorItem error = GetErrorItem();
			if(error != null) error.ShowSource();
		}

		#endregion

		#region ================== Events

		// Close clicked
		private void close_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		// Closing
		private void ErrorsForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			checkerrors.Stop();
			General.Settings.ShowErrorsWindow = checkshow.IsChecked == true;
		}

		// Checking for more errors
		private void checkerrors_Tick(object sender, EventArgs e)
		{
			// If errors have been added, update the list
			if(General.ErrorLogger.HasChanged) FillList();
		}

		// This clears all errors
		private void clearlist_Click(object sender, RoutedEventArgs e)
		{
			General.ErrorLogger.Clear();
			rows.Clear();

			copyselected.IsEnabled = false; //mxd
			showsource.IsEnabled = false; //mxd
			clearlist.IsEnabled = false; //mxd
			emptylabel.IsVisible = true;
		}

		// Copy selection
		private void copyselected_Click(object sender, RoutedEventArgs e)
		{
			StringBuilder str = new StringBuilder("");
			if(grid.SelectedItems != null && grid.SelectedItems.Count > 0)
			{
				Clipboard.Clear();
				foreach(Row row in rows) // ULDB: in list order, like upstream's cells
				{
					if(!grid.SelectedItems.Contains(row)) continue;
					if(str.Length > 0) str.Append("\r\n");
					str.Append(row.Description);
				}

				//mxd
				try { Clipboard.SetDataObject(str.ToString(), true, 5, 200); }
				catch(ExternalException) { General.Interface.DisplayStatus(StatusType.Warning, "Failed to perform a Clipboard operation..."); }
			}
		}

		//mxd
		private void showsource_Click(object sender, RoutedEventArgs e)
		{
			ShowErrorSource();
		}

		// Help requested
		private void ErrorsForm_HelpRequested(object sender, Avalonia.Input.KeyEventArgs e)
		{
			if(e.Key != Key.F1) return;
			General.ShowHelp("w_errorsandwarnings.html");
			e.Handled = true;
		}

		private void ErrorsForm_Shown(object sender, EventArgs e)
		{
			grid.SelectedItems?.Clear();
		}

		private void grid_CellContentClick(object sender, Avalonia.Controls.SelectionChangedEventArgs e)
		{
			copyselected.IsEnabled = grid.SelectedItems != null && grid.SelectedItems.Count > 0;

			//mxd. Can we show error source?
			ErrorItem error = GetErrorItem();
			showsource.IsEnabled = (error != null && error.IsShowable);
		}

		//mxd
		private void grid_CellContentDoubleClick(object sender, TappedEventArgs e)
		{
			ShowErrorSource();
		}

		#endregion
	}
}
