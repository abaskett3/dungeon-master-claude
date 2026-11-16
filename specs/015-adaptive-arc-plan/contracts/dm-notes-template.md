# Contract: the template `kit/templates/campaign/story/dm-notes.md`

Meets FR-002, FR-004, FR-005, FR-006, FR-009; research R3, R5, R6, R7, R9. Only the two comments change; the
`- Character arcs: none yet` line and every other section stay as they are.

## The Arc plan comment

The whole comment as it will read; what changed from `main` is listed after it.

```text
<!--
The current arc of a homebrew campaign, or the stretch after a module ends; in a module campaign, only Character arcs until then. Plan it when one starts, on a story structure as `dmc-new-campaign` step 4a says; the full save checks it and revises it when play makes it untrue. A plan, not a script: play decides what happens, the plan keeps the shape, and the arc still ends.

- Shape: the structure, then its stages, each naming its beat (Five Commandments: inciting incident; progressive complication, over one to three stages; crisis; climax; resolution), what changes in the story, and a rough budget in sessions (`about 4 sessions`). The current stage says the session it began (`current since s9`).
- Mix: for each stage, the fights, exploration, puzzles and roleplay it holds. A stage of talk alone is not a stage.
- Character arcs: for the PC and each companion, what they want or must face in this arc, how the main plot meets it, and a moment that tests it.
- Side quests: two or three short ones tied to the main story, with a reward.
- The party's gaps: what it cannot do (no healing, no answer to magic) and how the story offers it (an ally, items, a trainer, a place to buy), or which threats are chosen so it need not.
- Progression: under milestone leveling, each milestone as an achievement and the level it gives, "or a victory of the same weight".
- Money: the ways to earn it (rewards, bounties, treasure, paid work, things to sell) and things worth buying.
- Wonder: other peoples, magic, monsters and marvels that appear in each stage.

Mark a stage done when it ends, cut to one line (what changed), and a milestone awarded when it is given; delete finished beats. The full save's check of what was last played is one "Last played" line at the end of this section, overwritten each save, never a log. Keep the section short: the full save checks its size with `dmc size`.
-->
```

What changed from `main`: the first paragraph (structure, step 4a, revision, "the arc still ends"), the Shape line
(Freytag's stages replaced by the Five Commandments' beats, the budget, the current stage's start) and the Progression
line (achievement, "or a victory of the same weight", in place of "the level each milestone gives and where it
falls"), with money split onto a Money line of its own, so a filled Progression line stays under 400 characters (the
first headless creation wrote both on one line of 490).

## The Antagonists comment

```text
<!-- For each: what they want, what they are doing right now offscreen, their next move and the session by which it happens (`by s14`), what they know about the PC, and the names the party knows them by. Speak only those to the player. -->
```

## Sizes (measured on the drafts, research R9)

| Part | `main` | New | Change |
|---|---|---|---|
| Arc plan heading and comment | 1,542 bytes | 1,917 bytes | +375 |
| Antagonists comment | 175 bytes | 239 bytes | +64 |
| Read at every resume | | | about +110 tokens |

A seven-stage Arc plan filled with every field measured 1.3k of its 1.5k `dmc size` budget.

## Rules this comment is the home of

What the Arc plan and Antagonists hold (Development 6). When they are checked and revised is the full save's
([full-save-arc-check.md](full-save-arc-check.md)); how to design or revise an arc is `dmc-new-campaign` step 4a's
([new-campaign-arc-guidance.md](new-campaign-arc-guidance.md)).
