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
| `unfinished: <backup file>` | a backup has a `.migrating` mark (research R8); takes precedence over the others | 0 |
| `initialised NAME at standard K` | `--init`, and there was no `.standard` | 0 |
| error | `.standard` unreadable; `--init` on a campaign that already has one; no such campaign | 1 |

## `dmc campaign backups NAME`

Lists the campaign's backups, oldest first (by the UTC time in each one's manifest, so a clock going back an hour does
not misorder them; by the local time in the name when the manifest cannot be read), one per line (file name only), after a first line `N backups`
(`0 backups` and nothing else when there are none). Counts only names that match the backup rule (data-model.md).
When a removal question is due (FR-005a), a last line holds it and the flag a yes allows:

| Counted backups | Last line |
|---|---|
| fewer than 3 | none |
| exactly 3 | `ask: may the oldest backup, <file>, be removed? yes: --prune oldest` |
| `n` more than 3 | `ask: may the <n-2> oldest backups be removed, keeping the latest two? yes: --prune old` |
| any, with an unfinished migration | none |

Exit 0. The skill relays an `ask:` line to the player and holds none of these numbers.

## `dmc campaign backup NAME [--prune oldest|old]`

0. Refuses, with one line and exit 1, while the campaign has an unfinished migration: `NAME has an unfinished
   migration from <file>: finish it or restore it`.
1. Writes `adventures/NAME/backups/NAME_backup_<yyyyMMddHHmmss>.zip` with the exporter (R8), to a `.partial-` file
   renamed into place; never replaces a file.
2. Checks the written archive whole (manifest, sizes, SHA-256 of every file), then, when a migration is pending (the
   campaign is behind the kit), writes `<file>.migrating` beside it. A backup of a campaign that is current or ahead
   gets no mark: `verify` would have nothing to verify, so a mark there could only be cleared by a restore.
3. Only then, if `--prune` was given, removes: `oldest` the single oldest backup counted before this one; `old` every
   counted backup before this one except the latest two. Each removed backup's companion files
   (`.accounting.tsv`, `.verified.tsv`, `.migrating`) go with it.

Prints one line: `backed up NAME: <n> files to backups/<file>` and, with `--prune`, `; removed <k> older`.
Exit 0. A failure at step 1 or 2 removes the partial file, removes nothing, and exits 1 with one line.
`--prune` with nothing to remove is not an error. `dmc` itself never decides to prune: the skill passes `--prune`
only after the player said yes to the `ask:` line of `backups` (FR-005a).

## `dmc campaign verify NAME [--backup FILE] [--accounting FILE] [--list] [--all]`

`--backup` defaults to the marked backup of an unfinished migration, else the latest backup of NAME; `--accounting`
to `<backup>.accounting.tsv` (absent counts as empty). Compares campaign files only: never the top-level `backups/`
folder or `.standard` (data-model.md). Changes nothing on a failure. Steps and rules are research R5.

- **List (`--list`):** prints `unaccounted: <id> <file> <first 60 chars>` for the first 40 units not yet matched or
  accounted (all with `--all`; a unit whose accounting line is wrong carries `(anchor not found: ...)` or
  `(drop class X is not declared by the pending migrations)`), then `N more` if cut, then `skipped: <file>` for each
  `skipped` line, then `check <name>: <failure>` for each failing check, then
  `<k> to account in backups/<backup file>.accounting.tsv`. Writes nothing. Exit 0. This is how DMC gets the ids it
  accounts for, and where to write them.
