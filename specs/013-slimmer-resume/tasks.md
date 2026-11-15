# Tasks: A Slimmer Resume (issue #179, Phase D)

**Input**: Design documents from `specs/013-slimmer-resume/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Requested by the spec (its Independent Tests and SC-001 to SC-009); written before the code they test.

One PR, on branch `013-slimmer-resume`. It says `Part of #179`: the gate is checked after release (FR-031).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: The spec's user story
- Paths are relative to the repository root. Code is C# on .NET 10, Native AOT; warnings are errors; every type and
  member added or changed gets an XML doc comment, private ones included (CLAUDE.md § Documentation).

## Conventions every task follows

- Every failure is one line, `dmc <command>: <message>`: exit 2 for an argument mistake, exit 1 otherwise.
  `context-check` never fails. No reflection, no new dependency, no fallback for a missing `dmc`.
- Numbers live in `src/Dmc.Cli/Size/Budgets.cs`; the prompts name commands, not numbers.
- Tests use synthetic campaigns in temporary DMC folders (`tests/Dmc.Tests/Support/`). Nothing under `adventures/`,
  `modules/` or `rules/` is committed or force-added. `D:\Claude\dmc` is read only.
- Build with `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin` before `dotnet test -c Release`.
- Commit at each checkpoint, as part of this plan. Fixes from the code review are shown to the user and committed only
  on their approval.

---

## Phase 1: Setup

- [x] T001 Confirm branch `013-slimmer-resume` sits on an up-to-date `origin/main`; build and test it as it stands and
      record the result
- [x] T002 [P] Measure the standing cost's baseline (bytes ÷ 4) of `kit/dm-rules.md`, the resume, full save, sheet
      update and review-transcript skills, `kit/templates/npc-sheet.md`, `pc-sheet.md`, `session-log.md` and
      `campaign/story/canon.md`, and every skill's description (research R9)

## Phase 2: Foundational: budgets, the register and the scene read's core

**Purpose**: What every story uses. Blocks Phases 3 to 7.

- [x] T003 Add `Budgets.PlayCard` (1,000), `Budgets.CanonWholeBelow` (6,000) and `Budgets.SceneReadBytes` (12,000),
      with their reasons, in `src/Dmc.Cli/Size/Budgets.cs`; add `BudgetKind.PlayCard`
- [x] T004 [P] Write tests for the register's parse (bold spans before the first colon outside brackets, `(also: …)`
      items, words of 3 letters or more outside `ReadSet.CommonWords`, whole-word case-insensitive match, Rorn's
      `(properly **Hysvearorn**, …)` bracket not read as aliases, a two-person entry) in
      `tests/Dmc.Tests/Retrieval/RegisterTests.cs`
- [x] T005 Implement `src/Dmc.Cli/Retrieval/Register.cs` (research R6, data-model § Register) until T004 passes
- [x] T006 [P] Write tests for the scene's words from `game-state.md` (Scene fields, Active quests, Pending; comments
      left out; none found) in `tests/Dmc.Tests/Retrieval/SceneReadTests.cs`
- [x] T007 Implement `src/Dmc.Cli/Retrieval/SceneRead.cs`: the words, canon outside `ReadSet.AlwaysReadSections`,
      ranked with `Recall.Rank`, printed with `Recall.Hit` within `Budgets.SceneReadBytes`, the "small" line under
      `Budgets.CanonWholeBelow` (contracts/dmc-recall-scene.md), until T006 passes

**Checkpoint**: commit.

## Phase 3: User Story 1: a companion costs a play card at resume (Priority: P1) 🎯 MVP

- [x] T008 [P] [US1] Write `dmc size` tests: a companion sheet with `## Play card` first is measured to the card's end
      against 1k and shown `to its play card`; one without is measured to its history and flagged `a companion with
      no play card: the full save writes one`; the PC and a Present NPC unchanged, in `tests/Dmc.Tests/Size/`
- [x] T009 [US1] Implement the card part in `src/Dmc.Cli/Size/ReadSet.cs` (a new `PartKind.ToPlayCard` in
      `Measurement.cs`), the flag in `src/Dmc.Cli/Size/Flags.cs`, the report label in `SizeReport.cs`, and add
      `## Play card` to `ReadSet.Markers`, until T008 passes
- [x] T010 [P] [US1] Add the `## Play card` section, with its fields and a comment that it is for a companion only and
      is the only copy of its numbers, to `kit/templates/npc-sheet.md` and `kit/templates/pc-sheet.md` (research R1)
- [x] T011 [US1] Edit `kit/skills/dmc-resume-campaign/SKILL.md` item 5: a companion's sheet is read through its play
      card; one with none down to its history, as now; the "read the history only when…" rule extended to the rest of
      a companion's sheet
- [x] T012 [US1] Edit `kit/dm-rules.md` line 78's last sentence to cover FR-005 in one sentence (research R2); measure
      its bytes
