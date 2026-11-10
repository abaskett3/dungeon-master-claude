---

description: "Task list for campaign migrations (issue #172)"
---

# Tasks: Bring Campaign Files Up to the Latest Standard

**Input**: Design documents from `specs/005-campaign-migrations/`

**Prerequisites**: plan.md, spec.md, research.md (R1 to R13, amendments A1 to A9), data-model.md,
contracts/campaign-commands.md, contracts/migration-file.md, quickstart.md

**Tests**: Requested. FR-016 asks for a golden fixture test per migration, SC-005 and SC-009 to SC-013 are counted
outcomes, and quickstart.md names the test classes. Every test needs only the built program and temp folders, so
none skips (Constitution V). Tests are written first and fail before the code they cover.

**Organization**: Grouped by user story. Story 1a (backups) comes before story 1 because the one-step migration
cannot run without a backup; both are P1. Labels are the spec's story numbers: `[US1a]`, `[US1]` to `[US6]`.

**Revised 2026-11-10** after `/speckit-analyze`: verification skips `backups/` and `.standard`, counts headings,
uses 12-digit ids, has a `--list` mode and a `skipped` line; every migration names a shape check; an unfinished
migration is marked and resumed against its first backup; restore warns about changes since verify; `backups`
prints the removal question; import reports a campaign that is not current; sizes are left to `dmc size`; a live run
on a copy of the long campaign is added.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: The spec's user story
- Paths are relative to the repository root. Code is C# on .NET 10, Native AOT; warnings are errors; every type and
  member added or changed gets an XML doc comment, private ones included (CLAUDE.md § Documentation).

## Conventions every task follows

- Failures print one line, `dmc campaign: <message>`: exit 2 for an argument mistake, exit 1 for anything else
  (contracts/campaign-commands.md). Output stays small, because what `dmc` prints lands in DMC's context.
- `NAME` is a folder of `adventures/` whose name does not start with a dot.
- "Campaign files" means every file under `adventures/<NAME>/` except the top-level `backups/` folder and
  `.standard` (data-model.md § Campaign files). `verify` and `restore` compare only these.
- A backup's companions are `<backup name>.accounting.tsv`, `.verified.tsv` and `.migrating`; whatever removes a
  backup removes them (data-model.md § Backup).
- No reflection and no YAML library; JSON is source-generated (plan, Technical Context).
- Black-box tests run the built `dmc` through `tests/Dmc.Tests/Support/DmcRunner.cs` in temp DMC folders from
  `tests/Dmc.Tests/Support/DmcFolders.cs`; fixtures are read from the repository via `TestEnv`'s repository root,
  as `tests/Dmc.Tests/Pdf/Fixtures/` is.

---

## Phase 1: Setup

**Purpose**: Settle the two facts the design left open before anything depends on them.

- [ ] T001 Find the PR that made history the last section of a sheet: run `git log -S "## History" --oneline -- kit/templates/pc-sheet.md kit/templates/npc-sheet.md kit/skills/dmc-campaign-full-save/SKILL.md` and `git log -S "Names they use" --oneline -- kit/templates/npc-sheet.md kit/skills/dmc-character-sheet-update/SKILL.md`. Fix the order of migrations 002 and 003 by which shipped first, and record the PR numbers and the order in `specs/005-campaign-migrations/research.md` § R3 "The shipped cases" (if Names they use shipped first, swap the two slugs and their checks everywhere this file names them).
- [ ] T002 Add a test to `tests/Dmc.Tests/Campaign/ArchivePathsTests.cs` that `ArchivePaths.Refusal` accepts `.standard` and that `dmc campaign export` of a campaign holding `.standard` writes it to the archive and `import` restores it byte for byte (research R2). If either fails, rename the record to `standard.txt` in research.md R2, data-model.md, contracts/campaign-commands.md and every later task, and say so in the PR.
- [ ] T003 [P] Create `tests/Dmc.Tests/Support/MigrationFixtures.cs`: the path of `tests/Dmc.Tests/Fixtures/migrations/` under the repository root, a list of the fixture folders, and a helper that copies a fixture's `before/` or `after/` into `adventures/<name>/` of a temp DMC folder, replacing the campaign files and leaving `backups/` alone. XML comment on every member.

---

## Phase 2: Foundational (blocks every story)

**Purpose**: The standard record, the migration file reader and the new verbs' argument parsing. Plan phase 1.

### Tests first

- [ ] T004 [P] Write `tests/Dmc.Tests/Campaign/StandardTests.cs` for `dmc campaign standard [NAME] [--init]` (contracts/campaign-commands.md, data-model.md § Standard), in temp DMC folders with hand-made `migrations/` files: no `.standard` and kit 4 prints `behind: standard 1, kit 4: 002-…, 003-…, 004-…`; `.standard` = 4 prints `current`; 9 prints `ahead: standard 9, kit 4: update dmc`; a `<backup>.migrating` file in `backups/` prints `unfinished: <backup file>` whatever `.standard` says; `x` is a one-line error, exit 1; no `migrations/` means kit 1, so no record prints `current`; no NAME uses `adventures/.active-campaign`; `--init` without a record writes `4\n` and prints `initialised NAME at standard 4`; `--init` with a record fails, exit 1; a missing campaign or a dot-folder name fails, exit 1.
- [ ] T005 [P] Write `tests/Dmc.Tests/Toolkit/MigrationFilesTests.cs` with the build-time checks of contracts/migration-file.md § What `dmc` checks, over every file in `kit/migrations/`: numbers contiguous from 2 and equal to the file name; front matter well formed, with no unknown key (a `size` key is unknown); `checks` not empty and every name in it exists in `MigrationChecks`; every `touches` path is relative, has no `..` and is not under `backups/`; the five headings `## What changes`, `## Steps`, `## Judgments`, `## Declared drops`, `## Done when` present in that order; each `drops` class has a line under `## Declared drops`; a fixture folder with `before/`, `after/` and `accounting.tsv` exists for each. With no migration file yet the test passes vacuously; it starts biting when T040 adds the first migration.
- [ ] T006 [P] Add parser unit tests to `tests/Dmc.Tests/Campaign/MigrationFileTests.cs`: `key: value` and `key: [a, b]`; an unknown key (including `size`), a `standard` that differs from the name's number, a class name that is not lowercase words joined by hyphens, a missing closing `---` and a duplicate key are errors naming the line.
- [ ] T007 Extend `tests/Dmc.Tests/Campaign/CampaignCommandContract.cs` with the verbs `standard`, `backups`, `backup`, `verify`, `restore`: `--help` lists them; an unknown flag, a missing NAME where one is needed, `--prune` with a value other than `oldest` or `old`, and `--list` or `--all` on a verb other than `verify` each exit 2 with one line.

