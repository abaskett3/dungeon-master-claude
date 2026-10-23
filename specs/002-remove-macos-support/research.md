# Research: Remove macOS Support

Every fact below comes from the repository at `origin/main` (787a8b5) on 2026-10-22. There were no
`NEEDS CLARIFICATION` items in the Technical Context; these are the design decisions the spec left open.

## R1. What "support" consists of

- **Decision:** macOS support exists in exactly these places: the release matrix and its `osx-*` packing branch
  (`.github/workflows/release.yml` lines 107-151), the CI matrix (`tests.yml` line 49), the pin's `osx-arm64` hash
  (`tools/dmc-release.json`), the platforms `write-pin.sh` knows (line 44), `get-dmc.sh` and `get-dmc.ps1`
  (Darwin/`$IsMacOS`, Rosetta, Intel check, `shasum` branch), `dmc`'s `open` opener and `macos` platform
  declaration, test cases, comments and documents.
- **Rationale:** A ripgrep for `mac ?os|osx|darwin|osascript|\bmac\b` outside `adventures/`, `modules/`, `rules/` and
  `specs/` finds 62 matches in 28 files; this list accounts for all of them.
- **Alternatives considered:** None; this is an inventory.

## R2. What the setup scripts do on a Mac

- **Decision:** Keep a macOS check in both scripts that stops with one line and downloads nothing:
  `get-dmc: dmc is not supported on macOS: it runs on Windows and Linux`. Other unknown systems keep their
  existing refusal, with the list shortened: `dmc is not built for <system>: it runs on Windows and Linux`.
- **Rationale:** Without the check a Mac user reaches the `osx-arm64` branch, finds no pin entry and sees
  `tools/dmc-release.json has no SHA-256 for osx-arm64`, which reads like a bug. Both scripts already refuse
  unknown systems in one line, so this is a wording change plus one case. It also satisfies FR-006 and SC-004.
- **Alternatives considered:** Dropping every macOS mention (the macOS user then sees the generic "not built for Darwin"
  message in bash and "not built for this system" in PowerShell, with no hint why). Rejected in the spec's
  Assumptions: the generic message does not say macOS was removed on purpose.

## R3. Testing the PowerShell refusal

- **Decision:** `get-dmc.ps1` reads a test-only variable `DMC_GET_TEST_SYSTEM` first in `Get-Platform`; the value
  `macOS` takes the same path as `$IsMacOS`. The bash script is tested as it already is, with a fake `uname`.
- **Rationale:** `$IsMacOS` is read-only and false on the CI machines, so PowerShell cannot be made to lie about
  the system any other way. The script already has `DMC_GET_TEST_ASSETS` and `DMC_GET_TEST_MACHINE`, documented
  "for the tests only" in its header; this is the third of the same kind. Without it, FR-009's test would cover
  only one of the two scripts.
- **Alternatives considered:** Mocking `$IsMacOS` (not possible, constant); testing the PowerShell refusal only on a
  Mac runner (there is no longer one); checking the script text for the message (does not prove the behavior).

## R4. The pin file

- **Decision:** Remove the `osx-arm64` line from `tools/dmc-release.json` in this change, and change the
  release workflow's check `(.sha256 | length) == 5` to `== 4`. Keep the tag `dmc-v0.1.3`.
- **Rationale:** FR-004 says the pin must not contain a macOS entry. Nothing reads the removed hash once the
  script refuses macOS first. `ThePinIsReadableAndTheProjectsVersionIsTheDevelopmentOne` iterates whatever
  hashes are present, so it needs no change. The release 0.1.3 itself, with its macOS archive, stays published
  (FR-012); only the pin stops naming it. `write-pin.sh` already fails if a `.sha256` for an unknown platform is in
  its folder (`found` versus `entries`), so a stray macOS hash cannot get back in.
- **Alternatives considered:** Leave the entry until the next release rewrites the file (rejected: it leaves a
  pin that names an archive the toolkit says it does not support, for an unknown time); delete the macOS archive
  from release 0.1.3 (rejected: FR-012, and it gives nothing back).

## R5. Source code

