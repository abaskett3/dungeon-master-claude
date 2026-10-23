# Contract: `get-dmc` on macOS

`tools/get-dmc.sh` and `tools/get-dmc.ps1` are the interface a player's launcher (`play.sh`, `play.ps1`) calls. This
contract covers only what changes: the answer on macOS and on systems that are not recognized. Everything else,
including the output on Windows and Linux, is unchanged.

## Refusal on macOS

| | `get-dmc.sh` | `get-dmc.ps1` |
|---|---|---|
| Detected by | `uname -s` prints `Darwin` | `$IsMacOS` is true (or, for the tests only, `DMC_GET_TEST_SYSTEM` is `macOS`) |
| stderr | `get-dmc: dmc is not supported on macOS: it runs on Windows and Linux` | the same line |
| stdout | empty | empty |
| Exit code | 1 | 1 |
| Side effects | none: no `bin/`, no temporary folder, no network | none |
| Checked before | reading the pin, `curl`, `tar`, or any hash tool | reading the pin, any download |

Both scripts check the system first, so the answer does not depend on the pin, the network or the processor
(Apple Silicon and Intel get the same answer).

## Other systems that are not recognized

stderr `get-dmc: dmc is not built for <system>: it runs on Windows and Linux` (bash: `<system>` is `uname -s`, or
`this system` if empty; PowerShell: `this system`). Exit code 1, nothing written. Only the list at the end
changed (it said "Windows, Linux and macOS").

## What the launchers do with a refusal

Unchanged: `play.sh` and `play.ps1` print the one line and carry on, starting Claude Code without `dmc`, as they do
for any `get-dmc` failure (an offline machine, say; read in `play.sh` and `play.ps1`: a `bin/dmc` that still runs,
such as an older Mac install, keeps getting the session). No launcher change is part of this feature.

## Test hook

`DMC_GET_TEST_SYSTEM` (PowerShell script only, tests only; documented in its header beside `DMC_GET_TEST_ASSETS` and
`DMC_GET_TEST_MACHINE`). When set to `macOS`, the script behaves as if `$IsMacOS` were true. Any other value or none:
the script reads the real system.
