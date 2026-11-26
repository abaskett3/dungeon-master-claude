# Feature Specification: A wardrobe and keepsakes section on sheets

**Feature Branch**: `claude/project-thread-pna0lk` (spec 030)

**Created**: 2026-11-26

**Status**: Draft

**Input**: Issue #286: "Sheets: a wardrobe and keepsakes section for what a character owns but is not wearing now",
from player feedback of 2026-11-25 (DMC 0.4.3, bookkeeping). The maintainer: build it here; do not change
`kit/dm-rules.md`, which is at its byte cap.

## Background

A sheet's play card has `Looks` and `Items used in play`, which say what a character wears and carries now. Nothing
holds what they own and are not wearing now. When outfits changed for an occasion, DMC could only edit those lines: the
old clothes were overwritten or left in `Looks` as a mix of "now" and "also owns", and a slip about an old outfit
followed (issue #286, Evidence). DMC added a wardrobe section to two sheets by hand; it listed things worn now, and one
character's clothes ended up in four places. The PC template has `Stored elsewhere`; the NPC template, which
companions use, has nothing like it.

## Clarifications

### Session 2026-11-26

- Q: A clause in `kit/dm-rules.md` for the swap (about 120 bytes and a cap raise)? → A: No. The maintainer's brief
  keeps the file unchanged; its line on what someone wears or owns already says to rewrite the sheet. The swap is a
  step of `dmc-character-sheet-update`, paid only when that skill loads.
- Q: Which sheets does the migration give the section? → A: Every PC's and companion's, and any sheet that already
  has a hand-made wardrobe heading. Other NPCs get it when they have something to keep; resume reads their sheets to
  their history, so an empty section would cost every resume.
- Q: Where does an item kept by someone else go? → A: On its owner's sheet, with the keeper named; the keeper's
  sheet may keep a short pointer.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A change of clothes loses nothing (Priority: P1)

The player dresses a companion for an occasion. DMC writes the new outfit to `Looks` and moves the old one, with
where it is kept, to `## Wardrobe and keepsakes`.

**Why this priority**: it is the slip the feedback recorded: old clothes lost or muddled with the new.

**Independent Test**: the slip test's fork point A (issue #286): after the change, no item the character owned is
missing from the sheet, and `Looks` holds only what is worn now.

**Acceptance Scenarios**:

1. **Given** a companion in an everyday hood, **When** the player puts them in a ceremonial cloak, **Then** `Looks`
   names the cloak and the wardrobe names the hood, where it is kept and when it is worn.

### User Story 2 - A stored item comes back (Priority: P1)

Later, or in a sequel, the player puts a stored item back on. DMC finds it in the wardrobe, with its keeper and
place, and swaps it into `Looks`.

**Independent Test**: the slip test's fork point B (issue #286).

**Acceptance Scenarios**:

1. **Given** a hat kept by the PC at home for special days, **When** the player says the companion wears it, **Then**
   DMC describes the hat as the sheet does and, if the scene is away from home, says it must be fetched.

### User Story 3 - Existing campaigns get the section (Priority: P2)

A campaign made before this change is brought up to date by migration 009, and nothing it says about clothes is
lost.

**Independent Test**: `dmc campaign verify` with the check `wardrobe-sections-in-place` and the accounting of every
moved line.

### Edge Cases

- Where a thing is kept or when it is worn is not in the files: the line says "not recorded", never a guess.
- A thing lost, given away or sold is not in the wardrobe; it stays in the History.
- `Looks` lists several outfits by occasion: the everyday one, or the one the latest session shows, stays.

## Requirements *(mandatory)*

- **FR-001**: Both sheet templates have `## Wardrobe and keepsakes` right before `## History`, with a comment giving
  the line's shape (what it is; where it is kept and who holds it; when it is worn) and the swap.
- **FR-002**: The play card's `Looks` is labelled `(worn now)`; the PC template's `Stored elsewhere` is for tools and
  property, clothes and keepsakes going to the wardrobe.
- **FR-003**: `dmc-character-sheet-update` gains a step: Looks (Appearance without a card) is what they wear now; what
  they own and do not wear or carry goes on the owner's sheet in the wardrobe; a change of clothes is a swap; read the
  section before dressing them. Its linked-campaign paragraph carries the wardrobe into a sequel's sheet.
- **FR-004**: `dmc-generate-image-prompt` reads the wardrobe for what was worn at a past moment; the full save's
  sheet check names clothes.
- **FR-005**: `kit/dm-rules.md` does not change.
- **FR-006**: Migration 009 adds the section to each PC's and companion's sheet, renames a hand-made wardrobe heading,
  moves what the sheets say about things not worn now into it, accounts every moved line as `reworded`, and names the
  check `wardrobe-sections-in-place`, which `dmc` implements.
- **FR-007**: Tests: the check's cases in `MigrationChecksTests`, and a toolkit test pinning the templates, the
  skill's step and the migration's check.
- **FR-008**: Documents: `.docs/campaign-files.md`, `.docs/dmc-reference.md`, `.docs/roadmap.md`,
  `.docs/decisions.md`.

## Success Criteria *(mandatory)*

- **SC-001**: The slip test of issue #286 passes: the treatment loses no item in any of its 10 runs and slips fewer
  times than the control over the same runs.
- **SC-002**: `kit/dm-rules.md` is byte for byte unchanged; the skills grow by under 500 bytes in all.
- **SC-003**: All tests pass.

## Assumptions

- The slip test needs the maintainer's Claude Code logs and file history, so it runs on the maintainer's computer
  (`.docs/decisions.md`, issue #268, Limits).
