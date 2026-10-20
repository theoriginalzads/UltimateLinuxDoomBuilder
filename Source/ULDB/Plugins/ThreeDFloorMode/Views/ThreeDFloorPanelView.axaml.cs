// ULDB: code-behind for ThreeDFloorPanelView.axaml, 3D Floor mode's docker. The mode adds a card
// control to the panel the first time it needs one and reuses it after; compat collections don't
// signal changes, so the view checks for new cards while it's shown.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using ULDB.Shell;
using WF = System.Windows.Forms;

namespace CodeImp.DoomBuilder.ThreeDFloorMode
{
	public partial class ThreeDFloorPanelView : UserControl
	{
		private readonly List<WF.Control> shown = new List<WF.Control>();

		public ThreeDFloorPanelView()
		{
			InitializeComponent();
		}

		internal ThreeDFloorPanelView(ThreeDFloorPanel model) : this()
		{
			DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
			timer.Tick += (s, e) => Sync(model.flowLayoutPanel1);
			AttachedToVisualTree += (s, e) => { Sync(model.flowLayoutPanel1); timer.Start(); };
			DetachedFromVisualTree += (s, e) => timer.Stop();
		}

		private void Sync(WF.FlowLayoutPanel panel)
		{
			for(int i = shown.Count; i < panel.Controls.Count; i++)
			{
				WF.Control card = panel.Controls[i];
				shown.Add(card);
				items.Children.Add(ControlViews.Create(card) ?? new TextBlock { Text = card.GetType().Name });
			}
		}
	}
}
