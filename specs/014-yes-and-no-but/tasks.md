# Tasks: "Yes, and…" and "No, but…"

**Input**: Design documents from `specs/014-yes-and-no-but/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: the toolkit test change and one new toolkit test come from the plan (research R8); the shape check's tests
and the golden fixture come from the development guide's rule for a standard change (research R16, R17); the play
checks come from the spec's success criteria (quickstart § 3, § 3a, § 3b).

**Two rounds.** T001 to T017 built the first wording and ran the first play test (done, on the branch). The play test
found three gaps; the spec and plan were revised (research R12 to R19), and T018 onward build the revision.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: the user story the task belongs to (US1, US2, US3)

## Path Conventions

Repository root: `kit/`, `src/Dmc.Cli/Campaign/`, `tests/Dmc.Tests/`, `.docs/`. The headless tests run in
`D:\Claude\test-campaign`; the scratchpad holds the backups. The user stories' edits to `kit/dm-rules.md` run in
order, not in parallel.

---

## Phase 1: Setup

- [X] T001 Record the baseline of `kit/dm-rules.md` (expected 212 lines, 29,917 bytes) and run
  `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Toolkit"`, noting the three toolkit tests that fail
  locally on `main` (quickstart § 1)

---

## Phase 2: Foundational

- [X] T002 Add a toolkit test in `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` that § Fun first of `kit/dm-rules.md`
  contains `"yes, and"` and `"no, but"`, with an XML doc comment naming spec 014 (research R8); run it and see it fail

---

## Phase 3: User Story 1 - A bold choice that fits the fiction gets "Yes, and…" (Priority: P1) 🎯 MVP

**Goal**: a bold or creative choice is resolved and gets one detail or hook; a routine action gets nothing added.

**Independent Test**: quickstart § 3, messages 1 to 8 (SC-001). Passed in the first round; the "yes, and" sentences
do not change in the second.

- [X] T003 [US1] In `kit/dm-rules.md` line 166, replace "Say yes to creative ideas when you can. … rule in favor of
  the cool idea and keep moving." with the first three sentences of the contract's line 166
- [X] T004 [US1] Build, put the rules in `D:\Claude\test-campaign`, and run quickstart § 3 messages 1 to 8; record
  the results in `specs/014-yes-and-no-but/quickstart.md` § Results

---

## Phase 4: User Story 2 - A choice that would break the story gets "No, but…" in the fiction (Priority: P1)

**Goal**: what the world rules out is refused in the fiction with, in the same reply, a way toward what the player
wanted; the first attack on a major NPC leaves everyone alive, once, at any lethality, and is recorded in canon;
after that the dice fall where they fall; a wrong premise gets line 153's correction; no out-of-character "the story
does not allow it".

**Independent Test**: quickstart § 3a, messages 9′ to 12′, with the first round's message 13 (SC-002, SC-003).

- [X] T005 [US2] In `kit/dm-rules.md` line 166, append the first round's "no, but" sentences
- [X] T006 [US2] In `kit/dm-rules.md` line 169, append the contract's first-attack exception
- [X] T007 [US2] Run quickstart § 3 messages 9 to 14 and the out-of-character fireball question at `gritty` for 13
  and 14; record the results in `specs/014-yes-and-no-but/quickstart.md` § Results
- [X] T018 [US2] In `kit/dm-rules.md` line 166, replace "turn it aside, with another way forward (no door" with
  "turn it aside and offer, in the same reply, a way toward what they wanted (no door"
  ([contracts/dm-rules-fun-first.md](contracts/dm-rules-fun-first.md), research R12); measure the file: 212 lines
  and 30,636 bytes; no other line of the file changes
- [X] T019 [US2] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, set `DmRulesBytesAllowed` to `30_636` and say in its
  XML doc comment that spec 014 FR-008 raised the cap by 593, the allowance after the play test (the way-forward
  wording added 34); keep the assertion message naming spec 014 FR-008
