# Contract: migration 008 and its check

The format is `specs/005-campaign-migrations/contracts/migration-file.md`; the steps follow `.docs/development.md`
§ Changing a campaign-file standard. Meets FR-011 and SC-006; research R12.

## `kit/migrations/008-arc-plan-structure.md`

Front matter:

```
---
standard: 8
title: The Arc plan names its story structure, and antagonists move on a clock of sessions
shipped: PR #<this PR>
touches: [story/dm-notes.md]
drops: []
checks: [arc-plan-guidance-current]
---
```

Body, with the five headings in order:

- **What changes** (for the player, plain words): DMC's notes now plan each arc on a story structure, give each stage
  a rough number of sessions, write milestones as achievements, and time each villain's next move in sessions played.
  This updates the guidance in the notes only; nothing you or DMC wrote changes. DMC fills in the new parts at its next
  full save.
- **Steps**, each first checking whether it is needed. Read `story/dm-notes.md` and
  `templates/campaign/story/dm-notes.md` first; "the template's comment" means that section's comment there, copied
  exactly.
  1. **Arc plan comment.** In the `## Arc plan` section, a comment that opens "The current arc of a homebrew campaign"
     is a template comment of an earlier version: replace it with the template's. Leave any other comment, which may
     be your own notes. If the section has no comment, add the template's right after the heading. A campaign with no
     `## Arc plan` section is one migration 007 was skipped for (a `skipped` line): leave it and name it to the player.
  2. **Antagonists comment.** The same for `## Antagonists`, whose template comments open "For each: what they want".
  3. **No Antagonists section.** Add `## Antagonists`, a blank line and the template's comment right after the Arc plan
     section, before the heading that follows it; when there is no Arc plan section, after `## The central
     conflict`, before the section that follows it; when there is neither, after the file's title and the lines
     before its first `## ` heading.
  4. **Account.** Run the list form of verify, as the `dmc-migrate-campaign` skill says. Only comments change, so it
     should list no `unaccounted:` line; if it lists one, put that line back as it was.
- **Judgments**: a comment is the template's only when it opens with those words; anything else is kept. A module
  campaign takes the same steps. Never fill in budgets, clocks or milestones here: the next full save does, with the
  story in front of it.
- **Declared drops**: none; only template comments are replaced.
- **Done when**: `arc-plan-guidance-current` passes.

## The check `arc-plan-guidance-current`

In `src/Dmc.Cli/Campaign/MigrationChecks.cs`, registered after `dm-notes-have-arc-plan`. It reads the sections raw,
comments included (the existing `Section` helper strips them).

| `story/dm-notes.md` | Result |
|---|---|
| Missing | passes (not checked) |
| No `## Arc plan` section | passes (`dm-notes-have-arc-plan` reports it) |
| An Arc plan with no comment naming "Five Commandments" | `story/dm-notes.md's Arc plan has an older template comment` |
| No `## Antagonists` section | `story/dm-notes.md has no ## Antagonists section` |
| An Antagonists section with no comment naming "next move" | `story/dm-notes.md's Antagonists has an older template comment` |
| An older template comment (opening "The current arc of a homebrew campaign" or "For each: what they want", without "Five Commandments" or "next move") beside the current one | that section's older-template-comment line |
| Both current | passes |

An XML doc comment on the function names migration 008 and spec 015 FR-011; tests in
`tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`, one per row.

## The fixture `tests/Dmc.Tests/Fixtures/migrations/008/`

- `before/`: a campaign at standard 7 from the templates, its `story/dm-notes.md` holding PR #221's Arc plan comment
  with a filled stage line and Character arcs, the first kit's Antagonists comment with one antagonist, and a comment
  of the player's own in the Arc plan.
- `after/`: the same with both template comments replaced and the player's comment kept; every content line
  unchanged.
- `accounting.tsv`: made as `.docs/development.md` § Changing a campaign-file standard step 4 says.

`MigrationGoldenTests` and `MigrationFilesTests` pass on it. One live run, by `dmc-migrate-campaign` on a copy of the
`before/` campaign in a scratch DMC folder, leaves what `after/` holds and `dmc campaign verify` passes.
