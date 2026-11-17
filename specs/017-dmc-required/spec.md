# Feature Specification: `dmc` is a required part of DMC

**Feature Branch**: `017-dmc-required`

**Created**: 2026-11-17

**Status**: Draft

**Input**: User description: "Issue #208: retire "play never depends on dmc". dmc is a required part of a DMC install;
drop the kit's fallback rules for a missing dmc, keep the reinstall notice, the start-of-session check, markdown as the
source of truth, and lookup's fall-back to searching when it finds no heading."

## Clarifications

### Session 2026-11-17

- Q: Once DMC has told the player to reinstall, what does it do until `dmc` works? → A: A missing or broken `dmc`
  is an error, not a mode of play. The campaigns depend on `dmc`; without it DMC does not play. It gives the player
  the install line and plays nothing (no scene, no roll, no map, no resume) until `dmc --version` runs again.
- Q: A `dmc` command fails with an error while `dmc` is installed and runs: what does DMC do? → A: An error is not a
  missing `dmc`, and an error is never hidden from the player. If the shell says the command is not found (or the
  program cannot start), `dmc` is missing (FR-003). Otherwise DMC handles errors as it does today, which works
  smoothly: each skill says what to do where a failure matters (`dmc-draw-map:95`, `dmc-resume-campaign:14` and `:34`,
  `dmc-migrate-campaign:44` and `:75-76`), `dmc`'s errors are one plain line, and DMC uses its judgement elsewhere. A
  general rule sorting errors into kinds was drafted and dropped: no feedback entry shows a `dmc` error handled badly,
  and it would cost about 120 tokens on every turn.

## Background

Every play path now runs through `dmc`: dice, maps, PDF reading, `size`, `lookup`, `recall`, closing a campaign, the
sequel import and the play hooks. The principle that play never depends on `dmc` survives only as fallback wording,
which costs context on every turn that loads the kit and is never used in practice. The maintainer decided to retire it
on 2026-11-14, while clarifying #179's Phase C (spec 012); `.docs/decisions.md` § "A module check, and a pointer that
names the area" already says no fallback was added for a missing `dmc` because of this issue.

### Where the principle lives today (checked 2026-11-17 on `origin/main` at `fb71982`)

The kit, which reaches every player's DMC folder:

- `kit/dm-rules.md:78` (Look before you bring something back): "If `dmc` fails or the recall does not hold it, read
  its sheet and search `canon.md` and those archives as before".
- `kit/dm-rules.md:139` (Dice): after the reinstall lines, "and carry on without dice and maps until then".
- `kit/skills/dmc-draw-map/SKILL.md:114` (When there is no picture): "If `dmc` is unavailable, or the player options ask
  for text maps, draw the same grid in a fenced code block instead".
- `kit/skills/dmc-ingest-module/SKILL.md:26`: "If `dmc` is unavailable, fall back to the Read tool's `pages`
  parameter ... and tell the player that a PDF over 100 MB needs `dmc`."
- `kit/skills/dmc-setup-rules/SKILL.md:17`: "(Without `dmc`, skip this step.)"
- `kit/skills/dmc-setup-rules/SKILL.md:42`: "Otherwise, or when it finds nothing, open the index ..." ("Otherwise"
  covers a lookup that could not run; "when it finds nothing" is `dmc` finding nothing).

The documents:

- `README.md:11`: "Without `dmc`, play and saving still work, but DMC cannot roll its own dice, draw maps or read PDFs."
- `.docs/architecture.md:120`, a design principle: "**Play never depends on `dmc` to start.** Without it Claude Code
  still starts in the DMC folder, and DMC can still play and save."
- `.docs/user-guide.md:450` (troubleshooting): "Without `dmc`, DMC cannot roll its own dice, draw maps or read PDFs."
- `.docs/decisions.md` § "Retrieval runs over the markdown, with no stored index (issue #179)": "play never depends on
  `dmc`: when it fails, DMC searches and reads the files as before", and its Limits ("the fallback covers it"); §
  "Lookup and recall are for play only until Phase D (issue #179)": "with the fallback to searching".

### What stays (from the issue's "Not changed")

- The start-of-session check: `kit/settings.json`'s `SessionStart` hook runs `dmc --version` and, if it fails, prints
  one line telling DMC that `dmc` is missing or does not run, with the two install lines (`.docs/decisions.md` § Keep
  the missing-`dmc` notice at session start).
