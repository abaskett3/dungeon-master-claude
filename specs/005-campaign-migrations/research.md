# Research: Bring Campaign Files Up to the Latest Standard

Each entry is **Decision / Rationale / Alternatives**. Entries R1 to R3 settle the open questions of issue #172; the
rest settle what the spec left to the plan. The code facts were read on 2026-11-09 from `main` after PR #173.

## R1. Who does the work: DMC follows a migration, `dmc` does the exact steps

- **Decision.** A migration is a Markdown file that DMC reads and follows in a play session (spec FR-001, the
  clarification of 2026-11-09). `dmc campaign` gains the verbs for what must be exact and testable: `standard`
  (where a campaign is), `backups` (list), `backup` (take one, optionally pruning), `verify` (the no-loss check) and
  `restore`. A new skill, `dmc-migrate-campaign`, is the procedure that strings them together and asks the player.
- **Rationale.** The judgment (which Opposition rows are finished, which standing orders are in play, how a fight
  becomes a Timeline line) is prose work only DMC can do, and the issue says the first case needs it. Everything that
  can be exact is a `dmc` command, so CI tests it. DMC may run `dmc *` in play (`kit/settings.json` allows
  `Bash(dmc *)`), but not `dmc update`, `install` or `refresh`, so the new verbs are reachable without a settings
  change.
- **Alternatives.** A terminal-only `dmc campaign migrate` that does everything: rejected by the user, and it could
  only do mechanical edits. A skill with no `dmc` support: the backup, the retention rule, the no-loss check and the
  restore would all be prose, so nothing would test them.

## R2. Where the standard is recorded: `adventures/<campaign>/.standard`

- **Decision.** One file, `.standard`, in the campaign's root folder, holding a single integer and a newline (for
  example `4`). No file means standard 1, the baseline. `dmc campaign verify` writes it on a pass;
  `dmc campaign standard NAME --init` writes it for a brand-new campaign (the `dmc-new-campaign` skill runs it after
  copying the templates). Nothing else writes it.
- **Rationale.** It lives with the campaign, so an export carries it (the exporter walks every file, dotfiles
  included; `ArchivePaths.Refusal` is checked in T002 to confirm a leading dot passes). The player never reads it, so
  it costs no context and a player's edit of `game-state.md` or `player-options.md` cannot corrupt it. Only the
  command that has just verified the result sets it, so DMC cannot mark a campaign current by hand.
- **Alternatives.** A line in `game-state.md`: read every resume and rewritten at every save, so it would cost tokens
  and drift. A line in the campaign's `player-options.md`: the player edits that file and it is the file the November
  migration itself changes. A new `.dmc` JSON next to `.active-campaign` in `adventures/`: not part of the campaign,
  so it would not travel through export and import.
- **Fallback.** If a leading dot proves a problem on some platform (T002), name it `standard.txt`; nothing else in
  the design depends on the name.

## R3. Migrations are numbered on their own, stored in `migrations/`

- **Decision.** A *standard* is an integer. Standard 1 is the baseline: what a campaign with no `.standard` is
  assumed to follow. Migration `NNN` takes a campaign from standard `NNN-1` to `NNN`; it is the file
  `kit/migrations/NNN-<slug>.md`, installed to `migrations/NNN-<slug>.md` in a DMC folder by `dmc install`,
  `refresh`, `play` and `update` (one new row in `GeneratedFiles.Table`: `("kit/migrations/", "migrations/")`). The
  kit's current standard is the highest `NNN` present (1 with none). Numbers are contiguous; a test checks it.
- **Rationale.** A release may carry no migration or several, so tying the number to a release would leave gaps and
  force a bump on every release. A migration is added in the same PR that changes the standard, and its number is
  the next integer, so two PRs that add one conflict on the file name and are caught by merge. `migrations/` is
  read-only for DMC like `templates/` (settings deny list, R8) and is not a player folder.
- **Alternatives.** Version-keyed (`0.1.5.md`): gaps, and a dev build says `0.0.0-dev`. Dates: two on one day, and
  no order between a rebase and a merge. Embedding the migrations only in the program: DMC could not read them with
  the Read tool.
