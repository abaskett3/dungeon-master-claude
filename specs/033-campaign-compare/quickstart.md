# Quickstart: `dmc campaign compare` (spec 033)

```
dmc campaign backup rorn
dmc campaign compare rorn                       # rorn matches rorn_backup_<stamp>.zip: 4 files
# edit story/canon.md
dmc campaign compare rorn                       # changed: story/canon.md; exit 1, "1 file differs from ..."
dmc campaign export rorn
dmc campaign import exports/rorn-<stamp>.zip --as rorn-copy
dmc campaign compare rorn-copy --with exports/rorn-<stamp>.zip   # note: ... is an export of rorn; rorn-copy matches ...
```

A closed campaign: `dmc campaign compare NAME` and `dmc campaign verify NAME` use its closing backup, from `backups/`
or, for an imported copy, from `exports/`.
