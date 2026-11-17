#!/bin/bash
# Builds the Flatpak from the committed state of this checkout and installs it for the current user,
# without making a release (the same steps as .github/workflows/uldb-flatpak.yml).
#
#   tools/flatpak/local-install.sh        # from anywhere inside the checkout
#   flatpak run --user io.github.theoriginalzads.UltimateLinuxDoomBuilder
#
# Needs (user installation is enough): org.freedesktop.Platform//26.08, org.freedesktop.Sdk//26.08,
# org.freedesktop.Sdk.Extension.dotnet10//26.08 and org.flatpak.Builder. From VS Code's Flatpak, run it
# through `flatpak-spawn --host bash tools/flatpak/local-install.sh`.
#
# It builds a clean worktree of HEAD in ~/.cache/uldb-flatpak-src: building the working folder itself fails
# when a host `dotnet build` has left obj/ folders behind (their CMake caches name host paths).
# Uncommitted changes are not included.
set -e
REPO=$(git -C "$(dirname "$0")" rev-parse --show-toplevel)
W=${ULDB_FLATPAK_SRC:-$HOME/.cache/uldb-flatpak-src}

git -C "$REPO" worktree remove --force "$W" 2>/dev/null || rm -rf "$W"
git -C "$REPO" worktree add --detach "$W" HEAD
cd "$W"
git submodule update --init --recursive
git log --oneline -1

echo "== restore NuGet packages in the SDK sandbox"
mkdir -p .restore-home nuget-restore
chmod -R a+rwX .restore-home nuget-restore
flatpak run --command=sh --runtime=org.freedesktop.Sdk//26.08 --share=network --filesystem=host \
  --env=DOTNET_CLI_TELEMETRY_OPTOUT=1 --env=DOTNET_NOLOGO=1 \
  --env=HOME="$PWD/.restore-home" --env=DOTNET_CLI_HOME="$PWD/.restore-home" \
  org.freedesktop.Sdk.Extension.dotnet10//26.08 \
  -c 'export PATH="$PATH:/usr/lib/sdk/dotnet10/bin" LD_LIBRARY_PATH="/usr/lib/sdk/dotnet10/lib"; for p in Source/ULDB/Builder/ULDB.Builder.csproj Source/ULDB/Plugins/*/*.csproj; do dotnet restore "$p" -r linux-x64 --packages "$PWD/nuget-restore" || exit 1; done'
python3 tools/flatpak/nuget-sources.py nuget-restore packaging/flatpak/nuget-sources.json
rm -rf nuget-restore .restore-home

echo "== flatpak-builder"
flatpak run org.flatpak.Builder --user --install --force-clean --disable-rofiles-fuse build-dir packaging/flatpak/io.github.theoriginalzads.UltimateLinuxDoomBuilder.yml
echo "== DONE"
