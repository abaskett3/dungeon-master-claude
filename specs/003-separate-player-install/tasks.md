# Tasks: Separate the Player Install from the Toolkit Source

**Input**: Design documents from `/specs/003-separate-player-install/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md),
[contracts/](contracts/), [quickstart.md](quickstart.md)

**Tests**: Required. FR-021 asks for the toolkit tests on the generated files' source, SC-002 for proof that no player
file changes, and the constitution (V) and `.docs/development.md` for tests on every behaviour. Test tasks sit inside
each story, not in a separate TDD pass; write each test with its code and make it pass before the story's checkpoint.

**Organization**: Grouped by user story, in **delivery order**, not strict priority order. The feature ships in three
PRs under one issue, #67 (plan § Issues): PR 1 is Phases 1 to 8, PR 2 is Phase 9, PR 3 is Phase 10. US3 (P1) comes
after the P2 stories because it needs the release machinery of PR 2; US1's install line (the bootstrap) is in Phase 9
for the same reason. Every task names its PR in its phase heading.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1 to US6 from [spec.md](spec.md)
- **🧑 HUMAN (Hn)**: a step only the maintainer can do (plan § Human steps). An implementing session **stops and asks
  the user** at each one; it never skips it, does it itself, or assumes it was done. Each is also listed in #67.
- Paths are relative to the repository root. Line numbers drift; search for the text.
- Rules for every task: every new or changed type and member gets an XML doc comment (private ones too); every new or
  changed script and workflow gets its header comment; every `dmc` failure is one line `dmc <command>: <message>`;
  warnings are errors in `src/Dmc.Cli`. Never touch, move or delete anything under the repository's `adventures/`,
  `feedback/`, `modules/`, `rules/` or `exports/` (they hold the maintainer's personal files).
- **Issue linking:** PRs 1 and 2 say `Part of #67` and must not contain `Closes`, `Fixes` or `Resolves` #67 in their
  description or any commit message. Only PR 3 says `Closes #67`.

---

## Phase 1: Setup (PR 1)

**Purpose**: Start from a known-good state.

- [x] T001 Confirm the branch for PR 1 is a fresh branch from `origin/main` (not `main`; `git fetch` first), and that `specs/003-separate-player-install/` is on it (commit the spec folder first if it is not yet on `main`).
- [x] T002 Run `dotnet publish src/Dmc.Cli -r <rid> -o artifacts/bin` and `dotnet test` once before any change and note the result in the PR description draft, so a later failure can be told from one that was already there.
- [x] T003 Ask the user whether #67 (closed as not planned) should be reopened before PR 1 is opened; do what they say. Do not reopen it unasked.

---

## Phase 2: Foundational (PR 1, blocks every user story)

**Purpose**: The amended constitution, `kit/`, the embedded files, the manifest and refresh engine, and the DMC folder
lookup. Nothing in Phases 3 to 8 can start before this phase is done.

### Constitution and decision (research R13)

T004 and T005 are the first commit of PR 1, before any other change, so the rest of PR 1 is checked against
constitution 2.0.0 (user, 2026-10-24).

