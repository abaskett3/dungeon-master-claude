# Contract: `dmc size --arc`, the Arc plan flags (FR-026, research R2)

Agreed by the maintainer on 2026-11-16, on one condition: **the player never acts on these or sees them.** So they are
not part of `--check`, which the resume runs and whose leftover lines the full save names to the player
(`kit/skills/dmc-resume-campaign/SKILL.md:18`, `kit/skills/dmc-campaign-full-save/SKILL.md:81`).

## Use

`dmc size <campaign> --arc`, run only by the full save in step 5, after its arc checks. It prints one line per flag, or
nothing. The full save fixes each in `story/dm-notes.md` and runs it again; it says nothing about them to the player,
in play or in its report. A dead person who still acts (a ghost) is kept by writing `dead` and the reason on the line
that names them ("Joss (dead, a ghost bound to the tower)"), which clears the flag; it still does not mention it.

Only when the Arc plan holds at least one stage line (not only the template comment); otherwise prints nothing. No
file records that a module is finished, and every campaign's `dm-notes.md` has the `## Arc plan` heading from the
template (`kit/templates/campaign/story/dm-notes.md:11`); the full save writes stages only for a homebrew campaign or a
finished module (`kit/skills/dmc-campaign-full-save/SKILL.md:44`), so a stage line is the marker.

## Flags

| Flag | Fires when | Line (draft) |
|---|---|---|
| Dead named ahead | a sheet's `- **Status**:` starts with `dead` (any case), and a line of the Arc plan or Antagonists not marked `DONE` names them (`ReadSet.Mentions`: the title, first or last word) without the word `dead` on that line | `arc: the plan still names Auntie Hush, whose sheet says dead: revise the stages ahead (step 5, Revise)` |
| No current stage | the Arc plan has no `current since sN` | `arc: no current stage (current since sN): add it (step 5, Arc plan)` |
| No budget | the current stage's line has no `about N sessions` | `arc: the current stage has no budget in sessions: add one (step 5, Arc plan)` |
| Over budget | the current session (`game-state.md`) is past N + the budget | `arc: the current stage began at s12 with a budget of 4 and this is s59: tighten or revise (step 5, Pace)` |
| No clock | an Antagonists entry has no `by sN` | `arc: Gloam's next move has no session (by sN): add one (step 5, Clocks)` |

## Skill change

`dmc-campaign-full-save` step 5 gains one sentence (about 200 bytes): run `dmc size <campaign> --arc`, fix each line in
`dm-notes.md`, and never mention them to the player.

## Tests

`tests/Dmc.Tests/Size/`: each flag fires and stays silent; an Arc plan with only the template comment prints none; a
`DONE` stage naming the dead prints none; a line naming the dead that says `dead` prints none, and one that names them
by last word only ("Hush prepares") fires; `--check` prints none of them.
