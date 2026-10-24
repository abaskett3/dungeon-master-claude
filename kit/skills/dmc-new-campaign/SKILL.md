---
name: dmc-new-campaign
description: Create a new solo D&D campaign folder from the templates and run session zero (player preferences, character creation, opening premise). Use when the player wants to start a new adventure, to carry on a campaign played elsewhere (a chat export or transcript), or when no campaign exists yet.
---

# New campaign

Set up a campaign so that every file a future session needs exists and is filled in before play begins.

## 1. Name and folder

Ask for a campaign title if one was not given (offer two or three ideas if the player has none). Turn it into a folder name: lowercase, words joined by hyphens, no other punctuation. If `adventures/<folder>/` already exists, stop and ask whether to resume it or pick another name.

Copy everything in `templates/campaign/` to `adventures/<folder>/`, then write the folder name as the only line of `adventures/.active-campaign`.

## 1a. Homebrew or published module

Ask whether this is an original adventure or a published module.

For a module:

- Look in `modules/` for it. If it is not there, ask for the file and follow the `dmc-ingest-module` skill. The survey and the opening chapter must be ingested before play; the rest can follow later.
- Copy `templates/module-progress.md` to `adventures/<folder>/module-progress.md` and fill in the header and the starting position from the module's `index.md`.
- In session zero, take the edition, starting level and setting from the module, and tell the player those without spoiling the plot. Offer the module's own character hooks or backgrounds if it has them.
- In step 4, the conflict, antagonists and secrets in `dm-notes.md` come from the module's `index.md`. Use that file for how this player's character ties in and for anything you add.
- Modules expect four or five characters. Agree how to handle that: companions run by DMC, a higher starting level, scaled encounters, or a mix. Record the choice in `module-progress.md`.

## 1b. A campaign played elsewhere

If the player wants to carry on a campaign played somewhere else (a chat export, a transcript, a handoff note), import it first. Put the files in `adventures/<folder>/import/` and leave them there as the record.

Never read the export into this conversation. Whatever is read stays in context for the rest of the session, so reading a long export in parts still fills it. Give each part its own fresh context instead:

1. Cut the export into parts of about 100 KB, at session or scene breaks, and note each part's line range.
2. For each part in order, start a subagent with the Agent tool. Run them one after another, never at the same time: they write the same files. Tell each one: the campaign folder and its part's line range; to read the part with the Read tool's offset and limit, in pieces small enough for one read; to read the campaign files as earlier parts left them before adding to them; and to write, before it ends, everything its part establishes: canon lines (one per fact, with the session), character sheets from `templates/pc-sheet.md` and `templates/npc-sheet.md`, one session log per original session from `templates/session-log.md`, and DM plans, enemy numbers and anything the player has not seen in `story/dm-notes.md`. Only the last part writes `game-state.md`, as the present moment. Where the export contradicts itself, it adds the contradiction and a proposed reading under "Known inconsistencies" in `canon.md` instead of choosing silently. It replies with a short note only: where it stopped (line and in-world moment) and what it could not settle.
3. If subagents are not available, do one part per session instead: write the files, record where you stopped in `import/progress.md`, and ask the player to type `/clear` and then ask you to continue the import.

When every part is done, read the campaign files as `dmc-resume-campaign` does, run the consistency check from `dmc-campaign-full-save` (step 7), and settle the known inconsistencies with the player. Then go through steps 2 to 4 below only for what the export did not cover, such as the player options.

## 2. Session zero

Have a short out-of-character conversation. Ask a few questions at a time, not a form.

**General preferences.** If `adventures/player-options.md` does not exist, this is the player's first campaign: create it from `templates/player-options.md` and ask about what it covers:

- **Dice**: the default is that the player rolls for their own character and party checks and DMC rolls for everything else. Confirm, or change it.
- **What they enjoy**: combat, intrigue, exploration, puzzles, roleplay; any content to avoid.
- **Pacing and narration length**, and whether they want suggested actions offered.

If the file already exists, read it, tell the player in a line or two what it says, and ask only whether anything should be different for this campaign. Differences go in the campaign file's Overrides section.

**This campaign.** Write these into the campaign's `player-options.md`:

- **Rules edition**: 2014 (default) or 2024 5e. Starting level.
- **Tone**: heroic, grim, comic.
- **Lethality**: story, balanced or gritty. Explain each in a sentence.
- **Setting and premise**: their idea, or pitch three short hooks and let them choose.

Leave defaults where the player has no preference.

## 3. The player character

Build the character together, following the rules for the chosen edition. The player makes every choice; you explain options and do the arithmetic. If they want rolled stats, the player rolls them unless the player options say DMC rolls.

Create `character-sheets/pc-<name>.md` from `templates/pc-sheet.md` and fill in every field. Double-check derived numbers (modifiers, proficiency bonus, AC, HP, save DCs, attack bonuses) before saving. Ask for a few lines of backstory, a goal and a tie to the setting.

If the player wants a companion, create it the same way from `templates/npc-sheet.md`.

## 4. The world behind the curtain

Before the first scene, write down enough that you will stay consistent later:

- `story/dm-notes.md`: the central conflict, the main antagonist and what they want, two or three secrets, a few possible directions. Keep it loose; this is a plan, not a script. Fill in the Opposition table for whatever stands between the character and the first goal, sized for the party as described under "Sizing fights" in `CLAUDE.md`, and check that the party has some way to rest or heal along the way.
- `story/canon.md`: the facts the player already knows (setting name, starting town, anyone they know).
- `character-sheets/`: a sheet for each NPC who will matter in the first session.
- `game-state.md`: starting location, date and time, the PC's current HP and resources, the opening quest hook if there is one.
- `story/summary.md`: a two or three sentence premise.
- `story/sessions/session-001.md` from `templates/session-log.md`.

## 5. Begin

Confirm the player is ready, then open the first scene.
