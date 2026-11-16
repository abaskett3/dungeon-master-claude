# Contract: the arc check in `kit/skills/dmc-campaign-full-save/SKILL.md` step 5

Meets FR-003 to FR-006, FR-010; research R4 to R7, R11. The home of when the arc is checked, revised and paced; how to
revise is `dmc-new-campaign` step 4a.

## The list after "Then check the arc, in a few lines of `dm-notes.md` and not out loud:"

Bullets in this order. Mix and Promises are unchanged and shown only by name.

```markdown
- **Arc plan.** If the campaign is homebrew, or its module is finished, and `dm-notes.md` has no Arc plan or the arc's last stage is done, plan the next one now in that section (add it from `templates/campaign/story/dm-notes.md` if missing), following `dmc-new-campaign` step 4a (read only that step). Where the plan lacks what the template's comment asks for (the structure, each stage's beat and budget, the current stage's start, milestones as achievements, each antagonist's next move), add it from what the files say. Never delete a template comment.
- **Revise.** In those campaigns, if a stage still ahead is untrue, whenever play made it so (an antagonist dead, the arc's goal changed, a stage put off: the player said later or turned its hook down, or its budget ran out before it began), revise the stages still ahead as step 4a says. If nothing is untrue, change nothing.
- **Pace.** In those campaigns, compare the sessions since the current stage began with its budget. Over it, write in the stage how its next scenes tighten toward the next one, or revise. Never cut an open exchange or a choice short for it.
- **Clocks.** In those campaigns, each antagonist's next move names the session by which it happens (`by s14`); restate one timed only by an in-world date. When its session has come, the move happens in the world: on their line, write what changed and `Show next session:` the sign of it where the party is (news, a consequence, a changed place, an NPC's reaction), and give them a next move. Delete a sign once it has been shown.
- **Mix.** (unchanged)
- **Progression.** Under milestone leveling, if a milestone's achievement, or a victory of the same weight, was reached and not yet awarded, add a Pending line in `game-state.md` (`Milestone: level N at the next rest`) and mark it awarded in the Arc plan; the level-up follows `dmc-character-sheet-update` at that rest. If the party has had no way to earn money this arc, put one in the next stage.
- **Promises.** (unchanged)
```

## What changed from `main`

- **Arc plan**: "following `dmc-new-campaign` step 4a (read only that step)" added, and, after the first full save on a migrated campaign filled in nothing and deleted the comment: "Where the plan lacks what the template's comment asks for … add it from what the files say. Never delete a template comment."
- **Revise**, **Pace**, **Clocks**: new bullets, one line each.
- **Progression**: "if a milestone in the Arc plan was reached" becomes "if a milestone's achievement, or a victory of
  the same weight, was reached".

## Cost

3 lines added; +1,345 bytes (about 336 tokens) on 11,438, paid at each full save (research R11; +1,082 before the fix after the first full save on a migrated campaign).

## Cases

| Campaign at the full save | Result |
|---|---|
| Homebrew, a stage ahead names an antagonist who died | the stages ahead revised by step 4a; the rest unchanged |
| Homebrew, the current stage's hook turned down, or its budget spent before it began | it counts as put off: revised |
| Homebrew, nothing contradicts the plan | no change to the stages |
| Homebrew, the current stage over budget, nothing untrue | a line in the stage on how its next scenes tighten |
| An antagonist's session has come | what changed and the sign on their line; a new next move |
| An antagonist timed "by midsummer" only | restated as a session |
| A milestone's equivalent reached elsewhere | a Pending line; marked awarded |
| A module campaign, module running | Arc plan, Revise, Pace and Clocks skip (the module's arc and antagonists apply); Mix, Progression, Promises run as now |
