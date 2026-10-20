# compatgen

Extracts public enums, with their exact values, from .NET Framework reference
assemblies. ULDB uses it to generate the enum part of its WinForms and
System.Drawing compatibility layer (`Source/ULDB/Compat`). Upstream code can
then compile unchanged, and stored values (for example keyboard shortcuts in
settings files) stay compatible with upstream UDB.

```sh
tools/compatgen/regen.sh    # needs Mono's reference assemblies (Arch/CachyOS: the mono package)
```

The enum lists are in `regen.sh`; add a name there when compat needs another enum.

