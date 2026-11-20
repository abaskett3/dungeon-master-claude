# Feature Specification: The break baseline after a typed resume

**Feature Branch**: `019-typed-resume-baseline`

**Created**: 2026-11-18

**Status**: Draft

**Input**: User description: "#238", narrowed by the maintainer to its piece of work 0.1, issue #244: "Slips 0.1:
take the break baseline after a typed resume". Part of Phase 0 (#239) of the slip plan #238.

## Clarifications

### Session 2026-11-19, the code review of PR #271

The maintainer approved two changes from the code review on 2026-11-19 ("apply all").

- Q: A typed resume in turn 2 or 3, after a slash resume or after a first typed one, moved the baseline up. The rule
  that a resume later in the session cannot hide the context's growth no longer held. → A: Only the part's first
  resume can set the baseline. A resume before the first player message (the slash command, or DMC's own `Skill`
  call), or a slash command sent in turns 1 to 3, is the first resume too, and it leaves the present rule. FR-001 and
  FR-002 changed.
- Q: Dropping the sheet lines on the first message after a compaction rested on DMC reloading there, which no log
  shows. → A: Drop them only in a fresh log, with no reply and no compaction boundary: the first message after a
  `/clear` or of a session. After a compaction they print as before. FR-005 and User Story 2 changed.

## Background

