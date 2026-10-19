// ULDB: maps model-ported upstream user controls (docker contents and the like) to their
// Avalonia views. Built-in views are registered here; plugins register theirs on load.

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using CodeImp.DoomBuilder.Controls;
using WF = System.Windows.Forms;

namespace ULDB.Shell
{
	public static class ControlViews
	{
		private static readonly Dictionary<Type, Func<WF.Control, Control>> factories = new Dictionary<Type, Func<WF.Control, Control>>();

		static ControlViews()
		{
			Register<HintsPanel>(m => new HintsView(m));
			Register<ButtonsNumericTextbox>(m => new ButtonsNumericTextboxView(m));
			Register<FieldsEditorControl>(m => new FieldsEditorView(m));
			Register<PairedFloatControl>(m => new PairedFloatControlView(m));
			Register<ColorControl>(m => new ColorControlView(m));
			Register<ColorFieldsControl>(m => new ColorFieldsControlView(m));
			Register<ActionSpecialHelpButton>(m => new ActionSpecialHelpButtonView(m));
			Register<ActionSelectorControl>(m => new ActionSelectorControlView(m));
			Register<ArgumentBox>(m => new ArgumentBoxView(m));
			Register<ArgumentsControl>(m => new ArgumentsControlView(m));
			Register<TagSelector>(m => new TagSelectorView(m));
			Register<CommentEditor>(m => new CommentEditorView(m));
			Register<CheckboxArrayControl>(m => new CheckboxArrayControlView(m));
			Register<ThingBrowserControl>(m => new ThingBrowserControlView(m));
			Register<AngleControlEx>(m => new ULDB.Controls.AngleEditor(m));
			Register<PairedFieldsControl>(m => new PairedFieldsControlView(m));
			Register<PairedIntControl>(m => new PairedIntControlView(m));
			Register<SidedefPartLightControl>(m => new SidedefPartLightControlView(m));
			Register<SectorSlopeControl>(m => new SectorSlopeControlView(m));
			Register<TagsSelector>(m => new TagsSelectorView(m));
			Register<ImageSelectorControl>(m => new ImageSelectorControlView(m));
		}

		/// <summary>Registers the view for a model control type (and types derived from it).</summary>
		public static void Register<T>(Func<T, Control> create) where T : WF.Control
		{
			factories[typeof(T)] = m => create((T)m);
		}

		/// <summary>A new view of <paramref name="model"/>, or null when there's no view for its type.</summary>
		public static Control Create(WF.Control model)
		{
			for(Type t = model.GetType(); t != null; t = t.BaseType)
			{
				if(!factories.TryGetValue(t, out Func<WF.Control, Control> create)) continue;
				Control view = create(model);
				// WinForms raises a UserControl's Load when it's first shown.
				if(model is WF.UserControl uc)
				{
					bool loaded = false;
					view.AttachedToVisualTree += (s, e) => { if(!loaded) { loaded = true; uc.RaiseLoad(); } };
				}
				return view;
			}
			return null;
		}
	}
}
