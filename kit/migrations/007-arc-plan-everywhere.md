---
standard: 7
title: Every campaign's DM notes have an Arc plan, with the characters' arcs
shipped: PR #221
touches: [story/dm-notes.md]
drops: []
checks: [dm-notes-have-arc-plan]
---

# Arc plan everywhere

## What changes

DMC now keeps what each character wants, and how the main story will meet it, in one place in its notes: the
Character arcs line of the Arc plan in `story/dm-notes.md`. This gives every campaign that section. A campaign that
plans its story under another heading (its stages or acts) has that plan moved inside the Arc plan, word for word. A
module campaign keeps only the Character arcs there until the module ends. Nothing is removed. If the moved plan is
long, `dmc size` may say the Arc plan is over its budget until the next full save shortens the finished parts.

## Steps

Read `story/dm-notes.md` and `templates/campaign/story/dm-notes.md` before you change anything. The template's
`## Arc plan` section is a heading and one comment (`<!-- … -->`); "the template's comment" below means that comment,
copied exactly.

1. **An Arc plan already there.** If `story/dm-notes.md` has a `## Arc plan` section, keep everything in it. If its
   comment does not say "in a module campaign, only Character arcs until then", replace that comment with the
   template's. Then go to step 4.
2. **An arc under another heading.** If there is no `## Arc plan` but one `## ` section lays out the campaign's arc
   (see Judgments), insert a line `## Arc plan`, a blank line and the template's comment just before that section's
   heading. Then demote that section's headings by one level, down to the next `## ` heading: its own `## ` becomes
   `### `, each `### ` in it becomes `#### `, and so on. Change no other line. Go to step 4.
3. **No Arc plan.** Otherwise, add `## Arc plan`, a blank line and the template's comment as a new section after
   `## The central conflict`, before the section that follows it; when there is no Central conflict section, after
   the file's title and the lines before its first `## ` heading.
4. **Character arcs.** If no line in the Arc plan, outside its comment, starts with `Character arcs`, add one right
   after the comment: `- Character arcs:` and then, for the PC and each companion (each `### ` block under
   `## Party status` in `game-state.md`, other than "With the party, not companions"), one sentence each in the form
   `Name: what they want; how the main plot meets it.`, leaving out the part after the semicolon where `dm-notes.md`
   does not tie it to the plot. Take a want from their sheet (the PC's "Long-term goals"; a companion's play card line "Toward the PC,
   and what they want now", or "What they want") or from the notes. When the files say nothing for anyone, write `- Character arcs: none yet`.
5. **Account.** Run the list form of verify, as the `dmc-migrate-campaign` skill says. These steps add lines and
   change heading levels only, so it should list no `unaccounted:` line; if it lists one, you changed a line you
   should not have: put it back as it was.

## Judgments

- **Which section is the arc**: one that lays out the campaign's stages, acts, parts or chapters and what changes in
  each (for example "The five stages and the hero's journey", or "The three acts"). A section of milestones,
  income, treasure, open threads, secrets or ideas is not, even when it names stages; leave it where it is.
- **When in doubt**, or when two sections could each be the arc: use step 3, leave both where they are, and name them
  to the player in a line, so the next full save can bring them together.
- **Wants**: write only what the files say. Never invent a want, a tie to the plot, or a name the character has not
  met. A companion whose sheet says nothing gets no entry; when the files say nothing for anyone, `none yet`.
- **A module campaign**: the same steps. While the module runs, the Arc plan holds only the Character arcs line;
  do not plan stages for it.
- **A `dm-notes.md` too unlike the template** to place the section (no `## ` headings at all, say): leave it
  unchanged, name it to the player, and add a `skipped` line to the accounting file, as the skill says.

## Declared drops

None: nothing is removed. Headings change level and keep their words.

## Done when

The check passes: `dm-notes-have-arc-plan` (`story/dm-notes.md` has a `## Arc plan` section with a Character arcs
line outside its comment).
