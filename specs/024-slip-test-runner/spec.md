# Feature Specification: The slip test, a replay runner that passes its pilot

**Feature Branch**: `claude/project-thread-bs1df4` (spec 024)

**Created**: 2026-11-24

**Status**: Draft

**Input**: Issue #249 (Slips 1.1, part of #240, plan #238): "A runner in the repository, as developer tooling. It
never runs in CI. Its inputs (fork points, keys, saved replies) quote play, so they stay outside git. It has two
modes: fork (continue a real play session from the message before a known point; the treatment goes in by a rewrite
of the recorded instructions in the base log; the control gets an equivalent rewrite) and fresh resume (copy today's
campaign to a scratch DMC folder, start base sessions for each variant, then fork each base session with real player
messages). It also has the pilot, the many-fork measurement, blind export and a cost record."

## Background

A slip is a DMC reply that the player has to correct (#238 § Words). No fix round showed a decrease, and the project
could not have seen one (#240 § Purpose): a slip has a small chance in each reply. #237 replayed one slip 20 times from
the same context, and it did not come back. One headless run, or one replay of one slip, shows nothing.

#237 built a first harness, in a session's scratchpad, and its pilot found these faults (#249 § What exists):

- The forks got notices that play did not have: an "Environment update" block (the copy folder's path differed from
  the recorded working directory), "This session is non-interactive", and an attribution reminder.
- The copy folder was not trusted, so its allow rules did not apply. The player has since trusted the copy folder
  with one entry in `~/.claude.json` (#238 § Carried over from #237).
- A changed `CLAUDE.md` in the copy did not reach the model: a continued conversation keeps the instructions of its
  first request, as they are recorded in the log.
- The recorded kit rules differ by play session (32,517, 30,627 and 27,380 bytes).

This spec puts the runner in the repository, with tests on invented logs, and makes the pilot a command that checks
each of those faults.

## Clarifications

### Session 2026-11-24

- Q: Which language? → A: C#, as a console program `src/Dmc.SlipTest` (`slip-test`), beside `dmc` and never
  released. The runner reads and rewrites JSON lines, restores files, runs `claude` and computes intervals; the pure
  parts get MSTest tests on invented logs in CI, which a script would not have. The maintainer writes C#.
- Q: Where do the inputs live? → A: In a work folder the maintainer names, outside the repository (suggested:
  `slip-test/<trial>/` in the DMC folder). Nothing the runner reads or writes is in git.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The pilot shows the replays see what play saw (Priority: P1)

The maintainer picks about four fork points, writes the trial file and the key, and runs the pilot. The runner forks
each point in each arm and checks the four conditions of #249: no notice that play did not have, hook output that
matches the original record (or a documented difference), the arm's rules text present in the request, and the same
number of thinking blocks in both arms. It prints numbers only, which go on issue #249 as a comment.

**Why this priority**: every later number rests on the replays being faithful. #237's pilot failed on exactly these
points.

**Independent Test**: tests on invented logs: a fork log with an extra attachment type fails the notice check; a
changed hook text fails the hook check unless the allow file names it; a fork log without the arm's text fails the
treatment check; arms with different thinking counts fail. A live pilot on the maintainer's computer passes.

**Acceptance Scenarios**:

1. **Given** a locked trial of four fork points and two arms, **When** the maintainer runs `slip-test run --pilot`
   then `slip-test pilot`, **Then** the runner prints one line per check per run and a verdict, and exits 0 only if
   every check passes.
2. **Given** a fork turn whose hook output differs from the original only by a line the allow file documents,
   **When** the pilot checks it, **Then** that difference passes and is counted as documented.

---

### User Story 2 - Fork mode measures a change to the rules (Priority: P1)

The maintainer writes a control rules file (the present `kit/dm-rules.md`) and a treatment (the changed one). For
each fork point the runner takes the base session's log up to the player message, rewrites the recorded instructions
to the arm's text, restores the campaign files of a copy folder to that moment, and replays the player's message with
the same model and effort. Arms are interleaved, so both run at the same time.

**Why this priority**: most fixes change the rules, and the rules reach a continued conversation only through the
log.

**Independent Test**: tests on invented logs and file history: the truncated log ends before the fork message, its
session id and folder paths are rewritten, the recorded `CLAUDE.md` text is the arm's; the restored files are the
contents at the fork time; the command line has the original model, effort and permission mode.

**Acceptance Scenarios**:

1. **Given** a fork point in a play log, **When** a run starts, **Then** the copy folder's campaign files equal their
   state at that player message, and the log the replay resumes holds the arm's rules and the copy folder's path.
2. **Given** a run that finished, **When** the next starts, **Then** the copy folder's campaign files are restored
   again, so no run sees another run's changes.

---

### User Story 3 - Fresh-resume mode measures a change to the resume, the launch or a hook (Priority: P2)

For a change that a fork cannot carry (the resume skill, a hook, the settings), each arm is a folder of files laid
over a copy of the DMC folder. The runner starts one base session per arm with the resume, then forks each base
session with real player messages.

**Why this priority**: #256 (a smaller resume) and #253 (the context hook's lines) need it; most trials do not.

**Independent Test**: a test checks that the overlay is applied over a fresh copy and that each fork of a base session
starts from the base's end state.

**Acceptance Scenarios**:

1. **Given** two arms with overlay folders, **When** the maintainer runs the trial, **Then** each arm has one base
   session and each point is forked from its arm's base.

---

### User Story 4 - Blind export and the result (Priority: P1)

After the runs, the runner exports each reply with no tools and no hook output, under a random id, shuffled, into one
file for scoring. The scorer marks each id against the key written before the runs. The runner joins the scores with
the hidden map and prints each arm's slip rate with its 95% interval over fork points, the difference between arms,
the pass line's verdict, and the cost.

**Why this priority**: a score that knows the arm is not a measure (#240 rule 2).

**Independent Test**: tests: the export holds no arm name, no tool call and no hook text; the order differs from the
run order; the result's intervals and verdict match a worked example.

**Acceptance Scenarios**:

1. **Given** a scored export, **When** the maintainer runs `slip-test score`, **Then** the result names, per arm, the
   runs, points, slips, rate and interval, and says whether the pass line fixed in the trial file was met.

### Edge Cases

- A trial file, point list or key changed after `prepare` locked them: `run` and `score` refuse with one line.
- A fork point whose message is not a text message the player typed (a command, a tool result, an image): `prepare`
  refuses it with the point's id.
- A base log in the background session kind: Claude Code rejects `--bg` with `-p` (#249), so it is replayed as an
  ordinary session and the result says so. Trial #255 uses real play.
- A file the session changed with a shell command is not in Claude Code's file history; the restore cannot undo it.
  Since spec 020 the kit denies the usual shell writes; older logs may hold some (#238: about 45 in 5 logs).
- A file that did not exist at the fork time (its first backup has no content): the restore deletes it from the copy.
- An interrupted run leaves a partial result: the next `run` repeats it.
- The copy folder equals the DMC folder, or one is inside the other: every command refuses.
- A play session above 300,000 tokens, or the groups of slips in a long correction, are outside what a fork after a
  fresh resume measures (#249 § Limits).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A console program `slip-test` (`src/Dmc.SlipTest`) MUST provide the commands `prepare`, `run`,
  `pilot`, `export` and `score`, each taking `--work <folder>`. It is developer tooling: it is not released, not part
  of a DMC folder, and never runs in CI.
- **FR-002**: `prepare` MUST read the trial file, the fork points and the key, resolve every fork point in the logs,
  create the copy folder from the DMC folder, and write a lock holding the three files' SHA-256 hashes. `run` and
  `score` MUST refuse when a locked file changed, so the number of runs and the pass line are fixed before the run
  (#240 rule 1) and the key is written before the runs (#240 rule 2).
- **FR-003**: In fork mode, each run MUST: copy the base log up to (not including) the fork message, keeping only the
  fork message's chain of parents and the records with no uuid, under a new session
  id into the copy folder's Claude Code project folder; replace the original folder's path with the copy folder's in
  every string of it; replace the recorded text of the DMC folder's `CLAUDE.md` with the arm's rules file; restore the
  copy folder's campaign files to the fork time; and run `claude -p --resume <id> --fork-session` in the copy folder
  with the fork message on standard input, the original's model, effort and permission mode, and JSON output.
- **FR-004**: The restore MUST give each file under `adventures/` its content at the fork time: the content of its
  first file-history backup taken after the fork message in any session of that folder; the file's present content if
  it has none; absent if that backup says the file did not exist. Before each run, the copy's `adventures/` MUST be
  copied again from the DMC folder, so no run sees another's changes.
- **FR-005**: In fresh-resume mode, each arm MUST have an overlay folder laid over a fresh copy; the runner MUST start
  one base session per arm with the trial's resume message, keep the copy's `adventures/` as the base left it, and fork
  each point from its arm's base with the point's player message.
- **FR-006**: `run` MUST interleave the arms (for each point and run number, the arms in a random order), skip runs
  already done, and record for each run: point, arm, run number, the new session id, model, effort, permission mode,
  the Claude Code version, the original session's kind when the log has one, tokens and cost.
- **FR-007**: `run --pilot` MUST run only the first four point-and-arm pairs (two points in two arms, or four points
  in one arm). `pilot` MUST check, for each pilot run: (a) every attachment type and meta message of the replayed turn
  also occurs in the original turn; (b) every hook output of the replayed turn equals the original's, in fork mode;
  (c) the fork log holds the arm's rules text; (d) the thinking blocks sent are the same in both arms at each point.
  A difference listed in the work folder's allow file, with its reason, passes as documented. `pilot` MUST print
  numbers and type names only, no text of play, and exit 0 only if every check passes.
- **FR-008**: `export` MUST write every reply (the text blocks of the replayed turn, with no tool calls and no hook
  output) under a random id in a shuffled order to one file, a hidden map from id to point, arm and run, and a scores
  file with one row per id to fill in.
- **FR-009**: `score` MUST join the scores with the map and write the result: per arm the runs, the fork points, the
  runs with a slip, the rate, and its 95% interval from a bootstrap over fork points (fixed seed); for two arms the
  difference and its interval; the pass line's verdict; and the total tokens and cost of the runs.
- **FR-010**: The runner MUST never write into the DMC folder or its Claude Code project folder, and MUST delete the
  logs it puts in the copy folder's project folder after copying them into the work folder.
- **FR-011**: The documents MUST describe the runner and its limits in their homes: `.docs/development.md` (a section
  beside the recall test, with the limits of #249 § Limits), `.docs/architecture.md` (the program), `.docs/roadmap.md`
  (status), `.docs/decisions.md` (the language and the units). The contract is
  `specs/024-slip-test-runner/contracts/slip-test.md`.

### Key Entities

- **Trial**: one measurement: mode, the DMC folder, the copy folder, arms, runs per point, the pass line.
- **Arm**: a rules file (fork mode) or an overlay folder (fresh-resume mode).
- **Fork point**: a player message in a play log: an id, the session id and the message's uuid.
- **Key**: what counts as a slip at each fork point, written before the runs.
- **Run**: one replay of one fork point in one arm.

## Success Criteria *(mandatory)*

- **SC-001**: The pilot passes on the maintainer's computer, and its numbers are a comment on #249.
- **SC-002**: One many-fork run of the present kit with no treatment (about 50 fork points, 1 to 3 runs each) gives the
  base rate and its interval; the number is a comment on #249 and sizes the trials of #241.
- **SC-003**: The cost of one fork run and one base session is recorded on #249 (#237 measured about $0.02 for a fork
  run).
- **SC-004**: The runner's tests pass in CI on invented logs; every present test passes unchanged.
- **SC-005**: Nothing that quotes play is in git.

## Assumptions

- Verified on Claude Code 2.1.331 (research R1, R2): `-p --resume <id> --fork-session` continues a log that was copied
  and edited by hand, under a new id; the model sees the edited text; the fork log copies the history and records the
  replayed turn's attachments. Not verified: the format of the maintainer's logs and file history on Windows (the
  pilot checks it), and which billing applies to the runs.
- The player keeps the Claude Code logs and file history (#239), and the copy folder is trusted (#238).
- The context hook reads the copy's campaign files, so the restore (FR-004) is what makes its lines match.
- No campaign file, template or kit file changes, so no migration (Development 2), and play pays nothing
  (Development 1).

## Out of Scope

- The baseline from past sessions (#250) and the count in story sessions (#251).
- The trials themselves (#241); the runner only measures.
- Replaying a background session, or a session above 300,000 tokens.
- An automatic scorer: the scores are given by a person or a separate model, blind.
