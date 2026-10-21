// ULDB: extra colour presets for the script editor (Preferences → Script Editor → Preset), the
// well-known palettes people know from VS Code and other editors (issue #20). Each is the 18
// colours the upstream presets set, in the order of ScriptColorPresets.Fields.

using System;
using System.Collections.Generic;
using System.Linq;
using CodeImp.DoomBuilder.Rendering;

namespace ULDB.Scripting
{
	internal static class ScriptColorPresets
	{
		/// <summary>The colours of a preset, in this order.</summary>
		public static readonly string[] Fields =
		{
			"background", "linenumbers", "plaintext", "comments", "keywords", "properties", "literals", "constants", "strings",
			"includes", "indicator", "brace", "bracebad", "whitespace", "foldfore", "foldback", "selectionfore", "selectionback"
		};

		private static readonly Dictionary<string, string[]> presets = new Dictionary<string, string[]>
		{
			["Monokai"] = Hex("272822 90908A F8F8F2 75715E F92672 A6E22E AE81FF 66D9EF E6DB74 FD971F 3E3D32 4E7A3C FF5555 49483E 75715E 3E3D32 F8F8F2 49483E"),
			["Dracula"] = Hex("282A36 6272A4 F8F8F2 6272A4 FF79C6 50FA7B BD93F9 8BE9FD F1FA8C FFB86C 44475A 3C7A4E FF5555 44475A 6272A4 21222C F8F8F2 44475A"),
			["One Dark"] = Hex("282C34 495162 ABB2BF 5C6370 C678DD 61AFEF D19A66 E5C07B 98C379 56B6C2 2C313C 3E6B4E E06C75 3B4048 5C6370 21252B ABB2BF 3E4451"),
			["Nord"] = Hex("2E3440 4C566A D8DEE9 616E88 81A1C1 88C0D0 B48EAD 8FBCBB A3BE8C EBCB8B 3B4252 4F7A5E BF616A 434C5E 4C566A 3B4252 ECEFF4 434C5E"),
			["Gruvbox Dark"] = Hex("282828 7C6F64 EBDBB2 928374 FB4934 FABD2F D3869B FE8019 B8BB26 83A598 3C3836 5A7A3C FB4934 504945 7C6F64 3C3836 EBDBB2 504945"),
			["Solarized Dark"] = Hex("002B36 586E75 839496 586E75 859900 268BD2 D33682 CB4B16 2AA198 B58900 073642 2A6F5F DC322F 0F4350 586E75 073642 FDF6E3 0A4A5A"),
			["Solarized Light"] = Hex("FDF6E3 93A1A1 657B83 93A1A1 859900 268BD2 D33682 CB4B16 2AA198 B58900 EEE8D5 8AD6C6 DC322F D5CFB9 93A1A1 EEE8D5 FDF6E3 93A1A1"),
			["GitHub Light"] = Hex("FFFFFF 8C959F 24292F 6E7781 CF222E 8250DF 0550AE 953800 0A3069 116329 F6F8FA 9BE9A8 CF222E D0D7DE 8C959F F6F8FA 24292F B6E3FF"),
		};

		/// <summary>Preset names, in the order they are listed after upstream's own.</summary>
		public static IEnumerable<string> Names => presets.Keys;

		public static bool TryGet(string name, out PixelColor[] colors)
		{
			colors = null;
			if(name == null || !presets.TryGetValue(name, out string[] hex)) return false;
			colors = hex.Select(h =>
			{
				int v = Convert.ToInt32(h, 16);
				return new PixelColor(255, (byte)(v >> 16), (byte)(v >> 8), (byte)v);
			}).ToArray();
			return true;
		}

		private static string[] Hex(string list) => list.Split(' ');
	}
}
