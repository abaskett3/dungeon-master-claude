# Tasks: "Yes, and…" and "No, but…"

**Input**: Design documents from `specs/014-yes-and-no-but/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/dm-rules-fun-first.md](contracts/dm-rules-fun-first.md), [quickstart.md](quickstart.md)

**Tests**: the toolkit test change and one new toolkit test come from the plan (research R8); the play checks come
from the spec's success criteria (quickstart § 3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: the user story the task belongs to (US1, US2, US3)

## Path Conventions

Repository root: `kit/dm-rules.md`, `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, `.docs/`. The headless play test runs
in `D:\Claude\test-campaign`. The three user stories all edit § Fun first of `kit/dm-rules.md`, so their edits run in
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

**Checkpoint**: the new test fails until User Stories 1 and 2 land.

---

## Phase 3: User Story 1 - A bold choice that fits the fiction gets "Yes, and…" (Priority: P1) 🎯 MVP

**Goal**: a bold or creative choice is resolved and gets one detail or hook; a routine action gets nothing added.

**Independent Test**: quickstart § 3, messages 1 to 8 (SC-001).

- [X] T003 [US1] In `kit/dm-rules.md` line 166, replace "Say yes to creative ideas when you can. … rule in favor of
  the cool idea and keep moving." with the first three sentences of the contract's line 166: "Meet a bold idea with
  "yes, and" (a technique, never said): it happens, plus a detail or hook (the chandelier holds, and a key ring
  hangs by the ledge). Routine acts get nothing added. When a strict reading would kill the momentum over something
  minor, rule for the cool idea." Keep it one line
- [X] T004 [US1] Build `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`, have the maintainer refresh
  `D:\Claude\test-campaign` from it, and run quickstart § 3 messages 1 to 8; record each reply's pass or fail in
  `specs/014-yes-and-no-but/quickstart.md` § Results (first record `adventures/.active-campaign` and copy the
  campaign under test to the scratchpad)

**Checkpoint**: "yes, and" works; the T002 test still fails on "no, but".

---

## Phase 4: User Story 2 - A choice that would break the story gets "No, but…" in the fiction (Priority: P1)

**Goal**: what the world rules out is refused in the fiction with a way forward; the first attack on a major NPC
leaves everyone alive, once, at any lethality, and is recorded in canon; after that the dice fall where they fall;
no out-of-character "the story does not allow it".

**Independent Test**: quickstart § 3, messages 9 to 14 and the out-of-character fireball question (SC-002, SC-003).

- [X] T005 [US2] In `kit/dm-rules.md` line 166, append the contract's last two sentences: "When the world rules an
  idea out, use "no, but" in the fiction: turn it aside, with another way forward (no door in that wall, but a
  draughty crawlspace). Never say out of character that the story does not allow it."
- [X] T006 [US2] In `kit/dm-rules.md` line 169, append the contract's exception: "Except the first attack on an NPC
  the main plot needs (central in the module, or in the Arc plan or Opposition table), at any lethality: a "no,
  but", where they stop it, no one dies, and a canon line records it. After that, the dice fall where they fall."
- [X] T007 [US2] Rebuild and have the maintainer refresh `D:\Claude\test-campaign`; choose a major NPC from that
  campaign's Arc plan or Opposition table (research R9); run quickstart § 3 messages 9 to 12 and the
  out-of-character fireball question; then set **Lethality** to `gritty` in that campaign's `player-options.md` (the
  hardest case for FR-004) and run messages 13 and 14; check that message 13 wrote one canon line with the NPC's
  bold name and `sN.M` in `story/canon.md` (data-model.md, "The 'no, but' record"); record the results in
  `specs/014-yes-and-no-but/quickstart.md` § Results

**Checkpoint**: the T002 test passes; "no, but" works.

---

## Phase 5: User Story 3 - What the character wants becomes part of the main plot (Priority: P2)

**Goal**: a goal the character states is noted in the Arc plan's Character arcs and tied into the main plot.

**Independent Test**: state a goal in play, then save; the Arc plan's Character arcs holds it with how it ties in.