- The reinstall notice in `kit/dm-rules.md:139`: the two install lines DMC gives the player.
- Markdown stays the source of truth: the campaign files stay readable and editable by hand, and backups and exports
  stay plain files (a ZIP that opens without `dmc`, `.docs/decisions.md` § A campaign export is a ZIP).
- A `dmc lookup` that finds no heading still falls back to the nearest heading it lists and then to searching
  (`kit/dm-rules.md:113`, `kit/skills/dmc-resume-campaign/SKILL.md:34`), and a `dmc recall` that does not hold the fact
  is still followed by a read of the sheet and a search. That is `dmc` finding nothing, not `dmc` missing.
- The hooks still swallow a missing or broken `dmc` (`.docs/architecture.md` § Hooks never get in the way), so Claude
  Code always starts and the notice can be shown.
- Text maps as a player option (`Maps are shown as: text`, `kit/templates/player-options.md:31`): a player choice, not
  a fallback.
- The resume's closed-campaign guard, `kit/skills/dmc-resume-campaign/SKILL.md:14`: "If `dmc` fails, Glob
  `adventures/<campaign>/.closed`: if found, the same." It handles an error from a `dmc` that runs (one older than
  0.1.4 answers `campaign` with `dmc: unknown command "campaign"`, `.docs/user-guide.md:449`) and still keeps a closed
  campaign closed (FR-010); a missing `dmc` already stops play by FR-003.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - DMC with a working `dmc` carries no fallback rules (Priority: P1)

A player plays with a working `dmc`, as every install has. The rules and skills DMC loads no longer tell it how to play
without `dmc`, so none of that wording costs context and none of it can be followed by mistake.

**Why this priority**: This is the issue's goal: the fallback wording costs context and is never used.

**Independent Test**: Search the kit for any instruction for a missing or unavailable `dmc`; only the reinstall notice
and the rule for what DMC does until `dmc` works (FR-003) are found.

**Acceptance Scenarios**:

