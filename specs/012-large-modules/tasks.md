# Tasks: Large Modules Cost One Area, Not One Chapter

**Input**: Design documents from `specs/012-large-modules/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Requested by the spec (its Independent Tests and SC-001 to SC-009); written before the code they test.

One PR, on branch `012-large-modules` (this branch, with the spec folder). It says `Part of #179`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: The spec's user story
- Paths are relative to the repository root. Code is C# on .NET 10, Native AOT; warnings are errors; every type and
  member added or changed gets an XML doc comment, private ones included (CLAUDE.md § Documentation).

## Conventions every task follows

- Every failure is one line, `dmc <command>: <message>`: exit 2 for an argument mistake, exit 1 for anything else. No
  reflection, no new dependency.
- A module's checked files are `SourceFiles.Module(folder)`; files are read only through `MarkdownFile`, so fences
  and comments mean the same to the check as to lookup. Sizes are bytes ÷ 4, as `dmc size` counts them.
- Numbers live in `src/Dmc.Cli/Size/Budgets.cs` and nowhere else; the prompts name commands, not numbers.
- Tests use synthetic modules written by the tests in temporary DMC folders (`tests/Dmc.Tests/Support/DmcFolders.cs`),
  and run the built `dmc` through `tests/Dmc.Tests/Support/DmcRunner.cs`. Nothing under `modules/` is committed.
- Build with `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin` before `dotnet test -c Release`.
- Git: never commit to `main`, never force-add anything under `adventures/`, `modules/` or `rules/`, never skip hooks
  or signing. Commit at each checkpoint, as part of this plan. Fixes from a code review are shown to the user and
  committed only on their approval.

---

## Phase 1: Setup

- [X] T001 Fetch `origin` and confirm branch `012-large-modules` sits on an up-to-date `origin/main`
- [X] T002 Build and test the branch as it stands, and record the result
- [X] T003 [P] Measure the standing cost's baseline: bytes ÷ 4 of `kit/dm-rules.md`, `kit/skills/dmc-resume-campaign/SKILL.md`
      and the skill descriptions

---

## Phase 2: Foundational: the budget, the matcher and the fixtures

**Purpose**: What every story uses. Blocks Phases 3 to 6.

- [X] T004 Add `Budgets.ModuleSection = 3000` to `src/Dmc.Cli/Size/Budgets.cs`, with its reason in the XML comment
      (research R2)
- [X] T005 Factor `Lookup.FindSection(MarkdownFile file, string heading)` out of `Lookup.Run` in
      `src/Dmc.Cli/Retrieval/Lookup.cs`: the heading's index, the several matches, or the nearest headings, with no
      change to what `dmc lookup` prints (research R8). Every test in `tests/Dmc.Tests/Retrieval/` passes unchanged
- [X] T006 [P] Write `tests/Dmc.Tests/Modules/ModuleFixtures.cs`: builders for a synthetic module (index, chapters
      with keyed areas `## NN. Name (PDF n)`, a monster file with one stat block per heading, `- AC n; …` and
      `- HP n (…)` lines in both 2014 and 2024 wording), with knobs for each planted problem of quickstart.md § On CI
      step 2, the old skill format (step 3) and a module campaign whose `module-progress.md` names a pointer

**Checkpoint**: build and test pass; commit.

---

## Phase 3: User Story 2: ingest writes a module that lookup can reach (Priority: P1)

**Goal**: `dmc module check` reports the four problems, and the ingest skill writes the shape and runs the check.

**Independent Test**: quickstart.md § On CI steps 1 to 3 and 8.

### Tests

- [X] T007 [P] [US2] Write `tests/Dmc.Tests/Modules/ModuleCheckTests.cs` (library): a sound module gives no problem;
      each planted problem gives exactly its line (data-model.md § Module problem); a section over the limit with
      several stat blocks gives the stat-block line only; `### PLAYER-SAFE` under two parents gives none; a heading
      shared by two files gives none; fenced and commented headings are not headings; a chapter the index lists but
      no file holds is not a problem; `source/` is never read; the old skill format fails and its re-headed copy
      passes (SC-009)
- [X] T008 [P] [US2] Write `tests/Dmc.Tests/Modules/ModuleCommandContract.cs` (black box): every row of
      contracts/dmc-module-check.md § `dmc module check`, including `modules/<NAME>`, a dot name, the 12-line cap
      with `... and <n> more`, `--help`, and that nothing under the module changes (file list and timestamps)
