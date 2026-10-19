#!/usr/bin/env python3
"""Generate the GZDoom/UZDoom UDMF test maps used by ULDB's load/save tests.

The maps are generated rather than hand-made so they're reproducible and
contain no copyrighted IWAD data. Textures are named after Doom 2 / Freedoom
textures, so they display properly when an IWAD is loaded as a resource.

Usage: make_testmaps.py [--acc PATH_TO_ACC] [OUTPUT_DIR]
Default output directory: tests/maps (relative to the repository root).
"""

import argparse
import io
import os
import struct
import subprocess
import sys
import tempfile
import zipfile

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DEFAULT_ACC = os.path.join(REPO_ROOT, "Assets", "Linux", "Compilers", "ZDoom", "acc")

PLAYER1_START = 1
DOOM_IMP = 3001
BRIGHT_LIGHT = 9800  # GZDoom PointLight


class MapBuilder:
    """Builds UDMF geometry from closed polygons. Edges shared by two
    polygons become one two-sided linedef."""

    def __init__(self):
        self.vertices = []      # list of dicts: x, y, plus optional extra fields
        self.vertex_index = {}  # (x, y) -> index
        self.sectors = []       # list of dicts
        self.edges = {}         # (v1, v2) -> {"sector": s, "line": dict}
        self.things = []
        self.line_extra = {}    # (v1, v2) undirected key -> extra linedef fields

    def vertex(self, x, y, **extra):
        key = (x, y)
        if key not in self.vertex_index:
            self.vertex_index[key] = len(self.vertices)
            self.vertices.append({"x": float(x), "y": float(y)})
        index = self.vertex_index[key]
        self.vertices[index].update(extra)
        return index

    def sector(self, points, floor=0, ceiling=128, light=192,
               floortex="FLOOR4_8", ceiltex="CEIL3_5", walltex="STARTAN2", **extra):
        """Add a sector from a clockwise list of (x, y) points."""
        s = len(self.sectors)
        self.sectors.append(dict(heightfloor=floor, heightceiling=ceiling, lightlevel=light,
                                 texturefloor=floortex, textureceiling=ceiltex, **extra))
        indices = [self.vertex(x, y) for x, y in points]
        for i, a in enumerate(indices):
            b = indices[(i + 1) % len(indices)]
            self.edges[(a, b)] = {"sector": s, "texture": walltex}
        return s

    def rect(self, x1, y1, x2, y2, **kw):
        # Clockwise in Doom's coordinate system (y up): top-left, top-right, bottom-right, bottom-left.
        return self.sector([(x1, y2), (x2, y2), (x2, y1), (x1, y1)], **kw)

    def rect_inside(self, outer, x1, y1, x2, y2, **kw):
        """Add a rectangular sector inside the existing sector `outer` (a hole in it)."""
        inner = self.rect(x1, y1, x2, y2, **kw)
        points = [(x1, y2), (x2, y2), (x2, y1), (x1, y1)]
        indices = [self.vertex_index[p] for p in points]
        walltex = kw.get("walltex", "STARTAN2")
        for i, a in enumerate(indices):
            b = indices[(i + 1) % len(indices)]
            self.edges[(b, a)] = {"sector": outer, "texture": walltex}
        return inner

    def check(self):
        """Fail on T-junctions: a vertex lying inside another line's span."""
        for (a, b) in self.edges:
            ax, ay = self.vertices[a]["x"], self.vertices[a]["y"]
            bx, by = self.vertices[b]["x"], self.vertices[b]["y"]
            for i, v in enumerate(self.vertices):
                if i in (a, b):
                    continue
                vx, vy = v["x"], v["y"]
                cross = (bx - ax) * (vy - ay) - (by - ay) * (vx - ax)
                if cross == 0 and min(ax, bx) <= vx <= max(ax, bx) and min(ay, by) <= vy <= max(ay, by):
                    raise ValueError(f"T-junction: vertex {i} ({vx}, {vy}) lies on edge {a}-{b}")

    def thing(self, x, y, type, angle=0, **extra):
        self.things.append(dict(x=float(x), y=float(y), type=type, angle=angle,
                                skill1=True, skill2=True, skill3=True, skill4=True, skill5=True,
                                single=True, coop=True, dm=True, **extra))

    def set_line(self, p1, p2, **fields):
        """Attach extra fields (special, args, flags) to the line between two points."""
        a, b = self.vertex_index[p1], self.vertex_index[p2]
        self.line_extra[frozenset((a, b))] = fields

    def textmap(self):
        out = io.StringIO()
        out.write('namespace = "zdoom";\n\n')

        def block(kind, index, fields):
            out.write(f"{kind} // {index}\n{{\n")
            for key, value in fields.items():
                if isinstance(value, bool):
                    value = "true" if value else "false"
                elif isinstance(value, str):
                    value = '"' + value.replace('"', '\\"') + '"'
                elif isinstance(value, float):
                    value = f"{value:.3f}"
                out.write(f"{key} = {value};\n")
            out.write("}\n\n")

        for i, v in enumerate(self.vertices):
            block("vertex", i, v)

        sidedefs, linedefs, done = [], [], set()
        for (a, b), front in self.edges.items():
            key = frozenset((a, b))
            if key in done:
                continue
            done.add(key)
            back = self.edges.get((b, a))
            line = {"v1": a, "v2": b, "sidefront": len(sidedefs)}
            side = {"sector": front["sector"]}
            if back is None:
                side["texturemiddle"] = front["texture"]
                line["blocking"] = True
            else:
                side["texturetop"] = front["texture"]
                side["texturebottom"] = front["texture"]
            sidedefs.append(side)
            if back is not None:
                line["sideback"] = len(sidedefs)
                line["twosided"] = True
                sidedefs.append({"sector": back["sector"],
                                 "texturetop": back["texture"],
                                 "texturebottom": back["texture"]})
            line.update(self.line_extra.get(key, {}))
            linedefs.append(line)

        for i, l in enumerate(linedefs):
            block("linedef", i, l)
        for i, s in enumerate(sidedefs):
            block("sidedef", i, s)
        for i, s in enumerate(self.sectors):
            block("sector", i, s)
        for i, t in enumerate(self.things):
            block("thing", i, t)
        self.check()
        return out.getvalue().encode("ascii")