- [x] T013 [P] [US1] Edit `kit/skills/dmc-character-sheet-update/SKILL.md`: write the card when a character becomes a
      companion; keep it current in the same edit as the fact it holds
- [x] T014 [P] [US1] Edit `kit/skills/dmc-campaign-full-save/SKILL.md`: keep each card within its budget, write one for
      a flagged companion, and compare the cards' max HP and items with `game-state.md` in the consistency check

**Checkpoint**: build, test, commit.

## Phase 4: User Story 2: migration 006 (Priority: P1)

- [x] T015 [P] [US2] Write tests for the shape checks `companions-have-play-card` and `people-entries-named` in
      `tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`
- [x] T016 [US2] Implement both checks in `src/Dmc.Cli/Campaign/MigrationChecks.cs`, `people-entries-named` through
      `Register`, until T015 passes
- [x] T017 [US2] Write `kit/migrations/006-play-cards-and-names.md` (standard 6, touches, no drops, both checks, steps
      and judgments as research R7 says)
- [x] T018 [US2] Build the golden fixture `tests/Dmc.Tests/Fixtures/migrations/006/` (before, after, accounting) with
      two companions (one built on the PC template, one with history in the middle), a Party status block that is not
      a character, a "With the party, not companions" block, and People entries with and without other names; make
      the migration tests pass
- [x] T019 [US2] Check the standard: `dmc campaign standard` reports `behind` for a standard-5 campaign and the migrate
      skill lists 006; adjust tests in `tests/Dmc.Tests/Campaign/` that assume the kit is at standard 5

**Checkpoint**: build, test, commit.

## Phase 5: User Story 3: canon read as Rulings, Prices and the scene's matches (Priority: P1)

- [x] T020 [P] [US3] Write the contract tests for `dmc recall NAME --scene` (small canon, hits within 12,000 bytes and
      the closing line, nothing matched, no scene, words with `--scene` exit 2, no canon exit 1), in-process and
      through the binary, in `tests/Dmc.Tests/Retrieval/RecallSceneContract.cs`
- [x] T021 [US3] Add `--scene` to `src/Dmc.Cli/Retrieval/RetrievalArguments.cs` and `RecallCommand.cs` (help text
      included), calling `SceneRead`, until T020 passes
- [x] T022 [P] [US3] Write `dmc size` tests: canon under 6k read whole; at or over it, the always-read sections plus
      the scene read's bytes, shown `read in sections and by scene`; the over-limit flag's new wording; the measured
      part equals what `--scene` prints (SC-004), in `tests/Dmc.Tests/Size/`
- [x] T023 [US3] Implement the canon part in `src/Dmc.Cli/Size/ReadSet.cs` and the flag wording in `Flags.cs`; add
      `--scene` to `ReadSet.Markers`, until T022 passes
- [x] T024 [US3] Edit `kit/skills/dmc-resume-campaign/SKILL.md` item 7: run `dmc recall <campaign> --scene`; on the
      "small" line read canon whole; otherwise read the always-read sections whole and take the lines as the scene's
      matches; the joined-terms Grep and its escaping rules go; § 2's first paragraph no longer decides item 7 by the
      canon line
- [x] T025 [US3] Edit `kit/skills/dmc-campaign-full-save/SKILL.md`'s canon step: a fact that must hold in every scene
      belongs in an always-read section, on a card or under Pending (FR-010); a new People entry in the register's
      shape (FR-019)
- [x] T026 [P] [US3] Edit `kit/templates/campaign/story/canon.md`: the People comment gives the entry's shape
      (`- **Name** (also: …): …`); the paragraph on how canon is read says "read in part at every resume"
- [x] T027 [P] [US3] Add a timing test for `--scene` on a synthetic canon the size of Rorn's Landing's, on the release
      build, as `RecallTimingTests` does, in `tests/Dmc.Tests/Retrieval/`

**Checkpoint**: build, test, commit.

## Phase 6: User Story 5: the hook's canon reminder (Priority: P3)

- [x] T028 [P] [US5] Write tests: a prompt naming two registered people prints the header and at most 3 lines each
      within 4,000 bytes; a log holding that header after its latest compaction boundary prints nothing for them; a
      header before the boundary does not count; silence for no campaign, a closed one, a `/` prompt, no name, no
      canon, bad JSON; the break line still first; always exit 0, in `tests/Dmc.Tests/ContextCheck/`
- [x] T029 [US5] Implement `src/Dmc.Cli/Retrieval/CanonReminder.cs` (with the reminded names in its `Reminded`),
      and the call in `src/Dmc.Cli/Commands/ContextCheckCommand.cs`,
      until T028 passes
- [x] T030 [P] [US5] Add a timing test for the hook's canon part on the release build (SC-007)
- [x] T031 [US5] Edit `kit/skills/dmc-character-sheet-update/SKILL.md`: a new character's People entry in the
      register's shape