- [X] T009 [P] [US2] Write a timing test beside `RecallTimingTests`: the check over a fixture of about 1.5 MB within
      one second on the release build (SC-006)

### Implementation

- [X] T010 [US2] Write `src/Dmc.Cli/Modules/ModuleCheck.cs`: the sections (research R3), the stat-block rule
      (research R4), the repeat rule (research R5), the lines and their order (data-model.md)
- [X] T011 [US2] Write `src/Dmc.Cli/Modules/ModuleArguments.cs` and `src/Dmc.Cli/Commands/ModuleCommand.cs` (verb
      `check`, `NAME`, `--folder`, `--help`, as `SizeArguments` parses), and add `ModuleCommand` to
      `src/Dmc.Cli/CommandTable.cs`
- [X] T012 [US2] Edit `kit/skills/dmc-ingest-module/SKILL.md` § 3 to § 6 as contracts/prompts.md says
- [X] T013 [P] [US2] Add prompt tests to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`: the ingest skill names
      `dmc module check` at its verify step, and asks for one heading per area, NPC, stat block, item and handout

**Checkpoint**: build and test pass; commit.

---

## Phase 4: User Story 1: a resume reads the index and one area (Priority: P1) 🎯 MVP

**Goal**: The pointer names headings; `dmc size` measures them with lookup's matcher; the resume reads them with
`dmc lookup`.

**Independent Test**: quickstart.md § On CI steps 4, 5 and 7.

### Tests

- [X] T014 [P] [US1] Add to `tests/Dmc.Tests/Size/`: a pointer `§ H` measures the index and that section; `§ H1; § H2`
      measures both, each once; the total does not change when every other chapter grows tenfold (SC-001, SC-002);
      the old forms (`, section H`, a file alone, `none`, the-silver-egg's `none. Consult …`) and a finished module
      measure as before; a heading lookup would match differently from today's "contains, first" is measured as
      lookup matches it; a miss gives the flag line with the nearest headings and the chapter whole (research R8)
- [X] T015 [P] [US1] Add to `tests/Dmc.Tests/Retrieval/`: a stat block from a 37k fixture file prints about 650 tokens
      (SC-003)

### Implementation

- [X] T016 [US1] Edit `src/Dmc.Cli/Size/ReadSet.cs`: parse the pointer's `§` form (research R7), resolve each heading
      with `Lookup.FindSection` on that one file, one part per heading; keep the old forms; and edit
      `src/Dmc.Cli/Size/Flags.cs` for `FlagKind.PointerMiss`, which holds the save
- [X] T017 [US1] Edit `kit/skills/dmc-resume-campaign/SKILL.md` step 9 and `kit/templates/module-progress.md`'s
      comment as contracts/prompts.md says
- [X] T018 [US1] Edit `kit/dm-rules.md` § Running a published module, "Keep your place", as contracts/prompts.md says,
      with no line added; check the existing growth test still passes
- [X] T019 [P] [US1] Add prompt tests: the resume's step 9 names `dmc lookup` for the pointer's headings, and the
      template's comment shows the `§` form

**Checkpoint**: build and test pass; commit.

---

## Phase 5: User Story 3: a module already in a player's folder is checked, not re-ingested (Priority: P2)

**Goal**: `dmc size --check` names a failing module in one line; nothing writes to `modules/`.

**Independent Test**: quickstart.md § On CI step 6.

- [ ] T020 [P] [US3] Add to `tests/Dmc.Tests/Size/SizeCommandContract.cs`: the `module:` line of
      contracts/dmc-module-check.md for a planted module, singular and plural, placed after the flags and before the
      link lines; none for a sound, finished, homebrew or not-ingested module; none after the oversized file is
      split (SC-007); the full report prints it too; `modules/` unchanged after `size`, `module check`, `install` and
      `refresh` in a temporary folder
- [ ] T021 [US3] Edit `src/Dmc.Cli/Commands/SizeCommand.cs` (and `SizeReport` for the full report) to print the line
      from `ModuleCheck`'s count, and its `--help` text
- [ ] T022 [US3] Edit `kit/skills/dmc-resume-campaign/SKILL.md`, where `dmc size --check` runs: a `module:` line is for
      the full save, not mentioned to the player

**Checkpoint**: build and test pass; commit.

---

## Phase 6: User Story 4: the rules say which part comes from where (Priority: P2)

**Goal**: The rules and skills say what is read, looked up and opened in `source/`, and what to do when a lookup
misses. Mostly done by T012, T017 and T018.

- [ ] T023 [US4] Read the changed prompts together against spec FR-015 and FR-016: what a resume reads, what play
      looks up, what is opened in `source/`, the lookup miss; no canon, companion, hook or gate change; no new
      fallback for a missing `dmc` (issue #208). Fix any gap in the file that owns it

---

## Phase 7: Polish: documents, cost, live runs, the PR

- [ ] T024 [P] Edit `.docs/dmc-reference.md`: a `dmc module` section from contracts/dmc-module-check.md; `dmc size`'s
      pointer forms, miss flag and `module:` line
- [ ] T025 [P] Edit `.docs/campaign-files.md`: the pointer's new form in `module-progress.md`, and § Published
      modules: the shape a module is checked against
- [ ] T026 [P] Edit `.docs/user-guide.md` § Published modules: the check, what the player sees of it, and how to
      have DMC split a file the check names
- [ ] T027 [P] Edit `.docs/architecture.md`: resume item 9 reads the pointer's headings through `dmc lookup`
- [ ] T028 [P] Add to `.docs/decisions.md`, each with its reason and the alternatives rejected: the module check and
      its command (R1); the section limit (R2); the stat-block and repeat rules (R4, R5); the `module:` line (R6);
      the pointer form (R7); and an **Amended** line on "Lookup and recall are for play only until Phase D" (R10)
- [ ] T029 [P] Edit `.docs/roadmap.md`: issue #179 Phase C's status
- [ ] T030 Check every new and changed member has an XML doc comment, private ones included, and that
      `specs/012-large-modules/` matches what ships
- [ ] T031 Measure the standing cost again as in T003 and record it for the PR (research R12)
- [ ] T032 Run quickstart.md § Live: `dmc module check the-crooked-moon` with the release build against
      `D:\Claude\dmc` (read-only), and a headless resume in `D:\Claude\test-campaign` with
      `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0` of a campaign on a synthetic module, played into a fight with a module
      creature; record what DMC read at the resume (the index and the pointer's headings only) and that it looked the
      stat block up rather than reading the reference file (User Story 4's Independent Test)
- [ ] T033 Open the PR, then check it:
      - commit, push, and open the PR with the full description: what changed, why, the sources (spec 012, the
        research items, issue #179 step 7), the tests, the documents changed, why no migration is needed (the
        template keeps its line and label and every old pointer stays valid), the measured cost, and
        `Part of #179` on its own line;
      - check the description with `gh pr view <n> --json body`
