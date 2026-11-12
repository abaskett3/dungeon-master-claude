# Implementation Plan: Linked Campaigns and the Sequel Import

**Branch**: `011-linked-campaigns` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/011-linked-campaigns/spec.md`

## Summary

A campaign's single Predecessor line becomes a `## Continuity` section in its `player-options.md` (Continues, Side
campaign of, Shares a world with). One new class, `CampaignLinks`, follows the links both ways into a linked set with
distances and labels (`continues`, `sequel`, `side`, `world`); recall searches the whole set, its digests and all their
modules, ranks a digest above the folder it digests, prints each line once, and labels hits from other campaigns. A new
read-only verb, `dmc campaign links`, prints the set and its problems; in play the problems come from
`dmc size --check`, which resume and the full save already run, so play makes no new command call. Migration 005 moves the old line, and asks
before its backup whether to close an open predecessor, through a new optional `## Before the backup` section of the
migration format.

A sequel imports its closed predecessor through a new `dmc-new-campaign` § 1c: subagents, part by part, write a brief
into the sequel's always-read files and carried characters' sheets, and a digest in `digests/<closed>/` (subject files,
spine, secrets, accounting). A new verb, `dmc campaign sequel`, does the exact parts: start (index, spine, chain copies,
parts plan), list and check the accounting against the closing backup, and draw the sample test. A clause in the
sheet-update skill makes any sheet for a character from a linked campaign start from their history.

Last, a campaign linked to an open campaign writes `legacy.md` at each full save, and `dmc size --check` names a newer one
at resume. Three PRs, in that order (FR-032). See [research.md](research.md).

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT (`src/Dmc.Cli`); Markdown for the kit's skills, templates, migration
and rules

**Primary Dependencies**: None added. `System.IO.Compression` for reading the closing backup, `System.Security.Cryptography`
for SHA-256, as export and verify use them

**Storage**: Files only. `player-options.md` § Continuity; `digests/<campaign>/` in a sequel; `legacy.md` (PR 3);
`import/progress.md` for the import's progress

**Testing**: MSTest in `tests/Dmc.Tests` (`Campaign/`, `Retrieval/`, `Size/`, `Toolkit/`), temporary DMC folders from
`Support/`; the migration's golden fixture `Fixtures/migrations/005/`; live runs for the model's part
([quickstart.md](quickstart.md)), since no model runs on CI

**Target Platform**: Windows, Linux (x64, arm64), as every `dmc` command

**Project Type**: CLI plus the prompts it installs (rules, skills, templates, migrations)

**Performance Goals**: Recall over a three-campaign chain, a side and a same-world campaign and a large module (about
3 MB of markdown) within 1 second on the release build (SC-006); `links` reads one `player-options.md` per folder in
`adventures/`; `sequel --check` reads the closing backup once

**Constraints**: Every failure one line `dmc <command>: <message>`; outputs small (recall's 6,000-byte cap, `--list`
and `--check` capped at 40 lines); warnings are errors; an XML comment on every new or changed member, private ones
included; standing play cost under 150 tokens across the three PRs (FR-031, R16)

**Scale/Scope**: Two new verbs (`links`, `sequel`), one changed command (`recall`), one migration and its check, one
template change and one new template, edits to six skills and one rule line, about seven documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. The story stays consistent and the player has fun | The feature's purpose: a sequel's carry-over proved complete word for word against the closing backup, a sample test of the six questions, carried and returning characters built from their history (the feedback of 2026-11-09), world changes passed between open campaigns. Nothing changes a closed campaign; the migration never closes one without the player's yes, and a stop leaves the campaign playable. The first scene waits for the import, which is a cost the player sees once per sequel. | Pass |
| II. Play pays nothing for toolkit work | Resume reads no other campaign and no digest (SC-001); `dmc size --check`, already run at resume and full save, prints no link line for a campaign alone, and no command call is added. The import's text loads only in `dmc-new-campaign`; the sheet rule only when a sheet is written. Standing cost measured per PR (R16), under 150 tokens in all. Nothing new for players to install. | Pass, with a measured cost |
| III. Each rule has one home | The linked set in `CampaignLinks`; "the same line" in `ContentUnits`; the accounting's shape in `AccountingFile`; the migration's questions in migration 005, the skill change generic (R13); § 1c points to § 1b for parts and subagents; the side-campaign rule once in `dm-rules.md`. | Pass |
| IV. Documentation goes with every change | Each PR updates `.docs/` for what it ships (FR-030): `campaign-files.md`, `dmc-reference.md`, `user-guide.md`, `architecture.md`, `roadmap.md`, `decisions.md`, and the migration contract's note in `development.md` § Changing a campaign-file standard (the optional section). | Pass |
| V. Tests skip locally and fail on CI | Every test needs only the built program and temporary folders; the timing test uses the release build as `RecallTimingTests` does. | Pass |
| Build and Output Constraints | No reflection, no dependency added; one-line errors; capped outputs. | Pass |

