# Feature Specification: Keep a module campaign in its module's world

**Feature Branch**: `claude/project-thread-wno9kc` (spec 035)

**Created**: 2026-11-28

**Status**: Implemented

**Input**: Issue #215. Constitution Dungeon Master 3 says the party stays in the module's world while it runs, with
monsters and encounters from the module and NPCs, races, spells, skills and abilities from it first, and the world
keeps its character after the module ends. `kit/dm-rules.md` § Running a published module said only "improvise in
keeping with it" wherever the player went (`.docs/decisions.md` § "The constitution states principles of its own
(version 3.0.0)", Limits).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The party is turned back in the fiction (Priority: P1)

In a module campaign, the player says the party leaves the module's land for somewhere the module never mentions.
DMC does not say no out of character. The party meets an in-world reason that turns them back safely: they wake from a
fog in their own camp, the bridge out is down, or a border the module closes (a mist around the land) holds.

**Acceptance**: § Running a published module says a party leaving the module's world turns back for a safe in-world
reason, never an out-of-character no, and that a border the module closes holds.

### User Story 2 - What DMC adds comes from the module (Priority: P1)

The party goes where the module is silent. DMC improvises a scene from the module: its monsters and encounters; its
NPCs, races, spells and abilities first, then the core rules or another module. It invents a new NPC only when none of
the module's fits the role, and that NPC is minor.

**Acceptance**: the section says so, in one bullet with the deviation and hook rules it already had.

### User Story 3 - After the module (Priority: P2)

The module ends. At the full save that finds no arc after it, DMC asks the player once, out of character, whether to
go on in this world with an arc DMC plans, or take the characters to another setting (close this campaign and start a
sequel). An arc in the same world grows from what the module and play left there, and keeps the world's character:
a few newcomers, never a world overrun by them.

**Acceptance**: `dmc-new-campaign` step 4a, which the full save reads when it plans the arc after a module, has an
"After a module" paragraph saying this.

### Edge Cases

- A player who presses past the turn-back: Dungeon Master 4's "no, but" in the fiction applies (`kit/dm-rules.md`
  § Fun first); this change adds no second refusal rule.
- A module that closes its world in the fiction: the closure holds; DMC does not invent a way out.
- A homebrew campaign: unchanged; nothing in this change applies without `module-progress.md`.

## Requirements *(mandatory)*

- **FR-001**: § Running a published module's "Let the player leave the script" bullet becomes "Leave the script, not
  the world", keeping its rules on deviations, notes and hooks back to the plot.
- **FR-002**: A party leaving the module's world turns back for a safe in-world reason (examples: a fog they wake from
  in camp, a bridge out), never an out-of-character no.
- **FR-003**: A border the module closes in the fiction holds.
- **FR-004**: Monsters and encounters are the module's; NPCs, races, spells and abilities come from it first, then the
  core rules or another module.
- **FR-005**: DMC invents only a minor NPC, and only when none of the module's fits.
- **FR-006**: `dmc-new-campaign` step 4a gets an "After a module" paragraph: an arc in the module's world grows from
  what the module and play left there and keeps its character (a few newcomers fit, a world overrun by them does not).
- **FR-007**: The same paragraph has DMC ask the player once, out of character, whether to go on in the world or take
  the characters to another setting (`dmc-close-campaign`, then a sequel, step 1c).
- **FR-008**: `kit/dm-rules.md` does not grow: the bytes come from cuts elsewhere in the same section that change no
  instruction, and the line count stays 212.
- **FR-009**: A toolkit test holds FR-001 to FR-007 in place.

## Success Criteria *(mandatory)*

- **SC-001**: `kit/dm-rules.md` is at most 32,510 bytes (its cap) and 212 lines. Result: 32,503 bytes, 212 lines.
- **SC-002**: `ToolkitTests.AModuleCampaignStaysInItsModulesWorld` passes, and fails if a later cut drops a rule.
- **SC-003**: Slip test: waived by the maintainer on 2026-11-28 for this change ("let skip the slip test for this").

## Assumptions

- "Skills" in Dungeon Master 3 is covered by "abilities": 5e's skills are fixed by the core rules, so a module adds
  none of its own (unverified for every edition).
- The full save already plans the arc after a module through step 4a (`kit/skills/dmc-campaign-full-save/SKILL.md:44`,
  "or its module is finished"), so the after-module rule belongs there and is paid only when that step is read.
