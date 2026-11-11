# Quickstart: validating the campaign close

**Feature**: `010-campaign-close` | **Date**: 2026-11-11

How to prove the feature works end to end. The command outputs are in
[contracts/campaign-close.md](contracts/campaign-close.md); the files in [data-model.md](data-model.md).

## Prerequisites

- A build: `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin` (or your runtime).
- A scratch DMC folder outside any checkout: `artifacts/bin/dmc install --path <scratch>` in a terminal (not inside a
  Claude Code session).
- A finished test campaign in `<scratch>/adventures/`, at the kit's standard (`dmc campaign standard <c>` prints
  `current`). For scenario 6, a copy of a real finished campaign, never the original.

## 1. The automated tests

```text
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
dotnet test
```

Expected: all pass, including `CampaignCloseTests`, `CloseFailureTests`, `ClosedCampaignFreezeTests`,
`ClosingBackupTests`, `CampaignVerifyClosedTests` and the toolkit tests (research R12).

## 2. The plan writes nothing

```text
dmc campaign close <c>
```

Expected: one `would close ...` line; `git status`-style check of the folder (or the checksums of scenario 4) shows no
change; no `.closed`.

## 3. Close, and the refusals

```text
dmc campaign close <c> --yes
dmc campaign standard <c>
dmc campaign backups <c>
dmc campaign backup <c>
dmc campaign close <c> --yes
```

Expected: `closed <c>: ...` naming `backups/<c>_closed_<stamp>.zip` and `exports/<same name>`; `closed: <time>`; the
backups list ends with `closed: backups/...` and has no `ask:` line; `backup` and the second `close` are refused in one
line each.

On a campaign that is `behind`: `close` refuses and names `bring it up to date before closing`.

## 4. Nothing changes a closed campaign

Record a checksum of every file under `adventures/<c>/`, run every writing command on it (`backup`, `restore`,
`restore --yes`, `standard --init`, `verify`, `map` on one of its map files, `redraw` with it active, `pdf --out` and
`digest show --out` aimed into it) and the readers (`export`, `show`, `lookup`, `recall`, `size`), then compare.

Expected: 0 files changed (SC-001). `verify` prints `verified <c>: closed, <N> files match ...`. Edit one file by
hand and run `verify` again: it prints `changed: <path>` and fails.

## 5. The closing backup survives

Delete the scratch campaign folder (it is a scratch copy). Then:

```text
dmc campaign show exports/<c>_closed_<stamp>.zip
dmc campaign import exports/<c>_closed_<stamp>.zip --as <c>-copy
dmc campaign standard <c>-copy
```

Expected: `show` lists its files, the module it ran and `closed: <time>`; the import succeeds; the copy reports
`closed: <time>` (FR-018). On an open copy of a campaign, 10 rounds of `backup --prune old` leave the closing backup in
place (SC-002, also automated).

## 6. Live run: the close in play (not on CI)

In the scratch folder, `dmc play`, then ask DMC to close the copied finished campaign. Check, and record in the PR:

- DMC states that closing is permanent and that a sequel is the way back, and waits (FR-002). A "not yet" changes
  nothing.
- After a yes: `canon-archive.md` has no new lines (SC-005); every secret in `dm-notes.md` and `dm-notes-archive.md`
  before the close has `revealed sN`, `never revealed` or `abandoned` in § Secrets; `summary.md` has § How it ended
  with the date, every character's level, condition and place; the loose ends are canon lines in the character's
  terms; `dmc campaign standard` prints `closed: ...`.
- `/clear`, then resume the closed campaign: DMC says it is closed, opens no scene, creates no session log, and offers a
  sequel (SC-003). Repeat the resume three times.

## 7. Measured cost

Measure the bytes the change adds to an ordinary play session (the new skill's description, the resume clause, the
dm-rules clause) and to an ordinary full save (expected 0), divided by 4, and state them in the PR (FR-019, SC-006).
