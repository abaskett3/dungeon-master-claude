# Quickstart: Printable character sheets, `dmc sheet` (phase 1)

1. `dotnet publish src/Dmc.Cli -r <rid> -o artifacts/bin`, then `dotnet test`: the `Sheets/` tests, the sheet rows of
   `ErrorPathTests` and `Playwright/SheetPageTests` pass.
2. On a copy of a real campaign: `artifacts/bin/dmc sheet --check --party --folder <copy>` lists what to fix; fix it,
   then `artifacts/bin/dmc sheet --party --open --folder <copy>` and print the page from the browser.
3. `git diff origin/main -- kit/dm-rules.md` is empty.
4. On the maintainer's computer: the slip test of the spec (fork points A and B, 5 runs per point per arm, control and
   treatment at the same time), reported against its pass line in the PR.