def write_wad(path, lumps):
    """lumps: list of (name, bytes)."""
    data = io.BytesIO()
    directory = []
    offset = 12
    for name, content in lumps:
        directory.append((offset, len(content), name))
        data.write(content)
        offset += len(content)
    with open(path, "wb") as f:
        f.write(b"PWAD" + struct.pack("<ii", len(lumps), offset))
        f.write(data.getvalue())
        for lump_offset, size, name in directory:
            f.write(struct.pack("<ii", lump_offset, size) + name.encode("ascii").ljust(8, b"\0"))


def map_lumps(name, builder, behavior=b"", scripts=None):
    lumps = [(name, b""), ("TEXTMAP", builder.textmap())]
    if behavior:
        lumps.append(("BEHAVIOR", behavior))
    if scripts is not None:
        lumps.append(("SCRIPTS", scripts))
    lumps.append(("ENDMAP", b""))
    return lumps


def compile_acs(acc, source):
    """Compile ACS source with acc. Returns BEHAVIOR bytes, or None if acc isn't available."""
    if not acc or not os.path.isfile(acc):
        return None
    include_dir = os.path.dirname(acc)
    with tempfile.TemporaryDirectory() as tmp:
        src = os.path.join(tmp, "test.acs")
        out = os.path.join(tmp, "test.o")
        with open(src, "wb") as f:
            f.write(source)
        result = subprocess.run([acc, "-i", include_dir, src, out], capture_output=True, text=True)
        if result.returncode != 0 or not os.path.isfile(out):
            sys.exit(f"acc failed:\n{result.stdout}\n{result.stderr}")
        with open(out, "rb") as f:
            return f.read()


# --- Individual maps ---------------------------------------------------------

def small_map():
    m = MapBuilder()
    # The room's right wall includes the corridor's corner points, so the shared edge is one line.
    m.sector([(0, 512), (512, 512), (512, 320), (512, 192), (512, 0), (0, 0)])
    m.rect(512, 192, 768, 320, floor=16, ceiling=112, light=160)  # step-up corridor
    m.thing(128, 256, PLAYER1_START)
    m.thing(640, 256, DOOM_IMP, angle=180)
    return m


def large_map(rooms_per_side=50, size=128):
    """A grid of rooms with varying heights: about 2,500 sectors and 5,000 lines."""
    m = MapBuilder()
    for gx in range(rooms_per_side):
        for gy in range(rooms_per_side):
            x, y = gx * size, gy * size
            m.rect(x, y, x + size, y + size,
                   floor=((gx + gy) % 4) * 8, ceiling=160 + (gx % 3) * 16,
                   light=128 + ((gx * 7 + gy * 3) % 8) * 16)
    m.thing(64, 64, PLAYER1_START)
    for i in range(0, rooms_per_side, 5):
        m.thing(i * size + 64, (rooms_per_side - 1) * size + 64, DOOM_IMP)
    return m


ACS_SOURCE = b"""#include "zcommon.acs"

// Test script for ULDB: exercises includes, script types, specials and strings.
script 1 OPEN
{
    Print(s:"ULDB ACS test map loaded");
}

script 2 (int tag)
{
    Floor_LowerByValue(tag, 16, 64);
    Delay(35);
    Floor_RaiseByValue(tag, 16, 64);
}
"""


def acs_map():
    m = MapBuilder()
    room = m.rect(0, 0, 512, 512)
    m.rect_inside(room, 192, 192, 320, 320, floor=0, light=224, floortex="FLAT14", id=1)
    m.set_line((0, 512), (512, 512), special=80, arg0=2, arg1=0, arg2=1, playeruse=True, repeatspecial=True)  # ACS_Execute 2 with tag 1
    m.thing(64, 64, PLAYER1_START)
    return m


