# Data Model: "Yes, and…" and "No, but…"

One file changes shape: `story/dm-notes.md` has an `## Arc plan` section in every campaign (standard 7, migration 007).
The "no, but" record needs no new shape.

## The "no, but" record (in `story/canon.md`)

- **What**: one canon line per major NPC whose first attack was turned with a "no, but".
- **Fields**: the NPC's bold name, what happened, the session and scene (`sN.M`), as canon's template asks for
  every line.
- **Example**: `- **Phillip Druskenvald** stopped the party's attack on the train and spared them; he liked their
  spunk. (s1.2)`
- **Rule**: while the line exists, a later attack on that NPC is a real fight at the campaign's lethality: the dice
  fall where they fall, for both sides.
- **Lifecycle**: written the moment it happens (`kit/dm-rules.md:58`); never deleted by this feature.

## The Arc plan (in `story/dm-notes.md`)

- **What**: the `## Arc plan` section, from `kit/templates/campaign/story/dm-notes.md`, with the comment of
  [contracts/dm-notes-arc-plan.md](contracts/dm-notes-arc-plan.md).
- **In a homebrew campaign, or after a module ends**: the whole plan (shape, mix, Character arcs, side quests, the
  party's gaps, progression, wonder), as now.
- **In a module campaign, while the module runs**: Character arcs only.
- **Shape rule** (the check `dm-notes-have-arc-plan`): the section exists, and holds a line, outside comments, that
  starts with `Character arcs`.
- **Budget**: `Budgets.DmNotesArcPlan` (`src/Dmc.Cli/Size/Budgets.cs:21`), unchanged; `### ` subsections count inside
  it.

## A goal the character wants (in the Arc plan's Character arcs)

- **What**: the goal, and how it ties into the main plot.
- **Where**: the Character arcs line of the Arc plan, "for the PC and each companion, what they want or must face in
  this arc, how the main plot meets it, and a moment that tests it".
- **Example**: `- Character arcs: Shale: a hearth-stone of Kelderak stone for the Landing; a spare block from the
  tower's last course, lit from the Ember. Kibil: …`
- **Lifecycle**: written when the character states it (`kit/dm-rules.md:165`) or at the next save; updated when the
  goal changes or is met, as the Arc plan already is.

## Standard 7 (migration 007)

- **From**: standard 6 (migration 006, `kit/migrations/006-play-cards-and-names.md`).
- **Changes**: `story/dm-notes.md` only, by adding lines and demoting headings; no content unit is removed or
  reworded (research R15), so the accounting holds only its comment lines.
- **Transitions**: a campaign with an Arc plan keeps it (its comment updated); one with its arc under another
  heading has that section moved inside a new Arc plan; one with neither gets an Arc plan after The central conflict.
  Each gets a Character arcs line.