Re-check after Phase 1 design: unchanged. The template change is covered by migration 005 (PR 1). The digest (PR 2) and
`legacy.md` (PR 3) are new files that only new writes create, which an older campaign can simply lack, so they need no
migration (`.docs/development.md` § Changing a campaign-file standard); each PR says so.

## Project Structure

### Documentation (this feature)

```text
specs/011-linked-campaigns/
├── plan.md                     # This file
├── research.md                 # Phase 0: R1 to R16
├── data-model.md               # Phase 1: Continuity, linked set, digest, accounting, sample, legacy, migration 005
├── quickstart.md               # Phase 1: per-PR validation, the live runs, the by-hand digest
├── contracts/
│   ├── campaign-links.md       # dmc campaign links
│   ├── recall-and-lookup.md    # recall's scope, weights, duplicates, labels; lookup over digests
│   ├── campaign-sequel.md      # dmc campaign sequel --start/--list/--check/--sample
│   └── prompts.md              # every kit change, by PR
├── checklists/requirements.md  # spec quality (specify)
└── tasks.md                    # Phase 2 (/speckit-tasks), not created here
```

### Source Code (repository root)

```text
kit/
├── dm-rules.md                                  # PR 1: Accuracy item 1 rewritten, at most one line more
├── migrations/005-continuity-links.md           # PR 1: NEW
├── templates/campaign/player-options.md         # PR 1: Continuity section replaces the Predecessor line
├── templates/legacy.md                          # PR 3: NEW
└── skills/
    ├── dmc-migrate-campaign/SKILL.md            # PR 1: § 2, Before the backup
    ├── dmc-new-campaign/SKILL.md                # PR 1: § 2 Continuity; PR 2: § 1c and the description
    ├── dmc-resume-campaign/SKILL.md             # PR 1: notice lines of size --check; PR 3: legacy lines
    ├── dmc-close-campaign/SKILL.md              # PR 1: § 5 wording
    ├── dmc-character-sheet-update/SKILL.md      # PR 2: the sheet rule's one home (carried and returning)
    └── dmc-campaign-full-save/SKILL.md          # PR 1: a named campaign (R13); PR 3: the legacy clause

src/Dmc.Cli/
├── Commands/CampaignCommand.cs                  # PR 1: links; PR 2: sequel; help text
├── Commands/SizeCommand.cs                      # PR 1: link notices in --check; PR 3: legacy and legacy-due lines
├── Campaign/
│   ├── CampaignLinks.cs                         # PR 1: NEW: Continuity/Predecessor reading, the linked set, notices
│   ├── CampaignArguments.cs                     # PR 1, PR 2: LinksRequest, SequelRequest
│   ├── MigrationChecks.cs                       # PR 1: continuity-section (MigrationFile already allows other headings
│   │                                            #   between the five, so Before the backup needs no parser change)
│   ├── AccountingFile.cs                        # PR 2: the merged keyword for a digest's accounting
│   ├── SequelDigest.cs                          # PR 2: NEW: start (index, spine, chain copies, parts), accounted units, check
│   └── SampleDraw.cs                            # PR 2: NEW: the seeded draw
├── Retrieval/
│   ├── RecallScope.cs                           # PR 1: the set from CampaignLinks, Tier.Digest, last-resort factor
│   ├── LineUnits.cs                             # PR 1: digests/*/secrets.md is DM-only
│   └── Recall.cs                                # PR 1: duplicate removal, the label in the hit line
└── Size/Budgets.cs, Flags.cs,                   # PR 3: legacy.md's budget and flag, and a part outside the
    ReadSet.cs, SizeReport.cs                    #   resume total to carry it (dmc size has none today)

tests/Dmc.Tests/
├── Campaign/                                    # NEW: CampaignLinksTests, LinksCommandContract, SequelDigestTests,
│                                                #      SequelCommandContract, SampleDrawTests; EDIT: MigrationChecksTests
├── Retrieval/                                   # EDIT: RecallLibraryTests, RecallCommandContract, LookupLibraryTests,
│                                                #       RecallTimingTests, RetrievalFixtures (chain, side, digest)
├── Size/                                        # EDIT: the digest outside the resume total; PR 3 legacy budget
├── Fixtures/migrations/005/                     # NEW: before/, after/, accounting.tsv
├── Fixtures/sequel/                             # NEW: a closed fixture campaign and its correct digest
└── Toolkit/                                     # EDIT: migration format (optional section), templates, prompt paths

.docs/                                           # EDIT per PR: campaign-files, dmc-reference, user-guide, architecture,
                                                 #   roadmap, decisions, development (migration format)
tools/recall-test.ps1                            # PR 2 follow-up: unchanged; rerun for SC-004
```

