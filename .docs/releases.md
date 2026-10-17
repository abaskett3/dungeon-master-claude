# Releases

This document is for the maintainer who cuts a `dmc` release, and for anyone who needs to know how players get `dmc` and what to do when a release goes wrong.

## How `dmc` reaches players

Players never build `dmc`. A release is published on GitHub from a tag; a pull request then pins it in `tools/dmc-release.json` (its tag and the SHA-256 of each archive); and once that pull request is merged, `get-dmc` installs exactly that release on each player's computer the next time a launcher runs. The pin is the only thing that decides which `dmc` players run.

```
tag dmc-vX.Y.Z on main -> release.yml builds, checks, publishes -> "Pin dmc X.Y.Z" PR -> merged -> get-dmc installs it
```

## Platforms

| Runtime identifier | Built on | Archive |
|---|---|---|
| `win-x64` | `windows-latest` | `dmc-vX.Y.Z-win-x64.zip` |
| `win-arm64` | `windows-11-arm` | `dmc-vX.Y.Z-win-arm64.zip` |
| `linux-x64` | `ubuntu-22.04` | `dmc-vX.Y.Z-linux-x64.tar.gz` |
| `linux-arm64` | `ubuntu-22.04-arm` | `dmc-vX.Y.Z-linux-arm64.tar.gz` |
| `osx-arm64` | `macos-latest` | `dmc-vX.Y.Z-osx-arm64.tar.gz` |

Each is built with Native AOT on its own platform's runner, since AOT does not cross-compile between operating systems. Linux is built on Ubuntu 22.04, the oldest Ubuntu runner GitHub offers, because an AOT binary runs only on the same or a newer Linux than it was built on, and building on a newer system could raise the glibc it needs. Ubuntu 22.04 ships glibc 2.35, but the floor for players is glibc 2.34: the newest glibc symbol version that `dmc` and its native libraries actually use, as measured in issue #3's Phase 0 spike. Intel Macs (`osx-x64`) are not built. Each archive holds four files at the top level and no folder: `dmc` (`dmc.exe`) and the native libraries for SkiaSharp, HarfBuzzSharp and PDFium. The release also carries `SHA256SUMS.txt`.

## Cutting a release

### Once per repository

The pin job opens a pull request, which GitHub allows only when **Settings → Actions → General → Workflow permissions → "Allow GitHub Actions to create and approve pull requests"** is ticked. It is ticked in this repository. Alternatively, a repository secret `DMC_PIN_PR_TOKEN` (a token that may push and open pull requests) is used when present.

### Each release

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
   - `build`, once per platform: publishes with `-p:PublishAot=true -p:Version=X.Y.Z`, packs the four files (failing if there are not exactly four), writes the archive's SHA-256, then installs that archive on the same runner with `get-dmc.sh` and with `get-dmc.ps1` under PowerShell 7. Each must print the install line once and then stay silent. It runs `bin/dmc --version` (which must print `dmc X.Y.Z`), `roll`, `map --png` and `pdf render` from `bin/`, and checks that a corrupted copy of the archive is refused with `bin/` left empty.
   - `release` publishes the GitHub Release with the five archives and `SHA256SUMS.txt`, then downloads the archives back from the release and hashes them, so the pin is written from what is really published.
   - `pin` writes `tools/dmc-release.json` with `write-pin.sh` (one key per line, because `get-dmc.sh` reads it without a JSON parser), pushes branch `pin-dmc-vX.Y.Z`, and opens the pull request **"Pin dmc X.Y.Z"**, which changes only that file.
4. **Run CI on the pin pull request.** A pull request opened with the workflow's own token does not start other workflows. Close and reopen it to start the checks; on both pin pull requests so far (#17 and #21) the first runs ended at once with no jobs, and the runs after reopening passed. Because the pin is under `tools/`, the release workflow's dry run also runs on it.
5. **Merge the pin pull request** once green. Players get the new `dmc` the next time a launcher runs `get-dmc`; a session started some other way gets the stale-binary warning, which names the `get-dmc` command.

A prompt change that needs the new `dmc` (a new subcommand, say) should merge only together with its release: merge the change, tag, and merge the pin straight away. Until the pin is merged, players run the previous release.

### Dry runs

A pull request that changes the release files or the build settings every release build uses (`Directory.Build.props`, `Directory.Packages.props`, `global.json`, `Dmc.Cli.csproj`), or a manual run from the Actions tab (`workflow_dispatch`), runs the same workflow as a dry run with version `0.0.0`: it builds, packs, hashes and installs every archive and uploads the pin it would propose as an artifact, but publishes no release, pushes nothing and opens no pull request.

## How `get-dmc` installs and verifies

