// ULDB: a quick check of ZScript as it is typed, for the script editor's error list and line marks. ZScript
// is compiled by the engine when it starts, so the editor can't compile it; this catches the mistakes that
// are easy to make and slow to find from the engine's message:
//  - brackets that don't match or aren't closed, unterminated strings and comments;
//  - something outside a class that can't be there, unknown # directives;
//  - in States: a missing ';' between frames, a frame without a duration;
//  - in Default: a missing ';' between properties.
// It doesn't know the engine's classes or check types and names; the engine still has the last word.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CodeImp.DoomBuilder.Compilers;

namespace ULDB.Scripting
{
	internal static class ZScriptChecker
	{
		private enum Kind { Word, Number, String, Name, Punct, Directive }

		private sealed class Token
		{
			public Kind Kind;
			public string Text;
			public int Line;
			public bool LineStart; // first token on its line
		}

		/// <summary>The first problems found (line numbers 0-based, as CompilerError keeps them), at most a few.</summary>
		public static List<CompilerError> Check(string text, string filename)
		{
			List<CompilerError> errors = new List<CompilerError>();
			List<Token> tokens = Tokenize(text ?? "", filename, errors);
			if(errors.Count == 0) CheckBrackets(tokens, filename, errors);
			if(errors.Count == 0) CheckStructure(tokens, filename, errors);
			return errors;
		}

		#region ================== Tokens

		private static List<Token> Tokenize(string text, string filename, List<CompilerError> errors)
		{
			List<Token> tokens = new List<Token>();
			int i = 0, n = text.Length, line = 0;
			bool linestart = true;
			// Inside a States block the frames after a sprite are one word, whatever they contain ("XYZ[", "\\")
			int depth = 0, statesdepth = -1;
			bool statesnext = false; // "States" seen, its '{' to come
			while(i < n)
			{
				char c = text[i];
				if(c == '\n') { line++; i++; linestart = true; continue; }
				if(char.IsWhiteSpace(c)) { i++; continue; }

				// Comments
				if(c == '/' && i + 1 < n && text[i + 1] == '/')
				{
					while(i < n && text[i] != '\n') i++;
					continue;
				}
				if(c == '/' && i + 1 < n && text[i + 1] == '*')
				{
					int startline = line;
					int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal);
					if(end < 0) { errors.Add(new CompilerError("Comment opened here is never closed (missing */)", filename, startline)); return tokens; }
					for(int k = i; k < end; k++) if(text[k] == '\n') line++;
					i = end + 2;
					continue;
				}

				Token t = new Token { Line = line, LineStart = linestart };
				linestart = false;

				// The frames of a state: sprite at the start of a line in a States block, then this
				if(depth == statesdepth && tokens.Count > 0 && !t.LineStart && tokens[tokens.Count - 1].LineStart && tokens[tokens.Count - 1].Line == line
				   && tokens[tokens.Count - 1].Kind == Kind.Word && tokens[tokens.Count - 1].Text.Length == 4 && c != ':' && c != '.' && c != ';' && c != '"' && c != '(')
				{
					int j = i;
					while(j < n && !char.IsWhiteSpace(text[j]) && text[j] != ';' && text[j] != '/') j++;
					t.Kind = Kind.Word; t.Text = text.Substring(i, j - i);
					tokens.Add(t); i = j;
					continue;
				}

				// Directives: the rest of the line
				if(c == '#' && t.LineStart && i + 1 < n && char.IsLetter(text[i + 1]))
				{
					int end = i;
					while(end < n && text[end] != '\n') end++;
					t.Kind = Kind.Directive; t.Text = text.Substring(i, end - i).Trim();
					tokens.Add(t); i = end;
					continue;
				}

				// "strings" and 'names' (one line each, \ escapes)
				if(c == '"' || c == '\'')
				{
					int j = i + 1;
					while(j < n && text[j] != c && text[j] != '\n') j += text[j] == '\\' ? 2 : 1;
					if(j >= n || text[j] != c)
					{
						errors.Add(new CompilerError((c == '"' ? "String" : "Name") + " is not closed on this line (missing " + c + ")", filename, line));
						return tokens;
					}
					t.Kind = c == '"' ? Kind.String : Kind.Name; t.Text = text.Substring(i, j + 1 - i);
					tokens.Add(t); i = j + 1;
					continue;
				}

				// "----": a sprite that keeps the current one (in States)
				if(c == '-' && i + 4 <= n && text.Substring(i, 4) == "----" && (i + 4 == n || char.IsWhiteSpace(text[i + 4])))
				{
					t.Kind = Kind.Word; t.Text = "----";
					tokens.Add(t); i += 4;
					continue;
				}

				// "####" and "#": keep the current sprite / frame (in States)
				if(c == '#')
				{
					int j = i + 1;
					while(j < n && text[j] == '#') j++;
					t.Kind = Kind.Word; t.Text = text.Substring(i, j - i);
					tokens.Add(t); i = j;
					continue;
				}

				if(char.IsLetter(c) || c == '_')
				{
					int j = i + 1;
					while(j < n && (char.IsLetterOrDigit(text[j]) || text[j] == '_')) j++;
					t.Kind = Kind.Word; t.Text = text.Substring(i, j - i);
					tokens.Add(t); i = j;
					if(t.Text.Equals("States", StringComparison.OrdinalIgnoreCase)) statesnext = true;
					continue;
				}

				if(char.IsDigit(c) || (c == '.' && i + 1 < n && char.IsDigit(text[i + 1])))
				{
					int j = i + 1;
					while(j < n && (char.IsLetterOrDigit(text[j]) || text[j] == '.' || text[j] == '_' || ((text[j] == '-' || text[j] == '+') && (text[j - 1] == 'e' || text[j - 1] == 'E')))) j++;
					t.Kind = Kind.Number; t.Text = text.Substring(i, j - i);
					tokens.Add(t); i = j;
					continue;
				}

				// Two-character operators that matter here
				if(c == ':' && i + 1 < n && text[i + 1] == ':') { t.Kind = Kind.Punct; t.Text = "::"; tokens.Add(t); i += 2; continue; }
				t.Kind = Kind.Punct; t.Text = c.ToString();
				tokens.Add(t); i++;
				if(c == '{')
				{
					depth++;
					if(statesnext) { statesdepth = depth; statesnext = false; }
				}
				else if(c == '}')
				{
					if(depth == statesdepth) statesdepth = -1;
					depth--;
				}
				else if(c == ';') statesnext = false;
			}
			return tokens;
		}

