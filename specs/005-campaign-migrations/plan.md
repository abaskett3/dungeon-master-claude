# Implementation Plan: Bring Campaign Files Up to the Latest Standard

**Branch**: `005-campaign-migrations` | **Date**: 2026-11-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-campaign-migrations/spec.md`

## Summary

A migration is a numbered Markdown file in `kit/migrations/` that DMC follows in a play session. `dmc campaign` gains
the verbs for the steps that must be exact and testable: `standard` (where a campaign stands, from a `.standard`
file in the campaign, including an unfinished migration), `backups` and `backup` (a campaign export in `backups/`,
kept to three; `backups` prints the removal question and `backup` prunes only on the flag the player's yes allows),
`verify` (every unit of old content is found in the migrated campaign or accounted for as a declared drop, and each
migration's shape check passes; then it records the new standard; `--list` gives DMC the ids to account) and
`restore` (a plan by default, `--yes` to act). Sizes are left to `dmc size` (spec 006). The export
leaves `backups/` out. A new skill, `dmc-migrate-campaign`, runs the phases and asks the player; the resume skill adds
one check and the update commands one line. The November review's changes, the "Names they use" line and history-last
are migrations 002 to 004, each with a golden fixture that CI verifies. See [research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT (`src/Dmc.Cli`); Markdown for the migrations and the skill

**Primary Dependencies**: None added. `System.IO.Compression` (already used by export and import), `System.Text.Json`
(manifest, source-generated); the migration front matter is read by a hand-written parser to stay AOT-clean

**Storage**: Files only: `.standard` and `backups/` in each campaign, `migrations/` in each DMC folder

**Testing**: MSTest in `tests/Dmc.Tests` (`Campaign/`, `Toolkit/`), temp DMC folders from `Support/DmcFolders`, golden
fixtures under `tests/Dmc.Tests/Fixtures/migrations/`. No language model on CI (research R11)

**Target Platform**: Windows, Linux (x64, arm64), as for every `dmc` command

**Project Type**: CLI plus the prompts it installs (rules, skills, templates, migrations)

**Performance Goals**: `verify` on a 36-session campaign (about 30 MB of files, 10 000 units) finishes in under 5
seconds; `backup` costs one export. Neither runs unless the player asks for a migration

**Constraints**: Every failure one line `dmc campaign: <message>`; output small (at most 40 listed units without
`--all`); warnings are errors; every new member has an XML comment; a header comment on every new script and
workflow; the play context gains only the resume check (about 60 tokens) and the migrate skill's one-line description
(research R10), measured

**Scale/Scope**: Five new `dmc campaign` verbs, two changed (`export`, `import`), one new skill, two edited skills,
one template-folder row, three migrations with fixtures, about twelve documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. The story stays consistent and the player has fun | The point of the feature: verification refuses a migration that loses a unit of content, the player sees what is dropped before confirming, and a revert is one command. A migration never runs mid-fight or with a roll pending. | Pass |
| II. Play pays nothing for toolkit work | The migration text is read only while migrating. The standing cost is one `dmc` call at resume and a one-line skill description, both measured in T036 and recorded; the catch-up clauses leave the skills (FR-017), which is a net saving. `migrations/` is never loaded in play. | Pass, with a measured cost |
| III. Each rule has one home | The retention rule is code (`backups` prints the question, `backup --prune` applies the answer) and `dmc-migrate-campaign` only relays it; size targets stay in `dmc size` and no migration copies them (research R6); the migration format is `contracts/migration-file.md`, which `.docs/development.md` and `review-feedback` point to; a standard's content rules stay in the templates and skills, and a migration only moves old files to them. | Pass |
| IV. Documentation goes with every change | Tasks list the user guide, decisions, roadmap, development guide, `review-feedback` step 7, the skills and the XML comments, in the same PR. | Pass |
| V. Tests skip locally and fail on CI | Every test here needs only the built program and temp folders; none skips. | Pass |
| Build and Output Constraints | Native AOT: no reflection, source-generated JSON, hand-written front-matter parser. One-line errors; `verify` output capped. | Pass |

Re-check after Phase 1 design: unchanged. The one new cost to Principle II (the skill description) is the smallest
form the player's "bring my files up to date" request can trigger; a hidden skill would need the player to name it.

## Project Structure

### Documentation (this feature)

```text
specs/005-campaign-migrations/
├── plan.md              # This file
├── research.md          # Phase 0: R1 to R13 and four spec amendments
├── data-model.md        # Phase 1: standard, record, migration, backup, content unit, accounting
├── quickstart.md        # Phase 1: six validation scenarios
├── contracts/
│   ├── campaign-commands.md   # the new and changed dmc campaign verbs
│   └── migration-file.md      # the migration file format and the author's rules
├── checklists/requirements.md # spec quality (specify)
└── tasks.md             # Phase 2 (/speckit-tasks), not created here
```

### Source Code (repository root)

```text
kit/
├── migrations/                      # NEW: 002-sheet-history-last.md, 003-names-they-use.md,
│                                    #      004-lean-notes-and-state.md (installed to migrations/)
├── skills/
│   ├── dmc-migrate-campaign/SKILL.md      # NEW: the phases, the questions, the commands
│   ├── dmc-resume-campaign/SKILL.md       # EDIT: one `dmc campaign standard` check, one offer line
│   ├── dmc-new-campaign/SKILL.md          # EDIT: `dmc campaign standard NAME --init` after the copy
│   └── dmc-character-sheet-update/SKILL.md  # EDIT: drop the "if an older sheet lacks it" clause (FR-017)
└── settings.json                    # EDIT: deny Edit and Write of /migrations/**

