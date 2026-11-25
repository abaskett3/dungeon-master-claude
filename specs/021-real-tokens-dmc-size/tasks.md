---

description: "Task list for spec 021, real tokens in dmc size"
---

# Tasks: Real tokens in `dmc size`

**Input**: Design documents from `specs/021-real-tokens-dmc-size/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/dmc-size-output.md, quickstart.md

**Tests**: Requested by the spec (FR-009). The new `Measurement` tests are written first and fail before the measure
exists. The existing tests that size a fixture as `Budgets.X * 4` bytes move to the new inverse in the story that
changes their numbers.

**Organization**: Phase 2 builds the measure every story uses. US1 (the near-cap flag) and US2 (the total) are both
P1. US1 goes first, as the issue asks. US3 (the budgets) changes the numbers last, and it holds the check that no
campaign gains a flag. All three stories touch `Flags.cs` or `SizeReport.cs`, so they run in order.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

---

## Phase 1: Setup

**Purpose**: A known starting point, and the "before" outputs of SC-004.

- [X] T001 Run `dotnet test tests/Dmc.Tests` on the branch before any change and note the pass count.
- [X] T002 [P] Run `dotnet run --project src/Dmc.Cli -- size <name> --check --folder <folder>` on `main`'s code for
  each campaign at hand:
  - `rorns-landing` and `the-silver-egg` in `D:/Claude/dmc`;
  - each campaign under `D:/Claude/test-campaign/adventures`.
  Also run the full report (no `--check`) for `rorns-landing`. Save each output to the scratchpad as
  `before-<name>.txt`, for quickstart Q2 and Q3.

---

## Phase 2: Foundational (the measure)

**Purpose**: One home for the token count (FR-001, FR-002; data-model.md § The measure). Every story needs it.

- [X] T003 Write the tests first, in a new `tests/Dmc.Tests/Size/MeasurementTests.cs`, with an XML doc comment on the
  class and on each test. They must fail to build or fail until T004 is done.
  - `Measurement.Tokens(0)` is 0, `Tokens(4)` is 0, `Tokens(5)` is 2, and `Tokens(40_000)` is 16,000.
  - The R3 edge: for B = `Budgets.GameState` in the old unit (4,000), a text of 4B + 4 characters is not over 1.6 × B,
    and one of 4B + 5 characters is.
  - `Measurement.ReadCost(characters, calls)` is `Tokens(characters) + 160 × calls`.
  - `Measurement.Split`, given `"ab\r\ncd\nef"` as UTF-8, gives line characters 3, 3, 2:
    - the CR is not counted, the LF is, and a last line with no line end counts none;
    - the bytes stay 4, 3, 2.
  - A line of non-ASCII text (`"é—ü\n"`) has fewer characters than bytes.
- [X] T004 In `src/Dmc.Cli/Size/Measurement.cs`, add the measure. Each member gets an XML doc comment.
  - `Line`: add `Characters`, which is `Text.Length` plus 1 when the line has an LF. A CR before the LF is not counted.
    A BOM is not counted either, since `Text` already drops it.
  - `Split` fills `Characters`.
  - Add `public const int ReadCallTokens = 160` (research R1).
  - Add `public static long Tokens(long characters) => 2 * (characters / 5)`. Its comment states the calibration in
    one line (0.4 a character, research R1 and R2) and the R3 rounding.
  - Add `public static long ReadCost(long characters, int calls) => Tokens(characters) + ReadCallTokens * (long)calls`.
- [X] T005 In `src/Dmc.Cli/Size/Measurement.cs`, move `Section` and `Part` to characters, with the XML doc comments
  rewritten.
  - `Section`: `Bytes` becomes `Characters`, and `Tokens` becomes `Measurement.Tokens(Characters)`.
  - `Part`: `ExtraBytes` becomes `ExtraCharacters`, and `Bytes` becomes `Characters`.
  - `Part.Tokens` becomes `Tokens(Characters)`, and `FileTokens` becomes `Tokens(sum of FileLines' Characters)`.
  - Add `Part.ReadCost`, which is `ReadCost(Characters, ExtraCharacters > 0 ? 2 : 1)`.
  - `Sections(lines)` sums `Characters`.
  - Update every use of these members:
    - `ReadSet.cs:488`, where the recall output becomes characters through a `Recall` helper that counts characters
      the same way;
    - `Flags.SectionTokens`, and the uses in `SizeReport.cs`.
  - Build and run T003's tests: they pass.
- [X] T006 Replace every other bytes ÷ 4 with `Measurement.Tokens` of summed characters (research R6). Text tokens
  only, in this task:
  - `src/Dmc.Cli/Size/Flags.cs:160` (`ArcPlanContentTokens`);
  - `src/Dmc.Cli/Size/ReadSet.cs:485`;
  - `src/Dmc.Cli/Retrieval/CanonReminder.cs:69`;
  - `src/Dmc.Cli/Retrieval/SceneRead.cs:47`;
  - `src/Dmc.Cli/Modules/ModuleCheck.cs:91` (a heading's own text);
  - `src/Dmc.Cli/Modules/ModuleCheck.cs:74`, which keeps its text-token value for now: US1 makes it a Read cost.
    `MarkdownFile` needs characters per line beside `LineBytes`; add them, with doc comments.
  - `src/Dmc.Cli/Retrieval/Lookup.cs:143`, for now; US1 changes it.
  - `src/Dmc.Cli/Campaign/SequelDigest.cs:258`: read the file's text with `File.ReadAllText` and count its
    characters, with CR left out as in `Line`. Comment the change.
- [X] T007 Add `NoCodeDividesBytesIntoTokens` to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, with an XML doc comment
  naming spec 021 FR-001.
  - For every `*.cs` under `src/`, no line matches `(Bytes|Length)\)?\s*/\s*4\b` or `Sum\([^)]*Bytes[^)]*\)\s*/\s*4`.
  - It passes after T006.

**Checkpoint**: the code uses one measure. The numbers in `Budgets.cs` are still the old ones, so the size tests that
pin bytes fail. US3 fixes them; note the count of failures here and expect it to fall to 0 by T019.

---

## Phase 3: User Story 1 - The near-cap warning comes before the file is cut (Priority: P1) 🎯 MVP

**Goal**: every near-cap check compares a part's Read cost with 18,000. Canon within its limit is not flagged (FR-003;
research R5).

**Independent Test**: quickstart Q5. A whole session log of 46,000 characters is flagged and one of 44,000 is not.
A canon of 70,000 characters gets no near-cap line.

- [X] T008 [US1] Write the tests first, in `tests/Dmc.Tests/Size/SizeLibraryTests.cs`, replacing the tests at
  `:478`, `:480` and `:493`.
  - `story/dm-notes.md` is near the cap at a Read cost of `Budgets.NearCapTokens` and not one character under it.
    Size the text through a new `Chars(long tokens)` helper in `tests/Dmc.Tests/Size/SizeFixtures.cs`: `tokens * 5 / 2`
    characters, rounded up to the next multiple of 5. The text must land on the mark after the 160 a call:
    characters for `NearCapTokens - ReadCallTokens`.
  - A session log read whole, of `Chars(NearCapTokens)` characters, is flagged.
  - A new test: a canon below `CanonLimit` but with a Read cost over `NearCapTokens` has no `NearCap` flag and no
    `near the Read cap` note in the table.
- [X] T009 [US1] In `src/Dmc.Cli/Size/Budgets.cs`, set `NearCapTokens = 18000`.
  - Rewrite its summary: it is compared with the Read cost (`Measurement.ReadCost`); research R5's reason is
    "18k flags every part of 20k real tokens, since the estimate is at most 7.5% under the real count".
  - Keep `ReadCapTokens = 25000` and rewrite its summary: it is in real tokens, as the Read tool counts them.
- [X] T010 [US1] In `src/Dmc.Cli/Size/Flags.cs:89`, compare `part.ReadCost` with `Budgets.NearCapTokens`, skip a part
  whose `Budget` is `BudgetKind.Canon`, and show `Fmt(part.ReadCost)` in the line.
  - Make the same change to the table's note in `src/Dmc.Cli/Size/SizeReport.cs:124`.
  - Update the XML doc comment of `FlagKind.NearCap` (`Flags.cs:11`) to say canon is left out and why (research R5).
- [X] T011 [P] [US1] In `src/Dmc.Cli/Modules/ModuleCheck.cs:74-77`, compare the file's Read cost (`ReadCost`, 1 call)
  with the mark, and print that figure.
  - Update `tests/Dmc.Tests/Modules/ModuleCheckTests.cs:48`, `:150` and their sizes to `Chars(...)` from T008's helper.
    Copy the helper into the test, or make it `internal` in a shared support file under `tests/Dmc.Tests/Support/`.
- [X] T012 [P] [US1] In `src/Dmc.Cli/Retrieval/Lookup.cs:141-148`, compare the section's Read cost with the mark, and
  rewrite the comment at `:141`.
  - Update `tests/Dmc.Tests/Retrieval/LookupLibraryTests.cs:194` and its fixture, including the comment about "bytes ÷ 4
    rounded down", to the edge in characters.
- [X] T013 [P] [US1] In `src/Dmc.Cli/Campaign/SequelDigest.cs:258-261`, compare the file's Read cost with the mark
  (`>`, as now), and make `largest` a Read cost too. Update `tests/Dmc.Tests/Campaign/SequelDigestTests.cs` where it
  sizes a digest file over the mark.
- [X] T014 [US1] Run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~ModuleCheck|FullyQualifiedName~Lookup|FullyQualifiedName~SequelDigest|FullyQualifiedName~NearCap"`
  and check that the US1 tests pass.

**Checkpoint**: the near-cap flags are right. US1 is usable alone, since the budget flags still use their old numbers
in the new unit, which is stricter. It must not ship before US3.

---

## Phase 4: User Story 2 - The resume total says what the resume really costs (Priority: P1)

**Goal**: the report's total is the Read cost of the parts in the total. The header and total line follow
contracts/dmc-size-output.md (FR-006).

**Independent Test**: quickstart Q3. The long campaign's total is about 95k, within 88k to 124k.

- [X] T015 [US2] Write the tests first, in `tests/Dmc.Tests/Size/SizeCommandContract.cs`.
  - `:65` and `:353`: the header is `<name>: what a resume reads, in tokens (estimated: 2 for 5 characters)`.
  - A new test: the total line is `total <T>   to read, with <N> Read calls; DMC's own replies are not counted`,
    with T the sum of `ReadCost` of the parts in the total and N their calls. Take the exact column spacing from the
    existing total line's format in `SizeReport.cs`.
  - In `tests/Dmc.Tests/Size/SizeLibraryTests.cs:146`, the summary's `Tokens` is `Measurement.Tokens` of its
    characters, not `Length / 4`.
- [X] T016 [US2] In `src/Dmc.Cli/Size/SizeReport.cs`, make `Total` (`:13`) the sum of `ReadCost` over the parts with
  `InTotal`. Add `Calls`, the sum of their calls, with a doc comment. Write the header and total line as in T015.
- [X] T017 [US2] In `src/Dmc.Cli/Commands/SizeCommand.cs`, change the class's XML summary (`:6`) and `Help` (`:34`):
  "in estimated tokens (bytes / 4)" becomes "in tokens (estimated at 2 for 5 characters, which matches what Claude
  Code counts within a few percent)". `Summary` keeps "in estimated tokens".

