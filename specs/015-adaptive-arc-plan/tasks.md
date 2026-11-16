# Tasks: An arc plan built on a story structure, that DMC holds to and revises

**Input**: Design documents from `specs/015-adaptive-arc-plan/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: toolkit tests for the new wording and the byte cap (contract
[dm-rules-narration.md](contracts/dm-rules-narration.md), plan § Testing); the shape check's tests and the golden
fixture, from the development guide's rule for a standard change ([migration-008.md](contracts/migration-008.md));
the headless play checks, from the spec's success criteria ([quickstart.md](quickstart.md) § 2 to § 5).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: the user story the task belongs to (US1 to US6)

## Path Conventions

Repository root: `kit/`, `src/Dmc.Cli/Campaign/`, `tests/Dmc.Tests/`, `.docs/`. The headless tests run in
`D:\Claude\test-campaign`; the scratchpad holds the backups. `dmc install`, `refresh` and `update` refuse inside Claude
Code (`src/Dmc.Cli/ClaudeSessionGuard.cs`), so changed kit files are copied into the test folder by hand, each backed
up first. The edits to `kit/skills/dmc-campaign-full-save/SKILL.md` (US2 to US5) run in order, not in parallel.

---

## Phase 1: Setup

- [ ] T001 Record the baseline on the branch: `kit/dm-rules.md` (expected 212 lines, 30,631 bytes),
  `kit/skills/dmc-new-campaign/SKILL.md` (105 lines, 12,920 bytes), `kit/skills/dmc-campaign-full-save/SKILL.md` (78
  lines, 11,438 bytes), `kit/templates/campaign/story/dm-notes.md` (3,697 bytes); run
  `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Toolkit"` and note any test that already fails locally on
  `main`
- [ ] T002 Back up `D:\Claude\test-campaign` to the scratchpad: its `CLAUDE.md`, `adventures/.active-campaign`,
  `.claude/skills/dmc-new-campaign/`, `.claude/skills/dmc-campaign-full-save/`, `templates/campaign/story/dm-notes.md`,
  `migrations/`, and `adventures/rorns-landing/` whole; record the paths in `specs/015-adaptive-arc-plan/quickstart.md`
  § Results

---

## Phase 2: Foundational (blocks every story)

**Purpose**: the template's new comments, which every story reads, and the migration that brings existing campaigns
to them (Development 2).

- [ ] T003 [P] Add toolkit tests in `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, each with an XML doc comment naming
  spec 015: the template's Arc plan comment names "Five Commandments" and "`dmc-new-campaign` step 4a" and asks for
  "about 4 sessions" and "or a victory of the same weight"; its Antagonists comment names "next move"; run them and
  see them fail
- [ ] T004 [P] Write the tests of the check `arc-plan-guidance-current` in
  `tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`, one per row of [contracts/migration-008.md](contracts/migration-008.md)
  § The check, in the style of the `dm-notes-have-arc-plan` tests, each with an XML doc comment: no
  `story/dm-notes.md` passes; no `## Arc plan` passes; an Arc plan with no comment naming "Five Commandments" fails
  with "story/dm-notes.md's Arc plan has an older template comment"; no `## Antagonists` fails with
  "story/dm-notes.md has no ## Antagonists section"; an Antagonists comment without "next move" fails with
  "story/dm-notes.md's Antagonists has an older template comment"; both current passes; run them and see them fail
- [ ] T005 In `kit/templates/campaign/story/dm-notes.md`, replace the Arc plan comment and the Antagonists comment
  exactly as [contracts/dm-notes-template.md](contracts/dm-notes-template.md) says; nothing else in the template
  changes; measure: the Arc plan heading and comment 1,911 bytes, the Antagonists comment 239; T003's tests pass
- [ ] T006 Add `ArcPlanGuidanceCurrent` to `src/Dmc.Cli/Campaign/MigrationChecks.cs`, registered as
  `["arc-plan-guidance-current"]` after `dm-notes-have-arc-plan`; it reads the `## Arc plan` and `## Antagonists`
  sections raw, comments included (a small helper beside `Section`, which strips them), heading compared as `Section`
  does; an XML doc comment naming migration 008 and spec 015 FR-011, and one on the helper; T004's tests pass
