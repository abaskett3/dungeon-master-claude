# Development

This document is for contributors changing `dmc`, the kit (the Dungeon Master rules, the player skills, the templates and the play settings), the developer instructions in `CLAUDE.md` or the review skills. Playing needs none of it: players install `dmc`, which writes a DMC folder (see [architecture.md](architecture.md)). The binding rules for toolkit work are in [CLAUDE.md](../CLAUDE.md) § Git and GitHub; this document explains the setup and the conventions around them.

## Requirements

- The [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`winget install Microsoft.DotNet.SDK.10` on Windows). `global.json` asks for 10.0.300 or a later feature band.
- Git, and for the tests that use them, bash (Git Bash on Windows) and PowerShell. Tests for a tool that is missing are skipped locally.
- For the map page tests, Playwright's Chromium (below).
- Nothing else for the default build. Native AOT builds also need a C++ toolchain: Visual Studio's "Desktop development with C++" workload on Windows, `clang` and `zlib1g-dev` on Linux.

## Repository layout

| Path | Holds |
|---|---|
| `kit/` | The source of everything `dmc` writes into a DMC folder, embedded in `dmc`: `dm-rules.md` (becomes `CLAUDE.md`), `settings.json` (becomes `.claude/settings.json`), `skills/dmc-*/` (become `.claude/skills/dmc-*/`), `templates/` and `migrations/` (the numbered files DMC follows to bring an older campaign's files up to date) |
| `CLAUDE.md` | Instructions for working on the toolkit: the git rules. The Dungeon Master rules are in `kit/dm-rules.md` and are not loaded as instructions in a session here |
| `.claude/skills/`, `.claude/settings.json` | Developer-only: the `review-feedback` and `review-transcript` skills (they take a DMC folder), Spec Kit's `speckit-*` skills, and the permissions development needs |
| `tools/` | `install.ps1` and `install.sh`: the install line, attached to every release (tested by `Distribution/BootstrapTests.cs`) |
| `src/Dmc.Cli/` | `dmc`'s source (C#, .NET 10) |
| `src/Dmc.SlipTest/` | `slip-test`, the replay runner that measures slips (spec 024): developer tooling, built and tested with the solution, never released or run in CI (see [The slip test](#the-slip-test)) |
| `tests/Dmc.Tests/` | The tests (MSTest and Playwright) |
| `.github/workflows/` | `tests.yml` (CI) and `release.yml` (releases); `.github/release/sign.sh` signs a release's checksum list (see [releases.md](releases.md)) |
| `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `dmc.sln` | Shared build settings, centrally pinned package versions, the SDK pin, the solution |
| `.docs/` | These documents |
| `.claude/skills/speckit-*`, `.specify/`, `specs/` | Spec Kit: the planning skills, its templates and state, and the specs it writes; toolkit work only, never play (see [Spec-driven changes](#spec-driven-changes)) |
| `adventures/`, `feedback/`, `modules/`, `rules/`, `exports/`, `bin/`, `artifacts/` | Personal, copyrighted or built files; all ignored by git. The repository is not a place to play: the maintainer's own campaigns move to a DMC folder (see [user-guide.md](user-guide.md#moving-campaigns-from-a-repository-checkout)) |

## Building

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
```

Use your system's runtime identifier in place of `win-x64`: `win-arm64`, `linux-x64` or `linux-arm64`.

- **The default build** is a self-contained single-file program without AOT. It needs no C++ toolchain and no flags. It starts in about 160 ms on Windows.
- **The release build** adds `-p:PublishAot=true`: Native AOT, which starts in under 30 ms. CI and the release workflow build this way, and any test that measures time must use it.
- Plain `dotnet build` and `dotnet test` build an ordinary framework-dependent assembly; neither publishes `dmc`.
- The AOT, trim and single-file analyzers run on every build, and warnings are errors, so a local build already shows what a release build would complain about.

**Versions.** Every build that is not a release says `dmc 0.0.0-dev`. That version is set in `src/Dmc.Cli/Dmc.Cli.csproj`, and no release ever has it. A release gets its version from its tag (`-p:Version=X.Y.Z`); nothing writes a release version back into the project file.

**Your build and the installed `dmc` are separate.** Publish your own builds to `artifacts/bin/`. `dmc install` copies the running program as the player's `dmc` only for a release build; a development build does it only with `--force`, so trying a change does not replace the `dmc` you play with. A development build records the DMC folders it makes in `dmc.json` beside itself (in `artifacts/bin/`), not in the installed program's folder.

**Trying a change to the rules, a skill, a template or the settings.** Edit the file under `kit/`, publish, and write a scratch DMC folder from the build, then play there in a new session:

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
artifacts/bin/dmc install --path <scratch folder>      (or: artifacts/bin/dmc refresh <scratch folder>, on one made before)
cd <scratch folder>; dmc play      (or: claude --model sonnet, with dmc on the PATH)
```

`kit/` is embedded in the program, so an edit needs the publish before the install or refresh sees it. `--force` additionally installs the build as your `dmc`. Run `install` and `refresh` in a terminal: they refuse inside a Claude Code session (`CLAUDECODE` is set), so a session here cannot change a DMC folder's rules. The `--path` folder must be outside any checkout of this repository: `dmc install` refuses a folder with a `kit/dm-rules.md` in or above it.

## Tests

Publish first, because some tests run the program itself, then test:

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
dotnet test
```

For the map page tests, install Chromium once after a build:

```
pwsh tests/Dmc.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
```

The tests never touch `adventures/`.

### Layers

| Layer | Where | What it checks |
|---|---|---|
| Library tests | `*LibraryTests.cs`, `RollTests.cs`, `ContextCheckTests.cs`, `MapPngTests.cs`, `PythonCompatTests.cs` | The code under each command, called in-process: dice arithmetic, map checking and drawing, distances, PDF text, digest statistics, the break reminder |
| Contract tests | `*Contract.cs` | Each command's arguments, output and exit codes. Every contract runs twice: in-process against `CommandLine.Run`, and black-box against the published binary in a subprocess, the way DMC calls it |
| Case lists | `MapCases.cs`, `PdfCases.cs`, `DigestCases.cs` | Many inputs per command, run through the published binary; inputs that should work must exit 0 with empty stderr and write the expected files |
| Error-path guard | `TokenCost/ErrorPathTests.cs` | Every error path of every command prints exactly one line, no stack trace, a non-zero exit code, and writes nothing. `context-check` prints nothing and exits 0 for every bad input |
| Output bytes | `TokenCost/OutputBytesTests.cs` | No byte-order mark, LF line ends, non-ASCII output as expected |
| Kit | `Kit/` | The table from `kit/` to a DMC folder and its guard (nothing under a player folder), the manifest (round trip, `/` paths, lowercase hex), and every state of a generated file in a refresh: missing, current, older, edited by hand (kept in `.dmc/replaced/`), dropped, in the way of a first install, an interrupted refresh, and a hash of every player file before and after |
| Distribution | `Distribution/` | `dmc install` (the folder asked for and taken, `--path`, `~`, no terminal, refusals, the player folders made empty, a second install as a refresh, a development build without `--force`, the Windows `Path` through a fake, refusal inside a Claude Code session), `dmc refresh`, `dmc folder` and `DmcFolder.Find`, and `dmc play` (the folder, the refresh before the start, the `PATH`, the fixed arguments), in-process over temporary folders and fakes and through the published binary; SC-002 as a hash of a filled folder before and after; `dmc update` (`UpdateTests`: a throwaway signing key made in each test and given to the command through its internal seams, a local release folder or a fake HTTP handler for GitHub, a stand-in program whose content says what it prints: a newer release installed and refreshed, the same or an older one left, a development build and a Claude Code session refused, a hash that does not match the signed list or GitHub's digest, a missing, bad or unknown-key signature, either key of a set accepted, a new program that does not run, a failure in the middle of the swap moved back, the player's files identical; the published binary is a development build, so through it only the refusals that come before a release is read are tested, and the release workflow's dry run runs a release build's refusal of a throwaway-signed release), `ReleaseKeysTests` (both built-in keys parse and differ from a test key, a DER signature by either key of a set verifies and nothing else does, a signature made by `openssl dgst -sha256 -sign` verifies) and the install line (`BootstrapTests`: `install.sh` and `install.ps1` from local assets under every shell, with the home and temporary folders replaced, the checksum list and GitHub's digest, the API's rate limit, macOS and the processor types refused, and `install.ps1` not ending the session it is run in) |
| Toolkit | `Toolkit/ToolkitTests.cs` | `kit/` and the prompts: every file under `kit/` is embedded and in the table, none lands under a player folder, every player skill is `dmc-<name>` and every skill a kit prompt names exists; no kit file names `./bin/dmc`, `get-dmc`, `play.ps1`, `play.sh` or `tools/`; every `dmc` command in the prompts is a real subcommand, and the ones DMC runs match an allow rule for Bash and PowerShell (`install`, `update` and `refresh` are for the player and denied to DMC); paths under `templates/` and `.claude/skills/` resolve in `kit/`; every `/skill-name` is a real skill and the README lists them all; `dmc-resume-campaign` § 2 still holds every marker `dmc size` reads, no kit prompt states a size in lines or words or counts canon's lines, the resume and full save run `dmc size <campaign> --check`, and the budget table in `campaign-files.md` equals `Budgets`; the kit settings deny edits of the generated files (`Edit` on `/CLAUDE.md`, `/.claude/**`, `/templates/**`, `/migrations/**`, `/.dmc/**`, which Claude Code applies to `Write` too; a `Write(...)` rule is never matched, so the kit has none), `dmc update`, `install` and `refresh`, the shell deletes, the usual shell writes (`sed -i`, scripts, `tee`, `Set-Content`, `Add-Content`, `Out-File`) and the `SendFeedback` tool, which the developer settings leave allowed, and no kit prompt names a command those write rules deny, and the campaign allow rules do not start with `/`; hooks are well formed and silent when they should be, with `dmc` found on the `PATH`; the `Stop` hook runs `dmc redraw`, exits 2 only for a refused map file and stays silent with an older, missing or broken `dmc`; the repository's `CLAUDE.md` has no Dungeon Master section and `.claude/skills/` no player skill; the review skills name `dmc folder`; shell scripts keep LF and are executable; nothing personal is tracked (nothing under `adventures/`, `exports/`, `feedback/`, `modules/`, `rules/` or `bin/`); the feedback template has the keys (`dmc:` included) and sections `dmc-record-feedback` fills in; resume and full save never mention `feedback/`; only `review-feedback` mentions `gh issue` or `gh pr`; nothing points at the deleted Python scripts |
| Map page | `Playwright/MapPageTests.cs` | `live.html` opened from `file://` in Chromium: the SVG and tooltips render, a rewritten `live-data.js` shows within the two-second poll, and the PNG is the size of the SVG |
| Sheet page | `Playwright/SheetPageTests.cs`, `Sheets/` | The page `dmc sheet` writes, opened from `file://` in Chromium: a sheet per printed page, no box cut across pages, no sideways scrolling at a phone's width. `Sheets/` holds the contract (in-process and binary), the golden party page (`Fixtures/sheets/party.golden.html`; rewrite it with `DMC_UPDATE_GOLDEN=1` after a deliberate change), and the test that every label of the sheet templates and the Party status block is printed or named as not exported |

The toolkit tests scan `kit/` (what DMC reads in play), and `CLAUDE.md`, `README.md` and `.claude/skills/` (what a developer's session reads). They do not scan the documents in `.docs/` themselves, which may name the deleted scripts in their history; they only check that a `.docs/` path named in those prompts exists.

### Skip locally, fail on CI

`TestEnv.Need` skips a test whose tool is missing (bash, PowerShell, Playwright's Chromium, the published `dmc`) on a developer's machine, and fails it when the `CI` environment variable is set. CI installs everything, so nothing is skipped there.

### Which binary the black-box tests run

- By default, `artifacts/bin/dmc`. It must exist, must not be older than any source file (`.cs`, `.csproj`, `.props`) or any file under `kit/` (they are embedded), and must print `dmc 0.0.0-dev`; otherwise the test fails with one line saying to republish into `artifacts/bin`.
- `DMC_BIN` names another program to test, whatever version it prints.
- The tests that run `dmc install`, `dmc refresh` or `dmc update` through the binary pass `CLAUDECODE` unset and a temporary home, and put back the `dmc.json` a development build writes beside itself in `artifacts/bin/`. Tests that expect no DMC folder to be found are skipped if that file lists one (a developer who tried `artifacts/bin/dmc install` has one).

### The recall test

`tools/recall-test.ps1` reruns issue #179's recall test (spec 009, SC-002): it runs `dmc recall` for each question of a tab-separated questions file (`id`, `named` or `indirect`, the question, an answer regex) and prints, per style, how often the answer was in the output and in the first hit. The questions quote campaign facts, so the file lives in the maintainer's DMC folder (suggested: `recall-test/questions.tsv`) and is never committed; the script never runs in CI, where `tests/Dmc.Tests/Retrieval/` checks recall on invented fixtures. Run it after a change to the ranking (`src/Dmc.Cli/Retrieval/`) and compare with the figures on the issue. Usage and format: `specs/009-retrieval-lookup-recall/contracts/recall-test.md`.

### The slip test

`slip-test` (`src/Dmc.SlipTest/`, spec 024, issue #249) measures how often DMC's replies slip, so a change to the rules can be tested before it ships (#238, #240). It replays real play: for each fork point (a player message in a Claude Code log) and each arm (the control and a treatment), it writes the log up to that message under a new session id, with the DMC folder's path replaced by a copy folder's and the recorded `CLAUDE.md` replaced by the arm's rules; restores the copy's `adventures/` to that moment from Claude Code's file history; and runs `claude -p --resume <id> --fork-session` with the player's message and the original's model, effort and permission mode. Fresh-resume mode instead lays each arm's files over the copy, starts one base session per arm with the resume, and forks it. The pilot checks that the replays saw what play saw (no new notice, the same hook output, the arm's rules in the log, the same thinking blocks in both arms); `export` writes the replies under random ids for a blind score against a key written before the runs; `score` gives each arm's slip rate with a 95% interval over fork points, the difference, the pass line's verdict and the cost.

```
dotnet run --project src/Dmc.SlipTest -- prepare --work <work folder>
dotnet run --project src/Dmc.SlipTest -- run --pilot --work <work folder>
dotnet run --project src/Dmc.SlipTest -- pilot --work <work folder>
dotnet run --project src/Dmc.SlipTest -- run --work <work folder>
dotnet run --project src/Dmc.SlipTest -- export --work <work folder>
dotnet run --project src/Dmc.SlipTest -- score --work <work folder>
```

The work folder holds the trial, the fork points, the key and every run; they quote play, so it lives outside the repository (suggested: `slip-test/<trial>/` in the DMC folder) and is never committed. It needs Claude Code signed in, the logs and file history kept (#239), and a trusted copy folder. `tests/Dmc.Tests/SlipTest/` checks the runner on invented logs with a stand-in for Claude Code; nothing in CI runs a model. The file formats, the commands and their output: `specs/024-slip-test-runner/contracts/slip-test.md`; the pilot and the base rate step by step: its `quickstart.md`.

Limits (#249): a fork after a fresh resume measures a session's first slips, not the groups of slips in a long correction or a session above 300,000 tokens. A background session cannot be replayed (Claude Code rejects `--bg` with `-p`), so it runs as an ordinary one. A change made with a shell command left no backup and is not undone. The restore (spec 026) takes, for each file under `adventures/`, the backup that holds it at the fork: a backup is recorded at the next player message and holds the edits before it, so when a file was edited between the fork and that backup, the one before the fork is used; a session's first backup (`@v1`) is read from disk because no log names it; and `run` checks every point before its first model call and refuses, in one line, each point that no backup can restore. An arm in fork mode can also get its own text next to the player's message at each point (spec 029, for the oracle test #270): `context` in `trial.json` names a folder of `<point>.md` files, `run` adds a `UserPromptSubmit` hook to the copy's `.claude/settings.local.json` that prints the point's file for that arm's runs and puts the file back as the DMC folder has it after every run, and the pilot's `context` check confirms the text arrived. That Claude Code runs the hook in a fork is not verified in the cloud container, which runs no project hooks; the first pilot checks it. Which billing applies to the runs is not verified. A rewrite before a thinking block makes Claude Code drop that block (research R3), so the control's rules must differ from the recorded text as the treatment's do.

## CI

`.github/workflows/tests.yml` runs for every push to `main` and every pull request, on `ubuntu-latest` (linux-x64) and `windows-latest` (win-x64), each:

1. asks `.github/ci/needs-tests.sh` whether the changed files need the tests, and if not skips every step below;
2. installs the .NET 10 SDK, and on Linux `clang` and `zlib1g-dev` for Native AOT;
3. publishes `dmc` with Native AOT into `artifacts/bin`;
4. builds the tests and installs Playwright's Chromium through `pwsh`;
5. runs `dotnet test`.

**Required checks.** A ruleset on `main` requires `dotnet (ubuntu-latest)`, `dotnet (windows-latest)` and `claude-review` before a pull request can merge. A check that never reports blocks the merge for good, so the workflow has no `paths` filter: it starts on every change, and both `dotnet` checks always report.

**Which changes are tested.** A change that only changes or adds `*.md` or `*.txt` files skips the build and tests, so a documentation change (`.docs/`, the roadmap, a spec) passes in about a minute. `CLAUDE.md`, `README.md`, `.claude/skills/` and `kit/` always need them, because the toolkit tests read them as instructions, and so does deleting any file, because tests check that some Markdown and text files exist (the `.docs/` files a prompt names, the fonts' licences). The rule's one home is `.github/ci/needs-tests.sh`, which reads the changes (`git diff --name-status --no-renames` against the pull request's base, or the commit before a push) and prints `true` or `false`; a diff that fails counts as `true`. `ToolkitTests.CiRunsTheTestsForEveryChangeButDocumentation` checks it on example lists, and `CiReportsTheRequiredChecksOnEveryChange` checks the workflow's wiring. To change which files are tested, change the script and its test cases.

**Why the steps are skipped, not the job.** A job-level `if` is evaluated before the matrix is applied, so a skipped matrix job reports once, under its unexpanded name `dotnet (${{ matrix.os }})`, and matches neither required check. Each step after the decision carries `if: steps.changes.outputs.code == 'true'` instead; a new step needs the same condition, or the test above fails. See [decisions.md](decisions.md).

`.github/workflows/release.yml` also runs on pull requests that change the release files (`release.yml`, `.github/release/**`, `tools/**`, `kit/**`, `src/Dmc.Cli/Dmc.Cli.csproj`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`). On a pull request it is a dry run: it builds and packs every platform's archive, installs it with the install line (`tools/install.sh` and `tools/install.ps1`) from local assets into a temporary DMC folder and checks the result, checks that `dmc update` refuses a release signed by a throwaway key, signs the checksum list with a throwaway key and publishes nothing. Every third-party action in it is pinned to a full commit SHA, with its version in a comment. See [releases.md](releases.md).

`.github/workflows/claude-code-review.yml` runs Claude Code's review on every pull request. It uses the `CLAUDE_CODE_OAUTH_TOKEN` secret (renew it with `claude setup-token`). Pull requests opened by the Copilot coding agent run as the actor `Copilot`, which has no write access, so the workflow passes `github_token` (skipping Anthropic's OIDC app-token exchange, which answers 401 for that actor) and lists `Copilot` in `allowed_bots`; the review comment is then posted with `GITHUB_TOKEN`, hence `pull-requests: write`. Its `claude_args` append a system prompt that applies constitution Development 4 to the review: every claim it posts cites a file and line or a command, or says "unverified", and the subagents it launches get the same rule. The plugin's own prompt is upstream and unchanged; `ToolkitTests.CiReviewCitesItsClaims` keeps the appended prompt in place. See [decisions.md](decisions.md).

## Git and pull requests

`CLAUDE.md` § Git and GitHub is the home of these rules; in short:

- **Branches.** Never commit to `main`. Each change starts on its own branch from an up-to-date `origin/main`. Delete the branch once its PR has merged.
- **Never add personal or copyrighted files.** `adventures/`, `exports/`, `feedback/`, `modules/` and `rules/` are ignored; never force-add them.
- **Commits.** Only when asked or as part of an agreed plan. Never skip hooks or signing, never rewrite pushed history.
- **Pull requests** have a full description: what changed, why, the sources it rests on, how it was tested, and which documents it changed or why none needed to. After opening one, check with `gh pr view <n> --json body` that the description really landed. This applies to PRs opened by subagents too; one PR's body once came out as `-`.
- **Slip test.** A PR that changes how DMC plays (`CLAUDE.md` says what counts) states its slip-test result against a control, with the pass line fixed in its issue before the run, or says why the slip test cannot run it. See [The slip test](#the-slip-test).
- **Linking issues.** `Closes #N` only in the PR that finishes an issue. A PR that does one phase of a plan says `Part of #N`, so the issue stays open until the last part merges. One keyword per issue, each on its own line.
- **One review per PR.** Run Claude Code's built-in code-review command once, on the finished PR. Check the fixes for its findings by hand, and check a finding that cites no file, line or command against the code before acting on it; review again only if a fix turns into a redesign. A reviewer subagent launched for a PR or a plan gets constitution Development 4 in its prompt. Check that the documentation is current, whether or not the review found anything.
- **Merging.** CI must pass. The user merges.
- **Spec Kit.** Every change except prompt and context tuning from feedback, and documentation corrections, goes through it: see [Spec-driven changes](#spec-driven-changes), the rule's home.

### Documentation goes with every change

Since PR #22, `CLAUDE.md` § Git and GitHub (the "Documentation" and "Pull requests" bullets) requires every change to update its documentation in the same PR; a PR that leaves it stale is not finished:

- **in code**, an XML doc comment on every type and member the change adds or changes, private ones included (the build catches only missing visible ones, see below), and a header comment on every script and workflow it adds or changes;
- **in `.docs/`**, the documents that describe what changed, including [roadmap.md](roadmap.md) for status and [decisions.md](decisions.md) for any decision and its reason. A change to `CLAUDE.md`, a skill or a template changes how the product behaves, so it counts;
- **in the PR description**, which documents changed, or why none needed to.

`CLAUDE.md` is the rule's home; this section only describes it. The toolkit tests check that the `.docs/` paths the prompts name exist.

#### What the build checks (PR #24)

`Directory.Build.props` sets `GenerateDocumentationFile`, which makes the compiler report CS1591 (no XML comment on a visible type or member) and check that every comment is well formed. What is enforced, as of PR #24:

- **`src/Dmc.Cli`:** a missing comment on a public (or otherwise visible) member is a build error, because the project treats warnings as errors. A malformed comment (a `param` tag for some parameters but not all, a `cref` that does not resolve) is an error for the same reason.
- **`tests/Dmc.Tests`, comment correctness, every file:** CS1570 to CS1574, CS1580, CS1584, CS1587, CS1589 and CS1590 are errors (`WarningsAsErrors` in `Dmc.Tests.csproj`).
- **`tests/Dmc.Tests`, a comment on every visible member:** CS1591 is an error everywhere except in the files that hold MSTest test classes, `*Tests.cs`, `*Contract.cs` and `*Cases.cs`, where `tests/Dmc.Tests/.editorconfig` turns it off. Those files need public test methods, which go without a comment when their name says what they prove. `Support/`, the `*Fixtures.cs` files and `Bootstrap.cs` are fully checked.
- **Not checked by the compiler, but required by the rule above:** private and internal members anywhere, and the classes, helpers and stubs inside the exempt test files. Keep these documented, and check them in review.
- **Generated code** (`[GeneratedRegex]` output) is skipped by the compiler and needs no comments.
- **Release output:** `PublishDocumentationFile` is false in `Dmc.Cli.csproj`, so `dmc.xml` is not published, and the release archives stay at four files.

## Spec-driven changes

[GitHub Spec Kit](https://github.com/github/spec-kit) is installed for toolkit work only, so a planning session can produce a spec, plan and task list (`/speckit-specify` → `/speckit-clarify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-analyze`) and an implementing session can run `/speckit-implement` and `/speckit-converge` against them. Play never touches it: Spec Kit's skills, scripts and templates stay in this repository and never reach a DMC folder (the generated files come from `kit/` only), so the play settings no longer hide or deny them (the entries of `skillOverrides` and `Skill(speckit-*)` went with the move to `kit/settings.json`). The `speckit-*` skills are still installed with `disable-model-invocation: true` so their descriptions stay out of a developer's context. See [decisions.md](decisions.md), "Players install `dmc`, which writes and updates a DMC folder", for the route that made the play block unnecessary, and the earlier Spec Kit entries for what the block did.

The skills live in `.claude/skills/speckit-*/SKILL.md` and the templates, scripts and install state in `.specify/`, both tracked in git (except the machine-local files `.specify/.gitignore` lists, such as `feature.json`). Each feature's spec, plan and tasks go in `specs/NNN-<name>/`, which `/speckit-specify` creates. Using the skills needs only PowerShell (the scripts are PowerShell, `--script ps`). Playing needs none of this.

**How a change goes through it (issue #66).** Decided by the user after two pilots, `specs/001-campaign-import-export/` (PR #120) and `specs/002-remove-macos-support/` (PR #159); see [decisions.md](decisions.md):

- **Every toolkit change** started after this decision: a planning session runs `/speckit-specify` → `/speckit-clarify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-analyze`; an implementing session runs `/speckit-implement`, then `/speckit-converge` until it adds no tasks. The usual rules of [Git and pull requests](#git-and-pull-requests) apply on top.
- **Except feedback tuning.** A change that comes from recorded feedback and tunes DMC's prompts or its context use (`CLAUDE.md`, the skills, the templates, the hooks), as the `review-feedback` skill proposes, goes the way that skill takes it (on the user's word, an issue with the plan or a PR) and has no spec. Only a cause of the first kind (the instructions are wrong) is tuned with words; a missing mechanism becomes an issue in the form of a slip-plan trial, which goes through Spec Kit. A change to `dmc`'s code or a new feature is not tuning, even when it starts from feedback.
- **Except documentation corrections.** A change to `.docs/` or the README alone that brings a document in line with what is already true (a status, a release history, a stale reference) has no spec, so long as it changes no code, no kit file and no rule. Its PR says which documents it corrects and against what. A change to what a document says DMC or `dmc` should do is not a correction.
- **One issue per specced change** is the tracking point. Its description links to `specs/NNN-<name>/` and summarises the spec. If `/speckit-specify` was given no issue, its session opens one at the end, once `spec.md` is written, and names it in the spec's Input line. The issue gives the goal, why, and a short summary, and leaves the detail to the spec, so clarifying the spec does not make the issue stale. The tasks stay a checklist in `tasks.md`: do not run `/speckit-taskstoissues` or open an issue per task. The PR says `Closes #N` for that one issue (a plan split into phases links its issues as [Git and pull requests](#git-and-pull-requests) says).
- **A spec is frozen once converge is done.** A change ships in one PR: plan it as one, and split it into phases or several PRs only when the maintainer asks. Converge runs on the feature branch, after `/speckit-implement` and its tests, and again after the code review's fixes, which go in the same PR, until a pass adds no tasks; the spec is frozen when that PR merges. A correction found after the merge goes in the next PR that touches the feature or its documents, never a PR of its own, and may bring the spec folder in line with what shipped. Until then `spec.md`, `plan.md`, `tasks.md` and the rest are kept in line with what ships, as converge does; after that the folder is a record of the change, like an entry in [decisions.md](decisions.md), and is edited only by such a correction. A later change to the same feature gets its own `specs/NNN-<name>/` and may point back at the earlier one. The current behaviour lives in `.docs/`, the code and the prompts, not in old specs.

**The constitution.** `.specify/memory/constitution.md` is what `/speckit-plan` and `/speckit-analyze` check a feature against. It was written from this repository's existing rules (issue #62), following Spec Kit's [existing-projects guide](https://github.com/github/spec-kit/blob/main/docs/guides/existing-projects.md) ("Do not invent standards"): the two principles and § Git and GitHub of `CLAUDE.md`, § Writing prompts, § Skip locally, fail on CI and § Documentation goes with every change here, and four entries of [decisions.md](decisions.md) (native AOT and nothing for players to install, one-line errors and small output, every change updates its documentation, and the Spec Kit install). Version 2.0.0 (issue #67) added that players install Claude Code and `dmc` and nothing else. Since version 3.0.0 (2026-11-15) the constitution states principles of its own: Core (consistency, fun, low context usage and token cost), Development (count the token cost, no change breaks a campaign, one step to install and to upgrade, plans grounded in the code, a model and effort for each skill, and the former Principles III and V: each rule has one home, tests skip locally and fail on CI), Dungeon Master (an arc for a homebrew campaign, fair fights, the module is the source, the player shapes the story) and Documentation (concise; non-technical for players; simple technical English for developers; and the former Principle IV, documentation goes with every change). Version 4.0.0 (2026-11-17) removed Development 5, a model and effort for each skill (issue #219, not planned); the other principles keep their numbers. Version 5.0.0 (2026-11-25, issue #269) made Development 1 count bytes and tokens, not lines; it was drafted from the issue and applied on the maintainer's word in its PR, not with `/speckit-constitution`, as was 5.1.0 (2026-11-26, issue #268), which points at the slip-test rule for a PR that changes play. Each principle gives its rule, its check and a one-line reason. A rule that has a home elsewhere is still pointed at, not copied: to change it, change its home file. An amendment is made with `/speckit-constitution`, which only the user can run, or applied in a PR on the user's word from a draft they reviewed (5.0.0 and 5.1.0), and recorded in [decisions.md](decisions.md).

**Optional requirements, only for installing or upgrading Spec Kit** (never for using the skills, never for play): Python 3.11+, [uv](https://docs.astral.sh/uv/), and `specify-cli` pinned at 1.2.1:

```
uv tool install specify-cli --from git+https://github.com/github/spec-kit.git@v1.2.1
```

**Install.** `.specify/` and `.claude/skills/speckit-*` were written by:

```
specify init --here --force --non-interactive --ignore-agent-tools --integration claude --script ps --integration-options="--events false --no-model-invocation"
```

- `--integration claude` installs one skill per command into `.claude/skills/`.
- `--no-model-invocation` sets `disable-model-invocation: true` on every skill, keeping the descriptions out of every session's context.
- `--events false` stops the Claude integration writing event hooks into `.claude/settings.json`. With no extensions installed nothing is written anyway, but the option keeps it so if an extension is ever added.
- The stored options are kept in `.specify/integration.json` and reapplied on an integration upgrade.

**Upgrade.** (The subcommands and flags below were checked against `specify` 1.2.1's `--help`; the upgrade itself has not been run.) Bump the pin, then upgrade the CLI and the integration, repeating every stored option because passing `--integration-options` to an upgrade replaces all of them:

```
specify self upgrade --tag vX.Y.Z
specify integration upgrade claude --integration-options="--events false --no-model-invocation"
```

**What resets `--no-model-invocation`, and how to reapply it.** Per Spec Kit's [integrations reference](https://github.com/github/spec-kit/blob/main/docs/reference/integrations.md) ("Claude: user-invocable-only skills"), `specify preset add/remove/update`, `specify integration use claude` and some `switch` paths reset `disable-model-invocation` to the default. Reapply it with `specify integration upgrade claude --force --integration-options="--events false --no-model-invocation"`. The toolkit test `EverySpecKitSkillDisablesModelInvocation` checks that every `speckit-*` skill still has `disable-model-invocation: true`, so a silent reset fails the build. `SpecKitAddsNoHookAndNoClaudeMdBlockAndNothingInTheKit` checks that `.claude/settings.json` and `CLAUDE.md` carry nothing from Spec Kit and that no file of `kit/` names it. The other toolkit tests skip the `speckit-*` folders: they are not DMC's skills or prompts.

**A local line in a template.** `.specify/templates/tasks-template.md` carries one line of this repository's own, the slip-test task in its Polish phase (issue #268), so its hash no longer matches the manifest. An upgrade may report it as modified or replace it: re-apply the line after an upgrade.

**Line endings.** `.gitattributes` pins `.claude/skills/speckit-*/SKILL.md` to LF, because Spec Kit hashes those files' bytes in `.specify/integrations/*.manifest.json` and a CRLF checkout would look like a local modification at upgrade time (see [decisions.md](decisions.md)). The same manifest also hashes `.specify/scripts/powershell/*.ps1`, `.specify/templates/*.md` and `.specify/.gitignore`, which `.gitattributes` does not pin; on a Windows checkout with `core.autocrlf=true` they may be rewritten as CRLF, so if an upgrade reports them as modified, extend the rule to them.

**Extensions left out, and why.** `init` does not install the opt-in extensions, and none is wanted here:

- `git`: numbered feature branches, which duplicate `CLAUDE.md` § Git and GitHub.
- `agent-context`: writes a `<!-- SPECKIT START -->` block into `CLAUDE.md`, which is loaded on every play turn and would cost the player context.
- `github`: turns tasks into issues; the issue stays the tracking point and links to `specs/NNN-*/`. The core skill `/speckit-taskstoissues` does the same and is installed with the others, but is not used (see above).

## Writing prompts

`CLAUDE.md`, the skills and the templates are instructions to a model, and they have their own costs and conventions.

- **`kit/dm-rules.md` becomes the DMC folder's `CLAUDE.md`, which is loaded into every play session**, so every byte in it is paid for in every exchange of every game. Keep it lean: put procedures in skills, which load only when they apply, and put rarely needed reference material (the map format, for example) in a separate file a skill points to. When the rules must grow, look for something to cut or move.
- **Give each rule one home.** Write a rule in the one place it belongs, and have other places point to it ("in the order `CLAUDE.md` § Maps gives") instead of repeating it. Two copies drift apart.
- **Preferences are not rules.** How long one player likes replies, or how hard they like fights, belongs in `player-options.md`. The test, from the `record-feedback` and `review-feedback` skills: would the same behaviour have been wrong for a different player with different preferences? Only then is it a toolkit rule.
- **Feedback is recorded first, and tuned in one place.** Feedback about how DMC itself runs the game is recorded in `feedback/` of the DMC folder (see [campaign-files.md](campaign-files.md#feedback-entries)), by `dmc-record-feedback` in play and by `review-transcript` from the session logs. `review-feedback` is the only skill that reviews it, finds the cause, and files issues or makes the changes. During play nothing edits the toolkit. A toolkit test keeps `gh issue` and `gh pr` out of every other skill. `review-feedback` names one of four kinds of cause for each entry, with the file, line or log record that shows it: the instructions are wrong or contradict each other, a mechanism is missing or cannot do its work, the story files are wrong, or the model does not do it reliably. Only the first ends in changed words, and a fix of that kind that makes a file longer (a new template field included) is tried with the slip test first and changes the kit only if it passes (spec 027, which replaced the hold of spec 023; see [decisions.md](decisions.md)).
- **Prefer a mechanism to a paragraph** when the problem is that DMC forgets: a hook, a template field or a `dmc` feature is more reliable than another sentence.
- **Write for a reader with none of the history.** Say what to do and why; leave out the session that prompted it.
- **The toolkit tests check the prompts.** Every `dmc` command in a prompt must be a real subcommand, and the ones DMC runs must match an allow rule; every path under `templates/` or `.claude/skills/` in a kit prompt must exist in `kit/` (a DMC folder has no `tools/` or `.docs/`); a backticked `/word-word` is read as a skill name and must be a real one, so name Claude Code's own commands in words rather than as `/code-review`.

### Changing a campaign-file standard

A change to what campaign files should hold (a template, or a skill's rule for a file's shape) leaves every existing
campaign behind. It is not finished without a migration, or a line in the PR saying why none is needed (a new optional
field that an older file can simply lack, say). The format of a migration file is
`specs/005-campaign-migrations/contracts/migration-file.md`; this is how to add one.

1. **Number it.** The next integer after the highest file in `kit/migrations/`: `kit/migrations/NNN-<slug>.md`, three
   digits. Numbers run from 2 without a gap (standard 1 is the baseline, with no file). Two PRs that both add one
   conflict on the name, which is the point.
2. **Write it for a campaign you have never seen**, with the five headings in order: What changes, Steps, Judgments,
   Declared drops, Done when. Every step checks whether it is needed, so a campaign already in the new shape is left
   alone. Write before delete. Change only files the front matter's `touches` lists, and name in `drops` every class
   of content a step may remove. When the player must be asked something that may stop the migration, put it in an
   optional `## Before the backup` section between What changes and Steps (spec 011, migration 005's close question):
   `dmc-migrate-campaign` follows it after the player's yes and before the backup, so a stop changes nothing and the
   campaign stays playable, where a stop in Steps would leave it `unfinished`. Work it hands to another skill on
   another campaign (closing one, say) is that skill's, not a change the migration makes, so it stays out of `touches`.
3. **Name a shape check.** `checks` names at least one check in `src/Dmc.Cli/Campaign/MigrationChecks.cs` that fails
   on a file left in the old shape. A new check is a small function there, with a test in
   `tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`.
4. **Build the golden fixture** in `tests/Dmc.Tests/Fixtures/migrations/NNN/`: `before/`, a small campaign at standard
   `NNN-1` made from the templates plus the shapes the migration touches; `after/`, what a correct run leaves; and
   `accounting.tsv`, made by backing up `before/` in a scratch DMC folder, replacing the campaign with `after/`, and
   accounting for each unit `dmc campaign verify NAME --list` prints. Show one of each disposition the migration can
   produce. `Toolkit/MigrationGoldenTests.cs` verifies it on CI, and fails when a unit is removed, an undeclared drop
   class is claimed, or a touched file is left as it was; `Toolkit/MigrationFilesTests.cs` checks the file's format.
5. **Run it live** once: in a scratch DMC folder made with `artifacts/bin/dmc install`, put the `before/` campaign at
   standard `NNN-1`, start `dmc play`, ask DMC to bring the campaign's files up to date, and compare the result with
   `after/`. Fix the migration's prose for anything DMC got wrong. CI cannot run a model, so this is the only test
   that DMC can follow the steps.
6. **Say it in the PR**: which migration it adds and what the live run found, or why none is needed.

Never put a clause in a skill that repairs older files as it goes ("add the line if an older sheet lacks it"): that
work is the migration's, and the toolkit tests refuse such wording.
## Adding to `dmc`

- A subcommand is one class in `src/Dmc.Cli/Commands/` implementing `ICommand` (name, one-line summary, `Run`), added to `CommandTable.All`. It parses its own arguments and calls library code that the tests can call in-process.
- Throw `DmcException` for a failure the user can fix; `CommandLine` prints it as one line with the exit code it carries. A command that runs in a hook sets `NeverFails`, which swallows every failure and prints nothing.
- Add a contract test (run in-process and on the binary) and rows in the error-path guard.
- If a prompt is to call it, write the `dmc ...` form in the prompt; the existing `dmc *` allow rules cover it, and the toolkit tests check it. A command only a player should run (like `install`) is denied to DMC in `kit/settings.json` and refuses to run when `CLAUDECODE` is set.
- The generated files are inside `dmc`, so a change to `kit/` or to a command reaches players only with a new `dmc`, and a refresh of their DMC folder. Say so in the PR, as PR #18 did.
- A command that writes the generated files into a DMC folder goes through `GeneratedFiles.Guard` and `Refresher`: never write a path under `adventures/`, `feedback/`, `modules/`, `rules/` or `exports/`. A command that writes the player's own output is the exception, and writes only under `exports/`: `dmc campaign export` and `close` (ZIP files) and `dmc sheet` (`exports/sheets/<campaign>/`). Take the home folder, the environment, the clock and the registry as parameters, as `Machine` does, so a test cannot touch the real ones.
- Pin any package below 1.0 exactly in `Directory.Packages.props`.
