---
name: resume-campaign
description: Load a saved campaign from disk and recap it for the player. Use at the start of every play session, after a context compaction or /clear, or whenever DMC is unsure of the current state of the game.
---

# Resume campaign

Rebuild your picture of the campaign from the files. Do not rely on anything you think you remember.

## 1. Find the campaign

Use the campaign the player named. Otherwise read `adventures/.active-campaign`. If neither gives one, list the folders in `adventures/` and ask. Write the chosen folder name to `adventures/.active-campaign`.

## 2. Read, in this order

1. `player-options.md`: how to run the game for this player.
2. `game-state.md`: where things stand right now.
3. `story/summary.md`: the story so far.
4. `story/dm-notes.md`: your plans, secrets and open threads.
5. Every `character-sheets/pc-*.md`, and the sheets of any companions and NPCs present in the current scene.
6. The most recent file in `story/sessions/`, for the texture of the last few scenes.
7. `story/canon.md`: read it all while it is short; once it is long, read the Rulings section and search the rest as subjects come up.
8. Any map in `maps/` for the current location.
9. If `module-progress.md` exists, this is a published-module campaign: read it, then the module's `index.md`, then the section of the chapter digest it names under "Digest to read before continuing". If that chapter has not been ingested, say so and ingest it before play.

Read other NPC sheets and older session logs only when they become relevant.

## 3. Check the state

Compare `game-state.md` against the PC sheet and the end of the last session log. Look for disagreements: HP or slots that do not match, an item in one place but not the other, a location that differs, a fight the log says ended but the tracker still shows.

If you find any, tell the player out of character what disagrees, propose the most likely truth, and fix the files once they confirm. Do not silently pick one.

If `game-state.md` shows the last full save was older than the last session log entry, the previous session ended without a full save. Say so, and bring `summary.md` up to date from the session log before continuing.

## 4. Start or continue the session log

If this is a new play session, create the next `story/sessions/session-NNN.md` from `templates/session-log.md`. After a compaction in the same session, keep using the current one.

## 5. Recap and resume

Give the player a short recap in the style they asked for in `player-options.md`: where they are, what just happened, what they were about to do, and their current condition (HP, key resources). Mention open quests in a line. Then restate the immediate scene and ask what they do.

After a mid-session compaction, skip the recap unless the player asks; restate only the immediate situation.