**Checkpoint**: run T015's tests; they pass.

---

## Phase 5: User Story 3 - The budgets measure real tokens and hold the same text (Priority: P2)

**Goal**: every budget in tokens becomes 1.6 times its old value, and no campaign gains a budget or canon flag
(FR-004, FR-005; research R3; SC-004).

**Independent Test**: quickstart Q2. Compare the `--check` outputs before and after.

- [X] T018 [US3] In `src/Dmc.Cli/Size/Budgets.cs`, set the new numbers of research R3:
  - `GameState` 6400, `DmNotes` 16000, `DmNotesAboutThePlayer` 3200, `DmNotesArcPlan` 2400, `Summary` 4800;
  - `PcSheet` 9600, `OtherSheet` 6400, `PlayCard` 1600;
  - `CanonLimit` 30400 (`CanonWholeBelow` stays `= CanonLimit`), `CanonArchiveMark` 25600, `CanonPricesSection` 4800;
  - `Legacy` 4800, `ModuleSection` 4800.

  Rewrite the comments:
  - The class summary: "in tokens of file text (`Measurement.Tokens`, 2 for 5 characters) unless a member says
    otherwise; each is 1.6 times its number under bytes ÷ 4, so it allows the same text (spec 021)".
  - Each member's measured figures: for example "a PC's features... measured 5.4k" becomes 8.6k, and "companions
    measured 1.8k to 3.5k" becomes 2.9k to 5.6k. Mark each converted figure "(×1.6 of the bytes ÷ 4 figure)".
  - `SceneReadBytes`: "about 4,800 tokens". `Recall.cs:18`: "about 2,400 tokens".