		#endregion

		#region ================== Brackets

		private static void CheckBrackets(List<Token> tokens, string filename, List<CompilerError> errors)
		{
			Stack<Token> open = new Stack<Token>();
			foreach(Token t in tokens)
			{
				if(t.Kind != Kind.Punct) continue;
				switch(t.Text)
				{
					case "(": case "[": case "{": open.Push(t); break;
					case ")": case "]": case "}":
						string expected = t.Text == ")" ? "(" : t.Text == "]" ? "[" : "{";
						if(open.Count == 0)
						{
							errors.Add(new CompilerError("'" + t.Text + "' has nothing to close", filename, t.Line));
							return;
						}
						Token o = open.Pop();
						if(o.Text != expected)
						{
							errors.Add(new CompilerError("Expected " + Closer(o.Text) + " to close the '" + o.Text + "' from line " + (o.Line + 1) + ", found '" + t.Text + "'", filename, t.Line));
							return;
						}
						break;
				}
			}
			if(open.Count > 0)
			{
				Token[] left = open.ToArray();
				Token first = left[left.Length - 1];
				errors.Add(new CompilerError("The '" + first.Text + "' opened here is never closed (missing " + Closer(first.Text) + ")", filename, first.Line));
			}
		}

		private static string Closer(string opener) { return opener == "(" ? "')'" : opener == "[" ? "']'" : "'}'"; }

		#endregion

		#region ================== Structure

