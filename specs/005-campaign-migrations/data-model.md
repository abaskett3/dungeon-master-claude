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
| `checks` | list of names | built-in checks `verify` runs; each must exist in `MigrationChecks` |
| `size` | map path to int | line targets reported by `verify` (R6) |

Rules: numbers are contiguous from 2 (standard 1 is the baseline and has no file); a file with a number gap, a
`standard` that differs from its name, a `checks` entry that does not exist, or no fixture is a build failure.

## Backup

A campaign export (existing ZIP with `manifest.json`), plus a name and a place.

| Attribute | Rule |
|---|---|
| path | `adventures/<campaign>/backups/<campaign>_backup_<yyyyMMddHHmmss>.zip`, local time; a clash takes `-2`, `-3` before `.zip` |
| contents | the campaign's files, `backups/` excluded (FR-005d), plus `.standard` as it was |
| order | by the timestamp in the name, then the suffix; the oldest has the smallest |
| counted | only names matching `^<campaign>_backup_\d{14}(-\d+)?\.zip$` |
| companion | `<backup name>.accounting.tsv` beside it, written during a migration, removed with the backup |
| limit | the retention rule of FR-005a, applied by `backup --prune` |

## Content unit

A non-blank line of a campaign file, normalised: trimmed, whitespace runs collapsed to one space, a leading `- `,
`* `, `+ `, `N. ` or `> ` removed, then compared case-sensitively. Skipped (structure, not content): lines that start
with `#`, markdown table separator rows (`|---|`), horizontal rules, and the inside of `<!-- ... -->` comments.
A table row is one unit (its cells are not split). Its **id** is the first 8 hex digits of the SHA-256 of the
normalised text (UTF-8). Ids are only unique within one verification; two identical units in one file share an id
and are accounted once.

## Accounting file: `<backup name>.accounting.tsv`

Tab-separated, one unit per line, `#` lines ignored:

```
<id>\t<disposition>\t<detail>
```

| Disposition | Detail | `verify` accepts it when |
|---|---|---|
| `reworded` | an anchor, at least 8 characters | the anchor occurs in some file of the migrated campaign |
| `dropped:<class>` | a short reason, free text | `<class>` is in `drops` of a pending migration |

(`moved` is not a disposition: a unit that moved verbatim is matched and never listed.)

## Verification result

Printed by `verify`; not stored. Fields: backup used, pending migrations, units checked, matched, unaccounted
(count and the first 40), accounted by disposition, checks (name, pass or fail), sizes (file, lines, target),
and the outcome `passed` or `failed`.

## Check

A named function `(old campaign, new campaign, accounting) -> list of failure lines`. Registered in
`MigrationChecks`. Initial set: `no-dead-opposition-rows`, `fights-have-timeline` (R7).

## Fixture

`tests/Dmc.Tests/Fixtures/migrations/NNN/` with `before/`, `after/` (campaign folders) and `accounting.tsv`
(ids computed from `before` against `after`).
