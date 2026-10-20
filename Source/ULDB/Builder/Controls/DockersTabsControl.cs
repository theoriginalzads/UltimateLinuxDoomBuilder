// ULDB: port of upstream Controls/DockersTabsControl.cs. Upstream's class owner-draws vertical
// tabs with Windows visual styles; in ULDB the dockers view draws the tabs, so this keeps only
// the behaviour: the notify animation (a tab blinks when a docker is added while collapsed) and
// key absorption. Same name and members as upstream.

using System;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	internal class DockersTabsControl : TabControl
	{
		#region ================== Constants

		private const int NOTIFY_BLINK_COUNT = 8; //mxd

		#endregion

		#region ================== Variables

		//mxd. Tab notify anmimation
		private int notifytab = -1;
		private int notifycounter;
		private readonly Timer notifytimer;

		#endregion

		#region ================== Properties

		/// <summary>ULDB: the tab drawn highlighted by the notify animation right now, or -1.</summary>
		public int NotifyHighlightTab => (notifytab != -1 && notifycounter % 2 != 0) ? notifytab : -1;

		/// <summary>ULDB: raised when the notify animation changes, so the view redraws.</summary>
		public event EventHandler NotifyChanged;

		#endregion

		#region ================== Constructor

		// Constructor
		public DockersTabsControl()
		{
			//mxd. Tab notify anmimation
			notifytimer = new Timer { Interval = 500 };
			notifytimer.Tick += NotifyTimerOnTick;
			SelectedIndexChanged += OnSelectedIndexChanged;
		}

		#endregion

		#region ================== Methods

		//mxd. Start notify animation
		internal void PlayNotifyAnimation(int tabindex)
		{
			notifytab = tabindex;
			notifycounter = 1;
			notifytimer.Start();
			NotifyChanged?.Invoke(this, EventArgs.Empty);
		}

		#endregion

		#region ================== Events

		//mxd. Stop notify animation if user selects animated tab
		private void OnSelectedIndexChanged(object sender, EventArgs e)
		{
			// Stop animation
			if(notifytab != -1 && this.SelectedIndex == notifytab)
			{
				notifytimer.Stop();
				notifytab = -1;
				notifycounter = 0;
				NotifyChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		// Tabs don't process keys
		protected override void OnKeyDown(KeyEventArgs ke)
		{
			DockersControl docker = this.Parent as DockersControl;

			// Only absorb the key press when no focused on an input control, otherwise
			// the input controls may not receive certain keys such as delete and arrow keys
			if(docker != null && !docker.IsFocused) ke.Handled = true;
		}

		// Tabs don't process keys
		protected override void OnKeyUp(KeyEventArgs e)
		{
			DockersControl docker = this.Parent as DockersControl;

			// Only absorb the key press when no focused on an input control, otherwise
			// the input controls may not receive certain keys such as delete and arrow keys
			if(docker != null && !docker.IsFocused) e.Handled = true;
		}

		//mxd. Update notyfy animation
		private void NotifyTimerOnTick(object sender, EventArgs eventArgs)
		{
			if(notifycounter++ == NOTIFY_BLINK_COUNT)
			{
				notifytimer.Stop();
				notifycounter = 0;
				notifytab = -1;
			}

			// Trigger redraw
			NotifyChanged?.Invoke(this, EventArgs.Empty);
		}

		#endregion
	}
}
