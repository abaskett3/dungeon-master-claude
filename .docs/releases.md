# Releases

This document is for the maintainer who cuts a `dmc` release, and for anyone who needs to know how players get `dmc`, how a release is trusted, and what to do when a release goes wrong.

## How `dmc` reaches players

Players never build `dmc`. A release is published on GitHub from a tag, and players get it two ways:

- **The install line** (first install). The README gives one line per system, which downloads `tools/install.ps1` or `tools/install.sh` from the latest release. The script picks the archive for the computer, downloads it and the release's `SHA256SUMS.txt`, checks the archive against both that list and the `digest` GitHub's releases API reports for it, unpacks it into a temporary folder, checks that the new `dmc` runs, and hands over to `dmc install`, which installs the program and writes the player's DMC folder.
- **`dmc update`** (every later change). It reads the latest release, checks the signature of `SHA256SUMS.txt` against the public keys built into `dmc`, checks the archive against the signed list and the API's digest, swaps the program in place, and has the new program rewrite the generated files of every DMC folder.

```
tag dmc-vX.Y.Z on main -> release.yml builds, installs and checks every archive
                       -> you approve the environment "release" -> the list is signed, the draft is made and published
                       -> players: the install line, or dmc update
```

The generated files (the rules, skills, settings and templates) are inside `dmc`, so a change to `kit/` reaches players only with a release, and a refresh of their DMC folder (`dmc update`, or the refresh `dmc play` does before it starts).

## Platforms

| Runtime identifier | Built on | Archive |
|---|---|---|
| `win-x64` | `windows-latest` | `dmc-vX.Y.Z-win-x64.zip` |
| `win-arm64` | `windows-11-arm` | `dmc-vX.Y.Z-win-arm64.zip` |
| `linux-x64` | `ubuntu-22.04` | `dmc-vX.Y.Z-linux-x64.tar.gz` |
| `linux-arm64` | `ubuntu-22.04-arm` | `dmc-vX.Y.Z-linux-arm64.tar.gz` |

Each is built with Native AOT on its own platform's runner, since AOT does not cross-compile between operating systems. Linux is built on Ubuntu 22.04, the oldest Ubuntu runner GitHub offers, because an AOT binary runs only on the same or a newer Linux than it was built on, and building on a newer system could raise the glibc it needs. Ubuntu 22.04 ships glibc 2.35, but the floor for players is glibc 2.34: the newest glibc symbol version that `dmc` and its native libraries actually use, as measured in issue #3's Phase 0 spike. macOS is not built: releases up to `dmc-v0.1.4` have an `osx-arm64` archive, and it stays on those releases, but no later release has one. Each archive holds four files at the top level and no folder: `dmc` (`dmc.exe`) and the native libraries for SkiaSharp, HarfBuzzSharp and PDFium.

A release carries eight files: the four archives, `SHA256SUMS.txt` (the SHA-256 of each archive), `SHA256SUMS.txt.sig` (its signature) and the install scripts `install.ps1` and `install.sh`.

## How a release is trusted

Two measures are layered (research R16 of `specs/003-separate-player-install/`, and [decisions.md](decisions.md)):

1. **Immutable releases, published only after you approve.** The publish job runs in the GitHub environment `release`, which waits for you. It creates the release as a draft, attaches every file, checks the draft holds exactly the eight files, and only then publishes it, so the release locks (its files and tag cannot be changed) with everything in place. Every download is checked against the SHA-256 GitHub reports for it and against `SHA256SUMS.txt`.
2. **A signed checksum list.** `SHA256SUMS.txt` is signed with the project's private key, which exists only as the secret `DMC_RELEASE_SIGNING_KEY` of that environment. `dmc update` verifies the signature with two public keys built into `dmc` and installs nothing otherwise, so a whole new bad release published with a stolen token or account is refused, as long as the private key is not taken too. An old release served as the latest is refused too: `dmc update` only moves forward.

The install line cannot check the signature (it comes from the same release as everything it would check, and a `dmc` it downloaded cannot vouch for itself). The first install rests on HTTPS, immutable releases and the two hash checks, as almost every scripted installer does.

## Release keys