		private static readonly HashSet<string> TOPLEVEL = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "class", "struct", "extend", "enum", "const", "mixin", "version" };
		private static readonly Regex DIRECTIVE = new Regex(@"^#\s*(include|region|endregion)\b", RegexOptions.IgnoreCase);
		private static readonly HashSet<string> FLOW = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "stop", "loop", "wait", "fail", "goto" };

		private static void CheckStructure(List<Token> tokens, string filename, List<CompilerError> errors)
		{
			int i = 0;
			while(i < tokens.Count && errors.Count == 0)
			{
				Token t = tokens[i];
				if(t.Kind == Kind.Directive)
				{
					if(!DIRECTIVE.IsMatch(t.Text)) errors.Add(new CompilerError("Unknown directive \"" + t.Text + "\" (ZScript has #include)", filename, t.Line));
					i++;
					continue;
				}
				if(t.Kind == Kind.Punct && t.Text == ";") { i++; continue; }
				if(t.Kind != Kind.Word || !TOPLEVEL.Contains(t.Text))
				{
					errors.Add(new CompilerError("\"" + t.Text + "\" can't be outside a class or struct (expected class, struct, extend, enum, const or version)", filename, t.Line));
					return;
				}

				// version "4.10" (no ';')
				if(t.Text.Equals("version", StringComparison.OrdinalIgnoreCase))
				{
					if(i + 1 >= tokens.Count || tokens[i + 1].Kind != Kind.String) { errors.Add(new CompilerError("Expected a version string after version (version \"4.10\")", filename, t.Line)); return; }
					i += 2;
					if(i < tokens.Count && tokens[i].Text == ";") i++;
					continue;
				}

				// To the end of the declaration: its ';', or its block
				int j = i;
				while(j < tokens.Count && !(tokens[j].Kind == Kind.Punct && (tokens[j].Text == ";" || tokens[j].Text == "{"))) j++;
				if(j >= tokens.Count) return;
				if(tokens[j].Text == ";") { i = j + 1; continue; }
				int end = BlockEnd(tokens, j);
				string kind = t.Text.ToLowerInvariant();
				if(kind == "class" || kind == "struct" || kind == "extend" || kind == "mixin") CheckClassBody(tokens, j + 1, end, filename, errors);
				i = end + 1;
			}
		}

		// The index of the '}' closing the '{' at open (brackets are known to match by now)
		private static int BlockEnd(List<Token> tokens, int open)
		{
			int depth = 0;
			for(int k = open; k < tokens.Count; k++)
			{
				if(tokens[k].Kind != Kind.Punct) continue;
				if(tokens[k].Text == "{") depth++;
				else if(tokens[k].Text == "}" && --depth == 0) return k;
			}
			return tokens.Count - 1;
		}

		// States and Default blocks among a class's members (from..end is inside its braces)
		private static void CheckClassBody(List<Token> tokens, int from, int end, string filename, List<CompilerError> errors)
		{
			int i = from;
			while(i < end && errors.Count == 0)
			{
				Token t = tokens[i];
				if(t.Kind == Kind.Punct && t.Text == "{") { i = BlockEnd(tokens, i) + 1; continue; }
				if(t.Kind == Kind.Word && (t.Text.Equals("States", StringComparison.OrdinalIgnoreCase) || t.Text.Equals("Default", StringComparison.OrdinalIgnoreCase)))
				{
					// States may have a scope: States(Actor, Overlay)
					int j = i + 1;
					if(j < end && tokens[j].Text == "(") { while(j < end && tokens[j].Text != ")") j++; j++; }
					if(j < end && tokens[j].Kind == Kind.Punct && tokens[j].Text == "{")
					{
						int close = BlockEnd(tokens, j);
						if(t.Text.Equals("States", StringComparison.OrdinalIgnoreCase)) CheckStates(tokens, j + 1, close, filename, errors);
						else CheckDefault(tokens, j + 1, close, filename, errors);
						i = close + 1;
						continue;
					}
				}
				i++;
			}
		}

		private static bool IsSprite(Token t)
		{
			if(t.Kind == Kind.String) return t.Text.Length == 6; // "####"
			if(t.Kind == Kind.Word && t.Text.Length == 4) return true;
			return t.Kind == Kind.Number && t.Text.Length == 4; // sprites can be digits
		}

		private static void CheckStates(List<Token> tokens, int from, int end, string filename, List<CompilerError> errors)
		{
			// Statements end with ';'. Labels ("Spawn:", "Death.Fire:") stand on their own.
			int i = from;
			while(i < end && errors.Count == 0)
			{
				// Skip labels
				int k = i;
				while(k < end && tokens[k].Kind == Kind.Word)
				{
					int m = k + 1;
					while(m + 1 < end && tokens[m].Text == "." && tokens[m + 1].Kind == Kind.Word) m += 2;
					if(m < end && tokens[m].Text == ":") { k = m + 1; continue; }
					break;
				}
				i = k;
				if(i >= end) break;

				// One statement, to its ';' (or the end of the block, which is a missing ';' too)
				int s = i;
				int depth = 0;
				bool block = false; // ended by an action block { ... }, which needs no ';'
				while(i < end && !(depth == 0 && tokens[i].Text == ";"))
				{
					if(tokens[i].Kind != Kind.Punct) { }
					else if(tokens[i].Text == "(" || tokens[i].Text == "{" || tokens[i].Text == "[") depth++;
					else if(tokens[i].Text == ")" || tokens[i].Text == "}" || tokens[i].Text == "]")
					{
						depth--;
						if(depth == 0 && tokens[i].Text == "}") { block = true; i++; break; }
					}
					// A new frame or a flow keyword at the start of a line: the line before it isn't finished
					if(i > s && depth == 0 && tokens[i].LineStart && (StartsFrame(tokens, i, end) || IsFlow(tokens[i]) || IsLabel(tokens, i, end)))
					{
						errors.Add(new CompilerError("Missing ';' at the end of this line", filename, tokens[i - 1].Line));
						return;
					}
					i++;
				}
				if(i >= end && s < end && !block)
				{
					errors.Add(new CompilerError("Missing ';' at the end of this line", filename, tokens[end - 1].Line));
					return;
				}

				// A frame needs a sprite, frames and a duration
				Token first = tokens[s];
				if(!IsFlow(first) && IsSprite(first))
				{
					if(i - s < 3)
						errors.Add(new CompilerError("Expected a duration after \"" + first.Text + (i - s > 1 ? " " + tokens[s + 1].Text : "") + "\" (sprite, frames, duration)", filename, first.Line));
				}
				else if(!IsFlow(first))
				{
					errors.Add(new CompilerError("\"" + first.Text + "\" isn't a state: expected a sprite and frames (\"TROO A 10;\"), a label (\"Spawn:\") or stop, loop, wait, fail, goto", filename, first.Line));
				}
				if(!block) i++; // the ';'
				else if(i < end && tokens[i].Text == ";") i++;
			}
		}

		private static bool IsFlow(Token t) { return t.Kind == Kind.Word && FLOW.Contains(t.Text); }

		private static bool IsLabel(List<Token> tokens, int i, int end)
		{
			if(tokens[i].Kind != Kind.Word) return false;
			int m = i + 1;
			while(m + 1 < end && tokens[m].Text == "." && tokens[m + 1].Kind == Kind.Word) m += 2;
			return m < end && tokens[m].Text == ":";
		}

		// SPRT FRAMES DURATION...
		private static bool StartsFrame(List<Token> tokens, int i, int end)
		{
			if(!IsSprite(tokens[i]) || i + 2 >= end) return false;
			Token frames = tokens[i + 1], duration = tokens[i + 2];
			bool frameslike = (frames.Kind == Kind.Word || frames.Kind == Kind.String || frames.Text == "#" || frames.Text == "[" || frames.Text == "\\") && frames.Line == tokens[i].Line;
			bool durationlike = (duration.Kind == Kind.Number || duration.Text == "-" || duration.Kind == Kind.Word) && duration.Line == tokens[i].Line;
			return frameslike && durationlike;
		}

		private static void CheckDefault(List<Token> tokens, int from, int end, string filename, List<CompilerError> errors)
		{
			// "Property value;" and "+FLAG"/"-FLAG" (no ';' needed)
			int i = from;
			bool instatement = false;
			while(i < end)
			{
				Token t = tokens[i];
				bool flag = (t.Text == "+" || t.Text == "-") && i + 1 < end && tokens[i + 1].Kind == Kind.Word;
				if(flag && instatement && t.LineStart && !Continues(tokens[i - 1]))
				{
					errors.Add(new CompilerError("Missing ';' at the end of this line", filename, tokens[i - 1].Line));
					return;
				}
				if(flag && !instatement)
				{
					i += 2;
					while(i + 1 < end && tokens[i].Text == "." && tokens[i + 1].Kind == Kind.Word) i += 2;
					if(i < end && tokens[i].Text == ";") i++;
					continue;
				}
				if(t.Text == ";") { instatement = false; i++; continue; }
				if(instatement && t.LineStart && t.Kind == Kind.Word && !Continues(tokens[i - 1]))
				{
					errors.Add(new CompilerError("Missing ';' at the end of this line", filename, tokens[i - 1].Line));
					return;
				}
				instatement = true;
				i++;
			}
			if(instatement) errors.Add(new CompilerError("Missing ';' at the end of this line", filename, tokens[end - 1].Line));
		}

		// A line ending with one of these goes on on the next line
		private static bool Continues(Token t)
		{
			return t.Kind == Kind.Punct && (t.Text == "," || t.Text == "(" || t.Text == "+" || t.Text == "-" || t.Text == "*" || t.Text == "/" || t.Text == "|" || t.Text == "&" || t.Text == "." || t.Text == "=");
		}

		#endregion
	}
}