### Implementation

- [ ] T008 [P] Create `src/Dmc.Cli/Campaign/StandardRecord.cs`: read `adventures/<campaign>/.standard` ("One line, an integer, a newline"; missing means 1; anything else is unreadable), write it, and compare a campaign standard with the kit's as current, behind or ahead; report unfinished when `BackupFolder` (T021) finds a `.migrating` mark (data-model.md § Standard). Until T021 exists, take the mark lookup as a parameter.
- [ ] T009 [P] Create `src/Dmc.Cli/Campaign/MigrationFile.cs`: the hand-written front-matter reader of contracts/migration-file.md (keys `standard`, `title`, `shipped`, `touches`, `drops`, `checks`; no maps) and the five body headings, with one-line errors naming the line. No YAML library.
- [ ] T010 Create `src/Dmc.Cli/Campaign/MigrationSet.cs` (depends on T009): read `migrations/` of a DMC folder, the kit standard (highest `NNN`, 1 with none), and the pending migrations above a given standard, oldest first, with the union of their `drops` and `checks`. A number gap or a name/number mismatch is an error.
- [ ] T011 Create `src/Dmc.Cli/Campaign/MigrationChecks.cs`: a registry of named checks, each `(old campaign, new campaign, accounting) -> failure lines` (data-model.md § Check), empty for now, with a lookup the verifier and T005 use. The four checks arrive in T047.
- [ ] T012 [P] Add the row `("kit/migrations/", "migrations/")` to `GeneratedFiles.Table` in `src/Dmc.Cli/Kit/GeneratedFiles.cs`, so `install`, `refresh`, `play` and `update` carry migrations (research R3). Check `tests/Dmc.Tests/Kit/GeneratedFilesTests.cs` and `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` (`EveryFileUnderKitIsEmbeddedAndInTheTable`, `NoTableTargetIsUnderAPlayerFolder`) still pass, and add a case that a refresh writes `migrations/`.
- [ ] T013 [P] Add `Edit(/migrations/**)` and `Write(/migrations/**)` to the deny list in `kit/settings.json`, next to `/templates/**` (research R13), and extend `TheKitSettingsProtectTheGeneratedFiles` in `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` to require them. In the same file, add `.. MarkdownUnder("kit/migrations")` to `KitPrompts()`, so every prompt check (real `dmc` subcommands, paths that exist, no size in lines or words, no `230`) covers the migrations DMC follows.
- [ ] T014 Add the requests for `standard`, `backups`, `backup`, `verify` (with `--backup`, `--accounting`, `--list`, `--all`) and `restore` to `src/Dmc.Cli/Campaign/CampaignArguments.cs`, and the verbs to the help text in `src/Dmc.Cli/Commands/CampaignCommand.cs` (each verb not built yet fails with one line until its story lands).
- [ ] T015 Implement `dmc campaign standard [NAME] [--init]` in `src/Dmc.Cli/Commands/CampaignCommand.cs` using T008 and T010, printing exactly the lines of contracts/campaign-commands.md. T004, T006 and T007 pass, except the `unfinished` case, which passes after T021.

**Checkpoint**: `dmc campaign standard` works; migrations install with the kit; the build is green.

---

## Phase 3: User Story 1a - Backups are kept to a limit, and the player decides what goes (P1)

**Goal**: `dmc campaign backups` lists backups and prints the removal question; `dmc campaign backup [--prune
oldest|old]` writes a checked backup, marks the migration unfinished and removes old backups only on a flag. Export
leaves `backups/` out and import refuses it. Plan phase 2, the first slice that can ship on its own.

**Independent Test**: quickstart.md scenarios 2 and 5: 0, 2, 3 and 5 earlier backups, with and without `--prune`,
leave exactly the backups FR-005a gives (SC-010, SC-011), with the right `ask:` line each time; a backup made
beside 5 others is no larger than one made beside none (SC-012).

### Tests first

- [ ] T016 [P] [US1a] Write `tests/Dmc.Tests/Campaign/ExportBackupsTests.cs`: with `adventures/<NAME>/backups/` holding zips and companion files, `dmc campaign export` writes an archive and manifest with no `backups/` entry, the same size as without them (SC-012); `--out` inside `adventures/`, including inside the campaign's own `backups/`, is still refused (FR-005e); the default name and place of a player's export are unchanged.
- [ ] T017 [P] [US1a] Write `tests/Dmc.Tests/Campaign/BackupTests.cs` (remove the `.migrating` mark between backups where a test takes several): `backups` prints `0 backups` alone, or `N backups` then file names oldest first; it ends with no `ask:` line below three, `ask: may the oldest backup, <file>, be removed? yes: --prune oldest` at exactly three, `ask: may the 3 oldest backups be removed, keeping the latest two? yes: --prune old` at five, and no `ask:` line while a mark exists; `backup` writes `<NAME>_backup_<yyyyMMddHHmmss>.zip` (local time) and `<file>.migrating`, and prints `backed up NAME: <n> files to backups/<file>`; with a mark present `backup` refuses with `NAME has an unfinished migration from <file>: finish it or restore it`, exit 1, and writes nothing; a clash in the same second takes `-2`, then `-3`, and both count; with 3 earlier, `--prune oldest` leaves 3 and the oldest is gone; with 5 earlier, `--prune old` leaves 3 (the two latest old ones and the new one); with no flag one is always added (2→3, 3→4, 5→6); each removed backup's companions go with it; `notes.zip` and `x_backup_1.zip` in `backups/` are never counted or removed (counted names match `^<campaign>_backup_\d{14}(-\d+)?\.zip$`); `--prune` with nothing to remove is not an error; a write failure (a read-only `backups/`) removes the `.partial-` file, removes no backup, writes no mark, and exits 1 with one line; the written zip passes `dmc campaign show`.
- [ ] T018 [P] [US1a] Add cases to `tests/Dmc.Tests/Campaign/CampaignImportRefusalTests.cs`: a hand-built archive with a `backups/x.zip` entry is refused as `<file> is damaged: …` and nothing is written; an import never creates a `backups/` folder.

