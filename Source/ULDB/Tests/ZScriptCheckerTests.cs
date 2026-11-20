using System.Collections.Generic;
using CodeImp.DoomBuilder.Compilers;
using ULDB.Scripting;

namespace ULDB.Tests
{
	// The quick ZScript check of the script editor (Scripting/ZScriptChecker.cs).
	public class ZScriptCheckerTests
	{
		private const string GOOD = @"version ""4.10""
#include ""ZSACTORS""

// A comment with { and ( in it
/* and a block
   comment */
const MYCONST = 5;
enum EFlags { EF_ONE = 1, EF_TWO = 2 };

class TestImp : DoomImp replaces DoomImp
{
	int counter;

	Default
	{
		Health 100;
		Speed 10;
		+SOLID +SHOOTABLE
		-NOGRAVITY
		Obituary ""%o was clawed"";
		DropItem ""Clip"", 255;
		Monster;
	}

	override void Tick()
	{
		Super.Tick();
		if(counter++ > 35) { counter = 0; }
		string s = ""a } string"";
		name n = 'SomeName';
	}

	States
	{
	Spawn:
		TROO AB 10 A_Look;
		Loop;
	See:
		TROO AABBCCDD 3 A_Chase;
		TROO A 0 { A_StartSound(""imp/active""); return ResolveState(null); }
		TROO B 1 Bright Light(""X"") Offset(1, 2);
		Loop;
	Death.Fire:
		TROO I 8 A_Scream;
		#### # -1;
		""####"" ""#"" 1;
		Stop;
	Pain:
		Goto See+2;
	}
}

extend class Actor
{
	void MyHelper() { }
}

struct MyData
{
	int a;
}
";

		private static List<CompilerError> Check(string text) => ZScriptChecker.Check(text, "?ZSCRIPT");

		[Fact]
		public void ValidScriptHasNoErrors()
		{
			List<CompilerError> errors = Check(GOOD);
			Assert.True(errors.Count == 0, errors.Count > 0 ? errors[0].description + " (line " + (errors[0].linenumber + 1) + ")" : "");
		}

		// The engines' own ZScript is valid: the check must not complain about any of it. ULDB_ZSCRIPT_CORPUS names
		// folders of .zs files (GZDoom's and UZDoom's wadsrc/static/zscript, separated by ':').
		[SkippableFact]
		public void EnginesZScriptHasNoErrors()
		{
			string corpus = System.Environment.GetEnvironmentVariable("ULDB_ZSCRIPT_CORPUS");
			Skip.If(string.IsNullOrEmpty(corpus), "Set ULDB_ZSCRIPT_CORPUS to folders of engine ZScript to run this");
			List<string> failures = new List<string>();
			int files = 0;
			foreach(string dir in corpus.Split(':'))
				foreach(string file in System.IO.Directory.GetFiles(dir, "*.zs", System.IO.SearchOption.AllDirectories))
				{
					files++;
					foreach(CompilerError e in Check(System.IO.File.ReadAllText(file)))
						failures.Add(file + ":" + (e.linenumber + 1) + ": " + e.description);
				}
			Assert.True(files > 0);
			Assert.True(failures.Count == 0, failures.Count + " false errors in " + files + " files:\n" + string.Join("\n", failures.GetRange(0, System.Math.Min(30, failures.Count))));
		}

		// #include in a WAD: GZDoom finds the lump by its short name; paths only work in a PK3
		[Fact]
		public void IncludesNameLumpsOfTheWad()
		{
			string text = "#include \"ZSACTORS\"\n  #INCLUDE \"zsweap\"\n#include \"zscript/actors.zs\"\n#include \"TOOLONGNAME\"\n// #include \"NOTME\"\n";
			Assert.Equal(new[] { "ZSACTORS", "ZSWEAP" }, CodeImp.DoomBuilder.Controls.ScriptWadLumpDocumentTab.Includes(text));
		}

		[Theory]
		[InlineData("class A : Actor\n{\n\tStates\n\t{\n\tSpawn:\n\t\tTROO A 10\n\t\tTROO B 10;\n\t\tLoop;\n\t}\n}\n", 5, "Missing ';'")]
		[InlineData("class A : Actor\n{\n\tStates\n\t{\n\tSpawn:\n\t\tTROO A;\n\t}\n}\n", 5, "duration")]
		[InlineData("class A : Actor\n{\n\tStates\n\t{\n\tSpawn:\n\t\tTROO A 10;\n\t\tLoop\n\tDeath:\n\t\tStop;\n\t}\n}\n", 6, "Missing ';'")]
		[InlineData("class A : Actor\n{\n\tDefault\n\t{\n\t\tHealth 100\n\t\tSpeed 10;\n\t}\n}\n", 4, "Missing ';'")]
		[InlineData("class A : Actor\n{\n\tvoid F()\n\t{\n\t\tif(x) {\n\t}\n}\n", 1, "never closed")]
		[InlineData("class A : Actor\n{\n\tvoid F()\n\t{\n\t\tG(1, 2];\n\t}\n}\n", 4, "Expected ')'")]
		[InlineData("class A : Actor\n{\n\tvoid F()\n\t{\n\t\tstring s = \"oops;\n\t}\n}\n", 4, "String is not closed")]
		[InlineData("/* never closed\nclass A : Actor {}\n", 0, "never closed")]
		[InlineData("class A : Actor {}\nint x;\n", 1, "outside a class")]
		[InlineData("#define X 1\nclass A : Actor {}\n", 0, "Unknown directive")]
		[InlineData("class A : Actor {}\n}\n", 1, "nothing to close")]
		public void FindsTheMistake(string text, int line, string message)
		{
			List<CompilerError> errors = Check(text);
			Assert.NotEmpty(errors);
			Assert.Contains(message, errors[0].description);
			Assert.Equal(line, errors[0].linenumber);
			Assert.Equal("?ZSCRIPT", errors[0].filename);
		}
	}
}
