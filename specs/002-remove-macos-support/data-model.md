# Data Model: Remove macOS Support

The feature changes one data file and no stored user data. Campaigns, modules and the rules reference are not touched.

## Release manifest (`tools/dmc-release.json`)

| Field | Meaning | Rule |
|---|---|---|
| `about` | One line saying what the file is | Kept as it is by `write-pin.sh` |
| `tag` | The pinned release, `dmc-vX.Y.Z`; empty means none yet | Unchanged |
| `sha256` | One entry per supported system: runtime identifier -> 64-digit lowercase SHA-256 of that archive | After this change: exactly `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`. No `osx-*` key. One key per line (`get-dmc.sh` reads without a JSON parser). |

**Validation**: `write-pin.sh` writes only those four keys and fails if its folder holds a `.sha256` file for any
other archive; the release workflow checks `(.sha256 | length) == 4`.

**State transition**: the existing pin (`dmc-v0.1.4`, five hashes) loses its `osx-arm64` line in this change. The next
release's pin pull request writes the four-key form for the new tag.

## Supported system

A runtime identifier is the name of a system: `win-x64`, `win-arm64`, `linux-x64` or `linux-arm64`. It names the
archive (`<tag>-<rid>.zip` for Windows, `<tag>-<rid>.tar.gz` for Linux), the CI/release matrix row, and the key in the
manifest. `osx-arm64` is removed from all three; `osx-x64` was never built.

## Release archive

Unchanged in form: four files at the top level (`dmc`/`dmc.exe` and three native libraries) plus a `.sha256` file.
Releases already published, including `dmc-v0.1.4`'s `osx-arm64` archive, are not modified or withdrawn.

## Decision-log entry

An entry has Date, Decision, Reason, Limits and Source. The two entries that mention macOS (`Intel Macs not built`,
and the Gatekeeper/SmartScreen decision) gain a `superseded 2026-10-22 by <the new entry>` marker; the new entry
records the removal, the reason, and the limit that existing Mac installs keep working but get no updates.
