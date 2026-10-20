// ULDB: lets the host (the Avalonia UI, or a test harness) supply the OpenGL context
// BuilderNative renders with. Register before creating a RenderDevice. See
// Source/ULDB/Native/ULDBHost.h.

using System;
using System.Runtime.InteropServices;

namespace ULDB.Rendering
{
	public static unsafe class HostedGL
	{
		private static Func<string, IntPtr> getprocaddress;
		private static Func<bool> makecurrent;
		private static Action clearcurrent;
		private static Action present;

		public static bool IsRegistered => getprocaddress != null;

		/// <summary>Registers the host context. Callbacks can be called on any thread that renders.</summary>
		/// <param name="getProcAddress">Resolves an OpenGL entry point by name (e.g. eglGetProcAddress).</param>
		/// <param name="makeCurrent">Makes the host context current on the calling thread; returns false on failure.</param>
		/// <param name="clearCurrent">Releases the context from the calling thread (optional).</param>
		/// <param name="present">Called when the renderer presents a finished frame (optional).</param>
		public static void Register(Func<string, IntPtr> getProcAddress, Func<bool> makeCurrent, Action clearCurrent = null, Action present = null)
		{
			getprocaddress = getProcAddress ?? throw new ArgumentNullException(nameof(getProcAddress));
			makecurrent = makeCurrent ?? throw new ArgumentNullException(nameof(makeCurrent));
			clearcurrent = clearCurrent;
			HostedGL.present = present;
			ULDB_SetHostContext(&GetProcAddressCallback, &MakeCurrentCallback, &ClearCurrentCallback, IntPtr.Zero);
			ULDB_SetHostPresent(&PresentCallback);
		}

		/// <summary>Unregisters the host context. Dispose RenderDevices created with it first.</summary>
		public static void Unregister()
		{
			ULDB_ClearHostContext();
			getprocaddress = null;
			makecurrent = null;
			clearcurrent = null;
			present = null;
		}

		/// <summary>The framebuffer the renderer draws to when it has no render target texture, and its size in pixels.</summary>
		public static void SetFramebuffer(uint framebuffer, int width, int height)
		{
			ULDB_SetHostFramebuffer(framebuffer, width, height);
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
		private static IntPtr GetProcAddressCallback(byte* name, IntPtr user)
		{
			return getprocaddress?.Invoke(Marshal.PtrToStringUTF8((IntPtr)name)) ?? IntPtr.Zero;
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
		private static int MakeCurrentCallback(IntPtr user)
		{
			return (makecurrent?.Invoke() ?? false) ? 1 : 0;
		}

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
		private static void ClearCurrentCallback(IntPtr user)
		{
			clearcurrent?.Invoke();
		}

		[DllImport("BuilderNative", CallingConvention = CallingConvention.Cdecl)]
		private static extern void ULDB_SetHostContext(
			delegate* unmanaged[Cdecl]<byte*, IntPtr, IntPtr> getproc,
			delegate* unmanaged[Cdecl]<IntPtr, int> makecurrent,
			delegate* unmanaged[Cdecl]<IntPtr, void> clearcurrent,
			IntPtr user);

		[UnmanagedCallersOnly(CallConvs = new[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
		private static void PresentCallback(IntPtr user)
		{
			present?.Invoke();
		}

		[DllImport("BuilderNative", CallingConvention = CallingConvention.Cdecl)]
		private static extern void ULDB_SetHostPresent(delegate* unmanaged[Cdecl]<IntPtr, void> present);

		[DllImport("BuilderNative", CallingConvention = CallingConvention.Cdecl)]
		private static extern void ULDB_ClearHostContext();

		[DllImport("BuilderNative", CallingConvention = CallingConvention.Cdecl)]
		private static extern void ULDB_SetHostFramebuffer(uint framebuffer, int width, int height);
	}
}
