#!/bin/sh
# Builds a framework-dependent Release tree of Ultimate Linux Doom Builder, ready for
# packaging/install.sh. The tree needs the .NET 10 runtime on the target system.
#
# Usage: packaging/build.sh [extra dotnet arguments...]
#
# Environment:
#   ULDB_ARCH   CPU target for the native code (libBuilderNative.so and the compilers):
#               generic (default), x86-64-v3, x86-64-v4, znver4 or native.
#   ULDB_RID    .NET runtime identifier (default linux-x64).
#
# Extra arguments go to every dotnet command, for example `--source DIR` for an offline
# NuGet restore (Flatpak).
#
# Prints the path of the finished tree on the last line of output.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
ARCH=${ULDB_ARCH:-generic}
RID=${ULDB_RID:-linux-x64}

if [ ! -f "$ROOT/tools/compilers/src/acc/CMakeLists.txt" ]; then
	echo "build.sh: the compiler sources are missing. Run: git submodule update --init" >&2
	exit 1
fi

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# The plugins. Each one copies itself into Builder's bin/<Configuration>/net10.0/Plugins
# after it builds (see Source/ULDB/Plugins/*/*.csproj). This also builds Builder.
# Same runtime identifier as the publish below, so one NuGet restore covers both
# (this matters for the offline Flatpak restore).
for proj in "$ROOT"/Source/ULDB/Plugins/*/*.csproj; do
	dotnet build "$proj" -c Release -r "$RID" --self-contained false -p:UldbArch="$ARCH" "$@"
done

# The program: framework-dependent, one runtime identifier, so only that platform's
# native SkiaSharp and HarfBuzz libraries are included.
dotnet publish "$ROOT/Source/ULDB/Builder/ULDB.Builder.csproj" -c Release -r "$RID" \
	--self-contained false -p:UldbArch="$ARCH" "$@"

OUT="$ROOT/Source/ULDB/Builder/bin/Release/net10.0/$RID/publish"
PLUGINS="$ROOT/Source/ULDB/Builder/bin/Release/net10.0/Plugins"
mkdir -p "$OUT/Plugins"
for f in "$PLUGINS"/*.dll "$PLUGINS"/*.pdb; do
	[ -e "$f" ] && cp -f "$f" "$OUT/Plugins/"
done

if [ ! -x "$OUT/Builder" ] || [ ! -f "$OUT/libBuilderNative.so" ]; then
	echo "build.sh: the published tree in $OUT is incomplete" >&2
	exit 1
fi

echo "$OUT"
