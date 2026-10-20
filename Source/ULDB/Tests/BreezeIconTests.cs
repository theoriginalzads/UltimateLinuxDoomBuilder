using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SD = System.Drawing;

namespace ULDB.Tests
{
	// ULDB's Breeze-style icons (Shell/BreezeIcons) and the lookup that matches upstream's
	// resource images to them (Shell/ResourceImageNames). Both are internal to the Builder
	// assembly, so they're reached by reflection.
	public class BreezeIconTests
	{
		private static readonly Assembly builder = typeof(CodeImp.DoomBuilder.General).Assembly;
		private static readonly Type icons = builder.GetType("ULDB.Shell.BreezeIcons", true);
		private static readonly Type names = builder.GetType("ULDB.Shell.ResourceImageNames", true);

		private static Dictionary<string, string> Field(string name) =>
			(Dictionary<string, string>)icons.GetField(name, BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);

		private static string KeyFor(SD.Image image) => (string)names.GetMethod("KeyFor").Invoke(null, new object[] { image });

		// Arguments per SVG path command
		private static readonly Dictionary<char, int> ARGS = new Dictionary<char, int>
		{
			{ 'M', 2 }, { 'L', 2 }, { 'H', 1 }, { 'V', 1 }, { 'A', 7 }, { 'C', 6 }, { 'Q', 4 }, { 'T', 2 }, { 'Z', 0 },
		};

		[Fact]
		public void EverySpecIsWellFormed()
		{
			Regex head = new Regex(@"^[sf][iaargoyw](\.\d+)?$");
			foreach(KeyValuePair<string, string> spec in Field("specs"))
			{
				foreach(string layer in spec.Value.Split(';'))
				{
					string l = layer.Trim();
					int space = l.IndexOf(' ');
					Assert.True(space > 0, spec.Key + ": layer without path: " + l);
					Assert.Matches(head, l.Substring(0, space));

					// Commands with a whole number of argument sets
					MatchCollection tokens = Regex.Matches(l.Substring(space + 1), @"[A-Za-z]|-?\d*\.?\d+");
					Assert.True(tokens.Count > 0 && tokens[0].Value == "M", spec.Key + ": path must start with M");
					char cmd = ' ';
					int count = 0;
					void Check() { if(cmd != ' ') Assert.True(ARGS[cmd] == 0 ? count == 0 : count > 0 && count % ARGS[cmd] == 0, spec.Key + ": bad argument count for " + cmd); }
					foreach(Match t in tokens)
					{
						if(char.IsLetter(t.Value[0]))
						{
							Check();
							cmd = t.Value[0];
							Assert.True(ARGS.ContainsKey(cmd), spec.Key + ": unknown command " + cmd);
							count = 0;
						}
						else count++;
					}
					Check();
				}
			}
		}

		[Fact]
		public void AliasesPointAtIcons()
		{
			Dictionary<string, string> specs = Field("specs");
			foreach(KeyValuePair<string, string> alias in Field("aliases"))
				Assert.True(specs.ContainsKey(alias.Value), alias.Key + " → " + alias.Value + " has no icon");
		}

		[Fact]
		public void ResourceImagesAreRecognised()
		{
			// Same image object kinds upstream uses: a Properties.Resources bitmap...
			Type resources = builder.GetType("CodeImp.DoomBuilder.Properties.Resources", true);
			SD.Bitmap reset = (SD.Bitmap)resources.GetProperty("Reset", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
			Assert.Equal("res:Reset", KeyFor(reset));
			Assert.True((bool)icons.GetMethod("Has").Invoke(null, new object[] { KeyFor(reset) }));

			// ...and a form .resx copy of the same picture (Edit Sector's reset buttons).
			string png = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Builder", "FormResources", "SectorEditFormUDMF.resetfloorlight.Image.png");
			using(SD.Bitmap copy = new SD.Bitmap(png))
				Assert.Equal("res:Reset", KeyFor(copy));

			// A picture that isn't a resource has no key.
			using(SD.Bitmap blank = new SD.Bitmap(16, 16))
				Assert.Null(KeyFor(blank));
		}
	}
}
