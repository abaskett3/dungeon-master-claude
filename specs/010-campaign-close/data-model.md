# Data Model: Close a Campaign for Good

**Feature**: `010-campaign-close` | **Date**: 2026-11-11 | **Research**: [research.md](research.md)

Files only. Nothing here is a database or a stored index: markdown stays the source of truth, and the exact parts are
small files `dmc` writes.

## Closed mark

`adventures/<campaign>/.closed` (R1).

| Field | Form | Rule |
|---|---|---|
| Closed at | `yyyy-MM-ddTHH:mm:ssZ`, UTC | The moment `dmc campaign close --yes` ran; the same moment as the closing backup's `exportedAt` |
| Closing backup | a file name, no path | The closing backup this close wrote in `backups/` |

- One line, the two fields separated by one space, then `\n`. UTF-8, no byte-order mark.
- **Written by** `dmc campaign close --yes` only, through `.closed.partial-<8 hex>` renamed into place.
- **Removed by** nothing but the same run's rollback (FR-011). No verb clears it (FR-018).
- **Presence is the state.** A campaign with the file is closed, whatever it holds. A mark whose content cannot be read
  still closes the campaign; only `verify` needs the backup name, and it says when it cannot read it (then
  `--backup FILE` names the backup).
- **Protected in play** by the deny rule `Edit(/adventures/*/.closed)` (R2).
- **Travels** with every export and backup, like `.standard`; the closing backup holds it.

## Campaign state

`StandardState` (`src/Dmc.Cli/Campaign/StandardRecord.cs`) gains a fifth value. The order in which `Status` decides is:

1. **Closed**: `.closed` exists. Line: `closed: <closed at>`, or `closed: (mark unreadable)` when the mark cannot be read (the campaign is still closed).
2. **Unfinished**: a backup has a `.migrating` mark.
3. **Current**, **Behind**, **Ahead**: by comparing `.standard` with the kit.

State transitions:

```text
           new campaign (standard --init)
                     │
                     ▼
  behind ──migrate──▶ unfinished ──verify pass──▶ current ──close --yes──▶ closed
     ▲                    │                          │                     (final)
     └──── restore ───────┘◀────────── backup ───────┘
```

- **Closed is final.** No transition leaves it (FR-018). Only `current` can be closed (R3).
- An import of a closing backup creates a new campaign already **closed**.

## Closing backup

`adventures/<campaign>/backups/<campaign>_closed_<yyyyMMddHHmmss>.zip` (R4): one name, fixed from the closing moment,
never suffixed; a file already by that name stops the close before anything is written.

- An ordinary export (manifest, sizes, SHA-256, the general player options if there are any), of the campaign with
  its mark, without `backups/`.
- Names the module it ran in the manifest's `module` field; carries none of the module's files (FR-012).
- Never counted by `BackupFolder.List`, so never in `backups`' count, `AskLine`, `--prune`, `Resolve` or `Marked`.
- Permanent: no `dmc` verb removes it.

## Second copy

`exports/<same file name>` in the DMC folder (R5): a byte copy of the closing backup, checked by SHA-256. Never
replaces a file; a file already by that name stops the close before anything is written.

## Close plan and result

What `dmc campaign close` computes before writing (`CampaignCloser`):

| Field | Meaning |
|---|---|
| Name | The campaign folder name |
| Files | How many campaign files the backup will hold (as the exporter counts them) |
| Backup | The file name it will write in `backups/` |
| Copy | The file name it will write in `exports/` |

Refusals, checked in this order, each one line: no such campaign; already closed; unfinished migration; behind;
ahead; a record or migration that cannot be read; a file already by the closing backup's name in `backups/` or
`exports/`.

## Closing records, in the campaign's markdown

Written by DMC under `dmc-close-campaign`, in files that already hold their kind (FR-009); no new file, no template
change (R9, R10).

| Record | Where | Shape |
|---|---|---|
| Ending record | `story/summary.md` § How it ended, in place of § Where things stand | How it ended; the in-world date; per PC and companion: final level, condition, where; per recurring NPC: where; what in the folder is not canon, if anything |
| Secrets account | `story/dm-notes.md` § Secrets | Every secret, including any gathered back from `dm-notes-archive.md`, each line ending with its outcome: `revealed sN`, `never revealed` or `abandoned` |
| Loose ends | `story/canon.md`, under the heading the full save's canon step gives; promises and debts under **Prices, debts and promises** | One fact per line, with its session, in the character's terms |
| Why and how | `story/canon.md`, on the line of the fact | The reason and the means, where the session established them |

Validation (by the close skill's consistency step, and in the live run): no line moved to `canon-archive.md` by the
closing save (SC-005); every secret in `dm-notes.md` and `dm-notes-archive.md` before the close has an outcome in
§ Secrets after it.
