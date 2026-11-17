---

description: "Tasks for spec 017: dmc is a required part of DMC"
---

# Tasks: `dmc` is a required part of DMC

**Input**: `specs/017-dmc-required/` (plan.md, spec.md, research.md, quickstart.md). No data-model.md or contracts/
(plan.md § Project Structure).

**Tests**: yes, one toolkit test ([research.md](research.md) R6) and the lowered byte cap (R5). Write the test first
and check that it fails.

**Organization**: one PR on this branch, `Closes #208`. Line numbers are `origin/main` at `fb71982` (unchanged at `fbe3ede`, which changed only the constitution and `.docs/`). No task adds a
line to `kit/dm-rules.md` (FR-009); its two edits run in line order (T005, then T009). Each "Today" text below is
quoted from the file; each "After" text is the wording agreed in research.md R2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 to US3 in spec.md

---

## Phase 1: Setup

- [X] T001 Run `git fetch` and confirm branch `017-dmc-required` is at `origin/main` (`fb71982` or later), with only `specs/017-dmc-required/` untracked
- [X] T002 Build (`dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`) and run `dotnet test` from the repo root. Record the baseline pass count in specs/017-dmc-required/quickstart.md § Results
- [X] T003 [P] Record the baseline sizes in specs/017-dmc-required/quickstart.md § Results: `wc -c -l kit/dm-rules.md` (expected 32,522 bytes, 212 lines) and `wc -c` of kit/skills/dmc-draw-map/SKILL.md, kit/skills/dmc-ingest-module/SKILL.md and kit/skills/dmc-setup-rules/SKILL.md

---

## Phase 2: Foundational (the test both P1 stories are held to)

- [X] T004 Write `TheKitCarriesNoRuleForPlayingWithoutDmc` in a new file tests/Dmc.Tests/Toolkit/DmcRequiredTests.cs, a `public sealed partial class ToolkitTests` in `namespace Dmc.Tests.Toolkit` as tests/Dmc.Tests/Toolkit/FeedbackRoundTests.cs is, with an XML `<summary>` on the class part and on the method (spec 017, FR-001, FR-003). The test:
  - reads every file under `kit/` and asserts that none holds, ignoring case, the removed wording: "If `dmc` is unavailable", "Without `dmc`, skip", "If `dmc` fails or the recall", "Otherwise, or when it finds nothing" or "carry on without dice and maps"; a failure names the file and the phrase. Not a looser "if `dmc` fails": kit/skills/dmc-resume-campaign/SKILL.md:14's "If `dmc` fails, Glob `adventures/<campaign>/.closed`: if found, the same." stays (research.md R2, R3; FR-010)
  - asserts `Read("kit/dm-rules.md")` contains "then play nothing until `dmc --version` runs"
  - run it and confirm it fails on the four kit files named in research.md R2

**Checkpoint**: the test exists and fails; T005 to T009 make it pass.

---

## Phase 3: User Story 1 - DMC with a working `dmc` carries no fallback rules (P1) 🎯 MVP

**Goal**: the rules and skills no longer tell DMC how to play without `dmc` (FR-001, FR-004, FR-005).

**Independent Test**: quickstart.md § 2's grep finds nothing in kit/ once T009 (US2) has also landed; the recall,
lookup and text-map rules are still there (acceptance scenarios 1 to 3).

- [X] T005 [US1] In kit/dm-rules.md:78 (§ Look before you bring something back), replace "If `dmc` fails or the recall does not hold it, read its sheet" with "If the recall does not hold it, read its sheet". The rest of the line is unchanged (FR-004: the sheet and the search stay)
- [X] T006 [P] [US1] In kit/skills/dmc-draw-map/SKILL.md:114, replace "If `dmc` is unavailable, or the player options ask for text maps, draw the same grid" with "If the player options ask for text maps, draw the same grid". The rest of the paragraph is unchanged (FR-005)
- [X] T007 [P] [US1] In kit/skills/dmc-ingest-module/SKILL.md, delete line 26 whole: "  - If `dmc` is unavailable, fall back to the Read tool's `pages` parameter (at most 20 pages per call; it shows pages as images as well as text) and tell the player that a PDF over 100 MB needs `dmc`.". Lines 22 to 25 (the **PDF** bullet and its steps 1 to 3) and line 27 (**Markdown, text, HTML**) stay as they are
- [X] T008 [P] [US1] In kit/skills/dmc-setup-rules/SKILL.md:
  - line 17: delete " (Without `dmc`, skip this step.)", leaving "... gives its PDF page. To see a page as printed, ..."
  - line 42: replace "Otherwise, or when it finds nothing, open the index" with "When it finds nothing, open the index" (FR-004: `dmc` finding nothing keeps its next steps)

**Checkpoint**: four of the five phrases are gone from kit/; "carry on without dice and maps" goes with T009.

---

