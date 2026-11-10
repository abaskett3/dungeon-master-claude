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
  included; `ArchivePaths.Refusal` is checked in T012 to confirm a leading dot passes). The player never reads it, so
  it costs no context and a player's edit of `game-state.md` or `player-options.md` cannot corrupt it. Only the
  command that has just verified the result sets it, so DMC cannot mark a campaign current by hand.
- **Alternatives.** A line in `game-state.md`: read every resume and rewritten at every save, so it would cost tokens
  and drift. A line in the campaign's `player-options.md`: the player edits that file and it is the file the November
  migration itself changes. A new `.dmc` JSON next to `.active-campaign` in `adventures/`: not part of the campaign,
  so it would not travel through export and import.
- **Fallback.** If a leading dot proves a problem on some platform (T012), name it `standard.txt`; nothing else in
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
  November review (PR #173). Which PR shipped history-last is found in T020 (`git log -S` on the sheet templates)
  and the order fixed there. A campaign created after those PRs but before this feature has no `.standard`, so it
  runs 002 to 004 on files that are already current: **every migration must be idempotent**, which FR-011 already
  requires and the migration contract states (contracts/migration-file.md).

## R4. The shape of a migration, and what `dmc` reads from it

- **Decision.** Front matter that `dmc` parses (a few `key: value` and `key: [a, b]` lines; a hand-written reader,
  no YAML library, to stay AOT-clean), then prose DMC follows. The front matter holds `standard`, `title`, `shipped`
  (the PR), `touches` (files), `drops` (the classes of content it may drop, FR-009b), `checks` (names of built-in
  checks) and `size` (line targets for the resume files). The prose has fixed headings: What changes (shown to the
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
  2. **Every old content unit is matched or accounted for.** The content units of a file are its non-blank lines
     after normalisation (contracts/accounting-file.md): trimmed, runs of whitespace collapsed, a leading list or quote
     marker removed, table separator rows, headings, rules and HTML comments skipped (structure, not content). A file
     whose bytes are identical in the campaign and the backup is matched without reading its units. Any other old
     unit is *matched* if its normalised text equals a normalised line of any file of the migrated campaign, or, for a
     unit of at least 20 characters, is contained in one. A unit that is not matched is *unaccounted*.
  3. **Every unaccounted unit is accounted for in the accounting file** that DMC writes: a short id (the first 8 hex
     digits of the unit's SHA-256), a disposition and a detail. `reworded` needs an anchor (text of at least 8
     characters) that must occur in the migrated campaign; `dropped:<class>` needs a class from the `drops` of the
     pending migrations. Anything else fails.
  4. **The migration's `checks` pass**, and the file sizes are **reported** against the migration's `size` targets
     (R6).
  On a pass it writes `.standard` (R2) and prints one line. On a failure it prints the first 40 unaccounted units
  (id, file, 60 characters), `--all` prints them all, and it exits 1 with a one-line message.
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

## R6. Size targets are reported, not enforced

- **Decision.** `verify` counts the lines of each resume file named in the migration's `size` and prints them against
  the targets (`game-state.md 112/100`). A file over target is a **shortfall** line, not a failure. The planned
  `dmc size` (issue #174, spec `specs/006-dmc-size`) is a token measure; when it exists the migration may switch to
  it, and nothing here waits for it.
- **Rationale.** A migration cannot shrink a file below the live material it holds without dropping something that
  is still true, which would break R5. The decisions entry "Resume and full save are bounded" already calls the
  targets guesses. The spec's SC-003 says "within targets or the shortfall is reported"; this makes FR-009 agree
  (a spec amendment, A3 below).
- **Alternatives.** Fail over target: a long campaign could never be marked current. Wait for `dmc size`: blocks
  this feature on another.

## R7. Built-in checks for the November migration

- **Decision.** Two checks, implemented in `Campaign/MigrationChecks.cs` and named in migration 004's `checks`:
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
  old ones (so three remain), together with each removed backup's `.accounting.tsv`. `dmc campaign backups NAME`
  lists the backups oldest first and prints their count, so DMC can ask the player before it prunes. Only files that
  match `<NAME>_backup_<14 digits>(-N)?.zip` are counted or removed.
- **Rationale.** Pruning after the write keeps the player's last good backup if the disk is full, and ends with the
  same set the spec's order (remove, then back up) gives. The command removes only when asked by a flag that DMC
  passes only after the player's yes, so the retention rule is code, not prose, and DMC needs no `rm` (blocked in
  play). Counting by exact name leaves any other file in `backups/` alone.
- **Alternatives.** Prune first: loses the old backup when the new write fails. Let DMC delete: `rm` is denied in play.

## R9. Restore: `dmc campaign restore`, a plan by default

- **Decision.** `dmc campaign restore NAME [--from FILE] [--yes]`. `--from` defaults to the latest backup in
  `backups/`. Without `--yes` it checks the backup whole, prints what would change (files replaced, removed and
  added, and a warning line when the campaign has files newer than the backup or different from it), and changes
  nothing. With `--yes` it extracts the backup into a staging folder (`adventures/.restore-<id>/`, the `.`-prefix the
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
  (spec edge case). `dmc-resume-campaign` runs it in step 1 and says one line only when behind.
  (b) `dmc update` and `dmc refresh` print one extra line when any campaign of the refreshed folder is behind.
- **Cost.** The resume gains one `dmc` call of about 60 tokens and one sentence of skill text. The new skill's
  description is one line in every session. Both are measured in T036 with `dmc context-check`'s accounting and
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

- **Decision.** `import` is unchanged except two lines of safety: it refuses an archive that holds a `backups/` entry
  (it would be ignored by the exporter, so only a crafted file has one), and after creating the campaign it prints
  nothing extra; the resume check (R10) reports the campaign as behind. `.standard` travels inside the archive.
- **Rationale.** Spec FR-018. Printing a "behind" line from `import` would need the migrations folder of a DMC
  folder `import` otherwise does not read; the resume check already covers it.

## R13. Settings and the player's folders

- **Decision.** Add `Edit(/migrations/**)` and `Write(/migrations/**)` to `kit/settings.json`'s deny list, beside
  `/templates/**`. `migrations/` is a generated folder, not a player folder, so `GeneratedFiles.Guard` needs no
  change. `.standard` is inside `adventures/`, which DMC may edit, but the skill tells DMC not to.
- **Rationale.** A migration DMC could edit would let a session rewrite the standard it is judged against.

## Spec amendments this plan makes

- **A1.** FR-005a, story 1a scenarios 2 to 5: the end state is the spec's (three backups, or one more if the player
  declined), but the order is write, check, then prune (R8). Wording changed to say so.
- **A2.** FR-005b: removing a backup also removes its sibling `<backup name>.accounting.tsv`.
- **A3.** FR-009: size targets are reported as a shortfall, not a failure (R6).
- **A4.** FR-016: "run on a fixture campaign" means the golden test on CI plus an author's live run (R11).