The keys are two ECDSA P-256 key pairs, the active one and a backup. Make them once, on your own machine (the commands are the same for the backup, with its own name):

```
openssl ecparam -name prime256v1 -genkey -noout -out active.pem
openssl ec -in active.pem -pubout -out active.pub.pem
openssl ecparam -name prime256v1 -genkey -noout -out backup.pem
openssl ec -in backup.pem -pubout -out backup.pub.pem
```

- **The public keys** (`active.pub.pem`, `backup.pub.pem`) go into `src/Dmc.Cli/Install/ReleaseKeys.cs`, in the constants `ActivePublicKey` and `BackupPublicKey`: replace the placeholder text with the whole PEM, header and footer lines included, every line indented as far as the closing quotes. They are public; they are part of the source. `ReleaseKeysTests` fails until both are real keys.
- **The active private key** (`active.pem`) goes into the environment `release` as the secret `DMC_RELEASE_SIGNING_KEY` (the whole file, as text) and nowhere else. Then delete the local copy, or keep it only where you keep secrets.
- **The backup private key** (`backup.pem`) is kept offline, by you, and never uploaded anywhere. It exists only for the recovery below.

Never paste a private key into a chat, an issue, a pull request or a commit. The publish job checks that the key in the secret is one of the two public keys written in `ReleaseKeys.cs` before it publishes anything, so a mismatched secret fails the job, not the players.

## Once per repository

These settings are the maintainer's alone; the workflow cannot make them.

1. **The environment `release`.** Settings → Environments → New environment, named `release`. Add yourself as a required reviewer, and add the secret `DMC_RELEASE_SIGNING_KEY` (the active private key). Without the environment's protection the publish job would run unattended: check that the job waits for you the first time.
2. **Immutable releases.** Settings → General → Releases → "Enable release immutability". The publish job reports whether the release it published is immutable, and warns when it is not.
3. **Rulesets.** Settings → Rules → Rulesets: one that protects `main` (changes through a pull request, no force pushes, no deletion), and one that protects the tags `dmc-v*` (only you may create them; no updates, no deletion).
4. **Two-factor sign-in** on your GitHub account.

## Cutting a release

1. **Merge what the release should contain** into `main`, with CI green. Nothing in the repository holds the version; the project file stays at `0.0.0-dev`.
2. **Tag a commit of `main` and push the tag:**

   ```
   git fetch origin
   git tag dmc-vX.Y.Z origin/main
   git push origin dmc-vX.Y.Z
   ```

   The tag must be `dmc-v` followed by three numbers, on a commit that is on `main`. Pushing a tag is the only thing that publishes a release, and it is the maintainer's call.
3. **The workflow runs** (`.github/workflows/release.yml`):
   - `prepare` checks the tag's form and that it is on `main`, and takes the version from the tag.
   - `build`, once per platform: publishes with `-p:PublishAot=true -p:Version=X.Y.Z`, packs the four files (failing if there are not exactly four), writes the archive's SHA-256, then installs that archive with the install line from local assets (`DMC_INSTALL_TEST_ASSETS`) into a temporary DMC folder and home folder: `install.sh` and `install.ps1` under PowerShell 7 on Linux, `install.ps1` under PowerShell 7 and Windows PowerShell 5.1 on Windows. It checks the DMC folder (the generated files, a manifest naming `X.Y.Z`, empty `adventures/`, `feedback/`, `modules/` and `rules/`, nothing else), the installed program (`--version` prints `dmc X.Y.Z`; on Windows its folder is on the user's `Path`; on Linux `~/.local/bin/dmc` is a link), that a refresh straight after the install has nothing to do, and that the installed `dmc` runs `roll`, `map --png` and `pdf render` (so the native libraries came along). It checks that a corrupted archive is refused with no DMC folder made, and that `dmc update`, run on the build, refuses a newer release signed by a throwaway key and one with no signature.
   - `publish` (tags only) waits for your approval of the environment `release`; then signs the checksum list with `.github/release/sign.sh`, creates the draft release, attaches the eight files, checks the draft, publishes it and says whether it is immutable.
