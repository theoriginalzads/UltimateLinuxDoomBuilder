# ULDB build of BuilderNative (libBuilderNative.so): upstream's Source/Native plus
# ULDB's hosted-context support. Upstream sources are compiled in place with ULDB
# defined; ULDBHost.h is force-included so upstream files need no include edits.

cmake_minimum_required(VERSION 3.20)
project(BuilderNative LANGUAGES C CXX)

set(UPSTREAM_NATIVE "${CMAKE_CURRENT_SOURCE_DIR}/../../Native")

# CPU target for this build. "generic" runs on any x86-64 CPU; the others are for
# personal builds (a binary built for x86-64-v4/znver4 won't start on older CPUs).
set(UDB_ARCH "generic" CACHE STRING "CPU target: generic, x86-64-v3, x86-64-v4, znver4 or native")
set_property(CACHE UDB_ARCH PROPERTY STRINGS generic x86-64-v3 x86-64-v4 znver4 native)

if(NOT CMAKE_BUILD_TYPE)
	set(CMAKE_BUILD_TYPE Release)
endif()

file(GLOB UPSTREAM_SOURCES
	"${UPSTREAM_NATIVE}/*.cpp"
	"${UPSTREAM_NATIVE}/OpenGL/*.cpp"
	"${UPSTREAM_NATIVE}/OpenGL/gl_load/*.c"
	"${UPSTREAM_NATIVE}/VPO/*.cpp")
# Precomp.cpp only exists for MSVC precompiled headers.
list(FILTER UPSTREAM_SOURCES EXCLUDE REGEX "/Precomp\\.cpp$")

# Upstream compiles everything with g++, including gl_load.c (it links against the
# C++ GL_GetProcAddress), so build the .c files as C++ too.
set_source_files_properties(${UPSTREAM_SOURCES} PROPERTIES LANGUAGE CXX)

add_library(BuilderNative SHARED ${UPSTREAM_SOURCES} HostedContext.cpp)

set_target_properties(BuilderNative PROPERTIES
	CXX_STANDARD 14
	CXX_STANDARD_REQUIRED ON
	POSITION_INDEPENDENT_CODE ON)

target_include_directories(BuilderNative PRIVATE "${UPSTREAM_NATIVE}" "${CMAKE_CURRENT_SOURCE_DIR}")
target_compile_definitions(BuilderNative PRIVATE UDB_LINUX=1 ULDB=1)
target_compile_options(BuilderNative PRIVATE
	"$<$<COMPILE_LANGUAGE:CXX>:-include;${CMAKE_CURRENT_SOURCE_DIR}/ULDBHost.h>")

if(NOT UDB_ARCH STREQUAL "generic")
	target_compile_options(BuilderNative PRIVATE "-march=${UDB_ARCH}")
	if(UDB_ARCH STREQUAL "native" OR UDB_ARCH STREQUAL "znver4")
		target_compile_options(BuilderNative PRIVATE "-mtune=${UDB_ARCH}")
	endif()
endif()

find_package(X11 REQUIRED)
target_link_libraries(BuilderNative PRIVATE ${X11_LIBRARIES} ${X11_Xfixes_LIB} ${CMAKE_DL_LIBS})

# libuser32.so: no-op stand-ins for the user32 functions upstream P/Invokes (User32Stubs.c).
add_library(user32 SHARED User32Stubs.c)
set_target_properties(user32 PROPERTIES C_VISIBILITY_PRESET hidden)
