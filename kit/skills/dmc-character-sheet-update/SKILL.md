---
name: dmc-character-sheet-update
description: Create or update a character sheet for a player character, companion or NPC in the active campaign, including stats, inventory, notes, objectives, relationships and backstory. Use on level-up, when items or abilities change, when an NPC is introduced or becomes recurring, or when a character's goals, knowledge or attitude change.
---

# Character sheet update

Sheets live in `character-sheets/` in the active campaign folder: `pc-<name>.md` for player characters, `npc-<name>.md` for companions and recurring NPCs, and `minor-npcs.md` for one-line entries.

A sheet must let a DM who has never met the character play them correctly: the numbers for the rules, and the voice, wants and history for the story.

## Which kind of record

- **PC or companion**: full sheet from `templates/pc-sheet.md`.
- **Recurring NPC, villain, patron, rival, anyone with a stat block the player may fight or travel with**: sheet from `templates/npc-sheet.md`.
- **Shopkeeper, guard, passer-by with a name**: one line in `minor-npcs.md` (name, role, place, one distinguishing trait, attitude to the PC). Promote to a full sheet on their second meaningful appearance.

## Updating

1. Read the existing sheet first. Edit the lines that changed; do not rewrite the sheet.
2. On a rules change, recompute everything that depends on it. A level-up touches HP, hit dice, proficiency bonus, features, spell slots, spells known, save DC and attack bonuses. An ability score change touches modifiers, saves, skills, AC, attacks and DCs. Follow the edition in `player-options.md`.
3. On a level-up, the player makes the choices (subclass, feats, spells, ability increases). Explain the options, then record what they pick. Roll or use fixed HP according to `player-options.md`.
4. Keep the story sections current: what the character wants now, what they know, how they feel about the PC, what they have promised or been promised, what has happened to them. Add dated notes to the sheet's `## History` section, which is its last section, instead of deleting old ones when history matters. The full save moves history older than the current stretch of the story to a separate file, so the sheet stays a size resume can read.
5. For NPCs, keep the voice note (speech pattern, mannerism, a sample line) and the secrets section up to date. Secrets are spoilers; do not show the player an NPC sheet without warning them.
6. If current HP, slots or conditions changed, `game-state.md` is where those live during play. Update it too so the two agree.

## New character

Fill in every field of the template that applies; write "none" instead of leaving a field blank, so a later reader knows it was considered. Add a line to `story/canon.md` for the character's first appearance.

## Check

Before finishing, verify the derived numbers once more, and confirm the name is spelled the same as everywhere else in the campaign.