- [X] T019 [US3] Move every fixture that sizes text as `Budgets.X * 4` bytes to `Chars(Budgets.X)` characters, plus or
  minus the same margin in characters, so each test keeps what it checks:
  - `tests/Dmc.Tests/Size/SizeLibraryTests.cs:119-123`, `:340-400`, `:427`, `:504-585`, `:621-646`;
  - `tests/Dmc.Tests/Modules/ModuleCheckTests.cs:62` and `:70`;
  - `tests/Dmc.Tests/Retrieval/SceneReadTests.cs:35`, by counting characters, not `GetByteCount`;
  - `tests/Dmc.Tests/Retrieval/SceneTimingTests.cs:48`;
  - `tests/Dmc.Tests/ContextCheck/CanonReminderContract.cs:32`, whose summary says "about 83k bytes" past
    `CanonWholeBelow`. Check it still passes the new limit of 76,000 characters, and fix the summary.

  Then run `dotnet test tests/Dmc.Tests`: all pass. The one exception is `TheDocumentedBudgetsEqualTheCode`, which
  T022 fixes.
- [X] T020 [US3] Run quickstart Q2: the same commands as T002, with this branch's code. Save `after-<name>.txt`.
  - Compare with the `before-<name>.txt` files. The lines must be the same, in the same order, except for their
    numbers. A near-cap line may be added for a file other than canon.
  - Any budget, canon or section line that is new is a failure. Stop and report it, with the file's characters and
    bytes.
  - Keep both outputs for the PR.

