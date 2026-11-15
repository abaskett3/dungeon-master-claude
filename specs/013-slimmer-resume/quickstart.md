# Quickstart: A Slimmer Resume

## Build and test

```text
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
dotnet test -c Release
```

## 1. The sizes (SC-001 to SC-004)

Copy Rorn's Landing from `D:\Claude\dmc` (read only) into a scratch DMC folder, migrate it to standard 6 by following
migration 006, then:

```text
artifacts/bin/dmc size --folder <scratch> rorns-landing
artifacts/bin/dmc recall --folder <scratch> rorns-landing --scene
```

Pass: four companions at 4k or less in all, canon at 9k or less, total 50k or less; the size report's canon equals the
always-read sections plus the scene read's bytes.

## 2. The scene read

On the scratch copy, check that the first lines name the scene's place, people and threads, and that the output
stays within 12,000 bytes.

## 3. The hook (SC-007)

```text
'{"prompt":"We should ask Bruni about the housing","cwd":"<scratch>","transcript_path":"<a log>"}' | artifacts/bin/dmc context-check
```

Pass: a `Canon on Bruni` header and at most 3 lines; the same input against a log that holds that header prints
nothing; a prompt with no registered name prints nothing; under half a second.

## 4. Migration 006

`dotnet test` runs the golden fixture. Then a live run on the scratch copy: `dmc campaign verify` reports no
unaccounted line (SC-006).

## 5. Headless play test

In `D:\Claude\test-campaign` (record `adventures/.active-campaign` and the campaign first; restore them afterwards;
`.active-campaign` is normally `rorns-landing`). The maintainer refreshes it from the build in their own terminal:
`<worktree>\artifacts\bin\dmc.exe refresh D:\Claude\test-campaign`. Then:

```text
$env:CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0
claude -p "/dmc-resume-campaign" --allowedTools "Bash(dmc:*)" "Bash(dmc *)" "PowerShell(dmc *)"
```

Pass: the resume reads companions to their cards and runs `dmc recall <campaign> --scene`; a second prompt naming a
registered person shows the hook's reminder in the log; the hook's real input carries `prompt` and `cwd`.

## 6. The baseline

Count the slips recorded in every session log of The Silver Egg and Rorn's Landing in `D:\Claude\dmc` (read only), by
category, into `baseline.md`, and the totals into `.docs/decisions.md`.
