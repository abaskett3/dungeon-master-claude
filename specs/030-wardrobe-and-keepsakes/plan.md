# Implementation Plan: A wardrobe and keepsakes section on sheets

**Branch**: `claude/project-thread-pna0lk` (spec 030) | **Date**: 2026-11-26 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/030-wardrobe-and-keepsakes/spec.md` (issue #286)

## Summary

The two sheet templates get `## Wardrobe and keepsakes` before `## History`, and Looks is labelled "worn now". The
sheet-update skill makes a change of clothes a swap. Migration 009 brings existing campaigns into shape, held by a new
`dmc` check. `kit/dm-rules.md` does not change.

## Technical Context

**Language/Version**: Markdown kit files; C# on .NET 10 for the check and the tests

**Primary Dependencies**: none new

**Storage**: campaign Markdown files

**Testing**: MSTest (`tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`, `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`)

**Constraints**: `kit/dm-rules.md` at its cap of 32,510 bytes, unchanged

**Scale/Scope**: two templates, three skills, one migration, one check, four documents

## Constitution Check

- **Core 1, consistency**: one home for clothes not worn now, on the owner's sheet. Pass.
- **Core 3 and Development 1, cost**: `kit/dm-rules.md` 0 bytes; `dmc-character-sheet-update` +397,
  `dmc-generate-image-prompt` +77, `dmc-campaign-full-save` +9; templates +372 (NPC) and +445 (PC), paid when a sheet
  is written. `dmc size`: a companion is read through its card, so no change; a PC's and a present NPC's sheets are
  read to their history and pay for their section's lines (the PC's empty section with its comment removed is about
  30 bytes). Pass.
- **Development 2, no change breaks a campaign**: migration 009. Pass.
- **Development 3, one step to install and upgrade**: reaches players with `dmc update`. Pass.
- **Development 4, grounded in the code**: the check reuses `ReadSet.CompanionBlocks` and `ReadSet.MatchCompanion`,
  as `companions-have-play-card` does; `SheetReminder` matches `- **Looks**` as a prefix, so the "(worn now)" label
  still reaches the hook's sheet lines (`src/Dmc.Cli/Retrieval/SheetReminder.cs`, `HasValue`). Pass.
- **Development 6, one home per rule**: the swap lives in the sheet-update skill; the templates show the shape. Pass.
- **Documentation**: the four documents of FR-008. Pass.

## Project Structure

```
kit/templates/npc-sheet.md, kit/templates/pc-sheet.md
kit/skills/dmc-character-sheet-update/SKILL.md
kit/skills/dmc-generate-image-prompt/SKILL.md
kit/skills/dmc-campaign-full-save/SKILL.md
kit/migrations/009-wardrobe-and-keepsakes.md
src/Dmc.Cli/Campaign/MigrationChecks.cs
tests/Dmc.Tests/Campaign/MigrationChecksTests.cs, tests/Dmc.Tests/Toolkit/ToolkitTests.cs
```