- **Pass:** writes `.standard` (the kit's standard) and `<backup>.verified.tsv`, removes every `.migrating` mark of
  the campaign (not only the named backup's, so `--backup` naming another backup leaves no mark behind), and
  prints `verified NAME: <u> units, <m> matched, <a> accounted, <n> dropped; standard K` (`<a>` reworded and `<n>`
  dropped, so `u = m + a + n`). Exit 0.
- **Fail:** prints the unaccounted lines as `--list` does, then `skipped: <file> <reason>` per skipped file, then one
  line per failed check (`check <name>: <failure>`), then `dmc campaign: verification failed: <reason>; <backup file>
  is untouched`. Exit 1. `.standard` is not written and
  the mark stays.
- **Cannot verify:** the backup is missing, damaged or from another campaign; no pending migration is known; the
  accounting file has a bad line; two different units share an id. One line, exit 1. A bad accounting line names
  its line number.

Pending migrations are those with a number above the standard in the backup's `.standard` (1 if none) up to the kit's.

## `dmc campaign restore NAME [--from FILE] [--yes]`

`--from` defaults to the marked backup of an unfinished migration, else the latest backup of NAME. Without `--yes` (a
plan, nothing changes):

```
restore NAME from <file>: <r> files replaced, <d> removed, <a> added
warning: NAME has changed since the migration was verified (<c> files): those changes will be lost
confirm with --yes
```

(the warning line only when it applies; exit 0). The warning compares the campaign with `<file>.verified.tsv`; with
none, it compares with the backup and reads `has changed since the backup`. With `--yes`, checks the backup whole,
then replaces the campaign as research R9 describes, keeping `backups/`, and removes every `.migrating` mark of the
campaign. Prints `restored NAME from <file>: <n> files`. Exit 0. A failure leaves the campaign as it was, with one
line, exit 1. `--from` a file that is not a backup of NAME (the manifest's campaign differs) is an error. With
`--yes` the backup is read once: the extraction checks each file as it writes it.

**Recovery.** `standard`, `backups`, `backup`, `verify` and `restore` each first put back what an interrupted restore of
NAME left, and say so in one line before their own output: `recovered NAME from an interrupted restore`, followed by
`; <folder> ...` for anything kept. A restore's staging and aside folders share an id, so they show how far it got:

| Left | Means | Recovery |
|---|---|---|
| aside, no campaign folder | stopped before the rename into place | the aside goes back as the campaign |
| aside and campaign, no staging of that id | the rename into place was done | the marks are removed and the aside deleted |
| aside, campaign and staging of that id | something made a new campaign folder meanwhile | nothing is touched; exit 1 with `an interrupted restore left NAME as adventures/<aside>, and adventures/NAME exists too: keep the one to play, delete the other, and run the command again` |
| staging | its `backups/` was moved there | its `backups/` goes back (entry by entry when the campaign has a `backups/` again, never replacing one); the staging is deleted only when it holds none, else it is kept and named |

## Changed behaviour of existing verbs

- **`export`**: leaves the campaign's top-level `backups/` folder out of the archive and its manifest. `--out`
  inside `adventures/` is still refused, except by `backup`, which passes the backups folder of the campaign being
  exported to the exporter directly (not through `--out`), so a player cannot choose an output path in `adventures/`.
- **`import`**: leaves out the files and folders under a top-level `backups/`, which only an export made by an older
  `dmc` can hold, and says `left out backups/ (<n> files): an export made by an older dmc held it, and backups stay
  with their campaign` after the `imported` line when there were files (`<n>` files fewer are counted there). `restore`
  leaves them out of a backup the same way. Still never changes an existing campaign. After a successful import, one more line when the campaign is not current: `NAME is behind
  (standard C, kit K): ask DMC to bring it up to date` or `NAME is ahead (standard C, kit K): update dmc`. Nothing
  when current.
- **`show`**: unchanged.
- **`dmc update` / `dmc refresh`**: after a refresh that updated files, one extra line when any campaign of the
  refreshed DMC folder is behind or unfinished: `dmc refresh: <folder>: <n> campaigns are behind the new rules: ask DMC
  to bring them up to date` (`1 campaign is behind ... bring it up to date`). Nothing when none is, or when the refresh
  changed nothing (the resume tells the player then).

Companion files are named after the whole backup file: `<backup file>.accounting.tsv`, `<backup file>.verified.tsv`,
`<backup file>.migrating` (for example `rorn_backup_20261110120000.zip.accounting.tsv`), so DMC adds a suffix to the name
`backup` printed and never has to strip one. The staging and aside folders of `restore` are
`adventures/.restore-NAME-<id>/` and `adventures/.replaced-NAME-<id>/`.
