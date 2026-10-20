using System.Text.RegularExpressions;
using System.Windows.Forms;
using ULDB.Input;
using AKey = Avalonia.Input.Key;
using AKeyModifiers = Avalonia.Input.KeyModifiers;

namespace ULDB.Tests
{
	public class KeyMappingTests
	{
		[Theory]
		[InlineData(AKey.A, AKeyModifiers.None, Keys.A)]
		[InlineData(AKey.S, AKeyModifiers.Control, Keys.Control | Keys.S)]
		[InlineData(AKey.O, AKeyModifiers.Control | AKeyModifiers.Shift, Keys.Control | Keys.Shift | Keys.O)] // 196687 in Actions.cfg
		[InlineData(AKey.F12, AKeyModifiers.Control, Keys.Control | Keys.F12)]
		[InlineData(AKey.Delete, AKeyModifiers.Control, Keys.Control | Keys.Delete)]
		[InlineData(AKey.G, AKeyModifiers.Alt, Keys.Alt | Keys.G)]
		[InlineData(AKey.D5, AKeyModifiers.None, Keys.D5)]
		[InlineData(AKey.NumPad5, AKeyModifiers.None, Keys.NumPad5)]
		[InlineData(AKey.PageUp, AKeyModifiers.None, Keys.Prior)]
		[InlineData(AKey.Enter, AKeyModifiers.None, Keys.Return)]
		[InlineData(AKey.OemOpenBrackets, AKeyModifiers.None, Keys.OemOpenBrackets)]
		public void MapsToWinFormsCodes(AKey key, AKeyModifiers modifiers, Keys expected)
		{
			Assert.Equal(expected, KeyMapping.ToKeys(key, modifiers));
		}

		[Fact]
		public void EveryDefaultShortcutIsReachable()
		{
			// Every default shortcut in upstream's Actions.cfg files (core and in-scope plugins)
			// must be producible from some Avalonia key, or that action could never be triggered.
			string source = Path.Combine(HeadlessEditor.RepoRoot, "Source");
			string[] files = new[] { Path.Combine(source, "Core", "Resources", "Actions.cfg") }
				.Concat(new[] { "BuilderModes", "3DFloorMode", "BuilderEffects", "StairSectorBuilder", "TagRange", "TagExplorer", "CommentsPanel", "ColorPicker", "SoundPropagationMode" }
					.Select(p => Path.Combine(source, "Plugins", p, "Resources", "Actions.cfg")))
				.Where(File.Exists).ToArray();
			Assert.NotEmpty(files);

			HashSet<Keys> reachable = KeyMapping.All.Values.ToHashSet();
			List<string> missing = new List<string>();
			foreach(string file in files)
			{
				foreach(Match m in Regex.Matches(File.ReadAllText(file), @"default\s*=\s*(\d+)\s*;"))
				{
					Keys code = (Keys)int.Parse(m.Groups[1].Value) & Keys.KeyCode;
					// Mouse buttons and UDB's scroll pseudo-keys come from mouse input, not the keyboard.
					if(code == Keys.None || code <= Keys.XButton2 || (int)code >= 65530) continue;
					if(!reachable.Contains(code)) missing.Add(Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(file))) + ": " + code);
				}
			}
			Assert.Empty(missing);
		}

		[Fact]
		public void NoTwoKeysCollideExceptLeftRightModifiers()
		{
			var duplicates = KeyMapping.All.GroupBy(kv => kv.Value).Where(g => g.Count() > 1)
				.Where(g => g.Key != Keys.ShiftKey && g.Key != Keys.ControlKey && g.Key != Keys.Menu);
			Assert.Empty(duplicates);
		}
	}
}
