---

description: "Task list for spec 019, the break baseline after a typed resume"
---

# Tasks: The break baseline after a typed resume

**Input**: Design documents from `specs/019-typed-resume-baseline/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: The spec asks for them (FR-008), and `contracts/context-check-baseline.md` lists the cases. In each story
the tests come first and must fail before the code they cover, except the ones marked "passes today".

**Organization**: Tasks are grouped by user story, so each story can be built and checked on its own.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (a different file, and no dependency on an unfinished task)
- **[Story]**: the user story the task belongs to (US1 to US3)

## Path conventions

- CLI: `src/Dmc.Cli/`. Tests: `tests/Dmc.Tests/` (MSTest). Documents: `.docs/`.
- Line numbers are from `origin/main` at `68c3396`.
- Every new or changed type and member gets an XML doc comment, private ones included (CLAUDE.md § Documentation).
- Notation of the contract: `U` a player message, `S(n)` a main `Skill` call of `dmc-resume-campaign` at context
  `n`, `A(n)` a main call, `R` a tool result, `B` a compaction boundary.

---

## Phase 1: Setup

**Purpose**: a known starting point

- [X] T001 Check that the branch `019-typed-resume-baseline` has `origin/main` at `68c3396` or later, then run
  `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~ContextCheck|FullyQualifiedName~SheetReminder"`.
  Record the pass count, so T016 can show that nothing that passed now fails.
- [X] T002 Only if the maintainer agrees (agreed on 2026-11-18): in
  `specs/019-typed-resume-baseline/spec.md` § Background, "The evidence", change "#237 found this case in 1 of 89 play
  sessions" to say that #237 found 1 of 89, and that a count on 2026-11-18 found 3 of 115 main play logs
  (`082db04e`, `6a4e09c3`, `c88dd223`). The count is by the script `$CLAUDE_JOB_DIR/tmp/resume_cases.py`, which stays
  outside the repository. Make the same change in User Story 3's "Why this priority". Otherwise leave the spec as it
  is.

---

## Phase 2: Foundational

**Purpose**: the test helper that US1 and US3 both use

- [X] T003 In `tests/Dmc.Tests/ContextCheck/ContextCheckTests.cs`, next to `Assistant` (:55), add a static helper
  `SkillCall(int context, string skill = "dmc-resume-campaign", bool sidechain = false, bool usage = true)` with an
  XML doc comment. It returns the JSON line of `ContextCheckContract.Assistant(context, sidechain)`, with
  `message.content` replaced by one block:
  `{"type":"tool_use","id":"s1","name":"Skill","input":{"skill":<skill>}}`. When `usage` is false, it removes
  `message.usage`. This shape is the one in log `082db04e` line 38 (research R1).

---

## Phase 3: User Story 1 - A typed resume does not bring an early break offer (Priority: P1) 🎯 MVP

**Goal**: when a main `Skill` call of the resume comes in one of the first three turns of a log part, the last call
of the latest such turn is the baseline (FR-001, FR-002).

**Independent test**: `TranscriptReader.Read` on the first row of contract C1 gives `(154000, 146000)`. Today it
gives `(154000, 37000)`.

### Tests for User Story 1

- [X] T004 [US1] In `tests/Dmc.Tests/ContextCheck/ContextCheckTests.cs`, after `ARecapNobodyHasAnsweredYetIsItsOwnBaseline`
  (:159), add one test for each of these rows of contract C1. Each asserts the exact `ContextReading`:
  - `ATypedResumeSetsTheBaselineAtTheEndOfItsTurn`: `U S(37000) R A(120000) A(146000) U A(150000) U A(154000)` →
    `(154000, 146000)`.
  - `AResumeInTheSecondTurnSetsTheBaseline`: `U A(30000) U S(32000) R A(140000) U A(150000)` → `(150000, 140000)`.
  - `TwoResumeCallsInOneTurnGiveItsLastCall`: `U S(37000) S(38000) R A(146000) U A(150000)` → `(150000, 146000)`.
  - `AResumeTurnStillRunningIsItsOwnBaseline`: `U S(37000) R A(146000)` → `(146000, 146000)`.
  - `TheOldResumeNameCountsToo`: the first row with `SkillCall(37000, "resume-campaign")` → `(154000, 146000)`.
  - `AResumeCallWithNoUsageStillMarksItsTurn`: `U SkillCall(0, usage: false) R A(146000) U A(150000)` →
    `(150000, 146000)`.

  The `U` is `User("Resume campaign.")` for the resume turn and `User()` elsewhere. `R` is `ToolResult`.
