# Research: Separate the Player Install from the Toolkit Source

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Date**: 2026-10-24 (rewritten for the user's
direction of that day; the 2026-10-23 version researched #67's plugin design, which is replaced)

Each entry gives the decision, why, and what else was considered. Claims about Claude Code come from its documentation,
fetched on 2026-10-23, and are quoted where the plan depends on them. Claims about this repository were checked against
the files named.

## R1. A DMC folder is an ordinary Claude Code project

- **Decision:** the DMC folder holds the rules as `CLAUDE.md`, the play settings as `.claude/settings.json` and the
  player skills in `.claude/skills/`, exactly where Claude Code looks in any project. `dmc play` starts Claude Code
  there with `--model sonnet --effort high` and the player's arguments; it passes no `--settings`, plugin or
  system-prompt flags.
- **Rationale:** this is how play works today from the repository root (`CLAUDE.md` auto-loaded, `.claude/skills/`
  discovered), so it carries no new Claude Code risk: compaction, skill discovery and hooks behave as now. It also
  makes a plain `claude` started in the DMC folder a working Dungeon Master (FR-011). The project settings file is now
  the play settings, because no development happens in a DMC folder; the repository's split between
  `settings.json` and `play-settings.json` existed only because play and development shared one folder.
- **Consequence:** #67's Phase 0 spike is no longer a gate. Its open items reduce to two checks folded into Phase 1's
  check in play: hooks find `dmc` by bare name (R5), and start-up context is no larger (R13).
- **Alternatives:** #67's design (kit loaded per session with `--plugin-dir`, `--append-system-prompt-file` and
  `--settings` into separate game folders): replaced by the user's one-folder decision; it also made a plain `claude`
  in the folder not the Dungeon Master, and needed the `/dmc:` plugin prefix.

## R2. The source of the generated files: `kit/` in the repository, under names Claude Code does not load

- **Decision:** a new folder `kit/` holds the source of every generated file, under neutral names, and a fixed table in
  `dmc` maps each to its place in a DMC folder:

  | Source in the repository | Generated in the DMC folder |
  |---|---|
  | `kit/dm-rules.md` | `CLAUDE.md` |
  | `kit/settings.json` | `.claude/settings.json` |
  | `kit/skills/dmc-<name>/SKILL.md` (and any files beside it) | `.claude/skills/dmc-<name>/...` |
  | `kit/templates/**` | `templates/**` |

- **Rationale:** if the source were laid out as `kit/CLAUDE.md` and `kit/.claude/skills/`, a developer's Claude Code
  session in the repository would pick them up: Claude Code loads a subdirectory's `CLAUDE.md` when it works with files
  there, and discovers nested `.claude/skills/` the same way, so editing the Dungeon Master rules would pull them into
  the developer's context. Neutral names keep development and play apart, which is the point of the feature.
- **Repository after the move:** `CLAUDE.md` holds only the developer instructions (FR-003); `.claude/skills/` holds
  only `review-feedback`, `review-transcript` and the `speckit-*` skills; `.claude/settings.json` holds only what
  development needs; `.claude/play-settings.json` and `templates/` are gone (moved into `kit/`).
- **Alternatives:** generating from today's locations (keeps the DM rules in the repository's `CLAUDE.md`, the opposite
  of FR-003).

## R3. The generated files are built into `dmc`

- **Decision:** `src/Dmc.Cli/Dmc.Cli.csproj` embeds every file under `kit/` as a resource with a logical name of its
  path under `kit/`. `dmc` reads them with `Assembly.GetManifestResourceStream`. A toolkit test checks that the set of
  embedded resources equals the set of files under `kit/`, so a file missed by the glob fails the build's tests.
- **Rationale:** the user's choice (clarification 2026-10-24). The release archive stays the program and its three
  native libraries; a program and its files cannot drift apart. Manifest resources work under Native AOT (no
  reflection over types). The files are small (today's `CLAUDE.md` is 28,347 bytes; all skills and templates together
  are of the same order), so the program's size (about 47 MB) barely changes.
