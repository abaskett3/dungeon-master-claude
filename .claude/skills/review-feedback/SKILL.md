---
name: review-feedback
description: Work through the recorded feedback about DMC in feedback/ and tune DMC (CLAUDE.md, the skills, the templates, the hooks, dmc). This is the only skill that reviews feedback, finds the cause and names its kind, proposes changes and, on the user's word, files issues or makes PRs. Use when the user asks to review feedback, work through the feedback, or improve or tune DMC.
---

# Review feedback

This is toolkit work, not play: drop the DM role. The reader is the toolkit's author, so spoilers in entries are fine here unless they say otherwise.

The entries are in `feedback/` of a DMC folder, not of this repository. Take the folder's path from the user's request; without one, run `dmc folder`, which prints the DMC folder on this computer (if it fails, ask the user where it is). `feedback/` and `adventures/` below mean that folder's. The prompts to tune are in `kit/` in this repository: `kit/dm-rules.md` (generated into the DMC folder as `CLAUDE.md`), `kit/skills/dmc-<name>/`, `kit/templates/` and `kit/settings.json`. Players get a change through `dmc update`.

The entries come from two skills: `dmc-record-feedback` (what the player said in play) and `review-transcript` (what the session logs show). This skill is the one place where they are acted on. The aim is to improve the prompts for every future campaign and player. A cause counts only if it traces to something the toolkit says, fails to say, or makes expensive.

## 1. List the open entries

Grep `^status: (open|filed)` in `feedback/` and read each file. A `filed #N` entry is listed so that its issue or PR can be checked and the entry closed once it has shipped. A fresh worker needs no campaign file except where an entry's Context section points.

## 2. Check whether each is already fixed

Read the current prompt files in `kit/`, `git log` since the entry's `recorded:` date, and `gh issue list --state all`. If it is fixed, set the status to `fixed in PR #N` and go on.

## 3. Apply the preference test again

Would the same behaviour have been wrong for a different player with different preferences? If not, the entry is a preference: propose moving it to the player's options. With the user's agreement, write it as a one-line preference (never the incident) in the right `player-options.md`, and set the status to `moved to player options`. If it is not a toolkit problem at all, set `not a toolkit problem: <why>`.

If a preference was already written in the player options and DMC did not follow it, the finding is that the toolkit let an option be ignored. The fix is about how options are read and applied, never about copying the preference into `CLAUDE.md`.

## 4. Pull context only when the entry needs it

```
dmc digest list --folder <dmc folder> --campaign <campaign> --after <date>
dmc digest show --folder <dmc folder> <id>
```

Find the moment with the entry's quote in `adventures/.transcript-review/<id>-turns.txt` in the DMC folder, then read the stretch around it in `<id>-transcript.md`: about ten exchanges before it, so the cause is visible and not only the complaint. Never open raw `.jsonl` logs with Read: they hold page images and whole files inline.

## 5. Find the cause, and name its kind

For each entry read the relevant part of `kit/dm-rules.md`, the skill DMC had loaded (`kit/skills/dmc-<name>/SKILL.md`), the template behind the file it was editing (`kit/templates/`), and `kit/settings.json` for the hooks. Then name one of four kinds of cause, each with the file and line or the log record that shows it (issue #266). A kind you cannot show is not named: say what you would need to read.

1. **The instructions say the wrong thing**, or two of them contradict each other. Example: after a correction, one rule says to redo the beat and the next line says to go on from it. The fix is words: remove or replace the wrong text.
2. **A mechanism is missing, or cannot do its work.** Example: the context hook prints a sheet's looks, stats and traits but not its voice or names (`src/Dmc.Cli/Retrieval/SheetReminder.cs`, `Fields`), and DMC used a wrong name. The fix is the mechanism: an issue in the form of a trial of the slip plan (#241), with no sentence beyond the one that tells DMC to use it (§ 6).
3. **The story files are wrong.** Example: a sheet line DMC wrote says the player chose something they did not. The fix is the data, corrected with the player's ruling; if a skill's step wrote the wrong line, add a note to #262 naming the step. A wrong line DMC wrote with no step at fault is kind 4 as well.
4. **The model does not do it reliably**: the right line was in its context and was not used. Example: the sheet's name line was read two play turns before the slip. Record "the model does not do this reliably" and add no sentence: a rule to look a thing up does not run at the moment of writing. The entry is evidence for a mechanism; link the issue for one, or propose one as in kind 2.

## 6. Report

Lead with a short verdict: the two or three changes that would matter most. Group the entries by cause, most damaging first, and cite the entry file names. For each cause:

- **What happened**, in a line, with the entries' occurrences (how often it was raised says how much it matters).
- **Why the prompts allowed it**: its kind (§ 5), and the file and passage, data or log record that shows it.
- **The change**: which file, and the wording, the mechanism or the data, as its kind allows. Prefer the smallest change that would have prevented it. A hook or a script is better than a paragraph when the problem is that DMC forgot. Say what it costs: every line added to `CLAUDE.md` is paid for in every exchange of every session.

**Only kind 1 ends in changed words** (issue #266; it replaces the hold of #248). A fix of kind 1 removes or replaces text. A fix of kind 1 that makes the file longer (a new or longer sentence in `kit/dm-rules.md`, a skill or a template, a new template field included) is proposed as a slip-test trial (`.docs/development.md` § The slip test) with its pass line, and changes the kit only if it passes. Kinds 2, 3 and 4 never add a sentence to the prompts; a mechanism (a hook, a script, a `dmc` check, a deny rule) may carry the one sentence that tells DMC to use it.

List separately, in a line each, anything that is a model or harness setting and not a prompt (reasoning effort, plugins and connectors loaded into a game session). Say what you did not read.

## 7. Act only when the user says which entries to act on

Then file an issue, or make the change, under CLAUDE.md § Git and GitHub. An entry whose Worker notes say `held until the slip test` (the hold of #248, which ended) is reviewed again under § 5 and § 6; replace that line with the outcome.

- Make each change in the one place it belongs. Do not restate a rule in several files.
- Write for a DMC that has none of this review in front of it: say what to do and why, and leave out the story of the session that prompted it.
- If `kit/dm-rules.md` grows, look for something in it that the entries show DMC never needed, and cut or move it to a skill.
- A change that alters what campaign files should hold (a template, or a skill's rule for a campaign file's shape) adds the next-numbered migration in `kit/migrations/` and its golden fixture under `tests/Dmc.Tests/Fixtures/migrations/NNN/`, written for a campaign its author has never seen, or says in the PR why none is needed (a new optional field, say). The format is `specs/005-campaign-migrations/contracts/migration-file.md`; `.docs/development.md` § Changing a campaign-file standard says how to build and test one. Never add a clause to a skill that repairs older files as it goes.
- Under the entry's Worker notes, write a short note of the cause's kind and what was decided and done, with the issue or PR number, not the diff or the proposed wording. Set the status to `filed #N`, and to `fixed in PR #N` when the PR merges. A kind 4 entry is `filed #N` for the mechanism's issue. A kind 3 entry whose data is corrected is `filed #262` if a step wrote the line, `filed #N` for a mechanism's issue if DMC wrote it with no step at fault (as kind 4), and `not a toolkit problem: the story file was wrong, corrected with the player's ruling` only if someone other than DMC wrote it.
- Remove any stopgap option line that an older entry names under Done in play once its fix has shipped (`dmc-record-feedback` no longer writes them).
- Never delete an entry file: a closed status is the record.
