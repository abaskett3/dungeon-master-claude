# Feature Specification: The slip test's forks see what play saw

**Feature Branch**: `032-slip-test-fork-defects` (spec 032)

**Created**: 2026-11-26

**Status**: Draft

**Input**: Issue #289, part of Phase 2 #241, plan #238. The maintainer asked for a spec for three defects of the
slip-test runner's fork mode (spec 024), which the #252 and #270 trials found on 2026-11-25 and 2026-11-26: the hook
gets no history, the current turn's request record is left over, and the copy folder is untrusted. Those trials ran
with an interim wrapper outside the repository; this spec puts the fixes in the runner.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The context hook in a fork sees the history (Priority: P1)

The maintainer runs a fork trial. At each run, `dmc context-check` gets the session's history up to the fork message,
as it did in play, and so prints what play's hook printed at that turn: the canon reminder, the sheet reminder, the
out-of-character reminder.

**Why this priority**: without it no fork trial measures play. Claude Code names the fork's own new log as the hook's
transcript before it writes that log, so the hook reads nothing and prints nothing. No fork of #249's base rate got
hook output (0 of 88), while play had it at 27 of its 44 points.

**Independent Test**: a fork run with the stand-in for Claude Code. The hook input it hands the copy's hooks names a
transcript that exists and holds the history up to the fork message.

**Acceptance Scenarios**:

1. **Given** a fork point where play's hook printed a canon reminder, **When** the runner forks it, **Then** the
   fork's hook output is the same text, and the pilot's hook check passes.
2. **Given** a fork point where play's hook printed nothing, **When** the runner forks it, **Then** the fork's hook
   prints nothing.

### User Story 2 - Nothing of the replayed turn is left in the history (Priority: P1)

The base log that the runner writes holds the history before the fork message and nothing written for that message.

**Why this priority**: play's log holds a request record (`api-request-blob`) of the fork turn, written after play's
hook ran, and it carries play's hook output. Left in the base log, it makes the canon reminder think it already
reminded at this turn, so the fork's hook stays silent where play's spoke.

**Independent Test**: a fixture log whose fork turn has a request record: the base log has none, and every record
before the fork turn is kept.

**Acceptance Scenarios**:

1. **Given** a log with a request record that holds the fork message, **When** the runner writes the base log,
   **Then** that record is left out, and the runner reports how many it left out, as a count.

### User Story 3 - The copy folder runs with play's permissions (Priority: P2)

The forks run in a copy folder that Claude Code trusts, as it trusts the DMC folder, so the project's permission rules
in the copy apply as they did in play.

**Why this priority**: in an untrusted folder Claude Code ignores the project's allow rules, so a fork may be asked
for, or denied, a tool that play was allowed. A test run during the #252 pilot showed the copy was not trusted. No denial was
found in the #252 or #270 runs, so this did not change those results, but it is a difference from play that nothing
checks.

**Independent Test**: `prepare` and `run` with a copy folder that Claude Code does not trust: both refuse with one line
that names the fix.

**Acceptance Scenarios**:

1. **Given** a copy folder that is not trusted, **When** the maintainer runs `prepare`, **Then** it refuses with one
   line: the folder, and that it must be opened once in Claude Code and trusted.
2. **Given** a trusted copy folder, **When** the maintainer runs `prepare`, **Then** it goes on as before.

### Edge Cases

- A fork log folder that holds more than the base log, such as an earlier run's log: the hook still gets the base log,
  not another one.
- A fork message that appears in more than one request record: every record of that turn is left out.
- A context arm (spec 029): its added hook prints a file and needs no history, and the kit's hook still gets the
  history.
- Fresh mode: no fork and no base log, so stories 1 and 2 do not apply; story 3 applies to its copy folder too.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: In fork mode, every hook that the copy's settings run on the player's message MUST be able to read the
  history up to the fork message through the transcript the hook is given, as in play.
- **FR-002**: The base log MUST NOT hold a request record of the fork turn: any `api-request-blob` record that holds
  the fork message. Every other record before the fork message MUST be kept as it is. The runner MUST report the
  number it left out, and no text.
- **FR-003**: `prepare` and `run` MUST refuse, with one line, a copy folder that Claude Code does not trust. The runner
  MUST NOT change the maintainer's Claude Code settings to trust it.
- **FR-004**: The pilot's hook check MUST fail when play's turn had hook output and the fork's had none, and say so in
  one line that holds no text of play.
- **FR-005**: The documents MUST say what changed and why: the contract of spec 024, `.docs/development.md` § The slip
  test, `.docs/decisions.md` (that fork trials before this fix got no hook output, with the #249 correction) and
  `.docs/roadmap.md`.

### Key Entities

- **Base log**: the copy of play's log up to the fork message, with paths rewritten, which a fork resumes.
- **Request record**: a log record (`api-request-blob`) that holds a request Claude Code sent, written after the hook
  ran for that turn.

## Success Criteria *(mandatory)*

- **SC-001**: With the fixes and without the interim wrapper, #252's pilot on the maintainer's computer gives the
  control arm the hook output that play had at both of its pilot points, byte for byte.
- **SC-002**: #270's pilot, run the same way, gives the control arm play's hook output at both of its pilot points.
- **SC-003**: Every present test passes, and the new tests pass.

## Assumptions

- **Not verified**: whether Claude Code reads a request record when it resumes a log. If it does, a left-over record
  could change a fork's reply as well as the hook's output. FR-002 removes it either way.
- The interim fix is `D:\Claude\dmc-slip-test\scripts\arm_hook.py` on the maintainer's computer, outside git. It fed
  the hook the base log without the request records of the fork turn. With it, the #252 pilot's control hook output
  equalled play's. This spec replaces it.
- Trust is read from Claude Code's own record of trusted folders. Accepting it stays a step for the maintainer.
- Cost (Development 1): none in play. `slip-test` is a developer tool and is not released.
- No migration (Development 2): no campaign file changes.

## Out of Scope

- **Fork-mode arms that change more than the rules file**: a skill, a template or the `dmc` program. The slip tests
  of #286 and #191 need this, and it is a separate change.
- **Spec 026's refusal of a point for a file of another campaign**, which refused 17 of #270's 18 refused candidates:
  a separate change.
- Re-running #249's base rate. Its correction is posted on #249.
