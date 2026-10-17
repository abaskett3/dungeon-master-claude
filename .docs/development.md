# Development

This document is for contributors changing `dmc`, the launchers, the skills, the templates or `CLAUDE.md`. Playing needs none of it. The binding rules for toolkit work are in [CLAUDE.md](../CLAUDE.md) § Git and GitHub; this document explains the setup and the conventions around them.

## Requirements

- The [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`winget install Microsoft.DotNet.SDK.10` on Windows). `global.json` asks for 10.0.300 or a later feature band.
- Git, and for the tests that use them, bash (Git Bash on Windows) and PowerShell. Tests for a tool that is missing are skipped locally.
- For the map page tests, Playwright's Chromium (below).
- Nothing else for the default build. Native AOT builds also need a C++ toolchain: Visual Studio's "Desktop development with C++" workload on Windows, `clang` and `zlib1g-dev` on Linux, or the Xcode command line tools on macOS.

## Repository layout

| Path | Holds |
|---|---|
| `CLAUDE.md`, `.claude/skills/`, `templates/` | The prompts: DMC's operating rules, the skills and the blank campaign files |
| `.claude/settings.json`, `.claude/play-settings.json` | Permissions and hooks for every session, and the extra settings for play |
| `play.ps1`, `play.sh` | The launchers |
| `tools/` | `get-dmc.ps1`, `get-dmc.sh` and the release pin `dmc-release.json` |
| `src/Dmc.Cli/` | `dmc`'s source (C#, .NET 10) |
| `tests/Dmc.Tests/` | The tests (MSTest and Playwright) |
| `.github/workflows/` | `tests.yml` (CI) and `release.yml` (releases); `.github/release/write-pin.sh` |
| `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `dmc.sln` | Shared build settings, centrally pinned package versions, the SDK pin, the solution |
| `.docs/` | These documents |
| `adventures/`, `modules/`, `rules/`, `bin/`, `artifacts/` | Personal, copyrighted or built files; all ignored by git |

## Building

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
```

Use your system's runtime identifier in place of `win-x64`: `win-arm64`, `linux-x64`, `linux-arm64` or `osx-arm64`.

- **The default build** is a self-contained single-file program without AOT. It needs no C++ toolchain and no flags. It starts in about 160 ms on Windows.
- **The release build** adds `-p:PublishAot=true`: Native AOT, which starts in under 30 ms. CI and the release workflow build this way, and any test that measures time must use it.
- Plain `dotnet build` and `dotnet test` build an ordinary framework-dependent assembly; neither publishes `dmc`.
- The AOT, trim and single-file analyzers run on every build, and warnings are errors, so a local build already shows what a release build would complain about.

**Versions.** Every build that is not a release says `dmc 0.0.0-dev`. That version is set in `src/Dmc.Cli/Dmc.Cli.csproj`, and no release ever has it. A release gets its version from its tag (`-p:Version=X.Y.Z`); nothing writes a release version back into the project file.

**`bin/` belongs to the launchers.** `bin/` is the player's install, which `get-dmc` owns. Publish your own builds to `artifacts/bin/`. A build put in `bin/` is replaced by the pinned release the next time `./play.ps1` or `./play.sh` runs, because its version (`0.0.0-dev`) never matches the pin. A developer build in `artifacts/bin/` finds the toolkit root by looking up to three folders above itself for `tools/dmc-release.json`, and gets no stale-binary warning.

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
| Distribution | `Distribution/` | `dmc play`, the stale-binary warning, the pin file, `get-dmc` in bash, PowerShell 7 and Windows PowerShell 5.1 (install, silence when current, refusal of a bad archive, choice of archive per system), and the launchers in every case: no pin, pinned, `dmc` unavailable, `dmc` that does not run |
| Toolkit | `Toolkit/ToolkitTests.cs` | The prompts and settings: every `dmc` command in `CLAUDE.md`, the README, the skills and the templates is a real subcommand and matches an allow rule for Bash and PowerShell; paths under `templates/`, `tools/`, `.claude/skills/` and `.docs/` exist; every `/skill-name` is a real skill and the README lists them all; hooks are well formed and silent when they should be; the launchers fetch `dmc` first; shell scripts keep LF and are executable; nothing personal is tracked; nothing points at the deleted Python scripts |
| Map page | `Playwright/MapPageTests.cs` | `live.html` opened from `file://` in Chromium: the SVG and tooltips render, a rewritten `live-data.js` shows within the two-second poll, and the PNG is the size of the SVG |

The toolkit tests scan `CLAUDE.md`, `README.md`, `.claude/skills/` and `templates/`, the files DMC reads as instructions. They do not scan the documents in `.docs/` themselves, which may name the deleted scripts in their history; they only check that a `.docs/` path named in those prompts exists.

### Skip locally, fail on CI

