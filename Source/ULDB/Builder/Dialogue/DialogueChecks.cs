// ULDB: what the dialogue builder points out while a dialogue is edited: links to pages that don't exist,
// pages and choices without text, conversations without an actor, repeated IDs and page names, and items
// that aren't a class the map knows (often a typo).

using System;
using System.Collections.Generic;
using System.Linq;

namespace ULDB.Dialogue
{
	internal static class DialogueChecks
	{
		public static List<string> Check(UsdfDocument doc, ICollection<string> classnames)
		{
			List<string> found = new List<string>();
			HashSet<string> known = new HashSet<string>(classnames ?? new string[0], StringComparer.OrdinalIgnoreCase);
			Dictionary<int, int> ids = new Dictionary<int, int>();

			for(int ci = 0; ci < doc.Conversations.Count; ci++)
			{
				UsdfConversation c = doc.Conversations[ci];
				string who = c.Actor != null && c.Actor.Text.Length > 0 ? c.Actor.Text : "conversation " + (ci + 1);
				if(c.Actor == null || c.Actor.Text.Length == 0) found.Add("Conversation " + (ci + 1) + " has no actor");
				if(c.Id.HasValue)
				{
					if(ids.ContainsKey(c.Id.Value)) found.Add("ID " + c.Id.Value + " is used by two conversations");
					else ids[c.Id.Value] = ci;
				}
				if(c.Pages.Count == 0) found.Add(who + " has no pages");
				if(c.Actor != null && doc.UsesClassNames) CheckClass(c.Actor, who + "'s actor", known, found);

				HashSet<string> pagenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				for(int pi = 0; pi < c.Pages.Count; pi++)
				{
					UsdfPage p = c.Pages[pi];
					string where = who + ", page " + (pi + 1);
					if(string.IsNullOrWhiteSpace(p.Dialog)) found.Add(where + " has no dialogue");
					if(!string.IsNullOrEmpty(p.PageName))
					{
						if(!doc.IsGZDoom) found.Add(where + ": page names need the GZDoom namespace");
						else if(!pagenames.Add(p.PageName)) found.Add(where + ": the page name \"" + p.PageName + "\" is used twice");
					}
					CheckPage(doc, c, p.Link, where + "'s skip link", found);
					foreach(UsdfItemCheck check in p.IfItems) CheckItem(doc, check, where, known, found);
					if(p.Drop != null && doc.UsesClassNames) CheckClass(p.Drop, where + "'s drop", known, found);

					for(int chi = 0; chi < p.Choices.Count; chi++)
					{
						UsdfChoice ch = p.Choices[chi];
						string choice = where + ", choice " + (chi + 1);
						if(string.IsNullOrWhiteSpace(ch.Text)) found.Add(choice + " has no text");
						CheckPage(doc, c, ch.NextPage, choice, found);
						foreach(UsdfItemCheck check in ch.Costs.Concat(ch.Requires).Concat(ch.Excludes)) CheckItem(doc, check, choice, known, found);
						if(ch.GiveItem != null && doc.UsesClassNames) CheckClass(ch.GiveItem, choice + "'s item", known, found);
					}
				}
			}
			return found;
		}

		private static void CheckPage(UsdfDocument doc, UsdfConversation c, UsdfValue link, string where, List<string> found)
		{
			if(link == null || link.Text.Length == 0) return;
			if(link.Kind == UsdfValue.ValueKind.Integer)
			{
				int n = link.AsInt();
				if(n < 1 || n > c.Pages.Count) found.Add(where + " goes to page " + n + ", which doesn't exist");
			}
			else if(!doc.IsGZDoom) found.Add(where + " goes to a page by name, which needs the GZDoom namespace");
			else if(!c.Pages.Any(p => string.Equals(p.PageName, link.Text, StringComparison.OrdinalIgnoreCase)))
				found.Add(where + " goes to page \"" + link.Text + "\", which doesn't exist");
		}

		private static void CheckItem(UsdfDocument doc, UsdfItemCheck check, string where, HashSet<string> known, List<string> found)
		{
			if(check.Item == null || check.Item.Text.Length == 0) { found.Add(where + " checks an item without a name"); return; }
			if(doc.UsesClassNames) CheckClass(check.Item, where + "'s item", known, found);
		}

		private static void CheckClass(UsdfValue value, string what, HashSet<string> known, List<string> found)
		{
			if(value == null || value.Text.Length == 0 || known.Count == 0 || value.IsNumber) return;
			if(!known.Contains(value.Text)) found.Add(what + " \"" + value.Text + "\" isn't a class the map knows (fine for engine classes; check the spelling)");
		}
	}
}