- [X] T020 [US2] Publish (`dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`); back up
  `D:\Claude\test-campaign\CLAUDE.md`, `adventures/.active-campaign` and the play-test campaign to the scratchpad;
  copy `kit/dm-rules.md` over the folder's `CLAUDE.md`; in a new pinned headless session (quickstart § 3's method,
  with `Edit(adventures/**)`), resume and send quickstart § 3a's four ruled-out ideas, none resting on a wrong
  premise (a premise reply is recorded and replaced by another idea); record each reply's pass or fail in
  `specs/014-yes-and-no-but/quickstart.md` § Results under "Second round", and the SC-002 count with the first
  round's message 13

**Checkpoint**: SC-002 met (5 of 5 with a way toward what the player wanted), or the replies are shown to the
maintainer before going on.

---

## Phase 5: User Story 3 - What the character wants becomes part of the main plot (Priority: P2)

**Goal**: a goal the character states is noted in the Arc plan's Character arcs, with how it ties into the main
plot, in every campaign; migration 007 gives every campaign the section.

**Independent Test**: quickstart § 3b: migration 007's live run on fixture 007, then a goal stated in the migrated
campaign lands in Character arcs (US3 scenarios 1 and 3, SC-006).

- [X] T008 [US3] In `kit/dm-rules.md` line 165, replace "Note what you learn in `dm-notes.md`." with the contract's
  line 165
- [X] T009 [US3] State a goal in play, run the full save, and record where it landed in
  `specs/014-yes-and-no-but/quickstart.md` § Results
- [X] T021 [P] [US3] Write the tests of the check `dm-notes-have-arc-plan` in
  `tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`, in the style of the existing ones, each with an XML doc
  comment: no `story/dm-notes.md` passes; a `dm-notes.md` with no `## Arc plan` fails with "story/dm-notes.md has no
  ## Arc plan section"; a section whose only Character arcs line is inside its HTML comment fails with
  "story/dm-notes.md's Arc plan has no Character arcs line"; a section with `- Character arcs: …` (and one with
  `- **Character arcs**: …`) passes; a `### ` subsection inside the Arc plan does not end it
  ([contracts/migration-007.md](contracts/migration-007.md), § The check); run them and see them fail
- [X] T022 [US3] Add `DmNotesHaveArcPlan` to `src/Dmc.Cli/Campaign/MigrationChecks.cs`, registered as
  `["dm-notes-have-arc-plan"]` after `people-entries-named`, using the file's `Section` and `Lines` helpers (comments
  stripped, the heading compared ignoring case) and matching a line that starts with `Character arcs` after list
  marks and bold; an XML doc comment naming migration 007 and spec 014 FR-005b; T021's tests pass
- [X] T023 [P] [US3] In `kit/templates/campaign/story/dm-notes.md`, change the Arc plan comment's first line and its
  Character arcs line exactly as [contracts/dm-notes-arc-plan.md](contracts/dm-notes-arc-plan.md) says (+82 bytes);
  nothing else in the template changes
- [X] T024 [US3] Write `kit/migrations/007-arc-plan-everywhere.md` as [contracts/migration-007.md](contracts/migration-007.md)
  and research R15 say: front matter `standard: 7`, title, `shipped: issue #216` (set to the PR in T035),
  `touches: [story/dm-notes.md]`, `drops: []`, `checks: [dm-notes-have-arc-plan]`; the five headings in order (What
  changes, Steps, Judgments, Declared drops, Done when), written for a campaign its author has never seen, each step
  first checking whether it is needed, in the style of `kit/migrations/006-play-cards-and-names.md`
