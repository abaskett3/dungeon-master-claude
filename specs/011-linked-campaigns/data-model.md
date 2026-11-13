# Data Model: Linked Campaigns and the Sequel Import

Phase 1 of [plan.md](plan.md). Files and their shapes; the reasons are in [research.md](research.md).

## Continuity section (`adventures/<campaign>/player-options.md`)

```markdown
## Continuity

- **Continues**: none <!-- a sequel: the closed campaign's folder in adventures/ -->
- **Side campaign of**: none <!-- a campaign played alongside a main one, in its world: that campaign's folder -->
- **Shares a world with**: none <!-- other campaigns in the same world, folders separated by commas -->
```

| Field | Values | Rules |
|---|---|---|
| Continues | `none` or one folder name | Names a closed campaign when written by `dmc-new-campaign`; a migrated campaign may keep an open one on the player's word (FR-028a). |
| Side campaign of | `none` or one folder name | |
| Shares a world with | `none` or folder names, comma-separated | |

Read as R3 says. A campaign with no Continuity section is read through its `- **Predecessor**:` line as Continues
(FR-005), so closed campaigns, which are never migrated, keep their link.

## Linked set (computed, never stored)

| Field | Meaning |
|---|---|
| Campaign | A folder in `adventures/`. |
| Distance | Steps on the first path found from the campaign in question (0 for itself). |
| Label | `continues`, `sequel`, `side` or `world` (R2). |
| State | `open` or `closed` (`ClosedCampaign.IsClosed`). |
| Folder present | False for a link to a missing folder. |
| Digest held | The campaign in the set nearer to the start that holds `digests/<this>/` with a `.md` file in it, if any (R4's last-resort rule). |

Validation (notices, FR-004): a missing folder with no digest held, a value that is not a plain name, a link to
itself, a second name for a single-valued line (on it or on a second line of that name). A notice remembers whether
its line is in a closed campaign's file, which `dmc size --check` leaves out.

## Digest (`adventures/<sequel>/digests/<closed>/`)

| File | Holds | Written by |
|---|---|---|
| `index.md` | Format, Campaign, Closing backup (file name and SHA-256), Chain (the campaigns before it, nearest first), Carried (names), Files; no facts | `sequel --start`; Carried by DMC |
| `people.md` (or `people-1.md`, `people-2.md`, ...) | `## <Name>` per person, then their lines, a living character's stat block included | DMC (the import's subagents) |
| `places.md`, `factions.md`, `items.md` | The same, per place, faction or item | DMC |
| `world.md` | `## Rulings`, `## Prices, debts and promises`, `## Lore`, `## How it ended`, `## Module` (the deviations and module NPC statuses from `module-progress.md`, DM-only in recall as their source is), and any other subject that is the world's | DMC |
| `spine.md` | Per log, `## <the log's # title> (session-NNN.md)` (`## Session N (session-NNN.md)` if it has none), then `- sN.M <the heading's text after "Scene M: ">` per `### Scene M:` heading, word for word, nothing parsed; a log with no scene headings has its session line only | `sequel --start`, exactly |
| `secrets.md` | `## <Secret's subject>`, its lines, each ending with its outcome (`revealed sN`, `never revealed`, `abandoned`); each sheet's `## Secrets` lines, word for word, under the character's name | DMC |
| `accounting.tsv` | `<id>\tmerged\t<anchor>` or `<id>\tdropped:<class>\t<reason>` | DMC, from `sequel --list` |

`index.md`, written by `--start`:

```markdown
# Digest of the-silver-egg

- **Format**: 1
- **Campaign**: the-silver-egg
- **Closing backup**: the-silver-egg_closed_20261112093000.zip sha256:3f5a...
- **Chain**: none <!-- the digests copied with it, nearest first -->
- **Carried**: Kibil Rornson <!-- the characters this sequel carried over, as the closed campaign's sheets title them -->
- **Files**: people.md, places.md, factions.md, items.md, world.md, spine.md, secrets.md, accounting.tsv
```

Rules (checked by `sequel --check`, R10):

- Each subject has one `## ` heading in its kind, across the kind's parts; a part holds whole subjects.
- Every line copied from a source is word for word and keeps its `sN` or `sN.M`; a session-only fact line has its
  `sN.M`.
- No file over 20k estimated tokens (`Budgets.NearCapTokens`).
- Never changed after `--check` passes; copies in later sequels are identical byte for byte.

Lifecycle: `--start` (folder with `index.md` and `spine.md`, chain digests copied) → parts written → accounted →
`--check` passes → sample passes (recorded in `import/progress.md`) → the sequel's first scene. A format change that
older digests cannot meet is a migration that rebuilds them from the closing backup named in `index.md`.

## Accounted unit (computed from the closing backup)

| Field | Meaning |
|---|---|
| Id | `ContentUnits.Id` of the normalised line, as migrations use. |
| Source | Path in the closed campaign and line number. |
| Text | The normalised line. |
| Status | `copied` (found in a digest file), `merged` (accounting line, anchor found), `dropped:<class>`, or unaccounted. |

Sources (FR-021): `story/canon.md`; `story/canon-archive.md`; every line of every file in `character-sheets/` (each
sheet whole, each `*-history.md`, `minor-npcs.md`); `story/dm-notes.md` § Secrets; `story/summary.md` § How it ended;
`module-progress.md`.

Declared classes: `superseded`, `not-canon`, `structure`, `bookkeeping`, `stat-block` (R9).

## Sample draw (computed)

| Field | Meaning |
|---|---|
| Round | 1 unless `--round N`. |
| Seed | First 8 bytes of the closing backup's SHA-256, plus the round. |
| Facts | 20 digest lines, split between copies and session-only lines in proportion (R11). |
| People | 5 names from the closing backup (each sheet's `# ` title, each `minor-npcs.md` table row's Name), at least one on the index's Carried line. |

Pass: no miss on any question the closed campaign's files answer (FR-024). Recorded by DMC in `import/progress.md`
with the round.

## Legacy file (`adventures/<campaign>/legacy.md`, PR 3)

```markdown
# Legacy of <campaign>

- **Written**: session 14, 2026-11-20

## What changed in the world

- <a change, in the world's terms, with sN.M> 
```

Written at each full save (the closing save included) when the campaign's linked set holds an open campaign, from
`templates/legacy.md`; `dmc size --check` prints `legacy due:` until it is rewritten (contracts/campaign-links.md). Budget 3,000 estimated tokens.

News to a reader when its Written session is later than the one the reader's `game-state.md` records for that
campaign, or none is recorded, or the Written line is unreadable. The reader's full save records the session of each
legacy it folds, on a line only a linked campaign has (no template change, so no migration):

```markdown
- **Legacies folded**: side-quest s14, old-west s3
```

## Migration 005 (`kit/migrations/005-continuity-links.md`)

Front matter: `standard: 5`, `touches: [player-options.md]`, `drops: []`, `checks: [continuity-section]`. Body adds
the optional `## Before the backup` section (R13). State transitions for the campaign being migrated:

```text
behind ──(Before the backup: predecessor open)──► ask close
  ask close ──yes──► close predecessor (dmc-close-campaign) ──► backup ──► steps ──► verify ──► current
  ask close ──no──► "are you sure?" ──yes──► backup ──► steps ──► verify ──► current
                                    └─no──► stop: "Migration stopped until ..." (still behind, playable)
behind ──(predecessor closed, none, or missing)──► backup ──► steps ──► verify ──► current
```