## Phase 4: User Story 2 - DMC with a missing or broken `dmc` sends the player to reinstall (P1)

**Goal**: a missing `dmc` is an error: DMC gives the install line and plays nothing until `dmc --version` runs
(FR-002, FR-003, FR-006, FR-010).

**Independent Test**: T004's test passes; the existing tests of the notice and the reinstall line pass unchanged; the
headless run of quickstart.md § 3 (T022) gives the install line and plays nothing.

- [X] T009 [US2] In kit/dm-rules.md:139 (§ Dice), after T005, replace ", and carry on without dice and maps until then." with ", then play nothing until `dmc --version` runs.". The install lines before it and "`dmc` also draws the maps and reads PDFs." after it are unchanged. No line is added (FR-009)
- [X] T010 [US2] Run `dotnet test --filter "FullyQualifiedName~ToolkitTests"` and confirm: `TheKitCarriesNoRuleForPlayingWithoutDmc` passes; `WithoutAWorkingDmcASessionIsToldOnceToReinstall` (tests/Dmc.Tests/Toolkit/ToolkitTests.cs:336) and the reinstall-line assertion at :184 pass unchanged; kit/settings.json:101-108 (the `SessionStart` hook) and the skills' error lines named in research.md R4 are untouched (`git diff --stat` lists none of them) (FR-002, FR-006, FR-010)

**Checkpoint**: both P1 stories are done in the kit.

---

## Phase 5: User Story 3 - The documents say `dmc` is required (P2)

**Goal**: no document promises play without `dmc`, and one decision records the retirement (FR-007).

**Independent Test**: quickstart.md § 5's grep finds "never depends on `dmc`" and "play and saving still work" only in
decision entries marked "Partly superseded".

- [X] T011 [P] [US3] In README.md:11, replace "Without `dmc`, play and saving still work, but DMC cannot roll its own dice, draw maps or read PDFs." with "DMC needs it to play: if `dmc` goes missing, DMC tells you how to reinstall it and plays nothing until it runs again." (player words, constitution Documentation 2)
- [X] T012 [P] [US3] In .docs/architecture.md:120, replace the principle "**Play never depends on `dmc` to start.** Without it Claude Code still starts in the DMC folder, and DMC can still play and save." with "**`dmc` is required.** Without it Claude Code still starts in the DMC folder, so the start-of-session notice can be shown, but DMC gives the install line and plays nothing until `dmc --version` runs. An error that a working `dmc` reports is not a missing `dmc`: the skills handle it where it matters, and never hide it." Line 119 (§ Hooks never get in the way) stays
- [X] T013 [P] [US3] In .docs/user-guide.md:
  - line 65: after "DMC tells you to reinstall", add "and plays nothing until `dmc` runs again"
  - line 450 (troubleshooting row "DMC says `dmc` is missing or does not run"): replace "Without `dmc`, DMC cannot roll its own dice, draw maps or read PDFs." with "DMC does not play until `dmc` runs again."
