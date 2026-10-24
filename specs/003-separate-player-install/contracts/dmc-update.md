# Contract: `dmc update`

Replaces the program in place with the latest release, then rewrites the generated files (FR-014). Research R9;
states in [data-model.md](../data-model.md#program-states-update-research-r9).

## Behaviour

1. A development build (`0.0.0-dev`) refuses at once. Leftovers from an earlier update are removed if possible.
2. Read the latest release of `abaskett3/dungeon-master-claude` from the GitHub API (HTTPS); its tag is `dmc-vX.Y.Z`.
3. If `X.Y.Z` is not newer than this `dmc`, say so and stop (the generated files are still refreshed if stale).
4. Download `SHA256SUMS.txt` and `SHA256SUMS.txt.sig` and verify the signature with the public keys built into `dmc`
   (research R16). Download the archive for this machine (`win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`): 15
   seconds to connect, give up when no data arrives for 30 seconds, 10 minutes at most (`get-dmc`'s limits today). Its SHA-256 must match the signed list and the `digest` the API reports for the asset, before anything
   is unpacked.
5. Unpack into `<program folder>/.update-<random>/`; check it holds `dmc` and three libraries; run the new
   `dmc --version` and check it says `dmc X.Y.Z`.
6. Swap: rename each old file to `*.old` (a running `dmc.exe` can be renamed, not deleted), move the new ones in. A
   failure here moves back what was moved.
7. Run the new `dmc refresh --all` and pass its lines through.

Before step 6 nothing changes. Player files are never touched (the refresh writes only generated files).

## Output (stdout)

| Case | Output | Exit |
|---|---|---|
| Updated | `dmc update: installed dmc X.Y.Z (was A.B.C)`, then the refresh's lines | 0 |
| Already latest | `dmc update: dmc X.Y.Z is already the latest` | 0 |

## Errors (one line on stderr, `dmc update: <message>`, exit 1)

| Case | Message |
|---|---|
| Run inside a Claude Code session (`CLAUDECODE` set; research R6) | `cannot run inside a Claude Code session: run it in a terminal` |
| Development build | `this is a development build (0.0.0-dev): install a release to update` |
| No network, API or download fails or stalls | `could not reach GitHub: <reason>` |
| No archive for this machine | `the latest release has no archive for <rid>` |
| Signature missing | `release X.Y.Z has no signature: nothing was installed` |
| Signature invalid or by an unknown key | `release X.Y.Z is not signed by the DMC release key: nothing was installed` |
| Hash mismatch | `the download does not match the signed checksums (or GitHub's digest): nothing was installed` |
| New `dmc` does not run | `the new dmc does not run: nothing was installed` |
| Program folder not writable | `cannot write <folder>: <reason>` |
| Refresh after the swap fails | `installed dmc X.Y.Z, but the DMC folder was not refreshed: <reason>; dmc play will refresh it` |

## Play settings

The generated `.claude/settings.json` denies `Bash(dmc update*)` and `PowerShell(dmc update*)` (and `install`,
`refresh`): a play session never changes the program or files it is running on.

## Test seam

`DMC_UPDATE_TEST_ASSETS=<folder>`: the release is a local folder (`latest.json` naming the tag and asset digests, the
archives, `SHA256SUMS.txt` and its `.sig`). The signature and hash checks still apply with the built-in keys; nothing can
switch them off or change the keys in a release build. The success path is tested in-process with a test key passed as a
parameter.