- **Version:** the generated files carry the program's version (`CommandLine.Version`); a development build is
  `0.0.0-dev`.
- **Alternatives:** a `kit/` folder beside the program in the archive (#67 decision 1, option A): one more thing to keep
  in step and for the updater to swap.

## R4. Which files `dmc` owns: the manifest

- **Decision:** every write of the generated files also writes `.dmc/manifest.json` in the DMC folder: the `dmc`
  version, and for each generated file its path and SHA-256. The manifest is the only list of files `dmc` may rewrite or
  remove, and its presence is what makes a folder a DMC folder.
- **Rewrite (`dmc refresh`):** for each file in the program's table: if it is missing, write it; if it matches the
  program's copy, leave it; if it differs and its hash matches the manifest (an older generated version), replace it;
  if it differs from both (edited by hand), copy it to `.dmc/replaced/<yyyyMMdd-HHmmss>/<path>`, then replace it, and
  say so. For each file in the old manifest that the program no longer has (a removed or renamed skill): remove it if it
  is unchanged; if edited, move it to `.dmc/replaced/` instead. Empty folders it leaves under `.claude/skills/` are
  removed. Then write the new manifest.
- **First install over an existing folder:** a file the program would generate that exists, with no manifest entry,
  is not `dmc`'s: install refuses and names it (spec Edge Cases); install never adopts a file it did not write. A repeat
  install over a folder that has a manifest behaves as a refresh.
- **The guard (FR-012):** the table may not map anything under `adventures/`, `feedback/`, `modules/`, `rules/` or
  `exports/`; the writer refuses any such path at run time, and a toolkit test checks the table. Nothing outside the
  table and the manifest is ever read for writing.
- **Alternatives:** overwriting every generated file blindly (loses a hand edit with no copy); a version stamp only (cannot
  tell a hand edit from an old version, and cannot remove files a release dropped).

## R5. `dmc` is reached by its bare name

- **Decision:** `dmc install` puts the program folder on the user's PATH (R7), and `dmc play` also starts Claude Code
  with the program folder first on the child's `PATH`, so a session started by `dmc play` works even before a new
  terminal has picked up the PATH change. Every `./bin/dmc` in the prompts becomes `dmc`; the hooks call `dmc` bare
  instead of `"$CLAUDE_PROJECT_DIR/bin/dmc"`; allow rules become `Bash(dmc *)` and `PowerShell(dmc *)`.
- **Rationale:** the Bash and PowerShell tools and the hooks are children of Claude Code. Hooks reference: "A hook
  process inherits the parent environment". Today's prompts call `./bin/dmc` (in `CLAUDE.md` and the skills
  `draw-map`, `ingest-module`, `resume-campaign`, `setup-rules`, and the developer skills `review-feedback` and
  `review-transcript`).
- **Check:** in Phase 1's check in play, a roll from the Bash tool, one from the PowerShell tool, and the Stop hook
  (`dmc redraw`), in a session started by `dmc play` and in one started by plain `claude`.

## R6. The play settings protect the generated files

- **Decision:** `kit/settings.json` is today's `.claude/play-settings.json` plus the play parts of
  `.claude/settings.json` (the `allow` rules for the campaign folders and for `dmc`, and the PreCompact and
  SessionStart hooks). It adds deny rules for `Edit` and `Write` on `/CLAUDE.md`, `/.claude/**`, `/templates/**` and
  `/.dmc/**`, and for `Bash(dmc update*)`, `Bash(dmc install*)`, `Bash(dmc refresh*)` and the `PowerShell` forms
  (FR-005, FR-016).
- **Rationale:** in project settings, permissions reference: "`/path` | Path relative to the settings source", and for
  "Project settings at `.claude/settings.json`" it resolves to "`<primary working directory>/path`", which is the DMC
  folder. Campaign rules stay written `Edit(adventures/**)` (relative to the current directory, as today).
