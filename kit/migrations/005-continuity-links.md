---
standard: 5
title: Continuity links replace the Predecessor line
shipped: PR #195
touches: [player-options.md]
drops: []
checks: [continuity-section]
---

# Continuity links

## What changes

A campaign's links to other campaigns move from the one Predecessor line in `player-options.md` to a
`## Continuity` section of three lines: **Continues** (the closed campaign a sequel carries on), **Side campaign of**
and **Shares a world with**. `dmc recall` follows all three, both ways, so a sequel, a side campaign and the campaigns
of one world search each other. The Predecessor line's value moves to the Continues line, and nothing else in the
campaign changes.

## Before the backup

If the Predecessor line names a folder, run `dmc campaign standard <predecessor>` for it:

- **`closed`**, or the folder is not in `adventures/`: go on to the backup.
- **`behind`**: bring that campaign up to date first, with this same skill, then run the check again.
- **Anything else**: the earlier campaign is still open. Tell the player, out of character, that it is still open, and
  that playing it on would make its story and this sequel's disagree. Ask once whether to close it now, saying that
  closing is permanent: it can never be played or changed again, and only a sequel continues its story.
  - **Yes**: follow `dmc-close-campaign` for that campaign, naming it, and skip its step 2: this was its question. If
    it refuses, say why in a line and stop. Once it is closed, go on.
  - **No**: ask once more, simply, whether they are sure they want to go on without closing it. On yes, go on: the
    Continues line will name the open campaign, and nothing asks about it again. On no, stop and say this, with its
    folder name for `<predecessor>`:

    `Migration stopped until the previous campaign is closed. You can close it by running: /dmc-close-campaign <predecessor>`

A stop here changes nothing: no backup has been taken, so the campaign stays as it was and playable, and the migration
is offered again at the next resume.

## Steps

Read `player-options.md` before you change it.

1. **The Continuity section.** If the file has no `## Continuity` section, add it after the `## Rules` section (at the
   end of the file if there is none), as `templates/campaign/player-options.md` gives it: the heading and its three
   lines, with their comments. The Continues value is the Predecessor line's folder, or `none` when there is no
   Predecessor line or it says `none`; the other two values are `none`.
2. **The Predecessor line.** Delete the `**Predecessor**` line.
3. **Account.** Run the list form of the verify command, as the `dmc-migrate-campaign` skill says. It lists the
   Predecessor line: account it as `reworded`, with the new Continues line, `**Continues**: <value>`, as the anchor.

## Judgments

- **The folder name**: the Predecessor value may be in backticks, start with `adventures/`, end with a slash or carry a
  comment. Write the plain folder name on the Continues line.
- **A Continuity section already there** (written by hand): keep its lines. If its Continues line says `none` and the
  Predecessor line names a folder, write that folder on the Continues line. If the two name different folders, ask the
  player which one this campaign continues.
- **A Predecessor that names no folder in `adventures/`**: write it on the Continues line as it is. `dmc` names it at
  each resume until the player corrects it or the folder comes back.

## Declared drops

None: the Predecessor line's value moves to the Continues line.

## Done when

The check `continuity-section` passes: `player-options.md` has no Predecessor line, and its `## Continuity` section
has its Continues, Side campaign of and Shares a world with lines.