`tools/get-dmc.sh` (bash) and `tools/get-dmc.ps1` (Windows PowerShell 5.1 or PowerShell 7) do the same thing. On Windows, `get-dmc.sh` hands over to `get-dmc.ps1` through `powershell.exe -ExecutionPolicy Bypass`, unless `bin/dmc.exe` is already current.

1. **Pick the archive for this computer.** bash reads `uname -s` and `uname -m` (a Rosetta shell on Apple Silicon is detected and gets `arm64`); PowerShell reads the machine's processor type from the Windows registry, not from the process (an emulated x64 shell on an Arm PC would otherwise get the wrong archive), or the operating system's architecture elsewhere. Unsupported systems get one line: `dmc is not built for Intel Macs: on macOS it needs Apple Silicon`, and similar.
2. **Read the pin.** The tag must be `dmc-vX.Y.Z`, and there must be a 64-digit SHA-256 for this platform.
3. **Do nothing if current.** If `bin/dmc --version` prints `dmc X.Y.Z`, exit 0 and print nothing. Old `*.old` copies left by a Windows replacement are deleted on any run.
4. **Download** `https://github.com/abaskett3/dungeon-master-claude/releases/download/<tag>/<archive>` into a temporary folder inside `bin/`, over HTTPS only. An offline or stalled network gives up within seconds to tens of seconds, so the launcher can start without `dmc`; a slow download that keeps moving may take up to 10 minutes.
5. **Check the SHA-256** against the pin before unpacking or running anything. On a mismatch nothing is installed.
6. **Unpack and run** the new `dmc --version` in the temporary folder; it must print the tag's version. A program that cannot run here (too old a glibc, say) is never installed.
7. **Install**: move the files into `bin/`, `dmc` last. A `dmc.exe` that is running in another session cannot be deleted on Windows, so it is renamed to `*.old` first.
8. Print `get-dmc: installed dmc X.Y.Z (<rid>) in bin/`.

Every failure is one line on stderr starting `get-dmc:`, with exit code 1. The launchers then print their own line and start Claude Code without `dmc`.

## When a release fails

| What failed | What to do |
|---|---|
| `prepare`: the tag is not `dmc-vX.Y.Z`, or not on `main` | Nothing was built or published. Push a correct tag on a commit of `main`. |
| `build` on one platform | Nothing is published unless every build passes. If it was a runner fault, use **Re-run failed jobs**. If the code needs fixing, fix it on `main` through a pull request and release under the next version number rather than moving a pushed tag. |
| `release`: the release exists but an archive is missing (a half-finished upload) | The job says so. Delete the release but keep the tag (`gh release delete <tag> --yes`), then re-run the workflow. |
| `pin`: the pull request could not be opened (the Actions setting was off, say) | Fix the cause and use **Re-run failed jobs** or **Re-run all jobs**. A re-run keeps a release that is already published (its archives are what players download, so the pin is written from them), force-pushes its own `pin-<tag>` branch, and reuses an open pin pull request. If `main` already pins the tag, it stops with a notice. |
| The pin pull request shows no checks, or checks that ended at once | Close and reopen it. |
| Players report `does not match its SHA-256` | The pin does not match the files on the release. Nothing was installed, so players fall back to their previous `dmc` or play without it. Check that `tools/dmc-release.json` on `main` came from the pin pull request of that release. |

## History

| Release | Tag on | Published (UTC) | Pinned by | What it contained |
|---|---|---|---|---|
| 0.1.0 | `9e0e007`, the merge of PR #15 | 2026-10-16 | PR #17, merged 2026-10-17 | The first release: `roll`, `context-check`, `map`, `pdf`, `digest list` and `digest digest`, and `play`, from phases 1 to 5 of issue #3. Pinning it was what allowed PR #16 (the switch-over, which deleted the Python scripts) to merge. |
| 0.1.1 | `3ef8280`, the merge of PR #18 | 2026-10-17 | PR #21, merged 2026-10-17 | `dmc digest digest` renamed to `dmc digest show` (PR #18), which the `review-transcript` skill already called; developer builds report `0.0.0-dev` and the toolkit root is found from `artifacts/bin` (PR #20); the stale-binary warning names the `get-dmc` forms that work under any execution policy, and the digest recognises `dmc roll` as a dice roll (PR #16). |
| 0.1.2 | `e312f80`, the merge of PR #25 | 2026-10-17 | PR #26, merged 2026-10-17 | Issue #6 Phase 2: `dmc roll "label:expr@target"` prints a line ready to paste, with hit or miss against an AC (a natural 20 hits, a natural 1 misses) and success or fail against a DC, plus `dmc roll --help`; `live.json` takes `initiative` and `current` and `dmc map` draws the turn order (PR #25). Also the code documentation and its build checks (PR #24). |

Every release was built, installed and smoke-tested on all five platforms by the workflow before they were published. No release so far has needed a re-run of its tag workflow.
