#!/usr/bin/env python3
"""Generates FormResources/<Name>.resx (tools/resxgen/formresx.py) for every upstream designer file the
Builder project compiles that loads values through ComponentResourceManager, and registers it as an
embedded resource in ULDB.Builder.csproj. Run from the repository root after adding a model-ported
form or control; it skips the ones that already have a .resx."""

import os
import re
import subprocess

CSPROJ = 'Source/ULDB/Builder/ULDB.Builder.csproj'


def main():
    s = open(CSPROJ, newline='').read()
    nl = '\r\n' if '\r\n' in s else '\n'
    designers = re.findall(r'Compile Include="\$\(UpstreamCore\)([^";$]+\.Designer\.cs)"', s)
    have = set(re.findall(r'FormResources/(\w+)\.resx', s))
    add = []
    for d in designers:
        path = 'Source/Core/' + d
        if not os.path.exists(path):
            continue
        m = re.search(r'ComponentResourceManager\(typeof\((\w+)\)\)', open(path, encoding='utf-8-sig').read())
        if not m or m.group(1) in have:
            continue
        name = m.group(1)
        ns = 'CodeImp.DoomBuilder.' + d.split('/')[0]
        out = 'Source/ULDB/Builder/FormResources/%s.resx' % name
        r = subprocess.run(['python3', 'tools/resxgen/formresx.py', path, path.replace('.Designer.cs', '.resx'), out], capture_output=True, text=True)
        print(name, r.stdout.strip(), r.stderr.strip()[:100])
        add.append('    <EmbeddedResource Update="FormResources/%s.resx" LogicalName="%s.%s.resources" />' % (name, ns, name))
    if add:
        marker = '    <EmbeddedResource Update="FormResources/SectorEditFormUDMF.resx"'
        s = s.replace(marker, nl.join(add) + nl + marker)
        open(CSPROJ, 'w', newline='').write(s)


main()
