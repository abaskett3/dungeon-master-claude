---
standard: 4
title: Lean DM notes and game state; finished fights archived; the Predecessor line
shipped: PR #173
touches: [story/dm-notes.md, story/dm-notes-archive.md, game-state.md, player-options.md, story/canon.md]
drops: [mix-tracker, overtaken-plan, finished-arc-beat, scene-history, repeats-player-options]
checks: [no-dead-opposition-rows, fights-have-timeline, one-last-played-line]
---

# Lean DM notes and game state

## What changes

`story/dm-notes.md` and `game-state.md` are read whole at every resume, so they should hold only what is still
ahead. This moves what has already happened out of them: each finished fight becomes one line in the canon's
Timeline, and its row in the Opposition table moves, unchanged, to `story/dm-notes-archive.md`, where a rewind can
restore it; closed threads move there too. A log of sessions becomes one "Last played" line. Plans the story has
overtaken, finished beats of the arc and history in the Scene block are removed; notes about you that your player
options already say are removed; standing orders that are not in play move to the canon. A sequel's player options
gain the line naming the earlier campaign. The canon's Timeline grows by one line per finished fight.

## Steps

Read each file before you change it. A file too large for one Read is read in parts. Every step first checks
whether it is needed, so a file already in the new shape is left as it is. A step that removes text comes after the
step that gives it a new home.

1. **The archive.** If `story/dm-notes-archive.md` does not exist, create it with the line `# DM notes archive` and a
   blank line. Resume does not read it.