- **Dropped:** the `speckit-*` entries in `skillOverrides` and `Skill(speckit-*)` in `deny`. A DMC folder has no Spec Kit
  skills, so there is nothing to hide (this removes the cost recorded in `.docs/decisions.md` § Spec Kit installed in
  `.claude/skills/` and blocked in play).
- **The program refuses too (FR-016):** a deny rule matches the command's text, so `"dmc" update`, `cmd /c dmc update`
  or `& dmc update` would get past it. `dmc update`, `dmc install` and `dmc refresh` therefore refuse when the
  environment variable `CLAUDECODE` is set: Claude Code sets it (`CLAUDECODE=1`) for every Bash and PowerShell command it
  runs (checked in this session, 2026-10-24). The message is `cannot run inside a Claude Code session: run it in a
  terminal`. `dmc play` refreshes before Claude Code starts, so it is not affected. The deny rules stay as a second guard
  and keep the attempt from reaching the program at all.
- **Limit, unchanged from today:** a deny rule on `Edit` does not stop a shell command from writing a file; the existing
  deny rules on shell deletes and moves stay. The prompts never ask DMC to write a generated file, and FR-005 promises
  only that: the tools are refused, and a file changed another way is restored by the next `dmc play`'s refresh, which
  keeps the changed copy in `.dmc/replaced/` (R4).

## R7. `dmc install`

