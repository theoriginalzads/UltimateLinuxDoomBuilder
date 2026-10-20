#!/usr/bin/env python3
"""Write flatpak-builder sources for every NuGet package in a restored packages folder.

flatpak-builder has no network while building, so the Flatpak build restores from a folder of
.nupkg files that flatpak-builder downloads beforehand. Restore first (with the same .NET SDK the
Flatpak build uses, so implicit package versions match), then run this on the packages folder:

  dotnet restore Builder.Linux.sln -r linux-x64 --packages PKGDIR
  tools/flatpak/nuget-sources.py PKGDIR packaging/flatpak/nuget-sources.json

Same output format as flatpak-builder-tools' flatpak-dotnet-generator.py.
"""

import base64
import binascii
import json
import sys
from pathlib import Path


def main():
    pkgdir, out = Path(sys.argv[1]), sys.argv[2]
    sources = []
    for sha in sorted(pkgdir.glob("*/*/*.nupkg.sha512")):
        name, version = sha.parent.parent.name, sha.parent.name
        filename = f"{name}.{version}.nupkg"
        sources.append({
            "type": "file",
            "url": f"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{filename}",
            "sha512": binascii.hexlify(base64.b64decode(sha.read_text().strip())).decode("ascii"),
            "dest": "nuget-sources",
            "dest-filename": filename,
        })
    if not sources:
        sys.exit(f"nuget-sources.py: no packages in {pkgdir}")
    with open(out, "w") as f:
        json.dump(sources, f, indent=2)
    print(f"{len(sources)} packages")


if __name__ == "__main__":
    main()
