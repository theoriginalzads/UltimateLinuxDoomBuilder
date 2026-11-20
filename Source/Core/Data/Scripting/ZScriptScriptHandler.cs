using CodeImp.DoomBuilder.Compilers;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.ZDoom.Scripting;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.Data.Scripting
{
    [ScriptHandler(ScriptType.ZSCRIPT)]
    internal class ZScriptScriptHandler : ScriptHandler
    {
        #region ================== Methods

        //TODO: Remove ScriptDocumentTab from here
        public override List<CompilerError> UpdateFunctionBarItems(ScriptDocumentTab tab, MemoryStream stream, ComboBox target)
        {
            List<CompilerError> result = new List<CompilerError>();
            if (stream == null) return result;
            target.Items.Clear();

            ZScriptParserSE parser = new ZScriptParserSE();
            TextResourceData data = new TextResourceData(stream, new DataLocation(), "ZSCRIPT");

            if (parser.Parse(data, false))
                target.Items.AddRange(parser.Types.ToArray());

#if ULDB
            // ULDB: a check of the whole script (brackets, strings, States and Default blocks) instead of the
            // navigator parser's own errors; errors name the script as its tab recognises them
            stream.Seek(0, SeekOrigin.Begin);
            string text = new StreamReader(stream, ScriptEditorControl.Encoding).ReadToEnd();
            string name = (tab is ScriptWadLumpDocumentTab wadtab) ? wadtab.ErrorName : (tab != null ? tab.Filename : "ZSCRIPT");
            result.AddRange(ULDB.Scripting.ZScriptChecker.Check(text, name));
#else
            if (parser.HasError)
                result.Add(new CompilerError(parser.ErrorDescription, parser.ErrorSource, parser.ErrorLine));
#endif

            return result;
        }

        #endregion
    }
}