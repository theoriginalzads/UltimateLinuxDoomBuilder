// ULDB: upstream's ToastControl (a small notification that slides into the map view) as a model:
// its size, text and lifetime. ToastManager (upstream, unchanged) adds it to the display control,
// moves it and removes it; ToastOverlay (Shell) draws it.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Controls
{
	internal partial class ToastControl : UserControl
	{
		private const int WIDTH = 380;
		private long starttime;
		private readonly long lifetime;
		private bool pausedecay, remove;

		public ToastType Type { get; }
		public string Title { get; }
		public string Message { get; }

		/// <summary>Set by the view while the pointer is over the toast (it then doesn't decay).</summary>
		internal bool Hovered { get; set; }

		public ToastControl(ToastType type, string title, string text, long lifetime = 3000)
		{
			Type = type; Title = title; Message = text;
			this.lifetime = lifetime;
			starttime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			Margin = new Padding(10);

			// Height by the text: about 52 chars a line at the toast's width
			int lines = Math.Max(1, (int)Math.Ceiling((text ?? "").Length / 52.0) + (text ?? "").Split('\n').Length - 1);
			Size = new Size(WIDTH, 40 + lines * 17 + 12);
			// Starts outside the view; ToastManager slides it in
			Location = new Point(0, 100000);
		}

		/// <summary>Whether the toast is decaying (the pointer isn't over it).</summary>
		public void CheckDecay()
		{
			if(Hovered) pausedecay = true;
			else if(pausedecay)
			{
				pausedecay = false;
				starttime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
			}
		}

		public bool IsAlive()
		{
			return !(remove || (!pausedecay && DateTimeOffset.Now.ToUnixTimeMilliseconds() - starttime > lifetime));
		}

		/// <summary>The close button was clicked.</summary>
		internal void RequestRemove() { remove = true; }
	}
}
