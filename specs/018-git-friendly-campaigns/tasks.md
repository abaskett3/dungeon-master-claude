---

description: "Task list for spec 018, git-friendly campaigns"
---

# Tasks: Git-friendly campaigns

**Input**: Design documents from `specs/018-git-friendly-campaigns/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: The spec asks for them. FR-007 asks for a toolkit test of the ignore rules, and the two contracts list the
cases. Tests come before the code they cover in each story.

**Organization**: Tasks are grouped by user story, so each story can be built and checked on its own.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (a different file, and no dependency on an unfinished task)
- **[Story]**: the user story the task belongs to (US1 to US4)

## Path conventions

- CLI: `src/Dmc.Cli/`. Tests: `tests/Dmc.Tests/` (MSTest). Kit: `kit/`. Documents: `.docs/`.
- Line numbers are from `origin/main` at `fb71982`.
- Every new or changed type and member gets an XML doc comment, private ones included (CLAUDE.md § Documentation).

---

## Phase 1: Setup

**Purpose**: bring the spec in line with the plan amendment before any code is written

- [X] T001 In `specs/018-git-friendly-campaigns/spec.md`, record the plan amendment as approved, or revert it if the maintainer declines:
  - Change the `**Plan amendment, pending the maintainer's approval:**` line (:61) to say it was approved on 2026-11-17.
  - In § Assumptions, the line "apart from line endings made consistent by the migration" is stale, because FR-004 now has no migration. Change it to "A player who never uses git sees no difference in play."

---

## Phase 2: Foundational

**Purpose**: none. The four stories touch separate files, and no shared code has to exist first. The comparison
helper (T008) serves only US2.

---

## Phase 3: User Story 1 - Put a campaign in git without noise (Priority: P1) 🎯 MVP

**Goal**: the kit ships the ignore and line-ending rules for a repository at `adventures/`. They leave out backups
(except closing backups), map renders, per-machine state and staging folders, and they store text with LF.

**Independent Test**: run the T002 toolkit test. Then run quickstart Q1 and Q2 on a copy of the maintainer's
`adventures/`: the first commit holds no ignored file, `.git` is at most 25% of the campaigns' size, and
`git status --porcelain` is empty after a commit and after a clone.

### Tests for User Story 1

- [X] T002 [US1] Add a test class `AdventuresGitRulesTests` in a new file `tests/Dmc.Tests/Toolkit/AdventuresGitRulesTests.cs`, following contract `contracts/adventures-git-rules.md` § Test.
  - Find `git` on the PATH the way `tests/Dmc.Tests/Distribution/Bootstrap.cs:83` finds a tool, and call `TestEnv.Need(found, "git")` (Development 7).
  - In a temporary folder, run `git init`. Copy `kit/templates/adventures.gitignore` in as `.gitignore` and `kit/templates/adventures.gitattributes` in as `.gitattributes`. Create an empty file at every path in the contract's **Kept** and **Ignored** lists, using `rorns-landing` for `<c>` and `the-silver-egg` for `<other>`.
  - Run `git check-ignore` (exit code 1 means not ignored) on each path. Every Kept path must be not ignored, and every Ignored path must be ignored.
  - Run `git check-attr eol text -- x.md y.zip`. The `.md` file must report `eol: lf`, and the `.zip` must report `text: unset` (binary).
  - A second test method ties the Kept list to the resume. For every part `ReadSet.Build(dmcFolder, "rorns-landing")` returns (`src/Dmc.Cli/Size/ReadSet.cs:119`), run against a fixture DMC folder, the part's path relative to `adventures/` must be on the Kept list or be matched by one of its patterns (a sheet name, a session log), and must be not ignored. Build the fixture from an existing helper in `tests/Dmc.Tests/Size/SizeFixtures.cs`, if one makes a full campaign.
  - Run git with the temporary folder as the working folder, through the `TestEnv` command runner (`tests/Dmc.Tests/Support/TestEnv.cs`, the method after `Need`), and delete the folder afterwards.
  - The test fails until T003 and T004 exist.