- **The shipped cases.** The baseline is "before the first carried clause". Migrations 2 to 4, in the order they
  shipped: **002** history is the last section of a sheet, **003** the "Names they use" line (PR #170), **004** the
  November review (PR #173). Which PR shipped history-last is found in T001 (`git log -S` on the sheet templates)
  and the order fixed there. A campaign created after those PRs but before this feature has no `.standard`, so it
  runs 002 to 004 on files that are already current: **every migration must be idempotent**, which FR-011 already
  requires and the migration contract states (contracts/migration-file.md).

## R4. The shape of a migration, and what `dmc` reads from it

- **Decision.** Front matter that `dmc` parses (a few `key: value` and `key: [a, b]` lines; a hand-written reader,
  no YAML library, to stay AOT-clean), then prose DMC follows. The front matter holds `standard`, `title`, `shipped`
  (the PR), `touches` (files), `drops` (the classes of content it may drop, FR-009b) and `checks` (names of built-in
  checks, at least one: the shape the migration produces). It holds no size targets (R6). The prose has fixed headings: What changes (shown to the
  player), Steps in order, Judgments, Declared drops, Done when. Full format in contracts/migration-file.md.
- **Rationale.** `verify` has to know which drop classes are allowed and which checks to run without parsing prose.
  Everything else is for DMC and the player.
- **Alternatives.** A separate machine file per migration: two files to keep in step. Putting drop classes only in
  prose: `verify` could not enforce FR-009b.

## R5. Verification: account for every unit of old content

- **Decision.** `dmc campaign verify NAME` compares the campaign with the backup it names (the latest by default)
  and passes only when four things hold:
  1. **The backup is whole.** Its manifest and checksums are read the way `import` reads them (`ExportSummary` and
     the same extraction check), before anything is compared. This is also run at the start by `backup`.
  2. **Every old content unit is matched or accounted for.** The campaign's files are every file of the campaign
     folder except its top-level `backups/` folder and `.standard`, on both sides: the accounting file, the zips and
     the record are never content, so an anchor or a pasted line cannot match itself. The content units of a file are
     its non-blank lines after normalisation (data-model.md § Content unit): trimmed, runs of whitespace collapsed, a
     leading list or quote marker removed, a heading's leading `#`s removed; table separator rows, horizontal rules
     and HTML comments skipped (structure, not content). Headings are units, because in campaign files they carry
     names, places and session titles. A file whose bytes are identical in the campaign and the backup is matched
     without reading its units. Any other old unit is *matched* if its normalised text equals a normalised line of
     any file of the migrated campaign, or, for a unit of at least 20 characters, is contained in one. A unit that is
     not matched is *unaccounted*.
  3. **Every unaccounted unit is accounted for in the accounting file** that DMC writes: a short id (the first 12 hex
     digits of the unit's SHA-256; two different texts with one id in a run fail verification), a disposition and a
     detail. `reworded` needs an anchor (text of at least 8 characters) that must occur in the migrated campaign;
     `dropped:<class>` needs a class from the `drops` of the pending migrations. A `skipped` line names a file DMC
     left unmigrated (FR-010) and always fails the run. Anything else fails.
  4. **The migration's `checks` pass.** Each migration names at least one check for the shape it produces (R7), so a
     file left in the old shape fails even if DMC did not write a `skipped` line.
  On a pass it writes `.standard` (R2), writes `<backup name>.verified.tsv` (R9), removes the backup's
  `.migrating` mark (R8) and prints one line. On a failure it prints the first 40 unaccounted units (id, file, 60
  characters), `--all` prints them all, and it exits 1 with a one-line message.
- **`--list`.** DMC cannot compute the ids, so `verify --list` runs steps 1 to 3 against the accounting written so
  far and prints the units still unaccounted (same lines and cap as a failure), writing nothing and exiting 0. The
  skill runs migrate, `verify --list`, write the accounting, then `verify`; only that last run's failure is a
  failure to the player (FR-009c). The fixture's `accounting.tsv` is built the same way.
- **Rationale.** It makes "no data lost" checkable without understanding prose: what it cannot prove (that a
  reworded line says the same thing) it forces DMC to state per unit, and it limits the allowed drops to what the
  migration declared and the player was shown. A 94k `dm-notes.md` is about 800 units, most matched verbatim, so
  DMC accounts only for what changed.
- **Limits, to state in the user guide.** Verification proves nothing was lost *verbatim or by a stated mapping*; it
  cannot prove a rewording kept the meaning. A unit contained in a longer line counts as matched, so a short fact
  merged into an unrelated line could pass; the 20-character floor and the player-shown drop list keep that rare.
- **Alternatives.** Diffing files: reorganised files diff as total change. A model-only check ("read both, say if
  anything is missing"): untestable and the very failure the feature exists to prevent. Hashing paragraphs: too
  coarse, a one-word edit hides a unit.

## R6. Sizes are left to `dmc size`

- **Decision.** Neither a migration nor `verify` holds or reports size targets. The targets have one home, `dmc size`
  (issue #174, spec `specs/006-dmc-size`), which measures in tokens. Whichever of this feature and spec 006 merges
  second adds one step to the end of `dmc-migrate-campaign`: after a pass, run the play form of `dmc size` and say
  each line it prints as a shortfall, not a failure. Until both are in, a migration reports no sizes.
- **Merge order (decided 2026-11-10).** Spec 006 merges first. This branch rebases onto `main` once 006 has merged,
  before the skill edits of story 3, and adds the `dmc size` step itself (T037). Either order would work; 006 goes
  first because it is smaller and nearer done, and so that the live run on the long campaign (T050) measures with
  `dmc size`. If 006 is not merged when this PR is otherwise ready, it is not held back: the PR leaves the step out
  and says so, and 006 adds it.
- **Rationale.** The line targets live in `dmc-campaign-full-save`, and spec 006 is moving them to token budgets; a
  copy in a migration's front matter would be a second home (Constitution III) and would go stale when 006 lands. A
  migration cannot shrink a file below the live material it holds without dropping something still true, which would
  break R5, so a size over target is never a failure.
- **Alternatives.** A `size` map of line targets in the front matter (the first plan): a copied number. One shared
  constant in `src/Dmc.Cli`: forces the home of 006's budgets into this feature. Waiting for `dmc size` to merge
  first: blocks this feature on another.

## R7. Built-in checks

- **Decision.** Every migration names at least one check for the shape it produces, so a file left in the old shape
  fails verification whatever DMC wrote (FR-010). Migration 002 names `sheets-end-with-history` (the last `## `
  section of every `character-sheets/pc-*.md` and `npc-*.md` is `## History`, or the older `## Notes` / `## History
  with the PC`); 003 names `npc-sheets-have-names-line` (every `character-sheets/npc-*.md` has the template's
  "Names they use" line). Migration 004 names two checks, implemented in `Campaign/MigrationChecks.cs`:
  `no-dead-opposition-rows` (in `story/dm-notes.md`, under `## Opposition`, no table row whose second column starts
  `0 /`, the "alive / started" form of the template) and `fights-have-timeline` (every old row that was dead in the
  backup and is unaccounted has a `reworded` accounting entry whose anchor is a line under `## Timeline` in
  `story/canon.md` that carries an `sN.M` reference). A check is a small named function; adding one is a code change
  with its own test, so a migration can only name checks that exist (a test checks that).
- **Rationale.** These are the two checks the issue names. Both rest on formats the templates already fix, not on
  prose.
- **Risk.** A hand-edited Opposition table that no longer matches the template makes the first check pass vacuously.
  The check reports "no Opposition table found" as a failure rather than a pass, which is the FR-010 behaviour.

## R8. Backups: where, how many, how pruned

- **Decision.** `dmc campaign backup NAME` runs the existing exporter with a fixed destination:
  `adventures/<NAME>/backups/<NAME>_backup_<yyyyMMddHHmmss>.zip` (local time; the next free `-2` suffix before
  `.zip` on a clash, which still counts). It then checks the archive whole. The exporter changes in two places
  (FR-005d, FR-005e): `Walk` skips the top-level `backups/` folder of the campaign it reads, and `Plan` allows a
  destination inside `adventures/` only when it is inside that campaign's own `backups/`. `--prune oldest` and
  `--prune old` remove, *after* the new backup is written and checked, the oldest backup or all but the latest two
  old ones (so three remain), together with each removed backup's companion files (`.accounting.tsv`,
  `.verified.tsv`, `.migrating`). `dmc campaign backups NAME` lists the backups oldest first and prints their count
  and, when a removal question is due, one `ask:` line holding the question and the flag a yes allows (`ask: may the
  oldest backup, <file>, be removed? yes: --prune oldest` at exactly three; `ask: may the <k> oldest backups be
  removed, keeping the latest two? yes: --prune old` above three; nothing below three). The skill relays that line
  and holds none of the numbers, so the whole retention rule lives in `BackupFolder.cs`. Only files that match
  `<NAME>_backup_<14 digits>(-N)?.zip` are counted or removed.
- **Unfinished migrations.** `backup` writes `<backup name>.migrating` beside the new backup. A passing `verify` and
  any `restore` remove it. While one exists, `dmc campaign standard` prints `unfinished: <backup file>` instead of
  `behind`, `backups` asks no question, and `backup` refuses (`NAME has an unfinished migration from <file>: finish
  it or restore it`), so a rerun can never back up half-migrated files. `verify` and `restore` default to the marked
  backup. A rerun takes no backup and repeats the idempotent steps against that backup (FR-011). Comparing the
  backup's standard with the campaign's instead of writing a mark was rejected: after a restore and more play it
  would pick a backup that lacks that play.
- **Rationale.** Pruning after the write keeps the player's last good backup if the disk is full, and ends with the
  same set the spec's order (remove, then back up) gives. The command removes only when asked by a flag that DMC
  passes only after the player's yes, so the retention rule is code, not prose, and DMC needs no `rm` (blocked in
  play). Counting by exact name leaves any other file in `backups/` alone.
- **Alternatives.** Prune first: loses the old backup when the new write fails. Let DMC delete: `rm` is denied in play.

## R9. Restore: `dmc campaign restore`, a plan by default

- **Decision.** `dmc campaign restore NAME [--from FILE] [--yes]`. `--from` defaults to the marked backup of an
  unfinished migration (R8), else the latest in `backups/`. Without `--yes` it checks the backup whole, prints what
  would change (files replaced, removed and added, and a warning line when the campaign has changed since the
  migration was verified), and changes nothing. The warning compares the campaign with `<backup name>.verified.tsv`,
  the path and SHA-256 of every campaign file that a passing `verify` writes, so it names play after the migration,
  not the migration's own changes (FR-005g). With no such file (verify never passed, or the backup was not a
  migration's), it compares with the backup and says "since the backup". The walk skips `backups/` and `.standard`,
  as R5 does. A restore removes every `.migrating` mark of the campaign. With `--yes` it extracts the backup into a staging folder (`adventures/.restore-<id>/`, the `.`-prefix the
  importer already uses for its own staging), moves the campaign's `backups/` folder into the staging, renames the
  campaign aside to `adventures/.replaced-<NAME>-<id>`, renames the staging into place, then deletes the aside. If
  anything fails before the rename into place, the campaign is put back; a run that finds a `.replaced-<NAME>-*`
  folder and no campaign folder puts it back before doing anything else. `import` is unchanged.
- **Rationale.** The confirmation DMC relays from the player becomes an explicit flag, so the command is safe to call
  from a session and testable. Renames are atomic on one volume; the staging and the campaign share `adventures/`.
  The importer's extraction and checksum code is reused, not copied.
- **Alternatives.** Overwrite in place: a failure mid-way leaves a mixture. Prompting on stdin: DMC runs commands
  without a terminal.
- **Windows.** A rename can fail while a file is open (an open map page, an editor). The command reports the one
  file and leaves the campaign as it was.

## R10. Telling the player a campaign is behind

- **Decision.** (a) `dmc campaign standard [NAME]` prints one line: `current` for a campaign on the kit's standard,
  or `behind: standard 1, kit 4: 002-..., 003-..., 004-...`, or `ahead: ...` for a campaign newer than the kit
  (spec edge case), or `unfinished: <backup file>` (R8). `dmc-resume-campaign` runs it in step 1 and says one line
  only when it is not `current`; on `unfinished`, play waits until the player finishes the migration or restores
  the backup (FR-011).
  (b) `dmc update` and `dmc refresh` print one extra line when any campaign of the refreshed folder is behind.
- **Cost.** The resume gains one `dmc` call of about 60 tokens and one sentence of skill text. The new skill's
  description is one line in every session. Both are measured in T065 with `dmc context-check`'s accounting and
  recorded in decisions.md; Constitution II is the gate.
- **Alternatives.** A SessionStart hook that runs the check: it would print on every start, current or not, and a
  hook that fails is silent. Putting the check in `dmc-resume-campaign` prose only: DMC would have to compare
  numbers itself.

## R11. Testing a migration on CI without a language model

- **Decision.** CI cannot run DMC. Each migration therefore ships a **golden fixture**:
  `tests/Dmc.Tests/Fixtures/migrations/NNN/before/` (a small campaign built to standard `NNN-1`, from the templates
  plus the shapes the migration touches), `after/` (what a correct run leaves, written by the migration's author)
  and `accounting.tsv`. One test, over every migration in `kit/migrations/`, builds a temp DMC folder, imports
  `before` as the campaign, backs it up with the real `backup`, replaces the campaign with `after`, and requires
  `verify` with the accounting to **pass** and the campaign to be recorded as standard `NNN`. A second test removes
  one unit from `after` and requires `verify` to **fail naming it**; a third replaces a drop class with one the
  migration did not declare and requires a fail. A fixture-less migration fails the build.
- **What this proves, and what it does not.** It proves the migration's declared drops, checks and accounting are
  consistent with a correct outcome, and that the commands behave. It does not prove that a model following the prose
  produces that outcome. That is covered by an **author's live run** (`quickstart.md`, scenario 6): run the
  migration on the `before` fixture in a scratch DMC folder with `dmc play` and compare. The PR description records
  that it was done. This is a spec amendment (A4 below): FR-016's "run on a fixture campaign" is the golden test on CI
  plus the author's live run.
- **Alternatives.** A headless `claude -p` run on CI: cost, no key on forks, and non-deterministic results make a
  flaky required check. Skipping CI tests: nothing would keep migrations honest.

## R12. Import of an old export

- **Decision.** `import` changes in two ways. It refuses an archive that holds a `backups/` entry (the exporter
  leaves it out, so only a crafted file has one). After creating the campaign it prints one extra line when the
  campaign is not current, reading the DMC folder's `migrations/`: `NAME is behind (standard C, kit K): ask DMC to
  bring it up to date`, or `NAME is ahead (standard C, kit K): update dmc`; nothing when current. `.standard`
  travels inside the archive, and the imported files are unchanged.
- **Rationale.** Spec FR-018 requires the import to report it. The player who imports and does not resume at once
  would otherwise hear nothing. `import` already runs in a DMC folder, so reading `migrations/` costs little.

## R13. Settings and the player's folders

- **Decision.** Add `Edit(/migrations/**)` and `Write(/migrations/**)` to `kit/settings.json`'s deny list, beside
  `/templates/**`. `migrations/` is a generated folder, not a player folder, so `GeneratedFiles.Guard` needs no
  change. `.standard` is inside `adventures/`, which DMC may edit, but the skill tells DMC not to.
- **Rationale.** A migration DMC could edit would let a session rewrite the standard it is judged against.

## Spec amendments this plan makes

- **A1.** FR-005a, story 1a scenarios 2 to 5: the end state is the spec's (three backups, or one more if the player
  declined), but the order is write, check, then prune (R8). Wording changed to say so.
- **A2.** FR-005b: removing a backup also removes its sibling `<backup name>.accounting.tsv`.
- **A3.** FR-009: size targets are reported as a shortfall, not a failure, and by `dmc size`, not by `verify` (R6,
  revised by the analysis of 2026-11-10).
- **A4.** FR-016: "run on a fixture campaign" means the golden test on CI plus an author's live run (R11).

The analysis of 2026-11-10 (`/speckit-analyze`) made these further amendments, each approved by the user:

- **A5.** FR-009 and the `dmc size` assumption: sizes come from `dmc size`, added by whichever of 005 and 006 merges
  second; SC-003 is checked on a copy of the long campaign (R6).
- **A6.** FR-005b: a removed backup takes all its companion files with it (R8).
- **A7.** FR-005g: the warning is about changes since the migration was verified (R9).
- **A8.** FR-011: a rerun verifies against the first attempt's backup, and play waits on an unfinished migration
  (R8, R10).
- **A9.** FR-009: verification also checks each migrated file has its migration's shape (R7).
