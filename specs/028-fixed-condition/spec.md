# Feature Specification: A condition for "fixed"

**Feature Branch**: `claude/project-thread-bs1df4` (spec 028)

**Created**: 2026-11-25

**Status**: Draft

**Input**: Issue #267 (Slips 4.2, part of #243, plan #238). A feedback entry is `fixed` only when later play shows that
the slip stopped, not when a PR merges. It is `released` in between, and it opens again when its fault comes back.

## Background

`review-feedback` set an entry to `fixed in PR #N` when the PR merged, so "fixed" meant "a change was made". Entries
marked fixed came back: dead time (2026-11-08, 11-15, 11-17), a character who knows what nobody told it (11-06, twice
on 11-15), wrong names (11-06, 11-07, 11-17), clothing (11-11, 11-17). Each time a new entry or a bare occurrence
started from zero. `review-transcript` § 7 (spec 025) now counts play turns for each story session, and the baseline
of #250 gives 4.7 slips per 100 play turns in all.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A fix is confirmed by play, not by a merge (Priority: P1)

The dead-time entry's PR merges and ships in 0.5.0. The entry says `released in 0.5.0`. After 300 play turns on 0.5.0
or later with no dead time, a review sets `fixed in 0.5.0: no case in 312 play turns`.

### User Story 2 - A fix that failed opens its entry again (Priority: P1)

Dead time comes back in a session on 0.5.0. `dmc-record-feedback` finds the released entry, adds an occurrence
starting "came back after released in 0.5.0", and sets it `open`. The next review knows the change failed.

**Independent Test** (both): the toolkit test reads the template and the three skills for the statuses, the search,
the condition and the reopening.

## Requirements *(mandatory)*

- **FR-001**: The template's status list MUST hold `released in X.Y.Z` and `fixed in X.Y.Z: no case in N play turns`
  in place of `fixed in PR #N`.
- **FR-002**: `review-feedback` MUST keep an entry `filed #N` until its PR has merged, states its slip-test result or
  why it has none (#268), and a release holds the change. Then it sets `released in X.Y.Z`.
- **FR-003**: `review-feedback` MUST set `fixed in X.Y.Z: no case in N play turns` only when the story sessions on
  that release or later have at least 300 play turns, counted by `review-transcript` § 7, with no case of the entry's
  fault. With fewer, the entry stays released, and the review says how many turns there are.
- **FR-004**: `dmc-record-feedback` and `review-transcript` MUST search the released and fixed entries for a repeat.
  When one comes back, they MUST add an occurrence starting "came back after <the status>" and set the entry `open`.
- **FR-005**: An older entry's `fixed in PR #N` MUST be read as released, in the first release after that PR. The
  template says so, and `review-feedback` lists such entries.
- **FR-006**: The documents MUST say the same: `.docs/campaign-files.md` (the feedback entry),
  `.docs/user-guide.md` (reviewing feedback), `.docs/decisions.md`, `.docs/roadmap.md`.

## Success Criteria *(mandatory)*

- **SC-001**: No entry is set fixed by a merge. Every fixed entry names its release and its count of play turns.
- **SC-002**: Every present test passes, and the new toolkit test passes.

## Assumptions

- The condition is zero cases in 300 play turns. By the rule of three, that puts the fault's rate below 3 in 300, or 1
  in 100 turns, with 95% confidence, a quarter of the baseline's rate for all slips. A rate test against the rate
  before the release (the issue's other proposal) needs a count of the fault before it, which an entry's occurrences
  do not give; it is left out.
- No migration (Development 2). `feedback/` is not a campaign file, and old entries keep their words, which the skills
  read.
- Cost (Development 1): `kit/dm-rules.md` 0 bytes; `dmc-record-feedback` +172 bytes and the feedback template +125
  bytes, both read only when an entry is recorded. `review-feedback` and `review-transcript` are developer skills.

## Out of Scope

- The slip-test result in a PR (#268), which FR-002 only requires to be present.
