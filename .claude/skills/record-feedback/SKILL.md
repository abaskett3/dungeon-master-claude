---
name: record-feedback
description: The one and only way to record feedback about how DMC itself runs the game. Invoke it whenever the player gives feedback about DMC, complains about a mistake, an inconsistency, context use, combat procedure, pacing or what DMC offers, corrects a DMC slip, or says "feedback:", and when a slip of your own forced a rewind or a rebuilt file. Never record such feedback by hand, or anywhere else.
---

# Record feedback

Feedback about how DMC itself operates has exactly one route: this skill writes it to `feedback/` at the toolkit root, one file per piece of feedback, so it is never lost. Do not write it anywhere else: not `game-state.md`, `dm-notes.md`, `canon.md`, the session logs, `player-options.md` or a new file in the campaign folder. Do not decide whether it is a player's taste; `review-feedback` does that later. A worker acts on the entries there. During play you only record: never edit `CLAUDE.md`, the skills, the templates or the hooks mid-game. Invoke this skill; do not run its steps by hand.

A request to change a game setting ("make fights harder", "hide the dice math") is not feedback; it is a player option (CLAUDE.md § Player options).

## 1. Look for a duplicate

Grep `^status: (open|filed)` in `feedback/` to list the entries not yet fixed, then Grep `^# ` in `feedback/` and look only at the titles in files from that list. Do not read the entries whole. If one covers the same problem, add a line under Occurrences and the new quote under What the player said (two Edit calls), and go to step 3.

## 2. Write the entry

Create `feedback/YYYY-MM-DD-<slug>.md` from `templates/feedback.md` with one Write call, using today's real-world date, as the harness gives it to you, never the in-world date from `game-state.md`. DMC has no clock, so do not run a shell command for it.

- The title and What happened state the behaviour generally. The in-game details are an example. A worker must understand the entry if the campaign has been deleted.
- Quote the player exactly under What the player said. For a slip of your own, write "none".
- Occurrences: date, campaign folder, story session number, the in-game moment, and the quote to search for in the session log.
- Set `source:` to `player`, or `DMC` for a slip of your own, and pick the `category:` the template defines.
- Done in play: a rewind or ruling you made, or "none". Never a note in a campaign file.
- Point at campaign files only under Context.

## 3. Tell the player

One line, out of character: that it is recorded and the file name. Then go back to the fiction.