- **Decision:**
  - `SupportedPlatforms.cs`: delete the `macos` attribute and change the comment to two platforms. The build is the
    check that CA1416 stays quiet (PDFtoImage declares `windows`, `linux`, `macos` and others; an assembly that supports a
    subset is accepted). If it warns, the fix is to scope the call, not to restore `macos`.
  - `Browser.cs`: `sh` runs `xdg-open`; the doc comments name Windows and Linux. Windows still shell-executes.
  - Comments in `RandomBytes.cs`, `PdfFile.cs`, `PlayCommand.cs`, `ContextCheckCommand.cs`,
    `CampaignImporter.cs` and `Dmc.Cli.csproj` lose the macOS clause. `PdfFile.Open`'s behavior does not change: it already asks about
    OpenSSL only on Linux.
- **Rationale:** FR-008. Each is a dead branch or a sentence that would mislead. `OperatingSystem.IsMacOS()` is the
  only macOS conditional in the code.
- **Alternatives considered:** Leave the comments (rejected, SC-006); leave `macos` in the attribute list as a
  harmless allowance (rejected: the spec says no platform declaration remains, and it would tell the analyzer that
  code is fine on a system nobody tests).

## R6. Tests

- **Decision:**
  - `GetDmcTests.BashPicksTheArchiveForThisSystem`: delete the two `Darwin` rows; add `Darwin` as the refusal
    case in a new test that also runs the PowerShell script with `DMC_GET_TEST_SYSTEM=macOS`, asserting the message, exit code 1, and that no
    `bin/` was created. Remove `osx-arm64` from `JunkReleases`' rid list.
  - `Bootstrap.Rid`: drop the `IsMacOS() ? "osx"` branch.
  - `BrowserTests`: the opener is always `xdg-open` off Windows.
  - Comment-only changes in the three other test files.
- **Rationale:** FR-009, SC-005. The refusal test uses no tool that might be absent, so Constitution V (skip locally,
  fail on CI) does not arise.
- **Alternatives considered:** A runtime guard that skips the macOS test on non-Macs (not needed: the test pretends).

## R7. Documents

| File | Change |
|---|---|
| `README.md` | Line 8 (systems), line 28 (launcher), line 192 (rid list) |
| `.docs/user-guide.md` | Line 16 (systems table), line 27 (launcher table) |
| `.docs/product-overview.md` | Line 54 (unsigned/Gatekeeper/Intel sentence) |
| `.docs/architecture.md` | Line 20 (tests and CI) |
| `.docs/development.md` | Lines 10, 34, 89 (toolchain, rids, CI matrix) |
| `.docs/releases.md` | Platform table, the paragraph under it, and step 1 under "How get-dmc installs" |
| `.docs/decisions.md` | Mark "Intel Macs not built" and the Gatekeeper decision (line 99) as superseded; add a new entry |
| `.docs/roadmap.md` | Line 56 (Phase 0 spike) stays as history; add this change under Done; adjust the "Gatekeeper and SmartScreen" line under Skipped |
| `tools/get-dmc.sh`, `tools/get-dmc.ps1`, `.github/release/write-pin.sh`, `.github/workflows/*.yml` | Header comments and release-note wording |

- **Rationale:** `CLAUDE.md` § Git and GitHub requires a header comment on every changed script and workflow and
  the right `.docs/` pages. The decision log has a precedent for marking an entry superseded in place
  (`decisions.md` line 296, "superseded 2026-10-22 by ...").
- **Alternatives considered:** Rewriting history in the decisions and roadmap (rejected: they are a log; the spec's
  Edge Cases keep them as marked history).

## R8. Release notes

- **Decision:** The next release's notes are generated by `release.yml` and say "built with Native AOT for Windows and
  Linux." That is the only announcement, plus the decision entry. No GitHub issue is opened for it by this change.
- **Rationale:** A user of an existing 0.1.3 Mac install loses nothing; they only stop getting updates, and the
  next pin PR is the first place they would notice.
- **Alternatives considered:** A separate deprecation notice release (rejected: the toolkit has no mailing list or
  channel for it, and a release that exists only to say this would itself have to be built).