### Implementation for User Story 1

- [X] T003 [P] [US1] Create `kit/templates/adventures.gitattributes` with the exact content in `contracts/adventures-git-rules.md` § `kit/templates/adventures.gitattributes`: a `# DMC: copy to adventures/.gitattributes ...` comment line, `* text=auto eol=lf`, `*.zip binary` and `*.png binary`. Use LF line endings (`kit/** text eol=lf`).
- [X] T004 [P] [US1] Create `kit/templates/adventures.gitignore` with the patterns in `contracts/adventures-git-rules.md` § `kit/templates/adventures.gitignore`, which may not change without changing the contract:
  - `/.active-campaign`, `/.reviewed-sessions` and `/.transcript-review/`
  - `/.dmc-import-*/`, `/.restore-*/` and `/.replaced-*/`
  - `/*/maps/live.html`, `/*/maps/live-data.js`, `/*/maps/live.png` and `/*/maps/live-source.txt`
  - `/*/backups/*` and `!/*/backups/*_closed_*.zip`

  Keep the contract's comments; they are the file's documentation. Use LF line endings.
- [X] T005 [US1] Check that the two new files reach a DMC folder.
  - `GeneratedFiles.Table` maps `kit/templates/` to `templates/` (`src/Dmc.Cli/Kit/GeneratedFiles.cs:32`), and the csproj embeds `kit/**/*` (`src/Dmc.Cli/Dmc.Cli.csproj:68`).
  - Add a test to `tests/Dmc.Tests/Kit/GeneratedFilesTests.cs`. `GeneratedFiles.TargetOf("kit/templates/adventures.gitignore")` must be `templates/adventures.gitignore`, the same must hold for `.gitattributes`, and both must be in `GeneratedFiles.All`.
  - Search `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, `tests/Dmc.Tests/Distribution/InstallTests.cs` and `tests/Dmc.Tests/Kit/RefreshTests.cs` for a list of the template files, or a check that every file under `kit/templates/` is Markdown, and add the two names or an exception where one exists. `ToolkitTests.KitPrompts` (:54) only collects `*.md`, so it needs no change.
- [X] T006 [US1] Run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~AdventuresGitRules|FullyQualifiedName~GeneratedFiles"`. All pass, with git on the PATH.

**Checkpoint**: the kit ships the rules, and a test proves what they keep and leave out.

---

## Phase 4: User Story 2 - DMC's own checks still work on a git copy (Priority: P1)

**Goal**: the restore's "changed since" count and the closed-campaign check (used by `dmc campaign verify` and the
sequel import) treat a file whose only difference is CRLF against LF as unchanged (FR-005).

**Independent Test**: the T007 tests pass. Then run quickstart Q3 on a clone: verify of the-silver-egg reports it
unchanged, a restore dry run warns about no files, and a one-word change is still reported.

### Tests for User Story 2

- [X] T007 [US2] Add the cases in contract `contracts/line-ending-compare.md` § Tests. Each must fail before T008 to T010 except the last two, which pass today and must keep passing.
  - In `tests/Dmc.Tests/Campaign/CampaignVerifyClosedTests.cs`, a closed campaign whose closing backup holds CRLF text files, with the campaign's copies turned to LF, still verifies with no `changed:` line.
  - In the same file, the reverse: the backup holds LF and the campaign holds CRLF.
  - In `tests/Dmc.Tests/Campaign/SequelDigestTests.cs`, a sequel import from that closed campaign starts, with no "has changed since it closed" error.
  - In `tests/Dmc.Tests/Campaign/RestoreTests.cs`, beside `AfterAPassingVerifyAndNoMorePlayThereIsNoWarning` (:56), a restore without `--yes` after a passing verify, then turning every CRLF campaign file to LF, prints no `warning: ... has changed since` line.
  - In `RestoreTests.cs`, a restore where `verified.tsv` was written from LF files and the campaign now holds CRLF also prints no warning.
  - In both files, a one-word change in `story/canon.md` is still reported (`changed: story/canon.md`; `(1 files)`). This passes today.
  - In both files, a binary file (a NUL in its first 8,000 bytes) that differs only by a CR byte before an LF is still reported as changed. This passes today.
  - Use the existing fixtures in `tests/Dmc.Tests/Campaign/CampaignFixtures.cs` and `CampaignCases.cs`, and add a small helper there that rewrites a campaign's text files from CRLF to LF, or from LF to CRLF.

