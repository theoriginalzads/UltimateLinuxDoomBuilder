// ULDB: the in-scope plugins build their Avalonia views with ULDB's internal UI helpers
// (Shell.ModelBinder, Shell.BreezeIcons, ...), which aren't public API.

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BuilderModes")]
[assembly: InternalsVisibleTo("ThreeDFloorMode")]
[assembly: InternalsVisibleTo("BuilderEffects")]
[assembly: InternalsVisibleTo("StairSectorBuilder")]
[assembly: InternalsVisibleTo("TagRange")]
[assembly: InternalsVisibleTo("SoundPropagationMode")]
[assembly: InternalsVisibleTo("ColorPicker")]
[assembly: InternalsVisibleTo("TagExplorer")]
[assembly: InternalsVisibleTo("CommentsPanel")]
