---
standard: 8
title: The Arc plan names its story structure, and antagonists move on a clock of sessions
shipped: PR #228
touches: [story/dm-notes.md]
drops: []
checks: [arc-plan-guidance-current]
---

# Arc plan structure

## What changes

DMC's notes now plan each arc on a story structure, give each stage a rough number of sessions, write milestones as
achievements, and time each villain's next move in sessions played. This updates the guidance in the notes only:
nothing you or DMC wrote changes. DMC fills in the new parts at its next full save.

## Steps

Read `story/dm-notes.md` and `templates/campaign/story/dm-notes.md` before you change anything. "The template's
comment" below means that section's comment (`<!-- … -->`) in the template, copied exactly.

1. **Arc plan comment.** In the section headed `## Arc plan`, a comment that opens "The current arc of a homebrew
   campaign" is the template's comment of an earlier version: replace it with the template's. If it already names the
   Five Commandments, it is the current one: leave it. Leave any other comment as it is, since it may hold your own
   notes. If the section has no comment, add the template's right after the heading. If there is no `## Arc plan`
   section, migration 007 skipped this campaign: change nothing for this step, and name it to the player.
2. **Antagonists comment.** The same for the section headed `## Antagonists`: a comment that opens "For each: what
   they want" is the template's comment of an earlier version, unless it already asks for the next move; replace it
   with the template's, leave any other comment, and add the template's right after the heading if the section has
   none.
3. **No Antagonists section.** If there is no `## Antagonists` section, add `## Antagonists`, a blank line and the
   template's comment as a new section right after the Arc plan section, before the heading that follows it; when
   there is no Arc plan section, after `## The central conflict`, before the section that follows it; when there is
   neither, after the file's title and the lines before its first `## ` heading.
4. **Account.** Run the list form of verify, as the `dmc-migrate-campaign` skill says. These steps change comments and
   add at most one empty section, so it should list no `unaccounted:` line; if it lists one, you changed a line you
   should not have: put it back as it was.

## Judgments

- **Which comment is the template's**: only one that opens with the words the steps give. Anything else in a comment
  is kept, word for word.
- **Never fill in the plan here**: no budgets, start sessions, milestone equivalents or antagonists' next moves, even
  where the files suggest them. The next full save writes them with the story in front of it.
- **A module campaign**: the same steps.
- **A `dm-notes.md` too unlike the template** to find the sections (no `## ` headings at all, say): leave it
  unchanged, name it to the player, and add a `skipped` line to the accounting file, as the skill says.

## Declared drops

None: only the template's own comments are replaced, and nothing else is removed.

## Done when

The check passes: `arc-plan-guidance-current` (`story/dm-notes.md`'s Arc plan comment names the Five Commandments,
and it has an `## Antagonists` section whose comment asks for the next move).