4. **Approve the environment.** GitHub emails you; open the run, review the deployment to `release`, and approve it.
5. **Check the release page:** four archives, `SHA256SUMS.txt`, `SHA256SUMS.txt.sig`, `install.ps1`, `install.sh`, marked immutable. Try the install line on a clean folder, and `dmc update` from the previous release.

A prompt change that needs the new `dmc` (a new subcommand, say) should merge only together with its release: merge the change, tag, and approve the release straight away. Until players run `dmc update`, they run the previous release, and a session never runs on rules from another version: `dmc play` refreshes the DMC folder before it starts Claude Code.

### Dry runs

A pull request that changes the release files or the build settings every release build uses (`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `Dmc.Cli.csproj`), or a manual run from the Actions tab (`workflow_dispatch`), runs the same workflow as a dry run with version `0.0.0`: it builds, packs, hashes, installs and checks every archive, and the `dry-run` job signs the checksum list with `sign.sh` using a throwaway key made in the run (never the project's, and the secret is not used). It uploads the archives, the list, the signature and the install scripts as workflow artifacts, but publishes no release.

## How the install line and `dmc update` verify

`tools/install.sh` (POSIX `sh`, for `curl ... | sh`; it refuses on macOS, and on Windows tells the player to use the PowerShell line) and `tools/install.ps1` (Windows PowerShell 5.1 or PowerShell 7; under `iex` it never ends the player's session) do the same:

1. **Pick the archive for this computer** (`uname -s` and `-m`, or the machine type from the Windows registry, not from the process).
2. **Download** `SHA256SUMS.txt` from the latest release, find this computer's archive in it, and read that archive's `digest` from `https://api.github.com/repos/abaskett3/dungeon-master-claude/releases/latest`. Then download the archive: HTTPS only, 10 to 15 seconds to connect, no data for 30 seconds ends it, 10 minutes at most.
3. **Check the SHA-256** of the archive against both, before unpacking or running anything. If GitHub's API refuses the request because of its limit for requests without sign-in (60 an hour per address), only the list is checked and one line on stderr says so.
4. **Unpack and run** the new `dmc --version` in a temporary folder, then run its `dmc install` attached to the terminal (with `--path` if given). The temporary folder is removed afterwards.

`dmc update` does the same and more: it refuses a development build and a release that is not newer; it verifies the signature of `SHA256SUMS.txt` with the built-in keys (a missing signature and an invalid one are different errors); it unpacks beside the program (`.update-<random>/`), runs the new `dmc --version`, renames each old file to `*.old` (a running `dmc.exe` can be renamed, not deleted) and moves the new ones in, moving back if anything fails; then it runs the new `dmc refresh --all`. `*.old` and `.update-*` are removed by the next `dmc update`, `install` or `play`. The checks cannot be switched off: no environment variable or setting changes the keys or skips a hash check (the one seam, `DMC_UPDATE_TEST_ASSETS`, changes where a release comes from, not what is checked). The contract is `specs/003-separate-player-install/contracts/dmc-update.md`.

## If the active key is lost or exposed

Existing installations trust the two keys built into the `dmc` they run, so the next release must be signed by a key they trust: the backup key. Do this:

1. **Stop using the active key.** Delete the secret `DMC_RELEASE_SIGNING_KEY` from the environment `release`.
2. **Make a new key pair** with the commands above (call it `new`).
3. **Ship the new keys.** In a pull request, change `ReleaseKeys.cs`: `ActivePublicKey` becomes `new.pub.pem`, `BackupPublicKey` stays `backup.pub.pem` (or becomes a second new pair once the release is out, in a later release signed by `new`). Merge it, and tag the release as usual. The build jobs run; the `publish` job waits for your approval: **reject** it, so that nothing is signed with a missing or wrong secret.
4. **Sign that one release by hand with the backup key**, on the machine that holds it, doing what the `publish` job does:

   ```
   gh run download <run id> --pattern 'dmc-*' --dir artifacts-run
   mkdir release-files
   cp artifacts-run/*/* release-files/
   bash .github/release/sign.sh release-files /path/to/backup.pem src/Dmc.Cli/Install/ReleaseKeys.cs
   gh release create dmc-vX.Y.Z release-files/*.zip release-files/*.tar.gz release-files/SHA256SUMS.txt release-files/SHA256SUMS.txt.sig tools/install.ps1 tools/install.sh --draft --verify-tag --title "dmc X.Y.Z" --notes "..."
   gh release edit dmc-vX.Y.Z --draft=false --latest
   ```

   Existing installations verify it (they trust the backup key), and they now carry `new` as well.
5. **Go back to normal.** Put `new.pem` in the secret `DMC_RELEASE_SIGNING_KEY`, keep `backup.pem` offline, and sign later releases through the workflow.

If the backup key is lost too, installations that have no key in common with a new release cannot update by `dmc update`: they reinstall with the install line.

## When a release fails

| What failed | What to do |
|---|---|
| `prepare`: the tag is not `dmc-vX.Y.Z`, or not on `main` | Nothing was built or published. Push a correct tag on a commit of `main`. |
| `build` on one platform | Nothing is published unless every build passes. If it was a runner fault, use **Re-run failed jobs**. If the code needs fixing, fix it on `main` through a pull request and release under the next version number rather than moving a pushed tag. |
| `publish`: "The secret DMC_RELEASE_SIGNING_KEY is not set", or `sign:` says the signing key is not one built into dmc | Nothing was published. Fix the secret (the key must be `active.pem`, whose public half is in `ReleaseKeys.cs`) or the keys in `ReleaseKeys.cs`, then **Re-run failed jobs** (a fixed `ReleaseKeys.cs` needs a new tag and a new version number). |
| `publish`: the draft does not hold the eight files | Nothing was published. Use **Re-run all jobs**: the job removes the draft an earlier attempt left and makes a whole one. |
| `publish` failed after the release was published | A published release is immutable and is kept as it is: a re-run does nothing. If it is wrong, release the next version number. |
| `publish` says the release is not immutable | The release is published but not locked: turn on immutable releases (once per repository, above); the next release is locked. |
| `dmc update` says a release "is not signed by the DMC release key" | The list was signed with a key that is not in the `dmc` the player runs. See the recovery above, and do not publish more releases signed with it. |
| Players report `does not match the release's SHA-256 checksums` from the install line, or `does not match the signed checksums` from `dmc update` | Nothing was installed. Check the release's files against `SHA256SUMS.txt`; a release published by the workflow cannot differ, so suspect the download (try again) or a tampered release (see the recovery). |

## History

Releases up to 0.1.4 were pinned by a pull request in `tools/dmc-release.json` and installed by `get-dmc` into a repository's `bin/`; both are gone (issue #67). The install line and `dmc update` take a release from the latest published one, checked by hash and signature.

| Release | Tag on | Published (UTC) | Pinned by | What it contained |
|---|---|---|---|---|
| 0.1.0 | `9e0e007`, the merge of PR #15 | 2026-10-16 | PR #17, merged 2026-10-17 | The first release: `roll`, `context-check`, `map`, `pdf`, `digest list` and `digest digest`, and `play`, from phases 1 to 5 of issue #3. Pinning it was what allowed PR #16 (the switch-over, which deleted the Python scripts) to merge. |
| 0.1.1 | `3ef8280`, the merge of PR #18 | 2026-10-17 | PR #21, merged 2026-10-17 | `dmc digest digest` renamed to `dmc digest show` (PR #18), which the `review-transcript` skill already called; developer builds report `0.0.0-dev` and the toolkit root is found from `artifacts/bin` (PR #20); the stale-binary warning names the `get-dmc` forms that work under any execution policy, and the digest recognises `dmc roll` as a dice roll (PR #16). |
| 0.1.2 | `e312f80`, the merge of PR #25 | 2026-10-17 | PR #26, merged 2026-10-17 | Issue #6 Phase 2: `dmc roll "label:expr@target"` prints a line ready to paste, with hit or miss against an AC (a natural 20 hits, a natural 1 misses) and success or fail against a DC, plus `dmc roll --help`; `live.json` takes `initiative` and `current` and `dmc map` draws the turn order (PR #25). Also the code documentation and its build checks (PR #24). |

Every release up to 0.1.4 was built, installed and smoke-tested on all platforms by the workflow before it was published. No release so far has needed a re-run of its tag workflow.
