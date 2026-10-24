# Implementation Plan: Separate the Player Install from the Toolkit Source

**Branch**: `003-separate-player-install` | **Date**: 2026-10-24 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-separate-player-install/spec.md`

## Summary

The player installs `dmc`, and `dmc` writes and maintains a **DMC folder** (default `~/dmc`) that is an ordinary Claude
Code project: the Dungeon Master rules as `CLAUDE.md`, the play settings as `.claude/settings.json`, the player skills
(renamed `dmc-<name>`) in `.claude/skills/`, and `templates/`, beside the player's `adventures/`, `feedback/`, `modules/`
and `rules/`. The source of those files moves into `kit/` in the repository, under names Claude Code does not load, and
is built into `dmc`. A manifest in `.dmc/` records exactly which files `dmc` wrote, so `dmc refresh`, `dmc install` and
`dmc update` rewrite only those and never touch the player's files. A one-line bootstrap downloads `dmc` and runs
`dmc install`; `dmc update` replaces the program in place and refreshes the folder; `dmc play` refreshes if needed and
starts Claude Code there with `dmc` on the PATH. Releases are immutable and published only on the maintainer's approval;
`dmc update` accepts only a release whose checksum list is signed by a project key built into `dmc` (research R16). The
repository keeps a developer-only `CLAUDE.md` and developer skills;
the launchers, `get-dmc`, the release pin and the stale-binary check go; the constitution is amended so that players
install Claude Code and `dmc`. See [research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10 (`dmc`, Native AOT per platform); PowerShell 5.1/7 and POSIX sh (bootstrap
scripts); Markdown prompts; JSON settings; GitHub Actions YAML

**Primary Dependencies**: none added. Embedded resources, `HttpClient`, `System.IO.Compression` (`ZipFile`, `TarFile`,
`GZipStream`), `System.Security.Cryptography.SHA256` and `JsonDocument` are in the base library and AOT-safe.

**Storage**: files: `kit/` (source), `.dmc/manifest.json` (per DMC folder), `dmc.json` (beside the program).

**Testing**: MSTest `tests/Dmc.Tests` (Toolkit; new Install, Refresh, Play, Update, Folder and bootstrap tests);
`.github/workflows/tests.yml` on each supported OS; the release workflow's dry run; checks in play (quickstart).

**Target Platform**: Windows x64 and Arm, Linux x64 and Arm (glibc 2.34+). macOS stays unsupported.

**Project Type**: CLI plus prompts and bootstrap scripts (a toolkit)

**Performance Goals**: `dmc play`'s refresh check hashes about 20 small files before start-up: well under the 160 ms
start of a default build.

**Constraints**: warnings are errors in `src/Dmc.Cli`; every failure is one line `dmc <command>: <message>`; never
write under `adventures/`, `feedback/`, `modules/`, `rules/` or `exports/` (FR-012); start-up context no larger than
today's (SC-005); campaign permission rules relative to the current directory and generated-file deny rules anchored
with `/` (research R6).

**Scale/Scope**: about 25 prompt, template and settings files move (10 skills renamed); 4 new commands (`install`,
`refresh`, `update`, `folder`) and a rewritten `play`; `DmcFolder` replacing `Toolkit.Root` in 5 places; 2 bootstrap
scripts; 4 scripts and the pin machinery removed; the constitution, 9 `.docs/` files and the README.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Checked against the constitution **as amended by this feature** (research R13). The amendment is the user's decision
(spec Clarifications, 2026-10-24), made in its home file first as the constitution's Governance section requires.

| Principle | Check | Result |
|---|---|---|
| I. Story and fun | The rules, skills and templates move without changing what they say, apart from `dmc` called bare and the `dmc-` skill names. A DMC folder is a normal Claude Code project, so loading and compaction behave as today (R1). Player files are never written by `dmc` (FR-012, tested by hashing, R15). The maintainer's campaigns move by commands DMC gives but never runs, checked in play before the change is finished (SC-009). | Pass |
| II. Play pays nothing for toolkit work (amended: players install Claude Code and `dmc`) | **Context:** the DMC folder's `CLAUDE.md` is today's less the developer parts, and no developer or Spec Kit skill exists there (R13). **Install:** Claude Code and `dmc`, nothing else; `dmc` installs itself. | Pass: the amendment is the first commit of PR 1 (user, 2026-10-24), and the rest of PR 1 is checked against 2.0.0 |
| III. Each rule has one home | DM rules in `kit/dm-rules.md`, developer rules in `CLAUDE.md`; the generated-file table lives once in code and is tested against `kit/` (R2, R15). | Pass |
| IV. Documentation goes with every change | Each phase's PR updates its documents (below); `.docs/decisions.md` records the route, the superseded entries (R12) and the trust trade (R8); new scripts and workflows carry header comments; every new member an XML doc comment. | Pass, verified in each PR |
| V. Tests skip locally, fail on CI | The bootstrap tests need PowerShell and bash, like `GetDmcTests` today, and follow the rule (R15). | Pass |
| Build: Native AOT, warnings as errors | Only AOT-safe APIs (Technical Context). | Pass |
| Build: one-line errors, small output | Every message one line; install at most three lines, refresh at most two per folder (contracts). | Pass |
| Workflow: Git, Spec Kit, one review per PR | Tracking issue #67 (reopened, or a new one: the user decides); PRs as below. | Pass |
| Workflow: the toolkit tests check the prompts | They move to `kit/` and gain the checks in R15. | Pass |

**Re-checked after Phase 1 design:** no violation. New environment variables are test-only (`DMC_UPDATE_TEST_ASSETS`,
`DMC_INSTALL_TEST_*`, in the style of today's `DMC_GET_TEST_*`). Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/003-separate-player-install/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── bootstrap.md     # install.ps1, install.sh
│   ├── dmc-install.md
│   ├── dmc-refresh.md   # and dmc folder
│   ├── dmc-play.md
│   └── dmc-update.md
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
kit/                                     # NEW: source of every generated file (research R2)
├── dm-rules.md                          # -> CLAUDE.md. From CLAUDE.md less the toolkit clause and § Git and GitHub; ./bin/dmc -> dmc; skill names dmc-*
├── settings.json                        # -> .claude/settings.json. From .claude/play-settings.json + play parts of .claude/settings.json; dmc bare;
│                                        #    deny Edit/Write on /CLAUDE.md, /.claude/**, /templates/**, /.dmc/**; deny dmc update/install/refresh; speckit entries dropped
├── skills/dmc-*/SKILL.md                # -> .claude/skills/dmc-*/. The ten player skills, renamed and cross-references updated
└── templates/                           # -> templates/. Moved from templates/; feedback.md gains a dmc: key

CLAUDE.md                                # developer instructions only
.claude/settings.json                    # development only
.claude/play-settings.json               # removed (into kit/settings.json)
.claude/skills/                          # review-feedback, review-transcript (take a DMC folder, default dmc folder), speckit-*
templates/                               # removed (into kit/templates/)
adventures/, feedback/, modules/, rules/ # placeholders removed (Phase 3); .gitignore entries kept

src/Dmc.Cli/
├── Dmc.Cli.csproj                       # embed kit/** as resources
├── Kit/GeneratedFiles.cs                # NEW: the source -> target table, reading embedded resources, the player-folder guard
├── Kit/Manifest.cs                      # NEW: .dmc/manifest.json read/write, hashes
├── Kit/Refresher.cs                     # NEW: the refresh states (research R4)
├── DmcFolder.cs                         # NEW: dmc.json, DmcFolder.Find() (replaces Toolkit.Root)
├── Install/ProgramInstall.cs            # NEW: program folder, copy self, PATH (Windows registry, Linux link), leftovers
├── Install/ReleaseSource.cs             # NEW: latest release (GitHub or DMC_UPDATE_TEST_ASSETS), download, signature + SHA256SUMS + digest checks, unpack
├── Install/ReleaseKeys.cs               # NEW: the two ECDSA P-256 public keys (research R16)
├── Commands/InstallCommand.cs           # NEW, contracts/dmc-install.md
├── Commands/RefreshCommand.cs           # NEW, contracts/dmc-refresh.md
├── Commands/FolderCommand.cs            # NEW
├── Commands/UpdateCommand.cs            # NEW, contracts/dmc-update.md
├── Commands/PlayCommand.cs              # DMC folder, refresh, PATH prepend; no --settings
├── Commands/CampaignCommand.cs, RedrawCommand.cs, Digest/Digester.cs, Digest/LogFolder.cs   # DmcFolder instead of Toolkit.Root
├── CommandTable.cs                      # registers the new commands
├── CommandLine.cs                       # stops calling StaleBinaryCheck (PR 1)
├── Toolkit.cs, StaleBinaryCheck.cs      # removed together in PR 1 (the check reads the pin via Toolkit.Root)

tools/
├── install.ps1, install.sh              # NEW, contracts/bootstrap.md
├── get-dmc.ps1, get-dmc.sh              # removed
└── dmc-release.json                     # removed
play.ps1, play.sh                        # removed

.github/
├── workflows/release.yml                # publish job in environment `release` (approval); draft, attach archives + SHA256SUMS.txt + .sig + install.*, publish;
│                                        #   sign with DMC_RELEASE_SIGNING_KEY (a throwaway key in dry runs); actions pinned to SHAs;
│                                        #   dry run installs with the bootstrap into a temp DMC folder; pin job removed
├── workflows/tests.yml                  # path filters: kit/** instead of templates/** and the moved skills
└── release/write-pin.sh                 # removed

tests/Dmc.Tests/
├── Toolkit/ToolkitTests.cs              # read kit/; new checks (research R15)
├── Kit/RefreshTests.cs, Kit/ManifestTests.cs, Kit/GeneratedFilesTests.cs   # NEW
├── Distribution/InstallTests.cs, UpdateTests.cs, FolderTests.cs, BootstrapTests.cs   # NEW
├── Distribution/RefreshCommandTests.cs, ReleaseKeysTests.cs   # NEW
├── Distribution/PlayTests.cs            # rewritten for the DMC folder
├── Distribution/GetDmcTests.cs, LauncherTests.cs, Bootstrap.cs   # removed or reduced
└── Campaign/*, Map/RedrawTests.cs, Digest/*   # DMC folder instead of Toolkit.Root

.specify/memory/constitution.md          # Principle II redefined; 2.0.0 (research R13)
README.md                                # for players: install Claude Code, the install line, dmc play
.docs/architecture.md, campaign-files.md, user-guide.md, product-overview.md, development.md, releases.md, dmc-reference.md, roadmap.md, decisions.md
```