### Implementation

- [ ] T019 [US1a] Change `src/Dmc.Cli/Campaign/CampaignExporter.cs`: `Walk` skips the top-level `backups/` folder of the campaign it reads (FR-005d), and `Plan` takes a destination inside `adventures/` only when the caller passes that campaign's own `backups/` folder directly, never through `--out` (FR-005e, contracts § Changed behaviour). T016 passes.
- [ ] T020 [P] [US1a] Create `src/Dmc.Cli/Campaign/CampaignFiles.cs`: the one walk of a campaign's files that excludes the top-level `backups/` and `.standard`, used by the verifier and the restorer, and for an archive's entries the same exclusion (data-model.md § Campaign files).
- [ ] T021 [P] [US1a] Create `src/Dmc.Cli/Campaign/BackupFolder.cs`: the backup name for a time, the next free name (`-2`, `-3` before `.zip`), the listing of counted backups ordered by timestamp then suffix, the companion paths (`.accounting.tsv`, `.verified.tsv`, `.migrating`), the marked backup if any, the prune selection (`oldest`: the single oldest counted before the new one; `old`: every counted one before it except the latest two), and the `ask:` line for a count (contracts § backups). Every number of the retention rule lives here and nowhere else.
- [ ] T022 [US1a] Create `src/Dmc.Cli/Campaign/CampaignBackup.cs` (depends on T019, T021): refuse while a mark exists; write through the exporter to a `.partial-` file, rename it into place without replacing a file, check it whole (manifest, sizes, SHA-256 of every file) with the importer's existing check, write `<file>.migrating`, and only then prune what `--prune` selects with each pruned backup's companions. A removal that fails is reported in the output line and does not fail the backup (spec edge case).
- [ ] T023 [US1a] Implement `dmc campaign backups NAME` and `dmc campaign backup NAME [--prune oldest|old]` in `src/Dmc.Cli/Commands/CampaignCommand.cs`, printing the lines of contracts/campaign-commands.md (`; removed <k> older` with `--prune`). T017 passes, and so does T004's `unfinished` case.
- [ ] T024 [US1a] Make `src/Dmc.Cli/Campaign/CampaignImporter.cs` (with `src/Dmc.Cli/Campaign/ArchivePaths.cs` if the check belongs there) refuse an archive entry under a top-level `backups/` (research R12). T018 passes.

**Checkpoint**: Backups can be taken, listed and pruned on a flag; a backup marks a migration unfinished; export
and import never carry backups.

---

## Phase 4: User Story 1 - Catch every campaign up in one step (P1) 🎯 MVP

**Goal**: `dmc campaign verify` accounts for every unit of old content and records the standard on a pass, with
`--list` giving DMC the ids; `dmc campaign restore` reverts to a backup; the `dmc-migrate-campaign` skill runs
backup, migrate, verify in that order, asks the player, and resumes an unfinished migration. Plan phases 3 to 5.

**Independent Test**: quickstart.md scenarios 3 and 4 on a hand-made campaign: verify passes when every unit is
matched or accounted and writes `.standard`; removing one line makes it fail naming the unit (SC-013); restore with
`--yes` makes the campaign equal the backup file for file with `backups/` untouched (SC-009); an interrupted
migration rerun verifies against its first backup (SC-005).

### Tests first

