# Feature Specification: Close a Campaign for Good

**Feature Branch**: `010-campaign-close`

**Created**: 2026-11-10

**Status**: Draft

**Input**: User description: "Issue #180: a formal, permanent close for a campaign. Closed is closed for good, with no
reopening; the only way back to its story is a sequel (#179 Phase B). Only the player closes a campaign and DMC confirms
first; DMC may suggest closing when the story has ended but never closes on its own. Closing does four things. (1) A
closing full save: a normal full save, except canon is not trimmed to the archive, a sweep records the why and how
behind each fact, each DM secret is marked revealed (with its session), never revealed or abandoned, and loose ends are
written as facts in the character's terms. (2) An ending record: how it ended, where everyone is, each character's final
level and condition, and the in-world date. (3) A closed mark that dmc and resume can read: resume says the campaign is
closed, opens no scene and offers a sequel; dmc campaign standard reports it as closed and resume never offers to
migrate it; export, show and verify still work. (4) A closing backup: a checked export that --prune never counts or
removes, plus a second copy outside the campaign folder. A closed campaign's files are never changed again by play, a
sequel or a migration. No migration: the one campaign ended by hand is closed once, by hand, after merging. The open
questions are in the issue: a new skill or a mode of the full save; where the mark, the ending record and the secrets
account live; where the second copy goes; whether the backup carries the module's digests; and what to do about a close
made by mistake."

## Background

Nothing in the kit's rules, skills or templates says what ending a campaign means. Issue #180 measured the maintainer's
play folder, where one campaign (The Silver Egg, 46 sessions) was ended by hand and a sequel (Rorn's Landing, 39
sessions) continues it:

- **The ending is prose only.** `game-state.md` and `summary.md` say the campaign is closed. No program and no later
  session can test that, and resume would open a scene in it.
- **It has no closing backup.** `dmc campaign backup` was built for migrations: it writes into the campaign's own
  `backups/` folder, and `--prune` cuts the counted backups down to the latest two.
- **The full save is aimed at the next session of the same campaign.** Its canon step moves "subjects that can no longer
  come up" to the archive, and at an ending that is nearly everything. That is how the reasons behind a fact ended up in
  the least-read place: for one carried character's central trait, 4 lines were in `canon.md` and 21 in
  `canon-archive.md`, and the sequel then contradicted it (feedback 2026-11-09).
- **The hand-made ending worked.** The player asked what was left "to close this bad boy out", played a short epilogue,
  and the deferred threads went into the notes ("first thaw: the visit, then the build"). This spec makes that shape
  standard.

Issue #179 Phase B (the sequel import: a brief and a digest of the closed predecessor) depends on this change: its proof
that nothing was lost is checked against the closing backup, and its digest can never go stale only because a closed
campaign never changes.

### What is already decided, and where

- **Closed is closed forever; sequels only.** The user, 2026-11-10: "My choice is when a campaign is closed its closed
  forever. Sequels only." Recorded as the issue's Decision.
- **Ending a campaign is a full save and a backup.** The user, 2026-11-10: "we should formalize ending a campaign with a
  full save and backup in case a sequel does happen."
- **DMC does what needs judgment; `dmc` does what must be exact.** The migration design split the work this way
  (`.docs/decisions.md`, the migrations entry; `kit/skills/dmc-migrate-campaign/SKILL.md`; `.docs/architecture.md`).
  Here the closing save, the ending record and the secrets account need judgment; the mark, the backups and the refusal
  to change a closed campaign must be exact.
- **A campaign's state that a program must trust lives in a file only `dmc` writes, not in a line DMC edits.**
  `.docs/decisions.md` gives this reason for `.standard`: a line in `game-state.md` "would be read every resume and
  edited by the player", and only `dmc` writing it means DMC cannot mark a campaign current by hand. The closed mark is
  the same kind of fact.
- **A published module is never exported** (`.docs/decisions.md`; `.docs/dmc-reference.md` § campaign: "`modules/` is
  copyrighted and stays on its machine"). An export records the module's folder name only.
- **Play never depends on `dmc` to start** (`.docs/decisions.md`; `.docs/architecture.md`), and markdown stays the
  source of truth.
- **No migration** (issue #180 § Migration): only one campaign was ever ended by hand, and it is closed once, by hand,
  after merging (the issue's "After merging" steps). Campaigns still in play need nothing until they are closed, and
  closing writes what it needs.

## Clarifications

### Session 2026-11-11

A search of the issue, `.docs/`, the code and every earlier conversation found no decision of the user's and no
principle that settled these; the user answered them.

- Q: Is closing a new skill, or a mode of the full save? → A: A new skill, `dmc-close-campaign`. It follows the full
  save's steps with the closing differences, then the closing steps. Every play session's skill list grows by its one
  description; an ordinary full save loads nothing new (FR-003).
- Q: What can a player do who closed a campaign by mistake? → A: Nothing: closed is closed, and the way on is a sequel.
  No path reopens a closed campaign or clears its mark (FR-018).
- Q: Where do the ending record, the secrets account and the loose ends live? → A: In the files that already hold
  their kind, no new file (FR-009). The user had this answered from the principles and the code, with the play
  folder's campaigns as examples (measured with `dmc size`; The Crooked Moon has no campaign folder yet, only a module,
  so it was not measured):
  - One home per fact and per kind (Principle III; `.docs/architecture.md`): facts are canon lines
    (`kit/dm-rules.md`), secrets are `dm-notes.md` § Secrets (the template), and `summary.md` is the briefing for a DM
    taking over (its template).
  - A new file under `story/` would be searched, never read, at recall's lowest campaign weight (Story, 1.05, against
    canon 1.30 and the archives 1.20; `src/Dmc.Cli/Retrieval/RecallScope.cs`), and would give secrets a second home.
  - The Silver Egg's hand-made ending is spread over four files and seven sections today, which is the failure the
    issue names. Its five secrets are 1.8 KB, and its summary, holding its ending, is 2.6k of a 3k budget. Its
    `dm-notes.md` is over its budget because of "About this player", not the secrets; so is Rorn's Landing's.
- Q: At close, do revealed secrets stay in § Secrets or move to `dm-notes-archive.md`, as resolved items do at a full
  save? → A: All stay in § Secrets, and any an earlier save archived are gathered back: the account is one thing, and
  at an ending nothing is trimmed (as with canon, FR-004). The sequel searches its own digest, ranked ahead of the
  predecessor (issue #179 step 5), so the lower recall weight costs little.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The player closes a finished campaign (Priority: P1)

The story has ended. The player says they want to close the campaign (or DMC suggests it, when the main story has ended or
later when the last open thread is closed, and the player agrees). DMC says what closing means, that it cannot be undone and that the only way back to the
story is a sequel, and asks the player to confirm. On a yes, DMC runs the closing save, writes the ending record and the
account of its secrets, and has `dmc` take the closing backup, check it, place the second copy and set the closed mark.
DMC then tells the player, out of character, that the campaign is closed, where the two copies are, and that a sequel
can start from it.

**Why this priority**: it is the feature. Without it there is no checked, permanent end state for a sequel to build on,
and issue #179 Phase B cannot start.

**Independent Test**: close a test campaign in a play session. The closing files exist and say what FR-004 to FR-008
require; both copies of the closing backup exist and check whole; the mark is set; nothing was archived out of canon by
the closing save.

**Acceptance Scenarios**:

1. **Given** a campaign whose story has ended, **When** the player asks to close it, **Then** DMC states that closing is
   permanent and that the way back is a sequel, and closes nothing until the player confirms.
2. **Given** that confirmation question, **When** the player says no or anything other than a clear yes, **Then**
   nothing is written and the campaign plays on as before.
3. **Given** a campaign whose main story has ended, **When** the player has not asked to close it, **Then** DMC may
   suggest closing then, and once more when the last open thread is closed, and never closes it on its own.
4. **Given** a confirmed close, **When** the closing save runs, **Then** no canon line is moved to `canon-archive.md`,
   each fact it records carries its reason and means where the session established them, each DM secret is marked
   revealed (with its session), never revealed, or abandoned, and each loose end is written as a fact in the character's
   terms.
5. **Given** a confirmed close, **When** the ending record is written, **Then** it says how the campaign ended, where
   every PC, companion and recurring NPC is, each character's final level and condition, and the in-world date.
6. **Given** the closing files are written, **When** `dmc` closes the campaign, **Then** it writes the closing backup,
   checks it whole, writes the second copy outside the campaign folder, and sets the closed mark, in an order where a
   failure at any step leaves the campaign not closed and says what failed.

---

### User Story 2 - A closed campaign stays fixed (Priority: P1)

Weeks later the player, or a sequel, comes back to a closed campaign. Resume says the campaign is closed, opens no scene
and offers to start a sequel. `dmc campaign standard` reports it as closed, so resume never offers to migrate it. No
`dmc` command and no rule of DMC's changes its files. The player can still export it, show an export of it, verify it,
and look things up in it.

**Why this priority**: "closed for good" is only true if nothing can change the files afterwards. A sequel's digest
(issue #179 Phase B) relies on it.

**Independent Test**: on a closed test campaign, record every file's checksum; run resume, every `dmc` command that can
write to a campaign, and the play hooks; compare the checksums. None changed. Resume opened no scene and offered a
sequel.

**Acceptance Scenarios**:

1. **Given** a closed campaign, **When** the player resumes it, **Then** DMC says, out of character, that it is closed,
   opens no scene, reads no file for play, and offers to start a sequel from it.
2. **Given** a closed campaign, **When** `dmc campaign standard` runs on it, **Then** it reports the campaign as closed,
   whatever its standard, and resume makes no migration offer.
3. **Given** a closed campaign, **When** any `dmc` command that writes campaign files is run on it (backup, restore,
   verify's writes, standard `--init`, map, and `pdf` or `digest` aimed into it), **Then** it refuses with one line
   naming the campaign as closed, and changes nothing. `redraw`, which runs after every reply, does nothing and prints
   nothing (FR-015).
4. **Given** a closed campaign, **When** the player exports it, shows that export, verifies it against its closing
   backup, or runs lookup or recall over it, **Then** each works as for any campaign and writes nothing under the
   campaign folder.
5. **Given** a closed campaign named as a sequel's Predecessor, **When** recall runs for the sequel, **Then** the closed
   campaign's files are searched as they are today.
6. **Given** a closed campaign, **When** DMC is asked to change one of its files (a fix, a rewind, a migration, a full
   save), **Then** it declines, says the campaign is closed, and points to a sequel.

---

### User Story 3 - The closing backup survives (Priority: P2)

The closing backup is the permanent end state of the campaign. It is never pruned, and a second copy lives outside the
campaign folder so it survives the folder being lost.

**Why this priority**: the close is only as good as its backup; but the close (US1) and the freeze (US2) can be built
and tested first.

**Independent Test**: close a test campaign, then take and prune ordinary backups many times; the closing backup is
still there and checks whole. Delete the campaign folder; the second copy still exists, `dmc campaign show` lists it,
and it imports.

**Acceptance Scenarios**:

1. **Given** a closed campaign's `backups/` folder, **When** `dmc campaign backups` lists it or any prune runs, **Then**
   the closing backup is neither counted nor removed.
2. **Given** a closed campaign, **When** the closing backup is shown or imported, **Then** it holds the campaign as
   closed, its mark included, and names the module it ran without carrying the module's files.
3. **Given** the campaign folder is lost, **When** the player looks for the second copy, **Then** it is in `exports/` in
   the DMC folder, which install, refresh and update never touch, and it is the same file as the one in `backups/`.

---

### Edge Cases

- **A fight or scene still open when the player asks to close.** The close adds no scene of its own (the issue's
  one-off steps: "Add no scenes and replay nothing"). DMC says the scene is still open in its confirmation question, so
  the player can finish it in play first or close with the state as it stands.
- **The campaign is behind the kit's standard, or a migration is unfinished.** Closing refuses until it is current: a
  closed campaign is never changed by a migration afterwards (issue #180), so the closing backup must be at the current
  standard. The issue's "After merging" steps bring The Silver Egg to the standard before closing it for this reason
  ("This must come before the close: afterwards its files are frozen").
- **`dmc` is missing or fails during the close.** DMC writes nothing it cannot finish, says which step failed, and the
  campaign stays open; the closing files already written are kept and the close can be run again.
- **The second copy cannot be written** (a full disk). The close stops, removes what that run wrote, mark included,
  and says why (FR-011).
- **A file already has the closing backup's name** in `backups/` or `exports/` (a leftover the player put there or a
  cleanup that failed). The close checks before writing anything and fails with one line naming the file; it never
  replaces the file or picks another name.
- **The campaign is already closed.** Closing it again fails with one line saying it is already closed and that a
  sequel continues its story, and changes nothing.
- **A campaign closed by mistake.** Nothing reopens it: closed is closed, and the way on is a sequel (FR-018). The
  confirmation question (FR-002) is the guard.
- **A sequel of a sequel.** Each campaign in a chain is closed on its own; closing one changes none of its predecessors.
- **A one-shot or side campaign that ends.** Closing works the same; it needs no predecessor or sequel.
- **The player edits a closed campaign's files by hand outside play.** Nothing can stop that; `verify` against the
  closing backup reports the difference.

## Requirements *(mandatory)*

### Functional Requirements

**Who closes, and when**

- **FR-001**: Only the player closes a campaign. DMC MAY suggest closing at two moments only: when the main story has
  ended, and again when the last open thread in `story/dm-notes.md` § Open threads is closed (the user, 2026-11-11).
  A no ends the suggestion until the next of those moments. DMC MUST NOT close a campaign on its own. The rule lives in
  the close skill's description, which every session loads, so it costs no other prompt.
- **FR-002**: Before closing, DMC MUST state that closing is permanent and that the only way back to the story is a
  sequel, and MUST wait for the player's clear yes. Anything else closes nothing.
- **FR-003**: The closing procedure MUST be a new kit skill, `dmc-close-campaign`, which DMC loads only when the player
  asks to close a campaign. It follows `dmc-campaign-full-save`'s steps with the closing differences (FR-004 to FR-007)
  and points to that skill for every step it does not change, so each rule keeps one home (Principle III);
  `dmc-campaign-full-save` itself gains no closing text.

**The closing save**

- **FR-004**: The closing save MUST be a full save as `dmc-campaign-full-save` defines it, except that no canon line is
  moved to `canon-archive.md`: at an ending nothing is judged "can no longer come up". Pruning duplicates and superseded
  facts and splitting long lines still apply.
- **FR-005**: The closing save MUST sweep the session for unlogged facts as the full save does, and for each fact
  recorded, also record its reason and its means where the session established them, so that who, what, when, where,
  why and how can be answered later.
- **FR-006**: The closing save MUST give an account of every DM secret: revealed (with its session), never revealed, or
  abandoned.
- **FR-007**: The closing save MUST write each loose end (what was promised, deferred or left open) as a fact, in the
  character's terms, never in a label from `dm-notes.md`.
- **FR-008**: Closing MUST write an ending record: how the campaign ended, where every PC, companion and recurring NPC
  is, each character's final level and condition, and the in-world date.
- **FR-009**: The closing records MUST live in the files that already hold their kind, with no new file:
  - **The ending record** in `story/summary.md`, the briefing for a DM taking over, which the closing save rewrites as
    every full save does. `game-state.md` is brought to the final moment by the full save's own first step.
  - **The secrets account** in `story/dm-notes.md` § Secrets, every secret together, revealed ones included. Revealed
    secrets that earlier full saves moved to `dm-notes-archive.md` are gathered back into § Secrets at close, so the
    account is whole in one place. The full save's rule that moves resolved items to the archive does not apply to
    secrets at close, for the same reason as FR-004.
  - **The loose ends** as canon lines in `story/canon.md`, under the right heading as the full save's canon step says,
    with promises and debts under **Prices, debts and promises**.
  - The size budgets of these files serve resume, and a closed campaign is never resumed into play (FR-014), so the
    closing save's size check MUST NOT trim the ending record or the secrets account; the close skill says so.

**The mark and the backup**

- **FR-010**: The closed mark MUST be a file in the campaign folder that only `dmc` writes, beside `.standard`, so that
  it travels with an export, costs no context, cannot be set or cleared by DMC's edits, and can be tested by DMC without
  `dmc` (it exists or it does not).
- **FR-011**: `dmc` MUST close a campaign in one command that, in order: refuses a campaign that is already closed, is
  not current, or has an unfinished migration, or whose closing backup's name is already taken in `backups/` or
  `exports/` (never replacing that file or choosing another name); sets the mark; writes the closing backup into the campaign's `backups/` under a name that
  backup listing and pruning never count; checks it whole; writes the second copy to `exports/` in the DMC folder;
  checks it; and prints one line naming both files. A failure at any step MUST leave the campaign not closed (no mark),
  remove what that run wrote, and print one line saying what failed.
- **FR-012**: The closing backup MUST hold the whole campaign as closed (its mark included) and MUST name the module the
  campaign ran without carrying the module's files.

**A closed campaign afterwards**

- **FR-013**: `dmc campaign standard` MUST report a closed campaign as closed, ahead of any other status, and resume
  MUST make no migration offer for it.
- **FR-014**: Resume of a closed campaign MUST say it is closed, open no scene, and offer to start a sequel from it.
- **FR-015**: Every `dmc` command that writes under a campaign folder MUST refuse a closed campaign with one line, `dmc
  <command>: <name> is closed: start a sequel to continue its story`, and change nothing. This covers backup, restore,
  standard `--init`, map for files in that campaign, `pdf` and `digest` output aimed into it, and verify's writes. `dmc redraw` is the exception in form: the
  Stop hook runs it after every reply and turns its exit 2 into feedback to DMC (`.docs/dmc-reference.md` § redraw), so
  for a closed active campaign it MUST do nothing and print nothing, as it does today when nothing changed.
- **FR-016**: `dmc campaign export`, `show`, `verify` (reporting only), `lookup` and `recall` MUST work on a closed
  campaign and its exports, and MUST write nothing under the campaign folder.
- **FR-017**: The kit's rules MUST tell DMC never to change a closed campaign's files, whether in play, in a rewind, in a
  migration or when starting a sequel, and to say why when asked.
- **FR-018**: Nothing MUST reopen a closed campaign or clear its mark: no `dmc` verb does it, and the kit's rules give
  no way to. Importing a closing backup behaves as today for any export (it never changes an existing campaign), and the
  copy it makes holds the mark, so it is closed too.

**Cost, documentation and migration**

- **FR-019**: The cost closing adds to the context of every ordinary play session and every ordinary full save MUST be
  measured (bytes ÷ 4, as `.docs/decisions.md` measures skill descriptions) and stated in the PR.
- **FR-020**: The change MUST add no campaign migration, and the PR MUST say why (issue #180 § Migration).
- **FR-021**: The reference (`.docs/dmc-reference.md`), the campaign files description, the user guide, the
  architecture, the roadmap and the decisions MUST describe closing in the same PR, and the toolkit tests MUST check
  that the commands and skill names the kit's prompts name are real.

### Key Entities

- **Closed mark**: a file in the campaign folder, written only by `dmc`, that says the campaign is closed and when.
  Travels with exports. Read by `dmc` (standard, every writing command, close) and by resume.
- **Closing backup**: a checked export of the campaign as closed, in its `backups/` folder under a name pruning never
  counts; permanent.
- **Second copy**: the same file in `exports/` of the DMC folder.
- **Ending record**: how it ended, where everyone is, final levels and conditions, the in-world date.
- **Secrets account**: each DM secret with its outcome: revealed (session), never revealed, abandoned.
- **Loose ends**: what was promised, deferred or left open, as facts in the character's terms.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After closing, every file of the campaign is byte-for-byte unchanged by resume, by every `dmc` command and
  by the play hooks: 0 changed files across a test that runs each of them on a closed campaign.
- **SC-002**: The closing backup survives 10 rounds of backup and prune untouched, and both copies check whole.
- **SC-003**: Resume of a closed campaign opens no scene and offers a sequel in 100% of tested resumes.
- **SC-004**: A failed close at any of its steps leaves the campaign open (no mark) with nothing of that run left
  behind, in 100% of injected failures.
- **SC-005**: The closing save moves 0 canon lines to the archive, and every DM secret in `dm-notes.md` and
  `dm-notes-archive.md` has an outcome in the secrets account.
- **SC-006**: The added cost to every ordinary play session is measured and stated, and is at most the close skill's
  description plus the two short rules that keep a closed campaign fixed (the resume skill's check and one clause in
  `dm-rules.md`), under 100 tokens in all; never the close skill's steps. An ordinary full save gains nothing.

## Assumptions

- **"verify still works" means it reports.** Issue #180 says both that a closed campaign's files never change and that
  `verify` still works on it. Today a passing `verify` writes `.standard` and a `.verified.tsv`; the only reading that
  keeps both statements is that `verify` on a closed campaign checks and reports, and writes nothing.
- **The mark is written before the backup.** Issue #180 asks for "a checked export of the campaign as closed", so the
  backup must contain the mark; a failed backup step then removes the mark (FR-011).
- **The second copy goes to `exports/`.** It is the existing default for an export outside a campaign
  (`.docs/dmc-reference.md` § campaign), is never inside `adventures/` where DMC would search it, and install, refresh
  and update never touch it (`.docs/architecture.md`). The user guide already tells players to keep a further copy
  elsewhere.
- **The closing backup names the module only**, by the recorded decision that a published module is never exported.
- **Closing requires a current campaign** because a closed one can never be migrated afterwards; this follows from the
  issue's "never changed again by ... a migration".
- **The Silver Egg** is closed by hand after merging, as issue #180's "After merging" steps say; nothing for it ships in
  the kit.
- **Out of scope** (issue #180): building a sequel's brief and digest (issue #179 Phase B), and reopening. Also
  verifying a campaign against any export other than its own closing backup or the file `--backup` names (an
  imported copy found its source, a backup against the live campaign): issue #189, which reuses this change's byte
  check.