- [X] T005 [US1] In the same file, after `ABaselineAbove150kMovesTheFirstBandUp` (:247), add
  `ATypedResumeGetsNoBreakLineAt154k`. Use the same `Run` pattern on
  `User("Resume campaign."), SkillCall(37000), ToolResult, Assistant(120000), Assistant(146000), User(), Assistant(154000)`.
  The output is `""`. Then add `Assistant(186000)` at the end. The output starts with
  `"Context 186k is large, break due since 186k:"` (SC-001).
- [X] T006 [US1] Run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~ContextCheckTests"`. T004's six tests
  and T005 must fail, and every other test must still pass.

### Implementation for User Story 1

- [X] T007 [US1] In `src/Dmc.Cli/ContextCheck/TranscriptReader.cs`, beside `Classify` (:331), add a private static
  helper `CallsResume(ReadOnlyMemory<byte> line)` with an XML doc comment (research R1, R2):
  - Return false at once unless the line holds the bytes `resume-campaign` or `\u`. This is the same safe-no rule as
    `CouldBeRelevant` (:389-398).
  - Drop a UTF-8 BOM, then parse with the existing `Parse`. Return true only for an object with `type` `"assistant"`,
    no truthy `isSidechain`, and a `message.content` array holding a block with `type` `"tool_use"`, `name` `"Skill"`
    and an `input` object whose `skill` is exactly `"dmc-resume-campaign"` or `"resume-campaign"`.
  - Never throw. A shape it does not know returns false.
  - Do not change `Classify`, `Classified` or `RecordKind`.
- [X] T008 [US1] In `BaselineOf` (:156-201) of the same file, add the per-part values of data-model.md § "The state
  of the forward pass": `players` (counted up to 4), `resumeInTurn`, `lastInTurn` and `resumeBaseline`. Keep
  `before` and `baseline` exactly as they are.
  - On a `RecordKind.Compaction`, reset all of them to their start values.
  - On a `RecordKind.Player`, first close the current turn: if `resumeInTurn` and `1 <= players <= 3`, set
    `resumeBaseline = lastInTurn`. Then increment `players` (cap at 4), and set `resumeInTurn = false` and
    `lastInTurn = 0`. The present `when !playerSeen` branch still sets `baseline = before` on the first one.
  - On a `RecordKind.Assistant` while `players` is 1 to 3, set `lastInTurn = found.Context`, along with the present
    updates of `before` and `baseline`.
  - While `players` is 1 to 3, also set `resumeInTurn = true` when `CallsResume(line)`. Call it on every line in
    those turns, whatever `Classify` returns or throws: a resume record with zero usage is `Skipped` by `Classify`
    and must still mark the turn.
  - Change `settled` (:166) to `baseline > 0 && players > 3`, so the cheap skip starts only after the fourth player
    message (research R4).
  - After the scan, close the last turn the same way: if `resumeInTurn` and `players` is 1 to 3, set
    `resumeBaseline = lastInTurn`.
  - Return `resumeBaseline > 0 ? resumeBaseline : (playerSeen ? baseline : latest)`.
- [X] T009 [US1] In the same file, update the doc comments so they state the new rule and copy no one else's:
  - The class summary (:7-32): add to the numbered list that a resume `Skill` call in one of the first three turns
    of the part makes the last call of the latest such turn the baseline. Change "This needs no marker for the
    resume" to say that it needs none outside those turns, and that a resume after the third player message cannot
    move the baseline up.
  - `Read` (:46-67), `BaselineOf` (:148-155), and `ContextReading`'s `Baseline` parameter (:531).
- [X] T010 [US1] Run T006's filter again. All tests pass, including T004 and T005.

**Checkpoint**: US1 works on its own. A typed resume gets the right bands.

---

## Phase 4: User Story 3 - Every other way into play behaves as today (Priority: P1)

**Goal**: the slash command, a late resume, a sub-agent's call and a part after a compaction keep the present rule
(FR-003, FR-006).

**Independent test**: the present baseline tests and the new ones below pass.

### Tests for User Story 3

