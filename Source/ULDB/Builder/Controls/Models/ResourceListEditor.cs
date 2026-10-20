// ULDB: upstream's ResourceListEditor as a model, so forms that hold one in their designer code
// (ConfigForm) compile unchanged. The editor itself is ResourceListEditorView (Avalonia); this
// class forwards upstream's public API to it, and the view is shown through ControlViews.

using System.Windows.Forms;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;

namespace CodeImp.DoomBuilder.Controls
{
	internal class ResourceListEditor : UserControl
	{
		public delegate void ContentChanged();
		public delegate void WarningsChanged(int size);
		public event ContentChanged OnContentChanged;
		public event WarningsChanged OnWarningsChanged;

		private ResourceListEditorView view;

		/// <summary>The Avalonia editor (created when first needed, on the UI thread).</summary>
		internal ResourceListEditorView View
		{
			get
			{
				if(view == null)
				{
					view = new ResourceListEditorView();
					view.OnContentChanged += () => OnContentChanged?.Invoke();
					view.OnWarningsChanged += size => OnWarningsChanged?.Invoke(size);
				}
				return view;
			}
		}

		public string StartPath { get { return View.StartPath; } set { View.StartPath = value; } }
		public bool IsMapControl { get { return View.IsMapControl; } set { View.IsMapControl = value; } }
		public System.Drawing.Point DialogOffset { get { return View.DialogOffset; } set { View.DialogOffset = value; } }
		public GameConfiguration GameConfiguration { get { return View.GameConfiguration; } set { View.GameConfiguration = value; } }

		public void FixedResourceLocationList(DataLocationList list) { View.FixedResourceLocationList(list); }
		public void EditResourceLocationList(DataLocationList list) { View.EditResourceLocationList(list); }
		public DataLocationList GetResources() { return View.GetResources(); }
		public bool ResourcesAreValid() { return View.ResourcesAreValid(); }
	}
}