1. **Given** the kit after this change, **When** it is searched for the removed wording ("If `dmc` is unavailable",
   "Without `dmc`, skip", "If `dmc` fails or the recall", "Otherwise, or when it finds nothing", "carry on without dice
   and maps"), **Then** none is found.
2. **Given** a module PDF to ingest, **When** DMC follows `dmc-ingest-module`, **Then** it reads the PDF only through
   `dmc pdf`, with no alternative route through the Read tool's `pages` parameter.
3. **Given** a returning NPC and a recall that does not hold them, **When** DMC follows the recall rule, **Then** it
   still reads the sheet and searches the files (that rule stays; only the "if `dmc` fails" half goes).

---

### User Story 2 - DMC with a missing or broken `dmc` sends the player to reinstall (Priority: P1)

A player whose `dmc` is missing or does not run starts a session. The start-of-session notice tells DMC, and DMC tells
the player to reinstall with the install line for their system. DMC does not play until `dmc` works again (FR-003).

**Why this priority**: Without it a player with a broken install gets no way back.

**Independent Test**: Start a session in a copy of a DMC folder with `dmc` off the `PATH`; DMC gives the install line and
then plays nothing.

**Acceptance Scenarios**:

1. **Given** `dmc` is not on the `PATH`, **When** a session starts, **Then** the start-of-session hook prints the
   one-line notice and DMC tells the player to reinstall with the install line for their system.
2. **Given** `dmc` was missing and the player has reinstalled it, **When** the player says so, **Then** DMC checks that
   `dmc` runs and carries on as normal.

---

### User Story 3 - The documents say `dmc` is required (Priority: P2)

A player or developer reading the README, the user guide or the architecture document learns that `dmc` is a required
part of DMC, not an optional helper, and the decision record says when and why the principle was retired.

**Why this priority**: The documents must not promise a mode of play the kit no longer supports.

**Independent Test**: Read `README.md`, `.docs/architecture.md`, `.docs/user-guide.md` and `.docs/decisions.md`; none
says play works without `dmc`, and one decision entry records the retirement.

**Acceptance Scenarios**:

1. **Given** the documents after this change, **When** they are searched for "never depends on `dmc`" and "play and
   saving still work", **Then** each is found only in a decision entry marked as superseded, if at all.

### Edge Cases

- `dmc` is installed but a command reports an error (a bug, a file it cannot read, a version older than the kit): that
  is not a missing `dmc`; DMC handles it as today (FR-010).
- `dmc` goes missing mid-session (the player uninstalled it, or an update failed): the first command that cannot run is
  treated as a missing `dmc` (FR-002, FR-003).
- The player has no terminal at hand to reinstall (playing remotely): DMC still gives the install line and does not
  play until `dmc` runs (FR-003).
- A campaign file names `dmc` output (a pasted roll line): unchanged; this change touches no campaign file.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The kit (`kit/dm-rules.md`, `kit/skills/`, `kit/templates/`, `kit/settings.json`) MUST NOT carry an
  instruction for playing, saving, mapping, ingesting or looking things up without `dmc`, except FR-002 and FR-003.
  The places listed in Background § Where the principle lives today are the starting list.
- **FR-002**: When `dmc` is missing or does not run, DMC MUST tell the player to reinstall it with the install line for
  their system, as `kit/dm-rules.md:139` does today. The start-of-session check and its notice MUST stay.
- **FR-003**: `dmc` is missing when the shell says the command is not found or the program cannot start (and so
  `dmc --version` fails). A missing `dmc` MUST be treated as an error: until `dmc --version` runs again, DMC MUST NOT
  play (no scene, no roll, no map, no resume of a campaign). When the player says they have reinstalled, DMC checks
  `dmc --version` and, if it runs, carries on.
- **FR-004**: A `dmc lookup` that finds no heading and a `dmc recall` that does not hold the fact MUST keep their next
  steps (the nearest heading, then a search; the sheet, then a search). These are `dmc` finding nothing, not `dmc`
  missing.
- **FR-005**: Text maps MUST stay available as a player option; the ASCII-grid rule in `dmc-draw-map` MUST apply only to
  that option.
- **FR-006**: The hooks MUST still swallow a missing or broken `dmc` so that Claude Code always starts and the
  start-of-session notice can be shown.
- **FR-007**: `README.md`, `.docs/architecture.md` and `.docs/user-guide.md` MUST say `dmc` is required and what
  happens when it is missing (FR-002, FR-003). `.docs/decisions.md` MUST gain an entry that retires the principle,
  with its date (2026-11-14, the maintainer's decision), reason and source (#208), and the entries that state the
  principle MUST point to it.
- **FR-008**: The change MUST NOT change the shape of any campaign file, so no migration is added.
- **FR-009**: `kit/dm-rules.md`, which every play turn loads, MUST NOT grow; the bytes it gains or loses are measured and
  recorded.
- **FR-010**: An error `dmc` reports while it runs is not a missing `dmc`. The kit's existing handling of `dmc` errors
  (the per-skill lines named in Clarifications) MUST stay as it is, no general error rule is added, and no change may
  hide an error from the player.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A search of the kit finds 0 instructions for working without `dmc` beyond FR-002 and FR-003 (against 6
  today, listed in Background).
- **SC-002**: `kit/dm-rules.md` is the same size or smaller than today, measured in bytes; the always-loaded skill and
  rule text the change touches shrinks overall.
- **SC-003**: In one headless play test with `dmc` off the `PATH`, DMC gives the player the install line in its first
  reply and then plays nothing: no scene, no roll in its head, no map drawn by hand.
- **SC-004**: In one headless play test with a working `dmc`, play, a roll, a map and a recall behave as before.
- **SC-005**: No document says play works without `dmc`, except superseded decision entries.

## Assumptions

- Every supported install has `dmc`: `dmc install` writes the DMC folder and `dmc play` starts play, so a player
  without `dmc` has no DMC folder unless they copied one or `dmc` was later removed or broken.
- No toolkit test asserts the fallback lines (checked: `tests/` has no match for "unavailable", "carry on without" or
  "Without `dmc`"); the plan says whether a test is added for FR-001.
- The constitution states no rule about a missing `dmc` (checked: `.specify/memory/constitution.md` has no such line),
  so no amendment is needed.
- The play test uses the test folder `D:\Claude\test-campaign` or a scratch copy of a DMC folder, never the player's
  live campaign.
