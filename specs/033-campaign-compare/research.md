# Research: `dmc campaign compare` (spec 033)

## R1. A new verb, not a mode of `verify`

`CampaignVerifier.Run` picks the migration proof whenever the backup's standard is behind the kit
(`set.Pending(from)`), and on a pass writes `.standard`, `.verified.tsv` and clears the marks. A mode keyed on "no
migration is pending" could never compare a campaign with a backup older than the last upgrade, and one verb would
both write and not write by state the player cannot see. `kit/skills/dmc-migrate-campaign/SKILL.md` and the migration
files call `verify` by name and depend on its refusals. A new verb leaves them, and `MigrationSet` and
`MigrationChecks` (which #290's migration 010 touches), untouched.

## R2. One comparison

`CampaignVerifier.ClosedDifferences` (the campaign's files with `.standard`, against an export's files, through
`CampaignFiles.Differences`) is already the comparison of the closed check and of `SequelDigest`; the restore's
warning calls `CampaignFiles.Differences` too. `compare` calls `ClosedDifferences`, so the four cannot disagree. The
export side comes from `ExportSummary.ReadCampaign`, which checks each file's size and SHA-256 and leaves out
`backups/`. `.closed` is a campaign file, so it is compared.

## R3. Finding the file

`BackupFolder.Resolve` already reads `--backup` (a name in `backups/`, else a path). For an open campaign, compare's
default is the latest counted backup (`BackupFolder.List`), not the marked one: the question is "the campaign now
against its last copy". For a closed one, the mark's file name is looked up in `backups/`, then in `exports/` under
the DMC folder, where `CampaignCloser` writes the copy of the same name. An import of that copy has no `backups/`
(exports leave it out; `ClosingBackupTests.TheCopyInExportsStandsAloneAndItsImportIsClosed`), so `exports/` finds it.
`verify`'s closed path uses the same lookup (`BackupFolder.ClosingPath`).

## R4. No name check

`VerifyClosed` and `SequelDigest` open the export with `ExportSummary.Open`, not `BackupFolder.Open`, because an
import `--as` holds files that name the old campaign. `compare` does the same and prints a `note:` line naming the
other campaign, since "every file differs" is otherwise puzzling.

## R5. Writing nothing

`CampaignCommand` calls `Recover` before every verb that reads a campaign (`standard`, `backups`, `verify`), to put
back an interrupted restore; compare does the same, which is the only write.