- [ ] T007 Write `kit/migrations/008-arc-plan-structure.md` as [contracts/migration-008.md](contracts/migration-008.md)
  says: front matter `standard: 8`, the title, `shipped: issues #217, #181` (set to the PR in T033),
  `touches: [story/dm-notes.md]`, `drops: []`, `checks: [arc-plan-guidance-current]`; the five headings in order, each
  step first checking whether it is needed, in the style of `kit/migrations/007-arc-plan-everywhere.md`
- [ ] T008 Build the golden fixture `tests/Dmc.Tests/Fixtures/migrations/008/` as the contract's § The fixture says:
  `before/` (standard 7 from the templates, `story/dm-notes.md` with PR #221's Arc plan comment, copied from
  `git show 9dbb9d6:kit/templates/campaign/story/dm-notes.md`, a filled stage line, Character arcs, a comment of the
  player's own in the Arc plan, the first kit's Antagonists comment from `git show d4523c6:kit/templates/campaign/story/dm-notes.md`
  with one antagonist); `after/` (both template comments replaced by T005's, the player's comment kept, every content
  line unchanged); `accounting.tsv` made as `.docs/development.md` § Changing a campaign-file standard step 4 says;
  run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Migration"` until `MigrationGoldenTests`,
  `MigrationFilesTests` and T004's tests pass
- [ ] T009 Publish (`dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`) and check the standard: a standard-7
  campaign is reported `behind: standard 7, kit 8: 008-arc-plan-structure.md` by `artifacts/bin/dmc campaign
  standard`; run the tests in `tests/Dmc.Tests/Campaign/` and fix any that assume the kit is at standard 7
- [ ] T010 Live run, [quickstart.md](quickstart.md) § 2: copy fixture 008's `before/` to
  `D:\Claude\test-campaign\adventures\arc-mig-test\` with its standard at 7, `kit/migrations/007-arc-plan-everywhere.md`
  and `008-arc-plan-structure.md` into the folder's `migrations/`, and T005's template into its
  `templates/campaign/story/`; make `arc-mig-test` active; with `artifacts\bin` first on `PATH`, ask DMC headless to
  bring the campaign's files up to date; check `dmc campaign verify arc-mig-test` passes and the result matches
  `after/`; fix the migration's prose for anything DMC got wrong and rerun; record the result in
  `specs/015-adaptive-arc-plan/quickstart.md` § Results; move `arc-mig-test` to the scratchpad

**Checkpoint**: the template holds the new comments; migration 008 verifies on its fixture and live.

---

## Phase 3: User Story 1 - A new campaign's arc is built on a story structure (Priority: P1) 🎯 MVP

**Goal**: `dmc-new-campaign` carries researched guidance on the Five Commandments and the Hero's Journey, and a new
homebrew campaign's Arc plan names its structure, with every beat planned.

**Independent Test**: [quickstart.md](quickstart.md) § 3 (SC-001).

- [ ] T011 [US1] Add a toolkit test in `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, with an XML doc comment naming spec
  015 FR-001: `kit/skills/dmc-new-campaign/SKILL.md` has a `## 4a. Designing the arc` heading naming "Five
  Commandments" and "Hero's Journey", and step 4 points at step 4a; run it and see it fail
- [ ] T012 [US1] In `kit/skills/dmc-new-campaign/SKILL.md`, replace step 4's Arc plan sentence (line 96) and insert
  step 4a after step 4 and before `## 5. Begin`, exactly as
  [contracts/new-campaign-arc-guidance.md](contracts/new-campaign-arc-guidance.md) says; measure (expected +21 lines,
  +3,134 bytes); T011 passes
- [ ] T013 [US1] Copy T005's template and T012's skill into `D:\Claude\test-campaign` (`templates/campaign/story/`,
  `.claude/skills/dmc-new-campaign/`); create a new homebrew campaign `arc-new-test` headless from a short brief (a
  premise, a PC, one companion, milestone leveling), quickstart § 3; check its `story/dm-notes.md` against SC-001 (a
  Structure line; 5 to 7 stages each naming its beat, what changes and "about N sessions"; "current since s1"; every
  milestone an achievement "or a victory of the same weight"; every antagonist a next move "by sN") and
  `dmc size arc-new-test --check` prints no Arc plan line; record in `specs/015-adaptive-arc-plan/quickstart.md`
  § Results; move `arc-new-test` to the scratchpad

