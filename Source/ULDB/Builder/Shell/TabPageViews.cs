// ULDB: views for tab pages that plugins add to the Preferences dialog (PreferencesController.AddTab).
// The page's content is a form's own designer layout; a plugin registers an Avalonia view of its
// preferences form here, and ModelBinder.BindTabs shows it as a tab.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using WF = System.Windows.Forms;

namespace ULDB.Shell
{
	public static class TabPageViews
	{
		private static readonly Dictionary<Type, Func<WF.Form, WF.TabPage, Control>> factories = new Dictionary<Type, Func<WF.Form, WF.TabPage, Control>>();

		public static void Register<TForm>(Func<TForm, WF.TabPage, Control> factory) where TForm : WF.Form
		{
			factories[typeof(TForm)] = (f, p) => factory((TForm)f, p);
		}

		/// <summary>The view of a page that came from a registered form, or null.</summary>
		public static Control Create(WF.TabPage page)
		{
			WF.Form owner = page.OwnerForm;
			return owner != null && factories.TryGetValue(owner.GetType(), out Func<WF.Form, WF.TabPage, Control> factory) ? factory(owner, page) : null;
		}
	}
}
