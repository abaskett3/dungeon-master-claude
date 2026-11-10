# Data Model: Measure What a Resume Reads, in Tokens

Phase 1 of [plan.md](plan.md). Nothing here is stored: every entity is computed from the campaign's files each time
the command runs, and the command writes nothing (FR-012). The rules come from [research.md](research.md); the
entities are those of the spec's Key Entities.

## Campaign target

The campaign being measured.

| Field | From | Rule |
|---|---|---|
| DMC folder | `--folder`, else `DmcFolder.Require()` | must be a DMC folder (`.dmc/manifest.json`) |
| Name | `NAME`, else line 1 of `adventures/.active-campaign` | a folder in `adventures/`, name not starting with `.` |
| Folder | `adventures/<name>/` | must exist |
| Module campaign | `module-progress.md` exists | |

Failures (one line, exit 1): no DMC folder; no name given and no active campaign; the name is not a campaign folder.

## Resume read set

The files resume step 2 reads, in its order, each with the part read (R2, R9, R10). Built by `ReadSet`.

| Order | Entry | Part read | Budget kind |
|---|---|---|---|
| 1 | `adventures/player-options.md` | whole | none |
| 1 | `<campaign>/player-options.md` | whole | none |
| 2 | `game-state.md` | whole | budgeted |
| 3 | `story/summary.md` | whole | budgeted |
| 4 | `story/dm-notes.md` | whole | budgeted |
| 5 | each `character-sheets/pc-*.md` | down to the history | budgeted (PC) |
| 5 | each companion's sheet | down to the history | budgeted (other) |
| 5 | each present NPC's sheet | down to the history | budgeted (other) |
| 6 | latest `story/sessions/session-NNN.md` | tail, or whole | none |
| 7 | `story/canon.md` | whole, or always-read sections | canon limit |
| 9 | `module-progress.md`, the module's `index.md`, the digest section | whole, whole, section | none |

A sheet appears once even when it is both a companion and present. Item 8 (maps) is never counted.

**Missing entries.** A file the resume would read that does not exist is listed as `missing` and counts 0 (a new
campaign). A Party status block with no matching sheet is listed as `not found`. A module whose `index.md` or digest
chapter is missing is listed as `not ingested`.

### Part read

| Part | Rule |
|---|---|
| Whole | every byte |
| Down to the history | if the sheet's last `## ` heading is `## History`, `## Notes` or `## History with the PC`, the lines before it; otherwise whole (history in the middle is read whole) |
| Tail | from the third-last line matching `^### Scene [0-9]+:` to the end; whole with fewer than three; whole when the last full save does not cover the log (R10) |
| Always-read sections | canon over its limit: the `## Rulings` and `## Prices, debts and promises` sections, and any `## ` section whose first comment holds "Always read"; the search the resume also does is not counted, and the report says so |
| Section | the digest heading holding the named section, to the next heading of the same or a higher level |

## File measurement

One entry of the read set, measured.

| Field | Meaning |
|---|---|
| Path | relative to the campaign folder, or to the DMC folder for the general options and module files |
| Part | one of the parts above, shown in the full report |
| Bytes | UTF-8 bytes of the part, line ends included as on disk |
| Lines | lines in the part |
| Tokens | Bytes ÷ 4, rounded down (R1) |
| Budget | from `Budgets` (R4), or none |
| Sections | for a part read whole that has `## ` headings: each section's tokens; text before the first heading is `(top)` |
| State | `measured`, `missing`, `not found`, `not ingested` |

## Budgets

Fixed numbers, one home: the `Budgets` class (R4, R5).

| Name | Value (estimated tokens) |
|---|---|
| GameState | 4,000 |
| DmNotes | 10,000 |
| DmNotesAboutThePlayer (section) | 2,000 |
| DmNotesArcPlan (section) | 1,500 |
| Summary | 3,000 |
| PcSheet (part read) | 6,000 |
| OtherSheet (part read) | 4,000 |
| CanonLimit (read whole below) | 19,000 |
| CanonArchiveMark (archive down to) | 16,000 |
| CanonPricesSection (bloat flag) | 3,000 |
| NearCapTokens | 20,000 |
| NearCapLines | 1,600 |
| LongLineCharacters | 400 |

## Flag

A condition worth acting on, with what it concerns. One flag per kind and file; a count where there are several
matches.

| Kind | When | Concerns | Holds the save (FR-015a) |
|---|---|---|---|
| NearCap | part read ≥ NearCapTokens, or ≥ NearCapLines lines | any file | yes when the file is budgeted |
| OverBudget | budgeted file's tokens > its budget | a budgeted file | yes |
| CanonOverLimit | canon's whole-file tokens ≥ CanonLimit | canon | yes (the save archives) |
| SectionOverBudget | About this player or Arc plan over its budget | dm-notes section | yes |
| LastPlayedLog | more than one `Last played` line, or a `## Mix tracker` section | dm-notes | yes |
| FinishedOpposition | Opposition rows whose alive count is 0 | dm-notes `## Opposition` | yes |
| DoneLines | `DONE` or `RESOLVED` lines in the content, outside the Arc plan | dm-notes | yes |
| PricesSection | `## Prices, debts and promises` over CanonPricesSection | canon | yes |
| LongLines | lines over LongLineCharacters in the content (under a `## ` heading, outside comments) | a file the full save trims, read whole | yes |

A flag on a file without a budget (NearCap on a session log, say) is reported but not the save's to fix: the save's
rule covers the files it trims.

## Report

| Form | Contents | Lines |
|---|---|---|
| Full (`dmc size [NAME]`) | header with the campaign and the method; one line per file (path, part, tokens, budget, state); the total; the largest sections of each file read whole, at most four per file, `+N more`; the flags, or `nothing to flag` | about 40 for the long campaign (SC-008) |
| Check (`--check`) | one line per flag, at most 12, then `... and N more: run dmc size NAME`; nothing when there is no flag | 0 when all is well |

Lines and wording are fixed in [contracts/dmc-size.md](contracts/dmc-size.md).

## Relationships

- A **Campaign target** has one **Resume read set**.
- A **Resume read set** has one **File measurement** per entry.
- A **File measurement** has zero or more **Flags**; canon also has CanonOverLimit, which decides its **Part**.
- A **Report** is built from the read set's measurements and flags; the library returns the measurements, so issue
  #172's verification can call it in-process (FR-016).
