---
name: record-feedback
description: Record feedback about how DMC itself runs the game, outside the campaign. Use when the player gives feedback about DMC, complains about a mistake, an inconsistency, context use, combat procedure or pacing, corrects a DMC slip that would be wrong for any player, says "feedback:", or when a slip of your own forced a rewind or a rebuilt file. Not for the player's tastes, which are lines in player-options.md.
---

# Record feedback

Feedback about how DMC itself operates is not part of the story, so it goes outside the campaign, to `feedback/` at the toolkit root, one file per piece of feedback. A worker acts on it later with `review-feedback`. During play you only record: never edit `CLAUDE.md`, the skills, the templates or the hooks mid-game.

DMC feedback never goes into `game-state.md`, `dm-notes.md`, `canon.md`, the session logs or a new file in the campaign folder.

## 1. Classify

Would the same behaviour have been wrong for a different player with different preferences?

- **No**: it is a player option (HP always shown, shorter replies, fights harder). Write it as a one-line preference, never the incident behind it, in the campaign's `player-options.md`, or in `adventures/player-options.md` if it applies to every campaign (CLAUDE.md § Player options; ask "this campaign or all of them?" if unclear). Stop here.
- **Yes**: it is DMC feedback. Go on.
- **Both**: write the option line and an entry, and name each in the other.

## 2. Look for a duplicate

Grep `^status: (open|filed)` in `feedback/` to list the entries not yet fixed, then Grep `^# ` in `feedback/` and look only at the titles in files from that list. Do not read the entries whole. If one covers the same problem, add a line under Occurrences and the new quote under What the player said (two Edit calls), and go to step 5.

## 3. Write the entry

Create `feedback/YYYY-MM-DD-<slug>.md` from `templates/feedback.md` with one Write call, using today's real-world date, as the harness gives it to you, never the in-world date from `game-state.md`. DMC has no clock, so do not run a shell command for it.

- The title and What happened state the behaviour generally. The in-game details are an example. A worker must understand the entry if the campaign has been deleted.
- Quote the player exactly under What the player said. For a slip of your own, write "none".
- Occurrences: date, campaign folder, story session number, the in-game moment, and the quote to search for in the session log.
- Set `source:` to `player`, or `DMC` for a slip of your own, and pick the `category:` the template defines.
- Point at campaign files only under Context.

## 4. Stopgap

Only if the player wants the change applied in this campaign now, before the toolkit is fixed: write one preference line in the campaign's `player-options.md`, and name that file and line under Done in play so the worker can remove it once the fix ships. Also note under Done in play any rewind or ruling made.

## 5. Tell the player

One line, out of character: that it is recorded and the file name. Then go back to the fiction.
