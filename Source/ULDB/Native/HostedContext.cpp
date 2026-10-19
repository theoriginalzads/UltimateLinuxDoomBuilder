/*
**  ULDB: hosted OpenGL context for BuilderNative. See ULDBHost.h.
**  This file is part of ULDB, not upstream UDB.
*/

#include "Precomp.h"
#include "OpenGL/OpenGLContext.h"
#include "Backend.h"
#include "ULDBHost.h"

namespace
{
	struct HostState
	{
		ULDB_GetProcAddressFn getproc = nullptr;
		ULDB_MakeCurrentFn makecurrent = nullptr;
		ULDB_ClearCurrentFn clearcurrent = nullptr;
		void* user = nullptr;
		unsigned int framebuffer = 0;
		int width = 1;
		int height = 1;
	};

	HostState host;

	class HostedContext : public IOpenGLContext
	{
	public:
		HostedContext()
		{
			if (!host.makecurrent(host.user))
			{
				SetError("The host could not make its OpenGL context current");
				return;
			}

			// gl_load resolves every entry point through ULDB_HostGetProcAddress. Load once per
			// process, like upstream; the host's context must stay compatible across devices.
			static bool loaded = false;
			if (!loaded)
			{
				ogl_LoadFunctions();
				loaded = true;
			}

			GLint major = 0, minor = 0;
			glGetIntegerv(GL_MAJOR_VERSION, &major);
			glGetIntegerv(GL_MINOR_VERSION, &minor);
			if (major < 3 || (major == 3 && minor < 2))
			{
				SetError("The host OpenGL context is version %d.%d; ULDB needs 3.2 or newer", (int)major, (int)minor);
				return;
			}
			valid = true;
		}

		void MakeCurrent() override { host.makecurrent(host.user); }
		void ClearCurrent() override { if (host.clearcurrent) host.clearcurrent(host.user); }
		void SwapBuffers() override { } // The host presents the frame
		bool IsCurrent() override { return true; }

		int GetWidth() const override { return host.width; }
		int GetHeight() const override { return host.height; }

		bool IsValid() const { return valid; }

	private:
		bool valid = false;
	};
}

extern "C"
{
	void ULDB_SetHostContext(ULDB_GetProcAddressFn getproc, ULDB_MakeCurrentFn makecurrent, ULDB_ClearCurrentFn clearcurrent, void* user)
	{
		host.getproc = getproc;
		host.makecurrent = makecurrent;
		host.clearcurrent = clearcurrent;
		host.user = user;
	}

	void ULDB_ClearHostContext()
	{
		host = HostState();
	}

	void ULDB_SetHostFramebuffer(unsigned int framebuffer, int width, int height)
	{
		host.framebuffer = framebuffer;
		host.width = std::max(width, 1);
		host.height = std::max(height, 1);
	}
}

bool ULDB_IsHosted()
{
	return host.getproc != nullptr && host.makecurrent != nullptr;
}

void* ULDB_HostGetProcAddress(const char* name)
{
	return host.getproc ? host.getproc(name, host.user) : nullptr;
}

unsigned int ULDB_DefaultFramebuffer()
{
	return host.framebuffer;
}

std::unique_ptr<IOpenGLContext> ULDB_CreateHostedContext()
{
	auto ctx = std::make_unique<HostedContext>();
	if (!ctx->IsValid()) return nullptr;
	return ctx;
}
