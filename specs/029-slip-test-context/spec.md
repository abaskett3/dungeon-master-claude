# Feature Specification: The slip test puts per-point text next to the player's message

**Feature Branch**: `claude/project-thread-bs1df4` (spec 029)

**Created**: 2026-11-26

**Status**: Draft

**Input**: Issue #284, part of #270 (the oracle test), Phase 2 #241, plan #238. #270's oracle arm puts the correct
facts for the persons in a scene "next to the player's message, in the place where the hook's text arrives". Spec
024's runner cannot: in fork mode an arm is only a rules file, and the context hook runs live in the copy folder, so
every arm gets the same hook output.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - An arm gets its own text at each point (Priority: P1)

The maintainer writes, for the oracle arm, one file per fork point with the facts for that scene, and names the
folder in `trial.json`. At each run of that arm, the fork's turn holds the kit's hook output as play had it and,
as a second hook output, the point's text. The control arm's turn holds only the kit's.

**Independent Test**: a run with the stand-in for Claude Code: during the oracle arm's call the copy's
`.claude/settings.local.json` names a hook that prints the point's file; during the control's call it does not; after
the run the file is as the DMC folder has it.

### User Story 2 - The pilot shows the text arrived and nothing else changed (Priority: P1)

The pilot's hook check leaves the arm's text out before it compares the hook output with play's, and a new `context`
check passes only when the text is in the fork's hook output.

**Independent Test**: a pilot where the stand-in records the text passes; one where it drops the text fails the
`context` check and names no text of play.

### User Story 3 - A missing or changed file stops the trial (Priority: P2)

`prepare` refuses an arm whose folder lacks a point's file, and `run` refuses a context file changed after `prepare`.

## Requirements *(mandatory)*

- **FR-001**: `trial.json` MAY hold `context`: an object from arm name to a folder, relative to the work folder, that
  holds `<point id>.md` for every point. Only fork mode takes it. An unknown arm, a missing folder or a missing file
  MUST be refused with one line that names it.
- **FR-002**: Before each run of an arm with context, the runner MUST put the copy's `.claude/settings.local.json` back
  as the DMC folder has it (or remove it when the DMC folder has none) and, when the point's file is not blank, add
  one `UserPromptSubmit` hook whose command prints that file. After the run, failed or not, it MUST put the file back
  as the DMC folder has it. Before each run of an arm without context it MUST do the same with no hook added.
- **FR-003**: The hook's command MUST start the running `slip-test` program as `slip-test context <file>`, with
  forward slashes and in single quotes, so it works in the shell Claude Code runs hooks in on Windows and Linux.
  `context` prints the file as the hook's `additionalContext` in Claude Code's JSON form and exits 0, and a missing
  file prints nothing. `prepare` MUST refuse a context file over 10,000 characters, which Claude Code would shorten,
  and a DMC folder's `.claude/settings.local.json` that is not a JSON object.
- **FR-004**: The context files MUST be locked with `trial.json`, `points.tsv` and `key.tsv`.
- **FR-005**: The pilot's hook check MUST leave out every hook output whose text is the arm's text at that point,
  and a `context` check MUST pass when that output was found (or the file is blank) and fail otherwise. The report
  shows the `context` row only when an arm has context.
- **FR-006**: The documents MUST say how to use it: the contract of spec 024, `.docs/development.md` § The slip test,
  `.docs/decisions.md` and `.docs/roadmap.md`.

## Success Criteria *(mandatory)*

- **SC-001**: #270's pilot on the maintainer's computer passes with a control and an oracle arm, and the oracle fork
  shows the text after the kit's hook output.
- **SC-002**: Every present test passes, and the new tests pass.

## Assumptions

- Claude Code runs every `UserPromptSubmit` hook of the project's `settings.json` and `settings.local.json` for a
  `-p` call in a trusted folder, and records each one's output as a `hook_additional_context` attachment. **Not
  verified** here: the cloud container runs no project hooks in an untrusted folder (tried on 2026-11-26 with a plain `echo` hook in `settings.json`). SC-001 checks it.
- The model can read the copy folder but not the work folder, so the text is printed from the work folder.
- Cost (Development 1): none in play. `slip-test` is a developer tool and is not released.
- No migration (Development 2): no campaign file changes.

## Out of Scope

- #270's optional third arm, the hook output with the known wrong lines removed: that needs a per-point change to
  the restored files.
- Fresh mode, whose overlays may hold their own `.claude/settings.local.json`.
