#!/usr/bin/env python3
"""Extract the icon specs from Source/ULDB/Builder/Shell/BreezeIcons.cs as "key<TAB>spec" lines.

Evaluates the C# string expressions the dictionary uses: literals, the path constants and C(x, y, r).
Usage: specs.py BREEZEICONS_CS OUT_TSV
"""

import re
import sys


def circle(m):
    x, y, r = (float(v) for v in m.groups())
    f = lambda v: "%g" % v
    return '"M%s,%s A%s,%s 0 1 0 %s,%s A%s,%s 0 1 0 %s,%s Z"' % (f(x - r), f(y), f(r), f(r), f(x + r), f(y), f(r), f(r), f(x - r), f(y))


def main():
    src, out = sys.argv[1:3]
    s = open(src, encoding="utf-8").read()
    consts = dict(re.findall(r'private const string (\w+) = "([^"]*)";', s))
    body = s[s.index("specs = new"):s.index("aliases = new")]
    lines = []
    for key, expr in re.findall(r'\["([^"]+)"\] = (.*?),\n', body):
        expr = re.sub(r"C\(([\d.]+), ([\d.]+), ([\d.]+)\)", circle, expr)
        for name, value in consts.items():
            expr = re.sub(r"\b%s\b" % name, '"%s"' % value, expr)
        lines.append(key + "\t" + "".join(re.findall(r'"([^"]*)"', expr)))
    with open(out, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print("%d icons" % len(lines))


if __name__ == "__main__":
    main()
