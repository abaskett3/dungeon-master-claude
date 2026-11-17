---

description: "Tasks for the feedback round of #181"
---

# Tasks: The feedback round of #181

**Input**: `specs/016-feedback-round-181/` (plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md)

**Tests**: yes. The contracts name their tests, and SC-007 needs `dotnet test` to pass. Write each code test first
and check that it fails.

**Organization**: one PR on this branch, `Closes #181` (research R11), with the stories in priority order. Line numbers
are `origin/main` at `0cfdfa1`. Every `kit/dm-rules.md` change is a clause on an existing line, never a new line
(FR-027). Draft wording starts from #181's draft plan, and the maintainer approves each clause's exact wording before
it is committed (#181 § Process).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 to US7 in spec.md

---

## Phase 1: Setup

- [X] T001 Run `git fetch` and confirm branch `016-feedback-round-181` is at `origin/main` (`0cfdfa1` or later), with only `specs/016-feedback-round-181/` untracked
- [X] T002 Build and run `dotnet test` from the repo root. Record the baseline pass count in specs/016-feedback-round-181/quickstart.md § Results
- [X] T003 [P] Save #181's body (`gh issue view 181 --json body`) to the scratchpad as the source of the draft clauses: A1 to A7, B1 to B3, C1 to C3, D1, E1, F1 to F4, G and H

---

## Phase 2: Foundational

No blocking prerequisite. Each story's homes are separate (plan.md § Where each change goes). The one shared file,
`kit/dm-rules.md`, makes its edits run in order: no two tasks on it are [P].

---

## Phase 3: User Story 1 - DMC says what the files say about the people in the scene (P1) 🎯 MVP

**Goal**: the facts about people in the scene are in front of DMC, and their sheets stay true (FR-001 to FR-008,
FR-032).

**Independent Test**: quickstart.md § 2, the three replayed slips. SC-001 passes when 3 of 3 replies agree with the
files.

### Tests for User Story 1

- [X] T004 [P] [US1] Write the sheet-line contract tests in tests/Dmc.Tests/ContextCheck/SheetReminderContract.cs, using the fixture style of CanonReminderContract.cs. Cover each case in contracts/context-check-sheets.md § Tests:
  - a Present line holding a play-card companion, an older-shape NPC, a crowd and a name with no sheet
  - the field order, taking the first line found of each pair
  - the header `Sheets of A; B (what their sheets say now; read the sheet for more):`
  - the 10,000-byte cap, header included, with 8 people; a person whose lines don't all fit is left out of the header
  - the 100,000-token skip, both after a hook print and after a Read of the sheet; a compaction resets it
  - a sheet with none of the lines falls back to the canon lines
  - a `/` command prints nothing; any failure prints nothing and exits 0

### Implementation for User Story 1

- [X] T005 [US1] In src/Dmc.Cli/Size/ReadSet.cs:277, make `PresentText` internal so the hook matches people as the resume does (Development 6). Update its XML doc comment to name both callers. No behaviour change.
- [X] T006 [US1] In src/Dmc.Cli/Retrieval/SheetReminder.cs (`Seen`, beside the reminder as `CanonReminder.Reminded` is), add a reader that returns, after the latest compact_boundary, the context size at which:
  - a `Sheets of …` header last named a person
  - a Read tool call last opened a given `character-sheets/` path

  Reuse how `CanonReminder.Reminded` (src/Dmc.Cli/Retrieval/CanonReminder.cs:141) reads hook headers, and how BreakReminder gets the context size. Add XML doc comments.
- [X] T007 [US1] Create src/Dmc.Cli/Retrieval/SheetReminder.cs to the contract:
  - **Who:** `character-sheets/*.md` only: not `minor-npcs.md`, not `*-history.md`, not a PC's sheet. Matched by `ReadSet.PresentText` and `ReadSet.Mentions` against the Present line and the Combat tracker.
  - **Fields:** the field table, from either sheet shape.
  - **Skip:** anyone shown or read within 100,000 tokens (T006).
  - **Cap:** 10,000 bytes, in the Present line's order.

  XML doc comments on every member, private ones included.
- [X] T008 [US1] Print the sheet part after the canon reminder in src/Dmc.Cli/Commands/ContextCheckCommand.cs. For a sheet with none of the fields, fall back to its canon lines. Make T004 pass.
- [X] T009 [US1] FR-001 (A1): add the clause to kit/dm-rules.md:60. When play settles what someone can do, wears, owns or where they live, DMC rewrites the standing line and the stat block, not only the History.
- [X] T010 [US1] FR-004 (A4): add the clause to kit/dm-rules.md:74. A species or class is never the source of an established character's abilities, and DMC never invents a number about an established character.
- [X] T011 [US1] FR-002 (A2): add the clause to kit/dm-rules.md:78. Read the sheet down to its History, in this session part, before voicing anyone added to the Present line.
- [X] T012 [US1] FR-003 (A3): add a pointer to the scene's `- Present:` line to the "who could know" check in kit/dm-rules.md:81.
- [X] T013 [P] [US1] Make two changes in kit/templates/session-log.md:
  - add a `- Present:` line to the scene block, lines 13-22 (FR-003, research R7)
  - add to line 18 that a reply that matters to a relationship is quoted, never summarised (FR-007, A7)
- [X] T014 [P] [US1] Make two changes in kit/skills/dmc-campaign-full-save/SKILL.md § 2. Character sheets (lines 16-22):
  - check each sheet changed this session, and fix any standing line or stat block a History entry overturns (FR-005, A5)
  - move anyone under "With the party, not companions" whom the player directed in a fight this session into Party status (FR-032, research R6)
- [X] T015 [P] [US1] FR-006 (A6): in kit/skills/dmc-character-sheet-update/SKILL.md:33, widen the species-default rule to every character.
- [X] T016 [P] [US1] FR-032: in the companion comment at kit/templates/campaign/game-state.md:20, add "or directed by the player in a fight".
- [X] T017 [US1] In tests/Dmc.Tests/Toolkit/ToolkitTests.cs, update any assertion pinned to text that T013 to T016 changed (for example lines 977-990). Add assertions for the session log's `- Present:` line and the FR-032 comment.

**Checkpoint**: `dotnet test` passes, and the hook prints sheet lines for a seeded Present line.

---

## Phase 4: User Story 2 - A correction fixes the fact and nothing else (P1)

**Goal**: after a slip, DMC quotes the files, proposes one fix, and changes only the play state until the player
agrees (FR-009 to FR-012).

**Independent Test**: quickstart.md § 3. SC-002 passes when every run meets it.

### Tests for User Story 2

- [X] T018 [P] [US2] Write the out-of-character tests in tests/Dmc.Tests/ContextCheck/ContextCheckContract.cs:
  - one line printed for `ooc x`, `OOC: x`, `ooc` alone, `((x` and the same with leading spaces
  - silent for `oocyte`, "the ooc" mid-text and in-character text
  - the other parts of the output unchanged

### Implementation for User Story 2

- [X] T019 [US2] Create src/Dmc.Cli/ContextCheck/OocReminder.cs to contracts/context-check-sheets.md § Out-of-character line: the trigger, and one line of about 224 bytes with the draft wording. Print it from src/Dmc.Cli/Commands/ContextCheckCommand.cs. Add XML doc comments. Make T018 pass.
- [X] T020 [US2] FR-009 (B1): add the clause to kit/dm-rules.md:85. A correction fixes only the wrong fact; it never rewords the player's text, and never adds a ruling or canon line to make the slip true.
- [X] T021 [US2] FR-010 (B2): add the clause to kit/dm-rules.md:87. Read and quote the files before answering, say what you think, propose one fix, and change only the play state until the player agrees.
- [X] T022 [P] [US2] FR-012 (B3): in kit/skills/dmc-record-feedback/SKILL.md § 3, require naming the step skipped and the moment, never "a habit" or a promise.

**Checkpoint**: `dotnet test` passes, and an `ooc` prompt prints the line.

---

## Phase 5: User Story 3 - The arc plan follows play (P2)

**Goal**: the full save sees an overtaken Arc plan and revises it silently (FR-026).

**Independent Test**: the first half of quickstart.md § 7. SC-003 passes when the full save leaves no stage ahead
resting on Hush and times every clock in sessions.

### Tests for User Story 3

- [X] T023 [P] [US3] Write the arc tests in tests/Dmc.Tests/Size/SizeLibraryTests.cs and tests/Dmc.Tests/Size/SizeCommandContract.cs, with fixtures in SizeFixtures.cs:
  - each of the five flags in contracts/size-arc-flags.md fires, and stays silent when fixed
  - prints nothing: an Arc plan with only the template comment; a `DONE` stage naming the dead; a line naming the dead that also says `dead`; `--arc` when all is well
  - fires: a line naming the dead by last word only ("Hush prepares")
  - `--check` prints no `arc:` line

### Implementation for User Story 3

- [X] T024 [US3] Add the five arc flags to src/Dmc.Cli/Size/Flags.cs, with the contract's lines prefixed `arc:`:
  - dead named ahead: `- **Status**:` starts with `dead`, in any case, and a line not marked `DONE` names them (`ReadSet.Mentions`) without the word `dead` on that line
  - no `current since sN`
  - no `about N sessions`
  - over budget, against `game-state.md`'s `- **Current session**:`
  - an Antagonists entry with no `by sN`

  Only when the Arc plan holds at least one stage line, not only the template comment. Add XML doc comments.
- [X] T025 [US3] Add `--arc` to src/Dmc.Cli/Commands/SizeCommand.cs: the usage (line 29), the option text (near line 36), `Summary` (line 24) and the XML docs. `--arc` prints only the arc lines, and `--check` is unchanged. Make T023 pass.
- [X] T026 [US3] Add one sentence (about 200 bytes) to step 5 of kit/skills/dmc-campaign-full-save/SKILL.md (lines 44-50): run `dmc size <campaign> --arc`, fix each line in `story/dm-notes.md` and run it again, and never mention them to the player, in play or in the report. This file is also edited by T014, so run the two in order.

---

## Phase 6: User Story 4 - DMC never acts for the player's side (P2)

**Goal**: no choice is taken from the player (FR-013 to FR-018).

**Independent Test**: quickstart.md §§ 4 and 5.

- [X] T027 [US4] FR-015 (C3): add the clause to kit/dm-rules.md:142. A reaction on an enemy's turn is the player's roll: DMC asks and stops.
- [X] T028 [US4] FR-017 (H): add the clause to kit/dm-rules.md:148. A scene the player writes out with others' lines is an outline to play, and DMC asks if it is unclear.
- [X] T029 [US4] FR-013 (C1): add the clause to kit/dm-rules.md:150. A question about an action is answered and waited on.
- [X] T030 [US4] Add two clauses to kit/dm-rules.md:154:
  - FR-014 (C2): resolve only what the player declared, never choosing action, target or ammunition
  - FR-016 (G): "us" or "let's" covers everyone present unless the player excepts someone
- [X] T031 [US4] FR-018: add the clause to kit/dm-rules.md:180. A reply that may be sarcastic, ironic or venting, whose literal reading would change time, the files or the plot, is asked about in one line, with nothing written until the player confirms. A plain yes goes through.

---

## Phase 7: User Story 5 - Fight maps show the fight (P3)

**Goal**: the right colours, the size of the part in play, auras that move, and a view window (FR-022 to FR-025).

**Independent Test**: quickstart.md § 6.

### Tests for User Story 5

- [X] T032 [P] [US5] Write the map tests in tests/Dmc.Tests/Map/MapLibraryTests.cs and tests/Dmc.Tests/Map/MapCommandContract.cs, to contracts/map-aura-view.md § Tests:
  - an aura before and after a move, in both the SVG and the distance list
  - a view's cell labels match the full map's, and tokens outside the view are listed under the map
  - each refusal is one line: `ft` not a positive multiple of `cell_ft`, a missing aura `name`, a range outside the grid, a reversed range
  - a map with neither field is unchanged byte for byte

### Implementation for User Story 5

- [X] T033 [US5] Read a token's `aura` (`ft`, `name`) and a map's `view` (`cols`, `rows`) in src/Dmc.Cli/Maps/PyJson.cs.
- [X] T034 [US5] Add the contract's refusals, as one-line errors, in src/Dmc.Cli/Maps/MapChecker.cs.
- [X] T035 [US5] In src/Dmc.Cli/Maps/SvgDrawing.cs:
  - draw the aura as a shaded area around its token, moving with `at`, with terrain unchanged
  - draw only the view's columns and rows, with the place's own labels
  - list the tokens outside the view under the map
- [X] T036 [US5] In src/Dmc.Cli/Maps/Distances.cs, list the tokens inside each aura. Add XML doc comments to every changed member in T033 to T036, and make T032 pass.
- [X] T037 [P] [US5] Document `aura` and `view` in kit/skills/dmc-draw-map/map-format.md (the format's home). Update tests/Dmc.Tests/Map/MapFormatDocTests.cs if it pins the format's fields.
- [X] T038 [P] [US5] Make three changes in kit/skills/dmc-draw-map/SKILL.md:
  - F1 (line 62): `ally` is for companions the player may direct; `neutral` is for any other friendly creature
  - F2 (line 77): draw from a trimmed copy no bigger than the part in play, made before cells go into the tracker
  - F3 and F4 (line 104): fix the map itself and never send a crop; a moving hazard uses the token's `aura`. S2 ships in this PR, so FR-025's stopgap needs no wording.

---

## Phase 8: User Story 6 - Fewer break offers, and clean fiction (P3)

**Goal**: one break offer per 50k band, and no real-world words in the fiction (FR-019 to FR-021).

**Independent Test**: `dotnet test` for the band.

- [X] T039 [P] [US6] In tests/Dmc.Tests/ContextCheck/ContextCheckTests.cs (the band's tests), assert that the next offer comes 50,000 tokens after the last one, and that the first offer after a resume stays at `Margin` (40,000).
- [X] T040 [US6] FR-020: set `Band = 50_000` in src/Dmc.Cli/ContextCheck/BreakReminder.cs:18, leave `Margin` (line 15) unchanged, and update the XML doc comment. Make T039 pass.
- [X] T041 [US6] FR-021 (E1): add the clause to kit/dm-rules.md:193. A real-world word caught while writing is rewritten, never annotated.
- [X] T042 [US6] FR-019 (D1): add the clause to kit/dm-rules.md:207. Playing on after a break offer counts as declining it.

---

## Phase 9: User Story 7 - Routine time closes in a line (P3)

**Goal**: after the arc fix, a session starts with something pending (FR-031). No new kit/dm-rules.md wording
(research R3).

**Independent Test**: the second half of quickstart.md § 7, run in T049.

- [X] T043 [US7] No code or wording task. US7 rests on US3 (T024 to T026) and is checked by the session-start run in T049. If the replies still end on atmosphere, record the evidence in research.md § R3 for a later round.

---

## Phase 10: Polish and the PR

- [X] T044 Measure the cost (FR-027, FR-028) and write each figure into research.md § R10, replacing the estimates:
  - kit/dm-rules.md: it must still be 212 lines. Set `DmRulesBytesAllowed` in tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1209 to the measured bytes, and cite spec 016 FR-027 in its message at :1221.
  - the bytes added to each changed skill and template (`git diff --stat kit/` and a byte count per file)
  - the hook's sheet lines on the 8-person fixture from T004, and the `ooc` line's bytes
- [X] T045 Update the documents listed in plan.md § Documents:
  - .docs/decisions.md:145: amend 40,000 to 50,000, with the reason from #181
  - .docs/dmc-reference.md:813 and .docs/user-guide.md:187: 40,000 to 50,000. Leave .docs/user-guide.md:184 and .docs/roadmap.md:20 as they are (research R8).
  - .docs/dmc-reference.md: § context-check, § map and § size
  - .docs/architecture.md:71 and .docs/campaign-files.md
  - .docs/decisions.md: S1 and its refresh, the B-hook, FR-032, the aura and view, and the arc fix in code
  - .docs/roadmap.md: a new row; note that #226 is replaced
- [X] T046 Check that no document still says 40,000 for the band, "once per session part" for the sheet lines, or "skill only" for the arc fix as current (`git grep` over .docs/ and kit/)
- [X] T047 Run `dotnet test`; all pass (SC-007).
- [X] T048 Run `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`, then `artifacts/bin/dmc refresh D:\Claude\test-campaign`.
- [X] T049 Run quickstart.md §§ 2 to 7 headless in D:\Claude\test-campaign, from copies of rorns-landing, with `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0`. Run each once, and again only if it fails. Record the results in quickstart.md § Results.
- [X] T050 Show the maintainer each changed prompt clause (`git diff kit/`) for wording approval. Apply their changes, then measure again (T044).
- [ ] T051 When the user says to: commit, push and open the PR. Give it a full description: what changed, why, the sources, the tests, the documents, and why no migration is needed (research R6, R7, R9; the arc flags change no file). End it with `Closes #181`. Check it with `gh pr view <n> --json body`. Run the code review once, and show its fixes for approval before committing them. Wait for CI to pass; the user merges.
- [ ] T052 After the merge, set `status:` to `fixed in PR #N` for every entry in scope in D:\Claude\dmc\feedback\ (FR-030). Once 10 sessions have been played on the release, run `review-transcript` and count against the baseline of 1.4 (SC-004, SC-005). Then judge whether #193 is still needed (research R12).

---

## Dependencies & Execution Order

- **Setup (T001 to T003)** comes first.
- **The stories are independent of each other in code**, with three exceptions:
  - US7 rests on US3.
  - Every kit/dm-rules.md task runs in line order: T009 to T012, T020, T021, T027 to T031, T041, T042.
  - kit/skills/dmc-campaign-full-save/SKILL.md is edited by T014, then T026.
- **Polish**:
  - T044 comes after the last dm-rules edit.
  - T049 comes after every story.
  - T050 comes before T051.
- **Within a story**: tests first and failing, then the code, then the prompt clauses.

## Parallel Examples

```text
US1: T004 (tests), T013 (session-log template), T015 (sheet skill), T016 (game-state template)
US2: T018 (tests), T022 (record-feedback skill)
US3: T023 (tests)
US5: T032 (tests), T037 (map-format.md), T038 (draw-map SKILL.md)
Across stories: T004, T018, T023, T032 and T039 (tests in different files)
```

## Implementation Strategy

One PR. Build in priority order, US1 and US2 first (A and B), and check each story's checkpoint before the next. The
headless runs (T049) and the wording approval (T050) come once, at the end, before the PR.
