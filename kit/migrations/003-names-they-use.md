---
standard: 3
title: NPC and companion sheets say the names they use
shipped: PR #170
touches: [character-sheets/]
drops: []
checks: [npc-sheets-have-names-line]
---

# NPC and companion sheets say the names they use

## What changes

NPC and companion sheets gain a "Names they use" line: what that character calls the people, places and things that
matter, where it differs from DMC's own labels, and what they would never call them. It keeps an NPC from saying a
name they have never heard. Older sheets lack the line; this adds it, filled from what the campaign already says, or
left for DMC to fill in play when nothing does. Nothing is removed.

## Steps

1. List every `character-sheets/npc-*.md`, not the `*-history.md` files beside them. Companions have `npc-` sheets too.
2. For each sheet, Grep it for `Names they use`. If the line is there, leave the sheet as it is.
3. Otherwise add the line from `templates/npc-sheet.md` under `## Playing them`, after the Voice line (at the end of the
   section if there is no Voice line, or under the sheet's first `## ` section if there is no `## Playing them`):
   `- **Names they use** (for people, places and things that matter, where they differ from DMC's labels; what they would never call them):`
4. Fill it from that character's own sheet and from the session logs in `story/sessions/` (Grep the logs for the
   character's name and read the lines around their speech): a name they used for the PC, a place or a faction that is
   not DMC's label for it. Write only what was said in play.

## Judgments

- **Nothing says what names they use**: leave the line with nothing after the colon, and tell the player in one line
  that DMC will fill it as the character speaks. Do not invent names.
- **The logs disagree** (they called the PC two things): write both, with the session of each, and mention it to the
  player.
- **A sheet with no `## ` sections at all**: add the line at the end of the sheet's opening list.

## Declared drops

None: the migration only adds a line.

## Done when

The check `npc-sheets-have-names-line` passes: every NPC and companion sheet has a "Names they use" line.