- [X] T025 [US3] Build the golden fixture `tests/Dmc.Tests/Fixtures/migrations/007/` as the contract's § The fixture
  says: `before/` (standard 6, made from the templates: `game-state.md` with a PC and one companion under Party
  status, their sheets with a goal each, `story/dm-notes.md` from the template before T023 without `## Arc plan`, its
  arc under `## The three acts` with `### ` acts); `after/` (the same with `## Arc plan`, the new comment and a
  Character arcs line before `### The three acts`, its acts at `#### `); `accounting.tsv` with its comment lines
  only; run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Migration"` until `MigrationGoldenTests`,
  `MigrationFilesTests` and T021's tests pass
- [X] T026 [US3] Check the standard: a standard-6 campaign is reported `behind: standard 6, kit 7: 007-arc-plan-everywhere.md`
  by `artifacts/bin/dmc campaign standard`; run the tests in `tests/Dmc.Tests/Campaign/` and fix any that assume the
  kit is at standard 6 (none found by search, research R17)
- [X] T027 [US3] Live run, quickstart § 3b steps 1 to 3: with T020's backups in place, copy fixture 007's `before/` to
  `D:\Claude\test-campaign\adventures\arc-test\` with `.standard` `6`, `kit/migrations/007-arc-plan-everywhere.md`
  into the folder's `migrations/`, and the changed template into its `templates/campaign/story/` (back up the
  folder's copy first); make `arc-test` active; with `artifacts\bin` first on `PATH`, ask DMC headless to bring the
  campaign's files up to date; check `dmc campaign verify arc-test` passes and the result matches `after/`; fix the
  migration's prose for anything DMC got wrong and rerun; record the result in
  `specs/014-yes-and-no-but/quickstart.md` § Results
- [X] T028 [US3] Goal check, quickstart § 3b step 4: resume the migrated `arc-test` headless, have the character state
  a goal, run the full save, and check the Arc plan's Character arcs in `story/dm-notes.md` holds it with how it ties
  into the main plot; if `arc-test` is too thin to resume, migrate a copy of the play-test campaign instead (006
  then 007); record the result in `specs/014-yes-and-no-but/quickstart.md` § Results
- [X] T029 [US3] Restore `D:\Claude\test-campaign` (quickstart § 3b step 5): remove `arc-test` (move it to the
  scratchpad), the copied migration and template (put the folder's own copies back), and restore `CLAUDE.md`,
  `adventures/.active-campaign` and the play-test campaign from T020's backups; check the restored campaign matches
  its backup file for file

**Checkpoint**: all three stories work; the migration verifies on its fixture and live.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T010 Measure `kit/dm-rules.md` (first round: 212 lines, 30,602 bytes)
- [X] T011 Rename the cap test `TheAlwaysLoadedRulesStayAtTheirLineAndByteCaps` and name the specs that set each cap
- [X] T012 Run the toolkit tests
- [X] T013 [P] `.docs/user-guide.md` § Talking to DMC: the first round's paragraph
- [X] T014 [P] `.docs/product-overview.md` § What it deliberately does not do: the bullet
- [X] T015 [P] `.docs/decisions.md`: the entry "A bold idea gets "yes, and", a ruled-out one "no, but" in the
  fiction (issue #216)"
- [X] T016 [P] `.docs/roadmap.md`: the row for issue #216
- [X] T017 Restore `D:\Claude\test-campaign` after the first play test
- [X] T030 [P] In `.docs/user-guide.md` § Talking to DMC, change "gives you another way forward" to "offers a way
  toward what you wanted"; read § Bringing a campaign up to date and add a line only if it lists what migrations do
- [X] T031 [P] In `.docs/campaign-files.md`, the `story/dm-notes.md` row (line 73): the Arc plan's character arcs are
  kept in every campaign, a module campaign included, and the rest of the plan for a homebrew campaign or after the
  module
- [X] T032 [P] In `.docs/decisions.md`, revise the entry "A bold idea gets "yes, and", a ruled-out one "no, but" in
  the fiction (issue #216)": line 166's way-forward wording and its reason; the cost (0 lines, 719 bytes, about 180
  tokens per turn; the cap rise of 593; the template comment's 21 tokens per resume); the clarifications after the
  play test; the template change and migration 007 (standard 7, its check, what it moves and why it demotes headings
  instead of renaming, research R15); the wrong-premise rule kept (R13); and Limits rewritten from both play tests
  and the live run
- [X] T033 [P] In `.docs/roadmap.md`, the row for issue #216: changes the binary (the check) and the kit; migration
  007, standard 7; both play tests and the live run; the review after release (SC-005) pending
- [X] T034 Run the whole suite, `dotnet test tests/Dmc.Tests`, after a fresh publish: all pass but the known skips
- [ ] T035 Push the branch; open the PR (the user runs `/speckit-converge` first) with `Closes #216`, what changed
  and why, the sources, the measured cost, both play tests and the live run, the documents changed, and that it adds
  migration 007; set `shipped:` in `kit/migrations/007-arc-plan-everywhere.md` to the PR's number and push; check the
  description with `gh pr view <n> --json body`