- [ ] T025 [P] [US1] Write `tests/Dmc.Tests/Campaign/ContentUnitsTests.cs` for data-model.md § Content unit: a non-blank line is trimmed and whitespace runs collapsed to one space; a leading `- `, `* `, `+ `, `N. ` or `> ` is removed; a heading is a unit with its leading `#`s and the space after them removed (`### Captain Vex` → `Captain Vex`); table separator rows (`|---|`), horizontal rules and the inside of `<!-- ... -->` comments (including comments spanning lines) are skipped; a table row is one unit; comparison is case-sensitive; the id is the first 12 hex digits of the SHA-256 of the normalised UTF-8 text; two identical lines share an id; CRLF and LF files give the same units.
- [ ] T026 [P] [US1] Write `tests/Dmc.Tests/Campaign/AccountingFileTests.cs` for data-model.md § Accounting file: `<id>\t<disposition>\t<detail>` lines, `#` lines ignored; `reworded` needs an anchor of at least 8 characters; `dropped:<class>` needs a lowercase-hyphen class; `skipped\t<file>\t<reason>` is read as a skipped file with a relative campaign path; any other disposition, a missing field, an id that is not 12 hex digits, or a skipped path with `..` or under `backups/` is an error naming its line number.
- [ ] T027 [P] [US1] Write `tests/Dmc.Tests/Campaign/VerifyTests.cs` for contracts/campaign-commands.md § verify and research R5, each with a real `backup` taken first. Pass: identical files pass, write `.standard` = kit standard and `<backup>.verified.tsv` (path and SHA-256 of each campaign file), remove `<backup>.migrating`, and print `verified NAME: <u> units, <m> matched, <a> accounted, <n> dropped; standard K`; a unit moved verbatim to another file is matched; a unit of 20 or more characters contained in a longer line is matched, one under 20 is not; a dropped heading is unaccounted. Exclusions: a unit whose only copy is in `backups/<backup>.accounting.tsv` is unaccounted; a `reworded` anchor found only in the accounting file fails; `.standard` is never a unit. Fail: an unaccounted unit prints `unaccounted: <id> <file> <first 60 chars>` and `dmc campaign: verification failed: <reason>`, exit 1, `.standard` unchanged and the mark kept; 45 unaccounted print 40 and `5 more`, `--all` prints 45; `reworded` with an anchor found passes; `dropped:<class>` passes only for a class in the pending migrations' `drops`; a `skipped` line fails naming the file even when every unit matches; a check named in `checks` that fails is printed and fails the run. List: `--list` prints the same unaccounted lines and `skipped:` lines and `<k> to account`, writes nothing, exits 0, and with the listed ids written as accounting the next `verify` passes. Defaults: `--backup` defaults to the marked backup, else the latest; `--accounting` defaults to the backup's companion and an absent file counts as empty. Cannot verify: a damaged backup, a backup of another campaign, a missing backup, no pending migration (backup standard equals kit), and two different units with one id (forced through a test hook) each print one line, exit 1. No size lines are printed. Verify of a generated campaign of 10 000 units finishes in under 15 seconds (the goal is 5 s; T072 records the measured time).
- [ ] T028 [P] [US1] Write `tests/Dmc.Tests/Campaign/RestoreTests.cs` for contracts/campaign-commands.md § restore and research R9: without `--yes` it prints `restore NAME from <file>: <r> files replaced, <d> removed, <a> added` and `confirm with --yes` and changes nothing; after a passing verify and no further change there is no warning although the migration changed files; after a change since verify the plan adds `warning: NAME has changed since the migration was verified (<c> files): those changes will be lost`; with no `.verified.tsv` the warning compares with the backup and reads `has changed since the backup` (FR-005g); `--from` defaults to the marked backup, else the latest; with `--yes` the campaign equals the backup file for file (files added since are gone, `.standard` is the backup's), `backups/` with its zips and companions is byte-identical except that every `.migrating` mark is gone; `--from` a backup of another campaign is an error; a damaged backup changes nothing; an injected failure after the rename-aside leaves `.replaced-<NAME>-<id>` and the next run puts the campaign back first and reports it in one line; a failure before the rename into place leaves the campaign as it was.
- [ ] T029 [P] [US1] Write `tests/Dmc.Tests/Campaign/InterruptedMigrationTests.cs` for FR-011 and SC-005: take a backup of a campaign, delete one content line and edit another as a half-done migration would, then simulate the rerun: `standard` prints `unfinished: <that backup>`, `backup` is refused, `verify --list` lists the deleted line against the first backup, and `verify` fails naming it; account it and `verify` passes against the first backup; `restore` with no `--from` restores the first backup and clears the mark.
- [ ] T030 [P] [US1] Add prompt checks for `kit/skills/dmc-migrate-campaign/SKILL.md` to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`: its description is one line; it names `dmc campaign standard`, `backups`, `backup`, `verify --list`, `verify` and `restore`, and runs `backup` before any migration step and `verify --list` before the final `verify`; it passes `--prune` only on the player's yes to an `ask:` line and holds no backup count (no "three"/"3" in the retention text); on `unfinished` it takes no backup; it says not to start mid-fight or with a roll pending; it says not to edit `.standard`, `migrations/` or anything in `backups/` except the accounting file; it writes a `skipped` line for a file it leaves alone; on a failed final verify it offers `dmc campaign restore` and does not continue play; after a pass it runs `dmc size NAME --check` (once 006 is on `main`). `EveryDmcCommandInThePromptsIsARealSubcommandAndTheOnesDmcRunsNeedNoPermissionPrompt` covers the commands it names.

### Implementation

- [ ] T031 [P] [US1] Create `src/Dmc.Cli/Campaign/ContentUnits.cs`: split a file into content units, normalise (headings included), skip structure and compute 12-digit ids exactly as data-model.md § Content unit says. T025 passes.
- [ ] T032 [P] [US1] Create `src/Dmc.Cli/Campaign/AccountingFile.cs`: read and validate the TSV of data-model.md § Accounting file, including `skipped` lines, with one-line errors naming the line number. T026 passes.
- [ ] T033 [US1] Create `src/Dmc.Cli/Campaign/CampaignVerifier.cs` (depends on T010, T011, T020, T031, T032): read the backup's entries in memory after checking it whole, without extracting into the campaign; compare campaign files only (T020); take the pending migrations from the backup's `.standard` (1 if none) up to the kit's; run the steps of research R5 in order (backup whole; match units, skipping files whose bytes are identical; account every unaccounted unit; fail on any `skipped` line or id collision; run `checks`); in list mode stop after accounting and report; on a pass write `.standard` with `StandardRecord`, write `<backup>.verified.tsv`, and remove `<backup>.migrating`.
- [ ] T034 [US1] Implement `dmc campaign verify NAME [--backup FILE] [--accounting FILE] [--list] [--all]` in `src/Dmc.Cli/Commands/CampaignCommand.cs`, printing the list, pass, fail and cannot-verify lines of contracts/campaign-commands.md. T027 passes.
- [ ] T035 [US1] Create `src/Dmc.Cli/Campaign/CampaignRestorer.cs` (depends on T020, T021): the plan (replaced, removed, added) and the warning (against `<backup>.verified.tsv` when present, else against the backup), comparing campaign files only; with `--yes` the steps of research R9: extract and check into `adventures/.restore-<id>/` with the importer's extraction code, move `backups/` into the staging and remove every `.migrating` mark there, rename the campaign aside to `adventures/.replaced-<NAME>-<id>`, rename the staging into place, delete the aside; put the campaign back on any failure before the rename into place; recover a `.replaced-<NAME>-*` with no campaign folder before anything else. A rename refused because a file is open reports that file in one line (Windows). An internal hook lets T028 inject a failure after the rename-aside.
- [ ] T036 [US1] Implement `dmc campaign restore NAME [--from FILE] [--yes]` in `src/Dmc.Cli/Commands/CampaignCommand.cs`. T028 and T029 pass.
- [ ] T037 [US1] Write `kit/skills/dmc-migrate-campaign/SKILL.md` (front matter `name`, one-line `description` that the player's "bring my files up to date" triggers). The phases, in order: refuse mid-fight or with a roll pending (FR-008). For each campaign asked for (one, or every folder of `adventures/` not starting with a dot), run `dmc campaign standard NAME`: on `current` say so and stop; on `ahead` say `dmc` needs updating and stop; on `unfinished` say so, take no backup and go straight to the steps against that backup; on `behind` go on. Read each pending `migrations/NNN-*.md`, show its `## What changes` and its `## Declared drops` spelled out, and wait for the player's yes (FR-004, FR-009b). Run `dmc campaign backups NAME`; if it ends with an `ask:` line, put that question to the player once, before the backup. Run `dmc campaign backup NAME`, adding the flag the `ask:` line names only on a yes; stop with nothing changed if it fails. Follow each migration's `## Steps` in order, writing before deleting, applying `## Judgments` and showing the player anything doubtful; leave a file of unexpected shape unchanged, name it to the player and write `skipped<TAB><file><TAB><reason>` to the accounting file (FR-010). Run `dmc campaign verify NAME --list` (with `--all` if it says more are cut), write a line to `<backup name>.accounting.tsv` beside the backup for each unit listed (`reworded` with an anchor, or `dropped:<class>` from the migration's drops), and repeat until it lists nothing but skipped files. Run `dmc campaign verify NAME`. On a pass, tell the player the backup's path and that `dmc campaign restore NAME --yes` reverts; then, once spec 006 has merged and this branch is rebased on it (research R6, merge order), run `dmc size NAME --check` and say each line it prints as something the next full save trims, not a migration failure (nothing printed means nothing to say; a `dmc size` failure is said in one line and does not undo the migration; research R6, "The call"). Quote no budget or line form from 006. On a fail, name the items, say the backup is untouched, offer `dmc campaign restore NAME`, and do not continue play until the player chooses (FR-009c). A rerun after an interruption starts again from `dmc campaign standard`, which says `unfinished`, and the steps are idempotent (FR-011). Never edit `.standard`, `migrations/` or anything in `backups/` except the accounting file. The skill holds no backup count; the `ask:` line carries the rule (Constitution III).
- [ ] T038 [P] [US1] Add `dmc-migrate-campaign` to the skills table in `README.md` (`TheReadmeListsEverySkillAndThePromptsNameOnlyRealOnes`) and the five verbs to `.docs/dmc-reference.md` (synopsis line and one entry per verb, matching contracts/campaign-commands.md, `verify --list` included). T030 passes.

**Checkpoint**: A campaign can be backed up, migrated by hand, listed, verified and restored, and an interrupted
migration resumes safely; the skill strings it together. With no migration files yet, `standard` says `current`, so
the skill has nothing to apply.

---

## Phase 5: User Story 2 - The November review's changes migrate the long campaign without losing a fact (P1)

**Goal**: Migrations 002 (history last), 003 (Names they use) and 004 (the November review) with golden fixtures
that CI verifies, and the four built-in shape checks. Plan phase 6.

**Independent Test**: `MigrationGoldenTests` passes for 002, 003 and 004 and fails when one unit is removed from an
`after/`, an undeclared drop class is claimed, or a file is left in the old shape; on fixture 004, 100% of finished
fights have a Timeline line and 0 dead Opposition rows remain (SC-002).

### Tests first

- [ ] T039 [P] [US2] Write `tests/Dmc.Tests/Toolkit/MigrationGoldenTests.cs` (research R11), over every file in `kit/migrations/` with `MigrationFixtures` (T003): build a temp DMC folder from the built kit, copy `Fixtures/migrations/NNN/before/` in as the campaign with `.standard` = `NNN-1` and a temp `migrations/` holding migrations up to `NNN` only, run the real `dmc campaign backup`, replace the campaign files (not `backups/`) with `after/`, copy `accounting.tsv` beside the backup, and require `dmc campaign verify` to pass and `.standard` to be `NNN`. Second case: delete one content line from the copied `after/` and require a fail naming its id. Third case: change one `dropped:<class>` to a class not in `drops` and require a fail naming the class. Fourth case: put one touched file back as it was in `before/`, account its units, and require the migration's shape check to fail. A migration with no fixture fails the test.
- [ ] T040 [P] [US2] Write `tests/Dmc.Tests/Campaign/MigrationChecksTests.cs` for research R7: `sheets-end-with-history` fails for a `pc-*.md` or `npc-*.md` whose last `## ` section is not `## History` (or `## Notes` / `## History with the PC`) and passes otherwise; `npc-sheets-have-names-line` fails for an `npc-*.md` without the "Names they use" line of `kit/templates/npc-sheet.md`; `no-dead-opposition-rows` fails for a row under `## Opposition` in `story/dm-notes.md` whose second column starts `0 /` or `0/`, passes for live rows, and fails with "no Opposition table found" when the section or table is missing; `fights-have-timeline` fails when a row that was dead in the backup is not a row of `story/dm-notes-archive.md`, or when no line under `## Timeline` in `story/canon.md` carries both an `sN.M` reference and the row's "Who" text (case-insensitive), and passes when both hold; `one-last-played-line` fails for two `Last played` lines or a `## Mix tracker` section in `story/dm-notes.md`, and passes for one `Last played` line or none.

### Implementation

- [ ] T041 [US2] Write `kit/migrations/002-sheet-history-last.md` by contracts/migration-file.md (order and number from T001): `touches` the sheets under `character-sheets/`, no `drops`, `checks: [sheets-end-with-history]`; Steps move `## History` (or the older `## Notes` / `## History with the PC`) to be a sheet's last section, only if it is not already; Judgments for a sheet with two history-like sections; Done when the check passes. Take the rule's wording from `kit/skills/dmc-campaign-full-save/SKILL.md` step 2 so the two agree.
- [ ] T042 [P] [US2] Build `tests/Dmc.Tests/Fixtures/migrations/002/before/` (a campaign from `kit/templates/campaign/` with a PC sheet and an NPC sheet whose history sits in the middle, and one already last), `after/` (history moved to the end) and `accounting.tsv` (empty except a `#` header, since moved lines match verbatim). T039 passes for 002.
- [ ] T043 [US2] Write `kit/migrations/003-names-they-use.md`: `touches` the NPC and companion sheets; `checks: [npc-sheets-have-names-line]`; Steps add the "Names they use" line from `kit/templates/npc-sheet.md` to each NPC or companion sheet that lacks it, filled from that character's own sheet and the session logs; Judgments say to leave it with a placeholder and tell the player when nothing says what names they use; no drops.
- [ ] T044 [P] [US2] Build `tests/Dmc.Tests/Fixtures/migrations/003/` with `before/` (an NPC sheet without the line and one with it), `after/` and `accounting.tsv`. T039 passes for 003.
- [ ] T045 [US2] Write `kit/migrations/004-lean-notes-and-state.md` with the front matter of contracts/migration-file.md's example (`touches: [story/dm-notes.md, story/dm-notes-archive.md, game-state.md, player-options.md, story/canon.md]`, `drops: [mix-tracker, overtaken-plan, finished-arc-beat, scene-history]`, `checks: [no-dead-opposition-rows, fights-have-timeline, one-last-played-line]`, no size targets). Steps, in write-before-delete order (FR-006): each finished fight's story becomes a canon `## Timeline` line with its `sN.M` reference and the group's name as the row's "Who" cell gives it, then its Opposition row moves verbatim, with the fight's stat blocks, to `story/dm-notes-archive.md` (created if missing), as `kit/skills/dmc-update-game-state/SKILL.md` does when a fight ends; a group with members left keeps its row; closed threads (including lines marked DONE or RESOLVED outside the Arc plan) move verbatim to `story/dm-notes-archive.md`, as the full save does; the per-session Mix tracker becomes the one overwritten `Last played` line; overtaken plans and finished Arc plan beats leave `dm-notes.md`; "About this player" keeps only what the player options do not say; history leaves `game-state.md`'s Scene block; standing orders not in play move to canon, the ones in play stay; a sequel's `player-options.md` gains the Predecessor line, asking the player when the folder cannot be told. Judgments per contracts/migration-file.md (a finished fight is `0 / N` or the logs say the group was defeated; keep and ask when they disagree; when a standing order is in play; when a plan is overtaken). Take each target shape from the current templates and `kit/skills/dmc-campaign-full-save/SKILL.md`, not from PR #173's discussion. Say in `## What changes` that the canon Timeline grows by one line per finished fight. For a `dm-notes.md` too large for one Read, say to read it in parts.
- [ ] T046 [US2] Build `tests/Dmc.Tests/Fixtures/migrations/004/before/` at standard 3 from the templates: two finished fights (one `0 / 4`, one `0/3`) and one live one in Opposition, an open thread and two closed ones (one marked RESOLVED), an Arc plan with a done stage on one line, an expired and an active standing order, an overtaken and a current plan, a finished Arc beat, a per-session Mix tracker under its own heading, "About this player" lines that repeat the player options, a Scene block with history, a sequel `player-options.md` with no Predecessor line and a predecessor campaign folder beside it. Keep the files small. Then write `after/` as a correct run leaves it, and build `accounting.tsv` with `dmc campaign verify --list` of `after/` against a backup of `before/` (data-model.md § Fixture): the moved Opposition rows and closed threads need no entry (they match verbatim in `story/dm-notes-archive.md`), a `reworded` entry for each reworded unit (a standing order rewritten for canon, the Scene block's history), `dropped:<class>` for each dropped unit and heading, at least one of each disposition the migration can produce.
- [ ] T047 [US2] Implement `sheets-end-with-history`, `npc-sheets-have-names-line`, `no-dead-opposition-rows`, `fights-have-timeline` and `one-last-played-line` in `src/Dmc.Cli/Campaign/MigrationChecks.cs` (research R7). T040 passes.
- [ ] T048 [US2] Run `tests/Dmc.Tests/Toolkit/MigrationGoldenTests.cs` and `tests/Dmc.Tests/Toolkit/MigrationFilesTests.cs` for 002 to 004 and fix the migration, fixture or accounting until all pass (T039, T005); check on fixture 004 that every finished fight's row is in `story/dm-notes-archive.md` with a Timeline line, no dead row remains in `story/dm-notes.md`, and one `Last played` line replaces the Mix tracker (SC-002).
- [ ] T049 [US2] Author's live run on the fixtures (quickstart.md scenario 6): in a scratch DMC folder made with `artifacts/bin/dmc install`, build one campaign at standard 1 that combines the shapes of `tests/Dmc.Tests/Fixtures/migrations/002/before/`, `003/before/` and `004/before/`, run `dmc play`, say "bring my campaign files up to date", answer the questions, and let DMC finish; compare with the `after/` folders; confirm `verify` passed in the session; then close the session halfway through a second run on a fresh copy and check the rerun resumes against the first backup. Note what differed for the PR description. Fix the migration prose for anything DMC got wrong and repeat.
- [ ] T050 [US2] Live run on a copy of the long campaign (SC-003, the "very large first migration" edge case; quickstart.md scenario 6): from the maintainer's DMC folder run `dmc campaign export <campaign> --out <scratch>/long.zip` (it writes nothing in the real folder), import it into a scratch DMC folder built from this branch, and migrate it there with `dmc play`. Record in the PR whether it finished within one session, the context used, verify's result, what was dropped, and the sizes of `game-state.md`, `story/dm-notes.md` and `story/summary.md` before and after with `dmc size NAME` (spec 006 merges first; bytes ÷ 4 by hand only if it has not), and the `dmc size NAME --check` lines before and after (12 lines before, on 2026-11-10). Commit nothing from the campaign.

**Checkpoint**: The November review's standard can be reached from any older campaign, and CI proves the declared
drops and checks agree with a correct result.

---

## Phase 6: User Story 3 - The player is told when files are behind (P2)

**Goal**: One line at resume and after `dmc update` / `dmc refresh` when a campaign is behind; none when current;
play waits on an unfinished migration. A new campaign starts current.

**Independent Test**: quickstart.md scenario 1 plus a refresh over a folder with one behind and one current
campaign: one line after the refresh, one line at resume for the behind one, nothing for the current one (SC-007).

### Tests first

- [ ] T051 [P] [US3] Add cases to `tests/Dmc.Tests/Kit/RefreshTests.cs`: after `dmc refresh` of a DMC folder with any behind or unfinished campaign, exactly one line `n campaigns are behind the new rules: ask DMC to bring them up to date`; none when every campaign is current or there is none; dot-folders in `adventures/` are not counted. Cover `dmc update`'s use of the same code path if its tests allow it.
- [ ] T052 [P] [US3] Add prompt checks to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`: `kit/skills/dmc-resume-campaign/SKILL.md` step 1 runs `dmc campaign standard` and says one line only when the answer is not `current` (offer the migration on `behind`, the update on `ahead`, and on `unfinished` that play waits until the migration is finished or restored); `kit/skills/dmc-new-campaign/SKILL.md` runs `dmc campaign standard <folder> --init` right after copying the templates.

### Implementation

- [ ] T053 [US3] Edit `kit/skills/dmc-resume-campaign/SKILL.md` step 1: run `dmc campaign standard`; on `behind`, say one out-of-character line that the campaign's files are behind and offer to bring them up to date with `dmc-migrate-campaign`; on `ahead`, say `dmc` needs updating; on `unfinished`, say a migration was interrupted and that play waits until the player finishes it (`dmc-migrate-campaign`) or restores the backup (FR-011); on `current`, say nothing (FR-012, FR-019). A declined offer on `behind` changes nothing and comes back at the next resume. Keep it to two sentences of skill text.
- [ ] T054 [US3] Edit `kit/skills/dmc-new-campaign/SKILL.md` § 1: after copying `templates/campaign/`, run `dmc campaign standard <folder> --init` (research R2).
- [ ] T055 [US3] Add the "campaigns are behind" line to `src/Dmc.Cli/Kit/Refresher.cs` (or the shared path of `src/Dmc.Cli/Commands/UpdateCommand.cs` and `src/Dmc.Cli/Commands/RefreshCommand.cs`), counting behind and unfinished campaigns with `StandardRecord` and `MigrationSet` against the freshly written `migrations/`. T051 and T052 pass.

**Checkpoint**: A behind campaign is offered its migration once per resume; an unfinished one holds play; a
current one hears nothing.

---

## Phase 7: User Story 4 - Every change to a file standard ships its migration (P2)

**Goal**: The feedback flow and the development guide require a migration or a stated reason; the PR description
says which; CI runs the golden test (built in T039).

**Independent Test**: Read `review-feedback` step 7, `.docs/development.md` and `CLAUDE.md` § Git and GitHub: each
requires a migration or a reason none is needed; the toolkit test that checks this fails if the requirement is
removed.

- [ ] T056 [P] [US4] Add a prompt check to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` that `.claude/skills/review-feedback/SKILL.md` step 7 and `.docs/development.md` both require a migration in `kit/migrations/` with a golden fixture, or a stated reason none is needed, for any change to a template or a campaign-file rule.
- [ ] T057 [US4] Edit `.claude/skills/review-feedback/SKILL.md` § 7: a change that alters what campaign files should hold adds the next-numbered migration in `kit/migrations/` and its fixture under `tests/Dmc.Tests/Fixtures/migrations/NNN/`, written for a campaign its author has never seen, or says in the PR why none is needed; point to `specs/005-campaign-migrations/contracts/migration-file.md` for the format rather than restating it.
- [ ] T058 [US4] Add to `.docs/development.md` (a subsection under § Writing prompts or § Adding to `dmc`): when a change needs a migration, the numbering, the file format (pointing to the contract), the shape check every migration names, the golden fixture built with `verify --list`, the author's live run (quickstart.md scenario 6), and the PR line.
- [ ] T059 [US4] Edit `CLAUDE.md` § Git and GitHub, Pull requests bullet: the description also says which migration the PR adds, or why none is needed, when it changes a campaign-file standard (FR-015). T056 passes.

---

## Phase 8: User Story 5 - Skills stop carrying catch-up clauses (P3)

**Goal**: The two clauses that migrations 002 and 003 replace leave the skills.

**Independent Test**: Search `kit/skills/` for catch-up wording and find none (SC-008); fixtures 002 and 003 show the
repairs are migrations.

- [ ] T060 [P] [US5] Add a check to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` that no file under `kit/skills/` tells DMC to repair an older campaign file as it goes (patterns such as "if an older", "lacks it", "move it there if it is not"), and update `SheetsEndWithTheirHistoryAndResumeReadsThemDownToIt` if it requires the wording being removed.
- [ ] T061 [US5] Remove "(add the line if an older sheet lacks it)" from step 5 of `kit/skills/dmc-character-sheet-update/SKILL.md` (now migration 003).
- [ ] T062 [US5] Remove "and move it there if it is not" from step 2 of `kit/skills/dmc-campaign-full-save/SKILL.md` (now migration 002), keeping the rule that history is the last section. Search every `kit/` prompt for other catch-up clauses about older campaign files; move any found into a migration or report why it stays. T060 passes.

---

## Phase 9: User Story 6 - An imported old campaign is not silently half-current (P3)

**Goal**: An old export imports unchanged and the import says it is behind; a newer one is reported ahead and
refused for migration.

**Independent Test**: Import an export made at standard 1 and one made at standard 9 into a folder at kit 4: the files
are unchanged, the import prints the behind and ahead lines, and verify refuses the ahead one.

- [ ] T063 [P] [US6] Add cases to `tests/Dmc.Tests/Campaign/CampaignRoundTripTests.cs`: export a campaign with `.standard` = 1 (and one with none), import it under `--as`, and require identical files, the extra line `NAME is behind (standard 1, kit 4): ask DMC to bring it up to date`, and `dmc campaign standard` = `behind: …`; one with `.standard` = 9 imports unchanged and prints `NAME is ahead (standard 9, kit 4): update dmc`, and `dmc campaign verify` on it prints one line and exits 1 without writing `.standard` (FR-018); a current campaign's import prints no extra line.
- [ ] T064 [US6] Add the behind/ahead line to the import path in `src/Dmc.Cli/Campaign/CampaignImporter.cs` (or its caller in `src/Dmc.Cli/Commands/CampaignCommand.cs`), reading the DMC folder's `migrations/` with `MigrationSet` and the new campaign's record with `StandardRecord` (research R12). Confirm `kit/skills/dmc-migrate-campaign/SKILL.md` stops on `ahead` (T037). T063 passes.

---

## Phase 10: Polish and cross-cutting

- [ ] T065 Measure the standing play cost (research R10, Constitution II): the resume's `dmc campaign standard` call and its output on a current campaign, the added sentences in `dmc-resume-campaign`, and `dmc-migrate-campaign`'s description line, with `dmc context-check`'s accounting or the `tests/Dmc.Tests/TokenCost/` method; record the numbers in `.docs/decisions.md`.
- [ ] T066 [P] Update `.docs/user-guide.md`: bringing a campaign up to date (ask DMC; what it asks; backups and the limit of three; what verify proves and its limits from research R5; an interrupted migration; reverting with `dmc campaign restore`); import of an old campaign.
- [ ] T067 [P] Update `.docs/campaign-files.md` (`.standard`, `backups/` and the companion files, what reads and writes each) and `.docs/architecture.md` (the `migrations/` folder, the generated-files row, who does what per research R1).
- [ ] T068 [P] Add the decisions to `.docs/decisions.md`, each with its reason: the standard as a `.standard` integer (R2); migrations numbered on their own (R3); unit-accounting verification, with `--list`, headings as units and the `backups/` exclusion (R5); sizes left to `dmc size`, 006 merging first and this PR adding the `dmc size` call to `dmc-migrate-campaign` (R6); every migration names a shape check (R7); backups pruned after the write on a flag, the question printed by `backups`, and the unfinished mark (R8); restore as its own verb, a plan by default, warning against the verified record (R9); golden fixtures plus an author's live run (R11); import reports a campaign that is not current (R12).
- [ ] T069 [P] Update `.docs/roadmap.md`: the issue #172 row's status and this PR.
- [ ] T070 Check every type and member added or changed under `src/Dmc.Cli/` and `tests/Dmc.Tests/` has an XML doc comment (private ones included) and every new script or workflow a header comment; `dotnet build` with warnings as errors is clean.
- [ ] T071 Run `specs/005-campaign-migrations/quickstart.md` scenarios 1 to 5 by hand against `artifacts/bin/dmc` on Windows, and the full `dotnet test` suite; confirm CI passes on every platform it builds (Windows, Linux x64 and arm64).
- [ ] T072 Open the PR from `005-campaign-migrations`: what changed, why, the sources (issue #172, the spec, research and its amendments), how it was tested (the test classes, the measured verify time against the 5 s goal, the live runs of T049 and T050 and what differed), the documents changed, the migrations it adds (002 to 004), that spec 006 merged first and this PR adds the `dmc size` step (or, if 006 is not in yet, that the step is left to it), and `Closes #172` on its own line. Run `gh pr view <n> --json body` to check the description is there, run the built-in code review once, and present its fixes for approval before committing them.

---

## Dependencies and execution order

### Phase dependencies

- **Setup (Phase 1)**: none. T001 must finish before T041 and T043 (the migration numbers); T002 before anything
  that names `.standard`.
- **Foundational (Phase 2)**: after Setup; blocks every story.
- **US1a (Phase 3)**: after Phase 2.
- **US1 (Phase 4)**: after US1a (verify and restore read backups and their marks; the skill runs `backup`).
- **US2 (Phase 5)**: after US1 (the golden test runs `backup` and `verify`; the fixtures are built with `--list`).
- **US3 (Phase 6)**: after US1a (it reports `unfinished`); can run beside US1 and US2. Its resume line offers
  `dmc-migrate-campaign`, so ship it with US1.
- **Spec 006 merges first** (research R6). Rebase this branch onto `main` once it has, before T053 and T062 edit
  the resume and full-save skills that 006 also changes, and before the `dmc size` step of T037 and the measuring
  of T050.
- **US4 (Phase 7)**: after US2 (it points to the golden test and the fixture layout).
- **US5 (Phase 8)**: after US2 (the clauses leave only once 002 and 003 exist).
- **US6 (Phase 9)**: after US1.
- **Polish (Phase 10)**: after every story; T072 last.

### Within each story

Tests first and failing; then the plain types (`CampaignFiles`, `BackupFolder`, `ContentUnits`, `AccountingFile`);
then the classes that use them; then the verb in `CampaignCommand.cs`; then the prompts. `CampaignCommand.cs` is
edited by T014, T015, T023, T034, T036 and possibly T064, and `ToolkitTests.cs` by T013, T030, T052, T056 and T060,
so those tasks do not run in parallel with each other.

## Parallel examples

```text
# Phase 2, tests and plain types together:
T004 StandardTests.cs   T005 MigrationFilesTests.cs   T006 MigrationFileTests.cs
T008 StandardRecord.cs  T009 MigrationFile.cs   T012 GeneratedFiles.cs   T013 kit/settings.json

# US1a tests together, then the plain types beside the exporter change:
T016 ExportBackupsTests.cs   T017 BackupTests.cs   T018 CampaignImportRefusalTests.cs
T019 CampaignExporter.cs     T020 CampaignFiles.cs   T021 BackupFolder.cs

# US1 tests together, then the two readers:
T025 ContentUnitsTests.cs  T026 AccountingFileTests.cs  T027 VerifyTests.cs  T028 RestoreTests.cs
T029 InterruptedMigrationTests.cs  T030 ToolkitTests.cs
T031 ContentUnits.cs       T032 AccountingFile.cs

# US2: fixtures for 002 and 003 beside the 004 migration text
T042 Fixtures/migrations/002   T044 Fixtures/migrations/003   T045 004-lean-notes-and-state.md

# Polish documents together:
T066 user-guide.md   T067 campaign-files.md, architecture.md   T068 decisions.md   T069 roadmap.md
```

## Implementation strategy

### MVP

Phases 1 to 4 (Setup, Foundational, US1a, US1): the whole back-up, list, verify and restore mechanism, the
unfinished-migration mark and the skill, proven by tests on hand-made campaigns. With no migration files the kit is
at standard 1 and nothing is offered, so the MVP is safe to ship alone, but it does nothing for a player until US2.

### Incremental delivery

1. Phases 1 to 3: backups ship (export and import already safer).
2. Phase 4: verify, restore, the skill.
3. Phase 5: migrations 002 to 004 and the live runs; the feature now delivers the issue's acceptance cases.
4. Phase 6: the player hears about it.
5. Phases 7 to 9: the process rule, the clause removal and the import case.
6. Phase 10: documents, the measured cost, the PR.

All of it ships in one PR (CLAUDE.md: one change per branch, documentation in the same PR), so the increments are
checkpoints with a green build, not separate releases.

## Notes

- [P] = different files, no dependency on an unfinished task.
- Commit at each checkpoint when the user agrees; never on `main`.
- Nothing under `adventures/`, `modules/` or `rules/` is committed; fixtures are built from `kit/templates/`.
