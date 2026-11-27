# Contract: `dmc campaign compare`

```
dmc campaign compare NAME [--with FILE] [--all]
```

| Case | Output | Exit |
|---|---|---|
| Every file matches | `NAME matches <file>: <n> files` | 0 |
| Differences | up to 40 of `changed: <path>`, `added: <path>`, `missing: <path>` (ordinal path order), then `<n> more`; stderr `dmc campaign: <n> files differ from <file>` (`1 file differs`) | 1 |
| The export is of another campaign | first line `note: <file> is an export of <campaign>`, then as above | 0 or 1 |
| Open campaign, no backup, no `--with` | `dmc campaign: NAME has no backup: name an export with --with FILE` | 1 |
| Closed campaign, mark unreadable, no `--with` | `dmc campaign: adventures/NAME/.closed is unreadable: name the closing backup with --with` | 1 |
| Closed campaign, the mark's file in neither `backups/` nor `exports/` | `dmc campaign: no backup <file> in backups/ or exports/: name it with --with` | 1 |
| `--with` names no file | `dmc campaign: no backup <given>: dmc campaign backups NAME lists them` | 1 |
| The export is damaged | the export check's line (`<path> is damaged: ...`) | 1 |

`<file>` is the export's file name. `<n> files` counts the campaign's files now, the map page's derived files left out.
Nothing in `adventures/` or `exports/` is written, pass or fail; an interrupted restore is put back first and its
`recovered NAME from an interrupted restore` line printed, as for `verify`.

`verify` on a closed campaign with no `--backup` finds the closing backup the same way: `backups/`, then `exports/`,
with `--backup` in its messages. On a closed campaign, a bare file name given to `--with` or `--backup` is looked for
in `backups/`, then `exports/`, before it is read as a path.
