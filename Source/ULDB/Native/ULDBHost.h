/*
**  ULDB: hosted OpenGL context support for BuilderNative.
**
**  Upstream BuilderNative creates its own OpenGL context on a native window.
**  In ULDB the context belongs to the host instead: the Avalonia UI (a context
**  shared with Avalonia's) or a test harness (an off-screen EGL context). The
**  host registers callbacks before creating a RenderDevice, and the renderer
**  draws into the framebuffer the host names instead of framebuffer 0.
**
**  This file is part of ULDB, not upstream UDB.
*/

#pragma once

#include <memory>

class IOpenGLContext;

typedef void* (*ULDB_GetProcAddressFn)(const char* name, void* user);
typedef int (*ULDB_MakeCurrentFn)(void* user);
typedef void (*ULDB_ClearCurrentFn)(void* user);
typedef void (*ULDB_PresentFn)(void* user);

extern "C"
{
	// Registers the host context. getproc and makecurrent are required; clearcurrent may be null.
	void ULDB_SetHostContext(ULDB_GetProcAddressFn getproc, ULDB_MakeCurrentFn makecurrent, ULDB_ClearCurrentFn clearcurrent, void* user);

	// Called when the renderer presents a frame (instead of swapping buffers). Optional.
	void ULDB_SetHostPresent(ULDB_PresentFn present);

	// Unregisters the host context. RenderDevices created with it must be deleted first.
	void ULDB_ClearHostContext();

	// The framebuffer the renderer draws into when no render target texture is set,
	// and its size in pixels. Call whenever the host's view is resized.
	void ULDB_SetHostFramebuffer(unsigned int framebuffer, int width, int height);
}

// Used by the #ifdef ULDB hooks in upstream BuilderNative code.
bool ULDB_IsHosted();
void* ULDB_HostGetProcAddress(const char* name);
unsigned int ULDB_DefaultFramebuffer();
std::unique_ptr<IOpenGLContext> ULDB_CreateHostedContext();
