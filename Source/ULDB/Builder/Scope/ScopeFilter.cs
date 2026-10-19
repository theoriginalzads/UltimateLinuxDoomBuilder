// ULDB: the fork targets GZDoom and UZDoom only. Everything else upstream supports stays
// in the code and config files (so upstream merges stay clean) but is hidden in the UI
// through this one class. Every hidden item is listed in docs/scope-hidden.md.
//
// UI code asks ScopeFilter before showing a game configuration, node builder or script
// compiler. Anything new arriving from upstream is hidden unless it matches the rules here.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ULDB.Scope
{
	public static class ScopeFilter
	{
		/// <summary>Configuration for new maps.</summary>
		public const string DEFAULT_CONFIGURATION = "UZDoom_DoomUDMF.cfg";

		// Game configuration file names: <Engine>_<Game><Format>.cfg, e.g. "GZDoom_HexenUDMF.cfg".
		private static readonly Regex CONFIG_NAME = new Regex(@"^(?<engine>[A-Za-z0-9]+)_(?<game>Doom2|Doom|Heretic|Hexen|Strife)(?<format>Doom|Hexen|UDMF)\.cfg$", RegexOptions.IgnoreCase);
		private static readonly string[] VISIBLE_ENGINES = { "UZDoom", "GZDoom" };

		// Script configurations (Scripting/*.cfg) whose compilers make sense for GZDoom/UZDoom.
		private static readonly HashSet<string> VISIBLE_SCRIPT_COMPILERS = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"zdoom_acs.cfg", // ZDoom ACC
			"zt_bcs.cfg",    // ZT-BCC
			"bcs.cfg",       // BCC
		};

		/// <summary>True when a game configuration (by file name) is offered in the UI.</summary>
		public static bool IsConfigurationVisible(string filename)
		{
			Match m = CONFIG_NAME.Match(Path.GetFileName(filename ?? ""));
			return m.Success && VISIBLE_ENGINES.Any(e => string.Equals(e, m.Groups["engine"].Value, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>True when a node builder configuration (e.g. "zdbsp_udmf_normal") is offered in the UI.
		/// ZDBSP is the one GZDoom-relevant node builder ULDB builds; "none" is always allowed.</summary>
		public static bool IsNodebuilderVisible(string name)
		{
			return string.IsNullOrEmpty(name) || name.StartsWith("zdbsp_", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>True when a script configuration (by file name, e.g. "zdoom_acs.cfg") is offered as a compiler.</summary>
		public static bool IsScriptCompilerVisible(string filename)
		{
			return VISIBLE_SCRIPT_COMPILERS.Contains(Path.GetFileName(filename ?? ""));
		}

		/// <summary>
		/// For a map last edited with a hidden configuration (e.g. "Boom_Doom2Doom.cfg"), the visible
		/// configuration to open it with instead: same game and map format, on UZDoom where it has a
		/// matching config (UDMF), otherwise GZDoom. Returns the input when it's already visible.
		/// GZDoom and UZDoom play vanilla, Boom and ZDoom-family maps, so this is safe to offer.
		/// </summary>
		public static string SuggestConfiguration(string filename, IEnumerable<string> available)
		{
			List<string> visible = available.Where(IsConfigurationVisible).ToList();
			if(IsConfigurationVisible(filename) && visible.Contains(filename, StringComparer.OrdinalIgnoreCase)) return filename;

			Match m = CONFIG_NAME.Match(Path.GetFileName(filename ?? ""));
			if(m.Success)
			{
				string game = m.Groups["game"].Value.Equals("Doom2", StringComparison.OrdinalIgnoreCase) ? "Doom" : m.Groups["game"].Value;
				string format = m.Groups["format"].Value;
				foreach(string engine in VISIBLE_ENGINES)
				{
					string candidate = visible.FirstOrDefault(v => string.Equals(v, engine + "_" + game + format + ".cfg", StringComparison.OrdinalIgnoreCase));
					if(candidate != null) return candidate;
				}
			}

			return visible.FirstOrDefault(v => string.Equals(v, DEFAULT_CONFIGURATION, StringComparison.OrdinalIgnoreCase)) ?? visible.FirstOrDefault();
		}
	}
}
