# Feature Specification: Bring Campaign Files Up to the Latest Standard

**Feature Branch**: `005-campaign-migrations`

**Created**: 2026-11-09

**Status**: Draft

**Input**: User description: GitHub issue #172, "Bring campaign files up to the latest standards after any kit
change". The user asked `/speckit-specify` to specify it and to update the issue with what the specification comes
up with.

## Background

`dmc update` rewrites `CLAUDE.md`, the skills and the templates in a DMC folder and, by rule, never touches
`adventures/`. A campaign started under older rules therefore keeps its old structure and its old bulk until DMC
happens to rewrite the affected part during play. Nothing records which standard a campaign's files follow, nothing
says when they have caught up, and nothing checks that catching up was safe.

Until now each change carried its own catch-up clause inside a skill (for example, "add the line if an older sheet
lacks it"). That works for one added line. It does not work for a change that restructures a file, it leaves no
record of which campaign is on which standard, and every clause stays in the skill for good, paid for in every
session.

The 2026-11-07 to 2026-11-09 feedback review is the first change that needs more than a clause. It changes the
standard for three files that resume reads whole. On the maintainer's one long campaign (36 sessions) those files
hold about 12k tokens of material the new rules remove:

- **`story/dm-notes.md`** (94k bytes, at the Read tool's per-call cap, so it was truncated at resume): finished
  fights' Opposition rows are deleted after each fight's story moves to one canon Timeline line; the per-session Mix
  tracker becomes one overwritten line; overtaken plans and finished Arc plan beats go; "About this player" keeps
  only what the player options do not say.
- **`game-state.md`** (27.6k bytes against a 100-line target): history leaves the Scene block, and standing orders
  that are not in play move to canon.
- **the campaign's `player-options.md`**: a sequel gains a Predecessor line naming the earlier campaign's folder.

Two earlier changes are still carried as catch-up clauses and are cases too: the "Names they use" sheet line
(PR #170) and history as the last section of a character sheet.

## Clarifications

### Session 2026-11-09

- Q: How does the player start and run a migration? → A: By asking DMC in a play session. DMC follows the migration
  and calls `dmc` commands for the backup, backup removal, verification and restore; there is no separate terminal
  command that does the whole migration (FR-001).
- Q: When verification finds missing content, leave the files, or offer to revert at once? → A: Name the missing
  items and offer to revert at once; never revert automatically, and do not carry on with play until the player
  chooses to revert or to go on as is (FR-009c).
- Q: Should `restore` take a safety backup of the current state first? → A: No. It warns about changes since the
  backup and asks the player to confirm (FR-005g).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Catch every campaign up in one step (Priority: P1)

A player who has been away for a month runs `dmc update`, opens their campaign, and asks DMC to bring its files up
to the latest standard. DMC says in a line or two what will change, the player confirms, and the campaign's files
are brought up to date in one step, covering every change that has shipped since the campaign was last brought up to
date, whatever each one was. The step always does three things, in this order: **back up** the campaign, **migrate**
all its files to the latest standard, and **verify** that the migration succeeded and lost no data. The player is
told the result and how to restore the backup.

**Why this priority**: This is the whole feature. Without it, a campaign stays on its old standard until DMC
rewrites each part by chance, with no record and no check.

**Independent Test**: Take a fixture campaign aged to the standard of an earlier release, run the migration, and
check that its files match the current standard, that nothing live was lost, that a backup exists, and that running
it again changes nothing.

**Acceptance Scenarios**:

1. **Given** a campaign several releases behind, **When** the player confirms the migration, **Then** every
   migration since the campaign's recorded standard is applied in order and the campaign records that it is current.
2. **Given** a campaign that is already current, **When** the player runs the migration, **Then** DMC says so and
   changes no file.
3. **Given** a campaign that has no recorded standard (every campaign made before this feature), **When** the
   player runs the migration, **Then** it is treated as being on the oldest standard and every migration applies.
4. **Given** several campaigns in `adventures/`, **When** the player runs the migration, **Then** the player can
   bring every campaign up to date in one request, and each one is backed up, confirmed and recorded on its own.
5. **Given** a confirmed migration, **When** it finishes, **Then** a backup taken before anything changed exists,
   the files have been migrated, the verification has run, and DMC tells the player where the backup is and how to
   restore it. The three phases never run out of order: no migration without a backup first, and no "current"
   record without a passing verification.
6. **Given** a migration that drops content the new standard does not need, **When** the player is asked to confirm,
   **Then** the list of what is dropped is shown first, and the adventure's story and state (everything the
   standard still wants) survive in the migrated files.
7. **Given** a migration the player wants to undo, **When** the player runs `dmc campaign restore` and confirms,
   **Then** the campaign is back as it was before the migration (files the migration added are gone), and its
   `backups/` folder is untouched.

---

### User Story 1a - Backups are kept to a limit, and the player decides what goes (Priority: P1)

Each migration takes a new backup, so backups would pile up. The step keeps at most three migration backups of a
campaign and asks the player before removing any. The player may always say no: then the step takes another backup
and carries on. The player is ultimately responsible for their backups.

**Why this priority**: The backup is the safety net of the whole feature, and unbounded backups of a large campaign
fill the player's disk. The rule has to be settled before the first migration runs.

**Independent Test**: Run the migration on a fixture with 0, 2, 3 and 5 earlier backups, answering yes and no to the
removal question, and check the backups left on disk and the order of the phases each time.

**Acceptance Scenarios**:

1. **Given** fewer than three backups of the campaign, **When** the player confirms the migration, **Then** nothing
   is asked about backups: a new backup is taken, then the migration runs, then the verification.
2. **Given** exactly three backups and the player agrees to remove the oldest (DMC names it), **When** the migration
   runs, **Then** a new backup is taken and the oldest is removed once the new one is written and checked, then the
   migration runs, then the verification. Three backups remain.
3. **Given** exactly three backups and the player declines, **When** the migration runs, **Then** nothing is
   removed, a fourth backup is taken, then the migration runs, then the verification.
4. **Given** more than three backups and the player agrees to remove old backups, **When** the migration runs,
   **Then** every backup except the latest two is removed, a new backup is taken (leaving three), then the
   migration runs, then the verification.
5. **Given** more than three backups and the player declines, **When** the migration runs, **Then** nothing is
   removed, another backup is taken, then the migration runs, then the verification.
6. **Given** any answer to the removal question, **When** the step goes on, **Then** the question is asked once per
   migration, before the new backup is taken, and the migration never starts without a new backup.

---

### User Story 2 - The November review's changes migrate the long campaign without losing a fact (Priority: P1)

The maintainer brings the long campaign (36 sessions) up to the standard the November review sets. The three
oversized resume files shrink to their size targets, and every fight, standing order and plan that was still true
keeps a home.

**Why this priority**: These are the acceptance cases named in the issue. The mechanism is only worth building if
it handles the hardest real change, one that restructures files and where the order of steps decides whether a
story is kept or lost.

**Independent Test**: Run the migration on a fixture built to the pre-review standard that holds finished and live
fights, expired and active standing orders, overtaken and current plans, a sequel without a Predecessor line, a
sheet without a "Names they use" line, and a sheet with history in the middle. Check each outcome below.

**Acceptance Scenarios**:

1. **Given** a finished fight with an Opposition row, **When** the migration runs, **Then** the fight's story is
   written as a canon Timeline line with its session and scene reference **before** its row is deleted, so a stop
   at any moment never loses it.
2. **Given** the migrated `dm-notes.md`, **When** it is checked, **Then** no finished fight keeps an Opposition row,
   every finished fight has a Timeline line, and no Opposition row remains for a fight that is not alive.
3. **Given** a per-session Mix tracker, overtaken plans and finished Arc beats, **When** the migration runs, **Then**
   the tracker is one overwritten line and the overtaken and finished items are gone from the resume file.
4. **Given** "About this player" notes that repeat the player options, **When** the migration runs, **Then** only
   what the player options do not say remains.
5. **Given** a `game-state.md` whose Scene block carries history and standing orders that are not in play, **When**
   the migration runs, **Then** the history leaves the Scene block, the standing orders move to canon, and the
   orders in play stay.
6. **Given** a sequel campaign, **When** the migration runs, **Then** its `player-options.md` gains a Predecessor
   line naming the earlier campaign's folder; if the folder cannot be told, DMC asks the player.
7. **Given** a sheet that lacks the "Names they use" line, or keeps history in the middle, **When** the migration
   runs, **Then** the line is added and the history is the last section.

---

### User Story 3 - The player is told when files are behind (Priority: P2)

After `dmc update`, or when a session resumes, DMC says in one line that the campaign's files are behind the
current standard and offers to bring them up to date. A player who declines is not asked again until the next
resume, and play goes on as before.

**Why this priority**: A migration the player never hears about is not used. The offer costs one line.

**Independent Test**: Update the kit under a fixture campaign that is behind, resume it, and check for the one-line
offer; then decline, and check that play continues and that a current campaign gets no line at all.

**Acceptance Scenarios**:

1. **Given** a campaign behind the installed kit, **When** a session resumes, **Then** DMC says so in one line and
   offers the migration.
2. **Given** `dmc update` has just installed a newer kit, **When** it finishes, **Then** it says that campaigns may
   be behind and how to bring them up to date.
3. **Given** a current campaign, **When** a session resumes, **Then** DMC says nothing about migration.
4. **Given** the player declines, **When** play continues, **Then** nothing about the campaign changes.

---

### User Story 4 - Every change to a file standard ships its migration (Priority: P2)

A person tuning DMC from feedback (or making any other toolkit change) alters what campaign files should hold. The
change cannot be finished without a migration written for a campaign the author has never seen. The pull request
says which migration it adds, or why none is needed, and a check runs the migration on a fixture campaign.

**Why this priority**: Without this, the mechanism covers only the first cases and the clauses come back. It makes
migration part of the feedback flow instead of a one-off.

**Independent Test**: Open a change that alters a template without a migration or a stated reason and see the
process refuse it as unfinished; add the migration and see the fixture check pass.

**Acceptance Scenarios**:

1. **Given** a change that alters a file standard, **When** the author follows `review-feedback` step 7 or
   `.docs/development.md`, **Then** the instructions require a migration or a stated reason none is needed.
2. **Given** a pull request that adds a migration, **When** CI runs, **Then** the migration runs on a fixture
   campaign aged to the standard before it and the result is checked.
3. **Given** a pull request description, **When** it is reviewed, **Then** it says which migration it adds, or why
   none is needed.

---

### User Story 5 - Skills stop carrying catch-up clauses (Priority: P3)

Once the mechanism exists, each "add the line if an older sheet lacks it" clause moves into a migration and leaves
its skill, so play stops paying for it in every session.

**Why this priority**: It is the saving that justifies the feature, but it can follow the first release of the
mechanism.

**Independent Test**: Search the skills for catch-up wording and find none; run the migration on a fixture that
lacks the lines and see them added.

**Acceptance Scenarios**:

1. **Given** the shipped kit, **When** its skills are read, **Then** none tells DMC to repair an older campaign's
   file as it goes, and each repair it used to describe is a migration.

---

### User Story 6 - An imported old campaign is not silently half-current (Priority: P3)

A player imports an export made under an older standard. The campaign arrives exactly as exported, records the
standard it follows, and the player is told it is behind.

**Why this priority**: Exports are how campaigns move between computers; they are the other way old files appear.

**Independent Test**: Import an export from a fixture aged to an old standard, and check that the import changes no
content, that the campaign is reported as behind, and that the migration then brings it up to date.

**Acceptance Scenarios**:

1. **Given** an export made under an older standard, **When** it is imported, **Then** the imported files are
   unchanged and the player is told the campaign is behind.
2. **Given** an export from a newer standard than the installed kit knows, **When** it is imported or migrated,
   **Then** DMC says the kit needs updating and changes nothing.

---

### Edge Cases

- **Mid-fight or a roll pending.** The migration refuses to start and says it will wait for a safe moment (resume
  or a planned break).
- **Interrupted migration** (crash, closed window, power loss). The files are never left in a state where a fact
  exists in no file. Running the migration again resumes safely, and the backup is still there.
- **A file edited by hand into an unexpected shape.** The migration does not guess: it leaves that file unchanged,
  tells the player which file and why, and carries on with the files it can migrate, and the campaign is not
  recorded as current until the player resolves it.
- **Verification fails after migrating.** DMC tells the player which check failed, that the backup is untouched,
  and offers to revert; the campaign is not recorded as current, and play waits for the player's choice (FR-009c).
- **Content was reworded, not just moved** (a fight's Opposition row becomes a one-line Timeline entry). Verification
  cannot match it by text alone: the migration says how the old unit maps to the new one, and a unit that cannot be
  matched is shown to the player, not passed silently.
- **The backup is damaged or unreadable.** The step stops before migrating and offers to take another.
- **Backup cannot be written** (disk full, folder not writable). Nothing changes, and no older backup has been
  removed before the new one is written successfully.
- **The player agrees to remove backups but a removal fails.** The step tells the player which backup remains and
  does not stop the migration on that account; the new backup is still taken first.
- **Other files in `backups/`** (a player's own export copied there, a file with another name) are not counted or
  removed; only files that follow the naming rule are.
- **Two backups in the same second.** The second never replaces the first: the step waits or picks the next free
  name, and both count.
- **A campaign export or import after backups exist.** The export leaves `backups/` out, and an import never
  touches the `backups/` folder, so moving a campaign to another computer does not carry its backups. A restore
  leaves the campaign's backups where they are.
- **Restoring after more play.** The campaign has changed since the backup. `restore` says so and the player decides;
  the changes made since are lost (FR-005g).
- **Restoring a backup that is damaged, or from another campaign.** Nothing changes and the one-line error says why.
- **A restore interrupted part-way.** The campaign is left as it was before the restore, or the restore can be run
  again to finish; no file is left half-written.
- **A campaign newer than the kit** (the player installed an older `dmc`). Nothing changes and DMC says the kit
  needs updating.
- **The player declines or says "not now".** Nothing changes and the offer comes back at the next resume.
- **A migration needs a judgment** (is this Opposition row for a finished fight? is this standing order still in
  play?). The judgment is made against the campaign's own files, and anything doubtful is shown to the player rather
  than decided silently.
- **A published-module campaign** and a freeform campaign both migrate; a migration that applies to neither's files
  skips them.
- **Two migrations touch the same file.** They apply in order, each starting from the previous one's result.
- **A very large first migration** (the long campaign's). It still finishes within one session without exhausting
  DMC's context.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The toolkit MUST provide one player-facing step that brings a campaign's files up to the standard of
  the installed kit, covering every change since the campaign was last brought up to date. The player starts it by
  asking DMC in a play session (at resume or a planned break). DMC does the judgment edits itself and uses `dmc`
  commands for the steps that must be exact: the backup, the removal of an old backup the player agreed to, the
  verification and the restore. The player never has to run a separate migration command in their own terminal.
- **FR-002**: Each campaign MUST record which standard its files follow, in a place that travels with the
  campaign (so it survives export and import). A campaign with no record MUST be treated as being on the oldest
  standard.
- **FR-003**: Migrations MUST accumulate across releases and apply in order, so a campaign several releases behind
  ends current. Running the step on a current campaign MUST change nothing and say so.
- **FR-004**: Before changing anything, the step MUST tell the player in a line or two what will change and MUST
  wait for confirmation. The player MUST be able to decline with no effect.
- **FR-005**: The step MUST do three things in this order: back up the campaign, migrate all its files to the
  latest standard, and verify that the migration succeeded and lost no data. Before changing anything it MUST take a
  backup (the existing campaign export is the model), and MUST tell the player where it is and how to restore it. If
  the backup cannot be taken, nothing changes.
- **FR-005a**: The step MUST keep at most three migration backups of a campaign unless the player declines to
  remove any, and MUST ask the player before removing one. With fewer than three, it MUST ask nothing and take a new
  backup. With exactly three, it MUST ask whether the oldest (named) may be removed; on yes it removes that one, on
  no it removes nothing. With more than three, it MUST ask whether old backups may be removed; on yes it removes all
  but the latest two, on no it removes nothing. In every case it takes a new backup, migrates and verifies; a
  backup the player agreed to remove is removed only after the new one is written and checked, so a failed write
  never costs the player a good backup. A "no" MUST NOT stop or change the migration, and the player is responsible
  for the backups they keep.
- **FR-005b**: Backups are counted and removed per campaign, and only the backups the migration took (files in the
  backup folder that follow the naming rule of FR-005c) are counted. A backup MUST NOT be removed without the
  player's yes, and the new backup MUST NOT be skipped for any answer. Removing a backup also removes the record of
  what the migration did with its content (the accounting file kept beside it, see the plan).
- **FR-005c**: A backup MUST be a campaign export, made by the toolkit's existing campaign export, written to the
  `backups/` folder in the campaign's own root folder (`adventures/<campaign>/backups/`, made when needed) and named
  `<campaign>_backup_<yyyyMMddHHmmss>.zip` (local time). The oldest backup is the one with the earliest timestamp in
  its name. A backup MUST never replace an existing file.
- **FR-005d**: A backup MUST NOT contain the `backups/` folder or anything in it, so a backup never holds earlier
  backups and its size does not grow with their number. The same holds for any campaign export. An import MUST NOT
  create, fill, change or remove the `backups/` folder of the campaign it writes.
- **FR-005f**: The toolkit MUST provide a new campaign command, `dmc campaign restore`, that reverts a campaign to a
  backup. The existing `dmc campaign import` is unchanged: it still only creates a campaign that does not exist yet.
  `restore` takes the campaign and a backup (the latest by default), checks the backup whole, tells the player
  what will be replaced, and acts only after the player confirms. It makes the campaign's files exactly the
  backup's, including removing files the migration added, and leaves the campaign's `backups/` folder untouched. It
  is all or nothing: a failure part-way leaves the campaign as it was before the restore began. The player MUST be
  told this is how to revert.
- **FR-005g**: Before replacing anything, `restore` MUST warn the player if the campaign has changed since the backup
  (for example, sessions played after the migration), saying that those changes will be lost, and MUST make that part
  of what the player confirms. `restore` takes no safety backup of the state it replaces, so a restore never
  triggers the backup-limit question of FR-005a.
- **FR-005e**: The existing campaign export MUST be able to write into the campaign's `backups/` folder for this
  purpose. It keeps refusing every other destination inside `adventures/`, and its default behaviour, name and
  location for a player's own export do not change.
- **FR-006**: Material that is still true MUST be written to its new home before its old copy is removed; the order
  of steps is part of each migration. At no point may a fact exist in no file.
- **FR-007**: The step MUST preserve the adventure's content. A standard may drop what it no longer needs (data that
  is not needed, bulk that costs tokens), but only what its migration declares (FR-009b); everything else moves to
  its new home. No file is deleted from `adventures/` except a backup the player agreed to remove (FR-005a) and the
  files a confirmed restore replaces (FR-005f).
- **FR-008**: The step MUST refuse to start mid-fight or with a roll pending, and MUST say it will wait for resume
  or a planned break.
- **FR-009**: After migrating, the step MUST verify the result and report it. The crux is that the adventure's
  content is kept and only reorganized into the new standard, and that nothing is lost except what the migration
  declared it drops. Verification MUST compare the migrated files against the content of the backup taken in phase
  1, and MUST check that every unit of old content (a fact, a row, a line, a section) is either found in the
  migrated campaign or on the migration's declared-dropped list. It MUST also check that every finished fight has a Timeline line, that no Opposition row remains for
  a fight that is not alive. It MUST report each resume file against its size target and flag a shortfall, which
  is not a failure: a migration cannot shrink a file below the live material it holds. A campaign is recorded as
  current only when verification passes.
- **FR-009a**: The toolkit MUST give verification a way to read the files inside a backup without changing the
  campaign or the backup, and MUST check that the backup is whole (its checksums match) before the migration starts,
  so a damaged backup stops the step before anything changes.
- **FR-009b**: A migration MUST declare in advance any old content it drops (rather than moves), and the player is
  shown that list when asked to confirm. Dropping is allowed for content the new standard does not need. Content not
  declared is expected to be found; anything missing that was not declared fails verification. Content that cannot
  be classified is kept, not dropped.
- **FR-009c**: When verification fails, the step MUST name the missing or changed items, leave the campaign not
  recorded as current, tell the player the backup is untouched, and offer at once to revert with
  `dmc campaign restore`, which asks for its own confirmation (FR-005f). DMC MUST NOT revert on its own, and MUST NOT
  carry on with play until the player has chosen to revert or to go on with the campaign as it is.
- **FR-010**: When a file's shape is not one a migration expects, the step MUST leave that file unchanged, say which
  file and why, and MUST NOT record the campaign as current.
- **FR-011**: The step MUST be safe to interrupt and to run again: a repeated run completes the work without
  duplicating or dropping content.
- **FR-012**: A campaign that is behind MUST be flagged to the player in one line after `dmc update` and at resume,
  with an offer to migrate. A current campaign MUST produce no line.
- **FR-013**: A migration that needs judgment MUST state, in the migration itself, how each judgment is made (what
  counts as a finished fight, a standing order in play, an overtaken plan), and MUST show the player anything
  doubtful instead of deciding it silently.
- **FR-014**: The step MUST handle the acceptance cases: the `dm-notes.md`, `game-state.md` and campaign
  `player-options.md` changes of the November review, the "Names they use" sheet line, and history as the last
  section of a sheet.
- **FR-015**: A change that alters a file standard MUST ship its migration, written for a campaign its author has
  never seen. `review-feedback` step 7 and `.docs/development.md` MUST require this, and the pull request
  description MUST say which migration it adds or why none is needed.
- **FR-016**: Each migration MUST be tested on a fixture campaign aged to the standard before it. CI cannot run DMC,
  so the test on CI takes the outcome a correct run leaves (written by the migration's author) and requires that
  verification accepts it, and rejects it when a unit of content is removed or an undeclared drop is claimed; the
  author also runs the migration live on the fixture and says so in the pull request. Fixtures are built from the
  templates, not from real campaigns (which are git-ignored).
- **FR-017**: Once the step exists, the shipped skills MUST NOT carry catch-up clauses for older campaign files; each
  existing one MUST become a migration.
- **FR-018**: Importing an export MUST NOT change its content. It MUST report a campaign that is behind, and MUST
  refuse to migrate one that follows a newer standard than the installed kit.
- **FR-019**: The step MUST NOT add context cost to ordinary play: nothing about migration is loaded in a session
  where no campaign is behind, other than the one-line check at resume.

### Key Entities

- **Standard**: a numbered point in the history of what campaign files should look like. The kit knows the current
  one; each campaign records the one it follows.
- **Migration**: one step from one standard to the next. It says what changes, in what order, how any judgment is
  made, and what a correct result looks like. Migrations accumulate and apply in order.
- **Campaign record**: the campaign's note of the standard it follows, plus the outcome of its last migration.
- **Backup**: a checksummed copy of the campaign taken before migrating, kept as
  `adventures/<campaign>/backups/<campaign>_backup_<yyyyMMddHHmmss>.zip`, with instructions to restore it. At most
  three are kept per campaign unless the player declines to remove any.
- **Fixture campaign**: a campaign built from the templates and aged to an earlier standard, used to test a
  migration without a real campaign.
- **Verification result**: the pass/fail outcome of the post-migration checks, with the names of any content units
  missing from the migrated campaign.
- **Content unit**: the smallest piece of old content verification accounts for (a row, a line, a section); each
  ends in the migrated campaign or on the migration's declared-dropped list.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A player returning after several releases brings every campaign current with one request and one
  confirmation per campaign, with no manual file editing.
- **SC-002**: On the fixture built to the pre-review standard, 100% of finished fights end with a Timeline line and
  0 Opposition rows remain for a fight that is not alive.
- **SC-003**: On the maintainer's long campaign, the material the new rules remove (about 12k tokens across
  `dm-notes.md`, `game-state.md` and the player options) is gone, and the resume files are within their size
  targets or the shortfall is reported.
- **SC-004**: Running the step a second time on a campaign it just migrated changes 0 files.
- **SC-005**: Interrupting the step at any point and running it again ends with the same result as an uninterrupted
  run, and no fact is missing from every file.
- **SC-006**: 100% of pull requests that change a file standard either add a migration with a passing fixture test
  or state why none is needed.
- **SC-007**: A current campaign sees 0 lines about migration at resume, and a behind campaign sees exactly one.
- **SC-008**: The skills contain 0 catch-up clauses for older campaign files once the cases are migrations.
- **SC-010**: With 0, 2, 3 and 5 earlier backups and both answers to the removal question, the backups left on
  disk are exactly those FR-005a gives, and a new backup exists before every migration.
- **SC-011**: 0 backups are removed without the player's yes, and 0 files outside `backups/` that follow the
  naming rule are removed.
- **SC-013**: On the fixture, 100% of the old content units are found in the migrated campaign (new place or
  declared-dropped list), and removing one unit from the migrated fixture makes verification fail and
  name it.
- **SC-012**: A backup made with 5 earlier backups present is no larger than one made with none, to within the
  campaign's own changes.
- **SC-009**: A player can restore the pre-migration state from the backup using only what DMC told them, in one
  command and one confirmation, and the restored campaign matches the backup file for file with its `backups/`
  folder unchanged.

## Assumptions

- A campaign made before this feature has no record and is treated as being on the oldest standard; the first
  migration after it is the November review's.
- "Out of date" is decided by comparing the campaign's recorded standard with the installed kit's, not by inspecting
  file contents.
- The existing campaign export (`dmc campaign export`, a checksummed ZIP; there is no separate export skill) is the
  backup, and reverting uses a new `dmc campaign restore` (FR-005f), not import, which keeps its rule of never
  changing an existing campaign. The user fixed the backup location (`adventures/<campaign>/backups/`) and name
  (`<campaign>_backup_<yyyyMMddHHmmss>.zip`), which differ from the export's own default (`exports/`,
  `NAME-YYYYMMDDHHmm.zip`) and from its refusal to write inside `adventures/`; FR-005c to FR-005e change the export
  only as far as that needs.
- The limit of three, removing the oldest at exactly three, and keeping the latest two when there are more come from
  the player's rule for this feature. No answer is remembered: a player who declined is asked again at the next
  migration.
- Removing a backup is the one deletion the step may do, and only with the player's yes. Backups sit inside the
  campaign's folder, so this is a stated exception to "nothing is deleted from `adventures/`" (FR-007), limited to
  files that follow the backup naming rule. How a removal is carried out, given that DMC cannot run deletes itself in
  a play session (see `.docs/user-guide.md`), is a decision for the plan.
- The planned size check (`dmc size`, filed separately) is the natural source of the size verification; if it
  is not available yet, the step uses a line count against the existing targets, and the spec's checks do not
  depend on its being built first.
- "No data lost" means the adventure's content is preserved, as the user clarified. A standard may remove data and
  tokens it does not need (the issue's "material the new rules remove"), but only what its migration declares and
  the player sees (FR-009b); the backup keeps the old copy. Example layout: `D:\Claude\dmc\adventures\rorns-landing\backups\` holds the backups of that
  campaign.
- The player migrates with their consent and at a safe moment; there is no automatic migration on update.
- "Every campaign" means the campaigns in the player's `adventures/`; campaigns that live elsewhere are migrated by
  importing them first.
- Where the standard is recorded, how migrations are stored and numbered (tied to releases or on their own), the
  form of the fixture campaign, and how DMC removes a backup (it cannot run deletes itself in a play session, so a
  `dmc` command is the likely route) are decisions for `/speckit-plan`. The issue lists them as open questions, and
  this spec does not settle them. Who does what is settled (FR-001): DMC does the judgment, `dmc` does the exact steps.
- A migration's judgments are made by DMC against the campaign's own files and shown to the player when doubtful;
  they are not guaranteed correct, which is why the backup and the verification exist.
- Documentation, the roadmap, decisions, `review-feedback` and `.docs/development.md` change in the same pull
  request, as `CLAUDE.md` requires.
