# Implementation Plan: Close a Campaign for Good

**Branch**: `010-campaign-close` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/010-campaign-close/spec.md`

## Summary

A campaign is closed by a new kit skill, `dmc-close-campaign`, and a new verb, `dmc campaign close NAME [--yes]`. The
skill does what needs judgment: it checks with the plan form of the verb, asks the player once, then runs a closing
full save (the full save's steps, pointing to that skill, with no archiving of canon, a sweep for the why and how, an
account of every secret in § Secrets, the loose ends as canon lines, and § How it ended in `summary.md`). The verb does
what must be exact: it refuses a campaign that is not current, writes the mark `.closed`, writes a closing backup
`<name>_closed_<stamp>.zip` that backup counting and pruning never see, copies it to `exports/`, checks both, and on
any failure removes what it wrote. Afterwards `standard` reports `closed` first, every writing command refuses the
campaign through one guard, `redraw` does nothing, `verify` checks the bytes against the closing backup and writes
nothing, and resume says it is closed and offers a sequel. No template changes and no migration. See
[research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT (`src/Dmc.Cli`); Markdown for the skill and the prompt edits

**Primary Dependencies**: None added. `System.IO.Compression` and the source-generated manifest JSON, as export and
backup use them

**Storage**: Files only: `.closed` in the campaign folder, the closing backup in its `backups/`, the second copy in
`exports/` of the DMC folder

**Testing**: MSTest in `tests/Dmc.Tests` (`Campaign/`, `Toolkit/`), temporary DMC folders from `Support/DmcFolders`; a
live run for the model's part (quickstart.md § 6), since no language model runs on CI

**Target Platform**: Windows, Linux (x64, arm64), as every `dmc` command

**Project Type**: CLI plus the prompts it installs (rules, skills, settings)

**Performance Goals**: `close` costs one export, one file copy and two archive checks, about what `backup` costs today;
the guard on other commands is one `File.Exists` per call; `redraw` gains one `File.Exists` per reply

**Constraints**: Every failure one line `dmc <command>: <message>`; warnings are errors; an XML comment on every new or
changed member, private ones included; the play context gains under 100 tokens and an ordinary full save 0, measured
(R11)

**Scale/Scope**: One new verb, seven changed verbs (`standard`, `backups`, `backup`, `restore`, `verify`, `import`,
`show`), the guard in
`map`, `pdf`, `digest` and `redraw`, one new skill, three short prompt edits, one settings rule, about eight documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. The story stays consistent and the player has fun | The point of the feature: the closing save archives nothing out of canon and records the why and how of each fact, so a sequel cannot contradict a carried character's past through a fact left in the least-read place (feedback 2026-11-09). Closing asks first, says it is permanent, and never happens on DMC's own initiative. A failed close leaves the campaign open and playable. | Pass |
| II. Play pays nothing for toolkit work | The skill body is loaded only while closing. The standing cost is the skill's description, one clause in resume and one in `dm-rules.md`, measured in the PR (R11); the full save gains nothing (FR-003). `redraw` adds one `File.Exists`. Players install nothing new. | Pass, with a measured cost |
| III. Each rule has one home | The close skill points to `dmc-campaign-full-save` for every step it does not change and the full save gains no closing text; the mark's name, reading and the refusal live in one class (`ClosedCampaign`); the closing backup's name lives in `BackupFolder`; the "never change a closed campaign" rule lives once in `dm-rules.md`, and the skills only branch on `closed`. | Pass |
| IV. Documentation goes with every change | Tasks cover `.docs/dmc-reference.md`, `campaign-files.md`, `user-guide.md`, `architecture.md`, `roadmap.md`, `decisions.md`, `README.md`'s skill list and the XML comments, in the same PR (FR-021). | Pass |
| V. Tests skip locally and fail on CI | Every test needs only the built program and temporary folders; none skips. | Pass |
| Build and Output Constraints | Native AOT: no reflection, no new dependency. One-line errors; `verify`'s closed output capped at 40 lines without `--all`. | Pass |

Re-check after Phase 1 design: unchanged. The deny rule for `.closed` (R2) is a settings change, not a template change,
so it changes no campaign file's shape and needs no migration.

## Project Structure

### Documentation (this feature)

```text
specs/010-campaign-close/
├── plan.md                    # This file
├── research.md                # Phase 0: R1 to R12
├── data-model.md              # Phase 1: the mark, the states, the closing backup, the closing records
├── quickstart.md              # Phase 1: seven validation scenarios, one live
├── contracts/
│   └── campaign-close.md      # the new verb, the changed verbs, the prompts
├── checklists/requirements.md # spec quality (specify)
└── tasks.md                   # Phase 2 (/speckit-tasks), not created here
```

### Source Code (repository root)

```text
kit/
├── skills/
│   ├── dmc-close-campaign/SKILL.md        # NEW: the plan check, the question, the closing save, close --yes, report
│   ├── dmc-resume-campaign/SKILL.md       # EDIT: § 1, the `closed` branch
│   └── dmc-migrate-campaign/SKILL.md      # EDIT: § 1, `closed` is never migrated
├── dm-rules.md                            # EDIT: Accuracy, one clause on closed campaigns
└── settings.json                          # EDIT: deny Edit(/adventures/*/.closed)

