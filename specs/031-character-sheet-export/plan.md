# Implementation Plan: Printable character sheets, `dmc sheet` (phase 1)

**Branch**: `claude/project-thread-p5cvi9` (spec 031) | **Date**: 2026-11-26 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/031-character-sheet-export/spec.md` (issue #191)

## Summary

A new command reads the chosen characters' sheets and Party status blocks, checks them against the templates, and
writes one printable HTML page through fixed layouts that are also the allow-list. The PC template gains `Other
skills`; the sheet-update skill runs the command when the player asks to see a sheet.

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT; Markdown kit files

**Primary Dependencies**: none new (string literals and `GeneratedRegex`, as `Maps/MapPage.cs`)

**Storage**: reads `adventures/<campaign>/character-sheets/*.md` and `game-state.md`; writes
`exports/sheets/<campaign>/*.html` only

**Testing**: MSTest, contract tests in-process and on the binary, `ErrorPathTests`, Playwright

**Constraints**: one-line errors; output capped at 12 lines; `kit/dm-rules.md` unchanged (it is at its byte cap)

## Constitution Check

- **Core 1, consistency**: no new state; the page is read from the files every time and replaced by the next export.
  Pass.
- **Core 2, fun**: the player gets a full, printable sheet on request in one step, with no spoiler. Pass.
- **Core 3 and Development 1, cost**: `kit/dm-rules.md` 0 bytes; `dmc-character-sheet-update` +639 bytes (about 70 in
  its description, paid every session); `templates/pc-sheet.md` +58 bytes. `dmc size`: a PC's filled `Other skills`
  line is about 100 to 150 tokens at each resume; companions are read through their card, so nothing. An export prints
  one line. Pass.
- **Development 2, no change breaks a campaign**: no migration in phase 1. `Other skills` is not read by play; an older
  sheet plays as before and `dmc sheet` names it at its first export. Phase 2's migration writes it everywhere. Pass,
  stated in the PR.
- **Development 3, one step**: no new dependency; reaches players with `dmc update`. Pass.
- **Development 4, grounded in the code**: the campaign is found by `SizeReport.Target`; the DMC folder by
  `DmcFolder.FromOption`; the play card's one-copy rule is `kit/migrations/006-play-cards-and-names.md` step 3;
  exact matching replaces `ReadSet.MatchCompanion`'s loose matching, which #191's review found picks a wrong sheet;
  output follows `CampaignCommand.RunVerify` and `Flags.CheckLines`; escaping is `Maps/Py.cs` `HtmlEscape`. Pass.
- **Development 6, one home per rule**: what prints is `Sheets/SheetLayouts.cs`; when DMC runs the command is the
  sheet-update skill; the field's shape is the template. Pass.
- **Development 7, tests skip locally, fail on CI**: the Playwright tests use `TestEnv.Need`. Pass.
- **Documentation**: FR-018. Pass.

## Project Structure

```
src/Dmc.Cli/Commands/SheetCommand.cs, src/Dmc.Cli/CommandTable.cs
src/Dmc.Cli/Sheets/ (SheetArguments, SheetText, SheetLayouts, CharacterSheet, SheetNumbers, SheetCheck,
                     SheetMarkdown, SheetHtml, SheetExport)
kit/templates/pc-sheet.md, kit/skills/dmc-character-sheet-update/SKILL.md
tests/Dmc.Tests/Sheets/, tests/Dmc.Tests/Playwright/SheetPageTests.cs, tests/Dmc.Tests/TokenCost/ErrorPathTests.cs
tests/Dmc.Tests/Fixtures/sheets/ (an invented campaign and the golden party page)
```
