# Implementation Plan: `dmc campaign compare` (issue #189)

**Branch**: `claude/project-thread-xbww2p` (spec 033) | **Date**: 2026-11-27 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/033-campaign-compare/spec.md`

## Summary

A new `dmc campaign` verb, `compare`, checks a campaign against any export of it by path and content and writes
nothing. It reuses the closed check's comparison and `verify --backup`'s file lookup. The closing-backup lookup gains
`exports/`, for compare and for `verify` on a closed campaign.

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT

**Primary Dependencies**: none new

**Storage**: reads `adventures/<campaign>/`, its `backups/` and `exports/`; writes nothing

**Testing**: MSTest contract tests through `MigrationRoot.Run` (in-process), `ErrorPathTests`

**Constraints**: one-line errors; output capped at 40 lines without `--all`; no kit file changes

## Constitution Check

- **Core 1, consistency**: one comparison for compare, verify, restore and sequel (R2). Pass.
- **Core 3 and Development 1, cost**: no kit file; play never calls it; 0 bytes of context. Pass.
- **Development 2, no change breaks a campaign**: reads only; no migration, no change to what files hold. Pass.
- **Development 3, one step**: reaches players with `dmc update`. Pass.
- **Development 4, grounded in the code**: research R1 to R5 cite the files. Pass.
- **Development 6, one home per rule**: the verbs in `CampaignArguments`; the lookup in `BackupFolder`; the comparison
  in `CampaignVerifier.ClosedDifferences` and `CampaignFiles.Differences`. Pass.
- **Slip test**: not needed. Play does not call `compare`, and `verify`'s closed lookup is not called by play either
  (the sequel resolves its closing backup itself, unchanged). Pass.
- **Documentation**: FR-009. Pass.

## Project Structure

```
src/Dmc.Cli/Campaign/CampaignComparer.cs        (new)
src/Dmc.Cli/Campaign/BackupFolder.cs            (ClosingPath)
src/Dmc.Cli/Campaign/CampaignVerifier.cs        (VerifyClosed uses ClosingPath)
src/Dmc.Cli/Campaign/CampaignArguments.cs       (the verb and its options)
src/Dmc.Cli/Commands/CampaignCommand.cs         (help, dispatch)
tests/Dmc.Tests/Campaign/CampaignCompareTests.cs (new), ClosingBackupTests.cs, CampaignVerifyClosedTests.cs
tests/Dmc.Tests/TokenCost/ErrorPathTests.cs
```
