# Research: Printable character sheets, `dmc sheet` (phase 1)

## R1. #191 predates play cards

#191's design splits the NPC stat block's combined lines into one field per fact. Since spec 013 and migration 006, a
companion's AC, HP, speed, saves, skills, senses, attacks and traits live on its play card, "the only copy of each
number on it" (`kit/templates/npc-sheet.md`, `dmc-character-sheet-update` step 7), and migration 006 step 3 deleted
them from the rest of the sheet. Phase 1 reads the card's lines as boxes, and treats those stat-block or Combat
fields as optional on a companion. Splitting the card or the stat block is phase 2's to decide, against the resume's
card budget.

## R2. The budget measurement #191 asked for first

#191 asked to measure party sheets against the 4,000 `OtherSheet` budget before the spec. Budgets have since been
restated in real tokens (`Size/Budgets.cs`: `OtherSheet` 6,400, `PlayCard` 1,600), and a companion is read only to the
end of its card. Phase 1 adds nothing a companion's resume reads; a PC's sheet gains one line. The measurement on the
real campaign belongs to phase 2, whose field splits change what a resume reads.

## R3. Matching

`ReadSet.MatchCompanion` matches a block by title, by the whole title inside the block, then by a first or last word,
and #191's review found it resolves one real party block to a major NPC's sheet. `dmc sheet` matches by exact title or
file name only, so a party block that does not name its sheet exactly is skipped and named, and a name is refused.

## R4. Output shape

`ErrorPathTests.AssertOneLine` needs one line for every failure, and a multi-line `DmcException` is folded into one.
The verify pattern (`CampaignCommand.RunVerify`) prints lines on stdout, then one line on stderr: `SheetCommand`
writes the refusal itself and returns 1, so the error-path rows are only the one-line failures.

## R5. The slip test

Fork points need Claude Code's logs and file history of play (`.docs/development.md` § The slip test). They are on the
maintainer's computer, so the trial fixed in the spec and in #191 runs there.