- [x] T004 Add a decision entry to `.docs/decisions.md`: "Players install `dmc`, which writes and updates a DMC folder" (Date 2026-10-24; Decision: the route in [plan.md](plan.md) § Summary; Reason: separate development from play, the user's direction in spec Clarifications 2026-10-24; Alternatives: a Claude Code plugin, #67's kit-per-session design, a player repository, an MCP server, an Agent SDK app, with one line each from research R1; Source: #67 and this spec). Mark the clause "players install nothing but Claude Code" in § .NET 10 Native AOT on every platform as superseded by it.
- [x] T005 Amend `.specify/memory/constitution.md`: Principle II becomes "Toolkit work MUST NOT cost the player context: the rules loaded into every play session stay lean. Players install Claude Code and `dmc`, and nothing else." with sources `.docs/development.md` § Writing prompts and the T004 entry (drop the pointer to the Spec Kit install entry); version 1.0.1 → 2.0.0; Last Amended = the PR date. Change nothing else.

### `kit/`: the source of the generated files (research R2, data-model § Kit)

- [x] T006 Create `kit/dm-rules.md` from `CLAUDE.md`: everything except the toolkit-work paragraph ("If the player is clearly asking for work on this toolkit itself…") and § Git and GitHub. Replace the toolkit-work paragraph with: "If the player asks for changes to DMC itself (its rules, skills or templates), say that DMC is developed in its own repository and offer to record it as feedback with `dmc-record-feedback`; never edit `CLAUDE.md`, `.claude/` or `templates/` here." Keep `templates/...` paths as they are (they resolve inside a DMC folder).
- [x] T007 In `kit/dm-rules.md`: change every `./bin/dmc` to `dmc`; replace the "If `bin/dmc` is missing or says it is out of date, run `bash tools/get-dmc.sh`…" sentence with "If `dmc` is missing or does not run, tell the player to run the install line from the DMC README again, or `dmc update` in a terminal, and carry on without dice and maps until then."; rename every player-skill reference to its `dmc-` name (`resume-campaign` → `dmc-resume-campaign`, `/campaign-full-save` → `/dmc-campaign-full-save`, `record-feedback`, `draw-map`, `update-game-state`, and the rest of the ten in data-model § Kit). Remove any mention of `play.ps1`, `play.sh`, `tools/` or the repository.
- [x] T008 Rewrite `CLAUDE.md` as developer instructions only: a short opening ("This repository is the source of DMC, the Dungeon Master Claude toolkit. Help as a normal assistant. The Dungeon Master rules are in `kit/dm-rules.md` and are not instructions for this session.") and § Git and GitHub unchanged. No Dungeon Master section remains.
- [x] T009 [P] Move the ten player skills with `git mv` from `.claude/skills/<name>/` to `kit/skills/dmc-<name>/` (every file in each folder, including `draw-map/map-format.md`), and set each `SKILL.md` front-matter `name:` to `dmc-<name>`. Leave `review-feedback`, `review-transcript` and `speckit-*` in `.claude/skills/`.
- [x] T010 In every file under `kit/skills/`, rename references to player skills to their `dmc-` names, change `./bin/dmc` to `dmc`, and remove references to `tools/get-dmc*`, `play.ps1` and `play.sh`. Template paths stay `templates/...`.
- [x] T011 [P] Move `templates/` to `kit/templates/` with `git mv`. In `kit/templates/feedback.md` add the front-matter key `dmc:` (value: the version number alone, e.g. `dmc: 0.2.0`, from `dmc --version` without the leading `dmc `), after `recorded:`.
- [x] T012 Create `kit/settings.json` from `.claude/play-settings.json` plus, from `.claude/settings.json`, the `allow` rules `Edit(adventures/**)`, `Edit(feedback/**)`, `Edit(modules/**)`, `Edit(rules/**)` and the PreCompact and both SessionStart hooks. Then: allow `Bash(dmc *)` and `PowerShell(dmc *)` instead of every `./bin/dmc` form; drop the `get-dmc` allow rules; drop `enabledPlugins` (the maintainer's own plugin setting, meaningless to players); drop the `speckit-*` entries of `skillOverrides` and `Skill(speckit-*)` from `deny`; add to `deny` `Edit(/CLAUDE.md)`, `Edit(/.claude/**)`, `Edit(/templates/**)`, `Edit(/.dmc/**)`, the same four for `Write`, and `Bash(dmc update*)`, `Bash(dmc install*)`, `Bash(dmc refresh*)`, `PowerShell(dmc update*)`, `PowerShell(dmc install*)`, `PowerShell(dmc refresh*)`; hooks call `dmc` bare (`dmc redraw`, `dmc context-check`, `dmc --version`), the compact message names `dmc-resume-campaign`, and the missing-`dmc` notice says "dmc is missing or does not run, so dice, maps and PDF reading will not work: reinstall DMC with the install line in its README". Campaign `allow` rules must not start with `/` (research R6).
- [x] T013 Remove `.claude/play-settings.json` (`git rm`). Reduce `.claude/settings.json` to what development needs (no campaign `allow` rules, no play hooks, no `./bin/dmc` rules).
- [x] T014 [P] Update `.github/workflows/tests.yml` path filters: replace `templates/**` with `kit/**`, keep `CLAUDE.md`, `README.md` and `.claude/skills/**`. Remove the step "Publish a release-versioned dmc with Native AOT" (`-p:Version=9.9.9` into `artifacts/release`) and `DMC_TEST_RELEASE_DIR` from the test step: only the launcher and `get-dmc` tests used them, and both go in PR 1 (T041). Reword the job comment that says bash and PowerShell are for "the launcher and get-dmc tests", and the header (dmc is published once, and `kit/` replaces `templates/`).

### The embedded files and the refresh engine (research R3, R4; contracts/dmc-refresh.md)

- [x] T015 In `src/Dmc.Cli/Dmc.Cli.csproj` embed every file under `../../kit/` as an `EmbeddedResource` with `LogicalName="kit/%(RecursiveDir)%(Filename)%(Extension)"`, including files in dot-folders; code reading the names must normalise `\` to `/`.
- [x] T016 Create `src/Dmc.Cli/Kit/GeneratedFiles.cs`: the fixed table `kit/dm-rules.md` → `CLAUDE.md`, `kit/settings.json` → `.claude/settings.json`, `kit/skills/**` → `.claude/skills/**`, `kit/templates/**` → `templates/**`; enumerates the embedded resources and their target paths and bytes; `Version` = `CommandLine.Version`; and the guard: any target under `adventures/`, `feedback/`, `modules/`, `rules/` or `exports/` throws (FR-012).
- [x] T017 [P] Create `src/Dmc.Cli/Kit/Manifest.cs`: read and write `.dmc/manifest.json` exactly as data-model § `.dmc/manifest.json` ("dmc" version, "files" with "path" using `/` and "sha256" lowercase hex), with `JsonDocument`/`Utf8JsonWriter` (no reflection); written last.
- [x] T018 Create `src/Dmc.Cli/Kit/Refresher.cs` implementing data-model § A generated file's states verbatim: "missing -> write; same as the program's copy -> leave; same as the manifest's hash -> replace; different from both -> copy to .dmc/replaced/, then replace, and say so; in the old manifest, not in the program: unchanged -> remove, edited -> move to .dmc/replaced/; exists, no manifest entry, at install -> refuse, naming it". `.dmc/replaced/<yyyyMMdd-HHmmss>/<path>`. Removes empty folders it leaves under `.claude/skills/`. Writes the manifest last. Takes the folder and the file source as parameters (test seam).
- [x] T019 Create `src/Dmc.Cli/DmcFolder.cs`: `dmc.json` beside the executable (`{"folders": [...]}`, data-model § `dmc.json`), `Record(folder)`, `IsDmcFolder(path)` (has `.dmc/manifest.json`), and `Find()`: `CLAUDE_PROJECT_DIR` if a DMC folder, else the current folder if one, else the first recorded folder that still exists, else none. Environment and paths as parameters for tests.
- [x] T020 Replace `Toolkit.Root` with `DmcFolder.Find()` in `src/Dmc.Cli/Commands/CampaignCommand.cs`, `src/Dmc.Cli/Commands/RedrawCommand.cs`, `src/Dmc.Cli/Digest/Digester.cs` and `src/Dmc.Cli/Digest/LogFolder.cs` (a command that needs a folder and finds none fails with `no DMC folder: run dmc install`); add a `--folder` option to `dmc digest`. Delete `src/Dmc.Cli/Toolkit.cs`, and with it `src/Dmc.Cli/StaleBinaryCheck.cs` (it reads the pin through `Toolkit.Root`, so PR 1 would not build otherwise; a local build reports `0.0.0-dev` and was never checked) and its call in `src/Dmc.Cli/CommandLine.cs`; update the doc comments that mention the stale-binary check (`CommandLine.cs`, `ICommand.NeverFails`, `ContextCheckCommand`, `RedrawCommand`) and remove `TheStaleBinaryWarningComparesTheVersionWithThePin` from `tests/Dmc.Tests/Distribution/PlayTests.cs`. Update the tests that set the toolkit root (`tests/Dmc.Tests/Campaign/*`, `tests/Dmc.Tests/Map/RedrawTests.cs`, `tests/Dmc.Tests/Digest/*`) to pass a temporary DMC folder.
- [x] T021 In `src/Dmc.Cli/Digest/Session.cs` make `SaveSkill` and `ResumeSkill` match both the old and the new names (`campaign-full-save` and `dmc-campaign-full-save`; `resume-campaign` and `dmc-resume-campaign`), so old transcripts still digest; update `src/Dmc.Cli/ContextCheck/TranscriptReader.cs` the same way and its doc comment; add cases for the new names in `tests/Dmc.Tests/Digest/DigestLibraryTests.cs` and `tests/Dmc.Tests/ContextCheck/ContextCheckTests.cs`.
- [x] T022 [P] Update the map-format path in `src/Dmc.Cli/Commands/MapCommand.cs` (doc comment and help text) to `.claude/skills/dmc-draw-map/map-format.md`, and `tests/Dmc.Tests/Map/MapFormatDocTests.cs` to read `kit/skills/dmc-draw-map/map-format.md`.

### Foundational tests

- [x] T023 Retarget `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` to `kit/`: the rules are `kit/dm-rules.md`, the skills `kit/skills/`, the templates `kit/templates/`, the play settings `kit/settings.json`. Every existing check keeps its meaning (commands real and allowed for Bash and PowerShell, paths exist, skill names real, hooks well formed and silent, the Stop hook, shell-delete denies, feedback template keys, resume and full save never mention `feedback/`, only `review-feedback` mentions `gh issue`/`gh pr`). Remove checks about `play.ps1`, `play.sh`, `get-dmc` and `bin/`.
- [x] T024 Add to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`: every file under `kit/` is an embedded resource and in the table; no table target is under a player folder; every player skill folder and `name:` is `dmc-<name>` and every skill a kit prompt names exists; every `dmc <subcommand>` a kit prompt names is a real subcommand, except `update`, accepted as known until PR 2 adds it (T056 removes that exception); `install`, `update` and `refresh` appear only as commands the player runs in a terminal, never among the commands DMC runs; no kit file contains `./bin/dmc`, `get-dmc`, `play.ps1`, `play.sh` or `tools/`; `kit/settings.json` has the generated-file deny rules and the `dmc update/install/refresh` denies; campaign allow rules do not start with `/`; `CLAUDE.md` has no Dungeon Master section (no "## Dice", "## Sizing fights", "## Maps"); `.claude/skills/` holds no player skill; the feedback template has `dmc:`.
- [x] T025 [P] Create `tests/Dmc.Tests/Kit/GeneratedFilesTests.cs` (table, version, guard throws for each player folder) and `tests/Dmc.Tests/Kit/ManifestTests.cs` (round trip, `/` paths, lowercase hex).
- [x] T026 Create `tests/Dmc.Tests/Kit/RefreshTests.cs`: each state of T018, removal of a dropped file, the hand-edit copy under `.dmc/replaced/`, the refusal at install, an interrupted refresh repaired by the next, and a SHA-256 of every file under the player folders and of a player-added `.claude/settings.local.json` and `.claude/skills/my-skill/SKILL.md` before and after (identical).

**Checkpoint**: `dotnet test` passes; `kit/` exists and is embedded; the repository's `CLAUDE.md` is developer-only.

---

## Phase 3: User Story 1 - A new player installs DMC and plays (P1, PR 1 part) 🎯 MVP

**Goal**: `dmc install` writes a DMC folder and installs the program; `dmc play` plays there. (The one-line bootstrap
is in Phase 9.)

**Independent Test**: quickstart §1 steps 2 to 5, with `artifacts/bin/dmc install --force` in place of the install line.

- [x] T027 [US1] Create `src/Dmc.Cli/Install/ProgramInstall.cs`: the program folder (`%LOCALAPPDATA%\Programs\dmc\` on Windows; `~/.local/share/dmc/` with link `~/.local/bin/dmc` on Linux); copy the running `dmc` and its three native libraries there; add the folder to the user `Path` on Windows (`HKCU\Environment`, then broadcast `WM_SETTINGCHANGE` with a `LibraryImport` of `SendMessageTimeout`); on Linux create the link and report whether `~/.local/bin` is on PATH; remove leftovers (`*.old`, `.update-*`) quietly. Home and environment as parameters for tests.
- [x] T028 [US1] Create `src/Dmc.Cli/Commands/InstallCommand.cs` per [contracts/dmc-install.md](contracts/dmc-install.md): `--path`, `--force`; the prompt `DMC folder [<home>/dmc]: ` (Enter = default; `~` = home; on Linux read `/dev/tty` when stdin is not a terminal; default when no terminal); refuse a folder in or under a checkout (a `kit/dm-rules.md` in it or above it); a development build copies the program only with `--force`; write the files with `Refresher` (refuse a foreign file in the way; a folder with a manifest behaves as refresh); create `adventures/`, `feedback/`, `modules/`, `rules/` if missing and write nothing in them; record the folder; the output lines and errors exactly as the contract. Refuse first, before anything else, when `CLAUDECODE` is set (environment as a parameter; research R6, FR-016).
- [x] T029 [P] [US1] Create `src/Dmc.Cli/Commands/FolderCommand.cs` per [contracts/dmc-refresh.md](contracts/dmc-refresh.md) § `dmc folder`.
- [x] T030 [US1] Rewrite `src/Dmc.Cli/Commands/PlayCommand.cs` per [contracts/dmc-play.md](contracts/dmc-play.md): leftovers; the DMC folder (current if one, else recorded); refresh if the manifest's version differs or a file is missing or changed; `FindClaude` as today (drop the `./play.ps1` suggestion from its message); start in the DMC folder with `PATH` = program folder + separator + inherited `PATH`; `FixedArguments` = `--model sonnet --effort high` (no `--settings`); `Summary` updated; doc comments updated.
- [x] T031 [US1] Register `install`, `folder` and `refresh` in `src/Dmc.Cli/CommandTable.cs` with one-line summaries.
- [x] T032 [P] [US1] Create `tests/Dmc.Tests/Distribution/InstallTests.cs`: default folder; `--path`; `~` expansion; no-terminal default; refusal inside the repository; refusal on a foreign file; player folders created empty; a second install over the folder behaves as refresh; a development build does not copy the program without `--force`; refused with `CLAUDECODE=1` (and `dmc refresh` too), nothing written; output lines match the contract.
- [x] T033 [P] [US1] Rewrite `tests/Dmc.Tests/Distribution/PlayTests.cs`: folder resolution, refresh before start (a hand-changed `CLAUDE.md` is put back and the changed copy kept in `.dmc/replaced/`, FR-005), the PATH prepend, fixed arguments, arguments passed through unchanged and last, no DMC folder refused, Claude Code missing (127).
- [x] T034 [P] [US1] Create `tests/Dmc.Tests/Distribution/FolderTests.cs`.
- [ ] T035 🧑 HUMAN (H7, part 1) [US1] After T027 to T034 pass: ask the user to run `artifacts/bin/dmc install --force` (default folder), open a new terminal, run `dmc play`, type `/dmc`, play one scene and go through this checklist (SC-008), one quick try each: (1) the start-up hooks: no missing-`dmc` notice at start, and no hook error; (2) a roll from Bash and from PowerShell; (3) a map, and the Stop hook (`dmc redraw`) redrawing it after a move; (4) `/dmc-update-game-state` and a save with `/dmc-campaign-full-save`; (5) `/compact`, the PreCompact hook, and the resume after it; (6) `/clear` then `/dmc-resume-campaign` in the same folder; (7) `/dmc-record-feedback`: the entry is in `feedback/` with `dmc:`; (8) `/dmc-rewind-campaign` on a scratch campaign; (9) `/dmc-ingest-module` on a short PDF; (10) `/dmc-setup-rules`; then run `/context`, quit and start plain `claude` in the folder; and to report what worked, the `/context` size against a session from the repository today (SC-005), and anything odd. Fix what they report before the checkpoint.

**Checkpoint**: quickstart §1 steps 2 to 5 pass from a local build.

---

## Phase 4: User Story 2 - The player's files survive (P1, PR 1)

**Goal**: install and refresh never touch player files (update is covered in Phase 9).

**Independent Test**: quickstart §2 steps 1, 2 (install and refresh only), 3 and 4.

- [x] T036 [US2] Create `src/Dmc.Cli/Commands/RefreshCommand.cs` per [contracts/dmc-refresh.md](contracts/dmc-refresh.md): `[folder | --all]`, output at most two lines per folder, errors as the contract; refuse first when `CLAUDECODE` is set (research R6, FR-016).
- [x] T037 [P] [US2] Create `tests/Dmc.Tests/Distribution/RefreshCommandTests.cs` through the published binary: a filled DMC folder (campaigns, both player-options files, feedback entries, a module, a rules file, `exports/`), hashes before and after `dmc install --path` again and `dmc refresh`, identical, none added or removed (SC-002).

**Checkpoint**: quickstart §2 passes for install and refresh.

---

## Phase 5: User Story 4 - Feedback stays with the player; developers tune the files (P2, PR 1)

**Goal**: entries record the version; play cannot edit generated files; the review skills read a DMC folder.

**Independent Test**: quickstart §4.

- [x] T038 [US4] In `kit/skills/dmc-record-feedback/SKILL.md` fill the new `dmc:` key with the version number alone (`dmc: 0.2.0`: `dmc --version` without the leading `dmc `) when recording an entry.
- [x] T039 [P] [US4] In `.claude/skills/review-feedback/SKILL.md` and `.claude/skills/review-transcript/SKILL.md`: take a DMC folder path as an argument, defaulting to the output of `dmc folder`; read `feedback/` and `adventures/` there; the files to tune are in `kit/` (rename references: `CLAUDE.md` → `kit/dm-rules.md`, `.claude/skills/<name>` → `kit/skills/dmc-<name>`, `templates/` → `kit/templates/`, play settings → `kit/settings.json`); `./bin/dmc` → `dmc`; `dmc digest` gets `--folder`.
- [x] T040 [US4] Add to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`: the review skills name `dmc folder` and no `./bin/dmc`; the record-feedback skill writes the `dmc:` key.

**Checkpoint**: quickstart §4 passes (step 2 with refresh in place of update).

---

## Phase 6: User Story 5 - A developer tries a change from source (P2, PR 1)

**Goal**: development and play are apart; no launcher remains.

**Independent Test**: quickstart §5.

- [ ] T041 [US5] Remove `play.ps1` and `play.sh` (`git rm`), `tests/Dmc.Tests/Distribution/LauncherTests.cs` and `tests/Dmc.Tests/Distribution/GetDmcTests.cs`, and what `tests/Dmc.Tests/Distribution/Bootstrap.cs` only kept for them (`DMC_TEST_RELEASE_DIR`, the release-versioned build, the pin it writes). Once the launchers and the `get-dmc` line in the rules are gone, nothing a player runs calls `get-dmc`, so its tests would only cost CI time (user, 2026-10-24). The scripts, the pin and `write-pin.sh` stay until T061, because the release workflow's archive check still runs them on a tag and is rewritten in PR 2; remove launcher checks from the toolkit tests; update `tests/Dmc.Tests/Support/DmcRunner.cs`'s comment (no `get-dmc` owning `bin/`).
- [ ] T042 [P] [US5] Update `.gitignore`: the `bin/` comment no longer mentions `get-dmc` (keep ignoring `bin/` and `artifacts/`).
- [ ] T043 [US5] In `.docs/development.md`: how to try a change (edit `kit/`, publish, `artifacts/bin/dmc install --path <scratch>` or `refresh <scratch>`, play there in a new session; `--force` installs the build as the program); the repository layout table (`kit/`, the developer `CLAUDE.md`, `.claude/skills/` now developer-only); remove `play.ps1`/`bin/` text that no longer holds (the `get-dmc` and pin text goes in Phase 9); the Spec Kit paragraph no longer needs the play block (the Spec Kit skills never reach a DMC folder) and points at the T004 entry.

**Checkpoint**: quickstart §5 steps 1 to 4 pass.

---

## Phase 7: User Story 6 - The maintainer moves their campaigns (P2, PR 1)

**Goal**: documented move commands; DMC never runs them.

**Independent Test**: quickstart §6.

- [ ] T044 [US6] In `.docs/user-guide.md` add "Moving campaigns from a repository checkout": back up first; `dmc install` (or `--path`); then the PowerShell and bash commands that move `adventures/*`, `feedback/*` (except `.gitkeep`, `.ignore`), `modules/*` and `rules/*` from the checkout into the DMC folder; old session logs stay under the checkout's Claude Code project folder and `dmc digest --logs <folder>` reads them.
- [ ] T045 🧑 HUMAN (H6) [US6] Ask the user to back up and then run the T044 commands themselves on their real campaigns (DMC never runs them), resume the active campaign with `dmc play`, and confirm the recap matches.

**Checkpoint**: the maintainer plays from a DMC folder.

---

## Phase 8: PR 1 documentation and delivery

- [ ] T046 [P] Update `.docs/architecture.md`, `.docs/campaign-files.md`, `.docs/dmc-reference.md` (new `install`, `refresh`, `folder`; the new `play`; `digest --folder`), `.docs/product-overview.md` and `.docs/roadmap.md` (status of #67 Phase 1) for everything in Phases 2 to 7. Point player-facing text at `dmc install`; the install line itself comes in Phase 9.
- [x] T047 [P] Update `README.md` for the interim state: playing needs `dmc install` from a local build until the first release of this change; list the `dmc-` skills; remove `play.ps1`/`play.sh` instructions.
- [ ] T048 Run `dotnet publish src/Dmc.Cli -r <rid> -o artifacts/bin` and `dotnet test`; all pass.
- [ ] T049 Open PR 1 with the full description `CLAUDE.md` § Git and GitHub requires (what, why, sources, testing including T035, documents changed) and the line `Part of #67`. Then `gh pr view <n> --json body` and check the body landed and contains no `Closes`, `Fixes` or `Resolves` #67; check `git log origin/main..HEAD` messages too.
- [ ] T050 Run Claude Code's code-review command once on PR 1; fix its findings and check the fixes yourself; check the documentation is current; CI passes.
- [ ] T051 🧑 HUMAN (H8) Ask the user to merge PR 1.

---

## Phase 9: User Story 3 - Released versions only, updated in place; and the install line (P1, PR 2)

**Goal**: `dmc update` with signature and hash checks; the bootstrap; the hardened release; the old pin machinery gone.

**Independent Test**: quickstart §3, §1 step 1, and §2 step 2's update.

- [ ] T052 Start PR 2 on a fresh branch from `origin/main` after PR 1 has merged.
- [ ] T053 🧑 HUMAN (H1) [US3] Ask the user to make two ECDSA P-256 key pairs (active and backup) with the commands T060 writes into `.docs/releases.md` (`openssl ecparam -name prime256v1 -genkey -noout -out <name>.pem` and `openssl ec -in <name>.pem -pubout -out <name>.pub.pem`), keep the backup private key offline, and paste the two **public** keys. Never ask for or accept a private key.
- [ ] T054 [US3] Create `src/Dmc.Cli/Install/ReleaseKeys.cs` with the two public keys from T053 (PEM `SubjectPublicKeyInfo`), and `Verify(byte[] data, byte[] derSignature)` = true if either key verifies with `ECDsa.VerifyData(data, sig, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)`. A constructor taking other keys is `internal`, for tests only; nothing reads keys from the environment or a file.
- [ ] T055 [US3] Create `src/Dmc.Cli/Install/ReleaseSource.cs`: the latest release from `https://api.github.com/repos/abaskett3/dungeon-master-claude/releases/latest` (or `DMC_UPDATE_TEST_ASSETS`, whose `latest.json` holds the tag and asset digests), read with `JsonDocument`; download with a stall timeout; `SHA256SUMS.txt` and `SHA256SUMS.txt.sig` verified with `ReleaseKeys`; the archive for this RID checked against the signed list and the API's `digest` (`sha256:<hex>`); unpack (`ZipFile`, or `TarFile` with `GZipStream`) into `<program folder>/.update-<random>/`. No setting skips any check (FR-028).
- [ ] T056 [US3] Create `src/Dmc.Cli/Commands/UpdateCommand.cs` per [contracts/dmc-update.md](contracts/dmc-update.md): refuse first when `CLAUDECODE` is set (research R6, FR-016); refuse a development build; stop if not newer (still refresh stale folders); `ReleaseSource`; run the new `dmc --version`; swap with `*.old` renames and move back on failure; run the new `dmc refresh --all`; output and every error line exactly as the contract. Register it in `src/Dmc.Cli/CommandTable.cs`, and remove the `update` exception from the T024 subcommand check in `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`.
- [ ] T057 [P] [US3] Create `tests/Dmc.Tests/Distribution/UpdateTests.cs`: in-process with a test key (newer installs and refreshes; same says latest; older refused; development build refused; refused with `CLAUDECODE=1`; hash mismatch against the list and against the digest; missing signature; bad signature; unknown key; either built-in key accepted; a new `dmc` that does not run; a failure mid-swap moves back; player-file hashes identical); and through the binary with `DMC_UPDATE_TEST_ASSETS` for the refusals. Create `tests/Dmc.Tests/Distribution/ReleaseKeysTests.cs` (both keys parse; neither equals the test key).
- [ ] T058 [US3] Create `tools/install.ps1` and `tools/install.sh` per [contracts/bootstrap.md](contracts/bootstrap.md), reusing machine detection, the stalled-download timeout, the hash check and the macOS refusal from `tools/get-dmc.ps1` and `tools/get-dmc.sh`; check the archive against `SHA256SUMS.txt` and the API digest (on the API's rate-limit refusal only, `SHA256SUMS.txt` alone with the one-line notice in the contract); run the downloaded `dmc install` attached to the terminal (pass `--path`); header comments with the `DMC_INSTALL_TEST_*` variables. Keep `install.sh` LF and executable.
- [ ] T059 [P] [US3] Create `tests/Dmc.Tests/Distribution/BootstrapTests.cs` in the style of the removed `GetDmcTests.cs` (`git show origin/main~N:tests/Dmc.Tests/Distribution/GetDmcTests.cs`, or the commit before T041) (local assets, a corrupted archive refused, the rate-limit fallback and its notice, macOS refused, `--path` passed through); skip locally and fail on CI when PowerShell or bash is missing. If these tests need a build that reports a real version, add a release-versioned publish step back to `.github/workflows/tests.yml` with a comment naming them; otherwise add none.
- [ ] T060 [US3] Rewrite `.github/workflows/release.yml`: the publish job runs in environment `release`; create the release as a draft, sign `SHA256SUMS.txt` with `openssl dgst -sha256 -sign` using secret `DMC_RELEASE_SIGNING_KEY` (dry runs make a throwaway key in the run), attach the archives, `SHA256SUMS.txt`, `SHA256SUMS.txt.sig`, `tools/install.ps1` and `tools/install.sh`, then publish; pin every third-party action to a commit SHA; the dry run installs each archive with the bootstrap from local assets into a temporary DMC folder (`dmc install --path`), checks the files and the manifest, then rolls, draws a map and renders a PDF page as today; remove the pin job and the pin pull request; update the header comment. In `.docs/releases.md` document the keys (the T053 commands), the environment, immutable releases, rulesets, and key recovery (sign the next release by hand with the backup key and ship a new key pair).
- [ ] T061 [US3] Remove `tools/get-dmc.ps1`, `tools/get-dmc.sh`, `tools/dmc-release.json`, and `.github/release/write-pin.sh` (`StaleBinaryCheck` went in T020, `GetDmcTests.cs` in T041); update the `release.yml` trigger paths (`tools/**` now means the install scripts).
- [ ] T062 [P] [US3] Update `README.md` with the install lines from [contracts/bootstrap.md](contracts/bootstrap.md) and `dmc update`; `.docs/dmc-reference.md` (`update`); `.docs/user-guide.md` (installing and updating); `.docs/development.md` (no `get-dmc` or pin); `.docs/decisions.md`: the trust decision (research R16, options 3 and 4, why not 2, 5, 6, 7) and the superseded entries in research R12; `.docs/roadmap.md`.
- [ ] T063 🧑 HUMAN (H2) [US3] Ask the user to create the GitHub environment `release` with themselves as required reviewer and add the active private key as its secret `DMC_RELEASE_SIGNING_KEY`, and to confirm when done. Before PR 2 merges.
- [ ] T064 Run `dotnet publish` and `dotnet test`; all pass. Open PR 2 with the full description and `Part of #67`; check the body with `gh pr view <n> --json body` and that no `Closes`/`Fixes`/`Resolves` #67 appears in it or in any commit message.
- [ ] T065 Run the code-review command once on PR 2; fix and check; documentation current; CI and the release dry run pass.
- [ ] T066 🧑 HUMAN (H8) Ask the user to merge PR 2.
- [ ] T067 🧑 HUMAN (H3) [US3] Ask the user to turn on immutable releases in the repository settings.
- [ ] T068 🧑 HUMAN (H4) [US3] Ask the user to add rulesets protecting `main` and the `dmc-v*` tags, and to confirm two-factor sign-in.
- [ ] T069 🧑 HUMAN (H5) [US3] Ask the user to push the tag `dmc-v0.2.0` on `main` and approve the `release` environment; then check the release has the four archives, `SHA256SUMS.txt`, `.sig`, `install.ps1`, `install.sh`, and is marked immutable.

**Checkpoint**: quickstart §3 passes against the published release.

---

## Phase 10: Switch-over (PR 3, closes #67)

- [ ] T070 Start PR 3 on a fresh branch from `origin/main`.
- [ ] T071 Remove the repository's placeholder player folders: `git rm` only `adventures/.gitkeep`, `feedback/.gitkeep`, `feedback/.ignore`, `modules/.gitkeep`, `rules/.gitkeep` (never any other file there; the maintainer's files are git-ignored and must stay untouched). Keep the `.gitignore` entries, dropping the `!…/.gitkeep` and `!feedback/.ignore` lines. Remove the toolkit tests about these placeholders. Mark `.docs/decisions.md` § feedback/ has a .ignore file superseded.
- [ ] T072 [P] Finish `README.md` for players (install Claude Code, the install line, `dmc play`, `dmc update`, where the files are); move any remaining developer setup to `.docs/development.md`. Final pass over `.docs/` for stale references: `rg -n "play\.ps1|play\.sh|get-dmc|dmc-release|bin/dmc|Toolkit\.Root|/resume-campaign|play-settings" README.md .docs kit .claude CLAUDE.md` finds only marked history.
- [ ] T073 🧑 HUMAN (H7, part 2) Ask the user to install from the published release with the install line on a clean folder, play one scene there with the T035 checklist, then one scene in their migrated DMC folder after `dmc update` (or refresh) with the same checklist (rewind and ingest on scratch data only), and report (SC-008, SC-009). Before that, quickstart §3 step 1: change a line of `kit/dm-rules.md` on a branch, push it, and check the installed DMC folder's `CLAUDE.md` still has the released text (SC-006).
- [ ] T074 Run `dotnet test`; open PR 3 with the full description and `Closes #67`; check the body with `gh pr view <n> --json body`.
- [ ] T075 Run the code-review command once on PR 3; fix and check; documentation current; CI passes.
- [ ] T076 🧑 HUMAN (H8) Ask the user to merge PR 3 (this closes #67).

---

## Phase 11: Convergence

- [ ] T077 After each PR merges, run `/speckit-converge` and keep `spec.md`, `plan.md`, `research.md`, `contracts/` and this file in line with what shipped; the spec is frozen once a pass adds no tasks after PR 3 has merged (`.docs/development.md` § Spec-driven changes).

---

## Dependencies and execution order

- **Phase 1 → Phase 2 → Phases 3 to 7 → Phase 8** (PR 1). Within Phase 2: T004 → T005; T006 → T007 → T008; T009 → T010; T011 and T012 independent; T015 → T016 → T018; T017 alongside; T019 → T020; T023/T024 after T006 to T013 and T016.
- **Phases 3 to 7** depend only on Phase 2, except: T030 (play) uses T018 and T019; T036 can reuse T028's wiring; T045 (H6) needs T028 and T044; T035 (H7) needs Phase 3 done.
- **Phase 9** (PR 2) needs PR 1 merged (T051). T053 (H1) blocks T054; T063 (H2) blocks merging PR 2; T067 and T068 (H3, H4) block T069; T069 (H5) is the release.
- **Phase 10** (PR 3) needs the release (T069). T073 (H7) blocks T074's merge.
- **Phase 11** runs after each merge.

## Parallel examples

- Phase 2: T009 (skills move), T011 (templates move), T014 (CI filters) and T017 (manifest) touch different files.
- Phase 3: T029, T032, T033 and T034 in parallel once T027, T028 and T030 exist.
- Phase 9: T057 and T059 in parallel once T054 to T056 and T058 exist.

## Implementation strategy

- **MVP = PR 1 (Phases 1 to 8):** the maintainer installs from a local build, moves their campaigns and plays from a DMC
  folder. Development and play are already apart.
- **PR 2** adds safe updates and the public install line, then the first release of this change.
- **PR 3** removes the last placeholders and proves it from the published release.
- Stop at every 🧑 HUMAN task and ask; record the user's answer in the PR description.