---

## Dependencies & Execution Order

### Phase Dependencies

- First round: T001 → T002 → US1 → US2 → US3 → T010 to T017 (done).
- Second round: T018 → T019 → T020 (US2); T021 to T029 (US3); then T030 to T035.

### User Story Dependencies

- **US2 second round (T018 to T020)**: none on US3's second round; T020 backs up the test folder that T027 and T029
  also use, so T020 runs first.
- **US3 second round**: T021 → T022 (tests before the check); T023 before T025 (the fixture's `after/` holds the new
  comment) and before T027 (the live run copies the template); T024 before T025; T025 and T026 before T027; T027 →
  T028 → T029.

### Within Each User Story

- Tests before the code they test (T021 before T022); the edit, then the build, then the play or live check.

### Parallel Opportunities

- T021 and T023 edit different files and can run together, alongside T018 and T019.
- T030, T031, T032 and T033 edit four different documents and can run together once T029 is done.

## Parallel Example: User Story 3

```text
T021 tests/Dmc.Tests/Campaign/MigrationChecksTests.cs
T023 kit/templates/campaign/story/dm-notes.md
```

## Parallel Example: Polish

```text
T030 .docs/user-guide.md
T031 .docs/campaign-files.md
T032 .docs/decisions.md
T033 .docs/roadmap.md
```

## Implementation Strategy

### MVP First

US2's second round (T018 to T020) is the smallest fix for the gap that failed a success criterion (SC-002): stop and
look at its replies before building the migration.

### Incremental Delivery

US2's wording, then US3's check, template, migration and fixture (CI-testable), then the live run and the goal
check, then the documents. One PR at the end.

## Notes

- Do not change any line of `kit/dm-rules.md` other than 166 in the second round.
- `dmc install`, `refresh` and `update` refuse inside Claude Code (`src/Dmc.Cli/ClaudeSessionGuard.cs`): copy changed
  kit files into the test folder by hand, backing up each first.
- Commit after each phase.

---

## Phase 7: Convergence

- [X] T036 In `.docs/dmc-reference.md` § Migrations, the list of checks (`MigrationChecks`, line 453): add `dm-notes-have-arc-plan` (`story/dm-notes.md` has a `## Arc plan` section with a line outside comments starting Character arcs), and the three it already lacks, `continuity-section`, `companions-have-play-card` and `people-entries-named`, each as `src/Dmc.Cli/Campaign/MigrationChecks.cs` describes it, per FR-009 (partial)
- [X] T037 In `specs/014-yes-and-no-but/spec.md` § Clarifications, "after the play test", record that the maintainer accepted SC-002 at 4 of 5 after the second wording (2026-11-15), leaving the way forward in real play to SC-005, per SC-002 (partial)
- [X] T038 In `specs/014-yes-and-no-but/research.md` R19, correct the claim that `.docs/dmc-reference.md` § Migrations names no checks: it lists them at line 453, and T036 updates it, per plan: research R19 (partial)