BCS_SOURCE = b"""#include "zcommon.h.bcs"

// Test script for ULDB: BCS (ZT-BCC) syntax.
script 1 open {
    Print(s: "ULDB BCS test map loaded");
}

script 2 (int tag) {
    for (int i = 0; i < 2; ++i) {
        Floor_LowerByValue(tag, 16, 32);
        Delay(35);
        Floor_RaiseByValue(tag, 16, 32);
    }
}
"""


def bcs_map():
    m = acs_map()
    return m


def floors3d_slopes_map():
    m = MapBuilder()
    # Main room. Its top wall includes the corner points of the ramp and the triangle above it.
    room = m.sector([(0, 1024), (256, 1024), (512, 1024), (768, 1024), (1024, 1024), (1024, 0), (0, 0)],
                    ceiling=256)
    # Inner sector that receives a solid 3D floor (tag 10).
    m.rect_inside(room, 384, 384, 640, 640, ceiling=256, id=10)
    # Control sector for the 3D floor, outside the playable area.
    m.rect(1280, 0, 1408, 128, floor=64, ceiling=80, floortex="FLAT5_4", ceiltex="FLAT5_4", walltex="METAL")
    m.set_line((1280, 128), (1408, 128), special=160, arg0=10, arg1=1, arg2=0, arg3=255)  # Sector_Set3dFloor
    # Slope by line special: a ramp sector whose floor is aligned to its neighbour.
    m.rect(0, 1024, 256, 1280, floor=64, ceiling=256)
    m.set_line((0, 1024), (256, 1024), special=181, arg0=2, arg1=0)  # Plane_Align (back floor)
    # Vertex-height slope: a triangle with per-vertex floor heights.
    m.sector([(512, 1280), (768, 1024), (512, 1024)], floor=0, ceiling=256)
    m.vertex(512, 1280, zfloor=48.0)
    m.vertex(768, 1024, zfloor=0.0)
    m.vertex(512, 1024, zfloor=0.0)
    m.thing(128, 128, PLAYER1_START)
    m.thing(512, 512, BRIGHT_LIGHT, arg0=255, arg1=200, arg2=150, arg3=160)
    return m


PK3_FILES = {
    "MAPINFO.txt": b"""map MAP01 "ULDB PK3 test"
{
    levelnum = 1
    next = "MAP01"
    sky1 = "SKY1"
}

// A DoomEdNum for the ZScript actor, so it shows up in the editor's thing list.
DoomEdNums
{
    30000 = ULDBTestLamp
}
""",
    "GLDEFS.txt": b"""pointlight ULDBTESTLIGHT
{
    color 1.0 0.6 0.2
    size 96
}

object ULDBTestLamp
{
    frame TLMP { light ULDBTESTLIGHT }
}
""",
    "zscript.zs": b"""version "4.10"

// Real projects load GZDoom's/UZDoom's base resource (gzdoom.pk3 / uzdoom.pk3), which
// defines the actor hierarchy. This test PK3 declares minimal stand-ins so it's self-contained.
class Object native {}
class Thinker : Object native {}
class Actor : Thinker native {}

class ULDBTestLamp : Actor
{
    Default
    {
        //$Title "ULDB test lamp"
        //$Category "ULDB tests"
        Radius 16;
        Height 48;
        +SOLID
    }
    States
    {
    Spawn:
        TLMP A -1 Bright;
        Stop;
    }
}
""",
}


def pk3_resource(path, lumps):
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as z:
        for name, content in PK3_FILES.items():
            z.writestr(name, content)
        tmp = path + ".map.wad"
        write_wad(tmp, lumps)
        with open(tmp, "rb") as f:
            z.writestr("maps/MAP01.wad", f.read())
        os.remove(tmp)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("output", nargs="?", default=os.path.join(REPO_ROOT, "tests", "maps"))
    parser.add_argument("--acc", default=DEFAULT_ACC, help="path to ZDoom acc (default: the bundled Linux build)")
    args = parser.parse_args()
    os.makedirs(args.output, exist_ok=True)

    def out(name):
        return os.path.join(args.output, name)

    write_wad(out("small.wad"), map_lumps("MAP01", small_map()))
    write_wad(out("large.wad"), map_lumps("MAP01", large_map()))

    behavior = compile_acs(args.acc, ACS_SOURCE)
    if behavior is None:
        print("warning: acc not found, acs.wad gets source only", file=sys.stderr)
    write_wad(out("acs.wad"), map_lumps("MAP01", acs_map(), behavior or b"", ACS_SOURCE))

    # BCS compilers aren't built for Linux until Phase 2b, so this map carries source only.
    # UDB compiles it on save once ZT-BCC is configured.
    write_wad(out("bcs.wad"), map_lumps("MAP01", bcs_map(), b"", BCS_SOURCE))

    write_wad(out("3dfloors_slopes.wad"), map_lumps("MAP01", floors3d_slopes_map()))

    m = small_map()
    m.thing(384, 384, 30000)
    pk3_resource(out("pk3_resource.pk3"), map_lumps("MAP01", m))

    for name in sorted(os.listdir(args.output)):
        print(f"{name:24} {os.path.getsize(out(name)):>9} bytes")


if __name__ == "__main__":
    main()
