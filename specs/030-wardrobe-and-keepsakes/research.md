# Research: A wardrobe and keepsakes section on sheets

## R1. What the hand-made sections did

The two sheets DMC edited by hand (the feedback's campaign, not in the repository) each had a "Wardrobe and keepsakes
(kept, not worn now)" section. Both listed things worn or carried now (an everyday hood; a tabard and a kit strap).
One character's clothes were in Looks, the Appearance line, the section and canon; one companion's cloaks and hat were
also on the PC's `Stored elsewhere`. Looks held rules by occasion and a future outfit. Decision: Looks is now only;
the section holds the rest, on the owner's sheet.

## R2. Where the swap rule lives

`kit/dm-rules.md` § sheets already says that when play settles what someone wears or owns, that line of the sheet is
rewritten. The file is at its cap (`ToolkitTests.DmRulesBytesAllowed`, 32,510; the file is 32,507). The swap is a step
of `dmc-character-sheet-update`, which play loads when items change.

## R3. The hook's sheet lines

`SheetReminder.HasValue` matches a line that starts `- **Looks**` and has text after its colon, so `- **Looks** (worn
now): …` is printed as before.

## R4. The slip test

Fork points need Claude Code's logs and file history of play (`.docs/development.md` § The slip test). They are on the
maintainer's computer, so the trial of issue #286 runs there.