- [X] T008 [US3] In `kit/dm-rules.md` line 165, replace "Note what you learn in `dm-notes.md`." with "Note what you
  learn in `dm-notes.md`, and weave what the character wants into the main plot through the Arc plan's Character
  arcs." (the contract's line 165)
- [X] T009 [US3] Rebuild and refresh; in the headless session, have the character state a goal, then run
  `/dmc-campaign-full-save`; check that the Arc plan's Character arcs in `story/dm-notes.md` holds the goal and how it
  ties into the main plot (data-model.md, "A goal the character wants"); record the result in
  `specs/014-yes-and-no-but/quickstart.md` § Results

**Checkpoint**: all three stories work.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T010 Measure `kit/dm-rules.md` and confirm it is 212 lines and at most 30,603 bytes (SC-004; 30,602 if the
  wording matches the contract)
- [X] T011 In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, set `DmRulesBytesAllowed` to the size measured in T010;
  rename `TheAlwaysLoadedRulesStayAtTheirLineCountAndGrowByAtMostFourHundredBytes` to
  `TheAlwaysLoadedRulesStayAtTheirLineAndByteCaps`; and say in the XML doc comments of the constants and the test,
  and in the assertion message, which specs set each cap (spec 013 FR-028 the lines, spec 014 FR-008 the bytes)
- [X] T012 Run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Toolkit"`: the cap test and the T002 test
  pass, and only the three failures noted in T001 remain
- [X] T013 [P] In `.docs/user-guide.md` § Talking to DMC, add a short paragraph in plain words: DMC builds on a bold
  idea, turns down what the world rules out inside the story with another way forward, and never tells the player
  the story does not allow it (Documentation 2)
- [X] T014 [P] In `.docs/product-overview.md` § What it deliberately does not do, add a bullet: it does not refuse a
  choice out of character; a story-breaking choice gets a "no, but" in the fiction
- [X] T015 [P] In `.docs/decisions.md`, add an entry "A bold idea gets "yes, and", a ruled-out one "no, but" in the
  fiction (issue #216)": the decision, the reason (constitution Dungeon Master 4), the cost (0 lines, 685 bytes,
  about 171 tokens per turn), the clarifications, the rejected alternatives (research R1, R2, R6) and the limits, and
  that it meets the Dungeon Master 4 item of the constitution 3.0.0 entry's Limits
- [X] T016 [P] In `.docs/roadmap.md`, add a row for issue #216 with its status and a pointer to the decisions entry
- [ ] T017 Restore `D:\Claude\test-campaign`: put back `adventures/.active-campaign` and the campaign copied in T004,
  which also undoes T007's lethality change
- [ ] T018 Run `/speckit-converge`, then open the PR with `Closes #216`, the measured cost, the play test results,
  the documents changed, and a line saying no migration is needed (no template or campaign file changes shape);
  check its description with `gh pr view <n> --json body`

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (T001) → Foundational (T002) → US1 → US2 → US3 → Polish.
- The user stories run in order because all three edit `kit/dm-rules.md` § Fun first.

### User Story Dependencies

- **US1**: none beyond T002.
- **US2**: after US1, because T005 appends to the line T003 rewrites.
- **US3**: after US2 only for file order; it does not depend on their behaviour.

### Within Each User Story

- The edit, then a rebuild and refresh, then the play check.

### Parallel Opportunities

- T013, T014, T015 and T016 edit four different documents and can run together once T012 passes.

## Parallel Example: Polish

```text
T013 .docs/user-guide.md
T014 .docs/product-overview.md
T015 .docs/decisions.md
T016 .docs/roadmap.md
```

## Implementation Strategy

### MVP First (User Story 1 Only)

T001 to T004: "yes, and" alone, checked in play. Stop and look at the replies before adding "no, but".

### Incremental Delivery

US1, then US2 (the larger behaviour change), then US3, each checked in play before the next. One PR at the end.

## Notes

- The maintainer runs `dmc refresh` on the test campaign in their own terminal (quickstart § 3).
- Do not change any line of `kit/dm-rules.md` outside 165, 166 and 169.
- Commit after each phase.
