// ULDB: a script editor tab for a lump of the map's WAD that isn't part of the map: ZSCRIPT, and the lumps it
// #includes. Upstream only edits the map's own lumps (SCRIPTS, DIALOGUE...); everything else in the WAD needed
// another program (SLADE). The tab reads the lump from the map's WAD file and saves it straight back into it
// (Ctrl+S, like a file), so Test Map, which loads the map's WAD, sees what was saved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Compilers;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.IO;

namespace CodeImp.DoomBuilder.Controls
{
	internal sealed class ScriptWadLumpDocumentTab : ScriptDocumentTab
	{
		#region ================== Variables

		private readonly string lumpname;
		private readonly int occurrence; // which lump of that name (a WAD can hold several ZSCRIPT lumps)
		private string hash; // of the lump as it was loaded or last saved, to notice changes made by another program
		private readonly Timer checktimer = new Timer { Interval = 700 }; // the ZScript check, once typing pauses

		#endregion

		#region ================== Properties

		public override bool ExplicitSave { get { return true; } }
		public override bool IsSaveAsRequired { get { return false; } }
		public override bool IsClosable { get { return false; } }
		public override bool IsReconfigurable { get { return false; } }
		public override string Filename { get { return SettingsKey(lumpname, occurrence); } }
		public string LumpName { get { return lumpname; } }
		/// <summary>How errors in this lump name it (the "?" marks a lump; the error list shows the rest).</summary>
		public string ErrorName { get { return "?" + Title; } }

		#endregion

		#region ================== Constructor

		private ScriptWadLumpDocumentTab(ScriptEditorPanel panel, ScriptConfiguration config, string lumpname, int occurrence, byte[] data) : base(panel, config)
		{
			tabtype = ScriptDocumentTabType.RESOURCE;
			this.lumpname = lumpname;
			this.occurrence = occurrence;
			hash = Hash(data);
			if(data != null)
			{
				editor.SetText(data);
				editor.ClearUndoRedo();
				editor.SetSavePoint();
			}
			SetTitle(occurrence > 0 ? lumpname + " (" + (occurrence + 1) + ")" : lumpname);

			// Check the script as it's typed (ULDB.Scripting.ZScriptChecker, through the ZScript handler's navigator update)
			OnTextChanged += (s, e) => { checktimer.Stop(); checktimer.Start(); };
			checktimer.Tick += (s, e) =>
			{
				checktimer.Stop();
				if(panel.ActiveTab == this) panel.ShowErrors(UpdateNavigator(), false);
			};
		}

		protected override void Dispose(bool disposing)
		{
			checktimer.Dispose();
			base.Dispose(disposing);
		}

		/// <summary>
		/// The tabs for the map's WAD: one per ZSCRIPT lump (an empty one when there is none, so a ZSCRIPT can be
		/// started here), and one per lump they #include that is in the same WAD.
		/// </summary>
		public static List<ScriptWadLumpDocumentTab> CreateTabs(ScriptEditorPanel panel)
		{
			List<ScriptWadLumpDocumentTab> result = new List<ScriptWadLumpDocumentTab>();
			ScriptConfiguration config = General.GetScriptConfiguration(ScriptType.ZSCRIPT);
			if(config == null) return result;

			List<KeyValuePair<string, byte[]>> lumps = ReadLumps(General.Map.FilePathName);
			int zscripts = 0;
			for(int i = 0; i < lumps.Count; i++)
			{
				if(lumps[i].Key != "ZSCRIPT") continue;
				result.Add(new ScriptWadLumpDocumentTab(panel, config, "ZSCRIPT", zscripts++, lumps[i].Value));
			}
			if(zscripts == 0) result.Add(new ScriptWadLumpDocumentTab(panel, config, "ZSCRIPT", 0, null));

			// Lumps the scripts include, and the ones those include
			HashSet<string> added = new HashSet<string> { "ZSCRIPT" };
			for(int t = 0; t < result.Count; t++)
			{
				foreach(string include in Includes(result[t].editor.Text))
				{
					if(!added.Add(include)) continue;
					int index = lumps.FindIndex(l => l.Key == include);
					if(index >= 0) result.Add(new ScriptWadLumpDocumentTab(panel, config, include, 0, lumps[index].Value));
				}
			}
			return result;
		}

		#endregion

		#region ================== Methods

		public static string SettingsKey(string lumpname, int occurrence) { return "wad:" + lumpname + (occurrence > 0 ? "#" + occurrence : ""); }

		// #include "NAME": in a WAD, GZDoom finds a lump by its 8-character name (a path with folders only works in a PK3)
		private static readonly Regex includepattern = new Regex("^\\s*#include\\s+\"([^\"]+)\"", RegexOptions.Multiline | RegexOptions.IgnoreCase);

