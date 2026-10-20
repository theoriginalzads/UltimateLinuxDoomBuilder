#!/usr/bin/env python3
"""Regenerate ULDB's UI placeholders and prune members that don't compile.

Runs uistubgen, builds the Builder project, and adds every placeholder member
(or using directive) that causes a compile error to the prune list. Repeats
until the placeholders compile. Prints the errors left in upstream files, which
are the real porting work.

Usage: tools/uistubgen/prune.py [TARGET...]   (default: every target; see TARGETS)
"""

import os
import re
import subprocess
import sys

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

# Each target: ULDB's project, the upstream source it compiles, upstream's Mono project
# (whose compile list is authoritative) and the upstream folders (or files) holding WinForms UI.
TARGETS = {
    "Builder": dict(dir="Source/ULDB/Builder", project="ULDB.Builder.csproj",
                    upstream="Source/Core", mono="BuilderMono.csproj", ui=["Windows", "Controls"]),
    "BuilderModes": dict(dir="Source/ULDB/Plugins/BuilderModes", project="ULDB.BuilderModes.csproj",
                         upstream="Source/Plugins/BuilderModes", mono="BuilderModesMono.csproj", ui=["Interface"]),
    "ThreeDFloorMode": dict(dir="Source/ULDB/Plugins/ThreeDFloorMode", project="ULDB.ThreeDFloorMode.csproj",
               upstream="Source/Plugins/3DFloorMode", mono="ThreeDFloorModeMono.csproj", ui=["Controls", "Interface", "Windows"]),
    "BuilderEffects": dict(dir="Source/ULDB/Plugins/BuilderEffects", project="ULDB.BuilderEffects.csproj",
               upstream="Source/Plugins/BuilderEffects", mono="BuilderEffectsMono.csproj", ui=["Controls", "Interface"]),
    "StairSectorBuilder": dict(dir="Source/ULDB/Plugins/StairSectorBuilder", project="ULDB.StairSectorBuilder.csproj",
               upstream="Source/Plugins/StairSectorBuilder", mono="StairSectorBuilderMono.csproj", ui=["StairSectorBuilderForm.cs"]),
    "TagRange": dict(dir="Source/ULDB/Plugins/TagRange", project="ULDB.TagRange.csproj",
               upstream="Source/Plugins/TagRange", mono="TagRangeMono.csproj", ui=["TagRangeForm.cs", "ToolsForm.cs"]),
    "SoundPropagationMode": dict(dir="Source/ULDB/Plugins/SoundPropagationMode", project="ULDB.SoundPropagationMode.csproj",
               upstream="Source/Plugins/SoundPropagationMode", mono="SoundPropagationMono.csproj", ui=["Interface", "Windows"]),
    "ColorPicker": dict(dir="Source/ULDB/Plugins/ColorPicker", project="ULDB.ColorPicker.csproj",
               upstream="Source/Plugins/ColorPicker", mono="ColorPickerMono.csproj", ui=["Controls", "Windows"]),
    "TagExplorer": dict(dir="Source/ULDB/Plugins/TagExplorer", project="ULDB.TagExplorer.csproj",
               upstream="Source/Plugins/TagExplorer", mono="TagExplorerMono.csproj", ui=["Controls"]),
    "CommentsPanel": dict(dir="Source/ULDB/Plugins/CommentsPanel", project="ULDB.CommentsPanel.csproj",
               upstream="Source/Plugins/CommentsPanel", mono="CommentsPanelMono.csproj", ui=["CommentsDocker.cs"]),
}

STUBS = PRUNE = PORTED = MANUAL = PROJECT = UPSTREAM = MONO = None
UI_DIRS = []


