---
name: dmc-record-feedback
description: The one and only way to record feedback about how DMC itself runs the game. Invoke it whenever the player says something is wrong with DMC (a mistake, an inconsistency, context use, combat procedure, pacing, what DMC offers), corrects a DMC slip, or says "feedback:", and when a slip of your own forced a rewind or a rebuilt file. Never record such feedback by hand, or anywhere else. A preference about the player's own game is a player option, not feedback.
---

# Record feedback

Feedback is something wrong with how DMC itself operates, which DMC should change. It has exactly one route, so it is never lost: this skill writes it to `feedback/` in the DMC folder, one file per piece of feedback. It never goes into `game-state.md`, `dm-notes.md`, `canon.md`, the session logs, a `player-options.md` or a new file in the campaign folder, and never as a note on another entry. Invoke this skill; do not run its steps by hand. A worker acts on the entries later with `review-feedback`. During play you only record: never edit `CLAUDE.md`, the skills, the templates or the hooks mid-game.

## 1. Classify

A player option is a preference about their own game that the player wants changed (HP always shown, shorter replies, fights harder). Feedback is something wrong with DMC. The test: would the same behaviour have been wrong for a different player with different preferences?

- **No**: it is a player option. Write it as a one-line preference, never the incident behind it, in the campaign's `player-options.md`, or in `adventures/player-options.md` if it applies to every campaign (CLAUDE.md § Player options; ask "this campaign or all of them?" if unclear). Stop here.
- **Yes**: it is feedback. Go on.
- **Both** in one message: handle each part on its own, the option line in the options file and the entry in `feedback/`, with no reference from one to the other.

## 2. Look for a duplicate

Grep `^status: (open|filed)` in `feedback/` to list the entries not yet fixed, then Grep `^# ` in `feedback/` and look only at the titles in files from that list. Do not read the entries whole. If one covers the same problem, add a line under Occurrences and the new quote under What the player said (two Edit calls), and go to step 4.

## 3. Write the entry

Create `feedback/YYYY-MM-DD-<slug>.md` from `templates/feedback.md` with one Write call, using today's real-world date, as the harness gives it to you, never the in-world date from `game-state.md`. DMC has no clock, so do not run a shell command for it.

- The title and What happened state the behaviour generally. The in-game details are an example. A worker must understand the entry if the campaign has been deleted.
- Quote the player exactly under What the player said. For a slip of your own, write "none".
- Occurrences: date, campaign folder, story session number, the in-game moment, and the quote to search for in the session log.
- Set `source:` to `player`, or `DMC` for a slip of your own, and pick the `category:` the template defines.
- Set `dmc:` to the version number alone: run `dmc --version` and drop the leading `dmc ` (`dmc: 0.2.0`); it is the only shell command this step runs.
- Done in play: a rewind or ruling you made, or "none".
- Point at campaign files only under Context.

## 4. Tell the player

One line, out of character: that it is recorded and the file name. Then go back to the fiction.
