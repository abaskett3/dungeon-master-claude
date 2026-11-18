# Implementation Plan: Git-friendly campaigns

**Branch**: `018-git-friendly-campaigns` | **Date**: 2026-11-17 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/018-git-friendly-campaigns/spec.md`

## Summary

Make `adventures/` safe and quiet to keep in git, with the player running git and DMC running none (spec decisions
Q1: B, Q2: A). There are four changes:

1. **Two rules files.** The kit ships `templates/adventures.gitignore` and `templates/adventures.gitattributes`.
   They leave out migration backups, map renders, per-machine state and staging folders, keep closing backups, and
   store text with LF (contract [adventures-git-rules.md](contracts/adventures-git-rules.md)).
2. **`dmc` ignores line endings when it compares.** The restore's "changed since" count and the closed-campaign check
   (verify, sequel import) treat CRLF and LF as the same content, so a checkout does not look like a change
   (contract [line-ending-compare.md](contracts/line-ending-compare.md)).
3. **`dmc map` writes LF on every platform.**
4. **A user-guide section** shows a non-developer how to set it up.

Play cost is zero: no prompt line is added. No migration is needed.

**Changes to the spec, for the maintainer's approval** (research R1, R4):
- **FR-004's line-ending migration is dropped.** Git normalizes on commit, and `dmc` stops caring. DMC's own shell
  writes in play would bring CRLF straight back (research R1), so a migration would not last.
- **FR-003 now covers only what `dmc` writes,** not DMC's prompts, for the same reason.

The spec is amended to match. Revert the amendment if you disagree.

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT (`src/Dmc.Cli`); Markdown (`kit/templates/`, `.docs/`)

**Primary Dependencies**: none new. Git is the player's optional tool, and the tests need it on CI.

**Storage**: no new campaign file. Two new kit templates (data-model.md).

**Testing**: MSTest (`tests/Dmc.Tests`). A toolkit test runs `git check-ignore` against the rules. Campaign tests
cover the line-ending comparison. A map test checks for LF. The quickstart covers the rest: git on Windows and Linux,
Grep in a session, and headless play.

**Target Platform**: Windows and Linux, x64 and arm64

**Project Type**: CLI and prompt kit

**Performance Goals**: The comparison adds one CRLF scan per text file only when a raw hash differs. Restore and
verify read every file already.

**Constraints**:
- `dmc` never writes `adventures/` outside the campaign commands (`.docs/campaign-files.md:58`).
- No change to `kit/dm-rules.md` or any skill.
- One-line errors.
- `kit/**` stays LF (`.gitattributes`).

**Scale/Scope**: 2 templates, 2 callers of a new comparison, 1 writer fixed, 1 guide section; 11 FRs

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| Core 1. Consistency | the files that hold new state are named | Pass: none. data-model.md sorts existing files; every file the resume reads or DMC searches is kept (FR-007 test) |
| Core 2. Fun | what the player gains | Pass: a history of their campaign and a copy off the machine, at no cost to play |
| Core 3. Low context | cost counted | Pass: zero tokens (research R10) |
| Dev 1. Count the cost | lines per file | Pass: 0 lines in `kit/dm-rules.md`, 0 in skills; the resume reads nothing new |
| Dev 2. No change breaks a campaign | a migration or why none | Pass: no file's shape changes. The comparison accepts every existing record (R3), and no migration is needed (R4) |
| Dev 3. One step to install and upgrade | no new step or dependency | Pass: git is optional; install and update are unchanged |
| Dev 4. Grounded in the code | claims cite paths | Pass: research cites each. Unverified: `eol` overriding `core.autocrlf` on this machine (R2), and ripgrep's `!` handling (R6); quickstart Q2 and Q4 check both |
| Dev 5. A model and effort per skill | — | Not touched |
| Dev 6. One home per rule | homes named | Pass: the table below |
| Dev 7. Tests skip locally, fail on CI | tools | Pass: the git test calls `TestEnv.Need` |
| DM 1 to DM 4 | — | Not touched: no change to play |
| Doc 1 to 4 | documents named | Pass: § Documents below; Doc 2 by quickstart Q8 |

**Re-check after Phase 1:** unchanged. The FR-004 change is a spec amendment, not a constitution break.

## Where each change goes (Development 6)

Line numbers are `origin/main` at `fb71982`.

| Req | Home |
|---|---|
| FR-001, FR-007 | `kit/templates/adventures.gitignore` (new); test in `tests/Dmc.Tests/Toolkit/` (contract [adventures-git-rules.md](contracts/adventures-git-rules.md)) |
| FR-002 | `kit/templates/adventures.gitattributes` (new) |
| FR-003 | `src/Dmc.Cli/Maps/MapPage.cs:38-45` (`WriteText`); test in `tests/Dmc.Tests/Map/` |
| FR-004 (amended) | none: research R4 |
| FR-005 | `src/Dmc.Cli/Campaign/CampaignFiles.cs` (the comparison, beside `Hash` at :53); callers `CampaignRestorer.cs:99-101` and `CampaignVerifier.cs:205-223`; tests in `tests/Dmc.Tests/Campaign/` (contract [line-ending-compare.md](contracts/line-ending-compare.md)) |
| FR-006 | none: holds today (research R8) |
| FR-008 | `.docs/user-guide.md`, a new section after § Your files (where `adventures/` is described, :208) |
| FR-009, FR-010, FR-011 | constraints; checked in review. No code runs git, and nothing deletes. |

## Documents (Documentation 4)

- `.docs/user-guide.md`: the new section (FR-008). It covers the copy commands for PowerShell and bash, what is left
  out and why, keeping the repository private, when to commit (after a full save, and before and after a migration),
  the "CRLF will be replaced" warning, moving to a new computer (`dmc install`, then clone into `adventures/`), and
  not to play one campaign from two computers.
- `.docs/campaign-files.md`:
  - :58: "a DMC folder is not a git checkout" becomes "`adventures/` may be a git repository the player keeps".
  - § Layout: the two rules files.
  - :322: the note about the Grep tool and feedback still holds, because `feedback/` is outside `adventures/`.
- `.docs/dmc-reference.md` :477-482 (the restore warning) and :613 (closed-campaign check): line endings alone are
  not a change.
- `.docs/decisions.md`:
  - Git-ready only, with the repository at `adventures/`.
  - LF in git, and `dmc` ignoring line endings rather than a migration (R2 to R4).
  - Closing backups kept in git (R5).
- `.docs/architecture.md`: only if it describes the comparison or the map writer. To be checked in implementation.
- `.docs/roadmap.md`: a row.
- `.docs/releases.md`: the next release's notes say to copy the rules files again after an update that changes them.

## Project Structure

### Documentation (this feature)

```text
specs/018-git-friendly-campaigns/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── adventures-git-rules.md
│   └── line-ending-compare.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
kit/templates/
├── adventures.gitignore               # new
└── adventures.gitattributes           # new
src/Dmc.Cli/
├── Campaign/CampaignFiles.cs          # the comparison
├── Campaign/CampaignRestorer.cs       # uses it
├── Campaign/CampaignVerifier.cs       # uses it
└── Maps/MapPage.cs                    # LF
tests/Dmc.Tests/
├── Campaign/                          # line-ending cases
├── Map/                               # LF
└── Toolkit/                           # the rules test
```

**Structure Decision**: the existing CLI and kit; no new project or folder.

## Open points for implementation

- Check that `GeneratedFiles` and the manifest take files named `*.gitignore` and `*.gitattributes` under
  `kit/templates/` like any other (`src/Dmc.Cli/Kit/GeneratedFiles.cs:32`; the csproj embeds `kit/**/*`,
  `Dmc.Cli.csproj:68`). Unverified.
- Check whether any toolkit test lists the files in `kit/templates/` and needs the two new names
  (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`).
- Existing map tests may assert CRLF on Windows. Search `tests/Dmc.Tests/Map/` for `NewLine`, then update them.

## Complexity Tracking

| Departure | Why needed | Simpler alternative rejected because |
|---|---|---|
| FR-004's migration dropped | DMC's shell writes in play bring CRLF back (R1). Git and `dmc` absorb the difference (R2, R3) | A migration costs a DMC run per campaign and is undone by the next Python write |
| Closing backups kept in git, a binary per closed campaign | A sequel needs them on a new computer (R5) | Ignoring all of `backups/` stops sequels after a move |
