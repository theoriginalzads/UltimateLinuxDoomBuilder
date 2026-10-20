# Builds ULDB's script compilers and node builder from source (git submodules in src/),
# out of tree so the submodules stay clean:
#   acc     ZDoom ACC 1.60           ACS scripts
#   zdbsp   ZDBSP 1.19               nodes (incl. UDMF/ZDoom extended formats)
#   zt-bcc  ZT-BCC 0.10.0-alpha.8    BCS scripts
#   bcc     BCC (wormt, master)      BCS scripts; ships with its own lib/ (see docs/upstream-edits.md)
# Binaries end up in ${CMAKE_BINARY_DIR}/bin. UDB_ARCH works as for BuilderNative.

cmake_minimum_required(VERSION 3.20)
project(ULDBCompilers NONE)

include(ExternalProject)

set(UDB_ARCH "generic" CACHE STRING "CPU target: generic, x86-64-v3, x86-64-v4, znver4 or native")
set(SRC "${CMAKE_CURRENT_SOURCE_DIR}/src")
set(OUT "${CMAKE_BINARY_DIR}/bin")
if(NOT CMAKE_BUILD_TYPE)
	set(CMAKE_BUILD_TYPE Release)
endif()

set(ARCH_FLAGS "")
if(NOT UDB_ARCH STREQUAL "generic")
	set(ARCH_FLAGS "-march=${UDB_ARCH}")
endif()

foreach(missing acc zdbsp zt-bcc bcc)
	if(NOT EXISTS "${SRC}/${missing}/src" AND NOT EXISTS "${SRC}/${missing}/CMakeLists.txt")
		message(FATAL_ERROR "tools/compilers/src/${missing} is empty. Run: git submodule update --init --recursive")
	endif()
endforeach()

# ACC links fully static (its own CMakeLists). The Freedesktop SDK's GCC (Flatpak builds) always
# adds -latomic_asneeded, which has no static version; ACC uses no atomics, so an empty archive
# by that name on the library path satisfies it. Elsewhere it's never looked at.
find_program(AR_PROGRAM NAMES ar REQUIRED)
set(STATIC_SHIM "${CMAKE_BINARY_DIR}/static-shim")
file(MAKE_DIRECTORY "${STATIC_SHIM}")
if(NOT EXISTS "${STATIC_SHIM}/libatomic_asneeded.a")
	execute_process(COMMAND "${AR_PROGRAM}" rc "${STATIC_SHIM}/libatomic_asneeded.a")
endif()

set(COMMON_ARGS
	"-DCMAKE_EXE_LINKER_FLAGS=-L${STATIC_SHIM}"
	-DCMAKE_BUILD_TYPE=${CMAKE_BUILD_TYPE}
	"-DCMAKE_C_FLAGS=${ARCH_FLAGS}"
	"-DCMAKE_CXX_FLAGS=${ARCH_FLAGS}"
	-DCMAKE_POLICY_VERSION_MINIMUM=3.5)

# CMake-based tools: build, then copy the executable to bin/.
foreach(tool acc zdbsp zt-bcc)
	ExternalProject_Add(${tool}
		SOURCE_DIR "${SRC}/${tool}"
		BINARY_DIR "${CMAKE_BINARY_DIR}/${tool}"
		CMAKE_ARGS ${COMMON_ARGS}
		INSTALL_COMMAND ${CMAKE_COMMAND} -E make_directory "${OUT}"
			COMMAND ${CMAKE_COMMAND} -E copy "<BINARY_DIR>/${tool}" "${OUT}/${tool}"
		BUILD_ALWAYS OFF)
endforeach()

# BCC only has a Makefile; point its build directory and executable outside the source tree.
ExternalProject_Add(bcc
	SOURCE_DIR "${SRC}/bcc"
	BINARY_DIR "${CMAKE_BINARY_DIR}/bcc"
	CONFIGURE_COMMAND ""
	BUILD_COMMAND make -s -C "${SRC}/bcc" "BUILD_DIR=${CMAKE_BINARY_DIR}/bcc/build" "EXE=${CMAKE_BINARY_DIR}/bcc/bcc" "CC=cc ${ARCH_FLAGS}"
	INSTALL_COMMAND ${CMAKE_COMMAND} -E make_directory "${OUT}"
		COMMAND ${CMAKE_COMMAND} -E copy "${CMAKE_BINARY_DIR}/bcc/bcc" "${OUT}/bcc"
	BUILD_ALWAYS OFF)
