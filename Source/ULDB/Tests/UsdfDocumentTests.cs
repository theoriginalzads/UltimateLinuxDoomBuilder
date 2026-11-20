using System.Linq;
using ULDB.Dialogue;

namespace ULDB.Tests
{
	// The dialogue builder's reader and writer (Dialogue/UsdfDocument.cs).
	public class UsdfDocumentTests
	{
		private const string SAMPLE = @"namespace = ""GZDoom"";
include = ""DIALOG01"";

// The barkeep
conversation
{
	actor = ""Barkeep"";
	id = 3;
	customfield = 12;

	page
	{
		pagename = ""start"";
		name = ""Barkeep"";
		panel = ""M_BARKP"";
		dialog = ""What'll it be? He said \""hi\"" and left."";
		ifitem { item = ""BlueCard""; amount = 1; }
		link = ""secret"";

		choice
		{
			text = ""A drink (10 gold)"";
			cost { item = ""Coin""; amount = 10; }
			displaycost = true;
			yesmessage = ""Cheers."";
			nomessage = ""No money, no drink."";
			giveitem = ""HealthBonus"";
			nextpage = ""start"";
		}
		choice
		{
			text = ""Open the back door"";
			require { item = ""BlueCard""; amount = 1; }
			exclude { item = ""RedCard""; amount = 1; }
			special = 80;
			arg0 = 12;
			arg1 = 0;
			closedialog = true;
		}
	}

	page
	{
		pagename = ""secret"";
		name = ""Barkeep"";
		dialog = ""You found it."";
		goodbye = ""See you."";
	}
}
";

		[Fact]
		public void ReadsConversationsPagesAndChoices()
		{
			UsdfDocument d = UsdfDocument.Parse(SAMPLE);
			Assert.Equal("GZDoom", d.Namespace);
			Assert.True(d.IsGZDoom);
			Assert.Equal(new[] { "DIALOG01" }, d.Includes);
			UsdfConversation c = Assert.Single(d.Conversations);
			Assert.Equal("Barkeep", c.Actor.Text);
			Assert.Equal(3, c.Id);
			Assert.Equal("customfield", Assert.Single(c.Extra).Key);
			Assert.Equal(2, c.Pages.Count);
			UsdfPage p = c.Pages[0];
			Assert.Equal("start", p.PageName);
			Assert.Equal("What'll it be? He said \"hi\" and left.", p.Dialog);
			Assert.Equal("BlueCard", Assert.Single(p.IfItems).Item.Text);
			Assert.Equal("secret", p.Link.Text);
			Assert.Equal(2, p.Choices.Count);
			UsdfChoice a = p.Choices[0], b = p.Choices[1];
			Assert.Equal(10, Assert.Single(a.Costs).Amount);
			Assert.True(a.DisplayCost);
			Assert.Equal("start", a.NextPage.Text);
			Assert.Equal(UsdfValue.ValueKind.String, a.NextPage.Kind);
			Assert.Equal(80, b.Special);
			Assert.Equal(12, b.Args[0].AsInt());
			Assert.True(b.CloseDialog);
			Assert.Single(b.Requires);
			Assert.Single(b.Excludes);
			Assert.Equal("See you.", c.Pages[1].Goodbye);
		}

		[Fact]
		public void WritingAndReadingAgainKeepsEverythingButComments()
		{
			UsdfDocument d = UsdfDocument.Parse(SAMPLE);
			string written = d.Write();
			Assert.DoesNotContain("//", written);
			Assert.False(UsdfDocument.HasComments(written));
			Assert.True(UsdfDocument.HasComments(SAMPLE));
			UsdfDocument again = UsdfDocument.Parse(written);
			Assert.Equal(written, again.Write()); // stable
			Assert.Contains("customfield = 12;", written);
			Assert.Contains("dialog = \"What'll it be? He said \\\"hi\\\" and left.\";", written);
			Assert.Equal(d.Conversations[0].Pages[0].Choices.Count, again.Conversations[0].Pages[0].Choices.Count);
			Assert.Equal(80, again.Conversations[0].Pages[0].Choices[1].Special);
		}

		[Fact]
		public void StrifeNumbersStayNumbers()
		{
			UsdfDocument d = UsdfDocument.Parse("namespace = \"Strife\";\nconversation { actor = 64; page { dialog = \"x\"; choice { text = \"y\"; nextpage = 2; giveitem = 168; } } }");
			Assert.False(d.UsesClassNames);
			Assert.Equal(UsdfValue.ValueKind.Integer, d.Conversations[0].Actor.Kind);
			string w = d.Write();
			Assert.Contains("actor = 64;", w);
			Assert.Contains("nextpage = 2;", w);
			Assert.Contains("giveitem = 168;", w);
		}

		[Theory]
		[InlineData("conversation { page { dialog = \"x\" } }", 0, "Missing ';'")]
		[InlineData("conversation\n{\n\tpage\n\t{\n", 4, "not closed")]
		[InlineData("conversation { }\n}", 1, "no block")]
		[InlineData("conversation { page { dialog = \"x; } }", 0, "String is not closed")]
		public void ReportsWhereItCantRead(string text, int line, string message)
		{
			UsdfException e = Assert.Throws<UsdfException>(() => UsdfDocument.Parse(text));
			Assert.Contains(message, e.Message);
			Assert.Equal(line, e.Line);
		}
	}
}

namespace ULDB.Tests
{
	// What the dialogue builder points out (Dialogue/DialogueChecks.cs).
	public class DialogueChecksTests
	{
		[Fact]
		public void FindsBrokenLinksAndMissingText()
		{
			UsdfDocument d = UsdfDocument.Parse(@"namespace = ""GZDoom"";
conversation { actor = ""Barkeep""; id = 1;
	page { pagename = ""a""; dialog = ""Hi""; link = 3;
		choice { text = ""x""; nextpage = ""nowhere""; }
		choice { text = """"; nextpage = 2; cost { item = ""Clipp""; amount = 1; } } }
	page { pagename = ""a""; dialog = """"; } }
conversation { id = 1; page { dialog = ""y""; } }");
			System.Collections.Generic.List<string> found = ULDB.Dialogue.DialogueChecks.Check(d, new[] { "Barkeep", "Clip" });
			string all = string.Join("\n", found);
			Assert.Contains("goes to page 3, which doesn't exist", all);
			Assert.Contains("goes to page \"nowhere\", which doesn't exist", all);
			Assert.Contains("choice 2 has no text", all);
			Assert.Contains("\"Clipp\" isn't a class the map knows", all);
			Assert.Contains("page 2 has no dialogue", all);
			Assert.Contains("the page name \"a\" is used twice", all);
			Assert.Contains("Conversation 2 has no actor", all);
			Assert.Contains("ID 1 is used by two conversations", all);
			Assert.DoesNotContain("page 2, which", all); // nextpage = 2 exists
		}

		[Fact]
		public void ValidDialogueHasNoProblems()
		{
			UsdfDocument d = UsdfDocument.Parse("namespace = \"GZDoom\"; conversation { actor = \"Barkeep\"; id = 1; page { pagename = \"a\"; dialog = \"Hi\"; choice { text = \"x\"; nextpage = \"a\"; giveitem = \"Clip\"; } } }");
			Assert.Empty(ULDB.Dialogue.DialogueChecks.Check(d, new[] { "Barkeep", "Clip" }));
		}
	}
}
