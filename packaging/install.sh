#!/bin/sh
# Installs a built Ultimate Linux Doom Builder tree (from packaging/build.sh) into
# $DESTDIR$PREFIX:
#
#   $PREFIX/lib/uldb/                    the program, its data files, plugins and compilers
#   $PREFIX/bin/uldb                     launcher
#   $PREFIX/share/applications/          .desktop file
#   $PREFIX/share/metainfo/              AppStream metadata
#   $PREFIX/share/icons/hicolor/         app icon at every available size, plus scalable SVG
#   $PREFIX/share/licenses/uldb/         LICENSE.txt (GPL-3.0)
#
# Usage: [PREFIX=/usr/local] [DESTDIR=] [APPDIR=<built tree>] packaging/install.sh
#
# APPDIR defaults to the tree packaging/build.sh makes for linux-x64.
set -eu

ROOT=$(cd "$(dirname "$0")/.." && pwd)
PREFIX=${PREFIX:-/usr/local}
DESTDIR=${DESTDIR:-}
APPDIR=${APPDIR:-$ROOT/Source/ULDB/Builder/bin/Release/net10.0/linux-x64/publish}
APP_ID=io.github.theoriginalzads.UltimateLinuxDoomBuilder

if [ ! -x "$APPDIR/Builder" ] || [ ! -f "$APPDIR/libBuilderNative.so" ]; then
	echo "install.sh: no built tree in $APPDIR. Run packaging/build.sh first, or set APPDIR." >&2
	exit 1
fi

LIBDIR="$DESTDIR$PREFIX/lib/uldb"
BINDIR="$DESTDIR$PREFIX/bin"
SHAREDIR="$DESTDIR$PREFIX/share"

# The program. Replace an older install instead of mixing files from two versions.
rm -rf "$LIBDIR"
mkdir -p "$LIBDIR"
cp -R "$APPDIR"/. "$LIBDIR"/
# Files are readable by everyone; the program, its launcher and the compilers stay executable.
chmod -R u=rwX,go=rX "$LIBDIR"

# Launcher. Not a symlink to lib/uldb/uldb, so it also works when bin/ and lib/ end up on
# different paths (for example in a Flatpak).
mkdir -p "$BINDIR"
cat > "$BINDIR/uldb" <<EOF
#!/bin/sh
# Ultimate Linux Doom Builder launcher (installed by packaging/install.sh).
exec "$PREFIX/lib/uldb/Builder" "\$@"
EOF
chmod 755 "$BINDIR/uldb"

# Desktop integration.
install -Dm644 "$ROOT/packaging/$APP_ID.desktop" "$SHAREDIR/applications/$APP_ID.desktop"
install -Dm644 "$ROOT/packaging/$APP_ID.metainfo.xml" "$SHAREDIR/metainfo/$APP_ID.metainfo.xml"

ICONS="$ROOT/Source/ULDB/Assets/Icons"
for png in "$ICONS"/uldb-[0-9]*.png; do
	size=$(basename "$png" .png)
	size=${size#uldb-}
	install -Dm644 "$png" "$SHAREDIR/icons/hicolor/${size}x${size}/apps/$APP_ID.png"
done
install -Dm644 "$ICONS/uldb.svg" "$SHAREDIR/icons/hicolor/scalable/apps/$APP_ID.svg"

install -Dm644 "$ROOT/LICENSE.txt" "$SHAREDIR/licenses/uldb/LICENSE.txt"

echo "Installed Ultimate Linux Doom Builder to $DESTDIR$PREFIX"
