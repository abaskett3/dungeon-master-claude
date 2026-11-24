# Feature Specification: Count every slip in every story session

**Feature Branch**: `claude/project-thread-bs1df4-slip-count` (spec 025)

**Created**: 2026-11-24

**Status**: Draft

**Input**: Issue #251 (Slips 1.3, part of #240, plan #238): widen `review-transcript` section 7 to every story
session; remove conduct from its "Not slips" list; add a `Slip:` line for a conduct slip to the session-log template
and to the kit rule that writes the line; keep the count of the #179 gate separate; record with each count the
session kind, the model, the context before the first play turn and the number of play turns. Do not add a second
counter to `dmc digest`.

## Background

`review-transcript` § 7 counted `Slip:` lines only for issue #179's gate, three sessions after Phase D. The template
limits a `Slip:` line to a contradiction of the files, so a conduct slip (a roll that was the player's, an action they
did not declare) leaves no line, and § 7 listed conduct under "Not slips". A count has no denominator.
`kit/dm-rules.md` is at its byte cap (`ToolkitTests.DmRulesBytesAllowed`, 32,510).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A conduct slip leaves a `Slip:` line (Priority: P1)

DMC rolls a die that was the player's; the player corrects it. DMC redoes the beat and writes
`Slip: rolled the player's attack, the player rolls (conduct)` in the session log.

**Independent Test**: read `kit/templates/session-log.md` and `kit/dm-rules.md`: the category exists and the rule
logs every slip; the toolkit's byte cap test passes.

### User Story 2 - The review gives each story session's count with its denominator (Priority: P1)

`review-transcript` reports a line per story session: `s72: 3 slips in 41 play turns (2 fact, 1 conduct), Sonnet
high, ordinary session, 139,000 tokens at the start`.

**Independent Test**: a library test of the digest's new **Play turns** line on an invented log: slash commands, a
resume and an interruption are not play turns; the context before the first play turn, the efforts and the kind are
printed, or "not recorded".

### User Story 3 - The gate stays comparable (Priority: P2)

The #179 gate count is its own section and leaves `conduct` lines out, as its baseline did.

## Requirements *(mandatory)*

- **FR-001**: The session-log template's `Slip:` line MUST have a `conduct` category for a roll, an action or a choice
  that was the player's.
- **FR-002**: `kit/dm-rules.md` MUST tell DMC to log every slip, conduct included, without growing past its cap.
- **FR-003**: `review-transcript` § 7 MUST count the slips of every story session the review covers, fact and conduct,
  with no "Not slips" entry for conduct, and give one line per story session with its denominator.
- **FR-004**: The #179 gate count MUST stay a separate section with its rule unchanged, conduct left out.
- **FR-005**: `dmc digest show`'s `stats.md` MUST print, per session, the play turns (player messages that are not a
  slash command, a resume, a full save or an interruption), the context before the first, the efforts the API calls
  record and the session kind the log names, "not recorded" for each the log lacks. It adds no slip counter.
- **FR-006**: The documents MUST describe the change: `.docs/campaign-files.md` (the session log's `Slip:` line),
  `.docs/dmc-reference.md` (`stats.md`), `.docs/roadmap.md`, `.docs/decisions.md`.

## Success Criteria *(mandatory)*

- **SC-001**: `kit/dm-rules.md` does not grow (32,510 to 32,507 bytes).
- **SC-002**: Every present test passes; the new digest test passes.
- **SC-003**: The next `review-transcript` gives a count line for every story session it covers.

## Assumptions

- The session kind is a `sessionKind` property of a record (#250 names it); not verified, as the logs of this
  container have none. Effort is an assistant record's `effort` (Claude Code 2.1.331).
- No migration (Development 2): a new category of an optional line, which older logs simply lack.
- Cost (Development 1): `kit/dm-rules.md` −3 bytes; the template +125 bytes, read when a session log is written; one
  `Slip:` line of about 30 tokens per conduct slip, only when one occurs; `review-transcript` is developer-only.

## Out of Scope

- The baseline of slips DMC does not admit (#250); the correction procedure itself (#252).
