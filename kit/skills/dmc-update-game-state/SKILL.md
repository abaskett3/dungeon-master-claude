---
name: dmc-update-game-state
description: Bring the active campaign's game-state.md in line with the present moment of play (location, time, HP, resources, conditions, combat tracker, quests). Use after any change to those things, at the end of each combat round, and as part of a full save.
---

# Update game state

`game-state.md` in the active campaign folder describes right now. It holds no history; that belongs in the session log.

## How to update

1. Read the current file first. Compute new values from what is written there, not from memory.
2. Make one edit per reply. Work out everything that changed in this exchange first, then write it in a single call: for a fight, replace the whole combat tracker block; otherwise replace the section that changed. A string of small edits costs more than the play it records, and they fail when the file is not quite as you remember it.
3. Keep the section layout from the template so every session finds things in the same place.
4. Write the state, not the story of how it got there. A tracker row says where a combatant is and what condition it is in now; what it did last round goes in the session log at the end of the scene. If the file has grown well past the template's shape, rewrite it from the template with only current values.
5. Write only what happened in play or what the player said. Something you inferred (who slept, where an NPC went) is marked as an inference or left out, because whatever is in this file is later narrated as fact.

## What to keep current

- **Scene**: location (specific enough to resume from), in-world date and time, weather or light if it matters, who is present, what is happening and what the player was about to do. "What is happening" is the present situation in a sentence or two; how it came about is in the summary and the session log.
- **Party status**: a block for each PC and companion, and nobody else, because resume reads a sheet for every block there. Someone with the party who is not a companion (a rescued NPC, a prisoner, a guest) is one line under "With the party, not companions" instead. For each PC and companion, current and max HP, temporary HP, hit dice left, spell slots left per level, class resources (rage, ki, channel divinity and so on), conditions and their remaining duration, concentration, exhaustion, death saves, inspiration.
- **Carried changes**: gold and consumables spent or gained (ammunition, potions, rations, charges). The full inventory lives on the character sheet; update both when an item is gained or lost.
- **Combat tracker**, while a fight is running: round number; the current turn's movement used and left, and whether its action, bonus action, reaction and object interaction are spent; initiative order; each combatant's AC, current and max HP, conditions, position, whether it has acted this round, and what it has readied (trigger → action); and each enemy's stat block with every attack, its reach or range and what it needs (see CLAUDE.md § Sizing fights). Clear the Acted column at the top of each round, and a Readied entry when it fires or the creature's next turn starts. When a map is up, the position is the map cell (`E4`) and must match `maps/live.json`, and so must the initiative order and whose turn it is (its `"initiative"` and `"current"`); CLAUDE.md § Maps gives the order of writing, narrating and redrawing. When the fight ends, log the outcome, then:
  - add one line for the fight under Timeline in `story/canon.md`: what the characters did or said that others may talk about later (a deed, a command, a line), with `sN.M`, so the moment can be found when those characters come up again. The player sees this edit: use only names the character knows, and no enemy counts;
  - in the Opposition table in `story/dm-notes.md`, move the rows of groups the fight finished, with this fight's stat blocks, to `story/dm-notes-archive.md` (create it if missing), where a rewind can restore them; resume does not read it. A group with members left (some never fought, or one fled and could return) keeps its row with the new count, where it went, and what the party knows;
  - clear this section.
- **Quests**: active, completed and failed, each with a one-line status.
- **Clocks**: anything counting down (a ritual in three days, a ship leaving at dawn, a spell that lasts an hour).
- **Pending**: an unresolved roll, a question the player has not answered, a choice in front of them.
- **Plan and standing orders**: each agreed plan not yet carried out, one line per person, trigger → action, with who stands where and who holds what. A plan that lives only in the conversation is lost at a break or a compaction. It sits just after the tracker, so a fight's edit can take both. Keep only orders that apply where the party is now or to what is about to happen. A standing order that still holds but is not in play (camp rules, a signal code, a bounty) is a fact: move it to `story/canon.md`, where it is found when its subject returns, and delete one that no longer applies.

## Rests

On a short or long rest, apply the edition's recovery rules to every PC and companion, advance the clock, tick any countdowns, and write all of it in one pass.

## Check

After editing, confirm that HP is not above max, no slot count is negative or above its max, and the time has moved forward, not back.
