# Research: One kit rule for how far DMC reads a sheet

## R1. Copy, point, or state it short

- **Decision**: state the extent in the rule, shorter than the issue's text: "read their sheet through its play card
  (if none, down to its History)". A toolkit test holds it to resume step 2.5.
- **Rationale**: the first version pointed at the step ("read their sheet as resume step 2.5 does"), for one home
  (Development 6). The code review of PR #273 found three faults in the pointer, each checked against the files:
  - the skill never calls its item "step 2.5": § 2 has a plain item `5. Sheets`, and elsewhere the kit's "step N"
    means a `## N.` heading (`kit/dm-rules.md` "consistency check (step 7)" is the full save's `## 7.`);
  - read literally, the step decides *which* sheets to read, and someone about to join the Present line is in none of
    its three kinds, so it says "Any other sheet, however recurring the character, is not read now"
    (`kit/skills/dmc-resume-campaign/SKILL.md:26`);
  - the resume skill is not in context in a session that began with `dmc-new-campaign`, and a long session holds it
    far back, while the rule is always loaded.
  The step's companion case also points back at `CLAUDE.md` § Accuracy, so with the pointer neither text stated the
  extent. Stating it in the rule fixes all three. It is a second copy of the extent, which is how the drift began
  (spec 013 changed the step and not the rule); the toolkit test `ThePresentLineReadsASheetAsTheResumeDoes` now fails
  if either side changes alone.
- **Alternatives considered**: the issue's text ("as the resume does (a companion's through its play card, anyone
  else's down to its History)", about 70 bytes and a cap raise); the pointer (above); "as resume does" with no step
  (the same faults).

## R2. Where the 33 bytes come from

- **Decision**: two cuts in the same bullet that change no instruction.
  - " as before" in "read its sheet and search `canon.md` and those archives as before" (10 bytes): it refers to how
    DMC searched before `dmc recall` existed (spec 009); the sentence says what to search without it.
  - ": it is closed for good" after "Never change a campaign whose folder holds `.closed`" (23 bytes): the instruction
    is "Never change"; the reason is recorded in `.docs/decisions.md` § A closed campaign is closed for good, and
    `dmc campaign standard` and the resume refuse a closed campaign on their own (spec 010).
- **Rationale**: 33 - 10 - 23 = 0. The file stays at 32,510 bytes (measured with `wc -c`), so the cap does not move.
- **Alternatives considered**: raising the cap by 33 (the issue allows it; the plan of #238 asks for no more rule text).

## R3. What guards the two copies

- **Decision**: a toolkit test that checks the rule's sentence, that "read their sheet down to its History" is gone,
  and that the line of the resume skill's § 2 that starts `5. ` starts `5. Sheets` and holds both reads.
- **Rationale**: two copies drift unless a test holds them together. `SectionOf` in `ToolkitTests.cs` already
  extracts a `## ` section.

## R4. The token cost

- **Rules**: 0 bytes per turn (the file's size does not change). No skill changes.
- **Resume**: unchanged; the resume already follows step 2.5. A mid-play Present-line read of a companion now stops at
  the play card instead of the history; the largest resume in the logs read companions for 61 to 79 lines against 21
  through the card (issue #247), so the read is smaller where the rule is followed. Not measured here: SC-004.
