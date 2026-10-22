// ULDB: launch options for the engine families ULDB targets (GZDoom and UZDoom, which share their command line),
// so the Game Configurations' Testing tab can offer them as choices instead of making the user know the switches.
// An option is on when its arguments are in the test parameters; turning one on or off edits that text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ULDB.Platform
{
	public sealed class LaunchOption
	{
		public LaunchOption(string label, string arguments, string tip) { Label = label; Arguments = arguments; Tip = tip; }
		public string Label { get; }
		public string Arguments { get; }
		public string Tip { get; }
	}

	public static class EnginePresets
	{
		public static readonly string[] Families = { "GZDoom", "UZDoom" };

		// GZDoom and UZDoom (a GZDoom fork) take the same ZDoom switches.
		private static readonly LaunchOption[] zdoomfamily =
		{
			new LaunchOption("Windowed", "+vid_fullscreen 0", "Run in a window, so the editor stays in reach"),
			new LaunchOption("No monsters", "-nomonsters", "Start without any monsters"),
			new LaunchOption("Fast monsters", "-fast", "Monsters move and attack as on Nightmare"),
			new LaunchOption("Respawning monsters", "-respawn", "Monsters come back after they're killed"),
			new LaunchOption("Allow cheats", "+sv_cheats 1", "Lets you use console cheats such as noclip and god"),
			new LaunchOption("No sound", "-nosound", "Don't start the sound system"),
			new LaunchOption("No music", "-nomusic", "Don't play music"),
		};

		public static IReadOnlyList<LaunchOption> OptionsFor(string family) =>
			Families.Contains(family) ? zdoomfamily : new LaunchOption[0];

		/// <summary>Splits on spaces outside double quotes, keeping the quotes (so the text can be put back together).</summary>
		public static List<string> Tokens(string text)
		{
			List<string> tokens = new List<string>();
			System.Text.StringBuilder current = new System.Text.StringBuilder();
			bool inquotes = false;
			foreach(char c in text ?? "")
			{
				if(c == '"') inquotes = !inquotes;
				if(char.IsWhiteSpace(c) && !inquotes) { if(current.Length > 0) { tokens.Add(current.ToString()); current.Clear(); } }
				else current.Append(c);
			}
			if(current.Length > 0) tokens.Add(current.ToString());
			return tokens;
		}

		private static int IndexOf(List<string> tokens, List<string> wanted)
		{
			for(int i = 0; i + wanted.Count <= tokens.Count; i++)
			{
				bool same = true;
				for(int j = 0; j < wanted.Count && same; j++) same = string.Equals(tokens[i + j], wanted[j], StringComparison.OrdinalIgnoreCase);
				if(same) return i;
			}
			return -1;
		}

		public static bool IsSet(string parameters, LaunchOption option) => IndexOf(Tokens(parameters), Tokens(option.Arguments)) >= 0;

		/// <summary>The parameters with the option's arguments added (at the end) or removed.</summary>
		public static string Set(string parameters, LaunchOption option, bool on)
		{
			List<string> tokens = Tokens(parameters);
			List<string> wanted = Tokens(option.Arguments);
			int at = IndexOf(tokens, wanted);
			if(on && at < 0) tokens.AddRange(wanted);
			else if(!on && at >= 0) tokens.RemoveRange(at, wanted.Count);
			return string.Join(" ", tokens);
		}
	}
}