**Structure Decision**: Single existing repository, one new top-level folder `kit/` and a `Kit/` and `Install/` area in
`src/Dmc.Cli`. Delivered in three PRs under one tracking issue:

- **Phase 1, the kit and the DMC folder (`Part of #67`).** Constitution amendment and decision entry; `kit/` with the
  split `CLAUDE.md`, the renamed skills, the templates and the merged settings; embedding; `GeneratedFiles`, `Manifest`,
  `Refresher`, `DmcFolder`; `dmc install` (with `ProgramInstall`), `dmc refresh`, `dmc folder`, the new `dmc play`;
  `Toolkit.Root` replaced; the review skills read a DMC folder; `play.ps1`, `play.sh`, their tests, the `get-dmc` tests and CI's release-versioned build removed (the repository is no
  longer a place to play); toolkit and command tests; the documents for all of it. The maintainer installs from a local
  build into a DMC folder and moves their campaigns (the commands are in this PR's documents); check in play.
- **Phase 2, updating and releasing (`Part of #67`).** `ReleaseKeys`, `ReleaseSource`, `dmc update`, `install.ps1` and
  `install.sh`, the release workflow (approval-gated publish job, draft-then-publish, signing, the scripts attached,
  bootstrap-based dry run, actions pinned, pin job removed), `get-dmc`, the pin file and `write-pin.sh`
  removed (`StaleBinaryCheck` already went in Phase 1 with `Toolkit.cs`); README install line; `.docs/releases.md` documents the keys, the settings and key recovery. Then the release
  (`dmc-v0.2.0`), after the human steps H1 to H6 below.
- **Phase 3, switch-over (`Closes #67`).** Remove the repository's placeholder player folders and their toolkit tests;
  finish the README and `.docs/` for players; the check in play from the published release, in a fresh DMC folder and
  the migrated one (SC-009).

### Human steps

Steps only the maintainer can do (account settings, secrets, approvals, playing). Each becomes its own task in `tasks.md`,
marked as a human step, and is listed in #67, so an implementing session stops and asks instead of skipping or guessing.

| # | When | Step |
|---|---|---|
| H1 | Before Phase 2's PR merges | Make the two signing key pairs with `openssl` (commands in `.docs/releases.md`); send the two **public** keys for `ReleaseKeys.cs`; keep the backup private key offline. |
| H2 | Before Phase 2's PR merges | Create the GitHub environment `release` with yourself as required reviewer, and add the active private key as its secret `DMC_RELEASE_SIGNING_KEY`. |
| H3 | Before the first release | Turn on immutable releases in the repository settings. |
| H4 | Before the first release | Add rulesets protecting `main` and the `dmc-v*` tags; confirm two-factor sign-in on your account. |
| H5 | Each release | Push the tag `dmc-vX.Y.Z` and approve the `release` environment when the workflow asks. |
| H6 | After Phase 1 merges | Install from a local build into your DMC folder and run the move commands for your campaigns, feedback, modules and rules (DMC gives them, never runs them). |
| H7 | End of Phase 1, and before Phase 3 merges | Play one scene: Phase 1 from a local build; Phase 3 from the published release, in a fresh DMC folder and in the migrated one (SC-009). Report `/context` size and whether rolls, maps and hooks worked. |
| H8 | Each PR | Merge, once CI passes and the review is addressed (`CLAUDE.md` § Git and GitHub: the user merges). |

### Issues

One issue, #67, tracks the whole feature, as `.docs/development.md` § Spec-driven changes says: no sub-issues and no
issue per task. The tasks stay a checklist in `tasks.md`. Human steps are marked as such in `tasks.md` and listed
explicitly in #67 (user, 2026-10-24). Only the last PR closes it: Phases 1 and 2 say `Part of #67` and must not use
`Closes`, `Fixes` or `Resolves` anywhere in their description or commits; Phase 3 says `Closes #67` (`CLAUDE.md` § Git
and GitHub, Linking issues). Each PR's task list in `tasks.md` includes checking this after the PR is opened.

## Complexity Tracking

No constitution violations once Principle II is amended (Constitution Check), so nothing to justify.
