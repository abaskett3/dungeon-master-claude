---
name: review-feedback
description: Work through the recorded feedback about DMC in feedback/ and tune DMC's prompts (CLAUDE.md, the skills, the templates, the hooks). This is the only skill that reviews feedback, finds the cause in the prompts, proposes changes and, on the user's word, files issues or makes PRs. Use when the user asks to review feedback, work through the feedback, or improve or tune DMC.
---

# Review feedback

This is toolkit work, not play: drop the DM role. The reader is the toolkit's author, so spoilers in entries are fine here unless they say otherwise.

The entries in `feedback/` come from two skills: `record-feedback` (what the player said in play) and `review-transcript` (what the session logs show). This skill is the one place where they are acted on. The aim is to improve the prompts for every future campaign and player. A cause counts only if it traces to something the toolkit says, fails to say, or makes expensive.

## 1. List the open entries

Grep `^status: open` in `feedback/` and read each file. A fresh worker needs no campaign file except where an entry's Context section points.

## 2. Check whether each is already fixed

Read the current prompt files, `git log` since the entry's `recorded:` date, and `gh issue list --state all`. If it is fixed, set the status to `fixed in PR #N` and go on.

## 3. Apply the preference test again

Would the same behaviour have been wrong for a different player with different preferences? If not, the entry is a preference: propose moving it to the player's options. With the user's agreement, write it as a one-line preference (never the incident) in the right `player-options.md`, and set the status to `moved to player options`. If it is not a toolkit problem at all, set `not a toolkit problem: <why>`.

If a preference was already written in the player options and DMC did not follow it, the finding is that the toolkit let an option be ignored. The fix is about how options are read and applied, never about copying the preference into `CLAUDE.md`.

## 4. Pull context only when the entry needs it

```
./bin/dmc digest list --campaign <folder> --after <date>
./bin/dmc digest show <id>
```

Find the moment with the entry's quote in `adventures/.transcript-review/<id>-turns.txt`, then read the stretch around it in `<id>-transcript.md`: about ten exchanges before it, so the cause is visible and not only the complaint. Never open raw `.jsonl` logs with Read: they hold page images and whole files inline.

## 5. Find the cause in the prompts

For each entry read the relevant part of `CLAUDE.md`, the skill DMC had loaded, the template behind the file it was editing, and `.claude/settings.json` and `.claude/play-settings.json` for the hooks.

## 6. Report

Lead with a short verdict: the two or three changes that would matter most. Group the entries by cause, most damaging first, and cite the entry file names. For each cause:

- **What happened**, in a line, with the entries' occurrences (how often it was raised says how much it matters).
- **Why the prompts allowed it**: the file and passage that caused it, or what is missing.
- **The change**: which file, and the wording or the mechanism. Prefer the smallest change that would have prevented it. A hook or a script is better than a paragraph when the problem is that DMC forgot. Say what it costs: every line added to `CLAUDE.md` is paid for in every exchange of every session.

List separately, in a line each, anything that is a model or harness setting and not a prompt (reasoning effort, plugins and connectors loaded into a game session). Say what you did not read.

## 7. Act only when the user says which entries to act on

Then file an issue, or make the change, under CLAUDE.md § Git and GitHub.

- Make each change in the one place it belongs. Do not restate a rule in several files.
- Write for a DMC that has none of this review in front of it: say what to do and why, and leave out the story of the session that prompted it.
- If `CLAUDE.md` grows, look for something in it that the entries show DMC never needed, and cut or move it to a skill.
- Write each change under the entry's Worker notes. Set the status to `filed #N`, and to `fixed in PR #N` when the PR merges.
- Remove any stopgap option line named under Done in play once its fix has shipped.
- Never delete an entry file: a closed status is the record.
