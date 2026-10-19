// ULDB: process timing that works on Linux. Upstream reads Process.StartTime after the
// process has exited; on Linux .NET reads it from /proc, which is gone by then, so it
// throws. Record the start time while the process is alive instead.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ULDB.Platform
{
	public static class Processes
	{
		private static readonly ConditionalWeakTable<Process, StrongBox<DateTime>> starttimes = new ConditionalWeakTable<Process, StrongBox<DateTime>>();

		/// <summary>Call right after starting a process.</summary>
		public static void Started(Process process)
		{
			DateTime start;
			try { start = process.StartTime; }
			catch(InvalidOperationException) { start = DateTime.Now; } // Already exited
			starttimes.AddOrUpdate(process, new StrongBox<DateTime>(start));
		}

		/// <summary>How long an exited process ran (zero if Started wasn't called for it).</summary>
		public static TimeSpan RunTime(Process process)
		{
			if(!starttimes.TryGetValue(process, out StrongBox<DateTime> start)) return TimeSpan.Zero;
			DateTime end;
			try { end = process.ExitTime; }
			catch(InvalidOperationException) { end = DateTime.Now; }
			return end - start.Value;
		}
	}
}
