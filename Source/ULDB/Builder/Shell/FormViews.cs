// ULDB: windows for model-ported forms (upstream's form code compiled unchanged, with an
// Avalonia window as its view). Installed as compat's Form.ViewFactory; plugins register theirs.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using CodeImp.DoomBuilder.Windows;
using WF = System.Windows.Forms;

namespace ULDB.Shell
{
	public static class FormViews
	{
		private static readonly Dictionary<Type, Func<WF.Form, Window>> factories = new Dictionary<Type, Func<WF.Form, Window>>();

		static FormViews()
		{
			Register<VertexEditForm>(f => new VertexEditView(f));
			Register<TextEditForm>(f => new TextEditView(f));
		}

		/// <summary>Registers the window for a form type.</summary>
		public static void Register<T>(Func<T, Window> create) where T : WF.Form
		{
			factories[typeof(T)] = f => create((T)f);
		}

		/// <summary>A new window for <paramref name="form"/>, or null when it has none.</summary>
		public static Window Create(WF.Form form)
		{
			return factories.TryGetValue(form.GetType(), out Func<WF.Form, Window> create) ? create(form) : null;
		}

		internal static void Install() => WF.Form.ViewFactory = Create;
	}
}