- **Decision:** `dmc install [--path <folder>]`.
  1. **The program.** If the running `dmc` is not already in the install location, copy it and its three native
     libraries there: `%LOCALAPPDATA%\Programs\dmc\` on Windows, `~/.local/share/dmc/` with a link `~/.local/bin/dmc`
     on Linux. Add the folder to the user `Path` (Windows, `HKCU\Environment`, then broadcast the change) or check that
     `~/.local/bin` is on PATH and say how to add it if not (Linux).
  2. **The folder.** Ask `DMC folder [~/dmc]:` unless `--path` is given; Enter takes the default; with no terminal, the
     default. Refuse a folder inside a git checkout of this repository (it has `kit/dm-rules.md` above it).
  3. **Write** the generated files and the manifest (R4); create `adventures/`, `feedback/`, `modules/` and `rules/`
     if missing, and nothing inside them.
  4. **Record** the folder in `dmc.json` in the program folder (a list; a new install adds to it).
- **Reading the answer under `curl ... | sh`:** standard input is the pipe there, so on Linux `dmc install` reads the
  answer from `/dev/tty` when standard input is not a terminal, and takes the default if `/dev/tty` cannot be opened.
  On Windows `irm ... | iex` runs in the console, so standard input is the terminal.
- **What install does not write:** `adventures/player-options.md` (DMC creates it from `templates/player-options.md` in
  the first session, as `CLAUDE.md` § Player options already says), and `feedback/.ignore`. That file exists so ripgrep
  searches git-ignored entries in the repository (`.docs/decisions.md` § feedback/ has a .ignore file); a DMC folder is
  not a git checkout, so ripgrep does not apply `.gitignore` rules there and needs no override.
- **Alternatives:** an install script that writes the files itself (rejected by the user: `dmc` does it).

## R8. The bootstrap line

- **Decision:** `tools/install.ps1` and `tools/install.sh`, attached to every release, do only what `dmc` cannot do
  before it exists: pick the archive for this machine, download it and the release's `SHA256SUMS.txt` over HTTPS with a
  stall timeout, check the hash before unpacking, unpack into a temporary folder, check the new `dmc` runs, and run
  `dmc install` from there (passing `--path` through if given). The README's lines:
  `irm https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.ps1 | iex` and
  `curl -fsSL https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.sh | sh`.
- **Reuse:** machine detection, the download with a stall timeout, the hash check and the macOS refusal come from
  `tools/get-dmc.ps1` and `get-dmc.sh`, including their test hooks (renamed `DMC_INSTALL_TEST_*`).
- **Trust:** the archive must match both `SHA256SUMS.txt` and the `digest` GitHub's releases API reports for the asset
  (`sha256:<hex>`). The API allows 60 requests an hour per address without sign-in, which a shared network can use up;
  on that refusal only, the bootstrap checks `SHA256SUMS.txt` alone and says so in one line. The bootstrap does not
  check the signature: it comes from the same release as everything it would
  check, and a `dmc` it downloaded cannot vouch for itself. The first install rests on HTTPS, immutable releases (a
  published release cannot be altered) and the two hash checks (research R16).

## R9. `dmc update`

- **Decision:** read the latest release from the GitHub API (`releases/latest`, read with `JsonDocument`); refuse a
  development build; stop if not newer; download `SHA256SUMS.txt` and `SHA256SUMS.txt.sig` and verify the signature
  with the public keys built into `dmc` (R16); download the archive for this machine; check its hash against the signed
  list and against the API's `digest` for the asset; unpack into `<program folder>/.update-<random>/`; run the new `dmc --version`; swap (Windows: rename the running
  `dmc.exe` to `dmc.exe.old`, as `get-dmc.ps1` does, since a running executable can be renamed but not deleted; the
  libraries are renamed the same way); then start the **new** `dmc refresh --all` so the files come from the new
  program. Leftovers (`*.old`, `.update-*`) are removed by the next `dmc update`, `install` or `play`.
- **Failure:** before the swap, nothing changes. A failure inside the swap moves back what was moved. If the refresh after
  a successful swap fails, the update reports it in one line; `dmc play` refreshes before starting (FR-015), so play
  never runs on files from another version (SC-007).
- **AOT:** `HttpClient`, `System.IO.Compression` (`ZipFile`, and `TarFile` with `GZipStream` for Linux archives) and
  `JsonDocument` need no reflection.
- **Test seam:** `DMC_UPDATE_TEST_ASSETS=<folder>` takes the release (a `latest.json` with the tag, the asset digests,
  the archives, `SHA256SUMS.txt` and its `.sig`) from a local folder. The signature and hash checks still apply with the
  built-in keys, so through the real program only the refusals can be tested end to end; the success path is tested
  in-process, with a test key passed to the update code as a parameter. No environment variable or setting can change
  the keys or skip a check (FR-028).

## R10. Where `dmc` reads and writes game files: the DMC folder

- **Decision:** `Toolkit.Root` (the folder above `bin/`) is replaced by `DmcFolder.Find()`: `CLAUDE_PROJECT_DIR` if it
  is a DMC folder (hooks), else the current folder if it is one, else the first recorded folder that still exists; a
  command that needs one and finds none refuses in one line ("no DMC folder: run dmc install").
- **Who used `Toolkit.Root`** (from `grep`): `PlayCommand` (working directory), `CampaignCommand` (export, import,
  show), `RedrawCommand`, `Digester` (default output `adventures/.transcript-review`), `LogFolder` (Claude Code's log
  folder is named after the project path, now the DMC folder's) and `StaleBinaryCheck` (the pin file).
- **`dmc folder`** (new, one line): prints the folder `DmcFolder.Find()` resolves. The developer skills
  `review-feedback` and `review-transcript` take a DMC folder path as an argument and default to `dmc folder` (FR-018).
  `dmc digest` takes `--folder` and defaults the same way.

## R11. Feedback

- **Decision:** `dmc-record-feedback` and the feedback template add a `dmc:` key holding the version number alone (`dmc: 0.2.0`, from `dmc --version` without the leading `dmc `)
  (FR-017). Entries are written only under `feedback/`, which the guard (R4) never touches. `review-feedback` stays in
  the repository's `.claude/skills/`; it reads entries from a DMC folder and changes `kit/` in the repository; players
  get the change by `dmc update`. A play session is denied writes to the generated files (R6), so there is no local
  tuning to lose. Sending feedback to developers is a later issue (spec).

## R12. What is removed

- **Decision:** `play.ps1`, `play.sh`, `tools/get-dmc.ps1`, `tools/get-dmc.sh`, `tools/dmc-release.json`,
  `.github/release/write-pin.sh`, the release workflow's pin job and pin pull request, `StaleBinaryCheck` (and its call
  in `CommandLine`), `Toolkit.Root`, `.claude/play-settings.json`, and the repository's placeholder player folders
  (`adventures/`, `feedback/`, `modules/`, `rules/` with their `.gitkeep` and `feedback/.ignore`; the `.gitignore`
  entries stay so nothing personal is ever added by accident). Their tests go or are rewritten
  (`Distribution/GetDmcTests.cs`, `LauncherTests.cs`, the stale-binary tests, the toolkit tests about launchers and
  placeholders).
- **Decision log:** superseded entries: § .NET 10 Native AOT on every platform (its "players install nothing but Claude
  Code" clause), § The release opens its own pin pull request, § Keep the missing-`dmc` notice at session start (the
  notice stays, reworded to "reinstall DMC"), § Developer builds report `0.0.0-dev` and live in `artifacts/bin` (the
  part about launchers owning `bin/`), § feedback/ has a .ignore file, and the play-block parts of the Spec Kit entries.

## R13. Constitution amendment

- **Decision:** Principle II is redefined: "Toolkit work MUST NOT cost the player context: the rules loaded into every
  play session stay lean. Players install Claude Code and `dmc`, and nothing else." Its sources become
  `.docs/development.md` § Writing prompts and the new decision entry for this route. Version 1.0.1 → **2.0.0**
  (Spec Kit's convention: MAJOR for a principle redefined), ratified date kept, amended date the PR's. The "Workflow"
  line about the toolkit tests is unchanged. Done in Phase 1, in the same PR as the decision entry, as the constitution's
  Governance section requires ("change it in its home file and record the decision in `.docs/decisions.md`").
- **Context (SC-005):** expected smaller than today: the DMC folder's `CLAUDE.md` is today's less the toolkit clause and
  § Git and GitHub, the player skills are unchanged apart from their names, and no Spec Kit or developer skill exists
  there. Measured with `/context` in Phase 1's check in play.

## R14. Developers

- **Decision:** to try a change, a developer edits `kit/`, runs `dotnet publish src/Dmc.Cli -r <rid> -o artifacts/bin`
  (the default build, no AOT toolchain needed), and runs `artifacts/bin/dmc install --path <scratch>` (or
  `refresh <scratch>` on a folder installed before). A development build installs the program only if asked
  (`--force`); by default it writes the folder and leaves the installed release alone, so a developer's own play is
  not replaced by a work-in-progress build.
- **System prompt:** none involved now (R1), so an edited rule takes effect in a new session after the refresh.
- **Alternatives:** a `DMC_KIT` variable that reads files from disk instead of the embedded copy (faster iteration but a
  second code path that releases never use; rejected for now).

## R16. Release trust: immutable releases plus signed checksums (options 3 and 4)

- **Decision (user, 2026-10-24):** layer two measures. Option 3: immutable releases, published only after the
  maintainer approves, with downloads checked against GitHub's reported digest and `SHA256SUMS.txt`. Option 4:
  `SHA256SUMS.txt` signed with the project's private key, verified by `dmc update` with public keys built into `dmc`.
- **Threats covered:** a corrupt download and a swapped release file (both measures); a whole new bad release published
  with a stolen token or account (the signature, as long as the private key is not taken too: it lives only in the
  approval-gated environment); an old release served as latest (`dmc update` only moves forward). The first install
  rests on HTTPS, immutable releases and the hashes (R8), as almost every scripted installer does.
- **Prior art:** HashiCorp signs `SHA256SUMS` (PGP) and hashes the archives; OpenBSD signify and minisign sign with
  Ed25519; Sparkle and WinSparkle build an Ed25519 public key into the app and require a signature on every update; Tauri's
  updater requires a signature and cannot be told to skip it. GitHub immutable releases lock a published release's files
  and tag and generate a signed release attestation; the releases API reports each asset's SHA-256 `digest`.
- **Algorithm:** ECDSA P-256 with SHA-256, DER signature (`openssl dgst -sha256 -sign`), verified with
  `ECDsa.VerifyData(..., HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)`. Built into .NET and AOT-safe;
  .NET 10 has no built-in Ed25519, and a package for it is not worth adding.
- **Keys:** two key pairs, made by the maintainer with `openssl` (human step). The active private key is the secret
  `DMC_RELEASE_SIGNING_KEY` of a GitHub environment `release` that requires the maintainer's approval; the backup private
  key is kept offline by the maintainer and never uploaded. Both public keys are in `src/Dmc.Cli/Install/ReleaseKeys.cs`.
  A release signed by either verifies. If the active key is lost or exposed, the next release is signed with the backup
  (by hand, per `.docs/releases.md`) and ships a new pair of public keys.
- **Release workflow:** the publish job runs in environment `release` (waits for approval), creates the release as a
  **draft**, attaches the archives, `SHA256SUMS.txt`, `SHA256SUMS.txt.sig`, `install.ps1` and `install.sh`, then publishes
  it, so every file is in place before the release locks (GitHub's recommended order for immutable releases). Third-party
  actions are pinned to commit SHAs. The dry run signs with a throwaway key made in the run, to test the steps.
- **Repository settings (human steps):** turn on immutable releases; create environment `release` with the maintainer as
  required reviewer and the key secret; rulesets that protect `main` and the `dmc-v*` tags; two-factor sign-in on the
  maintainer's account.
- **Alternatives:** the checksum pin on `main` (option 2; weaker against a whole new release, since whoever can publish
  can usually merge); keyless Sigstore signing or GitHub artifact attestations (option 5; no mature .NET verifier, and
  `gh` is not on players' machines); TUF (option 6; out of proportion); Authenticode (option 7; declined 2026-10-17).

## R15. Tests

- **Toolkit tests** (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`) read `kit/` instead of `CLAUDE.md`, `.claude/skills/`,
  `templates/` and `.claude/play-settings.json`, and gain: every file under `kit/` is embedded and in the table; the table
  maps nothing into a player folder; every player skill is named `dmc-<name>` and every skill a kit prompt names exists;
  no kit prompt names `./bin/dmc`, `get-dmc`, `play.ps1` or a repository path; the deny rules for the generated files and
  for `dmc update/install/refresh` are present; the campaign allow rules do not start with `/`; the developer
  `CLAUDE.md` holds no Dungeon Master section; the repository's `.claude/skills/` holds no player skill.
- **Command tests:** `InstallTests` (default folder, `--path`, `/dev/tty` fallback to default, refusal inside the
  repository, refusal on a foreign file in the way, player folders created empty and never written), `RefreshTests` (the
  four cases of R4, removal of dropped files, the hand-edit copy, and a hash of every player file before and after),
  `PlayTests` (DMC folder resolution, refresh before start, PATH prepend, fixed arguments, arguments passed through),
  `UpdateTests` (newer, same, older, development build, hash mismatch against the list and against the digest, missing
  signature, bad signature, signature by an unknown key, either built-in key accepted, a new `dmc` that does not run,
  player files unchanged), `ReleaseKeysTests` (both keys parse; neither is a test key),
  `FolderTests`. All through seams that take the folder, the environment and the release source as parameters.
- **Distribution:** `InstallScriptTests` in the style of `GetDmcTests` (local assets, a corrupted archive refused); they
  need PowerShell and bash and follow skip-locally, fail-on-CI (constitution V). `tests.yml` path filters add `kit/**`.
- **Release dry run:** installs each archive with the bootstrap from local assets, runs `dmc install --path` into a
  temporary folder, checks the generated files and the manifest, then rolls, draws a map and renders a PDF page as today.
