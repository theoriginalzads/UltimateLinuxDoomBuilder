// ULDB: some upstream P/Invokes name the native library Windows-style ("BuilderNative.dll",
// "user32.dll"). On Linux .NET would look for "BuilderNative.dll.so"; map them to
// libBuilderNative.so / libuser32.so (ULDB's stubs for the few user32 calls upstream makes) like
// the plain imports. Plugins get the same mapping, and find these libraries next to Builder.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ULDB.Platform
{
	internal static class NativeLibraries
	{
		[ModuleInitializer]
		internal static void Install()
		{
			NativeLibrary.SetDllImportResolver(typeof(NativeLibraries).Assembly, Resolve);

			// Plugins (BuilderModes' UndoRedoPanel calls user32's GetWindowLong) load later, from Plugins/.
			AppDomain.CurrentDomain.AssemblyLoad += (s, e) =>
			{
				Assembly asm = e.LoadedAssembly;
				if(asm.IsDynamic || string.IsNullOrEmpty(asm.Location)) return;
				if(!Path.GetFileName(Path.GetDirectoryName(asm.Location)).Equals("Plugins", StringComparison.OrdinalIgnoreCase)) return;
				try { NativeLibrary.SetDllImportResolver(asm, Resolve); }
				catch(InvalidOperationException) { } // Already has one
			};
		}

		private static IntPtr Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
		{
			if(!name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) return IntPtr.Zero; // Default probing
			string plain = name.Substring(0, name.Length - 4);
			if(NativeLibrary.TryLoad(plain, assembly, searchPath, out IntPtr handle)) return handle;
			string beside = Path.Combine(AppContext.BaseDirectory, "lib" + plain + ".so");
			return File.Exists(beside) && NativeLibrary.TryLoad(beside, out handle) ? handle : IntPtr.Zero;
		}
	}
}
