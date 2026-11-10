# Contract: `dmc campaign` verbs added by this feature

The existing verbs `export`, `import` and `show` keep their contract (`specs/001-campaign-import-export/contracts/`)
except where this file says so. Conventions of the existing command apply: arguments are read as Python's `argparse`
reads them; a mistake is one line `dmc campaign: <message>` and exit code 2; any other failure is one line and exit
code 1; success prints few lines, because everything printed lands in DMC's context (Constitution, Build and Output
Constraints). `NAME` is a folder of `adventures/` whose name does not start with a dot. `--help` lists the verbs.

## `dmc campaign standard [NAME] [--init]`

Without `NAME`, the active campaign (`adventures/.active-campaign`). Reads `.standard` and the DMC folder's
`migrations/`. Changes nothing, except `--init`.

| Output (one line) | Meaning | Exit |
|---|---|---|
| `current` | campaign standard equals the kit's | 0 |
| `behind: standard C, kit K: 002-slug.md, 003-slug.md` | the pending migrations, oldest first | 0 |
| `ahead: standard C, kit K: update dmc` | campaign is newer than the kit | 0 |
| `initialised NAME at standard K` | `--init`, and there was no `.standard` | 0 |
| error | `.standard` unreadable; `--init` on a campaign that already has one; no such campaign | 1 |

## `dmc campaign backups NAME`

Lists the campaign's backups, oldest first, one per line (file name only), after a first line `N backups`
(`0 backups` and nothing else when there are none). Counts only names that match the backup rule (data-model.md).
Exit 0.

## `dmc campaign backup NAME [--prune oldest|old]`

1. Writes `adventures/NAME/backups/NAME_backup_<yyyyMMddHHmmss>.zip` with the exporter (R8), to a `.partial-` file
   renamed into place; never replaces a file.
2. Checks the written archive whole (manifest, sizes, SHA-256 of every file).
3. Only then, if `--prune` was given, removes: `oldest` the single oldest backup counted before this one; `old` every
   counted backup before this one except the latest two. Each removed backup's `.accounting.tsv` goes with it.

Prints one line: `backed up NAME: <n> files to backups/<file>` and, with `--prune`, `; removed <k> older`.
Exit 0. A failure at step 1 or 2 removes the partial file, removes nothing, and exits 1 with one line.
`--prune` with nothing to remove is not an error. `dmc` itself never decides to prune: the skill passes `--prune`
only after the player said yes (FR-005a).

## `dmc campaign verify NAME [--backup FILE] [--accounting FILE] [--all]`

`--backup` defaults to the latest backup of NAME; `--accounting` to `<backup>.accounting.tsv` (absent counts as
empty). Changes nothing on a failure. Steps and rules are research R5.

- **Pass:** writes `.standard` (the kit's standard), prints `verified NAME: <u> units, <m> matched, <a> accounted,
  <n> dropped; standard K` and one `size <file> <lines>/<target>` line per file that has a target, with `over` after a
  shortfall. Exit 0.
- **Fail:** prints `unaccounted: <id> <file> <first 60 chars>` for the first 40 unaccounted units (all with `--all`),
  then `N more` if cut, then one line per failed check, then `dmc campaign: verification failed: <reason>`. Exit 1.
  `.standard` is not written.
- **Cannot verify:** the backup is missing, damaged or from another campaign; no pending migration is known; the
  accounting file has a bad line. One line, exit 1. A bad accounting line names its line number.

Pending migrations are those with a number above the standard in the backup's `.standard` (1 if none) up to the kit's.

## `dmc campaign restore NAME [--from FILE] [--yes]`

`--from` defaults to the latest backup of NAME. Without `--yes` (a plan, nothing changes):

```
restore NAME from <file>: <r> files replaced, <d> removed, <a> added
warning: NAME has changed since the backup (<c> files newer or different): those changes will be lost
confirm with --yes
```

(the warning line only when it applies; exit 0). With `--yes`, checks the backup whole, then replaces the campaign
as research R9 describes, keeping `backups/`. Prints `restored NAME from <file>: <n> files`. Exit 0. A failure
leaves the campaign as it was, with one line, exit 1. `--from` a file that is not a backup of NAME (the manifest's
campaign differs) is an error. Recovery of an interrupted earlier restore runs first and is reported in one line.

## Changed behaviour of existing verbs

- **`export`**: leaves the campaign's top-level `backups/` folder out of the archive and its manifest. `--out`
  inside `adventures/` is still refused, except by `backup`, which passes the backups folder of the campaign being
  exported to the exporter directly (not through `--out`), so a player cannot choose an output path in `adventures/`.
- **`import`**: refuses an archive that has a `backups/` entry (`<file> is damaged: ...`). Still never changes an
  existing campaign.
- **`show`**: unchanged.
- **`dmc update` / `dmc refresh`**: after the refresh, one extra line when any campaign of a refreshed DMC folder is
  behind: `n campaigns are behind the new rules: ask DMC to bring them up to date`. Nothing when none is.
