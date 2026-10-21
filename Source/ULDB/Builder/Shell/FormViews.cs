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
			Register<ThingEditFormUDMF>(f => new ThingEditFormUDMFView(f));
			Register<ActionBrowserForm>(f => new ActionBrowserView(f));
			Register<SectorEditFormUDMF>(f => new SectorEditFormUDMFView(f));
			Register<LinedefEditFormUDMF>(f => new LinedefEditFormUDMFView(f));
			Register<AboutForm>(f => new AboutView(f));
			Register<TextureBrowserForm>(f => new TextureBrowserFormView(f));
			Register<GridSetupForm>(f => new GridSetupFormView(f));
			Register<CenterOnCoordinatesForm>(f => new CenterOnCoordinatesFormView(f));
			Register<ChangeMapForm>(f => new ChangeMapFormView(f));
			Register<TagStatisticsForm>(f => new TagStatisticsFormView(f));
			Register<ThingStatisticsForm>(f => new ThingStatisticsFormView(f));
			Register<PasteOptionsForm>(f => new PasteOptionsFormView(f));
			Register<ThingsFiltersForm>(f => new ThingsFiltersFormView(f));
			Register<SectorEditForm>(f => new SectorEditFormView(f));
			Register<LinedefEditForm>(f => new LinedefEditFormView(f));
			Register<ThingEditForm>(f => new ThingEditFormView(f));
			Register<ConfigForm>(f => new ConfigFormView(f));
			Register<PreferencesForm>(f => new PreferencesFormView(f));
			Register<ThingBrowserForm>(f => new ThingBrowserFormView(f));
			Register<ThingMultipleBrowserForm>(f => new ThingMultipleBrowserFormView(f));
			Register<EffectBrowserForm>(f => new EffectBrowserFormView(f));
			Register<AngleForm>(f => new AngleFormView(f));
			Register<RunExternalCommandForm>(f => new RunExternalCommandFormView(f));
			Register<LinedefColorPresetsForm>(f => new LinedefColorPresetsFormView(f));
			Register<CustomFieldsForm>(f => new CustomFieldsFormView(f));
			Register<BitFlagsAndOptionsForm>(f => new BitFlagsAndOptionsFormView(f));
			Register<BitFlagsForm>(f => new BitFlagsFormView(f));
			Register<FlagsForm>(f => new FlagsFormView(f));
			Register<TextureSetForm>(f => new TextureSetFormView(f));
			Register<ScriptEditorForm>(f => new ScriptEditorFormView(f));
			Register<ScriptGoToLineForm>(f => new ScriptGoToLineFormView(f));
			Register<ScriptFindReplaceForm>(f => new ScriptFindReplaceFormView(f));
			Register<PreAndPostCommandsForm>(f => new PreAndPostCommandsFormView(f));
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
