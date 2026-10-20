// uistubgen: generates placeholder classes for upstream UDB's WinForms windows and
// controls, so the editor core (and plugins) compile before the Avalonia ports exist.
//
// Each placeholder keeps the type's name, namespace, and non-private member
// signatures. Bodies do nothing and return defaults. WinForms base classes are
// dropped. Members listed in the prune file are skipped (they reference types that
// can't exist until the real port), and types listed in the ported file are skipped
// entirely (ULDB has a real implementation).
//
// Usage: uistubgen --out DIR --prune FILE --ported FILE --root UPSTREAM_DIR SOURCE_DIR_OR_FILE...

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static class Program
{
	static string outDir, pruneFile, portedFile, root;
	static readonly List<string> sourceDirs = new();
	static HashSet<string> prune = new();
	static HashSet<string> ported = new();
	static HashSet<string> manual = new(); // "Type.Member": implemented by hand in UIStubFixes
	static HashSet<string> winformsTypes = new();
	static HashSet<string> onlyFiles; // Relative paths upstream's project compiles (null = all)

	// Controls ULDB.Compat provides (Compat/WinForms): kept as bases.
	static readonly HashSet<string> compatControlTypes = new()
	{
		"Panel", "FlowLayoutPanel", "ComboBox", "ListView", "Label", "Button", "GroupBox", "CheckBox",
		"NumericUpDown", "TabControl", "TabPage", "ContainerControl", "UserControl", "DataGridView",
	};

	// The menu/toolbar model ULDB.Compat provides (Compat/WinForms/ToolStrip.cs): kept as bases.
	static readonly HashSet<string> compatToolStripTypes = new()
	{
		"ToolStrip", "MenuStrip", "StatusStrip", "ToolStripDropDown", "ToolStripDropDownMenu", "ContextMenuStrip",
		"ToolStripItem", "ToolStripButton", "ToolStripLabel", "ToolStripStatusLabel", "ToolStripSeparator",
		"ToolStripDropDownItem", "ToolStripMenuItem", "ToolStripDropDownButton", "ToolStripSplitButton",
		"ToolStripControlHost", "ToolStripTextBox", "ToolStripComboBox",
	};

	static int Main(string[] args)
	{
		for(int i = 0; i < args.Length; i++)
		{
			switch(args[i])
			{
				case "--out": outDir = args[++i]; break;
				case "--prune": pruneFile = args[++i]; break;
				case "--ported": portedFile = args[++i]; break;
				case "--manual": manual = ReadList(args[++i]); break;
				case "--root": root = args[++i]; break;
				case "--winforms-types": winformsTypes = ReadList(args[++i]); break;
				case "--files-from": onlyFiles = ReadList(args[++i]); break;
				default: sourceDirs.Add(args[i]); break;
			}
		}
		if(outDir == null || root == null || sourceDirs.Count == 0)
		{
			Console.Error.WriteLine("usage: uistubgen --out DIR --root UPSTREAM_DIR [--prune FILE] [--ported FILE] SOURCE_DIR...");
			return 2;
		}

		prune = ReadList(pruneFile);
		ported = ReadList(portedFile);

		// Parse every non-designer source file and group type declarations by full name,
		// so partial classes spread over several files become one placeholder.
		var types = new Dictionary<string, TypeInfo>();
		foreach(string dir in sourceDirs)
		{
			// A source can also be a single file (plugins with their forms at the top level).
			IEnumerable<string> files = File.Exists(dir) ? new[] { dir } : Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories);
			foreach(string file in files.OrderBy(f => f, StringComparer.Ordinal))
			{
				if(file.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase)) continue;
				if(onlyFiles != null && !onlyFiles.Contains(Path.GetRelativePath(root, file).Replace('\\', '/'))) continue;
				var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(file), new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: new[] { "NO_SCINTILLA", "NO_WIN32", "NO_UPDATER", "ULDB" }));
				var rootNode = tree.GetCompilationUnitRoot();
				// Keep aliases ("X = A.B") and static usings, not just the name.
				var usings = rootNode.Usings.Select(u =>
					(u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) ? "static " : "") +
					(u.Alias != null ? u.Alias.Name.Identifier.Text + " = " : "") + u.Name.ToString()).ToList();
				foreach(var t in rootNode.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
				{
					if(t.Parent is BaseTypeDeclarationSyntax) continue; // Nested: handled by the parent
					string ns = (t.Parent as BaseNamespaceDeclarationSyntax)?.Name.ToString() ?? "";
					string full = ns + "." + t.Identifier.Text;
					if(!types.TryGetValue(full, out var info))
					{
						info = new TypeInfo { Namespace = ns, Name = t.Identifier.Text, SourceFile = Path.GetRelativePath(root, file) };
						types[full] = info;
					}
					info.Declarations.Add(t);
					foreach(string u in usings) info.Usings.Add(u);
				}
				foreach(var d in rootNode.DescendantNodes().OfType<DelegateDeclarationSyntax>().Where(d => d.Parent is BaseNamespaceDeclarationSyntax))
				{
					string ns = ((BaseNamespaceDeclarationSyntax)d.Parent).Name.ToString();
					string full = ns + "." + d.Identifier.Text;
					types[full] = new TypeInfo { Namespace = ns, Name = d.Identifier.Text, SourceFile = Path.GetRelativePath(root, file), Delegate = d };
					foreach(string u in usings) types[full].Usings.Add(u);
				}
			}
		}

		var stubbedNames = new HashSet<string>(types.Values.Where(t => !ported.Contains(t.Namespace + "." + t.Name)).Select(t => t.Name));

		if(Directory.Exists(outDir)) Directory.Delete(outDir, true);
		Directory.CreateDirectory(outDir);

		int count = 0;
		foreach(var group in types.Values.Where(t => !ported.Contains(t.Namespace + "." + t.Name)).GroupBy(t => t.SourceFile))
		{
			var sb = new StringBuilder();
			sb.AppendLine("// <auto-generated>");
			sb.AppendLine("// ULDB placeholder generated by tools/uistubgen from upstream " + group.Key.Replace('\\', '/'));
			sb.AppendLine("// Replaced by the real Avalonia port when that type is ported. Do not edit.");
			sb.AppendLine("// </auto-generated>");
			sb.AppendLine("#pragma warning disable CS0067, CS0108, CS0114, CS0169, CS0414, CS0649, CS0659, CS0661, CS0660, CS1998");
			sb.AppendLine();
			foreach(string u in group.SelectMany(t => t.Usings).Distinct().OrderBy(u => u, StringComparer.Ordinal))
			{
				if(prune.Contains("using:" + u)) continue;
				sb.AppendLine("// stub-key: using:" + u);
				sb.AppendLine("using " + u + ";");
			}
			sb.AppendLine();
			foreach(var t in group)
			{
				sb.AppendLine("namespace " + t.Namespace);
				sb.AppendLine("{");
				if(t.Delegate != null)
				{
					string dkey = t.Namespace + "." + t.Name;
					if(prune.Contains(dkey)) { sb.AppendLine("}"); sb.AppendLine(); continue; }
					sb.AppendLine("\t// stub-key: " + dkey);
					sb.AppendLine("\t" + StripAttributes(t.Delegate).ToString().Trim());
				}
				else EmitType(sb, t.Declarations, t.Namespace + "." + t.Name, stubbedNames, 1);
				sb.AppendLine("}");
				sb.AppendLine();
				count++;
			}
			string outfile = Path.Combine(outDir, Path.ChangeExtension(group.Key.Replace(Path.DirectorySeparatorChar, '_').Replace('/', '_'), ".g.cs"));
			File.WriteAllText(outfile, sb.ToString());
		}
		Console.WriteLine($"Generated {count} placeholder types into {outDir} ({prune.Count} pruned members, {ported.Count} ported types)");
		return 0;
	}

	static HashSet<string> ReadList(string file)
	{
		if(file == null || !File.Exists(file)) return new HashSet<string>();
		return new HashSet<string>(File.ReadAllLines(file).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")));
	}

	class TypeInfo
	{
		public string Namespace, Name, SourceFile;
		public DelegateDeclarationSyntax Delegate;
		public List<BaseTypeDeclarationSyntax> Declarations = new();
		public SortedSet<string> Usings = new(StringComparer.Ordinal);
	}

	static void EmitType(StringBuilder sb, List<BaseTypeDeclarationSyntax> decls, string key, HashSet<string> stubbedNames, int depth)
	{
		string ind = new string('\t', depth);
		var first = decls[0];

		if(first is EnumDeclarationSyntax en)
		{
			sb.AppendLine(ind + "// stub-key: " + key);
			sb.AppendLine(Indent(StripAttributes(en).NormalizeWhitespace("\t").ToString(), ind));
			return;
		}

		var td = (TypeDeclarationSyntax)first;
		string kind = td.Keyword.Text; // class, struct, interface, record
		var modifiers = decls.SelectMany(d => d.Modifiers).Select(m => m.Text)
			.Where(m => m != "partial" && m != "sealed").Distinct().ToList();
		if(!modifiers.Any(m => m == "public" || m == "internal" || m == "private" || m == "protected")) modifiers.Insert(0, "internal");

		// WinForms bases become ULDB's compat Form/Control stand-ins (which carry the members
		// callers use, like ShowDialog and ClientSize). WinForms interfaces are dropped,
		// except IWin32Window. Other bases (placeholders, EventArgs, ...) are kept.
		var bases = new List<string>();
		foreach(string b in decls.OfType<TypeDeclarationSyntax>().Where(d => d.BaseList != null)
			.SelectMany(d => d.BaseList.Types).Select(b => b.Type.ToString()).Distinct())
		{
			string name = BaseName(b);
			if(stubbedNames.Contains(name) || !winformsTypes.Contains(name)) bases.Add(b);
			else if(name == "IWin32Window") bases.Add("System.Windows.Forms.IWin32Window");
			else if(name.StartsWith("I") && name.Length > 1 && char.IsUpper(name[1])) continue;
			else if(name == "Form") bases.Add("System.Windows.Forms.Form");
			else if(compatControlTypes.Contains(name) || compatToolStripTypes.Contains(name)) bases.Add("System.Windows.Forms." + name);
			else if(IsControlType(name)) bases.Add("System.Windows.Forms.Control");
		}
		// A class's base class must come first.
		bases = bases.OrderBy(b => BaseName(b).StartsWith("I") && BaseName(b).Length > 1 && char.IsUpper(BaseName(b)[1]) ? 1 : 0).ToList();
		string typeParams = td.TypeParameterList?.ToString() ?? "";
		string constraints = string.Join(" ", td.ConstraintClauses.Select(c => c.ToString()));

		sb.AppendLine(ind + "// stub-key: " + key);
		sb.AppendLine(ind + string.Join(" ", modifiers) + " partial " + kind + " " + td.Identifier.Text + typeParams + (bases.Count > 0 ? " : " + string.Join(", ", bases) : "") + (constraints.Length > 0 ? " " + constraints : ""));
		sb.AppendLine(ind + "{");

		bool isInterface = kind == "interface";
		bool hasParameterlessCtor = false;
		bool isStatic = modifiers.Contains("static");
		var nested = new Dictionary<string, List<BaseTypeDeclarationSyntax>>();
		int n = 0;

		foreach(var member in decls.OfType<TypeDeclarationSyntax>().SelectMany(d => d.Members))
		{
			if(member is BaseTypeDeclarationSyntax nt)
			{
				if(!IsVisible(nt.Modifiers, false)) continue;
				if(!nested.TryGetValue(nt.Identifier.Text, out var list)) nested[nt.Identifier.Text] = list = new List<BaseTypeDeclarationSyntax>();
				list.Add(nt);
				continue;
			}
			if(member is DelegateDeclarationSyntax dd)
			{
				if(!IsVisible(dd.Modifiers, false)) continue;
				string dkey = key + "." + dd.Identifier.Text;
				if(prune.Contains(dkey)) continue;
				sb.AppendLine(ind + "\t// stub-key: " + dkey);
				sb.AppendLine(ind + "\t" + StripAttributes(dd).ToString().Trim());
				continue;
			}
			if(member is ConstructorDeclarationSyntax ctor && ctor.ParameterList.Parameters.Count == 0 && !ctor.Modifiers.Any(SyntaxKind.StaticKeyword))
				hasParameterlessCtor = true;

			string mkey = key + "." + MemberKey(member) + "#" + (n++);
			string code = EmitMember(member, isInterface, td.Identifier.Text);
			if(code == null || prune.Contains(mkey) || manual.Contains(key + "." + MemberKey(member))) continue;
			sb.AppendLine(ind + "\t// stub-key: " + mkey);
			sb.AppendLine(Indent(code, ind + "\t"));
		}

		// Derived placeholders call the parameterless base constructor implicitly. A class that
		// declares no constructors has C#'s implicit public one, which callers may use ("new X()").
		bool anyCtor = decls.OfType<TypeDeclarationSyntax>().SelectMany(d => d.Members).OfType<ConstructorDeclarationSyntax>().Any(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword));
		if(kind == "class" && !hasParameterlessCtor && !isStatic)
			sb.AppendLine(ind + "\t" + (anyCtor ? "protected " : "public ") + td.Identifier.Text + "() { }");

		foreach(var kv in nested) EmitType(sb, kv.Value, key + "." + kv.Key, stubbedNames, depth + 1);
		sb.AppendLine(ind + "}");
	}

	// Non-control WinForms bases (renderers, collections, designers) are dropped rather than mapped.
	static bool IsControlType(string name) => !(name.EndsWith("Renderer") || name.EndsWith("ColorTable") || name.EndsWith("Designer") || name.EndsWith("Collection") || name.EndsWith("EventArgs") || name.EndsWith("Row") || name.EndsWith("Cell") || name.EndsWith("Column") || name == "Component");

	static string BaseName(string type)
	{
		int lt = type.IndexOf('<');
		if(lt >= 0) type = type.Substring(0, lt);
		int dot = type.LastIndexOf('.');
		return dot >= 0 ? type.Substring(dot + 1) : type;
	}

	static bool IsVisible(SyntaxTokenList modifiers, bool interfaceMember)
	{
		if(interfaceMember) return true;
		return modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || m.IsKind(SyntaxKind.InternalKeyword) || m.IsKind(SyntaxKind.ProtectedKeyword));
	}

	static string MemberKey(MemberDeclarationSyntax m) => m switch
	{
		MethodDeclarationSyntax x => x.Identifier.Text,
		PropertyDeclarationSyntax x => x.Identifier.Text,
		FieldDeclarationSyntax x => string.Join(",", x.Declaration.Variables.Select(v => v.Identifier.Text)),
		EventFieldDeclarationSyntax x => string.Join(",", x.Declaration.Variables.Select(v => v.Identifier.Text)),
		EventDeclarationSyntax x => x.Identifier.Text,
		ConstructorDeclarationSyntax x => ".ctor",
		IndexerDeclarationSyntax => "this[]",
		_ => m.Kind().ToString()
	};

	static string Mods(SyntaxTokenList modifiers, bool keepAbstract = false)
	{
		var result = new List<string>();
		foreach(var m in modifiers)
		{
			switch(m.Kind())
			{
				case SyntaxKind.OverrideKeyword: result.Add("virtual"); break; // Bases may be dropped WinForms types
				case SyntaxKind.AbstractKeyword: if(keepAbstract) result.Add("abstract"); else result.Add("virtual"); break;
				case SyntaxKind.SealedKeyword:
				case SyntaxKind.AsyncKeyword:
				case SyntaxKind.ExternKeyword:
				case SyntaxKind.PartialKeyword:
				case SyntaxKind.ReadOnlyKeyword:
				case SyntaxKind.UnsafeKeyword:
					break;
				default: result.Add(m.Text); break;
			}
		}
		return string.Join(" ", result.Distinct());
	}

	static string EmitMember(MemberDeclarationSyntax member, bool isInterface, string typeName)
	{
		switch(member)
		{
			case FieldDeclarationSyntax f:
			{
				if(!IsVisible(f.Modifiers, false)) return null;
				bool isConst = f.Modifiers.Any(SyntaxKind.ConstKeyword);
				if(isConst) return StripAttributes(f).ToString().Trim();
				var vars = string.Join(", ", f.Declaration.Variables.Select(v => v.Identifier.Text));
				return Mods(f.Modifiers) + " " + f.Declaration.Type + " " + vars + ";";
			}
			case EventFieldDeclarationSyntax ef:
				if(!IsVisible(ef.Modifiers, isInterface)) return null;
				return (isInterface ? "" : Mods(ef.Modifiers) + " ") + "event " + ef.Declaration.Type + " " + string.Join(", ", ef.Declaration.Variables.Select(v => v.Identifier.Text)) + ";";
			case EventDeclarationSyntax ev:
				if(!IsVisible(ev.Modifiers, isInterface)) return null;
				return (isInterface ? "" : Mods(ev.Modifiers) + " ") + "event " + ev.Type + " " + ev.Identifier.Text + ";";
			case PropertyDeclarationSyntax p:
			{
				if(!IsVisible(p.Modifiers, isInterface) || p.ExplicitInterfaceSpecifier != null) return null;
				var accessors = new List<string>();
				if(p.ExpressionBody != null) accessors.Add("get");
				else if(p.AccessorList != null)
				{
					foreach(var a in p.AccessorList.Accessors)
					{
						string amods = string.Join(" ", a.Modifiers.Select(m => m.Text));
						if(amods == "private" && !isInterface) amods = "protected"; // Keep settable by derived ports
						accessors.Add((amods.Length > 0 ? amods + " " : "") + a.Keyword.Text);
					}
				}
				if(isInterface) return p.Type + " " + p.Identifier.Text + " { " + string.Join(" ", accessors.Select(a => a + ";")) + " }";
				string body = string.Join(" ", accessors.Select(a => a.EndsWith("get") ? a + " => default;" : a + " { }"));
				return Mods(p.Modifiers) + " " + p.Type + " " + p.Identifier.Text + " { " + body + " }";
			}
			case IndexerDeclarationSyntax ix:
			{
				if(!IsVisible(ix.Modifiers, isInterface)) return null;
				bool hasSet = ix.AccessorList?.Accessors.Any(a => a.Keyword.IsKind(SyntaxKind.SetKeyword)) ?? false;
				if(isInterface) return ix.Type + " this" + ix.ParameterList + " { get;" + (hasSet ? " set;" : "") + " }";
				return Mods(ix.Modifiers) + " " + ix.Type + " this" + ix.ParameterList + " { get => default;" + (hasSet ? " set { }" : "") + " }";
			}
			case MethodDeclarationSyntax m:
			{
				if(!IsVisible(m.Modifiers, isInterface) || m.ExplicitInterfaceSpecifier != null) return null;
				string sig = m.ReturnType + " " + m.Identifier.Text + m.TypeParameterList + StripParamAttributes(m.ParameterList) + (m.ConstraintClauses.Count > 0 ? " " + string.Join(" ", m.ConstraintClauses) : "");
				if(isInterface) return sig + ";";
				var outs = m.ParameterList.Parameters.Where(pp => pp.Modifiers.Any(SyntaxKind.OutKeyword)).Select(pp => pp.Identifier.Text + " = default; ");
				bool isVoid = m.ReturnType is PredefinedTypeSyntax pt && pt.Keyword.IsKind(SyntaxKind.VoidKeyword);
				return Mods(m.Modifiers) + " " + sig + " { " + string.Concat(outs) + (isVoid ? "" : "return default; ") + "}";
			}
			case ConstructorDeclarationSyntax c:
			{
				if(c.Modifiers.Any(SyntaxKind.StaticKeyword)) return null;
				if(!IsVisible(c.Modifiers, false)) return null;
				var outs = c.ParameterList.Parameters.Where(pp => pp.Modifiers.Any(SyntaxKind.OutKeyword)).Select(pp => pp.Identifier.Text + " = default; ");
				return Mods(c.Modifiers) + " " + typeName + StripParamAttributes(c.ParameterList) + " { " + string.Concat(outs) + "}";
			}
			default:
				return null; // Operators, destructors, conversion operators: not needed by callers
		}
	}

	static string StripParamAttributes(ParameterListSyntax list)
	{
		return "(" + string.Join(", ", list.Parameters.Select(p => p.WithAttributeLists(default).ToString().Trim())) + ")";
	}

	static T StripAttributes<T>(T node) where T : SyntaxNode
	{
		var lists = node.DescendantNodesAndSelf().OfType<AttributeListSyntax>()
			.Where(a => !a.Attributes.All(at => at.Name.ToString() is "Flags" or "FlagsAttribute" or "System.Flags"));
		return node.RemoveNodes(lists, SyntaxRemoveOptions.KeepNoTrivia);
	}

	static string Indent(string code, string ind)
	{
		return string.Join("\n", code.Replace("\r\n", "\n").Split('\n').Select(l => ind + l.TrimEnd()));
	}
}
