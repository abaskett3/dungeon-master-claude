---
name: new-campaign
description: Create a new solo D&D campaign folder from the templates and run session zero (player preferences, character creation, opening premise). Use when the player wants to start a new adventure or no campaign exists yet.
---

# New campaign

Set up a campaign so that every file a future session needs exists and is filled in before play begins.

## 1. Name and folder

Ask for a campaign title if one was not given (offer two or three ideas if the player has none). Turn it into a folder name: lowercase, words joined by hyphens, no other punctuation. If `adventures/<folder>/` already exists, stop and ask whether to resume it or pick another name.

Copy everything in `templates/campaign/` to `adventures/<folder>/`, then write the folder name as the only line of `adventures/.active-campaign`.

## 1a. Homebrew or published module

Ask whether this is an original adventure or a published module.

For a module:

- Look in `modules/` for it. If it is not there, ask for the file and follow the `ingest-module` skill. The survey and the opening chapter must be ingested before play; the rest can follow later.
- Copy `templates/module-progress.md` to `adventures/<folder>/module-progress.md` and fill in the header and the starting position from the module's `index.md`.
- In session zero, take the edition, starting level and setting from the module, and tell the player those without spoiling the plot. Offer the module's own character hooks or backgrounds if it has them.
- In step 4, the conflict, antagonists and secrets in `dm-notes.md` come from the module's `index.md`. Use that file for how this player's character ties in and for anything you add.
- Modules expect four or five characters. Agree how to handle that: companions run by DMC, a higher starting level, scaled encounters, or a mix. Record the choice in `module-progress.md`.

## 2. Session zero

Have a short out-of-character conversation. Ask a few questions at a time, not a form. Cover:

- **Rules edition**: 2014 or 2024 5e. Starting level.
- **What they enjoy**: combat, intrigue, exploration, puzzles, roleplay; tone (heroic, grim, comic); any content to avoid.
- **Lethality**: story, balanced or gritty. Explain each in a sentence.
- **Dice**: DMC rolls everything and shows the results (default), DMC rolls only for the world, or the player rolls physical dice.
- **Pacing and narration length**, and whether they want suggested actions offered.
- **Setting and premise**: their idea, or pitch three short hooks and let them choose.

Write the answers into `player-options.md`. Leave defaults where the player has no preference.

## 3. The player character

Build the character together, following the rules for the chosen edition. The player makes every choice; you explain options and do the arithmetic. Roll ability scores with the dice script if they want rolled stats.

Create `character-sheets/pc-<name>.md` from `templates/pc-sheet.md` and fill in every field. Double-check derived numbers (modifiers, proficiency bonus, AC, HP, save DCs, attack bonuses) before saving. Ask for a few lines of backstory, a goal and a tie to the setting.

If the player wants a companion, create it the same way from `templates/npc-sheet.md`.

## 4. The world behind the curtain

Before the first scene, write down enough that you will stay consistent later:

- `story/dm-notes.md`: the central conflict, the main antagonist and what they want, two or three secrets, a few possible directions. Keep it loose; this is a plan, not a script.
- `story/canon.md`: the facts the player already knows (setting name, starting town, anyone they know).
- `character-sheets/`: a sheet for each NPC who will matter in the first session.
- `game-state.md`: starting location, date and time, the PC's current HP and resources, the opening quest hook if there is one.
- `story/summary.md`: a two or three sentence premise.
- `story/sessions/session-001.md` from `templates/session-log.md`.

## 5. Begin

Confirm the player is ready, then open the first scene.