### Implementation for User Story 2

- [X] T008 [US2] In `src/Dmc.Cli/Campaign/CampaignFiles.cs`, beside `Hash` (:53), add three `internal static` members with XML doc comments, following contract `contracts/line-ending-compare.md`:
  - `IsText(byte[] bytes)`: true when the first 8,000 bytes hold no `0x00` byte.
  - `SameContent(byte[] a, byte[] b)`: true when the two are equal byte for byte, or both are text and are equal after every CRLF in each is turned into LF. A lone CR stays as it is.
  - `MatchesHash(byte[] bytes, string hash)`: true when any of these give `hash` through `Hash`: `bytes`; `lf`, which is `bytes` with every CRLF turned into LF (text only); and `lf` with every LF turned into CRLF (text only).
  - Work on bytes, not strings, so no encoding is assumed. Compute the extra forms only when the raw comparison fails (plan § Performance Goals).
- [X] T009 [US2] In `src/Dmc.Cli/Campaign/CampaignRestorer.cs:99-101` (`Changed`), replace `hash != CampaignFiles.Hash(f.Value)` with `!CampaignFiles.MatchesHash(f.Value, hash)`. In its doc comment, say that a file whose only change is its line endings is not counted.
- [X] T010 [US2] In `src/Dmc.Cli/Campaign/CampaignVerifier.cs:221` (`ClosedDifferences`), replace `CampaignFiles.Hash(bytes) != CampaignFiles.Hash(old)` with `!CampaignFiles.SameContent(bytes, old)`. In its doc comment, say that line endings alone are not a change. The sequel import gets this through `SequelDigest.cs:157`. Read that call to confirm it uses `ClosedDifferences` and nothing else.
- [X] T011 [US2] Search `src/Dmc.Cli/` for any other place that compares `CampaignFiles.Hash` of a campaign file on disk with a backup or a record: `grep -rn "CampaignFiles.Hash\|Manifest.Hash" src/Dmc.Cli`. Leave the comparisons of ZIP entries against a manifest as they are (contract § Where it is used). If a campaign-on-disk comparison remains, either move it to the new helper or write in the PR description why it stays.
- [X] T012 [US2] Run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Campaign"`. All pass.

**Checkpoint**: a campaign checked out from git verifies, restores and starts a sequel as it did before git.

---

## Phase 5: User Story 3 - Files DMC writes are stable between saves (Priority: P2)

**Goal**: `dmc map` writes LF on every platform (FR-003). FR-006 already holds for every kept file `dmc` writes
(research R8), so it needs no code.

**Independent Test**: the T013 test passes on Windows. Quickstart Q7: after a map is drawn on Windows, no CR byte is
in `live.html`, `live-data.js` or `live-source.txt`.

### Tests for User Story 3

- [X] T013 [US3] In `tests/Dmc.Tests/Map/RedrawTests.cs`, add a test that draws a map with the class's existing helpers (see `AChangedFileIsDrawnAgainInSilence`, :135) and asserts that `File.ReadAllBytes` of `maps/live.html`, `maps/live-data.js` and `maps/live-source.txt` holds no `0x0D` byte. It fails on Windows before T014 and passes on Linux both before and after.

### Implementation for User Story 3

