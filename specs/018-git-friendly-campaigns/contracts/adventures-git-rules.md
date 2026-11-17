# Contract: the git rules for `adventures/`

Two kit files, written by `dmc install` and `dmc refresh` to the DMC folder's `templates/` (research R9). The player
copies them into `adventures/` as `.gitignore` and `.gitattributes`.

## `kit/templates/adventures.gitattributes`

```gitattributes
# DMC: copy to adventures/.gitattributes. Text is stored and checked out with LF on every computer.
* text=auto eol=lf
*.zip binary
*.png binary
```

## `kit/templates/adventures.gitignore`

```gitignore
# DMC: copy to adventures/.gitignore. Everything else in adventures/ is a campaign file and belongs in git.

# One computer's state
/.active-campaign
/.reviewed-sessions
/.transcript-review/

# Staging folders of a running or interrupted dmc campaign import or restore
/.dmc-import-*/
/.restore-*/
/.replaced-*/

# Leftovers of a dmc write that was cut off (the record is written to a temporary file, then renamed)
/*/.standard.partial-*
/*/.closed.partial-*

# Map renders: dmc redraw makes them again from maps/live.json
/*/maps/live.html
/*/maps/live-data.js
/*/maps/live.png
/*/maps/live-source.txt

# Migration backups (whole copies of a campaign). A closed campaign's closing backup is kept: a sequel reads it.
/*/backups/*
!/*/backups/*_closed_*.zip
```

The comments are the files' own documentation (Documentation 4). The exact wording may change in implementation;
the patterns may not without changing this contract.

## Test (FR-007, SC-004)

`tests/Dmc.Tests/Toolkit/` gains a test that:

1. Calls `TestEnv.Need` for git (Development 7).
2. In a temporary folder, runs `git init`, copies the two kit files in as `.gitignore` and `.gitattributes`, and
   creates an empty file at every path below. `<c>` is a campaign folder name.
3. Runs `git check-ignore --no-index` on each path, or `git check-ignore` after `git add` where needed.
   - **Kept (must not be ignored):** `player-options.md`; `<c>/.standard`, `<c>/.closed`, `<c>/player-options.md`,
     `<c>/game-state.md`, `<c>/module-progress.md`, `<c>/legacy.md`, `<c>/character-sheets/pc-a.md`,
     `npc-b.md`, `npc-b-history.md`, `minor-npcs.md`, `<c>/story/summary.md`, `canon.md`, `canon-archive.md`,
     `dm-notes.md`, `dm-notes-archive.md`, `sessions/session-001.md`, `<c>/maps/place.json`, `place.md`,
     `live.json`, `<c>/import/progress.md`, `<c>/digests/<other>/index.md`, `<c>/backups/<c>_closed_20261113212205.zip`,
     `.gitignore`, `.gitattributes`.
   - **Ignored:** `.active-campaign`, `.reviewed-sessions`, `.transcript-review/x.md`, `.dmc-import-ab12/x`,
     `.restore-<c>-ab12/x`, `.replaced-<c>-ab12/x`, `<c>/.standard.partial-ab12cd34`, `<c>/.closed.partial-ab12cd34`, `<c>/maps/live.html`, `live-data.js`, `live.png`,
     `live-source.txt`, `<c>/backups/<c>_backup_20261115113646.zip` with its `.migrating`, `.accounting.tsv` and
     `.verified.tsv`.
4. Checks, with `git check-attr eol`, that a `.md` file gets `eol: lf` and that a `.zip` is binary.

A second assertion ties the kept list to the read set: every file name `ReadSet` can return
(`src/Dmc.Cli/Size/ReadSet.cs`) has a matching kept path in the test, so a new resume read cannot fall outside it
unnoticed.
