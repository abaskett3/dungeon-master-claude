---
name: resume-campaign
description: Load a saved campaign from disk and recap it for the player. Use at the start of every play session, after a context compaction or /clear, or whenever DMC is unsure of the current state of the game.
---

# Resume campaign

Rebuild your picture of the campaign from the files. Do not rely on anything you think you remember.

## 1. Find the campaign

Use the campaign the player named. Otherwise read `adventures/.active-campaign`. If neither gives one, list the folders in `adventures/` and ask. Write the chosen folder name to `adventures/.active-campaign`.

## 2. Read, in this order

Open each file with the Read tool, one call per file (several calls can go in one message). Do not join files together with a shell command: output over a few thousand words is cut to a short preview, and you would resume without having read your own notes. If any read comes back shortened, read that file again in parts before going on.

1. `adventures/player-options.md` (the player's general preferences; create it from `templates/player-options.md` if missing), then the campaign's own `player-options.md`, which overrides it.
2. `game-state.md`: where things stand right now.
3. `story/summary.md`: the story so far.
4. `story/dm-notes.md`: your plans, secrets and open threads.
5. Sheets, and only these three kinds: every `character-sheets/pc-*.md`; the sheet of each companion, meaning each character with their own block under `game-state.md`'s Party status (the "With the party, not companions" block is not a companion); and the sheet of each NPC present in the current scene (named in the Scene block's Present line or in the combat tracker), including anyone listed under "With the party, not companions" who is present. Any other sheet, however recurring the character, is not read now.
6. The most recent file in `story/sessions/`, for the texture of the last few scenes. Find its real scene headings with the Grep tool (pattern `^### Scene [0-9]+:`, line numbers on, `head_limit` 0; the pattern skips the template's commented example, `### Scene N:`). Then:
   - **Read the tail**: Read from the third-last scene heading to the end of the file (offset and limit). A log with fewer than three scenes is read whole.
   - **Read the whole log instead** when the previous session may have ended without a full save: the "Last full save" line in `game-state.md` ends "through scene N" and the log has scenes after N, or it names an earlier session than this log, or it does not say which scene it went through. Section 3 then rebuilds `summary.md` from the whole log.

   Either way, read the log once; do not read it again in section 3, "Check the state".
7. `story/canon.md`, by its size. Count its lines with the Grep tool (pattern `^`, count mode).
   - **Under about 230 lines** (the archive level of `campaign-full-save`, step 4): read it whole.
   - **Past that**: do not read it whole. Find its section headings with Grep (pattern `^## `, line numbers on, `head_limit` 0). Read these sections whole, whatever the scene, because leaving one out is how a ruling or a promise gets contradicted: **Rulings**, and **Prices, debts and promises**. Read any other section whose comment under its heading says "Always read". Then search the rest, case-insensitive, for the names, places and threads in `game-state.md`'s Scene block (Location, Present, What is happening, The player was about to), its Active quests and its Pending lines: one Grep call, the terms joined with `|`, `head_limit` 0 so nothing is cut off, lines shown. Use the distinctive word of a name (a surname, a place) and put a backslash before any of `. ( ) [ ] { } * + ? ^ $ | \`; if the pattern is rejected, search the terms one at a time. Search again whenever a subject comes up that you have not read (CLAUDE.md § Accuracy). Do not archive anything now: that belongs to the full save, which may rightly keep a long canon when every subject in it is still live.
8. Any map in `maps/` for the current location. If a fight is running or the scene names a map, run `./bin/dmc map adventures/<campaign>/maps/live.json --open` so the player has it on screen again (see the `draw-map` skill), and check its positions against the combat tracker.
9. If `module-progress.md` exists, this is a published-module campaign: read it, then the module's `index.md`, then the section of the chapter digest it names under "Digest to read before continuing". If that chapter has not been ingested, say so and ingest it before play. If its chapter status shows the module finished, read neither `index.md` nor the digest.

Read other NPC sheets and older session logs only when they become relevant. Do not read `story/canon-archive.md` or `story/dm-notes-archive.md`: they hold what can no longer come up, and are searched when a subject returns.

## 3. Check the state

Compare `game-state.md` against the sheets of the PCs and of the companions in the party, and the end of the last session log. Look for disagreements: HP or slots that do not match, an item in one place but not the other, a location that differs, a fight the log says ended but the tracker still shows.

If you find any, tell the player out of character what disagrees, propose the most likely truth, and fix the files once they confirm. Do not silently pick one.

If the last full save was older than the last session log entry (the test is in step 6: scenes after the "through scene N" stamp, an earlier session, or no stamp to compare), the previous session ended without a full save, and you have read the whole log. Say so, and bring `summary.md` up to date from the session log before continuing.

## 4. Start or continue the session log

If this is a new play session, create the next `story/sessions/session-NNN.md` from `templates/session-log.md`. After a compaction, or a planned break (step 5), keep using the current one.

## 5. Recap and resume

Give the player a short recap in the style the player options ask for: where they are, what just happened, what they were about to do, and their current condition (HP, key resources). Mention open quests in a line. Then restate the immediate scene and ask what they do.

After a mid-session compaction, skip the recap unless the player asks; restate only the immediate situation.

After a planned break, the same: if Pending in `game-state.md` has the line `Planned break: restate the immediate situation only.`, skip the recap, restate the immediate situation (in a fight: the round, whose turn it is, positions, and any plan or choice that was pending), and delete the line.
