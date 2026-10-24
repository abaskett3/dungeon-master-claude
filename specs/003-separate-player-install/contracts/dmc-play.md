# Contract: `dmc play [claude arguments...]`

Starts a play session in the DMC folder (FR-011, FR-015). Changes `src/Dmc.Cli/Commands/PlayCommand.cs`. Research R1,
R5, R10.

## Behaviour

1. **Leftovers** from an earlier update are removed quietly if possible.
2. **The folder:** the current folder if it is a DMC folder, else the first folder in `dmc.json` that still exists.
3. **Refresh** it if its manifest's version is not this `dmc`'s, or a generated file is missing or differs (the
   refresh's own lines are printed; see [dmc-refresh.md](dmc-refresh.md)).
4. **Find Claude Code** as today (`FindClaude`: `claude.exe`, then the `claude.ps1` shim through PowerShell, never
   `claude.cmd`).
5. **Start it** in the DMC folder, with the child's `PATH` set to the program folder, the path separator, then the
   inherited `PATH`, and these arguments:

   ```text
   [shim prefix] --model sonnet --effort high [every argument given to dmc play, unchanged]
   ```

   The rules, skills and settings load because the folder is a Claude Code project (research R1); no `--settings` is
   passed.
6. Ignore Ctrl+C while Claude Code runs (as today), wait, and exit with Claude Code's exit code.

## Errors (one line on stderr, `dmc play: <message>`, Claude Code not started)

| Case | Message | Exit |
|---|---|---|
| No DMC folder here or recorded | `no DMC folder: run dmc install` | 1 |
| The refresh fails | the refresh's message | 1 |
| Claude Code not on PATH | `Claude Code (claude) is not on the PATH: install it, then run this again` | 127 |
| Only `claude.cmd` found | as today, without the `./play.ps1` suggestion | 1 |

## Test seam

`Run(args, launch, start)` keeps its shape; the folder, the program folder and the environment are passed in, so a
test records the `ProcessStartInfo` (working directory, arguments, `PATH`) without starting anything.