The context hook (`dmc context-check`, run before each player message) offers a break when the context has grown a
band past where the resume left it. Its bands start at the larger of 150,000 tokens and the baseline plus 40,000,
then rise every 50,000 (`.docs/decisions.md` § "The break alert is relative to where the resume left the context",
as amended by the feedback round of #181).

### What is wrong today (checked 2026-11-18 on `origin/main` at `68c3396`)

- **The baseline.** `TranscriptReader.BaselineOf` (`src/Dmc.Cli/ContextCheck/TranscriptReader.cs:186-194`) takes the
  context of the last call before the first player message. After `/clear` and the slash command
  `/dmc-resume-campaign`, that is the end of the resume, because a slash-command echo is not a player message. When the
  player types the words "Resume campaign." instead, the typed message is the first player message, DMC resumes in
  its turn, and the baseline is the context from before the resume. The resume itself then counts as growth.
- **The evidence.** Log `082db04e` (`~/.claude/projects/D--Claude-dmc/`): line 8 is the `/clear`, line 18 the typed
  "Resume campaign.", lines 38 and 42 two main-conversation `Skill` calls with `{"skill":"dmc-resume-campaign"}` in
  that one turn, and line 216 the first play message. The baseline was 37,008 tokens, so the first band was the
  150,000 floor. The break line came on the fifth message after the resume, at about 154,000 tokens, in the middle of
  a correction. The right first band was about 186,000 (#237). #237 found this case in 1 of 89 play sessions.
  A count on 2026-11-18 found it in 3 of 115 main play logs (`082db04e`, `6a4e09c3`, `c88dd223`), and found no
  slash resume sent after a typed message. The counting script stays outside the repository, because the logs quote
  play.
- **The sheet lines.** `SheetReminder.For` (`src/Dmc.Cli/Retrieval/SheetReminder.cs:79-106`) prints nothing for a
  message that starts with `/`, but a typed first message prints: no one has been shown or read yet, so every person
  present with a sheet is due, up to `CapBytes` (10,000 bytes, about 2,500 estimated tokens). The resume then reads
  the same sheets.

The present method needs no resume marker, so it holds however the files were loaded, and a resume the player runs
later cannot move the baseline up (`TranscriptReader.cs:26-31`, `.docs/dmc-reference.md:825`). This change keeps both
properties outside the first three player messages.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A typed resume does not bring an early break offer (Priority: P1)

The player starts a play session by typing "Resume campaign." (or any words that make DMC run the resume skill)
instead of the slash command. DMC resumes and the scene starts. The first break offer comes only when the play itself
has added a band, as it does after the slash command.

**Why this priority**: an early offer interrupts play, often mid-scene, and costs turns. It is the defect the issue
names.

**Independent Test**: run the hook over a log of a typed resume (a test fixture shaped like `082db04e`) and read its
baseline: it is the context at the end of the resume turn, not before it.

**Acceptance Scenarios**:

1. **Given** a log that starts with `/clear`, then the typed message "Resume campaign.", whose turn holds a main
   `Skill` call of `dmc-resume-campaign` and ends at 146,000 tokens, **When** the hook runs on a later message,
   **Then** the baseline is 146,000 and the first band is 186,000.
2. **Given** the same log at 154,000 tokens five messages after the resume, **When** the hook runs, **Then** it prints
   no break line.
3. **Given** a log where the player says "hi" first and asks for the resume in the second message, **When** the hook
   runs after that turn, **Then** the baseline is the context at the end of the second turn.
6. **Given** a typed resume in turn 1 and another in turn 2, **When** the hook runs, **Then** the baseline is the end
   of turn 1: only the first resume counts.
4. **Given** a log where the resume turn holds two resume `Skill` calls (as `082db04e` does), **When** the hook runs,
   **Then** the baseline is the context of the last call of that turn, after both.
5. **Given** a typed resume whose turn is still running (no later player message yet), **When** the hook runs,
   **Then** the baseline is the latest call of that turn.

---

### User Story 2 - The first typed message after a `/clear` prints no sheet lines (Priority: P2)

On the first message of a fresh log, after a `/clear` or at the start of a session, before DMC has answered
anything, the hook prints no sheet lines. The resume, which reads the sheets, comes next. After a compaction the lines
print as before.

**Why this priority**: it saves up to about 2,500 estimated tokens once per typed resume. It removes a cost, not a
slip.

**Independent Test**: run the hook on a typed first message in a campaign whose Scene block names people with sheets:
it prints no sheet lines.

**Acceptance Scenarios**:

1. **Given** a log with a `/clear` and no assistant call after it, and a Present line naming three people with sheets,
   **When** the player types a message, **Then** the hook prints no sheet lines.
2. **Given** a log with an assistant call, **When** the player types a message, **Then** the sheet lines print as
   they do today.
3. **Given** a log whose latest record is a compaction boundary, **When** the player types a message, **Then** the
   sheet lines print as they do today.

---

### User Story 3 - Every other way into play behaves as today (Priority: P1)

A resume by the slash command, a session that loads its files one by one, a resume run late in the session, and a
session after a compaction keep the baseline and the bands they have today.

**Why this priority**: the break line works in all other cases (issue #35, spec 016). A fix for 3 play logs in 115
must not move the other 112.

**Independent Test**: the present break-line and baseline tests pass unchanged.

**Acceptance Scenarios**:

1. **Given** a log with `/clear` and `/dmc-resume-campaign` before the first player message, **When** the hook runs,
   **Then** the baseline is the end of the resume, as today.
2. **Given** a resume `Skill` call in the turn of the fourth player message or later, **When** the hook runs, **Then**
   the baseline is the one the present method gives, so a late resume cannot move it up.
3. **Given** a resume `Skill` call made by a sub-agent (a sidechain record), **When** the hook runs, **Then** it does
   not count.
4. **Given** a compaction after which the player's first three messages include a typed resume, **When** the hook
   runs, **Then** the new rule applies to the part after the compaction, counting player messages from the boundary.
5. **Given** `/dmc-resume-campaign` (or DMC's own resume call) before the first player message, and a typed resume in
   turn 2, **When** the hook runs, **Then** the baseline is the end of the first resume, as today.

### Edge Cases

- The resume skill under its name from before the rename, `resume-campaign`, in an old log: counts as a resume.
- A typed resume that DMC answers by reading the files one by one, with no `Skill` call: the present method applies.
  This case is not fixed (see Assumptions).
- A resume `Skill` call in each of the first three turns: the first of them sets the baseline.
- A slash resume sent in turns 1 to 3, after a typed message: it is the part's first resume, so the present method
  applies and no later resume counts.
- A log record of an unexpected shape in the resume turn: skipped, as the present baseline pass skips one; at worst
  the baseline falls back to the present method.
- A hook input that names no log, or a log that cannot be read: the sheet lines behave as today; only a log that is
  read and holds no reply and no compaction boundary stops them.
- The break offer's own check (`TranscriptReader.OfferedSince`) and the out-of-character line are unchanged.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: In the part of a log after its latest compaction boundary (a `/clear` starts a new log), if the part's
  first resume is a main-conversation `Skill` call of `dmc-resume-campaign` (or `resume-campaign`) in the turn of one
  of the first three player messages, the baseline MUST be the context of the last assistant call of that turn: the
  last call before the next player message, or the latest call when no next message has come. A resume before the
  first player message (the slash command or a `Skill` call), and the slash command sent in turns 1 to 3, are first
  resumes too; they leave the baseline to FR-003. (Amended 2026-11-19, see Clarifications.)
- **FR-002**: A resume after the part's first one MUST NOT move the baseline. (Amended 2026-11-19: it was "the latest
  of those turns".)
- **FR-003**: In every other case the baseline MUST be the one the present method gives (`TranscriptReader.cs:16-31`).
- **FR-004**: The bands, the 150,000 floor, the band step and the one-offer-per-band check MUST stay as they are; only
  the baseline they start from changes.
- **FR-005**: The hook MUST print no sheet lines when it reads the session's log and finds no assistant call and no
  compaction boundary in it (a fresh log). On that message the sheet part hands no person on to the canon reminder
  either. After a compaction the lines print as before. (Amended 2026-11-19, see Clarifications.)
- **FR-006**: A player message, a turn, a main-conversation call and a compaction boundary MUST be recognised as they
  are today, by the classifier shared with `dmc digest` (`UserText.IsTyped`, `TranscriptReader.Classify` at `TranscriptReader.cs:331`).
- **FR-007**: The hook MUST stay as fast as the decision records: about 20 ms over process start for an 18 MB log. It
  MUST still read past the point where the baseline is settled only the lines that could be a compaction boundary.
- **FR-008**: Toolkit tests MUST cover User Story 1 scenarios 1 and 3 to 6, User Story 2 all scenarios, and User
  Story 3 scenarios 2 to 5, with log fixtures; the present tests of the break line and the sheet lines MUST pass
  unchanged.
- **FR-009**: The documents MUST describe the new rule in its homes: the decision "The break alert is relative to
  where the resume left the context" in `.docs/decisions.md` gains an amendment with this reason and source, the
  baseline section of `.docs/dmc-reference.md` (§ `context-check`) states the rule, the XML doc comments of
  `TranscriptReader` and `SheetReminder` match, and `.docs/roadmap.md` records the status. Other places point to these
  homes and copy nothing.

### Key Entities

- **Log part**: the records of a Claude Code log after its latest compaction boundary; a `/clear` starts a new log.
- **Player message**: a user record that the shared classifier counts as the player's (not a tool result, command
  echo, meta record, sub-agent message or injection).
- **Turn**: a player message and every record up to the next player message.
- **Resume turn**: a turn among the first three of a log part that holds the part's first resume, a main-conversation
  `Skill` call of the resume skill.
- **Fresh log**: a log with no main-conversation call and no compaction boundary.
- **Baseline**: the context, in tokens, that the break bands are counted from.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a fixture with the shape of log `082db04e`, the baseline is the context at the end of the resume
  turn, and the hook prints no break line at the message that got one in real play (about 154,000 tokens).
- **SC-002**: A typed first message after a `/clear` prints 0 bytes of sheet lines, against up to 10,000 today.
- **SC-003**: Every present test of the break line, the baseline and the sheet lines passes with no change to its
  expected result.
- **SC-004**: On an 18 MB play log, the hook's time over process start stays within the recorded budget of about
  20 ms (release build). No timing test covers the hook today; the plan says how this is measured.
- **SC-005**: An ordinary play turn gains no output and no kit or skill text: 0 lines added to `kit/dm-rules.md` and
  to each skill.

## Assumptions

- The rule's numbers (three player messages, the last call of the resume turn) come from issue #244 and are taken as
  given.
- A typed resume nearly always runs as a `Skill` call. A resume done by reading files one by one, with no call, keeps
  the present method; no log has shown it after typed words, and a marker-free rule for it would bring back the risk
  of a late resume moving the baseline up.
- After a compaction, `kit/dm-rules.md:202` tells DMC to re-run the resume steps before narrating, but no log shows a
  reload after a compaction, so the sheet lines still print there (Clarifications, 2026-11-19).
- No campaign file, template or skill changes, so no campaign migration is needed (Development 2), and install and
  `dmc update` gain no step (Development 3).
- Until this ships the player starts play with `/dmc-resume-campaign` (#239, "Work for the player").

## Out of Scope

- The canon reminder's own lines, the out-of-character line and the break line's wording.
- Any change to how often a person's sheet lines are shown again (`SheetReminder.Refresh`); that is #253.
- The other Phase 0 work (#245 to #248) and any trial of Phase 2.
