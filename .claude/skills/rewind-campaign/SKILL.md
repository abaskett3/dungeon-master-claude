---
name: rewind-campaign
description: Take the active campaign back to an earlier moment by agreement with the player, undoing everything after it, and bring every campaign file into line with that moment. Use when the player asks to rewind, redo or undo more than the last exchange, or when a DM mistake has to be unwound.
---

# Rewind campaign

A rewind makes an earlier moment the present again. Afterwards the files must read as if the undone events never happened: a new session resuming from them should find no trace that could be mistaken for fact.

Undoing only the last exchange does not need this skill. Restate the position, correct `game-state.md` and carry on.

## 1. Agree the point

Find the moment the player means in the session logs (`story/sessions/`) and say it back in one line: the day and time, the place, and what had just happened. If their description fits more than one moment, ask which. Do not start changing files until they confirm.

If the player also wants something different after the rewind (easier fights, another route), settle that now, and record any lasting preference in the player options.

## 2. Work out what is undone

Read the session log from the rewind point to the present and list, for yourself:

- HP, hit dice, class resources, spell slots, conditions and death saves as they stood at the point.
- Items, gold, ammunition and consumables spent or gained since, for the character and each companion.
- Enemies killed, hurt, alerted or moved since.
- Facts, rulings, names, promises and maps established since.
- What NPCs have learned or how their attitudes changed since.
- Clocks and the in-world time.

Take the values at the rewind point from the log and the files. Where the log does not give a number, work it out from what the log says happened; tell the player the figure if it is one their character would know, and ask if you cannot determine it.

## 3. Rewrite the files

Use Read and Edit or Write on each file. Do not write a script to do it.

- **`game-state.md`**: rewrite it for the rewind point from the template's layout: scene, party status, tracker, plan, quests, clocks, pending.
- **Character sheets**: restore inventory and resources; remove history, knowledge and relationship notes that came from undone events.
- **`story/canon.md`**: delete the lines established after the point. A ruling about how a rule works may stay if it would apply the same way again; a ruling about a place or event that no longer exists goes.
- **`story/dm-notes.md`**: restore the Opposition table to the point (the dead are alive again, positions reset), and remove plans that only made sense after the undone events.
- **Session log**: cut the undone scenes from the log and add one line where they were: `Rewound on <date> to <moment> at the player's request; N scenes removed.` Do not keep the removed scenes anywhere a later session will read them.
- **`story/summary.md`**: rewrite the recent part.
- **`maps/`**: reset `live.json`; delete maps of places the party has now never seen.
- **`module-progress.md`**, if present: unmark areas, encounters and treasure that are untouched again.

Then search the campaign folder for the names of things that existed only in the undone stretch (a place, an NPC, an item) and for `void`, `rewound` and `~~`, and remove what you find.

## 4. What changes and what does not

- The player has seen what lay ahead; their character has not. Anything the party had not yet discovered at the rewind point is yours to keep or change, and changing some of it keeps the replay honest. Decide, write it in `dm-notes.md`, and do not tell the player which.
- Anything already established before the rewind point stays exactly as it was, including enemy numbers. If a count is being changed as part of the rewind, change it in the Opposition table and nowhere else, and do not announce it.
- Dice already rolled in the undone stretch are gone. New rolls are made when the events come round again.

## 5. Resume

Run the consistency check from `campaign-full-save` (step 7). Then tell the player, out of character and briefly: the moment the game is back at, the party's condition, and any figure you had to reconstruct that they may want to correct. Restate the scene and ask what they do.
