# Quickstart: checking spec 036 by hand

1. Publish and test: `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`, then `dotnet test`.
2. In a scratch DMC folder (`artifacts/bin/dmc install --path <folder>`, in a terminal), copy
   `tests/Dmc.Tests/Fixtures/migrations/011/after/` in as a campaign and make it active.
3. `dmc records show <campaign> hessa --all` prints the sheet with sources; `--card` stops after the play card.
4. `dmc size <campaign>` measures the printed card against the play-card budget.
5. Add `{ "key": "Status", "value": "dead", "source": "setup" }` to the record with Edit in a `dmc play` session:
   `dmc` refuses it (a second value for "Status").
6. Add a History entry "ready on the 14th": refused for want of a month.
7. Migration 011 live run: put `before/` in place at standard 10, start `dmc play`, ask DMC to bring the campaign's
   files up to date, and compare with `after/` (`.docs/development.md` § Changing a campaign-file standard, step 5).
