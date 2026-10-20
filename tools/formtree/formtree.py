#!/usr/bin/env python3
"""Print a WinForms designer file's control tree (names, types, texts, positions, events).

Used when rebuilding an upstream designer layout as .axaml, to see every control
(fields and designer locals), its parent, text and location, in the designer's order.

Usage: formtree.py DESIGNER_CS
"""
import re
import sys

s = open(sys.argv[1], encoding="utf-8-sig").read()
ref = r"(?:this\.)?(\w+)"
types = dict(re.findall(ref + r" = new ([\w.]+)\(", s))
texts = dict(re.findall(ref + r"\.Text = \"(.*?)\";", s))
locs = {n: (int(x), int(y)) for n, x, y in re.findall(ref + r"\.Location = new System\.Drawing\.Point\((-?\d+), (-?\d+)\)", s)}
events = {}
for n, ev, h in re.findall(ref + r"\.(\w+) \+= new [\w.]+\(this\.(\w+)\)", s):
    events.setdefault(n, []).append(f"{ev}->{h}")
parent = {}
order = []
for p, c in re.findall(ref + r"\.Controls\.Add\(" + ref + r"\);", s):
    p = "(root)" if p == "Controls" else p
    parent[c] = p
    order.append(c)
for c in re.findall(r"^\s*this\.Controls\.Add\(this\.(\w+)\);", s, re.M):
    parent[c] = "(root)"


def tree(p, ind):
    kids = [c for c in order if parent.get(c) == p]
    kids = list(dict.fromkeys(kids))
    kids.sort(key=lambda c: (locs.get(c, (0, 0))[1], locs.get(c, (0, 0))[0]))
    for c in kids:
        t = types.get(c, "?").split(".")[-1]
        loc = locs.get(c, "")
        ev = " " + ",".join(events[c]) if c in events else ""
        print("  " * ind + f"{c} [{t}] {texts.get(c, '')!r} {loc}{ev}")
        tree(c, ind + 1)


tree("(root)", 0)
if "(root)" in events:
    print("form events:", events["(root)"])
