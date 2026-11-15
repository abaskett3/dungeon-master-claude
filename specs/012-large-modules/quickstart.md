# Quickstart: validating Large Modules

Phase 1 of [plan.md](plan.md). How to show the feature works, end to end. The details are in
[contracts/dmc-module-check.md](contracts/dmc-module-check.md) and [contracts/prompts.md](contracts/prompts.md).

## Build and test

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
dotnet test -c Release
```

The tests need a binary newer than every source file, so publish first.

## On CI (synthetic fixtures only)

`tests/Dmc.Tests/Modules/` builds modules in temporary DMC folders; nothing under `modules/` is committed.

1. **Sound module** (38 chapter files, one hub chapter of about 13k tokens with 30 area headings, a monster file of
   about 15k with one heading per stat block): `dmc module check` prints nothing, exit 0.
2. **Planted problems**: a reference file of 37k tokens; a section of 5k; one heading over three stat blocks; a
   heading repeated under one parent; `### PLAYER-SAFE` under two parents. The check prints exactly the first four,
   one line each (SC-004).
3. **Old skill format** (a chapter's NPCs under one heading with their stat blocks): fails on that heading; the same
   fixture re-headed passes (SC-009).
4. **Resume read**: a campaign whose pointer is `chapters/05-hub.md § 12. The Well`. `dmc size` shows the index and
   that one section; the total does not change when every other chapter is made ten times larger (SC-001, SC-002).
   A pointer in the old form, a file alone, `none`, and a finished module measure as before.
5. **Pointer miss**: a heading that matches nothing gives the flag line with the nearest headings, and the chapter
   measured whole.
6. **`--check`**: the campaign on the planted module prints one `module:` line; on the sound module, none; after the
   oversized file is split, none (SC-007).
7. **Lookup unchanged**: a stat block from the 37k file prints about 650 tokens (SC-003); every existing lookup test
   passes unchanged.
8. **Timing**: the check over a fixture of about 1.5 MB finishes in under one second on the release build (SC-006),
   as `RecallTimingTests` measures.

## Live, after the merge (maintainer, not CI)

- `dmc module check the-crooked-moon` in `D:\Claude\dmc` names `monsters-crooked-moon.md`,
  `monsters-legendary.md` and `items.md`, and nothing else; `git status` in the folder (or the files' timestamps)
  shows nothing written (SC-005). After the split the player chooses, it prints nothing.
- A headless resume in `D:\Claude\test-campaign` (with `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0`) of a campaign on a
  synthetic module: DMC reads the index and runs `dmc lookup` for the pointer's heading, and reads no other module
  file.
