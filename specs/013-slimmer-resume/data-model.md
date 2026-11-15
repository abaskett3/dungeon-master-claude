# Data Model: A Slimmer Resume

## Play card (a section of a companion's sheet)

- **Where:** `## Play card`, the first `## ` section of the sheet, after the title and the top fields.
- **Fields:** who they are (a line); appearance (short); voice (a line and one sample); names they use; attitude to the
  PC and what they want now; AC, max HP, speed, saves, senses; each attack (to hit, damage, reach or range, anything it
  needs); recharge abilities; standing traits that change play; items used in play. `none` for a field nothing fills.
- **Rules:** each number on it is the sheet's only copy (FR-002); the part the resume reads (top through the card) is
  within `Budgets.PlayCard`, 1,000 tokens.
- **Read:** the resume reads to the line before the next `## ` heading after the card. A sheet with no card is read down
  to its history and flagged.
- **Written by:** migration 006 (existing companions), `dmc-character-sheet-update` (a new companion), kept by the full
  save.

## Scene read (output of `dmc recall NAME --scene`)

- **Words:** from `game-state.md`, comments left out: the Scene block's Location, Present, What is happening and The
  player was about to; the `### Active` quests; the `## Pending` section. Recall's stems and common words.
- **Lines:** `story/canon.md` outside the always-read sections (Rulings; Prices, debts and promises; any section whose
  first comment says "Always read"), ranked by `Recall.Rank`, printed with `Recall.Hit`.
- **Cap:** 12,000 bytes, then `(N more lines matched)`.
- **Small canon:** under `Budgets.CanonWholeBelow` (6,000 tokens), the single line `story/canon.md is small: read it
  whole`.

## Register (inside canon's `## People`)

- **Entry:** a list item under `## People`, starting `- **`.
- **Names:** every bold span before the first colon outside brackets; the items of an `(also: …)` bracket right after
  the first bold span, split at `,` and `;`; each word of a bold name of 3 letters or more not in `ReadSet.CommonWords`.
- **Match:** whole word, case-insensitive. A person is the entry; a prompt names a person when it holds any of its names.

## Canon reminder (the hook's output)

- **Header:** `Canon on <name>, <name> (a reminder of some lines, not all canon says; recall for more):`.
- **Lines:** at most 3 a name, from canon outside the always-read sections then `story/canon-archive.md`, best first by
  `Recall.Rank` on the name's words; recall's hit format. 4,000 bytes in all.
- **Once:** a name in a header after the latest compaction boundary of the log is not reminded again.
- **Silent:** no active campaign, a missing or closed one, a prompt starting `/`, no new name, any failure.

## Slip line (in a session log scene)

- **Shape:** `- Slip: <what was wrong, and the right fact> (canon|companion|hook|other)`.
- **Categories:** FR-021.

## Gate result

- **Baseline:** slips per session per category, every session log of The Silver Egg and Rorn's Landing before Phase D
  (`baseline.md`), and Rorn's Landing's own average beside it.
- **After:** the first 3 qualifying sessions after migration 006, counted from their `Slip:` lines and other records,
  each checked against its transcript.
- **Verdict:** per gate category (canon, companion, hook), risen when the after average is higher (FR-024).
- **Action:** that category's rollback (research R8); recorded on #179, in `decisions.md` and `roadmap.md`.

## Migration 006

- **standard:** 6. **touches:** `character-sheets/`, `story/canon.md`. **drops:** none. **checks:**
  `companions-have-play-card`, `people-entries-named`.
