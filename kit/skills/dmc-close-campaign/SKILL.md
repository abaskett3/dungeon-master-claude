---
name: dmc-close-campaign
description: Close a finished campaign for good, on the player's word. Suggest it only when the main story or its last open thread ends.
---

# Close a campaign

Closing ends a campaign for good: a last full save that keeps every fact where a sequel will find it, then `dmc` marks
the campaign closed and writes a checked closing backup that is never pruned, with a copy in `exports/`. Nothing changes
a closed campaign again, and nothing reopens it; a sequel continues its story. The campaign is the active one unless the
player, or the skill that called this one, named another: then close that one, and `adventures/.active-campaign` stays
as it is.

## 0. Not now

Do not start in the middle of a fight, with a roll pending or with a question to the player unanswered: say so in one
line and finish that first.

## 1. Can it be closed

Run `dmc campaign close <campaign>`. It changes nothing. On `would close ...`, go on. On a refusal, say it in a line and
stop; for `behind`, offer to bring the files up to date first with `dmc-migrate-campaign`.

## 2. Ask

Tell the player, out of character, that closing is permanent: the campaign can never be played or changed again, and the
only way back to its story is a sequel. If a scene or a fight is still open, name it. Ask once and wait. Only a clear
yes closes it; anything else closes nothing.

## 3. The closing save

Follow `dmc-campaign-full-save`, steps 1 to 8, with these differences and no others:

- **Step 2, sheets.** Move no history to a `-history.md` file: each sheet keeps all of it.
- **Step 4, canon.** Prune and split as usual, but archive nothing: no line moves to `story/canon-archive.md`. Sweep the
  canon for facts whose reason or means the sessions established, and put the reason and the means on the fact's line.
  Write each loose end (a debt, a promise, an unanswered question, where someone was left) as a canon line in the
  character's terms, with its session; promises and debts go under **Prices, debts and promises**.
- **Step 5, DM notes.** Gather every secret from `story/dm-notes-archive.md` back into § Secrets of `story/dm-notes.md`,
  and end each secret's line with its outcome: `revealed sN`, `never revealed` or `abandoned`. Move no secret out.
- **Step 6, summary.** Replace § Where things stand in `story/summary.md` with § How it ended: how the story ended, in a
  short paragraph; the in-world date; one line per PC and companion with their final level, condition and where they
  are; one line per recurring NPC with where they are; and, only if there is any, what in the campaign folder is not
  canon (a test fight, an import), named so a sequel leaves it out.
- **Step 7, sizes.** Act on the `legacy:` and `legacy due:` lines as that step says, so linked campaigns learn how
  this one ended. Leave every other `dmc size` line and trim nothing: a closed campaign is never resumed into play, so
  every file stays whole.
- **Step 8, stamp.** Set the "Last full save" line only: no Planned break line and no report; § 5 reports.

## 4. Close

Run `dmc campaign close <campaign> --yes`. On `closed ...`, go on. On a line ending `it is still open`, tell the player
which step failed, that the campaign is still open, and that the close can be run again. On a line ending `delete them
by hand`, name those files to the player and say the close can be run again once they have deleted them. On anything
else, say it in a line and stop. Running it again means steps 1 and 2 again, then straight to this step if
`story/summary.md` already has § How it ended and every secret has its outcome.

## 5. Report

Tell the player, out of character, that the campaign is closed, and where its two copies are (the two paths the
`closed` line names); suggest keeping a further copy somewhere else. Say that `dmc-new-campaign` can start a sequel,
whose Continues line names this campaign, so its facts are found when they return. When another skill called this one,
say instead, in one line, that the campaign is closed and where its two copies are, and go back to that skill where it
called this one.
