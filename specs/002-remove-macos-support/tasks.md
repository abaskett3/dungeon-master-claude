# Tasks: Remove macOS Support

**Input**: Design documents from `/specs/002-remove-macos-support/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/get-dmc-refusal.md](contracts/get-dmc-refusal.md), [quickstart.md](quickstart.md)

**Tests**: The spec asks for them (FR-009, SC-005): the macOS-only cases are removed and one refusal test is added. They are part of the stories below, not a separate TDD pass.

**Organization**: Grouped by user story. The four stories touch different files, so they can be done in any order or in parallel; the one real ordering is inside US3 (the scripts, then their test).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 docs, US2 release and CI, US3 refusal, US4 code and tests
- Paths are relative to the repository root. Line numbers are from `origin/main` at 787a8b5 and may drift as earlier edits land; search for the text.
- Every changed script or workflow gets its header comment updated to match (`CLAUDE.md` § Git and GitHub). Do not touch `adventures/`, `modules/` or `rules/`.

## Phase 1: Setup

**Purpose**: Start from a known-good state on the right branch. There are no blocking prerequisites beyond this.

- [X] T001 Confirm the branch is `002-remove-macos-support`, taken from the tip of `origin/main` (`git branch --show-current`, `git log --oneline origin/main..HEAD` empty). Never work on `main`.
- [X] T002 Run `dotnet build tests/Dmc.Tests -c Release` and `dotnet test tests/Dmc.Tests -c Release` once before any change and note the result, so a later failure can be told from one that was already there.

---

## Phase 2: User Story 1 - The toolkit no longer promises macOS (Priority: P1) 🎯 MVP

**Goal**: No document names macOS as supported; history is kept and marked.

**Independent Test**: `rg -n -i 'mac ?os|osx|darwin|apple|\bmac\b|gatekeeper' .docs README.md` finds only marked history (quickstart step 2).

- [X] T003 [P] [US1] In `README.md` change line 8 to list Windows (x64 or Arm) and Linux (x64 or Arm, glibc 2.34 or later) only; line 28 to say `./play.sh` is for Linux or Git Bash; line 192 to list the runtime identifiers `win-arm64`, `linux-x64` or `linux-arm64` with no `osx-arm64`.
- [X] T004 [P] [US1] In `.docs/user-guide.md` delete the macOS row of the systems table (line 16) and change the launcher row (line 27) from "macOS, Linux or Git Bash" to "Linux or Git Bash". Add one sentence under the table that macOS is not supported.
- [X] T005 [P] [US1] In `.docs/product-overview.md` line 54 drop the macOS Gatekeeper and "not built for Intel Macs" clauses; keep the point that the program is not signed (Windows SmartScreen).
- [X] T006 [P] [US1] In `.docs/architecture.md` line 20 change "Windows, Linux and macOS" to "Windows and Linux".
- [X] T007 [P] [US1] In `.docs/development.md`: line 10 drop "or the Xcode command line tools on macOS"; line 34 drop `osx-arm64` from the runtime identifiers; line 89 drop `macos-latest` (osx-arm64) from the CI matrix sentence.
- [X] T008 [P] [US1] In `.docs/releases.md`: delete the `osx-arm64` / `macos-latest` row of the Platforms table; in the paragraph below it remove "Intel Macs (`osx-x64`) are not built." and add that macOS is not built since this change (releases up to `dmc-v0.1.3` still hold an `osx-arm64` archive); in "How `get-dmc` installs" step 1 remove the Rosetta clause and replace the Intel-Mac example with the macOS refusal line from [contracts/get-dmc-refusal.md](contracts/get-dmc-refusal.md).
- [X] T009 [P] [US1] In `.docs/decisions.md`: add `(superseded 2026-10-22 by "macOS is no longer supported")` to the heading text of the "Intel Macs not built" entry and put the same marker at the start of its Decision line; in the Gatekeeper entry (line 99) mark the Mac half superseded; add a new entry "macOS is no longer supported" at the end with Date 2026-10-22, Decision (releases and CI stop building and testing `osx-arm64`; `get-dmc` refuses on macOS in one line), Reason (state the reason the user gives; the user gave none in the request, so ask them if it is not already in this conversation instead of inventing one), Limits (an existing Mac install of `dmc-v0.1.3` keeps working but gets no updates; earlier releases keep their macOS archive), Source (this spec: `specs/002-remove-macos-support/`).
- [X] T010 [P] [US1] In `.docs/roadmap.md` leave line 56 (Phase 0 spike) as history; add a row for this change under Done (what: macOS support removed; PR: fill in once opened); in the "Skipped" bullet about Gatekeeper and SmartScreen say the Mac half no longer applies.

**Checkpoint**: The quickstart step 2 search shows only marked history.

---

## Phase 3: User Story 2 - Releases and automated checks stop building for macOS (Priority: P1)

**Goal**: The next release has four archives and the per-change checks run on two systems.

**Independent Test**: Quickstart step 1 (`jq '.sha256 | keys' tools/dmc-release.json` prints four keys) and step 5 (CI shows two jobs; a release dry run builds four archives).

- [X] T011 [P] [US2] In `.github/workflows/tests.yml` delete the `os: macos-latest` / `rid: osx-arm64` matrix entry (lines 49-50) and change the header comment (line 6) from "Linux, Windows and macOS (Apple Silicon)" to "Linux and Windows".
- [X] T012 [P] [US2] In `.github/workflows/release.yml`: delete the `osx-arm64` / `macos-latest` matrix entry (lines 107-108); delete the `osx-*)` branch of the pack `case` (lines 148-151, the `COPYFILE_DISABLE` tar); remove `osx-arm64` from both rid loops (lines 273 and 332); change `(.sha256 | length) == 5` to `== 4` (line 336); change the release-notes line (255) to "built with Native AOT for Windows and Linux."; update any header comment that lists macOS.
- [X] T013 [P] [US2] In `.github/release/write-pin.sh` change the platform loop (line 44) to `win-x64 win-arm64 linux-x64 linux-arm64` and the header (line 12) to list those four. Keep the check that fails on a `.sha256` file that is not one of them.
- [X] T014 [P] [US2] In `tools/dmc-release.json` delete the `"osx-arm64"` line and the comma after the `linux-arm64` hash so the file stays valid with one key per line. Keep `tag` `dmc-v0.1.3`. Verify with `jq '.sha256 | keys' tools/dmc-release.json`.

**Checkpoint**: `jq -e '(.sha256 | length) == 4' tools/dmc-release.json` succeeds, and `ThePinIsReadableAndTheProjectsVersionIsTheDevelopmentOne` still passes (T024).

---

## Phase 4: User Story 3 - A Mac user gets a clear answer, not a broken install (Priority: P2)

**Goal**: Both setup scripts refuse on macOS with the line in [contracts/get-dmc-refusal.md](contracts/get-dmc-refusal.md), download nothing and create nothing.

**Independent Test**: Quickstart step 4 (by hand) and the new test in T017.

- [X] T015 [US3] In `tools/get-dmc.sh`: change the `case "$system"` so `Darwin)` calls `fail "dmc is not supported on macOS: it runs on Windows and Linux"` and the `*)` case says `... : it runs on Windows and Linux`; delete `Darwin) os=osx ;;`, the Rosetta block (lines 63-66), the `osx-x64` check (73-75) and the `[ "$os" = osx ]` shasum branch (110-111), keeping the generic `sha256sum`, then `shasum` fallbacks; the refusal must come before the pin is read, so before `curl`, `mktemp` or `mkdir bin`. Update the header comment (line 59 wording, "unsupported system" line 20).
- [X] T016 [US3] In `tools/get-dmc.ps1`: at the top of `Get-Platform` add `if ($IsMacOS -or $env:DMC_GET_TEST_SYSTEM -eq 'macOS') { Fail 'dmc is not supported on macOS: it runs on Windows and Linux' }`, guarding `$IsMacOS` for Windows PowerShell 5.1 where it does not exist (use `$PSVersionTable.PSEdition -eq 'Core' -and $IsMacOS`); change the non-Windows branch (line 68) to `$os = if ($IsLinux) { 'linux' } else { Fail 'dmc is not built for this system: it runs on Windows and Linux' }`; delete the `osx-x64` check (line 74). Update the header: drop "or macOS" (lines 2, 14, 16), and describe `DMC_GET_TEST_SYSTEM` beside `DMC_GET_TEST_ASSETS` and `DMC_GET_TEST_MACHINE` as "for the tests only".
- [X] T017 [US3] In `tests/Dmc.Tests/Distribution/GetDmcTests.cs`: delete the two `[DataRow("Darwin", ...)]` rows (lines 91-92) of `BashPicksTheArchiveForThisSystem`; remove `"osx-arm64"` from the `rids` array in `JunkReleases` (line 158); add a test `MacOsIsRefused` (summary comment included) that, for `bash` with the fake `uname` printing `Darwin` and `arm64`, and for `pwsh` with `DMC_GET_TEST_SYSTEM=macOS`, asserts the stderr is exactly `get-dmc: dmc is not supported on macOS: it runs on Windows and Linux`, the exit code is 1, stdout is empty and no `bin/` folder exists in the toolkit copy. Reuse `RunScript`, `AssertRefused` and `JunkReleases`. Depends on T015, T016.

**Checkpoint**: `dotnet test tests/Dmc.Tests -c Release --filter "FullyQualifiedName~GetDmcTests"` passes.

---

## Phase 5: User Story 4 - macOS-only code and test cases are removed (Priority: P3)

**Goal**: No branch, platform declaration, comment or test that exists only for macOS.

**Independent Test**: Quickstart step 1 (`rg` over `src`, `tests`, `tools`, `.github`) and a warning-free build.

- [X] T018 [P] [US4] In `src/Dmc.Cli/SupportedPlatforms.cs` delete `[assembly: SupportedOSPlatform("macos")]` and change "exactly these three" to "exactly these two". Build; if CA1416 (an error) appears, fix the call it names, do not restore `macos`.
- [X] T019 [P] [US4] In `src/Dmc.Cli/Maps/Browser.cs` make the non-Windows opener always `xdg-open` (line 22) and update the two doc comments (lines 5 and 10) to "shell execute on Windows, `xdg-open` on Linux" and "On Linux the opener runs under `sh`".
- [X] T020 [P] [US4] Comment-only edits that remove the macOS clause, keeping the sentence true: `src/Dmc.Cli/Dice/RandomBytes.cs` line 19 ("BCrypt on Windows"), `src/Dmc.Cli/Pdf/PdfFile.cs` line 17 ("Windows does not use OpenSSL"), `src/Dmc.Cli/Commands/PlayCommand.cs` line 63, `src/Dmc.Cli/Commands/ContextCheckCommand.cs` line 63, `src/Dmc.Cli/Campaign/CampaignImporter.cs` line 206, `src/Dmc.Cli/Dmc.Cli.csproj` line 32 (drop "and the Xcode command line tools on macOS").
- [X] T021 [P] [US4] In `tests/Dmc.Tests/Distribution/Bootstrap.cs` line 24 change `Rid` to `(OperatingSystem.IsWindows() ? "win" : "linux") + "-" + ...`. In `tests/Dmc.Tests/Map/BrowserTests.cs` replace both `OperatingSystem.IsMacOS() ? "open" : "xdg-open"` (lines 62 and 72) with `"xdg-open"`.
- [X] T022 [P] [US4] Comment-only edits in tests: `tests/Dmc.Tests/Campaign/CampaignImportRefusalTests.cs` line 213, `tests/Dmc.Tests/Pdf/OutputStagingTests.cs` line 65, `tests/Dmc.Tests/Pdf/PdfLibraryTests.cs` line 286, each naming only Windows and Linux.

**Checkpoint**: `dotnet build tests/Dmc.Tests -c Release` has no warnings.

---

## Phase 6: Polish and cross-cutting

**Purpose**: Prove the whole change, then hand it over under the repository's rules.

- [X] T023 Run the full [quickstart.md](quickstart.md) steps 1-4 and fix anything it finds. Step 1's `rg` must leave only the refusal in `tools/get-dmc.*`, the new test, and a comment explaining the refusal.
- [X] T024 Run `dotnet build tests/Dmc.Tests -c Release` and `dotnet test tests/Dmc.Tests -c Release`; everything passes and the build has no warnings (SC-005). Compare with T002.
- [X] T025 Re-read `.specify/memory/constitution.md` against the diff (`git diff origin/main --stat`): no `CLAUDE.md` change (II), no rule copied (III), documentation done (IV), no skipping test added (V).
- [ ] T026 Commit with the attribution lines from the session reminder, then push the branch `002-remove-macos-support`. Ask the user before pushing if they have not already said to.
- [ ] T027 Open the PR to `main` with a full description (what changed, why, sources: this spec and `research.md`, how it was tested, which documents changed), ending with the PR footer from the session reminder. Write `Closes #124`, then one `Closes #<n>` line for each of #125 to #153, each on its own line. Then run `gh pr view <n> --json body` and check the description landed.
- [ ] T028 Wait for CI: the Tests workflow, and the Release workflow (it runs as a dry run on this PR because the PR touches release.yml, tools/ and .github/release/): four archives, no macOS job, the pin check passes with `length == 4`. Then, the Tests workflow shows exactly two `dotnet (...)` jobs and all pass (SC-003). Fill the PR number into the roadmap row from T010 if it was left blank.
- [ ] T029 Run `/code-review` once on the finished PR, check the fixes for its findings yourself, and re-review only if a fix becomes a redesign. Then ask the user to merge; do not merge.

---

## Dependencies and execution order

- Phase 1 first. Phases 2-5 are independent of each other and can run in any order or in parallel (different files). Phase 6 comes last.
- Inside US3: T015 and T016 before T017.
- T014 (pin) and T012's `== 4` change go together; either one alone leaves the release workflow failing its own check. Do both before the next release.
- T023-T024 need all of US1-US4 finished. T026-T029 are strictly in order.

## Parallel example

```text
After T002, in parallel:
  US1: T003 T004 T005 T006 T007 T008 T009 T010   (documents)
  US2: T011 T012 T013 T014                        (workflows, pin)
  US4: T018 T019 T020 T021 T022                   (source and tests)
  US3: T015 and T016 together, then T017
```

## Implementation strategy

- **MVP**: US1 and US2 are both P1 and together stop the promise and the build; US3 is the user-facing safety net and should ship in the same PR, since a Mac user would otherwise get a confusing "no SHA-256 for osx-arm64" error once T014 lands. US4 is housekeeping but small, and leaving it out would fail SC-006, so it is in the same PR too.
- **One PR, one branch**, since the pieces depend on each other at release time (the pin, the workflow check and the refusal).
- **Order I would work in**: US3 (T015-T017, the only part with real logic), then US2, US4, US1, then Polish.
