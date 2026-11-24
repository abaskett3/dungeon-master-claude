# Feature Specification: Hold fixes from feedback that only add words

**Feature Branch**: `claude/project-thread-86m1m7-248` (spec 023)

**Created**: 2026-11-24

**Status**: Draft

**Input**: User description: "Issue #248 (Slips 0.5, part of #239, plan #238, decision D5): for a feedback entry whose
only fix is words, `review-feedback` writes 'held until the slip test' under the entry's Worker notes and makes no
change. Not held: #247, #252 and #261, which remove a contradiction or replace text; a mechanism (a hook, a `dmc`
check, a deny rule); a fix that replaces a kit rule with a shorter one. The hold ends when the slip test of #249 can
test a held change, or on 2026-12-31."

## Background

`review-feedback` (`.claude/skills/review-feedback/SKILL.md`) answers most feedback entries with a new sentence in
`kit/dm-rules.md` or a skill. No measurement shows these sentences decrease slips (issue #248):

- Corrections as a share of player turns, by period (#237): before PR #170 7.7%, #170 to #173 8.2%, #173 to #223
  12.5%, #223 to #230 19.2%, after #230 19.7%. The campaign grew and the player watched more closely in the same
  period, so this shows no decrease, not harm.
- `kit/dm-rules.md` grew from 13,581 to 32,510 bytes between 2026-10-12 and 2026-11-17 and is at its cap
  (`DmRulesBytesAllowed`, `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`).
- Entries marked fixed came back (#243). A sentence PR #230 added (`kit/dm-rules.md:87`) contradicts the line before
  it.

The skill already prefers a mechanism to a paragraph (§ 6) and asks for a cut when `kit/dm-rules.md` grows (§ 7). It
does not stop a word-only fix.

## Clarifications

### Session 2026-11-24

- Q: What status does a held entry get? → A: None new. Its status stays as it is (`open`), so step 1 still lists it
  and the next review sees the Worker note. A new status would need the feedback template, `dmc-record-feedback`'s
  grep and their tests to change, for a hold that ends within weeks.
- Q: How does the skill know the slip test is ready? → A: From `.docs/roadmap.md`, the repository's home of status:
  the hold ends when it shows #249 done.
- Q: Decision D5 is the player's. → A: Asked on 2026-11-24, with the issue's terms recommended; the spec follows those
  terms and changes if the player picks another end date.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A word-only fix waits for the slip test (Priority: P1)

The maintainer runs `review-feedback`. An entry's best fix is a new sentence in `kit/dm-rules.md`. The report lists it
under "Held until the slip test" with no change proposed; when the maintainer says which entries to act on, the
entry's Worker notes get `held until the slip test` (once), and no kit file changes.

**Why this priority**: the sentence would cost every exchange of every session with no evidence it helps.

**Independent Test**: a toolkit test checks that the skill carries the hold, its exceptions and its end.

**Acceptance Scenarios**:

1. **Given** an open entry whose only fix is a new sentence, **When** the skill reports, **Then** it lists the entry as
   held and proposes no change; on the user's word it writes the Worker note once, keeps the status and changes
   nothing.
3. **Given** an entry that already has the note, **When** a later review runs during the hold, **Then** the note is not
   written again.
2. **Given** a held entry, **When** the user asks to act on all entries, **Then** the held one is not acted on unless
   the user lifts the hold for it.

### User Story 2 - Fixes that are not words still go ahead (Priority: P1)

An entry's fix is a hook, a `dmc` check or a deny rule, or it removes a contradiction, or it replaces a rule with a
shorter one. The skill proposes it as before.

**Independent Test**: the same toolkit test checks that the three exceptions are named.

**Acceptance Scenarios**:

1. **Given** an entry whose fix is a deny rule, **When** the skill reports, **Then** the fix is proposed, not held.
2. **Given** an entry whose fix replaces two contradicting sentences with one no longer than they were, **When** the
   skill reports, **Then** it is proposed, not held.

### User Story 3 - The hold ends (Priority: P2)

The roadmap shows #249 done, or the date passes 2026-12-31. The next review proposes the held entries again, each with
its slip-test result where the test can run it, and changes only those that pass.

**Acceptance Scenarios**:

1. **Given** the roadmap shows #249 done, **When** the skill runs, **Then** it proposes the held entries again, and
   when it acts on one it replaces the note with the outcome.
2. **Given** 2026-12-31 has passed and #249 is not done, **When** the skill runs, **Then** it proposes them again with
   no slip-test result.

### Edge Cases

- A fix that is mostly a mechanism but adds a sentence that points at it (a new `dmc` check and the line that tells
  DMC to run it) is a mechanism: the sentence is what makes the mechanism run. A script counts as a mechanism, as § 6
  of the skill already prefers it to a paragraph.
- The date is read with `date`, not guessed (code review).
- A fix that replaces text and makes the file longer is held: "without making the file longer" is the test.
- A template field is words in a template, so a new field is held unless it replaces text, although
  `.docs/development.md` § Writing prompts counts a template field as a mechanism in general; the skill and that
  document say so (code review).
- The hold covers `review-feedback` only; play and `dmc-record-feedback` do not change.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `review-feedback` § 6 MUST hold a cause whose only fix is words (a new or longer sentence in
  `kit/dm-rules.md`, a skill or a template, a new template field included): list it under "Held until the slip test"
  and propose no change for it.
- **FR-002**: It MUST name the three kinds of change that are not held: one that removes a contradiction or replaces
  text without making the file longer; a mechanism (a hook, a script, a `dmc` check, a deny rule, with the sentence
  that tells DMC to use it); one that replaces a kit rule with a shorter one.
- **FR-003**: It MUST end the hold at the first of: `.docs/roadmap.md` shows the slip test (#249) done, or today's
  date (from `date`) is after 2026-12-31; then propose the held entries again, each with its slip-test result where
  the test can run it, and change only those that pass.
- **FR-004**: § 7 MUST not act on a held entry while the hold lasts, unless the user lifts it for that entry: on the
  user's word it writes `held until the slip test` under the entry's Worker notes unless it is there already, leaves
  the status as it is and changes nothing; once the hold has ended and the entry is proposed again, it replaces that
  line with the outcome.
- **FR-005**: A toolkit test MUST check FR-001 to FR-004.
- **FR-006**: The documents MUST record the change: an entry in `.docs/decisions.md`, a row in `.docs/roadmap.md`,
  the "Feedback is recorded first" bullet of `.docs/development.md` § Writing prompts, and the description of
  `review-feedback` in `.docs/user-guide.md`, `README.md` and `.docs/product-overview.md`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The toolkit test of FR-005 passes, and every present toolkit test passes.
- **SC-002**: No file under `kit/` changes: 0 bytes in play.
- **SC-003**: From the merge to the end of the hold, no PR from `review-feedback` adds a sentence to `kit/dm-rules.md`
  or a skill that is not one of the three exceptions (checked by the maintainer at the end of the hold).

## Assumptions

- The player accepts decision D5 on the issue's terms (asked 2026-11-24). If they pick another end date, the date in
  FR-003, the skill and the decision changes.
- `review-feedback` is a developer skill (`.claude/skills/`), not part of the kit, so the change reaches no DMC folder
  and needs no migration.

## Out of Scope

- The slip test itself (#249) and the condition for "fixed" (#267).
- Changing `dmc-record-feedback` or the feedback template.
