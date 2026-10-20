// ULDB: code-behind for ThreeDFloorEditorWindowView.axaml, the window of 3D Floor mode's editor. Upstream
// adds a ThreeDFloorControl to its flow panel per 3D floor; compat collections don't signal changes, so
// the view keeps its card list in step with the panel's while it's open.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.ThreeDFloorMode
{
	public partial class ThreeDFloorEditorWindowView : Window
	{
		private readonly List<WF.Control> shown = new List<WF.Control>();

		public ThreeDFloorEditorWindowView()
		{
			InitializeComponent();
		}

		internal ThreeDFloorEditorWindowView(ThreeDFloorEditorWindow model) : this()
		{
			ModelBinder binder = new ModelBinder(model).BindByName(this);
			WF.FlowLayoutPanel panel = (WF.FlowLayoutPanel)typeof(ThreeDFloorEditorWindow).GetField("threeDFloorPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(model);

			void Sync()
			{
				List<WF.Control> now = panel.Controls.Cast<WF.Control>().ToList();
				if(!now.SequenceEqual(shown))
				{
					shown.Clear();
					shown.AddRange(now);
					cards.Children.Clear();
					foreach(WF.Control card in now) cards.Children.Add(ControlViews.Create(card) ?? new TextBlock { Text = card.GetType().Name });
				}
				if(panel.ScrollTarget != null)
				{
					int index = shown.IndexOf(panel.ScrollTarget);
					panel.ScrollTarget = null;
					if(index >= 0 && index < cards.Children.Count) cards.Children[index].BringIntoView();
				}
				no3dfloorspanel.IsVisible = shown.Count == 0;
				scroller.IsVisible = shown.Count > 0;
				binder.UpdateNow();
			}

			DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
			timer.Tick += (s, e) => Sync();
			Opened += (s, e) => { Sync(); timer.Start(); };
			Closed += (s, e) => timer.Stop();
			Sync();
		}
	}
}
