// ULDB: some upstream P/Invokes name the native library "BuilderNative.dll" (Windows
// style). On Linux .NET would look for "BuilderNative.dll.so"; map them to
// libBuilderNative.so like the plain "BuilderNative" imports.

using System;
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
		}

		private static IntPtr Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
		{
			if(name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
			   NativeLibrary.TryLoad(name.Substring(0, name.Length - 4), assembly, searchPath, out IntPtr handle))
				return handle;
			return IntPtr.Zero; // Default probing
		}
	}
}
