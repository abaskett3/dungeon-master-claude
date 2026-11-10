# Quickstart: validating campaign migrations

Run from the repository root. `dmc` below is your own build (`artifacts/bin/dmc`), never the installed one
(`.docs/development.md` § Building). Scenarios 1 to 5 are covered by automated tests (named in brackets); scenario 6
is the author's live run and is not automated (research R11).

## Setup

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
artifacts/bin/dmc install --path <scratch>          # a scratch DMC folder; cd into it
```

Make a campaign to work on: copy the fixture `tests/Dmc.Tests/Fixtures/migrations/004/before/` to
`<scratch>/adventures/demo/`, and write `demo` to `<scratch>/adventures/.active-campaign`.

## 1. Where a campaign stands [StandardTests]

```
dmc campaign standard demo          # behind: standard 1, kit 4: 002-..., 003-..., 004-...
```

`dmc campaign standard demo --init` on a campaign with no `.standard` writes the kit's standard (`initialised demo
at standard 4`); run it on a copy, because it makes the campaign current without migrating it. On a campaign that
already has a record it fails with one line. A campaign newer than the kit (write `9` to `.standard`) prints
`ahead: ...`. A `.standard` holding `x` is an error.

## 2. Backups and the limit of three [BackupTests]

```
dmc campaign backups demo                         # 0 backups
dmc campaign backup demo                          # backed up demo: N files to backups/demo_backup_<stamp>.zip
```

A backup leaves a migration marked unfinished, so `dmc campaign standard demo` now says `unfinished: <file>` and a
second `backup` is refused. For this scenario, delete the `.migrating` file by hand after each backup. Repeat until
`backups` says `3 backups` and ends `ask: may the oldest backup, <file>, be removed? yes: --prune oldest`, then:

- `dmc campaign backup demo --prune oldest` leaves three: the oldest is gone and a new one exists (and its
  `.accounting.tsv`, if any, is gone).
- With five backups, `dmc campaign backup demo --prune old` leaves three: the two latest of the old five and the new.
- `dmc campaign backup demo` with no flag always adds one, whatever the count (the player said no).
- A zip in `backups/` named `notes.zip` is never counted or removed.
- Inspect the new zip with `dmc campaign show <zip>`: it holds no `backups/` entry (FR-005d).

## 3. Verify: pass, and fail when a unit is lost [VerifyTests, MigrationGoldenTests]

With a backup taken in scenario 2 and the campaign's files (not `backups/`) replaced by the fixture's `after/`:

```
dmc campaign verify demo --list         # the units still to account, with ids; writes nothing
dmc campaign verify demo --accounting tests/Dmc.Tests/Fixtures/migrations/004/accounting.tsv
```

Expect `verified demo: ...`, `.standard` containing `4`, a `.verified.tsv` beside the backup and no `.migrating`.
Add a line `skipped<TAB>game-state.md<TAB>test` to a copy of the accounting file and verify with it: expect a
failure naming the file. Then delete one line from
`adventures/demo/story/canon.md` and run it again: expect `unaccounted: <id> story/canon.md ...` and exit 1, with
`.standard` unchanged. Edit the accounting file to use `dropped:made-up-class`: expect a failure naming the class.

## 4. Restore [RestoreTests]

```
dmc campaign restore demo                  # prints the plan; nothing changes
dmc campaign restore demo --yes            # restored demo from backups/<latest>: N files
```

The campaign equals the backup file for file, `.standard` holds what it held then, and `backups/` is untouched. Change
a file after a passing verify first: the plan shows `warning: demo has changed since the migration was verified`.
Without a change since verify, there is no warning, although the migration changed files. Kill the command during `--yes` (a test injects a
failure after the rename-aside): the next run puts the campaign back before anything else.

## 5. Export and import leave `backups/` alone [ExportBackupsTests, CampaignImportRefusalTests, CampaignRoundTripTests]

`dmc campaign export demo` with backups present produces an archive with no `backups/` entry and the same size as
without them. `dmc campaign import` of that archive under `--as demo2` makes a campaign with no `backups/` folder,
and prints `demo2 is behind (standard 1, kit 4): ask DMC to bring it up to date` when it is behind. A hand-built
archive with a `backups/x.zip` entry is refused.

## 6. The author's live run (not automated)

For a new migration `NNN`: in the scratch folder, with `before/` as the campaign, run `dmc play`, say "bring my
campaign files up to date", answer the questions, and let DMC finish. Then compare `adventures/demo` with
`after/`: `diff -r` shows only differences that are not content (wording of a reworded line). `dmc campaign verify`
must have passed in the session. Record in the PR description that this was done and what differed.

For this feature, repeat it on a copy of the maintainer's long campaign: `dmc campaign export <campaign> --out
<scratch>/long.zip` from the real DMC folder (which writes nothing there), `dmc campaign import <scratch>/long.zip`
in the scratch folder, then migrate. Record whether it finished in one session, the context used, verify's result,
what was dropped, and the sizes before and after (SC-003). Nothing from the campaign is committed.

## Where the tests are

`tests/Dmc.Tests/Campaign/` (`StandardTests`, `BackupTests`, `VerifyTests`, `RestoreTests`,
`InterruptedMigrationTests`, `ExportBackupsTests`, `ContentUnitsTests`, `AccountingFileTests`, `MigrationFileTests`,
`MigrationChecksTests`) and `tests/Dmc.Tests/Toolkit/` (`MigrationFilesTests`,
`MigrationGoldenTests`, and the prompt checks for the resume, new-campaign and migrate skills).
