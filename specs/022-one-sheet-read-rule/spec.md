# Feature Specification: One kit rule for how far DMC reads a sheet

**Feature Branch**: `claude/project-thread-86m1m7` (spec 022)

**Created**: 2026-11-24

**Status**: Draft

**Input**: User description: "Issue #247 (Slips 0.4, part of #239, plan #238): `kit/dm-rules.md:78` says to read the
sheet of a person who joins the scene's Present line down to its History; the resume skill (step 2.5) reads a
companion's sheet through its play card. Make the two agree. Do not grow `kit/dm-rules.md`, which is at its cap."

## Background

Two kit rules give different instructions for a companion's sheet (issue #247):

- `kit/dm-rules.md:78`: "Before you add someone to the Scene's Present line, read their sheet down to its History,
  once per session part."
- `kit/skills/dmc-resume-campaign/SKILL.md:26` (§ 2, item 5, "step 2.5"): a companion's sheet with a `## Play card`
  is read "from the top to the line before the next `## ` heading after the card, and the rest when play needs it
  (CLAUDE.md § Accuracy)"; any other sheet is read down to its history.

A companion is in the scene's Present line from the start of each session part, so both rules apply to the same read,
and they disagree. Three resumes of one campaign read the five companion sheets in three ways (logs `bf7609c5`: all
five for 61 to 79 lines; `082db04e`: two for about 80, three for 21; `f8f2df71`: all five for 21). The difference
between the first and the third is about 25,000 tokens (issue #247; the logs are on the maintainer's computer).

The issue proposes a new sentence that names the resume's two cases in `kit/dm-rules.md` (about 70 bytes more). The
plan of #238 asks for no more rule text in `kit/dm-rules.md`, which is at its cap of 32,510 bytes
(`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, `DmRulesBytesAllowed`). So this spec states the extent in a shorter form
and pays for it with two cuts that change no instruction, leaving the file at its cap.

## Clarifications

### Session 2026-11-24

- Q: Copy the two cases into `kit/dm-rules.md` (the issue's text, +70 bytes and a cap raise) or point at resume step
  2.5? → A: First a pointer ("as resume step 2.5 does"). The code review of PR #273 found three faults in it (research
  R1), so the rule states the extent in its own words, shorter than the issue's text, and a toolkit test holds it to
  the resume step. The bytes come from two cuts that change no instruction, so the file stays at its cap. The brief for
  this work repeats the plan's rule that no rule text is added to `kit/dm-rules.md`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - One answer to "how far do I read this sheet?" (Priority: P1)

A companion joins the scene, or a session part starts with the party present. DMC looks up how far to read the sheet
and finds one answer: as the resume does, through the play card for a companion, down to the history for anyone
else.

**Why this priority**: two answers let each resume pick one, and the choice costs up to about 25,000 tokens with no
way to say which read was right.

**Independent Test**: a toolkit test checks that `kit/dm-rules.md` says to read the sheet through its play card (if
none, down to its History), no longer says "down to its History" alone for that read, and that the resume skill's § 2
item 5 is still the sheets step with the same two cases.

**Acceptance Scenarios**:

1. **Given** the kit, **When** DMC adds a companion with a play card to the Present line, **Then** both rules say to
   read the sheet through the play card.
2. **Given** the kit, **When** DMC adds anyone else to the Present line, **Then** both rules say to read the sheet
   down to its history.
3. **Given** a later edit that changes the resume's read of sheets or the rule's, **When** the toolkit tests run,
   **Then** a test fails, so the two cannot drift apart unnoticed.
4. **Given** a session with no resume (a new campaign played on) or one where the resume skill's text is no longer in
   context, **When** a companion joins the Present line, **Then** the rule alone says how far to read.

### Edge Cases

- A companion's sheet without a `## Play card` (a campaign not yet migrated to standard 6) is read down to its
  history, as resume step 2.5 reads it. Only companions have a play card (`kit/templates/npc-sheet.md`), so "if none"
  covers everyone else.
- Someone about to join the Present line is in none of resume step 2.5's three kinds of sheet yet, and the step says
  "Any other sheet ... is not read now". The rule therefore states the extent itself and does not send DMC to the step
  to decide whether to read (research R1).
- A sheet whose history is in the middle is read whole: resume step 2.5 already says so.
- The rest of a companion's sheet is still read when play needs it: the next sentence of `kit/dm-rules.md:78` (before
  they speak of their past or what they know, or use a feature or number not on their play card) does not change.
- This makes the rules agree. It does not make each resume read the same lines; a resume that reads more is now a
  slip against a kit rule, which #251 can count.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `kit/dm-rules.md` § Accuracy, "Look before you bring something back", MUST say: "Before you add someone
  to the Scene's Present line, read their sheet through its play card (if none, down to its History), once per session
  part."
- **FR-002**: `kit/dm-rules.md` MUST no longer say to read that sheet "down to its History" alone.
- **FR-003**: The resume skill's § 2 item 5 MUST stay the sheets step, reading a companion's sheet with a play card
  through the card and any other sheet down to its history; a toolkit test MUST check this and FR-001, and fail if
  either is lost.
- **FR-004**: `kit/dm-rules.md` MUST NOT grow. The new extent is 33 bytes longer than the text it replaces. In the
  same bullet, the vestigial " as before" of the recall fallback ("search `canon.md` and those archives as before")
  goes (10 bytes), and so does ": it is closed for good" after "Never change a campaign whose folder holds `.closed`"
  (23 bytes; the instruction is the "Never change", and `.docs/decisions.md` § A closed campaign is closed for good
  keeps the reason). The file stays at 32,510 bytes and its cap does not change.
- **FR-005**: `FeedbackRoundTests.cs`, which pins the old words, MUST be updated to the new test of FR-003.
- **FR-006**: The documents MUST record the change: a new entry in `.docs/decisions.md` and a row in
  `.docs/roadmap.md`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `kit/dm-rules.md` and resume step 2.5 give the same extent for every sheet read when a person joins
  the Present line (the toolkit test of FR-003).
- **SC-002**: `kit/dm-rules.md` stays at 32,510 bytes and 212 lines; no skill changes.
- **SC-003**: Every present toolkit test passes, the updated one included.
- **SC-004** (after the merge, by the maintainer): three resumes on the released kit are measured for the lines read
  of each companion sheet, and the result is posted on #247 (quickstart Q2). This is a measurement, not a pass line:
  the issue does not promise that each resume reads the same lines.

## Assumptions

- "Resume step 2.5" names § 2 item 5 of `dmc-resume-campaign`, as issue #247 does. The kit rules do not depend on
  that label: they state the extent themselves, because the resume skill is not in context in a session that began
  with a new campaign, and its text may be far back in a long one (code review of PR #273).
- No campaign file, template or rule for a file's shape changes, so no migration is needed. `dmc update` rewrites
  `CLAUDE.md` from the kit, so the change reaches existing DMC folders with no new step.

## Out of Scope

- Changing how far the resume reads (that is #256, a trial).
- The other contradictions of #238 (the correction procedure #252, when to record feedback).
