# Data Model: Campaign Migrations

Files and values only; there is no database. Names follow the spec's Key Entities. Formats are in `contracts/`.

## Standard

An integer `>= 1`. **Kit standard** = the highest migration number in `migrations/` of the DMC folder (1 when there
is none). **Campaign standard** = the integer in `adventures/<campaign>/.standard`, or 1 when the file is missing.

| State | Condition | `dmc campaign standard` says |
|---|---|---|
| current | campaign == kit | `current` |
| behind | campaign < kit | `behind: standard C, kit K: <pending migration files>` |
| ahead | campaign > kit | `ahead: standard C, kit K: update dmc` |
| unfinished | a backup of the campaign has a `.migrating` mark | `unfinished: <backup file>` (takes precedence) |
| unreadable | `.standard` is not one integer | one-line error; counts as not current |

Transitions: only `dmc campaign verify` (on a pass) sets the campaign standard to the kit standard;
`dmc campaign standard NAME --init` sets it for a campaign that has no record, to the kit standard.
`dmc campaign restore` puts back whatever `.standard` the backup holds (the standard before the migration).

## Campaign record: `adventures/<campaign>/.standard`

One line, an integer, a newline. Included in exports and backups (it is an ordinary file of the campaign).

## Migration: `migrations/NNN-<slug>.md` (installed from `kit/migrations/`)

| Field | Type | Meaning |
|---|---|---|
| `standard` | int | `NNN`; must equal the file name's number |
| `title` | text | one line, shown to the player |
| `shipped` | text | the PR that changed the standard, e.g. `PR #173` |
| `touches` | list of paths | campaign files the migration may change, relative to the campaign |
| `drops` | list of class names | content the migration may drop (FR-009b); each is a lowercase-hyphen word |
| `checks` | list of names | built-in checks `verify` runs, at least one for the shape the migration produces; each must exist in `MigrationChecks` |

No size targets: those live in `dmc size` (research R6).

Rules: numbers are contiguous from 2 (standard 1 is the baseline and has no file); a file with a number gap, a
`standard` that differs from its name, an empty `checks`, a `checks` entry that does not exist, or no fixture is a
build failure.

## Backup

A campaign export (existing ZIP with `manifest.json`), plus a name and a place.

| Attribute | Rule |
|---|---|
| path | `adventures/<campaign>/backups/<campaign>_backup_<yyyyMMddHHmmss>.zip`, local time; a clash takes `-2`, `-3` before `.zip` |
| contents | the campaign's files, `backups/` excluded (FR-005d), plus `.standard` as it was |
| order | by the timestamp in the name, then the suffix; the oldest has the smallest |
| counted | only names matching `^<campaign>_backup_\d{14}(-\d+)?\.zip$` |
| companions | beside it, each removed with the backup: `<backup name>.accounting.tsv` (written by DMC during a migration), `<backup name>.verified.tsv` (written by a passing `verify`), `<backup name>.migrating` (written by `backup`, removed by a passing `verify` or any `restore`) |
| limit | the retention rule of FR-005a, all in `BackupFolder`: `backups` prints the question as an `ask:` line, `backup --prune` applies a yes |

## Campaign files (what `verify` and `restore` compare)

Every file under `adventures/<campaign>/` except the top-level `backups/` folder and `.standard`, on the backup's
side and the campaign's alike.

## Content unit

A non-blank line of a campaign file, normalised: trimmed, whitespace runs collapsed to one space, a leading `- `,
`* `, `+ `, `N. ` or `> ` removed, a heading's leading `#`s and the space after them removed, then compared
case-sensitively. Headings are units. Skipped (structure, not content): markdown table separator rows (`|---|`),
horizontal rules, and the inside of `<!-- ... -->` comments. A table row is one unit (its cells are not split). Its
**id** is the first 12 hex digits of the SHA-256 of the normalised text (UTF-8). Two identical units share an id and
are accounted once; two different texts that share an id in one verification fail it.

## Accounting file: `<backup name>.accounting.tsv`

Tab-separated, one unit per line, `#` lines ignored:

```
<id>\t<disposition>\t<detail>
```

| Disposition | Detail | `verify` accepts it when |
|---|---|---|
| `reworded` | an anchor, at least 8 characters | the anchor occurs in some file of the migrated campaign |
| `dropped:<class>` | a short reason, free text | `<class>` is in `drops` of a pending migration |

A file DMC left unmigrated (FR-010) has a line `skipped\t<file>\t<reason>`, where `<file>` is a campaign path in
place of an id. `verify` never accepts it: the run fails and names the file.

(`moved` is not a disposition: a unit that moved verbatim is matched and never listed.)

## Verified record: `<backup name>.verified.tsv`

Written by a passing `verify`: one line `<path>\t<sha256>` per campaign file (as above) of the migrated campaign.
`restore` compares the campaign with it for its warning (research R9).

## Verification result

Printed by `verify`; not stored. Fields: backup used, pending migrations, units checked, matched, unaccounted
(count and the first 40), accounted by disposition, skipped files, checks (name, pass or fail), and the outcome
`passed` or `failed`. `verify --list` prints the unaccounted units and skipped files only.

## Check

A named function `(old campaign, new campaign, accounting) -> list of failure lines`. Registered in
`MigrationChecks`. Initial set (R7): `sheets-end-with-history` (002), `npc-sheets-have-names-line` (003),
`no-dead-opposition-rows` and `fights-have-timeline` (004).

## Fixture

`tests/Dmc.Tests/Fixtures/migrations/NNN/` with `before/`, `after/` (campaign folders) and `accounting.tsv`
(ids listed by `verify --list` of `after` against a backup of `before`).
