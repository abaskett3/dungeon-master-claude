# Product overview

This document is for anyone who wants to know what Dungeon Master Claude is, what it does and what it leaves out, before playing it or working on it.

## What it is

Dungeon Master Claude (DMC) is a toolkit that turns [Claude Code](https://claude.com/claude-code) into the Dungeon Master for a solo game of Dungeons & Dragons 5th edition. The player installs the toolkit's program, `dmc`, which writes a DMC folder for them (the Dungeon Master rules, skills and settings, beside their own campaigns); they start a session with `dmc play`, and play by typing what their character does. Claude, acting as DMC, narrates, runs the non-player characters and monsters, applies the rules and keeps the books.

The whole campaign lives in plain Markdown files on the player's machine. Every session starts by reading those files, so a campaign can be picked up weeks later, after a crash, or after the conversation has been cleared, and carry on where it stopped.

It is for one person who wants to play D&D 5e without a group, and who is comfortable starting a program in a terminal. Players install Claude Code and `dmc`, and nothing else; `dmc` installs itself and writes and updates the DMC folder, and never touches the player's own files.

## Two principles

[kit/dm-rules.md](../kit/dm-rules.md), DMC's operating rules (a DMC folder's `CLAUDE.md`), puts two things above everything else:

1. **Consistency.** What was true last scene, last session and last month is still true now. The numbers on the sheet are the numbers in play. A new session with no memory must be able to continue from the files alone.
2. **Fun.** The rules and the bookkeeping serve the story, and the story serves the player.

When they pull against each other, DMC looks for the ruling that keeps both. Most of the toolkit exists to serve the first principle cheaply enough that it does not get in the way of the second.

## What it does

| Area | What the player gets | Where it is defined |
|---|---|---|
| Campaigns | `/dmc-new-campaign` runs a short session zero (preferences, edition, tone, lethality, character creation) and creates the campaign folder from templates. Several campaigns can exist side by side. A campaign played elsewhere can be imported from a chat export. | `dmc-new-campaign` skill, `kit/templates/` |
| Saving | DMC writes every change to the files as it happens: HP, resources, position, new facts, scene beats. `/dmc-campaign-full-save` also rewrites the summary and cross-checks the files against each other. | kit/dm-rules.md § Saving as you go, `dmc-campaign-full-save` skill |
| Resuming | `/dmc-resume-campaign` reloads a campaign from disk, checks the files agree, and recaps. | `dmc-resume-campaign` skill |
| Dice | By default the player rolls for their own character; DMC asks in dice notation with the modifiers and the target included. DMC's own rolls come from `dmc roll`, a real random-number roller, and are shown as the player options ask. | kit/dm-rules.md § Dice, `dmc roll` |
| Maps | When positions matter, a map opens in the player's browser and redraws itself as creatures move. Distances come from `dmc map`, not from DMC's estimate. | `dmc-draw-map` skill, `dmc map` |
| Published modules | `/dmc-ingest-module` turns the player's own copy of an adventure (PDF, Markdown, text, HTML, EPUB) into an index and chapter digests with page references, so DMC runs it from the text and tracks progress through it. | `dmc-ingest-module` skill, `dmc pdf` |
| Rules lookup | An optional local copy of the free System Reference Document in `rules/`, indexed by `/dmc-setup-rules`, so rulings come from the text rather than memory. | `dmc-setup-rules` skill |
| Breaks | Before each player message, a hook tells DMC how full the conversation is. Past a threshold DMC offers a save and a fresh session at the next natural pause, which keeps sessions cheap and accurate. | `dmc context-check`, kit/dm-rules.md § Compaction and long sessions |
| Rewinds | `/dmc-rewind-campaign` takes the campaign back to an earlier moment by agreement and makes every file read as if the undone events never happened. Files DMC may not delete (maps of places the party has now never seen) are listed for you to confirm and delete yourself, and are not used meanwhile. | `dmc-rewind-campaign` skill |
| Feedback about DMC | In play, the player's feedback about how DMC itself runs the game (a mistake, a drifting fact, a badly run fight) is recorded as one file in `feedback/` (with the `dmc` version), outside the campaign; a repeat adds an occurrence to the same file. A player's taste is a player option, not feedback. | `dmc-record-feedback` skill, `kit/templates/feedback.md` |
| Session review | `/review-transcript` reads the recorded sessions of a campaign and records the problems it finds as entries in `feedback/`. It proposes and applies no fixes. | `review-transcript` skill, `dmc digest` |
| Tuning DMC | `/review-feedback` works through the open entries, finds the cause in the prompts, reports, and on the user's word files issues or makes the changes. It is the only skill that tunes DMC. | `review-feedback` skill |

The player only needs to type `/dmc-new-campaign`, `/dmc-resume-campaign` and `/dmc-campaign-full-save`, plus Claude Code's own `/clear` at a break. DMC uses the other skills on its own.

## How it is put together

DMC is mostly text: the rules of conduct, ten player skills (`dmc-*`) that describe the procedures, and blank campaign files, all kept in `kit/` in this repository and written into a DMC folder by `dmc` (the repository's own `CLAUDE.md` and two review skills are for developers). Development and play are apart: a change to `kit/` reaches a player only with a new `dmc`. The campaign files themselves are the source of truth; the conversation is only a cache of them. One small native program, `dmc`, does what a language model should not do by itself: roll dice, draw maps and measure distances, extract text and page images from PDFs, condense session logs, and measure the conversation's size; it also installs itself and writes, refreshes and starts the DMC folder. [architecture.md](architecture.md) describes how these fit together.

## What it deliberately does not do

- **It does not support more than one player at the keyboard.** Sheets and state are kept per character, so a second character fits the files, but the turn handling and out-of-character rules assume one person.
- **It does not ship rulebook text or adventures.** The rules reference and any module are the player's own copies, kept in folders of their own that `dmc` never writes. DMC falls back to its knowledge of the rules, a web lookup of the SRD, or a ruling it records.
- **It does not play the player's character.** DMC never decides, speaks or acts for the character, never ends a turn without asking, and treats a question as a question, not a go-ahead.
- **It does not roll the player's dice** unless asked, and it never changes a die that has been shown. At the default lethality it keeps a campaign alive through other means (enemies that flee or take prisoners, a way out), and never says it went easy.
- **It does not run a module from memory.** It runs from the ingested digest and the original pages, and does not railroad: leaving the script is recorded, not blocked.
- **It does not upload anything.** Maps are local HTML files with no server; session digests are written to `adventures/.transcript-review/` in the DMC folder, one local folder shared by all campaigns and rewritten on each review, and feedback entries to `feedback/`, which is never rewritten and never written by `dmc install`, `refresh` or `play`; PDFs are read on the player's machine.
- **It does not need Python, .NET or any other runtime on the player's machine.** `dmc` is a self-contained native program. Until the first release of the separated install it is built from source; the checked download that replaces that comes with the release.
- **It is not signed.** No code signing or notarization; the Windows SmartScreen check on fresh machines was deliberately skipped (see [decisions.md](decisions.md)).
- **It does not prevent context compaction.** It saves as it goes so that a compaction loses nothing, and offers breaks so that long conversations are rare.
- **It is not a rules engine.** `dmc` rolls, draws and reads; the rules are applied by DMC, following the edition set in the player options.