**Structure Decision**: Code goes under the existing `src/Dmc.Cli/Campaign/` and `Retrieval/` and the existing
`campaign` command, as specs 005 and 010 did; no new project and no new top-level command. The new template and the
migration are installed by the existing generated-files table, which already carries `kit/templates/` and
`kit/migrations/`.

## Phases and order of work

Each PR leaves the build green and its documentation current, and each says `Part of #179`.

**PR 1: links and recall**

1. `CampaignLinks` (parse, the set, labels, notices) with its tests; `dmc campaign links` and its contract test.
2. `RecallScope` on the set, `Tier.Digest`, the last-resort factor, DM-only secrets, duplicates and labels; recall,
   lookup and size tests on chain, side and digest fixtures; the timing test at SC-006's size.
3. The template, migration 005 with `## Before the backup`, `continuity-section`, the format's optional section, the
   golden fixture.
4. The prompts (dm-rules, migrate, new-campaign § 2, resume, close § 5); the toolkit tests.
5. Docs, the measured cost, the live runs of quickstart PR 1 (migration 005's four paths), the PR, the review.

**PR 2: the sequel import**

1. `AccountingFile`'s keyword; `SequelDigest` (start, units from the closing backup, check); `SampleDraw`;
   `dmc campaign sequel` and its contract test; the sequel fixture.
2. The prompts: § 1c, the sheet clause, the description.
3. Docs, the live run of quickstart PR 2 on the fixture, the PR, the review. Then, with the player, the by-hand digest of
   Rorn's Landing and the SC-004 measurement, posted on the issue.

**PR 3: legacy files**

1. The template, the budget and flag, the `legacy:` and `legacy due:` lines in `dmc size --check`, their tests.
2. The prompts (full save, resume); docs; the live run of quickstart PR 3; the PR, the review.

## Complexity Tracking

No constitution violations. Choices worth a reviewer's attention, each justified in research:

| Addition | Why needed | Simpler alternative rejected because |
|---|---|---|
| Links followed both ways, by reading every campaign's options (R2) | FR-003: a main campaign must reach its side campaigns without naming them | One-way links would need every link written twice, by hand, and kept in step |
| An optional section in the migration format (R13) | The clarification: the close question comes before the backup, so a stop leaves the campaign playable | Asking in Steps leaves the campaign `unfinished`, which blocks play; putting the check in the skill by name gives one migration's logic a permanent home in a generic skill |
| A second accounting verb rather than reusing `verify` (R8, R9) | The digest is checked against another campaign's closing backup, and only some files are sources | `verify` compares one campaign with its own backup, all files, and writes the verified record |