**Checkpoint**: SC-001 met, or the plan is shown to the maintainer before going on.

---

## Phase 4: User Story 2 - The plan is revised when play overtakes it (Priority: P1)

**Goal**: the full save revises the stages still ahead when an outcome makes one untrue, by step 4a's rules, and
leaves a plan nothing contradicted unchanged.

**Independent Test**: [quickstart.md](quickstart.md) § 4 (SC-002).

- [ ] T014 [US2] In `kit/skills/dmc-campaign-full-save/SKILL.md` step 5, change the Arc plan bullet and add the Revise
  bullet after it, exactly as [contracts/full-save-arc-check.md](contracts/full-save-arc-check.md) says
- [ ] T015 [US2] Make the overtaken-plan copy: copy T002's backup of `rorns-landing` to
  `D:\Claude\test-campaign\adventures\arc-rorns\`; copy `kit/migrations/006-play-cards-and-names.md` (if the folder
  lacks it), `007-arc-plan-everywhere.md` and `008-arc-plan-structure.md` into the folder's `migrations/`, T014's full
  save into `.claude/skills/dmc-campaign-full-save/`, and `kit/dm-rules.md` over the folder's `CLAUDE.md`; make
  `arc-rorns` active; with `artifacts\bin` first on `PATH`, migrate it headless (006, 007, 008) and check
  `dmc campaign verify arc-rorns` passes; keep a copy of its migrated `story/dm-notes.md` in the scratchpad
- [ ] T016 [US2] Run one full save on `arc-rorns` headless; diff its Arc plan with T015's copy and check SC-002: no
  stage still ahead rests on Auntie Hush; every stage, thread and foreshadowing line play did not contradict unchanged
  word for word; every milestone not yet awarded anchored, each saying "or a victory of the same weight"; every stage still ahead
  with "about N sessions" and the current stage with "current since sN" (SC-006: the first full save after the
  migration fills them); a climax and a resolution still planned; `dmc size arc-rorns --check` prints no Arc plan
  line; record in `specs/015-adaptive-arc-plan/quickstart.md` § Results

**Checkpoint**: SC-002 met, or the revised plan is shown to the maintainer before going on.

---

## Phase 5: User Story 3 - Antagonists move on a clock of sessions played (Priority: P2)

**Goal**: every antagonist's next move names a session; when it comes, the move happens and the next session shows a
sign of it.

**Independent Test**: after a full save every antagonist has "by sN"; a due move gets its sign, shown the session
after (SC-003, checked in Phase 9).

- [ ] T017 [US3] In `kit/skills/dmc-campaign-full-save/SKILL.md` step 5, add the Clocks bullet after Revise (T022
  later puts Pace between them), exactly as [contracts/full-save-arc-check.md](contracts/full-save-arc-check.md)
  says
- [ ] T018 [US3] Copy T017's full save into the test folder; run a full save on `arc-rorns` and check every
  antagonist in its `story/dm-notes.md` has a next move "by sN" and none is timed by an in-world date alone; record in
  `specs/015-adaptive-arc-plan/quickstart.md` § Results

---

## Phase 6: User Story 4 - Milestones are achievements, not places (Priority: P2)

**Goal**: a milestone's achievement or a victory of the same weight earns it; a revision re-anchors milestones not
yet awarded.

**Independent Test**: a milestone reached by an equivalent is awarded at the next full save (SC-003, checked in
Phase 9); T016 already checks the re-anchoring (SC-002).

- [ ] T019 [US4] In `kit/skills/dmc-campaign-full-save/SKILL.md` step 5, change the Progression bullet exactly as
  [contracts/full-save-arc-check.md](contracts/full-save-arc-check.md) says

---

## Phase 7: User Story 6 - DMC offers to move on once a scene has delivered (Priority: P2)

**Goal**: at the end of a scene whose point has landed, a one-line offer to jump ahead, which the player may decline.

**Independent Test**: [quickstart.md](quickstart.md) § 5's jump columns (SC-004).

- [ ] T020 [US6] In `kit/dm-rules.md` § Narration (line 195), insert the sentence of
  [contracts/dm-rules-narration.md](contracts/dm-rules-narration.md) after "(a running clock or threat still acts)."
  and before "A beat with no decision in it"; no other line changes; measure: 212 lines, 30,898 bytes (+267)
- [ ] T021 [US6] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, set `DmRulesBytesAllowed` to the measured size from
  T020 and say in its XML doc comment that spec 015 FR-008 (at most 300) raised it by 267 for the jump offer (about 67
  more tokens per turn); keep the assertion messages naming the specs; add a test, with an XML doc comment naming spec
  015 FR-007, that § Narration contains "Skip to the morning?" and "the break test"; run the toolkit tests

---

## Phase 8: User Story 5 - Each stage has a rough session budget (Priority: P3)

**Goal**: the full save compares the current stage's sessions with its budget and tightens or revises, never rushing
the player.

**Independent Test**: [quickstart.md](quickstart.md) § 5's "Hurried for budget" column (SC-004), and a pace note
written when a stage runs over.

- [ ] T022 [US5] In `kit/skills/dmc-campaign-full-save/SKILL.md` step 5, add the Pace bullet between Revise and
  Clocks, exactly as [contracts/full-save-arc-check.md](contracts/full-save-arc-check.md) says; check the list now
  reads Arc plan, Revise, Pace, Clocks, Mix, Progression, Promises; measure (expected +3 lines, +1,082 bytes on
  T001's baseline)

---

## Phase 9: Five sessions in play (US2 to US6 together)

**Purpose**: SC-003 and SC-004 over at least 5 consecutive sessions, as the maintainer asked ("test in play over
several sessions").

- [ ] T023 Copy the finished `kit/dm-rules.md` (over `CLAUDE.md`), full save, `dmc-new-campaign` and template into
  the test folder; on `arc-rorns`, play at least 5 sessions headless, each a scripted player brief of several moves
  with at least one scene played to its end with nothing open and one open exchange left hanging, and in one session
  a declined jump offer; run a full save after each; fill in [quickstart.md](quickstart.md) § 5's table per session in
  `specs/015-adaptive-arc-plan/quickstart.md` § Results
- [ ] T024 Check the run against SC-003 and SC-004: every scene that ended with nothing open got an offer (at least 3);
  0 offers in open exchanges, mid-scene or fights; 0 repeats after a decline; a move that came due had its "Show next session:" sign shown in
  the session after (at least 1; if not, report it on issue #181 as evidence for cause 1, research R5); a milestone reached by an equivalent awarded at the next full save; no plan churned and
  no reply hurried for a budget; `dmc size arc-rorns --check` prints no Arc plan line after the last save; if a
  criterion fails, change the wording in its contract and the kit file, re-measure, and rerun the sessions it
  concerns; record the outcome in `specs/015-adaptive-arc-plan/quickstart.md` § Results
- [ ] T025 Restore `D:\Claude\test-campaign` from T002's backups: move `arc-rorns` to the scratchpad, put back
  `CLAUDE.md`, `adventures/.active-campaign`, the two skills, the template and `migrations/`, and check
  `adventures/rorns-landing/` matches its backup file for file

**Checkpoint**: SC-003 and SC-004 met, or the results are shown to the maintainer before the documents.

---

## Phase 10: Polish & Cross-Cutting Concerns

- [ ] T026 [P] In `.docs/user-guide.md` § Talking to DMC (line 115), add in plain words that when a scene is done DMC
  may offer to skip ahead, and you can say no; read § Bringing a campaign up to date and add a line only if it lists
  what migrations do
- [ ] T027 [P] In `.docs/campaign-files.md`, the `story/dm-notes.md` row (line 73): the Arc plan names its structure
  and its stages' beats and rough session budgets, milestones are achievements, antagonists carry a next move with its
  session; migration 008 brings older notes' guidance up to date
- [ ] T028 [P] In `.docs/dmc-reference.md` § Migrations, the list of checks: add `arc-plan-guidance-current` as
  `src/Dmc.Cli/Campaign/MigrationChecks.cs` describes it; search `.docs/` for `007-arc-plan-everywhere` and
  `dm-notes-have-arc-plan` and add migration 008 wherever the migrations are listed
- [ ] T029 [P] In `.docs/decisions.md`, add the entry "Arcs are built on a story structure and revised when play
  overtakes them (issues #217, #181)": the decision (step 4a, the template, the full save's checks, the jump offer,
  migration 008), the reason (Dungeon Master 1; #181's evidence and principle), the clarifications, the measured cost
  (plan § Performance Goals, updated to the measured figures), the play test's results, Limits (causes 1 and 7 wait;
  the unverified items in research R1, R2, R6; fixture 007's `after/` keeps PR #221's comment, which a run of 007 now
  writes as the new one, since a fixture records its own standard), and the sources (research R1 to R14); and in the entry "The
  constitution states principles of its own (version 3.0.0)", remove the Dungeon Master 1 item from Limits (line 731)
- [ ] T030 [P] In `.docs/roadmap.md`, add a row for issue #217 and issue #181's causes 2 to 6: changes the binary (the
  check) and the kit; migration 008, standard 8; the play tests; causes 1 and 7 and the review after release (SC-007)
  pending
- [ ] T031 Update `specs/015-adaptive-arc-plan/plan.md` § Performance Goals and `research.md` R8 to R11 to the
  measured figures if any differ from the plan's
- [ ] T032 Run the whole suite, `dotnet test tests/Dmc.Tests`, after a fresh publish: all pass but the known skips
  from T001; then check each rule has one home (FR-010): search `kit/skills/dmc-campaign-full-save/SKILL.md`,
  `kit/templates/campaign/story/dm-notes.md` and `kit/dm-rules.md` for any copy of step 4a's revision rules ("word for
  word", "never restart") or of § Compaction's break test, and replace a copy with a pointer
- [ ] T033 Push the branch; open the PR (the user runs `/speckit-converge` first) with `Closes #217` and `Part of
  #181` on their own lines; what changed and why, the sources, the measured cost, the migration live run and the play
  tests, the documents changed, and that it adds migration 008; set `shipped:` in
  `kit/migrations/008-arc-plan-structure.md` to the PR's number and push; check the description with
  `gh pr view <n> --json body`

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (T001, T002) → Foundational (T003 to T010) → the story phases → Phase 9 (T023 to T025) → Polish (T026 to
  T033).