2. **Finished fights, the Timeline line first.** In `story/dm-notes.md`, find the `## Opposition` table. A row is for
   a finished fight when its second cell ("How many (alive / started)") starts `0 /` or `0/` (see Judgments). For
   each such row, look in `story/canon.md` under `## Timeline` for a line that names the row's group by the text of
   its first cell ("Who") and has a session reference (`s12` or `s12.3`). If there is none, add one: what the
   characters did or said in that fight that others may talk about later, with `sN.M`, found in the session logs
   (Grep `story/sessions/` for the group's name). Write the group's name exactly as the Who cell gives it. The player
   may read canon: use only names the character knows, and no enemy counts. Add `## Timeline` at the end of the file
   if canon has none.
3. **Finished fights, then the row.** Append each finished row, unchanged, to `story/dm-notes-archive.md` under a
   heading `## Opposition` (add it, with the table's header row and separator row copied from `dm-notes.md`, if the
   archive has none), together with that fight's stat blocks if they are written anywhere in `dm-notes.md` outside the
   table. Then delete the row, and those stat blocks, from `dm-notes.md`. A group with members left keeps its row.
4. **Opposition section.** If `dm-notes.md` has no `## Opposition` section with a table, add the section from
   `templates/campaign/story/dm-notes.md`, with any enemy numbers the file keeps elsewhere written as its rows (move
   them, unchanged in substance, out of where they were; account for each as `reworded`).
5. **Closed threads.** Move every thread that is closed, and every line marked DONE or RESOLVED outside the
   `## Arc plan` section, unchanged, to `story/dm-notes-archive.md` under a heading `## Closed threads` (add it if
   missing). Then delete them from `dm-notes.md`. Open threads stay.
6. **The Mix tracker.** If `dm-notes.md` keeps a log of sessions (a section or table named "Mix tracker", or more than
   one line starting "Last played"), write one line at the end of the `## Arc plan` section, in the form the template
   gives: `Last played (as of sN): fight sA.B, check sC.D, puzzle sE.F`, taking the latest session of each kind from
   the log. Then delete the log, its heading included. Its lines are accounted as `dropped:mix-tracker`.
7. **The Arc plan.** In `## Arc plan`, a stage marked done stays as its one line (cut it to one line, what changed, if
   it is longer: account for the rest as `reworded`, with the kept line as the anchor). Delete the beats of the current
   stage that have already happened (`dropped:finished-arc-beat`).
8. **Overtaken plans.** Delete plans in `dm-notes.md` that the story has overtaken: a plan for something that can no
   longer happen, or that happened another way (`dropped:overtaken-plan`). Keep what is still true in one as a canon
   line or a secret first, and account for it as `reworded`.
9. **About this player.** In `## About this player`, delete each line that `player-options.md` (the campaign's or the
   general one) already says (`dropped:repeats-player-options`). Merge dated lines that say the same thing into one
   lasting statement (account for the merged ones as `reworded`, with the new line as the anchor).
10. **The Scene block.** In `game-state.md` under `## Scene`, keep the present moment only: where, when, who is present,
    what is happening and what the player was about to do. History in it (what happened earlier in the session or
    before) is already in the session logs: if a line of it is in no session log, append it to the current session log
    first and account for it as `reworded`; otherwise it is `dropped:scene-history`. Then delete it from the Scene
    block.
11. **Standing orders.** In `game-state.md` under `## Plan and standing orders`, a standing order that is not in play
    where the party is now (camp rules, a signal code, an order for a place they have left) moves to `story/canon.md`
    as a line under the right heading, with the session it was agreed in. Write the canon line first, then delete the
    order. Account for it as `reworded`, with the canon line as the anchor. Orders in play stay. If nothing is left,
    write `- none`.
12. **The Predecessor line.** If the campaign's `player-options.md` has no `**Predecessor**` line, add it under
    `## Rules`, after the House rules line, as `templates/campaign/player-options.md` gives it. Its value is the
    folder in `adventures/` of the campaign this one continues, when the campaign is a sequel (its summary or canon
    says so, or the player said so), else `none`.
13. **Account.** Run the list form of the verify command, as the `dmc-migrate-campaign` skill says, and write a line
    in the accounting file for each unit it lists, with the disposition the step that changed it names.

## Judgments

- **A finished fight**: its row's second cell is `0 / N` or `0/N`, or the session logs say the group was defeated. When
  the two disagree (the table says some are alive and the logs say all fell, or the reverse), keep the row as it is and
  ask the player.
- **A group with members left** (some never fought, one fled and could come back) keeps its row with its count.
- **A closed thread**: the thread's own status says it is resolved or abandoned, or it is marked DONE or RESOLVED.
  When in doubt, keep it open: an open thread costs a line, a lost one costs the story.
- **A plan is overtaken** when what it plans for can no longer happen, or happened differently. A plan that might
  still come up is kept. When in doubt, keep it and show it to the player.
- **A standing order is in play** when it applies where the party is now or to what is about to happen (a watch
  rota on the road they are on, an ambush signal for the fight ahead). When in doubt, keep it in `game-state.md`.
- **A sequel**: the campaign is a sequel when its own files name an earlier campaign. When you cannot tell which folder
  that is, ask the player, and write `none` if they do not know.
- **Doubtful content** of any kind is kept, and shown to the player before you continue.

## Declared drops

- `mix-tracker`: the session-by-session log of fights, checks and puzzles. The one Last played line keeps what the
  full save's check needs; the sessions themselves are in the session logs.
- `overtaken-plan`: plans for things that can no longer happen. Anything still true in one is kept as canon first.
- `finished-arc-beat`: beats of the current arc stage that have happened. The session logs and the summary hold what
  happened.
- `scene-history`: history in the Scene block of `game-state.md` that the session logs already hold.
- `repeats-player-options`: notes about the player that `player-options.md` already says.

## Done when

The checks pass: `no-dead-opposition-rows` (no row of the Opposition table in `story/dm-notes.md` has none left
alive), `fights-have-timeline` (each finished row is a row of `story/dm-notes-archive.md`, and a Timeline line in
`story/canon.md` names its group with a session reference) and `one-last-played-line` (at most one Last played line,
and no Mix tracker section, in `story/dm-notes.md`).
