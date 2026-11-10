---
standard: 2
title: Each character sheet ends with its history
shipped: PR #68
touches: [character-sheets/]
drops: []
checks: [sheets-end-with-history]
---

# Each character sheet ends with its history

## What changes

Resume reads each character sheet down to its history and stops there, so the history has to be the sheet's last
section. On older sheets it can sit in the middle, which makes resume read the whole sheet. This moves each sheet's
history section, unchanged, to the end of the sheet. Nothing is reworded or removed.

## Steps

1. List the sheets: every `character-sheets/pc-*.md` and `character-sheets/npc-*.md`, not the `*-history.md` files beside
   them and not `character-sheets/minor-npcs.md`.
2. For each sheet, Grep it for `^## ` with line numbers on. Its history section is the one headed `## History`, or the
   older `## Notes` or `## History with the PC`. If the sheet has no such section, or it is already the last `## `
   section, leave the sheet as it is.
3. Otherwise move the section: in one Edit, cut it from its place (the heading and every line down to the next `## `
   heading) and paste it, unchanged, after the last line of the sheet. Keep the heading as it is: do not rename
   `## Notes` or `## History with the PC`. Keep one blank line between sections.

## Judgments

- **Two history-like sections** (for example `## Notes` and `## History`): keep both, unchanged and in their order, as
  the last sections of the sheet, with `## History` (or the later of the two) last. Tell the player which sheet it was.
- **A section that is not history** but is headed `## Notes` (notes about the player, say): it is still treated as
  history, because resume treats it so. Moving it changes nothing that is read.
- **A sheet whose shape you do not recognise** (no `# ` title, sections in a code block): leave it unchanged, name it to
  the player and add a `skipped` line for it to the accounting file.

## Declared drops

None: every line is moved, not removed.

## Done when

The check `sheets-end-with-history` passes: on every PC and NPC sheet that has a history section, that section is the
last `## ` section.
