# Contract: a migration file

`kit/migrations/NNN-<slug>.md`, installed to `migrations/` of a DMC folder. `NNN` is three digits, the standard the
migration brings a campaign to. The file is both what DMC follows and what `dmc` reads (the front matter). It is
written for a campaign its author has never seen, and for a DMC that has none of the review that prompted it in
front of it (the `review-feedback` rule for prompts).

## Front matter

```
---
standard: 4
title: Lean dm-notes and game-state; Predecessor line
shipped: PR #173
touches: [story/dm-notes.md, story/dm-notes-archive.md, game-state.md, player-options.md, story/canon.md]
drops: [mix-tracker, overtaken-plan, finished-arc-beat, scene-history]
checks: [no-dead-opposition-rows, fights-have-timeline, one-last-played-line]
---
```

One `key: value` per line; lists in `[a, b]`. Unknown keys are an error (so a typo is caught). Class names are
lowercase words joined by hyphens. `checks` names at least one check for the shape the migration produces. There
are no size targets: sizes are `dmc size`'s (research R6).

## Body, in this order, with these headings

1. **`## What changes`**: two to six lines for the player, plain language, no jargon. DMC shows it, with the
   `drops` classes spelled out, when it asks the player to confirm (FR-004, FR-009b).
2. **`## Steps`**: a numbered list DMC follows in order. **Write before delete**: a step that removes content comes
   after the step that gives it a new home, and says which. Each step names its file. A step that can run twice
   without harm says nothing; one that cannot says how to tell it has run.
3. **`## Judgments`**: for each decision DMC makes, the rule and what to do when in doubt. Doubtful content is kept
   and shown to the player (FR-013, FR-009b). Example: "a row is for a finished fight when its second column is
   `0 / N` or the session logs say the group was defeated; when the two disagree, keep the row and ask".
4. **`## Declared drops`**: one line per class in `drops`: what it is, and why the new standard does not need it.
5. **`## Done when`**: what a correct result looks like, in the terms of `checks`.

## Rules for the author

- **Idempotent.** A campaign created under standard 3 has no `.standard` and runs migrations 2 to 4 over files that
  are already current. Every step first checks whether it is needed ("if the sheet has no `Names they use` line, add
  it"), so running it over a current file changes nothing.
- **No file outside `touches`** is changed. A new file is listed.
- **Accounting.** Content a step rewords is accounted in the accounting file as `reworded` with an anchor; content a
  step drops as `dropped:<class>`. DMC takes the ids from `dmc campaign verify NAME --list`. The migration's golden
  fixture (`tests/Dmc.Tests/Fixtures/migrations/NNN/`) shows one of each the migration can produce.
- **A hand-edited file** whose shape a step does not recognise is left unchanged, named to the player and given a
  `skipped` line in the accounting file; the campaign is then not verified (FR-010). The migration's shape check
  fails on it too, if DMC forgets the line.
- **Size.** A migration that adds a token cost to the files resume reads says so in `## What changes`.
- **A change to a standard is not finished without its migration** or a line in the PR description saying why none
  is needed (a change that adds a new optional field, for example).

## What `dmc` checks at build time (in `Dmc.Tests`)

Numbers contiguous from 2 and equal to the file name; front matter well formed with no unknown key; `checks` not
empty and every name in it exists in `MigrationChecks`; every `touches` path is a plausible campaign path (no `..`, not absolute, not
under `backups/`); all five headings present, in order; each `drops` class has a line under `## Declared drops`;
a fixture folder with `before/`, `after/` and `accounting.tsv` exists.