`TestEnv.Need` skips a test whose tool is missing (bash, PowerShell, Playwright's Chromium, the published `dmc`) on a developer's machine, and fails it when the `CI` environment variable is set. CI installs everything, so nothing is skipped there.

### Which binary the black-box tests run

- By default, `artifacts/bin/dmc`. It must exist, must not be older than any source file (`.cs`, `.csproj`, `.props`), and must print `dmc 0.0.0-dev`; otherwise the test fails with one line saying to republish into `artifacts/bin`.
- `DMC_BIN` names another program to test, whatever version it prints.
- Installing a release in the distribution tests needs a program that prints a release version. CI publishes a second AOT copy with `-p:Version=9.9.9` and names its folder in `DMC_TEST_RELEASE_DIR`. Locally, without that variable, the tests build such a copy themselves from a copy of the sources (about 15 seconds).

## CI

`.github/workflows/tests.yml` runs for every push to `main` and every pull request, on `ubuntu-latest` (linux-x64), `windows-latest` (win-x64) and `macos-latest` (osx-arm64), each:

1. installs the .NET 10 SDK, and on Linux `clang` and `zlib1g-dev` for Native AOT;
2. publishes `dmc` with Native AOT into `artifacts/bin`;
3. publishes a second copy with `-p:Version=9.9.9` into `artifacts/release`;
4. builds the tests and installs Playwright's Chromium through `pwsh`;
5. runs `dotnet test` with `DMC_TEST_RELEASE_DIR` set.

`.github/workflows/release.yml` also runs on pull requests that change the release files (`release.yml`, `.github/release/**`, `tools/**`, `src/Dmc.Cli/Dmc.Cli.csproj`, `Directory.Packages.props`). On a pull request it is a dry run: it builds, packs and installs every platform's archive with `get-dmc` and publishes nothing. See [releases.md](releases.md).

## Git and pull requests

`CLAUDE.md` § Git and GitHub is the home of these rules; in short:

- **Branches.** Never commit to `main`. Each change starts on its own branch from an up-to-date `origin/main`. Delete the branch once its PR has merged.
- **Never add personal or copyrighted files.** `adventures/`, `modules/` and `rules/` are ignored; never force-add them.
- **Commits.** Only when asked or as part of an agreed plan. Never skip hooks or signing, never rewrite pushed history.
- **Pull requests** have a full description: what changed, why, the sources it rests on, how it was tested, and which documents it changed or why none needed to. After opening one, check with `gh pr view <n> --json body` that the description really landed. This applies to PRs opened by subagents too; one PR's body once came out as `-`.
- **Linking issues.** `Closes #N` only in the PR that finishes an issue. A PR that does one phase of a plan says `Part of #N`, so the issue stays open until the last part merges. One keyword per issue, each on its own line.
- **One review per PR.** Run Claude Code's built-in code-review command once, on the finished PR. Check the fixes for its findings by hand, and review again only if a fix turns into a redesign. Check that the documentation is current, whether or not the review found anything.
- **Merging.** CI must pass. The user merges.

### Documentation goes with every change

Since PR #22, `CLAUDE.md` § Git and GitHub (the "Documentation" and "Pull requests" bullets) requires every change to update its documentation in the same PR; a PR that leaves it stale is not finished:

- **in code**, an XML doc comment on every type and member the change adds or changes, private ones included (the build catches only missing public ones, from PR #24), and a header comment on every script and workflow it adds or changes;
- **in `.docs/`**, the documents that describe what changed, including [roadmap.md](roadmap.md) for status and [decisions.md](decisions.md) for any decision and its reason. A change to `CLAUDE.md`, a skill or a template changes how the product behaves, so it counts;
- **in the PR description**, which documents changed, or why none needed to.

`CLAUDE.md` is the rule's home; this section only describes it. The toolkit tests check that the `.docs/` paths the prompts name exist.

## Writing prompts

`CLAUDE.md`, the skills and the templates are instructions to a model, and they have their own costs and conventions.

- **`CLAUDE.md` is loaded into every session**, so every line in it is paid for in every exchange of every game. Keep it lean: put procedures in skills, which load only when they apply, and put rarely needed reference material (the map format, for example) in a separate file a skill points to. When `CLAUDE.md` must grow, look for something to cut or move.
- **Give each rule one home.** Write a rule in the one place it belongs, and have other places point to it ("in the order `CLAUDE.md` § Maps gives") instead of repeating it. Two copies drift apart.
- **Preferences are not rules.** How long one player likes replies, or how hard they like fights, belongs in `player-options.md`. The test, from the `review-transcript` skill: would the same behaviour have been wrong for a different player with different preferences? Only then is it a toolkit rule.
- **Prefer a mechanism to a paragraph** when the problem is that DMC forgets: a hook, a template field or a `dmc` feature is more reliable than another sentence.
- **Write for a reader with none of the history.** Say what to do and why; leave out the session that prompted it.
- **The toolkit tests check the prompts.** Every `./bin/dmc` command must be a real subcommand and match an allow rule; every path under `templates/`, `tools/`, `.claude/skills/` or `.docs/` must exist; a backticked `/word-word` is read as a skill name and must be a real one, so name Claude Code's own commands in words rather than as `/code-review`.

## Adding to `dmc`

- A subcommand is one class in `src/Dmc.Cli/Commands/` implementing `ICommand` (name, one-line summary, `Run`), added to `CommandTable.All`. It parses its own arguments and calls library code that the tests can call in-process.
- Throw `DmcException` for a failure the user can fix; `CommandLine` prints it as one line with the exit code it carries. A command that runs in a hook sets `NeverFails`, which swallows every failure and skips the stale-binary check.
- Add a contract test (run in-process and on the binary) and rows in the error-path guard.
- If a prompt is to call it, write the `./bin/dmc ...` form in the prompt; the existing `./bin/dmc *` allow rules cover it, and the toolkit tests check it.
- A change the player's `bin/dmc` must have reaches players only through a release, and a prompt that calls a new subcommand breaks until that release is pinned. Say so in the PR, as PR #18 did.
- Pin any package below 1.0 exactly in `Directory.Packages.props`.