**Checkpoint**: build, test, commit.

## Phase 7: User Story 4: the gate (Priority: P2)

- [x] T032 [P] [US4] Add the `Slip:` bullet to `kit/templates/session-log.md`'s scene comment, and "and log a `Slip:`
      line" to `kit/dm-rules.md` line 85's sentence (FR-033, research R8); measure the bytes
- [x] T033 [US4] Add "The slip tally" to `.claude/skills/review-transcript/SKILL.md`: where slips are recorded, the
      categories, the qualifying sessions, the baseline file, the table, the threshold, the rollback per category and
      where the result is recorded (FR-020 to FR-027)
- [x] T034 [US4] Count the baseline: every session log of The Silver Egg and Rorn's Landing in `D:\Claude\dmc` (read
      only), plus the sheets', canon's and `dm-notes.md`'s slip notes, by category, into
      `specs/013-slimmer-resume/baseline.md`, with Rorn's Landing's own average beside the combined one

**Checkpoint**: commit.

## Phase 8: Polish: documents, cost, live runs, the PR

- [x] T035 Tighten the toolkit test in `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` to 212 lines for `kit/dm-rules.md`
      and add the byte cap (the T002 figure + 400) (FR-028)
- [x] T036 [P] Edit `.docs/campaign-files.md`: the play card, the People shape, canon's read, the budgets table
      (equal to `Budgets`), standard 6
- [x] T037 [P] Edit `.docs/dmc-reference.md`: `recall --scene`, `size`'s new labels and flag, `context-check`'s
      reminder
- [x] T038 [P] Edit `.docs/architecture.md` (resume items 5 and 7, the hook) and `.docs/user-guide.md` (what a
      resume reads, the migration)
- [x] T039 [P] Edit `.docs/decisions.md`: decisions for the play card, canon by scene, the hook and register, and the
      gate (with the baseline totals); amend "Lookup and recall are for play only until Phase D" and "Size rules are
      measured in tokens by `dmc size`"
- [x] T040 [P] Edit `.docs/roadmap.md`: issue #179 Phase D shipped, the gate pending
- [x] T041 Measure the standing cost again as in T002 and record the change by when it is paid (FR-029)
- [x] T042 Live checks on a scratch copy of Rorn's Landing: migrate by migration 006, then `dmc size` and
      `dmc recall --scene` (quickstart 1, 2, 4; SC-001 to SC-004, SC-006)
- [x] T043 Headless play test in `D:\Claude\test-campaign` (quickstart 5): record `.active-campaign` and the campaign
      first, ask the user to refresh the folder from the build, run, restore both
- [x] T044 Open the PR with the full description (what, why, sources, tests, documents, migration 006, the standing
      cost, `Part of #179`), then check it with `gh pr view <n> --json body`
- [x] T045 Run the built-in code review once on the PR; check each finding; show the fixes to the user and commit
      them only on approval; check the documents are current; ask the user to merge once CI passes
- [x] T046 After the merge: delete the branch; run `/speckit-converge` until a pass appends nothing, then tick the last
      tasks in a small PR

## Dependencies and order

- Phase 1 → Phase 2 → the stories. T005 blocks T016 and T029; T007 blocks T021 and T023.
- US1 (Phase 3) before US2 (Phase 4): the migration writes the card the size code reads.
- US3 (Phase 5) depends only on Phase 2. US5 (Phase 6) after T005. US4 (Phase 7) after T012 (both edit `dm-rules.md`).
- Phase 8 last.

## Requirement coverage

| Requirement | Tasks |
|---|---|
| FR-001 to FR-006a (cards) | T008 to T014 |
| FR-007 to FR-010 (canon by scene) | T006, T007, T020 to T027 |
| FR-011 to FR-014 (migration 006) | T015 to T019 |
| FR-015 to FR-019 (hook, register) | T004, T005, T028 to T031, T026 |
| FR-020 to FR-027, FR-033 (gate, slip line) | T032 to T034 |
| FR-028, FR-029 (cost) | T002, T012, T032, T035, T041 |
| FR-030 (documents) | T036 to T040 |
| FR-031, FR-032 (links, limits) | T044, T045 |
| SC-001 to SC-009 | T042, T043, T027, T030, T034 |

## Implementation strategy

US1 is the MVP: cards alone save about 11k. Deliver all five stories in the one PR; the phases are checkpoints, each
committed.

## Phase 9: Convergence

- [x] T047 Mark issue #179 Phase D shipped in `.docs/roadmap.md` (PR #213 merged; the gate checked after release, pending) per FR-030 (partial)
- [x] T048 Give a People entry's shape where DMC adds a canon line in play (`kit/dm-rules.md` line 58: a person's line starts with their bold name, then `(also: …)`), within FR-028's 400 bytes and no new line, per FR-019 (partial)
