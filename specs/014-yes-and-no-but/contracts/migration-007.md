# Contract: migration 007 and its check

The format is `specs/005-campaign-migrations/contracts/migration-file.md`; the steps are research R15, the check R16,
the fixture R17. Meets FR-005b and SC-006.

## `kit/migrations/007-arc-plan-everywhere.md`

Front matter:

```
---
standard: 7
title: Every campaign's DM notes have an Arc plan, with the characters' arcs
shipped: PR #<this PR>
touches: [story/dm-notes.md]
drops: []
checks: [dm-notes-have-arc-plan]
---
```

Body, with the five headings in order:

- **What changes** (for the player, plain words): DMC keeps what each character wants, and how the main story meets
  it, in one place in its notes, the Arc plan, in every campaign. A campaign that plans its story under another
  heading has that plan moved under the Arc plan, unchanged. Nothing is removed. If the moved plan is long, `dmc
  size` may flag it until the next full save shortens finished parts.
- **Steps**: R15's five, in order, each first checking whether it is needed:
  1. A section headed exactly `## Arc plan`: content kept; the template's earlier comment replaced by the current
     one, any other comment left, the comment added where there is none; a heading that only begins with Arc plan is
     step 2's case (research R20).
  2. No `## Arc plan`, one `## ` section laying out the arc: `## Arc plan` and the comment inserted before it; that
     section's headings demoted one level.
  3. Otherwise: `## Arc plan` and the comment added after `## The central conflict`.
  4. A `Character arcs` line added when the Arc plan has none: the PC and each companion, what their sheets already
     say they want, and how the notes tie it to the main plot where they do; `none yet` when nothing does.
  5. The account: the list form of verify; nothing removed, so no `reworded` or `dropped` lines.
- **Judgments**: an arc section is one that lays out the campaign's stages, acts or chapters and what changes in each;
  a section of milestones, income, threads or ideas is not. When in doubt, or when two sections could be it, add the
  Arc plan by step 3, leave both, and name them to the player. A want comes only from the files (the sheets' goals,
  a play card's "attitude and what they want now", the notes); never invent one. A `dm-notes.md` too unlike the
  template to place the section: skip it, name it to the player, and write a `skipped` line.
- **Declared drops**: none; nothing is removed.
- **Done when**: `dm-notes-have-arc-plan` passes.

## The check `dm-notes-have-arc-plan`

In `src/Dmc.Cli/Campaign/MigrationChecks.cs`, registered with the others:

| Campaign | Result |
|---|---|
| No `story/dm-notes.md` | passes (not checked) |
| No `## Arc plan` section (comments stripped, heading compared ignoring case) | `story/dm-notes.md has no ## Arc plan section` |
| The section, but no line or subheading in it, outside comments, starting `Character arcs` after heading marks, list marks and bold | `story/dm-notes.md's Arc plan has no Character arcs line` |
| Both | passes |

An XML doc comment on the function names migration 007 and spec 014 FR-005b.

## The fixture `tests/Dmc.Tests/Fixtures/migrations/007/`

- `before/`: `game-state.md` (a PC and one companion under Party status), `character-sheets/pc-*.md` and
  `npc-*.md` (a goal each), `story/dm-notes.md` from the template without `## Arc plan`, its arc under
  `## The three acts` with `### ` acts.
- `after/`: the same, with `## Arc plan`, the new comment and a Character arcs line before `### The three acts`, its
  acts at `#### `.
- `accounting.tsv`: its comment lines only (nothing is reworded or dropped).

`MigrationGoldenTests` and `MigrationFilesTests` pass on it.