- [X] T011 [US3] In `tests/Dmc.Tests/ContextCheck/ContextCheckTests.cs`, next to T004's tests, add:
  - `AResumeInTheFourthTurnDoesNotMoveTheBaseline`:
    `U A(30000) U A(31000) U A(32000) U S(33000) R A(140000) U A(150000)` → `(150000, 30000)`.
  - `ASubAgentsResumeCallDoesNotCount`: `U SkillCall(37000, sidechain: true) A(40000) U A(50000)` →
    `(50000, 40000)`.
  - `TurnsAreCountedFromTheLatestCompaction`: `U A(250000) Boundary CompactSummary A(30000) U S(31000) R A(120000) U A(130000)`
    → `(130000, 120000)`.

  Each passes once T008 is done. The first two pass today as well, and must keep passing.
- [X] T012 [US3] Check, without editing them, that `AfterOneResumeTheBaselineIsTheEndOfTheRecap` (:132),
  `TheRenamedResumeSkillGivesTheSameBaselineAsTheOldOne` (:141), `AResumeMidSessionDoesNotMoveTheBaseline` (:152),
  `OnlyThePartAfterTheLatestCompactionCounts`, `ABadOlderRecordIsSkippedWhenFindingTheBaseline` (:216) and
  `TheBaselineIsFoundInALogLongerThanSeveralChunks` (:233) pass, with no change to what they expect (SC-003). If one
  fails, fix the code, not the test.

**Checkpoint**: US1 and US3 together. The fix moves only the typed-resume case.

---

## Phase 5: User Story 2 - The first typed message after a `/clear` prints no sheet lines (Priority: P2)

**Goal**: no sheet lines while the named log has no main call after its latest boundary (FR-005).

**Independent test**: a hook input with a log of only a `/clear` echo gives no `Sheets of` header. The same input
with an empty `transcript_path` still gives the lines.

### Tests for User Story 2

- [X] T013 [P] [US2] In `tests/Dmc.Tests/ContextCheck/SheetReminderContract.cs`, after `ACompactionStartsAfresh`
  (:190), add two tests with XML doc comments:
  - `NoSheetLinesBeforeTheFirstReplyAfterAClear`: a log whose one record is `ContextCheckContract.User("<command-name>/clear</command-name>")`,
    and `Input("Resume campaign.", log)`. `SheetPart` is empty. `Input("Resume campaign.")`, with no log, still
    gives `Header("Tam Birch", "Hala Rornsdottir")` first.
  - `NoSheetLinesWhileACompactionIsTheLatestThing`: `Log(Call(50_000), Boundary())`. `SheetPart` is empty.

  Both must fail before T014. The class runs in-process (`SheetReminderInProcessTests`) and through any other
  subclass of the contract.

### Implementation for User Story 2

- [X] T014 [US2] In `src/Dmc.Cli/Retrieval/SheetReminder.cs`, in `For` (:79), return `Found.None` when `transcript_path`
  is a non-empty string and `contextNow` is 0 (research R5). Read the field once and reuse it at :101. Put the check
  after the prompt test (:82-83) and before any file is read, so the message costs no reads. Update the doc comments:
  - On `For`: the summary's list of "nothing when" cases, and the `contextNow` parameter ("0 when the log has no
    reply since its latest boundary, and then nothing is printed; also 0 when no log is named").
  - On `Recent` (:198-201) and `Seen` (:209-221): their `contextNow` 0 branch is now reached only by a direct caller.
- [X] T015 [US2] Run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~SheetReminder"`. All pass, and every
  test that existed before has the same expected result.

**Checkpoint**: all three stories work.

---

## Phase 6: Polish & cross-cutting concerns

**Purpose**: the documents the plan names (§ Documents), the quickstart and the PR

- [X] T016 Run the full suite and both builds: `dotnet test tests/Dmc.Tests`, `dotnet build -c Debug` and
  `dotnet build -c Release`. All pass with 0 warnings, and no test that passed in T001 fails.
  *Done 2026-11-18:* 2,194 passed, 5 skipped (platform-only, as on `main`), 0 failed; Debug and Release build with 0 warnings. The black-box tests run the published `artifacts/bin/dmc`, so republish after each source change.
- [X] T017 [P] In `src/Dmc.Cli/Commands/ContextCheckCommand.cs`, in the class summary (:6-20), add after the sheet
  lines' sentence: "none before DMC's first reply after a `/clear` or a compaction". Make no code change.
- [X] T018 [P] In `.docs/dmc-reference.md` § `context-check`:
  - In the baseline paragraph (:821), add the typed-resume rule: a main `Skill` call of `dmc-resume-campaign` (or
    `resume-campaign`) in one of the first three turns of the part makes the end of the latest such turn the baseline.
  - In the "needs no marker" paragraph (:825), say it holds outside those turns. Keep the compaction limit, and add
    that it may no longer apply when DMC reloads with a `Skill` call in the next three turns. That is unverified,
    because no log shows a reload after a compaction (research R6).
  - In the sheet-lines paragraph, add that the hook prints none while the log has no reply since its latest boundary.
  - At :827, update the timing figures only if T022 changes them.
