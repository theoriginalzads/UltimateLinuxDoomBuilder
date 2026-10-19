// ULDB compatibility layer: System.Media.SystemSounds (Windows-only on modern .NET).
// Sounds are played by the UI through WinFormsHost; the headless host ignores them.

namespace System.Media
{
	public sealed class SystemSound
	{
		internal SystemSound(string name) { Name = name; }

		public string Name { get; }

		public void Play() => SoundPlayer?.Invoke(Name);

		/// <summary>Installed by the UI to play (or flash for) a named system sound.</summary>
		public static Action<string> SoundPlayer { get; set; }
	}

	public static class SystemSounds
	{
		public static SystemSound Asterisk { get; } = new SystemSound("Asterisk");
		public static SystemSound Beep { get; } = new SystemSound("Beep");
		public static SystemSound Exclamation { get; } = new SystemSound("Exclamation");
		public static SystemSound Hand { get; } = new SystemSound("Hand");
		public static SystemSound Question { get; } = new SystemSound("Question");
	}
}