- T002's backups come before any copy into the test folder (T010, T013, T015, T018, T023).

### User Story Dependencies

- **US1** (T011 to T013): after Foundational; independent of the full save stories.
- **US2** (T014 to T016): after Foundational (T015 runs migration 008). T016 needs T014.
- **US3** (T017, T018): after T014 (same file, same list); T018 needs T015's `arc-rorns`.
- **US4** (T019): after T017 (same file).
- **US6** (T020, T021): independent of the others; T021 after T020.
- **US5** (T022): after T019 (same file).
- **Phase 9** needs every story's kit change (T012, T014, T017, T019, T020, T022).

### Within Each User Story

- Tests before the change they test (T003, T004 before T005, T006; T011 before T012; T020 before T021's cap).
- The edit, then the copy into the test folder, then the headless check.

### Parallel Opportunities

- T003 and T004 (different test files), then T005 and T006 (template and check), can each run together.
- US1 (`dmc-new-campaign`) and US6 (`kit/dm-rules.md`) touch different files from the full save stories and can run
  alongside US2 to US5.
- T026 to T030 edit five different documents and can run together.

## Parallel Example: Foundational

```text
T003 tests/Dmc.Tests/Toolkit/ToolkitTests.cs
T004 tests/Dmc.Tests/Campaign/MigrationChecksTests.cs
```

## Parallel Example: stories on different files

```text
T012 kit/skills/dmc-new-campaign/SKILL.md            (US1)
T014 kit/skills/dmc-campaign-full-save/SKILL.md      (US2, then T017, T019, T022 in order)
T020 kit/dm-rules.md                                 (US6)
```

## Parallel Example: Polish

```text
T026 .docs/user-guide.md
T027 .docs/campaign-files.md
T028 .docs/dmc-reference.md
T029 .docs/decisions.md
T030 .docs/roadmap.md
```

## Implementation Strategy

### MVP First

Foundational, then US1 (T011 to T013): the constitution's rule (Dungeon Master 1) and issue #217 on their own. Stop
and look at the new campaign's Arc plan before the full save stories.

### Incremental Delivery

US1, then US2 (the revision, checked on the overtaken plan), then US3, US4 and US5 in the full save, US6 in the play
rules, then the five sessions together, then the documents. One PR at the end.

## Notes

- Change no line of `kit/dm-rules.md` other than 195.
- Every type and member added or changed in code gets an XML doc comment, private ones included (`CLAUDE.md`
  § Documentation).
- Commit after each phase.
