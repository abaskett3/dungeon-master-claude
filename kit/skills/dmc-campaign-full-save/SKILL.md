---
name: dmc-campaign-full-save
description: Write the complete current state of the campaign to disk and verify it, so a new session could resume with no other context. Use when the player asks to save, when stopping play, before /compact or /clear, and at natural breaks in a long session.
---

# Campaign full save

The test for this save: a new DMC with none of this conversation, reading only the files, could continue the game and the player would notice no difference.

The campaign folder is `adventures/<name>/`, where `<name>` is in `adventures/.active-campaign`.

## 1. Game state

Follow the `dmc-update-game-state` skill so `game-state.md` matches this exact moment, including a fight in progress. This file is read whole at every resume: after the update, aim for under about 100 lines, one fact per line, with anything that is history (it belongs in the session log) cut.

## 2. Character sheets

For each PC, companion and NPC who appeared since the last full save, follow the `dmc-character-sheet-update` skill. Check for anything gained, lost, learned or changed: items, gold, XP or level, spells, relationships, attitudes, goals, injuries, what each NPC now knows.

Add a line to `character-sheets/minor-npcs.md` for any named bit-part character met. Promote anyone who has become recurring to their own sheet.

Resume reads each sheet down to its history, so keep every sheet's history (`## History`, or the older `## Notes` / `## History with the PC`) as its last section, and move it there if it is not. After splitting any line that has grown into a paragraph, aim for a sheet under about 150 lines. Move dated history older than the current stretch of the story to `character-sheets/<sheet name>-history.md` (create it if missing; resume does not read it) and leave the sheet with what is true now.

## 3. Session log

Append any scenes not yet written to the current `story/sessions/session-NNN.md`. If the session is ending, fill in its closing section.

## 4. Canon

Go back over the session for facts you established and did not log: names, places, distances, prices, dates, lore, promises made by or to the PC, rulings. Add each to `story/canon.md` under the right heading. One line per fact, with the session number.

Then prune. Merge lines that say the same thing, replace a superseded fact instead of keeping both versions, and delete anything struck through or marked void. Every line left should be true now.

Then split. Keep each line to one fact in a sentence or two. A line that has grown into a paragraph is split into one line per fact, or its detail goes on the sheet it belongs to, so that the line count means something. Do this before counting.

Then archive, every full save, not only when it seems worth it. Move lines in bulk: append the whole block to `canon-archive.md` in one Edit and remove it from `canon.md` in one Edit, never one Edit per line. Count the lines of `canon.md` now (Grep, pattern `^`, count mode). Resume reads canon whole only while it is under about 230 lines; past that it reads the always-read sections and a search for the scene's names, so a long canon is searched rather than read. If the count is over about 230, move the lines whose subject can no longer come up (a person dead and done with, a place left for good) to `story/canon-archive.md`, under the same headings; create it if missing. Keep moving until the count is under about 200, so the next session's additions do not push it back over. Split by whether the subject is still live, not by age: anyone or anything that can still come up stays. Never move lines out of the always-read sections, **Rulings** and **Prices, debts and promises**: settle those by pruning (a debt paid or a promise kept is deleted, or reduced to a clause on the person's line if it still matters). If nothing more can be moved because every remaining subject is live, leave the file as it is: a long canon is legitimate, and resume searches it. Say so in the step 8 report.

## 5. DM notes

Update `story/dm-notes.md`: threads opened, advanced or closed; what offscreen factions and villains have done in the meantime; what you plan to pay off later; what you learned about what this player enjoys.

Bring the Opposition table up to date (who is dead, who has moved, what the party now knows), and check that no other file gives a different count. This file is read whole at every resume, so it should hold what is still ahead, not what has happened. Delete plans the story has overtaken and anything voided by a rewind. Move threads that are closed to `story/dm-notes-archive.md` (create it if missing), which resume does not read. Never move the Opposition table, secrets, open threads, foreshadowing or the notes about this player. Keep the notes about this player as lasting statements, not a diary: merge dated lines that say the same thing, and keep the section under about 40 lines. After splitting long lines, aim for the whole file under about 150 lines.

## 5a. Module progress

Only if `module-progress.md` exists. Bring it fully up to date: current chapter and area, the digest section to read next, every area entered or cleared, each encounter with the scaling used and its outcome, module NPCs' status, treasure and clues found or missed, scripted events fired, and any deviation from the text with what it changes later. Check that the next chapter the party is likely to reach has been ingested; if not, tell the player it needs ingesting before then.

## 6. Summary

Rewrite `story/summary.md` to include this session. Keep it under about 600 words: recent events in some detail, older arcs compressed to a few sentences each. It should read as a briefing for someone taking over as DM. Do not drop anything that is still an open thread.

## 7. Consistency check

Check the facts this save changed against each other with targeted reads: Grep each changed file for the values you are comparing (HP, slots, items, gold, location, time, the names you added) in one message of parallel calls, and Read only the lines or sections the Grep points at. Re-read a whole file only if an edit to it failed or you rewrote it. Do not check from memory: the point of this step is to catch what you think you wrote but did not. Confirm:

- HP, resources, inventory and gold agree between `game-state.md` and the sheets of the PCs and companions.
- Location and time agree between `game-state.md` and the end of the session log.
- Every quest in `game-state.md` appears in the summary or session log, and none is marked both active and complete.
- No name is spelled two ways. No new canon line contradicts an older one (search for the subject).
- `canon.md` is under about 230 lines (count them again with Grep, after both the split and the archive in step 4), or every subject left in it is live and you have said so in the step 8 report.

Fix what you find. If two sources disagree and you cannot tell which is right, ask the player.

## 8. Stamp and report

Set the "Last full save" line in `game-state.md` to the real-world date and time, the session number and the last scene of the session log this save covers, for example `2026-10-17 18:40, session 8, through scene 33`. Resume uses the scene to tell whether the log has scenes that were never saved.

If this save is a planned break (the player accepted a break offer and will start a fresh session), also add this line under Pending in `game-state.md`: `Planned break: restate the immediate situation only.`

Tell the player, briefly and out of character, that the save is complete and what it covers: session number, where the story stands in one sentence, and any discrepancy you corrected. Do not list every file. For a planned break, end by telling them it is safe to type `/clear` and then `/dmc-resume-campaign`.