- [X] T014 [US3] In .docs/decisions.md, after T019 (the skill sizes) and T022 (the notice under `claude -p`):
  - add an entry at the end, "## `dmc` is a required part of DMC (issue #208)", with Date 2026-11-14 (decided while clarifying #179's Phase C, spec 012), clarified 2026-11-17; Decision (the six kit edits of research.md R2; a missing `dmc` is the shell's "command not found" or a program that cannot start, and DMC gives the install line and plays nothing until `dmc --version` runs; an error from a working `dmc` keeps today's handling, R4); Reason (every play path runs through `dmc`; the fallback wording cost context and was never used); Cost (`kit/dm-rules.md` 0 lines, −16 bytes, cap 32,522 → 32,506; the skill sizes from T019); Rejected (a line of its own in `dmc-resume-campaign`; a general rule sorting `dmc` errors into five kinds, about 470 bytes or 120 tokens a turn, with no feedback entry showing an error handled badly); Limits (the `SessionStart` notice under `claude -p`, as T022 found it); Source (issue #208; `specs/017-dmc-required/`; the maintainer's answers of 2026-11-17)
  - add a last bullet "- **Partly superseded 2026-11-17** by "`dmc` is a required part of DMC (issue #208)": ..." to § Retrieval runs over the markdown, with no stored index (issue #179) (its Decision's "play never depends on `dmc`: when it fails, DMC searches and reads the files as before" and its Limits' "the fallback covers it"; a recall or lookup that finds nothing is still followed by a search) and to § Lookup and recall are for play only until Phase D (issue #179) ("with the fallback to searching", where it meant a failing `dmc`)
- [X] T015 [P] [US3] In .docs/roadmap.md, add a row above the spec 016 row (line 54): "`dmc` is a required part of DMC, issue #208, spec 017 | Done (PR #N), for the next release | Changes the kit only: six fallback instructions for a missing `dmc` removed from `kit/dm-rules.md`, `dmc-draw-map`, `dmc-ingest-module` and `dmc-setup-rules`; a missing `dmc` stops play until it runs again; the notice and the install lines stay. `kit/dm-rules.md` 0 lines, −16 bytes. No migration. Closes #208. Spec: `specs/017-dmc-required/`. See [decisions.md](decisions.md)." (PR number filled in once the PR is open, T024)

**Checkpoint**: quickstart.md § 5 passes.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T016 Measure `wc -c -l kit/dm-rules.md` after T005 and T009: expected 32,503 bytes, 212 lines. If it differs, find out why before going on (measured 32,506: the expected figure counted the "If " that stays at line 78; research.md R5 corrected)
- [X] T017 In tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1211, set `DmRulesBytesAllowed` to the size T016 measured, and extend its `<summary>` with "; and spec 017 (FR-009) lowered it by 16 when the fallback for a missing `dmc` went". `DmRulesLines` (212) and its summary stay. Also update `ThePlayRulesNameRecallAndLookupAndKeepTheFallback` (`ToolkitTests.cs:1324`), which held the line-78 text T005 removed, to hold "If the recall does not hold it, read its sheet" (research.md R6)
- [X] T018 Run `dotnet test` from the repo root: every test passes, the count is the baseline of T002 plus one. Record it in specs/017-dmc-required/quickstart.md § Results
- [X] T019 [P] Measure `wc -c` of the three skills edited in T006 to T008 against T003's baseline and record the bytes each lost in specs/017-dmc-required/research.md R5 (replacing "the sizes are measured at implementation") and in quickstart.md § Results
- [X] T020 [P] Run quickstart.md § 2 and § 5 (the kit grep, the sizes, the documents grep) and record the results in quickstart.md § Results (SC-001, SC-002, SC-005)
- [X] T021 (Done instead in a scratch copy of the test folder with this branch's kit written in, as quickstart.md § Results says: the maintainer works remotely.) Ask the maintainer to run, from a terminal outside Claude Code, `artifacts/bin/dmc refresh D:\Claude\test-campaign` once this branch has been rebuilt after the kit edits (`dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`, so the binary embeds the edited kit), and wait for them to confirm
- [X] T022 Run quickstart.md § 3 in D:\Claude\test-campaign with neither `artifacts/bin` nor `C:\Users\webhe\AppData\Local\Programs\dmc` on `PATH`: `claude -p --model sonnet "Let's continue the campaign east-hills."`. Pass: the reply gives the install line and plays nothing (no scene, no roll, no map). Record the reply, whether the start-of-session notice appeared, and pass or fail in quickstart.md § Results (SC-003). Then, with `artifacts/bin` back first on `PATH`, run `claude -p --continue --model sonnet "I've reinstalled dmc."` in the same folder. Pass: DMC runs `dmc --version` and resumes (FR-003, US2 scenario 2). Record it too
- [X] T023 Run quickstart.md § 4 in a scratch copy of a test campaign with `artifacts/bin` first on `PATH` (set `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0`): a roll, a recall of a returning NPC and a map, all by `dmc`. Record pass or fail in quickstart.md § Results (SC-004)
- [ ] T024 After the PR is open, fill in its number in .docs/roadmap.md (T015), and, with the maintainer's word, edit issue #208's description to link `specs/017-dmc-required/` and summarise the spec in two or three lines (`.docs/development.md` § Spec-driven changes). The PR description also names the corrections from PR #232's code review that this branch carries (`.docs/decisions.md`: the 4.0.0 entry's Reason and Limits, and the 3.0.0 entry's stale Development 5 to-do; `.docs/development.md:170`: the 3.0.0 list restored)

---

## Dependencies & Execution Order

- **Setup (T001 to T003)** comes first.
- **Foundational (T004)** comes before every kit edit, so the test is seen to fail first.
- **The stories:**
  - US1 (T005 to T008) and US2 (T009, T010) touch the same file in one place, kit/dm-rules.md: T005, then T009.
  - T010 comes after T005 to T009.
  - US3 (T011 to T015) needs no kit edit and can run alongside; T014 waits for T019 and T022.
- **Polish**:
  - T016 and T017 come after T009.
  - T018 comes after T017.
  - T021 comes after every kit edit, then T022 and T023.
  - T024 comes once the PR is open.

## Parallel Examples

```text
US1: T006 (dmc-draw-map), T007 (dmc-ingest-module), T008 (dmc-setup-rules), each a different file
US3: T011 (README), T012 (architecture), T013 (user guide), T015 (roadmap)
Polish: T019 (skill sizes) and T020 (greps)
```

## Implementation Strategy

One PR. Write the test (T004) and see it fail, then make the kit edits in priority order (US1, then US2), and the
documents (US3). The byte cap and the full test run follow, then the maintainer's refresh and the two headless runs,
before `/speckit-converge` and the PR.
