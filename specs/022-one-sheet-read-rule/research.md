# Research: One kit rule for how far DMC reads a sheet

## R1. Copy or point

- **Decision**: point at resume step 2.5.
- **Rationale**: the contradiction exists because the rule had two homes (`kit/dm-rules.md:78` and
  `kit/skills/dmc-resume-campaign/SKILL.md:26`); spec 013 changed the second and left the first. Development 6 (one
  home) says to point. The pointer "as resume step 2.5 does" is 4 bytes longer than "down to its History"; the issue's
  copied text is about 70 bytes longer and would need a cap raise, which the plan of #238 rules out.
- **Alternatives considered**: the issue's text (two homes again; +70 bytes); "as resume does" with no step (shorter,
  but the skill has many reads and the step makes the target exact).

## R2. Where the 4 bytes come from

- **Decision**: drop " as before" from "If the recall does not hold it, read its sheet and search `canon.md` and those
  archives as before" in the same bullet.
- **Rationale**: "as before" refers to how DMC searched before `dmc recall` existed (spec 009); the sentence says what
  to search without it. Removing it changes no instruction. Net: 32,510 to 32,504 bytes (measured with `wc -c`).
- **Alternatives considered**: raising the cap by 4 (adds rule bytes against the plan's rule).

## R3. What guards the pointer

- **Decision**: a toolkit test that reads § 2 of the resume skill, takes the line that starts `5. `, and checks it
  starts `5. Sheets` and holds both reads.
- **Rationale**: a pointer to a step number breaks silently if the step moves. `SectionOf` in `ToolkitTests.cs` already
  extracts a `## ` section.

## R4. The token cost

- **Rules**: 6 bytes less in every play turn (about 2 tokens). No skill changes.
- **Resume**: unchanged; the resume already follows step 2.5. A mid-play Present-line read of a companion now stops at
  the play card instead of the history; the largest resume in the logs read companions for 61 to 79 lines against 21
  through the card (issue #247), so the read is smaller where the rule is followed. Not measured here: SC-004.
