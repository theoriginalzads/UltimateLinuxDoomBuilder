// ULDB: blocking waits on the UI thread. Upstream code expects MessageBox.Show,
// ShowDialog and file dialogs to return the answer; Avalonia's are async. These run a
// nested dispatcher frame (the UI keeps working) until the task completes.

using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ULDB.Shell
{
	internal static class Modal
	{
		public static T Wait<T>(Task<T> task)
		{
			Wait((Task)task);
			return task.GetAwaiter().GetResult();
		}

		public static void Wait(Task task)
		{
			if(!task.IsCompleted)
			{
				DispatcherFrame frame = new DispatcherFrame();
				task.ContinueWith(_ => Dispatcher.UIThread.Post(() => frame.Continue = false), TaskScheduler.Default);
				Dispatcher.UIThread.PushFrame(frame);
			}
			task.GetAwaiter().GetResult();
		}

		/// <summary>Shows a window modally (over the owner, if any) and waits until it closes.</summary>
		public static void ShowDialog(Window dialog, Window owner)
		{
			if(owner != null && owner.IsVisible)
			{
				Wait(dialog.ShowDialog(owner));
			}
			else
			{
				TaskCompletionSource closed = new TaskCompletionSource();
				dialog.Closed += (s, e) => closed.TrySetResult();
				dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
				dialog.Show();
				Wait(closed.Task);
			}
		}
	}
}