		internal static IEnumerable<string> Includes(string text)
		{
			foreach(Match m in includepattern.Matches(text ?? ""))
			{
				string name = m.Groups[1].Value.Trim();
				if(name.Length > 0 && name.Length <= 8 && name.IndexOf('/') < 0 && name.IndexOf('\\') < 0 && name.IndexOf('.') < 0)
					yield return name.ToUpperInvariant();
			}
		}

		// The WAD's lumps, by name, in order (an unsaved map has none)
		private static List<KeyValuePair<string, byte[]>> ReadLumps(string wadfile)
		{
			List<KeyValuePair<string, byte[]>> result = new List<KeyValuePair<string, byte[]>>();
			if(string.IsNullOrEmpty(wadfile) || !File.Exists(wadfile)) return result;
			try
			{
				WAD wad = new WAD(wadfile, true);
				try
				{
					foreach(Lump l in wad.Lumps)
						result.Add(new KeyValuePair<string, byte[]>(l.Name, l.Name == "ZSCRIPT" || l.Length < 4 * 1024 * 1024 ? ReadLump(l) : null));
				}
				finally { wad.Dispose(); }
			}
			catch(Exception e) { General.ErrorLogger.Add(ErrorType.Warning, "Unable to read the scripts in \"" + wadfile + "\": " + e.Message); }
			return result;
		}

		private static byte[] ReadLump(Lump l)
		{
			using(MemoryStream ms = new MemoryStream())
			{
				Stream s = l.GetSafeStream();
				s.Seek(0, SeekOrigin.Begin);
				s.CopyTo(ms);
				return ms.ToArray();
			}
		}

		private static string Hash(byte[] data)
		{
			if(data == null) return "";
			return Convert.ToHexString(System.Security.Cryptography.MD5.HashData(data));
		}

		// The lump's index in the WAD, or -1
		private int FindLump(WAD wad)
		{
			int found = -1;
			for(int n = 0; n <= occurrence; n++)
			{
				found = wad.FindLumpIndex(lumpname, found + 1);
				if(found < 0) return -1;
			}
			return found;
		}

		// Writes the lump into the map's WAD (Ctrl+S or Save)
		public override bool Save()
		{
			if(!editor.IsChanged) return false;
			string wadfile = General.Map.FilePathName;
			if(string.IsNullOrEmpty(wadfile) || !File.Exists(wadfile))
			{
				General.ShowWarningMessage("Save the map first: " + lumpname + " is stored in the map's WAD file, which doesn't exist yet.", MessageBoxButtons.OK);
				return false;
			}

			RemoveTrailingWhitespace();
			byte[] text = editor.GetText();

			// The map's WAD is open as a resource: let go of it while it's written
			General.Map.Data.Suspend();
			try
			{
				WAD wad = new WAD(wadfile);
				try
				{
					int index = FindLump(wad);

					// Changed by another program (SLADE) since it was loaded?
					string current = index >= 0 ? Hash(ReadLump(wad.Lumps[index])) : "";
					if(current != hash && MessageBox.Show(General.MainWindow, lumpname + " was changed in the WAD by another program since it was opened here. Replace it with this version?", "Save " + lumpname, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
						return false;

					if(index >= 0) wad.RemoveAt(index, false);
					if(text.Length == 0) { hash = ""; } // an emptied script: the lump goes
					else
					{
						// A new lump goes before the map (GZDoom reads ZSCRIPT anywhere in the WAD; a lump right after a map can look like part of it)
						int at = index >= 0 ? index : 0;
						Lump l = wad.Insert(lumpname, at, text.Length, false);
						l.Stream.Seek(0, SeekOrigin.Begin);
						l.Stream.Write(text, 0, text.Length);
						hash = Hash(text);
					}
					wad.WriteHeaders();
				}
				finally { wad.Dispose(); }
			}
			catch(Exception e)
			{
				General.ShowErrorMessage("Unable to save " + lumpname + " into \"" + wadfile + "\":" + Environment.NewLine + e.Message, MessageBoxButtons.OK);
				return false;
			}
			finally { General.Map.Data.Resume(); }

			editor.SetSavePoint();
			UpdateTitle();
			General.MainWindow.DisplayStatus(CodeImp.DoomBuilder.Windows.StatusType.Info, lumpname + " saved into the map's WAD. Reload resources (F8) to update the thing list from it.");
			return true;
		}

		// Errors from the ZScript check (ULDB.Scripting.ZScriptChecker) name the lump with ErrorName
		public override bool VerifyErrorForScript(CompilerError e)
		{
			return string.Equals(e.filename, ErrorName, StringComparison.OrdinalIgnoreCase);
		}

		#endregion
	}
}