- [X] T019 [P] In `.docs/decisions.md`, add an entry "A typed resume sets the break baseline" in the file's format
  (Date 2026-11-18, Decision, Reason, Source). It covers:
  - the decision (FR-001 to FR-005);
  - the reason: log `082db04e`, where the break offer came at 154,000 tokens, mid-correction, against a right band
    of about 186,000;
  - the options rejected in research R2, R3 and R5;
  - R7, a slash resume after a typed message, which is not covered: 0 cases in 115 logs, and counting it would
    change `AResumeMidSessionDoesNotMoveTheBaseline`.

  Its Source is issue #244, #238 and this spec. On the Date line of "The break alert is relative to where the resume
  left the context" (:144), add "amended 2026-11-18 by "A typed resume sets the break baseline"".
- [X] T020 [P] In `.docs/architecture.md:71`, change the parenthesis "(where the context settled before the first
  player message after the latest compaction, usually the end of the resume's recap)" to add "or the end of a typed
  resume in the first three messages". The rule stays in `dmc-reference.md`.
- [X] T021 [P] In `.docs/roadmap.md`, add a row for spec 019, "The break baseline after a typed resume, issue #244
  (slip plan #238, Phase 0)". Its status is "Done (PR #N), for the next release", and it says that it changes the
  binary only.
- [X] T022 Run quickstart steps 2 to 4 (`specs/019-typed-resume-baseline/quickstart.md`), on copies in
  `$CLAUDE_JOB_DIR/tmp/019` only. Record for the PR description:
  - step 2: whether there is a break line on the cut `082db04e` log, on this branch and on `main`;
  - step 3: the sheet lines with and without a log;
  - step 4: the times for no log, the largest main log and an 18 MB synthetic log, on this branch and on `main`, in
    the release build. If the C++ build tools are missing, say so; CI's release build is then the check.
  *Run 2026-11-18 on copies in `$CLAUDE_JOB_DIR/tmp/019`:* step 2: on the real `082db04e` log, `main` prints the break line from the sixth player message (the fifth after the resume, "Context 154k is large, break due since 150k"); the branch prints none. The resume turn ends at 146,190 tokens, so the first band is 186,190. Step 3, on `D:\Claude	est-campaign` (read only): with a log of only the `/clear`, `main` prints 9,181 bytes of sheet lines for "Resume campaign." and the branch prints none; with no log both print them. Step 4: the C++ linker is missing, so no AOT build; with the default build, 20 runs each, branch against `main`: no log 87 against 87 ms, the largest main log (7.1 MB) 128 against 129 ms, an 18.2 MB synthetic log 114 against 116 ms. No change, so `.docs/dmc-reference.md`'s figures stay. CI's release build is the AOT check.
- [X] T023 Run `/speckit-converge` on the branch until it adds no tasks, and fold its tasks into this PR
  (`.docs/development.md` § Spec-driven changes).
  *Done 2026-11-19:* the first run added T026 and T027 (Phase 7); the second run, after T026, added nothing.
- [X] T024 Commit, push and open the PR from `019-typed-resume-baseline`. The description says `Closes #244` on its
  own line. It also gives:
  - what changed, and why;
  - the sources: `082db04e`, #237 and #238;
  - the tests, with T016's counts and T022's results;
  - the documents changed;
  - that no migration is needed, because no campaign file, template or skill changes.

  Check it with `gh pr view <n> --json body`.
  *Done 2026-11-19:* PR #271; `gh pr view 271 --json body` shows the full description.
- [X] T025 Run the code-review command once on the PR. Present its fixes to the maintainer and get approval before
  committing them. Check the documents are current whether or not the review found anything. Wait for CI to pass
  before asking the maintainer to merge.

---

## Dependencies & execution order

### Phase dependencies

- **Setup (Phase 1)**: first. T002 only if the maintainer agrees.
- **Foundational (Phase 2)**: T003 before T004, T005 and T011.
- **US1 (Phase 3)**: after T003.
- **US3 (Phase 4)**: after T008. It uses the same files as US1, so it runs after it.
- **US2 (Phase 5)**: independent of US1 and US3, because it touches other files. It can run at any time after T001.
- **Polish (Phase 6)**: after every story. T017 to T021 can run in parallel. T022 to T025 come last, in order.

### Within each story

- Tests first, failing (T006), then the code, then a filtered run (T010, T015).
- US1: T007 (the helper) comes before T008 (its caller), and T009 after T008.

### Parallel opportunities

- US2 (T013 to T015) alongside US1 (T004 to T010): different source and test files.
- T017 to T021: five different files.

---

## Parallel example: US1 and US2 together

```text
Task: "T004–T010 in TranscriptReader.cs and ContextCheckTests.cs"
Task: "T013–T015 in SheetReminder.cs and SheetReminderContract.cs"
```

## Parallel example: Polish

```text
Task: "T017 ContextCheckCommand.cs summary"
Task: "T018 .docs/dmc-reference.md § context-check"
Task: "T019 .docs/decisions.md entry"
Task: "T020 .docs/architecture.md:71"
Task: "T021 .docs/roadmap.md row"
```

---

## Implementation strategy

- **MVP**: Phases 1 to 3 (US1). They fix the defect of issue #244 by themselves.
- **Then**: US3 (Phase 4) proves nothing else moved. US2 (Phase 5) removes the wasted sheet lines.
- **One PR** for all of it, after converge and the review (T023 to T025), closing #244.

## Phase 7: Convergence

- [X] T026 In `tests/Dmc.Tests/ContextCheck/ContextCheckTests.cs`, next to `ASubAgentsResumeCallOrAnotherSkillDoesNotCount`, add `AnEscapedOrBadResumeRecordIsHandled`: (a) a resume call whose skill name is written with a `\u` escape (`dmc-resume-campaign` in the raw JSON line) still marks its turn, so `TypedResume, Assistant(37000), <escaped call at 38000>, ToolResult, Assistant(146000), User(), Assistant(150000)` reads `(150000, 146000)`; (b) a record of an unexpected shape in the resume turn (`{"type": "assistant", "message": "text"}` and `{"type": "assistant", "message": {"content": 5}}`) is skipped and the reading is the same as without it. Run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~ContextCheckEdgeTests"` per Edge Cases and research R2 (partial)
- [X] T027 After T024 opens the PR, replace "PR #N" in the spec 019 row of `.docs/roadmap.md` (:55) with the PR's number, and push the change to the same PR per FR-009 and T021 (partial)
  *Done 2026-11-19:* the row says PR #271.

## Phase 8: Code review of PR #271

- [X] T028 Apply the nine points of the code review of PR #271, approved by the maintainer on 2026-11-19 ("apply all"):
  (1) only the part's first resume can set the baseline, and a resume before the first player message or a slash
  resume in turns 1 to 3 is the first one (spec FR-001, FR-002; tests `OnlyTheFirstResumeTurnCounts`,
  `AResumeBeforeTheFirstMessageKeepsTheBaseline`, `ASlashResumeInTheFirstTurnsIsTheFirstResume`); (2) no sheet lines
  only in a fresh log, so they print right after a compaction (FR-005, User Story 2; `TranscriptReader.IsFresh`; test
  `TheSheetLinesStillPrintRightAfterACompaction`); (3) the bad records of `AnEscapedOrBadResumeRecordIsHandled` parse and
  reach the check; (4) the resume helper `ResumeIn` parses only lines with `resume-campaign` and `"assistant"` or
  `command-name`, or a `\u` escape; (5) the pass settles once the first resume turn closes; (6) the `contextNow == 0`
  branches of `Recent` and `Seen` are reachable again after (2), and their comments are as before; (7) the fresh log is a
  `freshLog` flag that `ContextCheckCommand` passes to `SheetReminder.For`, not a meaning of `contextNow`; (8)
  `ParseLine` and `TryContent` are shared by `Classify`, `TextNames` and `ResumeIn`; (9) XML doc comments on the new test
  methods. Documents: `.docs/decisions.md`, `.docs/dmc-reference.md`, the spec, research R10, the data model and the
  contract.
  *Done 2026-11-19 (commit `c12bea5`):* 2,198 passed, 5 skipped, 0 failed; Debug and Release with 0 warnings. On
  `082db04e` `main` still prints the break line at 154k and the branch none; a typed first message after `/clear`
  prints 0 bytes of sheet lines; default build, branch against `main`: no log 96 against 98 ms, 7.1 MB 140 against
  140 ms, 18.2 MB 126 against 129 ms.