src/Dmc.Cli/
├── Kit/GeneratedFiles.cs                  # EDIT: row ("kit/migrations/", "migrations/")
├── Commands/CampaignCommand.cs            # EDIT: help text, the new verbs
├── Commands/UpdateCommand.cs, RefreshCommand.cs   # EDIT: the "campaigns are behind" line
└── Campaign/
    ├── CampaignArguments.cs               # EDIT: the new requests
    ├── CampaignExporter.cs                # EDIT: skip backups/; allow the backups destination for backup
    ├── CampaignImporter.cs, ArchivePaths.cs   # EDIT: refuse a backups/ entry
    ├── StandardRecord.cs                  # NEW: read, write, compare the .standard file
    ├── MigrationFile.cs, MigrationSet.cs  # NEW: parse front matter; the pending set for a standard
    ├── BackupFolder.cs                    # NEW: names, listing, free name, prune rule
    ├── CampaignBackup.cs                  # NEW: backup and prune around the exporter
    ├── ContentUnits.cs                    # NEW: split, normalise, id
    ├── AccountingFile.cs                  # NEW: read and validate the TSV
    ├── CampaignVerifier.cs                # NEW: the four checks of R5, writes .standard on a pass
    ├── MigrationChecks.cs                 # NEW: sheets-end-with-history, npc-sheets-have-names-line,
    │                                      #      no-dead-opposition-rows, fights-have-timeline
    └── CampaignRestorer.cs                # NEW: plan, replace, recovery

tests/Dmc.Tests/
├── Campaign/                              # NEW: StandardTests, BackupTests, VerifyTests, RestoreTests,
│                                          #      InterruptedMigrationTests, ExportBackupsTests, ContentUnitsTests,
│                                          #      AccountingFileTests, MigrationFileTests, MigrationChecksTests;
│                                          # EDIT: CampaignCommandContract, CampaignImportRefusalTests,
│                                          #       CampaignRoundTripTests, ArchivePathsTests
├── Toolkit/                               # NEW: MigrationFilesTests, MigrationGoldenTests; EDIT: ToolkitTests
└── Fixtures/migrations/{002,003,004}/{before,after,accounting.tsv}

.docs/                                     # EDIT: user-guide.md, decisions.md, roadmap.md, development.md, and the
                                           #       file listing in architecture docs if one names kit/ folders
.claude/skills/review-feedback/SKILL.md    # EDIT: step 7 requires a migration (FR-015)
CLAUDE.md                                  # EDIT: § Git and GitHub, the PR names its migration (T059)
```

**Structure Decision**: all code goes under the existing `src/Dmc.Cli/Campaign/` and the existing `campaign` command;
no new project and no new top-level command. The migrations are prompt files under `kit/`, installed by the existing
generated-files table, so `refresh` and `update` carry them with no new mechanism.

## Phases and order of work

The tasks (next command) follow this order, each phase leaving the build green:

1. **Foundations**: `StandardRecord`, `MigrationFile`/`MigrationSet`, `GeneratedFiles` row, settings deny, the
   `standard` verb, the file-format tests.
2. **Backups**: exporter changes (skip `backups/`, the destination rule), `BackupFolder`, `CampaignBackup`, the
   `backups` and `backup` verbs, `import` refusal. This is the first independently shippable slice (story 1a).
3. **Verification**: `ContentUnits`, `AccountingFile`, `CampaignVerifier`, `MigrationChecks`, the `verify` verb.
4. **Restore**: `CampaignRestorer`, the `restore` verb, recovery.
5. **The procedure**: `dmc-migrate-campaign`, the resume and new-campaign edits, the update line.
6. **The cases**: migrations 002 to 004 with fixtures and golden tests; remove the catch-up clauses (story 5).
7. **The flow and docs**: `review-feedback` step 7, `.docs/*`, the measured cost, the author's live run on the
   fixtures and on a copy of the long campaign.

The analysis of 2026-11-10 (`/speckit-analyze`) revised the design; research.md lists the changes as amendments A5
to A9 and the decisions are in R5 to R10 and R12.

## Complexity Tracking

No constitution violations. Two additions worth a reviewer's attention, both justified in research:

| Addition | Why needed | Simpler alternative rejected because |
|---|---|---|
| A unit-accounting `verify` instead of a diff or a model check | FR-009 needs "no data lost" to be checkable for files that are reorganised, not edited in place | A diff reports a reorganised file as all changed; a model-only check is the failure the feature prevents (R5) |
| `restore` as its own verb | `import` must keep refusing to change an existing campaign; the user asked for a separate command | Extending `import` would drop its safety rule (decision of 2026-11-09) |
