# Quickstart: validating the removal of macOS support

Run these from the repository root on Windows or Linux after the change. See [contracts/get-dmc-refusal.md](contracts/get-dmc-refusal.md)
for the exact refusal and [data-model.md](data-model.md) for the manifest.

## 1. No macOS left in the code, scripts, workflows or pin

```bash
rg -n -i 'mac ?os|osx|darwin|apple|\bmac\b' src tests tools .github README.md
```

Expected: matches only in the refusal itself (`tools/get-dmc.sh`, `tools/get-dmc.ps1`, the test that proves it)
and in a comment that explains the refusal. No `osx`, `macos-latest`, `IsMacOS`, `Rosetta` or `Security framework`.

```bash
jq '.sha256 | keys' tools/dmc-release.json
```

Expected: `["linux-arm64","linux-x64","win-arm64","win-x64"]`.

## 2. Documents

```bash
rg -n -i 'mac ?os|osx|darwin|apple|\bmac\b|gatekeeper' .docs README.md
```

Expected: every match is in `.docs/decisions.md` or `.docs/roadmap.md` and is marked as history/superseded, except
the one-line statement in `.docs/user-guide.md` and `.docs/releases.md` that macOS is not supported (if kept).

## 3. Build and tests

```bash
dotnet build tests/Dmc.Tests -c Release
dotnet test tests/Dmc.Tests -c Release
```

Expected: the build has no warnings (warnings are errors; in particular no CA1416), and all tests pass. The new
refusal test runs for both `bash` and `pwsh` and asserts the one-line message, exit code 1 and no `bin/` folder.

## 4. The refusal by hand

```bash
mkdir /tmp/fake && printf '#!/bin/sh\ncase "$1" in -s) echo Darwin ;; -m) echo arm64 ;; esac\n' > /tmp/fake/uname && chmod +x /tmp/fake/uname
PATH=/tmp/fake:$PATH bash tools/get-dmc.sh; echo "exit $?"
DMC_GET_TEST_SYSTEM=macOS pwsh -NoProfile -File tools/get-dmc.ps1; echo "exit $?"
```

Expected for both: `get-dmc: dmc is not supported on macOS: it runs on Windows and Linux` on stderr and `exit 1`;
no `bin/` folder created.

## 5. Release and CI (needs GitHub)

- Push the branch: the Tests workflow shows two `dotnet (...)` jobs (ubuntu-latest, windows-latest) and no macOS one.
- The Release workflow starts by itself as a dry run on a PR that touches `release.yml`, `tools/` or `.github/release/` (or run it by hand with `workflow_dispatch`): it builds four archives, `write-pin.sh` writes four keys, and the pin
  check passes with `length == 4`. Earlier releases (`dmc-v0.1.4` and before) still list their `osx-arm64` archive.
