// ULDB: converts Avalonia keyboard and mouse input into the WinForms-style codes upstream
// UDB uses everywhere: Keys values (Windows virtual-key codes) for shortcuts stored in
// settings and Actions.cfg, and MouseButtons. Keeping these codes means existing UDB
// shortcut settings import unchanged.

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using AKey = Avalonia.Input.Key;
using AKeyModifiers = Avalonia.Input.KeyModifiers;
using AMouseButton = Avalonia.Input.MouseButton;

namespace ULDB.Input
{
	public static class KeyMapping
	{
		private static readonly Dictionary<AKey, Keys> map = Build();

		/// <summary>The WinForms key code for an Avalonia key, or Keys.None when UDB has no equivalent.</summary>
		public static Keys ToKeys(AKey key) => map.TryGetValue(key, out Keys k) ? k : Keys.None;

		/// <summary>Key code plus modifier flags, as UDB stores shortcuts (e.g. Ctrl+S = Keys.Control | Keys.S).</summary>
		public static Keys ToKeys(AKey key, AKeyModifiers modifiers) => ToKeys(key) | ToModifiers(modifiers);

		public static Keys ToModifiers(AKeyModifiers modifiers)
		{
			Keys result = Keys.None;
			if((modifiers & AKeyModifiers.Control) != 0) result |= Keys.Control;
			if((modifiers & AKeyModifiers.Shift) != 0) result |= Keys.Shift;
			if((modifiers & AKeyModifiers.Alt) != 0) result |= Keys.Alt;
			return result;
		}

		public static MouseButtons ToMouseButton(AMouseButton button)
		{
			switch(button)
			{
				case AMouseButton.Left: return MouseButtons.Left;
				case AMouseButton.Right: return MouseButtons.Right;
				case AMouseButton.Middle: return MouseButtons.Middle;
				case AMouseButton.XButton1: return MouseButtons.XButton1;
				case AMouseButton.XButton2: return MouseButtons.XButton2;
				default: return MouseButtons.None;
			}
		}

		/// <summary>All Avalonia keys that map to a WinForms key code (for tests and diagnostics).</summary>
		public static IReadOnlyDictionary<AKey, Keys> All => map;

		private static Dictionary<AKey, Keys> Build()
		{
			Dictionary<AKey, Keys> m = new Dictionary<AKey, Keys>();

			// Letters, digits, function keys and numpad digits share names in both enums.
			for(AKey k = AKey.A; k <= AKey.Z; k++) m[k] = Keys.A + (k - AKey.A);
			for(AKey k = AKey.D0; k <= AKey.D9; k++) m[k] = Keys.D0 + (k - AKey.D0);
			for(AKey k = AKey.NumPad0; k <= AKey.NumPad9; k++) m[k] = Keys.NumPad0 + (k - AKey.NumPad0);
			for(AKey k = AKey.F1; k <= AKey.F24; k++) m[k] = Keys.F1 + (k - AKey.F1);

			// Everything else, by hand: the enums' names and order differ.
			(AKey, Keys)[] pairs =
			{
				(AKey.Back, Keys.Back), (AKey.Tab, Keys.Tab), (AKey.Enter, Keys.Return), (AKey.Escape, Keys.Escape),
				(AKey.Space, Keys.Space), (AKey.PageUp, Keys.Prior), (AKey.PageDown, Keys.Next),
				(AKey.End, Keys.End), (AKey.Home, Keys.Home), (AKey.Left, Keys.Left), (AKey.Up, Keys.Up),
				(AKey.Right, Keys.Right), (AKey.Down, Keys.Down), (AKey.Insert, Keys.Insert), (AKey.Delete, Keys.Delete),
				(AKey.Pause, Keys.Pause), (AKey.CapsLock, Keys.CapsLock), (AKey.NumLock, Keys.NumLock),
				(AKey.Scroll, Keys.Scroll), (AKey.PrintScreen, Keys.PrintScreen), (AKey.Apps, Keys.Apps),
				(AKey.Multiply, Keys.Multiply), (AKey.Add, Keys.Add), (AKey.Subtract, Keys.Subtract),
				(AKey.Decimal, Keys.Decimal), (AKey.Divide, Keys.Divide), (AKey.Separator, Keys.Separator),
				// Modifier keys pressed on their own (UDB's KeyDown sees these as ShiftKey etc.)
				(AKey.LeftShift, Keys.ShiftKey), (AKey.RightShift, Keys.ShiftKey),
				(AKey.LeftCtrl, Keys.ControlKey), (AKey.RightCtrl, Keys.ControlKey),
				(AKey.LeftAlt, Keys.Menu), (AKey.RightAlt, Keys.Menu),
				(AKey.LWin, Keys.LWin), (AKey.RWin, Keys.RWin),
				// US-layout punctuation (the OEM keys), as Windows reports them.
				(AKey.OemSemicolon, Keys.OemSemicolon), (AKey.OemPlus, Keys.Oemplus), (AKey.OemComma, Keys.Oemcomma),
				(AKey.OemMinus, Keys.OemMinus), (AKey.OemPeriod, Keys.OemPeriod), (AKey.OemQuestion, Keys.OemQuestion),
				(AKey.OemTilde, Keys.Oemtilde), (AKey.OemOpenBrackets, Keys.OemOpenBrackets), (AKey.OemPipe, Keys.OemPipe),
				(AKey.OemCloseBrackets, Keys.OemCloseBrackets), (AKey.OemQuotes, Keys.OemQuotes), (AKey.OemBackslash, Keys.OemBackslash),
			};
			foreach((AKey a, Keys k) in pairs) m[a] = k;
			return m;
		}
	}
}