- [ ] T034 Finish the PR: run the built-in code review once and check each finding yourself; show the fixes to the
      user and commit them only on their approval; check the documents; wait for CI to pass, then ask the user to
      merge
- [ ] T035 After the PR merges, the user runs `/speckit-converge`. The spec is frozen once a pass adds no tasks and
      its PR has merged (`.docs/development.md` § Spec-driven changes)

---

## Dependencies and order

- **Phase 1 → Phase 2** → the stories. T005 blocks T016; T004 and T006 block T007 to T010.
- **US2 (Phase 3) before US3 (Phase 5)**: the `module:` line uses `ModuleCheck`.
- **US1 (Phase 4)** depends only on Phase 2 and can run beside Phase 3.
- **Phase 6** after T012, T017 and T018. **Phase 7** last.

## Requirement coverage

| Requirements | Tasks |
|---|---|
| FR-001 to FR-004 (resume, pointer, `dmc size`) | T005, T014, T016, T017, T018 |
| FR-005 to FR-011 (the check, ingest) | T004, T006 to T013 |
| FR-012 to FR-014 (existing modules) | T005, T020 to T022 |
| FR-015, FR-016 (the rules, Phase D kept out) | T018, T023, T028 |
| FR-017 (documents) | T024 to T030 |
| FR-018 (constraints) | every code task; T030 |
| SC-001 to SC-003 | T014, T015 |
| SC-004, SC-009 | T007 |
| SC-005 | T032 |
| SC-006 | T009 |
| SC-007 | T020 |
| SC-008 | T018, T031 |

## Parallel examples

- After Phase 2: T007, T008, T009 (US2 tests) beside T014, T015 (US1 tests).
- In Phase 7: T024 to T029, each a different document.

## Implementation strategy

MVP is US1 with US2's check behind it: the resume saving needs the pointer, and the shape that makes the pointer
work needs the check. Deliver all four stories in the one PR; the phases are checkpoints, each committed with the
build and tests passing.
