# Feature Specification: The slip test restores each file from the backup that holds it at the fork

**Feature Branch**: `claude/project-thread-bs1df4` (spec 026)

**Created**: 2026-11-25

**Status**: Draft

**Input**: Issue #278. The pilot of #249 on the maintainer's computer (comment of 2026-11-25 on #249): one fork saw a session
log as it was after play had corrected the slip being replayed. The fix was written on the maintainer's computer
(branch `fix-slip-test-restore`). This spec covers it. Part of #240, plan #238. Corrects spec 024 research R4.

## Background

Spec 024 restores a copy's `adventures/` to a fork point's moment from Claude Code's file history. Research R4 assumed
that a backup is taken before each turn's first edit, so a file's content at time T is its first backup after T. It
marked that format **not verified**, because the cloud container keeps no file history for `-p` runs. The maintainer's
logs show three things R4 missed:

- A backup is written at each edit and recorded at the `file-history-snapshot` of the next player message. It holds
  the file as it was then, so an edit between the fork and that next message is already in it.
- A session's first backup of a file (`<stem>@v1`, the file before the session's first edit of it) is on disk under
  `file-history/<session>/`, but no snapshot names it. Every session's first recorded version is `@v2`.
- A file that a `Write` creates has no backup before the `Write`.

So the earliest backup after the fork can hold a later edit, and a fork can see play that came after it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A fork sees the files as they were at its message (Priority: P1)

The maintainer runs a trial at a point where play then corrected a slip in a session log. The fork's copy holds the
log as it was before the correction.

**Independent Test**: a test on invented history: an edit falls between the fork and the next backup, and the
restore takes the backup before the fork.

### User Story 2 - A file no backup can restore refuses the point (Priority: P1)

When no backup holds a file as it was at the fork, `run` stops with one line that names the file and the time and
says to leave the point out. It never forks from a wrong state.

**Independent Test**: a test where edits fall on both sides of the only backup refuses with that line.

## Requirements *(mandatory)*

- **FR-001**: The runner MUST read every call of an edit tool (`Edit`, `Write`, `MultiEdit`, `NotebookEdit`) from
  the DMC folder's logs, with its file and the time of its record.
- **FR-002**: The runner MUST add each session's unrecorded first backup (`<stem>@v1`) when it is on disk, with the
  name stem any log gives that file, timed just before that session's first edit of the file.
- **FR-003**: For each file under `adventures/`, the restore MUST take the earliest backup after the fork when no edit
  of the file falls between the fork and that backup. Otherwise it MUST take the latest backup at or before the fork,
  when no edit falls between that backup and the fork.
- **FR-004**: A file with no backup and no edit before the fork, and an edit after it, MUST be treated as not existing
  at the fork (a `Write` created it later).
- **FR-005**: Otherwise the runner MUST refuse the point with one line:
  `slip-test: no backup holds <file> as it was at <time>: leave this fork point out`.
- **FR-006**: The documents MUST say what the file history holds: `.docs/development.md` § The slip test,
  `.docs/decisions.md`, `.docs/roadmap.md`, and a pointer from spec 024 research R4 to this spec.

## Success Criteria *(mandatory)*

- **SC-001**: The pilot at the point that leaked no longer shows `attachment:edited_text_file` (the maintainer's rerun,
  #249 comment of 2026-11-25).
- **SC-002**: Every present test passes, and the four new tests pass.

## Assumptions

- The format comes from the maintainer's logs with Claude Code 2.1.317. A later release that records `@v1` is still
  read correctly, because a recorded backup is not added twice.
- Cost (Development 1): none in play. `slip-test` is a developer tool.
- No migration (Development 2): no campaign file changes.

## Out of Scope

- Changes made with a shell command, which leave no backup (spec 024's limit stands).
- The harness notices that forks see and play did not (#249's pilot comment).
- A second blind scorer for #249's base rate.
