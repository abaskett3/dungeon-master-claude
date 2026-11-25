# Research: Hold fixes from feedback that only add words

## R1. Where the hold goes in the skill

- **Decision**: a paragraph in § 6 (Report), after the bullet list of what to say per cause, and one sentence in § 7.
- **Rationale**: § 6 is where the skill decides what change to propose ("Prefer the smallest change ... A hook or a
  script is better than a paragraph"); § 7 is where it acts on the user's word, so it needs the "not while held" rule.
  The skill had 61 lines; it gains 8.
- **Alternatives considered**: a new section (renumbers § 7, which other text does not point at, but adds a heading
  for a rule that ends within weeks).

## R2. The status of a held entry

- **Decision**: unchanged (`open`).
- **Rationale**: `kit/templates/feedback.md:11` lists the statuses and `kit/skills/dmc-record-feedback/SKILL.md:20`
  greps `^status: (open|filed)`; a new status would change a kit template and a player skill, and a held entry should
  still be listed so the next review sees it.

## R3. How the skill knows the hold ended

- **Decision**: `.docs/roadmap.md` showing #249 done, or the date.
- **Rationale**: the roadmap is the repository's home of status (CLAUDE.md § Documentation); the skill already reads
  `git log` and `gh issue list` in § 2, so either source is in reach.

## R4. Token cost

- Play: 0 (no kit file changes).
- Review: about 8 lines more in a developer skill, paid only when `review-feedback` loads.