- [X] T014 [US3] In `src/Dmc.Cli/Maps/MapPage.cs:38-45`, make `WriteText` write `text` as UTF-8 with no byte-order mark and with its `\n` left as they are: remove `.Replace("\n", Environment.NewLine)`. Change its doc comment from "the platform's line ending for each \"\\n\"" to "LF on every platform, so the map files are the same on Windows and Linux".
- [X] T015 [US3] Check that no test reads back a map file expecting `Environment.NewLine`. Run `grep -rn "NewLine\|\\\\r\\\\n" tests/Dmc.Tests/Map tests/Dmc.Tests/Playwright`, and update any test that does. `File.ReadAllLines` and `ReadAllText` read both endings, so those need no change. Leave `src/Dmc.Cli/Pdf/PdfExtractor.cs:303` as it is: it writes `modules/`, which is outside the repository (research R7).
- [X] T016 [US3] Run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Map"`. All pass.

**Checkpoint**: every text file `dmc` writes in a campaign is LF.

---

## Phase 6: User Story 4 - The player runs git; the guide shows how (Priority: P3)

**Goal**: a player who is not a developer can follow the user guide to keep `adventures/` in git (FR-008). DMC runs
no git (FR-011).

**Independent Test**: quickstart Q8. A reader new to git follows the section on a copy, from install to a resumed
clone in a second DMC folder.

### Implementation for User Story 4

- [X] T017 [US4] In `.docs/user-guide.md`, add a section `### Keeping your campaigns in git` under `## Your files`, after `### Back up, copy or move a campaign` (:298). Write it for a reader who has not used git, in short steps. It covers:
  - **What it gives you:** a history of every save, and a copy off the computer. Git is optional, and DMC never runs it.
  - **Setting it up:** the copy commands, one block for PowerShell and one for bash, run from the DMC folder. `templates/adventures.gitignore` goes to `adventures/.gitignore`, and `templates/adventures.gitattributes` to `adventures/.gitattributes`. Then `git init`, `git add -A` and `git commit` inside `adventures/`.
  - **What is left out and why:** migration backups, because git's history does their job. Map renders, because DMC draws them again. One computer's state, and staging folders. Closing backups are kept, because a sequel needs them. Ignored files stay on disk.
  - **Keep the repository private:** it holds spoilers (`story/dm-notes.md`, archives, NPC sheets), and a module campaign quotes the module.
  - **When to commit:** after a full save, and before and after a migration (`dmc update` asks DMC to bring a campaign up to date).
  - **The "CRLF will be replaced by LF" warning:** it is expected and harmless.
  - **Don't add campaign files to a global excludes file or `.git/info/exclude`:** DMC's searches would stop finding them (research R11).
  - **A new computer:** run `dmc install` first, then clone the repository into the new DMC folder's `adventures/`. Modules and the rules reference come back the way they came the first time. The map page is drawn again after DMC's first reply.
  - **Play from one computer:** use the repository as its copy. Playing from two computers in turn can cause merge conflicts in DMC's memory, and is not supported.
  - **After an update that changes the rules files:** copy them again.

  Give the two copy commands with real paths, and check each one runs: PowerShell on Windows, bash in WSL or Git Bash.
- [X] T018 [US4] Confirm FR-009 to FR-011 in the diff: no code in `src/` starts `git`, nothing deletes or moves a player file, and install and update are unchanged. Run `grep -rn "\"git\"" src/Dmc.Cli` (expected: nothing new), and check that `kit/dm-rules.md` and `kit/skills/**` are not in `git diff --stat origin/main`.

**Checkpoint**: the player can set it up from the guide alone.

---

## Phase 7: Polish & cross-cutting concerns

**Purpose**: the documents the plan names (§ Documents), and the quickstart

