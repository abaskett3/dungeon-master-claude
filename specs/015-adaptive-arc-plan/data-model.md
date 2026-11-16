# Data model: the Arc plan and Antagonists in `story/dm-notes.md`

No new file, and no new state outside `story/dm-notes.md` and the existing Pending line in `game-state.md`. Every
field is a markdown line DMC writes; `dmc` reads none of them except the section sizes (`dmc size`) and the
migration's check. What each section holds is set by the template's comments
([contracts/dm-notes-template.md](contracts/dm-notes-template.md)).

## Arc plan (`## Arc plan`)

| Field | Form | Rules |
|---|---|---|
| Structure | `Structure: the Five Commandments.` (or the Hero's Journey) | one line, first in the section, outside the comment |
| Stage | `- Stage N, <beat> (about K sessions[; current since sM]): <what changes>. Mix: …` | one per beat; progressive complication on one to three; 5 to 7 in all |
| Done stage | `- Stage N, <beat>, done (sA to sB): <what changed>.` | cut to one line when it ends (template, line 24 on `main`) |
| Pace note | a clause on the current stage: how its next scenes tighten | written when the stage is over its budget |
| Character arcs | `- Character arcs: <Name>: want; how the plot meets it; the test, in stage N.` | as on `main` (spec 014) |
| Side quests, the party's gaps, Wonder | as on `main` | unchanged |
| Milestone | in the Progression line: `level N for <achievement>, or a victory of the same weight` | re-anchored on a revision; `awarded sM` once given, never again |
| Money | its own line | split from milestones so no line passes 400 characters |
| Last played | `- Last played (as of sN): …` | one line, overwritten each save (as on `main`) |

### A stage's life

```text
planned ──(previous stage done)──► current since sM ──(its end reached)──► done (one line)
   │                                    │
   │                                    ├─(sessions since sM > budget, nothing untrue)──► pace note
   │                                    │
   └──(made untrue, or put off)─────────┴──► revised by step 4a (stays planned or current)
```

Put off: the player says later or turns its hook down, or the budget runs out before the stage has started
(spec clarification).

### Milestone

```text
planned ──(achievement or a victory of the same weight reached)──► Pending line in game-state.md ──(rest)──► awarded sM
   │
   └──(revision)──► re-anchored to an achievement in the revised stages (still planned)
```

## Antagonist (`## Antagonists`)

| Field | Form | Rules |
|---|---|---|
| Name and names known | as on `main` | spoken to the player only by the names known |
| Wants, offscreen, knows | as on `main` | |
| Next move | `Next move: <what>, by sN.` | always a session; an in-world date alone is restated |
| Done move and sign | `Did (sN): <what changed>. Show next session: <news, consequence, changed place, NPC's reaction>.` | written when sN has come, as an instruction to the next session, not a note; the sign is deleted once shown; a new next move is given |

```text
next move by sN ──(full save at or after sN)──► did (sN) + sign ──(next session shows it)──► sign deleted; next move by sP
```

## Session number

The number of the current `story/sessions/session-NNN.md`, which the full save writes (step 3) and stamps on the
Last full save line of `game-state.md` (step 8). Budgets, stage starts and antagonist moves all count in it.

## Sizes

The section is read whole at resume and budgeted at 1.5k by `dmc size` (`src/Dmc.Cli/Size/Budgets.cs:22`). A filled
seven-stage example measured 1.3k (research R9). The full save keeps it there by cutting done stages and finished
beats, as now.
