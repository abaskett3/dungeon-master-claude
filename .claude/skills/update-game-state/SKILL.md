---
name: update-game-state
description: Bring the active campaign's game-state.md in line with the present moment of play (location, time, HP, resources, conditions, combat tracker, quests). Use after any change to those things, at the end of each combat round, and as part of a full save.
---

# Update game state

`game-state.md` in the active campaign folder describes right now. It holds no history; that belongs in the session log.

## How to update

1. Read the current file first. Compute new values from what is written there, not from memory.
2. Change only the lines that changed. Use small edits, not a rewrite of the file.
3. Keep the section layout from the template so every session finds things in the same place.

## What to keep current

- **Scene**: location (specific enough to resume from), in-world date and time, weather or light if it matters, who is present, what is happening and what the player was about to do.
- **Party status**: for each PC and companion, current and max HP, temporary HP, hit dice left, spell slots left per level, class resources (rage, ki, channel divinity and so on), conditions and their remaining duration, concentration, exhaustion, death saves, inspiration.
- **Carried changes**: gold and consumables spent or gained (ammunition, potions, rations, charges). The full inventory lives on the character sheet; update both when an item is gained or lost.
- **Combat tracker**, while a fight is running: round number, initiative order with whose turn is next, each combatant's AC, current and max HP, conditions, position or zone, and the stat numbers you are using for each enemy. Clear this section when the fight ends, after logging the outcome.
- **Quests**: active, completed and failed, each with a one-line status.
- **Clocks**: anything counting down (a ritual in three days, a ship leaving at dawn, a spell that lasts an hour).
- **Pending**: an unresolved roll, a question the player has not answered, a choice in front of them.

## Rests

On a short or long rest, apply the edition's recovery rules to every PC and companion, advance the clock, tick any countdowns, and write all of it in one pass.

## Check

After editing, confirm that HP is not above max, no slot count is negative or above its max, and the time has moved forward, not back.
