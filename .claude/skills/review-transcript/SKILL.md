---
name: review-transcript
description: Review the recorded play sessions of a campaign to find where DMC made the game worse (context size and token use, story and number consistency, difficulty, DM conduct, pacing and bookkeeping), and record each problem as an entry in feedback/. It proposes and applies no fixes; review-feedback does that. Use when the player asks to review a transcript or a session, or asks how DMC performed.
---

# Review transcript

This is toolkit work, not play: drop the DM role. The reader is the toolkit's author, so spoilers from `dm-notes.md` and the modules are fine here unless they say otherwise.

This skill finds problems in the session logs and records them as feedback entries in `feedback/`, the same place `record-feedback` writes to during play. It does not find the cause in the prompts, propose fixes or edit anything: `review-feedback` does that. A finding is a problem that would be wrong for any player, not this player's taste (section 5).

## 1. Pick the sessions

```
./bin/dmc digest list --campaign <folder>
```

Use the campaign and sessions the user named. Otherwise take the sessions for the active campaign that are not yet listed in `adventures/.reviewed-sessions`. The newest session in the list is usually this review itself; leave it out. Sessions with only a message or two are rarely worth reading.

A transcript that did not come from Claude Code (for example a chat export in a campaign's `import/` folder) has no statistics. Read it as text and apply section 4 only.

## 2. Build the digest

```
./bin/dmc digest show <id> [<id> ...]
```

This writes to `adventures/.transcript-review/`:

- `stats.md`: the numbers for each session.
- `<id>-turns.txt`: every player message with the context size at that point and the size and tool calls of the reply. A `!` marks wording that often goes with friction; it is a hint, and it misses most of it.
- `<id>-transcript.md`: the whole session in order, with tool calls and results cut to a line each.

Never open the raw `.jsonl` logs with Read. They hold page images and whole files inline and will fill the context.

## 3. Read

1. `stats.md`, then each `-turns.txt` from top to bottom. The player's side of the conversation shows where the game went well and where it went wrong faster than anything else.
2. In each `-transcript.md`, read in full the stretches around every problem: about ten exchanges before it, so the cause is visible and not only the complaint. Skim the rest. A session where the player stopped early, rewound, or corrected DMC deserves a complete read from the point things started to slide.

Check `git log` for changes to the prompts made after the session. Do not record something that has already been fixed.

## 4. What to look for

**Context and tokens**

- Where the context starts, how fast it grows per player message, and what the biggest jumps were.
- Files read whole when a section or a search would do, and files read more than once.
- A file edited many times in one reply, or a file that keeps growing (compare its size in the transcript with its template). Bookkeeping that costs more calls than the play it records.
- Output tokens far above the visible text and tool calls: that is reasoning effort, a model setting and not a prompt, but report it.
- How much of the starting context is the toolkit's own text (`CLAUDE.md`, loaded skills) and how much is the harness.
- Long replies to short questions, and blocks repeated unchanged from one reply to the next.

**Consistency**

- A name, number, count, distance, position or time that changed without an in-game cause. For each, find whether the fact was in the files when DMC got it wrong.
- DMC accepting the player's recollection, or its own, without checking the files.
- A map or description that disagreed with the state, or showed something that had not happened.
- A ruling applied differently the second time, or a rule stated wrongly and corrected later.
- After a compaction, a new session or a rewind: anything lost, restored wrongly or left stale in a file.

**Difficulty**

- For each fight: the enemy's numbers against the party's, how they were chosen and when. Was the encounter sized for the party actually present before it began?
- The player dropping to 0, running out of resources, or having no reasonable way to retreat, rest or recover. Whether DMC used the tools the lethality setting gives it, or attacked a downed character.
- Difficulty the player only learned about after committing. Odds stated late or contradicted.
- The opposite as well: fights with no tension, or danger that was announced and never arrived.

**DM conduct**

- Acting, speaking or deciding for the player's character; spending their actions, reactions or resources unasked; treating a question as an action.
- Rules talk or mechanical costs dropped into a roleplay moment.
- Spoilers: enemy numbers, hidden counts, what lies ahead, the contents of `dm-notes.md`.
- Advice that was wrong, or that steered the player off their own plan. Inventing obstacles to a plan and then solving them at length.
- Rolls asked for without the notation, modifier and target, or rolled on the player's behalf.

**Pacing and fun**

- Stretches where nothing happened and the player had to keep saying "ready again" or "end turn".
- Rounds of clarifying questions before anything could be resolved.
- What the player plainly enjoyed (long in-character messages, quick replies, plans of their own). Did DMC give them more of it?
- The moment the mood turned, and what DMC had done in the few exchanges before.

**Tools and saving**

- Tool errors and their cause. Edits that failed because the file was not as DMC remembered it.
- Shell commands used where Read, Edit or Grep fits, and anything that would have raised a permission prompt in the middle of play.
- Skills that should have run and did not, or ran and were not followed.
- Whether a new session could really resume from the files: what did resume get wrong or have to ask?

## 5. Keep preferences out of the entries

Some of what you find is this player's taste: how long they like replies, how much mechanical detail, how hard they want the fights, what format they like. That belongs in `player-options.md`, and it is not an entry.

The test: would the same behaviour have been wrong for a different player with different preferences? If yes, it is an entry. If the preference was already written in the player options and DMC did not follow it, the entry is that the toolkit let an option be ignored. A finding that is the player's taste is not an entry: list it in the report, and with the user's agreement write it as a one-line preference in the right `player-options.md`.

## 6. Record

Add the reviewed session ids, one per line with today's date, to `adventures/.reviewed-sessions` first, so that an interrupted review is not repeated. Then Grep `^status: (open|filed)` in `feedback/` and Grep `^# ` in `feedback/`, looking only at the titles in files from that list, so that a problem already recorded gets a new occurrence and not a second file.

Write each inconsistency or mistake as an entry from `templates/feedback.md` with `source: transcript review`, in `feedback/YYYY-MM-DD-<slug>.md`. Describe the behaviour generally, so that it makes sense without the adventure. Put the Claude Code session id, the time and the quote or the numbers under Occurrences. If an open entry covers the problem, add an occurrence to it instead.

Group the context and token numbers in one entry per cause, with a small table. Anything that is a model or harness setting and not a prompt (reasoning effort, plugins and connectors loaded into a game session) is an entry too, in the category `other`, saying so.

Then give the user a short report: the verdict on how the sessions went, the entries created and extended (file names, one line each), the player preferences you noticed, and what you did not read.

Acting on the entries, which means finding the cause in the prompts, proposing changes and making them, is `review-feedback`.