def select(name):
    global STUBS, PRUNE, PORTED, MANUAL, PROJECT, UPSTREAM, MONO, UI_DIRS
    t = TARGETS[name]
    d = os.path.join(REPO, t["dir"])
    STUBS = os.path.join(d, "UIStubs")
    PRUNE = os.path.join(d, "uistubs-prune.txt")
    PORTED = os.path.join(d, "uistubs-ported.txt")
    MANUAL = os.path.join(d, "uistubs-manual.txt")
    PROJECT = os.path.join(d, t["project"])
    UPSTREAM = os.path.join(REPO, t["upstream"])
    MONO = t["mono"]
    UI_DIRS = [os.path.join(UPSTREAM, u) for u in t["ui"]]
    for f in (PORTED, MANUAL):
        if not os.path.exists(f):
            open(f, "w").close()


ERROR = re.compile(r"^(?P<file>/[^(]+)\((?P<line>\d+),\d+\): error (?P<code>CS\d+): (?P<msg>.*?)(?: \[.*\])?$")


def compiled_files():
    """Files upstream's Linux (Mono) project actually compiles; some stale files on disk don't."""
    with open(os.path.join(UPSTREAM, MONO), encoding="utf-8-sig") as f:
        files = re.findall(r'<Compile Include="([^"]+)"', f.read())
    path = os.path.join(os.path.dirname(PROJECT), "obj", "uistubgen-files.txt")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w") as f:
        f.write("\n".join(x.replace("\\", "/") for x in files))
    return path


def generate():
    subprocess.run(["dotnet", "run", "--project", os.path.join(REPO, "tools", "uistubgen"), "--",
                    "--out", STUBS, "--prune", PRUNE, "--ported", PORTED, "--manual", MANUAL, "--root", UPSTREAM,
                    "--winforms-types", os.path.join(REPO, "tools", "uistubgen", "winforms-types.txt"),
                    "--files-from", compiled_files()] + UI_DIRS,
                   check=True, stdout=subprocess.DEVNULL)


def build():
    result = subprocess.run(["dotnet", "build", PROJECT, "-nologo", "-v", "q", "-clp:NoSummary"],
                            capture_output=True, text=True)
    errors = set()
    for line in result.stdout.splitlines():
        m = ERROR.match(line.strip())
        if m:
            errors.add((m["file"], int(m["line"]), m["code"], m["msg"]))
    return sorted(errors)


def key_for(path, line):
    with open(path, encoding="utf-8") as f:
        lines = f.read().splitlines()
    for i in range(min(line, len(lines)) - 1, -1, -1):
        if "// stub-key: " in lines[i]:
            return lines[i].split("// stub-key: ", 1)[1].strip()
    return None


def write_prune(prune):
    with open(PRUNE, "w") as f:
        f.write("# Placeholder members/usings that can't compile until the real Avalonia port exists.\n")
        f.write("# Generated by tools/uistubgen/prune.py; don't edit by hand.\n")
        for k in sorted(prune):
            f.write(k + "\n")


def main():
    names = sys.argv[1:] or list(TARGETS)
    status = 0
    for name in names:
        print(f"== {name}")
        select(name)
        status |= run()
    return status


def run():
    # Start from scratch each run, so entries that compile again (after ports or
    # compat additions) drop off the list automatically.
    prune = set()
    write_prune(prune)
    for round in range(1, 30):
        generate()
        errors = build()
        stub_errors = [e for e in errors if e[0].startswith(STUBS)]
        new = set()
        for path, line, code, msg in stub_errors:
            key = key_for(path, line)
            if key and key not in prune:
                new.add(key)
        print(f"round {round}: {len(errors)} errors ({len(stub_errors)} in placeholders), pruning {len(new)}")
        if not new:
            break
        prune |= new
        write_prune(prune)
    rest = [e for e in errors if not e[0].startswith(STUBS)]
    for path, line, code, msg in rest:
        print(f"{os.path.relpath(path, REPO)}:{line}: {code}: {msg}")
    stub_left = [e for e in errors if e[0].startswith(STUBS)]
    for path, line, code, msg in stub_left:
        print(f"UNPRUNABLE {os.path.relpath(path, REPO)}:{line}: {code}: {msg}")
    return 0 if not errors else 1


if __name__ == "__main__":
    sys.exit(main())