**Checkpoint**: the numbers are correct and no campaign trims more.

---

## Phase 6: Polish and documentation

**Purpose**: FR-010 to FR-012, Documentation 4, and the calibration checks.

- [X] T021 [P] In `.docs/decisions.md`, under "Size rules are measured in tokens by `dmc size`", add after the existing
  **Amended 2026-11-15** paragraph an **Amended 2026-11-24** paragraph (issue #246, spec 021), in the style of that
  one. It gives:
  - the measure: 2 tokens for 5 characters; a Read call costs that plus 160;
  - the calibration: 611 Read batches in 194 logs, with a median error of 2.1% on large batches, and real counts of
    four files within −7% to +5%, the same on Sonnet and Opus (research R1, R2, R4);
  - the reason: the budgets keep their text, at 1.6 times their numbers, because keeping the numbers would make every
    full save trim about 40% more, the probable source of #262's false lines;
  - the near-cap mark of 18k, and canon within its limit left out;
  - no migration, and why;
  - the limits:
    - an estimate, about 8% off on small batches;
    - measure again if a model's tokenizer changes (scripts in `D:\Claude\dmc-slip-analysis\spec021\`);
    - the Crooked Moon module files and the Silver Egg digest files now over the mark, reported at the next ingest or
      sequel import;
    - the Read cap not re-measured.

  Leave the Decision line as it is.
- [X] T022 [P] In `.docs/campaign-files.md`, update:
  - `:85`: "in estimated tokens (bytes ÷ 4)" becomes "in tokens (2 for 5 characters)";
  - the budget table under `:87`, to the new numbers;
  - `:103`: the near-cap rule becomes "a Read cost of 18,000 tokens or 1,600 lines, canon within its limit left out";
    the Prices section becomes 4,800;
  - `:239`: "A kind near 20k tokens" becomes "A kind near the Read cap mark (18k tokens of Read cost)";
  - `:255`: "over 20k tokens" becomes "over the Read cap mark".

  Then run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~TheDocumentedBudgetsEqualTheCode"`: it passes.
- [X] T023 [P] In `.docs/dmc-reference.md`:
  - § size (`:692` unit sentence; `:703` what it flags: 18,000 of Read cost, canon left out, the 30,400 limit; the
    `--check` example), using the contract's example lines;
  - § module (`:737` and the example at `:746`, with `(18k)` and the file's Read cost);
  - § sequel (`:666`, "over 18,000 tokens of Read cost");
  - any other "estimated tokens" figure in the file (grep for it).
- [X] T024 [P] In `kit/skills/dmc-new-campaign/SKILL.md:47`, "a kind near 20k tokens goes on in `people-2.md`" becomes
  "a kind that `dmc campaign sequel --check` reports too large goes on in `people-2.md`". Check step 3's sentence there
  names the check the subagent runs. If it names another command, use that command's name instead.
- [X] T025 [P] In `.docs/roadmap.md`, add or update the row for issue #246 (spec 021, real tokens in `dmc size`), and
  the status line's date.
- [X] T026 Run quickstart Q3 (`dmc size rorns-landing --folder D:/Claude/dmc`) and Q4 (the three scripts in
  `D:\Claude\dmc-slip-analysis\spec021\`). Check the figures match research R1, R2 and R4, and save the outputs for
  the PR.
- [X] T027 Run quickstart Q5 by hand in a scratch folder, and Q1 (`dotnet test tests/Dmc.Tests`): all pass.
- [X] T028 Check the documentation is complete, with these two greps:
  - `.docs/`, `kit/` and `src/` hold no stale "bytes ÷ 4", "bytes / 4", "20,000 estimated", "20k tokens", "19,000" or
    "19k limit". Leave the decision text where an amendment explains it.
  - Every type and member added or changed in T004 to T018 has an XML doc comment.

---

## Dependencies & Execution Order

- Phase 1, then Phase 2 (T003 → T004 → T005 → T006 → T007).
- US1 (T008 to T014) after Phase 2. T011, T012 and T013 are in different files and can run in parallel after T009
  and T010.
- US2 (T015 to T017) after US1, since it shares `SizeReport.cs`.
- US3 (T018 to T020) after US2. T020 needs T002's before outputs.
- Phase 6 after US3. T021 to T025 are in different files and can run in parallel. T026 to T028 come last.

## Parallel Example: User Story 1

```text
After T010: T011 (ModuleCheck.cs), T012 (Lookup.cs), T013 (SequelDigest.cs) together.
```

## Implementation Strategy

- **MVP**: Phase 2 and US1, which fixes the flag that loses facts at resume. It cannot ship alone: until US3, the old
  numbers in the new unit flag almost every file over budget. Ship all three stories in one PR, as the spec and
  `converge-before-pr` ask.
- **Order**: the measure, then the near-cap flags, the total and the numbers, then the documents and the PR.
- **The PR**:
  - It says `Closes #246` and `Part of #239`, one per line.
  - It gives the Development 2 statement (no migration, with T020's outputs) and quotes T026's figures.
  - After opening it, check its body with `gh pr view <n> --json body`.
