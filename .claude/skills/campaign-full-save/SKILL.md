---
name: campaign-full-save
description: Write the complete current state of the campaign to disk and verify it, so a new session could resume with no other context. Use when the player asks to save, when stopping play, before /compact or /clear, and at natural breaks in a long session.
---

# Campaign full save

The test for this save: a new DMC with none of this conversation, reading only the files, could continue the game and the player would notice no difference.

The campaign folder is `adventures/<name>/`, where `<name>` is in `adventures/.active-campaign`.

## 1. Game state

Follow the `update-game-state` skill so `game-state.md` matches this exact moment, including a fight in progress.

## 2. Character sheets

For each PC, companion and NPC who appeared since the last full save, follow the `character-sheet-update` skill. Check for anything gained, lost, learned or changed: items, gold, XP or level, spells, relationships, attitudes, goals, injuries, what each NPC now knows.

Add a line to `character-sheets/minor-npcs.md` for any named bit-part character met. Promote anyone who has become recurring to their own sheet.

## 3. Session log

Append any scenes not yet written to the current `story/sessions/session-NNN.md`. If the session is ending, fill in its closing section.

## 4. Canon

Go back over the session for facts you established and did not log: names, places, distances, prices, dates, lore, promises made by or to the PC, rulings. Add each to `story/canon.md` under the right heading. One line per fact, with the session number.

## 5. DM notes

Update `story/dm-notes.md`: threads opened, advanced or closed; what offscreen factions and villains have done in the meantime; what you plan to pay off later; what you learned about what this player enjoys.

## 5a. Module progress

Only if `module-progress.md` exists. Bring it fully up to date: current chapter and area, the digest section to read next, every area entered or cleared, each encounter with the scaling used and its outcome, module NPCs' status, treasure and clues found or missed, scripted events fired, and any deviation from the text with what it changes later. Check that the next chapter the party is likely to reach has been ingested; if not, tell the player it needs ingesting before then.

## 6. Summary

Rewrite `story/summary.md` to include this session. Keep it under about 600 words: recent events in some detail, older arcs compressed to a few sentences each. It should read as a briefing for someone taking over as DM. Do not drop anything that is still an open thread.

## 7. Consistency check

Re-read the files you just changed and confirm:

- HP, resources, inventory and gold agree between `game-state.md` and the PC sheet.
- Location and time agree between `game-state.md` and the end of the session log.
- Every quest in `game-state.md` appears in the summary or session log, and none is marked both active and complete.
- No name is spelled two ways. No new canon line contradicts an older one (search for the subject).

Fix what you find. If two sources disagree and you cannot tell which is right, ask the player.

## 8. Stamp and report

Set the "Last full save" line in `game-state.md` to the real-world date and time and the session number.

Tell the player, briefly and out of character, that the save is complete and what it covers: session number, where the story stands in one sentence, and any discrepancy you corrected. Do not list every file.
