# Quickstart: validating campaign export and import

How to see the feature work end to end once it is built. The command forms and messages are in [contracts/campaign-command.md](contracts/campaign-command.md); the file layout is in [data-model.md](data-model.md).

## Prerequisites

- A developer build: `dotnet publish src/Dmc.Cli -r <rid> -o artifacts/bin` (see `.docs/development.md`).
- At least one campaign under `adventures/`. Work on a copy if it matters: these steps create folders but never change an existing one.

## Automated

```
dotnet test
```

The `Campaign` tests cover the round trip, every refusal in the contract's failure table, the general player options rule and the path rules, against a temporary toolkit root. On CI they run on Linux, Windows and macOS.

## By hand

1. **Export.** `artifacts/bin/dmc campaign export the-silver-egg`
   Expect one line naming `exports/the-silver-egg-<YYYYMMDDHHmm>.zip`, stamped with your local date and time to the minute (for example `the-silver-egg-202610220605.zip`). Run it again within the same minute: the second file ends `-2.zip`. Run it a minute later: a new stamp, no suffix. `git status` shows nothing new (`exports/` is ignored).
2. **Show.** `artifacts/bin/dmc campaign show exports/the-silver-egg-<stamp>.zip`
   Expect two or three lines: name, date, version, file count, module.
3. **Import under a new name.** Rename a copy of the export to `backup.zip` first, to see that the file name does not matter, then `artifacts/bin/dmc campaign import backup.zip --as silver-egg-copy`
   Expect the four-line report with `kept your existing adventures/player-options.md`.
4. **Compare.** The two folders are identical:
   - bash: `diff -r adventures/the-silver-egg adventures/silver-egg-copy && echo same`
   - PowerShell: `Compare-Object (Get-ChildItem -Recurse -File adventures/the-silver-egg | Get-FileHash | % Hash) (Get-ChildItem -Recurse -File adventures/silver-egg-copy | Get-FileHash | % Hash)` prints nothing.
5. **Refusals.** Import the same file again without `--as`: one line, `already exists`, and the folder is untouched. Copy the archive, change one byte in the middle, import it with `--as broken`: one line, `is damaged` or `is not a dmc campaign export`, and no `adventures/broken/`.
6. **Resume.** Start a session and ask DMC to resume `silver-egg-copy`: the recap and present moment match the original's.
7. **Clean up.** Delete `adventures/silver-egg-copy` and the test exports yourself.
