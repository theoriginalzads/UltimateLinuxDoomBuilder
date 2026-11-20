// ULDB: Strife dialogue (USDF, and ZDoom's/GZDoom's extensions: specs/usdf*.txt in GZDoom) as a model the
// dialogue builder edits: conversations, their pages, and the pages' choices. Read from the DIALOGUE lump's
// text and written back in a standard layout. Fields the builder doesn't know are kept and written back;
// comments and the original layout are not (the builder says so before replacing the text).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ULDB.Dialogue
{
	/// <summary>A value as written in the text: a string, an integer, a float, true/false or a bare word.</summary>
	public sealed class UsdfValue
	{
		public enum ValueKind { String, Integer, Float, Boolean, Word }

		public ValueKind Kind { get; }
		public string Text { get; } // the string's content (unescaped), or the token as written

		public UsdfValue(ValueKind kind, string text) { Kind = kind; Text = text ?? ""; }

		public static UsdfValue Of(string s) => new UsdfValue(ValueKind.String, s);
		public static UsdfValue Of(int i) => new UsdfValue(ValueKind.Integer, i.ToString(CultureInfo.InvariantCulture));
		public static UsdfValue Of(bool b) => new UsdfValue(ValueKind.Boolean, b ? "true" : "false");

		public bool IsNumber => Kind == ValueKind.Integer || Kind == ValueKind.Float;

		public int AsInt(int fallback = 0)
		{
			if(Kind == ValueKind.Integer && int.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) return i;
			if(Kind == ValueKind.Float && double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return (int)d;
			return fallback;
		}

		public bool AsBool() => Kind == ValueKind.Boolean ? Text.Equals("true", StringComparison.OrdinalIgnoreCase) : AsInt() != 0;

		public string Write()
		{
			if(Kind != ValueKind.String) return Text;
			return "\"" + Text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
		}

		public override string ToString() => Kind == ValueKind.String ? Text : Text;
	}

	/// <summary>A field or block the builder doesn't edit; kept as it was.</summary>
	public sealed class UsdfExtra
	{
		public string Key;
		public UsdfValue Value;        // a field
		public List<UsdfExtra> Block;  // or a block of them
	}

	/// <summary>An item check: ifitem (page), cost, require, exclude (choice).</summary>
	public sealed class UsdfItemCheck
	{
		public UsdfValue Item = UsdfValue.Of("");
		public int Amount = 1;
		public List<UsdfExtra> Extra = new List<UsdfExtra>();
	}

	public sealed class UsdfChoice
	{
		public string Text = "";
		public bool DisplayCost;
		public string YesMessage, NoMessage, Log;
		public UsdfValue GiveItem;
		public int Special;
		public string SpecialName;
		public UsdfValue[] Args = new UsdfValue[5];
		public UsdfValue NextPage; // page number (from 1) or, in the GZDoom namespace, a page name
		public bool CloseDialog;
		public List<UsdfItemCheck> Costs = new List<UsdfItemCheck>();
		public List<UsdfItemCheck> Requires = new List<UsdfItemCheck>();
		public List<UsdfItemCheck> Excludes = new List<UsdfItemCheck>();
		public List<UsdfExtra> Extra = new List<UsdfExtra>();
		public bool HasDisplayCost; // displaycost was written (kept so an explicit "false" survives)
	}

	public sealed class UsdfPage
	{
		public string Name;      // the speaker's name, shown over the text
		public string PageName;  // GZDoom: a name for links
		public string Panel, Voice, Dialog = "", Goodbye, UserString;
		public UsdfValue Drop, Link;
		public List<UsdfItemCheck> IfItems = new List<UsdfItemCheck>();
		public List<UsdfChoice> Choices = new List<UsdfChoice>();
		public List<UsdfExtra> Extra = new List<UsdfExtra>();
	}

	public sealed class UsdfConversation
	{
		public UsdfValue Actor; // a class name (ZDoom/GZDoom) or a Strife conversation ID
		public int? Id;          // ZDoom/GZDoom: the number things' "conversation" field refers to
		public string Class;     // a custom conversation menu class
		public List<UsdfPage> Pages = new List<UsdfPage>();
		public List<UsdfExtra> Extra = new List<UsdfExtra>();
	}

	public sealed class UsdfDocument
	{
		public string Namespace; // "Strife", "ZDoom", "GZDoom", or null when the text has none
		public List<string> Includes = new List<string>();
		public List<UsdfConversation> Conversations = new List<UsdfConversation>();
		public List<UsdfExtra> Extra = new List<UsdfExtra>();

		public bool IsGZDoom => string.Equals(Namespace, "GZDoom", StringComparison.OrdinalIgnoreCase);
		public bool UsesClassNames => IsGZDoom || string.Equals(Namespace, "ZDoom", StringComparison.OrdinalIgnoreCase);

		#region ================== Reading

		/// <summary>Reads USDF text. Throws UsdfException (with a line number) when it can't be read.</summary>
		public static UsdfDocument Parse(string text)
		{
			List<UsdfExtra> items = new Reader(text ?? "").ReadAll();
			UsdfDocument doc = new UsdfDocument();
			foreach(UsdfExtra e in items)
			{
				switch(e.Key.ToLowerInvariant())
				{
					case "namespace" when e.Value != null: doc.Namespace = e.Value.Text; break;
					case "include" when e.Value != null: doc.Includes.Add(e.Value.Text); break;
					case "conversation" when e.Block != null: doc.Conversations.Add(ReadConversation(e.Block)); break;
					default: doc.Extra.Add(e); break;
				}
			}
			return doc;
		}

		private static UsdfConversation ReadConversation(List<UsdfExtra> items)
		{
			UsdfConversation c = new UsdfConversation();
			foreach(UsdfExtra e in items)
			{
				switch(e.Key.ToLowerInvariant())
				{
					case "actor" when e.Value != null: c.Actor = e.Value; break;
					case "id" when e.Value != null: c.Id = e.Value.AsInt(); break;
					case "class" when e.Value != null: c.Class = e.Value.Text; break;
					case "page" when e.Block != null: c.Pages.Add(ReadPage(e.Block)); break;
					default: c.Extra.Add(e); break;
				}
			}
			return c;
		}

		private static UsdfPage ReadPage(List<UsdfExtra> items)
		{
			UsdfPage p = new UsdfPage();
			foreach(UsdfExtra e in items)
			{
				string v = e.Value?.Text;
				switch(e.Key.ToLowerInvariant())
				{
					case "name" when e.Value != null: p.Name = v; break;
					case "pagename" when e.Value != null: p.PageName = v; break;
					case "panel" when e.Value != null: p.Panel = v; break;
					case "voice" when e.Value != null: p.Voice = v; break;
					case "dialog" when e.Value != null: p.Dialog = v; break;
					case "goodbye" when e.Value != null: p.Goodbye = v; break;
					case "userstring" when e.Value != null: p.UserString = v; break;
					case "drop" when e.Value != null: p.Drop = e.Value; break;
					case "link" when e.Value != null: p.Link = e.Value; break;
					case "ifitem" when e.Block != null: p.IfItems.Add(ReadItemCheck(e.Block)); break;
					case "choice" when e.Block != null: p.Choices.Add(ReadChoice(e.Block)); break;
					default: p.Extra.Add(e); break;
				}
			}
			return p;
		}

		private static UsdfChoice ReadChoice(List<UsdfExtra> items)
		{
			UsdfChoice c = new UsdfChoice();
			foreach(UsdfExtra e in items)
			{
				string v = e.Value?.Text;
				string key = e.Key.ToLowerInvariant();
				if(e.Value != null && key.Length == 4 && key.StartsWith("arg") && key[3] >= '0' && key[3] <= '4') { c.Args[key[3] - '0'] = e.Value; continue; }
				switch(key)
				{
					case "text" when e.Value != null: c.Text = v; break;
					case "displaycost" when e.Value != null: c.DisplayCost = e.Value.AsBool(); c.HasDisplayCost = true; break;
					case "yesmessage" when e.Value != null: c.YesMessage = v; break;
					case "nomessage" when e.Value != null: c.NoMessage = v; break;
					case "log" when e.Value != null: c.Log = v; break;
					case "giveitem" when e.Value != null: c.GiveItem = e.Value; break;
					case "special" when e.Value != null: c.Special = e.Value.AsInt(); break;
					case "specialname" when e.Value != null: c.SpecialName = v; break;
					case "nextpage" when e.Value != null: c.NextPage = e.Value; break;
					case "closedialog" when e.Value != null: c.CloseDialog = e.Value.AsBool(); break;
					case "cost" when e.Block != null: c.Costs.Add(ReadItemCheck(e.Block)); break;
					case "require" when e.Block != null: c.Requires.Add(ReadItemCheck(e.Block)); break;
					case "exclude" when e.Block != null: c.Excludes.Add(ReadItemCheck(e.Block)); break;
					default: c.Extra.Add(e); break;
				}
			}
			return c;
		}

		private static UsdfItemCheck ReadItemCheck(List<UsdfExtra> items)
		{
			UsdfItemCheck check = new UsdfItemCheck();
			foreach(UsdfExtra e in items)
			{
				switch(e.Key.ToLowerInvariant())
				{
					case "item" when e.Value != null: check.Item = e.Value; break;
					case "amount" when e.Value != null: check.Amount = e.Value.AsInt(1); break;
					default: check.Extra.Add(e); break;
				}
			}
			return check;
		}

		#endregion

		#region ================== Writing

		/// <summary>The dialogue as USDF text, in a standard layout (tabs, one field per line).</summary>
		public string Write(string newline = "\n")
		{
			Writer w = new Writer(newline);
			if(!string.IsNullOrEmpty(Namespace)) { w.Field("namespace", UsdfValue.Of(Namespace)); w.Blank(); }
			foreach(string include in Includes) w.Field("include", UsdfValue.Of(include));
			if(Includes.Count > 0) w.Blank();
			w.Extras(Extra);
			for(int ci = 0; ci < Conversations.Count; ci++)
			{
				UsdfConversation c = Conversations[ci];
				if(ci > 0) w.Blank();
				w.Open("conversation");
				if(c.Actor != null) w.Field("actor", c.Actor);
				if(c.Id.HasValue) w.Field("id", UsdfValue.Of(c.Id.Value));
				if(!string.IsNullOrEmpty(c.Class)) w.Field("class", UsdfValue.Of(c.Class));
				w.Extras(c.Extra);
				foreach(UsdfPage p in c.Pages)
				{
					w.Blank();
					w.Open("page");
					if(!string.IsNullOrEmpty(p.PageName)) w.Field("pagename", UsdfValue.Of(p.PageName));
					if(p.Name != null) w.Field("name", UsdfValue.Of(p.Name));
					if(!string.IsNullOrEmpty(p.Panel)) w.Field("panel", UsdfValue.Of(p.Panel));
					if(!string.IsNullOrEmpty(p.Voice)) w.Field("voice", UsdfValue.Of(p.Voice));
					w.Field("dialog", UsdfValue.Of(p.Dialog ?? ""));
					if(!string.IsNullOrEmpty(p.Goodbye)) w.Field("goodbye", UsdfValue.Of(p.Goodbye));
					if(!string.IsNullOrEmpty(p.UserString)) w.Field("userstring", UsdfValue.Of(p.UserString));
					if(p.Drop != null && p.Drop.Text.Length > 0) w.Field("drop", p.Drop);
					if(p.Link != null && p.Link.Text.Length > 0) w.Field("link", p.Link);
					foreach(UsdfItemCheck check in p.IfItems) WriteCheck(w, "ifitem", check);
					w.Extras(p.Extra);
					foreach(UsdfChoice ch in p.Choices)
					{
						w.Blank();
						w.Open("choice");
						w.Field("text", UsdfValue.Of(ch.Text ?? ""));
						foreach(UsdfItemCheck check in ch.Costs) WriteCheck(w, "cost", check);
						foreach(UsdfItemCheck check in ch.Requires) WriteCheck(w, "require", check);
						foreach(UsdfItemCheck check in ch.Excludes) WriteCheck(w, "exclude", check);
						if(ch.DisplayCost || ch.HasDisplayCost) w.Field("displaycost", UsdfValue.Of(ch.DisplayCost));
						if(!string.IsNullOrEmpty(ch.YesMessage)) w.Field("yesmessage", UsdfValue.Of(ch.YesMessage));
						if(!string.IsNullOrEmpty(ch.NoMessage)) w.Field("nomessage", UsdfValue.Of(ch.NoMessage));
						if(!string.IsNullOrEmpty(ch.Log)) w.Field("log", UsdfValue.Of(ch.Log));
						if(ch.GiveItem != null && ch.GiveItem.Text.Length > 0) w.Field("giveitem", ch.GiveItem);
						if(ch.Special != 0) w.Field("special", UsdfValue.Of(ch.Special));
						if(!string.IsNullOrEmpty(ch.SpecialName)) w.Field("specialname", UsdfValue.Of(ch.SpecialName));
						for(int a = 0; a < 5; a++) if(ch.Args[a] != null && ch.Args[a].Text.Length > 0) w.Field("arg" + a, ch.Args[a]);
						if(ch.NextPage != null && ch.NextPage.Text.Length > 0) w.Field("nextpage", ch.NextPage);
						if(ch.CloseDialog) w.Field("closedialog", UsdfValue.Of(true));
						w.Extras(ch.Extra);
						w.Close();
					}
					w.Close();
				}
				w.Close();
			}
			return w.ToString();
		}

		private static void WriteCheck(Writer w, string name, UsdfItemCheck check)
		{
			w.Open(name);
			w.Field("item", check.Item ?? UsdfValue.Of(""));
			w.Field("amount", UsdfValue.Of(check.Amount));
			w.Extras(check.Extra);
			w.Close();
		}

		private sealed class Writer
		{
			private readonly StringBuilder sb = new StringBuilder();
			private readonly string nl;
			private int depth;
			private bool blankpending;

			public Writer(string newline) { nl = newline; }

			private void Line(string s)
			{
				if(blankpending && sb.Length > 0) sb.Append(nl);
				blankpending = false;
				sb.Append('\t', depth).Append(s).Append(nl);
			}

			public void Blank() { blankpending = true; }
			public void Field(string key, UsdfValue value) => Line(key + " = " + value.Write() + ";");
			public void Open(string name) { Line(name); Line("{"); depth++; blankpending = false; }
			public void Close() { blankpending = false; depth--; Line("}"); }

			public void Extras(List<UsdfExtra> extras)
			{
				foreach(UsdfExtra e in extras)
				{
					if(e.Block != null) { Open(e.Key); Extras(e.Block); Close(); }
					else Field(e.Key, e.Value);
				}
			}

			public override string ToString() => sb.ToString();
		}

		#endregion

		#region ================== Tokens

		private sealed class Reader
		{
			private readonly string text;
			private int pos, line;

			public Reader(string text) { this.text = text; }

			public List<UsdfExtra> ReadAll()
			{
				List<UsdfExtra> items = ReadItems(false);
				return items;
			}

			private List<UsdfExtra> ReadItems(bool inblock)
			{
				List<UsdfExtra> items = new List<UsdfExtra>();
				while(true)
				{
					SkipSpace();
					if(pos >= text.Length)
					{
						if(inblock) throw new UsdfException("A block is not closed (missing '}')", line);
						return items;
					}
					if(text[pos] == '}')
					{
						if(!inblock) throw new UsdfException("'}' has no block to close", line);
						pos++;
						return items;
					}
					int keyline = line;
					string key = ReadWord();
					if(key == null) throw new UsdfException("Expected a field or block name, found '" + text[pos] + "'", line);
					SkipSpace();
					if(pos < text.Length && text[pos] == '{')
					{
						pos++;
						items.Add(new UsdfExtra { Key = key, Block = ReadItems(true) });
					}
					else if(pos < text.Length && text[pos] == '=')
					{
						pos++;
						SkipSpace();
						UsdfValue value = ReadValue();
						SkipSpace();
						if(pos >= text.Length || text[pos] != ';') throw new UsdfException("Missing ';' after the value of " + key, line);
						pos++;
						items.Add(new UsdfExtra { Key = key, Value = value });
					}
					else throw new UsdfException("Expected '=' or '{' after " + key, keyline);
				}
			}

			private void SkipSpace()
			{
				while(pos < text.Length)
				{
					char c = text[pos];
					if(c == '\n') { line++; pos++; }
					else if(char.IsWhiteSpace(c)) pos++;
					else if(c == '/' && pos + 1 < text.Length && text[pos + 1] == '/') { while(pos < text.Length && text[pos] != '\n') pos++; }
					else if(c == '/' && pos + 1 < text.Length && text[pos + 1] == '*')
					{
						int end = text.IndexOf("*/", pos + 2, StringComparison.Ordinal);
						if(end < 0) throw new UsdfException("Comment is not closed (missing */)", line);
						for(int i = pos; i < end; i++) if(text[i] == '\n') line++;
						pos = end + 2;
					}
					else break;
				}
			}

			private string ReadWord()
			{
				int start = pos;
				while(pos < text.Length && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_')) pos++;
				return pos > start ? text.Substring(start, pos - start) : null;
			}

			private UsdfValue ReadValue()
			{
				if(pos >= text.Length) throw new UsdfException("Expected a value", line);
				char c = text[pos];
				if(c == '"')
				{
					StringBuilder sb = new StringBuilder();
					pos++;
					while(pos < text.Length && text[pos] != '"')
					{
						if(text[pos] == '\\' && pos + 1 < text.Length && (text[pos + 1] == '"' || text[pos + 1] == '\\')) { sb.Append(text[pos + 1]); pos += 2; continue; }
						if(text[pos] == '\n') line++;
						sb.Append(text[pos++]);
					}
					if(pos >= text.Length) throw new UsdfException("String is not closed (missing \")", line);
					pos++;
					return new UsdfValue(UsdfValue.ValueKind.String, sb.ToString());
				}
				int start = pos;
				while(pos < text.Length && !char.IsWhiteSpace(text[pos]) && text[pos] != ';' && text[pos] != '}') pos++;
				string token = text.Substring(start, pos - start);
				if(token.Length == 0) throw new UsdfException("Expected a value", line);
				if(token.Equals("true", StringComparison.OrdinalIgnoreCase) || token.Equals("false", StringComparison.OrdinalIgnoreCase))
					return new UsdfValue(UsdfValue.ValueKind.Boolean, token.ToLowerInvariant());
				if(int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _) || (token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && token.Length > 2))
					return new UsdfValue(UsdfValue.ValueKind.Integer, token);
				if(double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
					return new UsdfValue(UsdfValue.ValueKind.Float, token);
				return new UsdfValue(UsdfValue.ValueKind.Word, token);
			}
		}

		#endregion

		/// <summary>True when the text has comments, which writing it back drops.</summary>
		public static bool HasComments(string text)
		{
			if(string.IsNullOrEmpty(text)) return false;
			bool instring = false;
			for(int i = 0; i < text.Length - 1; i++)
			{
				char c = text[i];
				if(c == '\\' && instring) { i++; continue; }
				if(c == '"') instring = !instring;
				else if(!instring && c == '/' && (text[i + 1] == '/' || text[i + 1] == '*')) return true;
			}
			return false;
		}
	}

	public sealed class UsdfException : Exception
	{
		/// <summary>0-based line.</summary>
		public int Line { get; }
		public UsdfException(string message, int line) : base(message) { Line = line; }
	}
}