- [X] T028 [US2] Found by quickstart Q3 (research R13): the closed check and the restore's warning counted the map page files, which git leaves out, so a clone of a closed campaign failed verify and the sequel. Add `CampaignFiles.DerivedFiles` and `IsDerived` in `src/Dmc.Cli/Campaign/CampaignFiles.cs`, skip them in `CampaignVerifier.ClosedDifferences` and `CampaignRestorer.Plan`/`Changed`, with tests in `CampaignVerifyClosedTests.cs`, `RestoreTests.cs`, `SequelDigestTests.cs`, and `AdventuresGitRulesTests.TheIgnoredMapFilesAreTheDerivedFiles`.
- [X] T029 [US2] The code review of PR #234, fixes approved by the maintainer on 2026-11-17: `SequelDigest.ChainProblems` compares chain copies with `CampaignFiles.SameContent` (test `AChainCopyWithOtherLineEndingsIsNotNamed`); the gitignore leaves out `.standard.partial-*` and `.closed.partial-*`; the guide says never to commit mid-migration; `IsText`'s comment and the contract say it is only git's first test; `CampaignFiles.Differences` is the one comparison of the closed check and the restore's no-record warning; the map page names are `MapRedraw` constants; `campaign-files.md` says what the toolkit test checks. Not acted on: `.active-campaign` (the Glob tool finds it inside a repository, and the resume reads it), map pages outside the top of `maps/` (no skill draws one), and the comparison's speed (restore and verify only).
- [X] T019 [P] In `.docs/campaign-files.md`, make three changes:
  - At :58, replace "a DMC folder is not a git checkout" with wording that says `adventures/` may be a git repository the player keeps, with the rules from `templates/`, and that `dmc` still never writes `adventures/` outside the campaign commands.
  - In § Layout, add the two rules files (`adventures/.gitignore` and `adventures/.gitattributes`, the player's copies) and the two templates.
  - Re-read :322 (the Grep tool and `feedback/`) and leave it if it still holds.
- [X] T020 [P] In `.docs/dmc-reference.md`, at the restore warning (:477-482) and at the closed-campaign check (:613), add that a file whose only change is its line endings (CRLF or LF) is not counted as changed.
- [X] T021 [P] In `.docs/decisions.md`, add three entries in the file's existing format, each with its reason:
  - **Git-ready only, with one repository at `adventures/`:** spec Q1 B and Q2 A, and FR-011.
  - **LF in git, and `dmc` ignoring line endings, instead of a line-ending migration:** research R1 to R4. DMC's shell writes in play would undo a migration.
  - **Closing backups kept in git and migration backups left out:** research R5.
- [X] T022 [P] In `.docs/roadmap.md`, add or update the row for spec 018, git-friendly campaigns, with its status.
- [ ] T023 [P] In `.docs/releases.md`, in the notes for the next release, say that campaigns can be kept in git (user guide § Keeping your campaigns in git), and that a player copies the rules files again after an update that changes them.
  *Deferred to the release PR:* `releases.md` records a release only when it is cut (PR #231), and the roadmap row carries the reminder until then.
- [X] T024 [P] Read `.docs/architecture.md` for any description of the campaign comparison (`CampaignFiles`, `CampaignRestorer`, `CampaignVerifier`) or of the map writer (`MapPage.WriteText`), and update it to match T008 to T010 and T014. If nothing describes them, say so in the PR description.
- [X] T025 Run the full suite, `dotnet test tests/Dmc.Tests`, and the build, which catches missing XML doc comments on public members. Both pass with no new warning.
  *Done 2026-11-17:* 2,178 passed, 5 skipped (platform-only, as on `main`), 0 failed; Debug and Release build with 0 warnings.
- [X] T026 Run quickstart Q1 to Q7 (`specs/018-git-friendly-campaigns/quickstart.md`), on copies only, in `$CLAUDE_JOB_DIR/tmp` or `D:\Claude\test-campaign`. Never run git in `D:\Claude\dmc`.
  - On Windows, run Q2 once with `core.autocrlf=true` and once with it unset. Run Q2 on Linux too, in WSL.
  - Run Q5 and Q6 headless with `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0`.
  - Record the Q1 sizes, each result, and any Q6 failure, which goes to `feedback/` and does not block this feature, for the PR description.
  *Run 2026-11-17 on copies of `D:\Claude\dmc\adventures` in the job folder:* Q1 passes (no left-out file committed; the closing backup kept; rorns-landing alone 0.71 MB of `.git` against 4.02 MB, 17.7%; with the-silver-egg's closing backup 26%). Q2 passes with `core.autocrlf` true and false (status empty; no CRLF file in the clone). Q3 found research R13, fixed by T028; after it, verify and the sequel check pass on the clone and the restore warns as in the real folder. Q4 passes with the Grep tool (archive, session logs and digests found; a hash only in an ignored `verified.tsv` not found; ripgrep skips ZIPs inside or outside git, so the `!` exception is not observable this way). Q7 passes. Linux for Q2 runs on CI through the toolkit test only. Q5 passes (headless, installed `dmc` 0.4.3, map page files removed first as on a fresh checkout): resume and one beat with a map changed only `game-state.md`, `maps/live.json`, `story/canon.md`, the session log and a new `maps/landing-parlour.json`; the map page was drawn again and stayed out of `git status`; every file DMC wrote was LF ($1.89). Q6 passes: a full save after the beat changed six campaign files ($3.39), and a second full save straight after changed zero bytes ($3.49).
- [ ] T027 Q8 needs a person: ask the maintainer to follow the guide section, or to name a reader who will. Record the result in the PR description.
  *Not run, by the maintainer's decision of 2026-11-17:* no reader new to git will follow the guide, so SC-006 is not measured. The section was checked by running its commands on a copy (Q1, Q2).

---

## Dependencies & execution order

### Phase dependencies

- **Setup (Phase 1)**: none. Do it first, because it fixes what the spec says.
- **Foundational (Phase 2)**: empty.
- **US1 (Phase 3)** and **US2 (Phase 4)**: both P1. They are independent of each other (different files) and can be done in either order or together.
- **US3 (Phase 5)**: independent of US1 and US2.
- **US4 (Phase 6)**: the guide names the template files from US1 (T003 and T004), so write it after US1. T018 checks the whole diff, so run it last in the phase.
- **Polish (Phase 7)**: after every story. T019 to T024 can run in parallel. T025 to T027 come last, in order.

### Within each story

- The tests come first and fail (T002, T007, T013), then the code, then a filtered test run.
- US2: T008 (the helper) comes before T009 and T010 (its callers). T009 and T010 are in different files and can run in parallel once T008 is done.

### Parallel opportunities

- T003 and T004 (two new files).
- T009 and T010, after T008.
- T019 to T024 (six different documents).
- Across stories: US1, US2 and US3 touch separate files and can be built at once.

---

## Parallel example: User Story 1

```text
Task: "Create kit/templates/adventures.gitattributes with the exact content from the contract"
Task: "Create kit/templates/adventures.gitignore with the patterns from the contract"
```

## Parallel example: User Story 2

```text
# after T008 (CampaignFiles.IsText, SameContent, MatchesHash)
Task: "CampaignRestorer.Changed uses CampaignFiles.MatchesHash"
Task: "CampaignVerifier.ClosedDifferences uses CampaignFiles.SameContent"
```

---

## Implementation strategy

### MVP first (User Story 1)

1. Phase 1 (T001).
2. Phase 3 (T002 to T006): the rules files and their test.
3. **Stop and validate**: quickstart Q1 and Q2.

US1 alone makes a git history quiet. US2 is needed before a player can safely restore, verify or start a sequel from a
checkout, so both P1 stories ship together.

### Incremental order

1. US1, the rules. Quickstart Q1 and Q2.
2. US2, the line-ending comparison. Quickstart Q3 and Q4.
3. US3, the map writer writes LF. Quickstart Q7.
4. US4, the guide. Quickstart Q8.
5. Polish: the documents, the full suite and the quickstart.

All of it is one PR (one change per branch, converged before the PR), with `Closes #N` for the issue if one exists.

---

## Notes

- [P] tasks are in different files with no dependency on an unfinished task.
- No task changes `kit/dm-rules.md` or a skill (plan § Constraints), and no task adds a migration (FR-004, as amended).
- Commit when the maintainer asks, on branch `018-git-friendly-campaigns`, never on `main`.