src/Dmc.Cli/
├── Commands/CampaignCommand.cs            # EDIT: help, the close verb, closed lines in standard/backups/import/show
├── Commands/MapCommand.cs, PdfCommand.cs, DigestCommand.cs   # EDIT: the guard on their output paths
├── Maps/MapRedraw.cs                      # EDIT: Pending returns null for a closed active campaign
└── Campaign/
    ├── ClosedCampaign.cs                  # NEW: the mark (name, read, write, IsClosed) and the one refusal
    ├── CampaignCloser.cs                  # NEW: plan, the ordered close, the rollback, a test-only fault seam
    ├── CampaignArguments.cs               # EDIT: CloseRequest
    ├── StandardRecord.cs                  # EDIT: StandardState.Closed, checked first; the closed line
    ├── BackupFolder.cs                    # EDIT: ClosedName, ParseClosed
    ├── CampaignExporter.cs                # EDIT: ClosingBackup (destination fixed to backups/, the closed stem)
    ├── CampaignBackup.cs, CampaignRestorer.cs   # EDIT: the refusal
    ├── CampaignVerifier.cs                # EDIT: the closed path: byte check against the closing backup, no writes
    └── ExportSummary.cs                   # EDIT: show's closed line when the file holds .closed

tests/Dmc.Tests/
├── Campaign/                              # NEW: ClosedCampaignTests, CampaignCloseTests, CloseFailureTests,
│                                          #      ClosedCampaignFreezeTests, ClosingBackupTests, CampaignVerifyClosedTests;
│                                          # EDIT: StandardTests, CampaignCommandContract
└── Toolkit/                               # EDIT: ToolkitTests (skill list, the deny rule)

.docs/                                     # EDIT: dmc-reference.md, campaign-files.md, user-guide.md, architecture.md,
                                           #       roadmap.md, decisions.md
README.md                                  # EDIT: the skill list
```

**Structure Decision**: all code goes under the existing `src/Dmc.Cli/Campaign/` and the existing `campaign` command,
as for the migrations (spec 005); no new project and no new top-level command. The skill is installed by the existing
generated-files table, which already carries every folder under `kit/skills/`.

## Phases and order of work

The tasks (next command) follow this order, each phase leaving the build green:

1. **The mark and the state**: `ClosedCampaign`, `StandardState.Closed`, `standard`'s line, `--init`'s refusal, the
   settings deny rule and its toolkit test.
2. **The close** (US1, exact part): `BackupFolder` names, `CampaignExporter.ClosingBackup`, `CampaignCloser` with the
   plan, the ordered steps and the rollback, the `close` verb; the close and failure tests.
3. **The freeze** (US2): the guard in `backup`, `restore`, `map`, `pdf`, `digest`; `redraw`; `verify`'s closed path;
   the `backups`, `show` and `import` lines; the freeze test.
4. **The backup survives** (US3): the prune-rounds test, the second-copy and import tests.
5. **The prompts** (US1 and US2, judgment part): `dmc-close-campaign`, the resume, migrate and dm-rules edits.
6. **Docs and proof**: `.docs/*`, `README.md`, the measured cost, the live run (quickstart.md § 6), the PR.

## Complexity Tracking

No constitution violations. Two choices worth a reviewer's attention, both justified in research:

| Addition | Why needed | Simpler alternative rejected because |
|---|---|---|
| A deny rule for `.closed` in the play settings (R2) | FR-010: DMC's edits cannot set or clear the mark | A rule in a prompt is a request; a wrong mark would freeze or unfreeze a campaign with nothing to catch it |
| `verify`'s closed path as a byte check (R7) | The issue keeps `verify` working on a closed campaign and the spec says it writes nothing | Today's content-unit check refuses a current campaign ("no migration is pending") and would miss added lines |
