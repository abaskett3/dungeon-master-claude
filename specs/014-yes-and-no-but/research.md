# Research: "Yes, and…" and "No, but…"

All measurements on `main` at `adb0e07`, on a scratch copy of `kit/dm-rules.md`.

## R1. Where the rule goes

- **Decision**: rewrite three bullets of `kit/dm-rules.md` § Fun first in place: line 165 (what the player enjoys),
  line 166 ("Say yes to creative ideas"), line 169 (recklessness).
- **Rationale**: § Fun first already holds the nearest rules: line 166 is "Yes" without the "and", and line 169 is
  the rule FR-004 makes its one exception to. Rewriting in place adds no line (FR-008). One home (Development 6).
- **Alternatives considered**: § Out of character (its subject is out-of-character talk, not the fiction); a new
  bullet (the line cap is 212 and the file is at 212); a skill (it would load too rarely for a rule of every turn).

## R2. The budget, and cuts looked for first

- **Decision**: +685 bytes, 0 lines. The file goes from 29,917 to 30,602 bytes; the test's cap
  (`DmRulesBytesAllowed`, `tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1205`) goes from 30,043 to 30,602, a rise of 559
  bytes, under the 560 the spec allows.
- **Cuts found**: the rewrite of line 166 replaces "Say yes to creative ideas when you can" instead of adding to it,
  and shortens "rule in favor of the cool idea and keep moving" to "rule for the cool idea". No other bullet of
  § Fun first or § Out of character (lines 161 to 180, read in full) repeats another, so nothing else was cut. The
  rest of the file was not searched for cuts.
- **Cost**: about 171 tokens per turn, by `dmc`'s estimate of bytes / 4 (`src/Dmc.Cli/Size/Measurement.cs:16`).
- **Alternatives considered**: definitions without examples (about 300 bytes less, but the clarification chose the
  examples); the cap set to 30,603 (leaves 1 byte nobody has decided to spend; the cap is set to the new size
  instead, as spec 013's FR-028 held it to what was used).

## R3. Who counts as a major NPC, in the rule's words

- **Decision**: "an NPC the main plot needs (central in the module, or in the Arc plan or Opposition table)".
- **Rationale**: the clarification's definition, in the words the play rules already use: § Running a published
  module for the module, and the Arc plan and Opposition table of `kit/templates/campaign/story/dm-notes.md`.

## R4. How the first "no, but" is recorded

- **Decision**: "a canon line records it". The rule names no section.
- **Rationale**: `kit/dm-rules.md:58` already says any new fact gets one line in `story/canon.md` right away, and
  canon's template says each line carries its session. The NPC's name is in the line, so a later attack finds it
  when DMC looks the NPC up (§ Accuracy and consistency, line 78, "Look before you bring something back") and when
  the prompt hook reminds DMC of canon for names the player uses (`.docs/decisions.md` § "The prompt hook reminds
  DMC of canon for people the player names").
- **Alternatives considered**: naming a section (People or History) in the rule (more bytes; canon's template
  already says where facts go).

## R5. Where a goal is noted

- **Decision**: the Arc plan's Character arcs in `story/dm-notes.md`.
- **Rationale**: the template already has the field: "Character arcs: for the PC and each companion, what they want
  or must face in this arc, and a moment that tests it" (`kit/templates/campaign/story/dm-notes.md`, Arc plan
  comment). No template change, so no migration (Development 2). `kit/dm-rules.md:60` already sends "changed goals"
  to the character sheet; the Arc plan is where DMC plans how the plot meets them.

## R6. "Never refuse out of character", in words that do not stop an honest answer

- **Decision**: "Never say out of character that the story does not allow it."
- **Rationale**: "Never refuse out of character" would clash with § Out of character, line 175 ("Answer as a
  collaborator, frankly"): a rules question still gets a frank no. The ban is on the flat story refusal (FR-003).

## R7. Routine actions

- **Decision**: "Routine actions get nothing added." in the "yes, and" bullet.
- **Rationale**: the clarification limits "yes, and" to bold or creative choices; a hook on every action would
  lengthen replies and clash with § Out of character, line 177 ("do not invent obstacles").

## R8. Tests

- **Decision**: in `ToolkitTests.cs`, raise `DmRulesBytesAllowed` to 30,602 and name this spec in its comment and
  message; add one test that § Fun first holds "yes, and" and "no, but", so a later cut cannot drop them silently.
- **Rationale**: the toolkit tests are how this repository checks the prompts (`.docs/development.md` § Writing
  prompts). No test quotes the three bullets being changed (searched `tests/`, `src/` and `kit/` for their
  phrases).

## R9. The headless play test

- **Decision**: `claude -p` with `--continue` for each later prompt, in `D:\Claude\test-campaign`, on the play
  model and effort (`--model sonnet --effort high`, the arguments `dmc play` passes, `ToolkitTests.cs:1193`), as
  spec 013's quickstart § 5 did.
- **Unverified**: which major NPC of the test campaign's Arc plan or Opposition table can be put in the scene; it is
  chosen when the test runs.

## R10. "Yes, and" and "no, but" are techniques, not words to say

- **Decision**: the rule names them as techniques ("Meet a bold idea with "yes, and" (a technique, never said)",
  "use "no, but" in the fiction: turn it aside"), and its examples show the technique without the words.
- **Rationale**: the maintainer's clarification. "Answer with "yes, and"" or "say "no, but"" could be read as a
  phrase to open replies with. A model copies the shape of its examples, so the examples carry no such phrase.
- **Cost**: +54 bytes over the earlier draft, with R11; the cap rises by 559 of the 560 allowed, leaving 1.

## R11. The exception says "at any lethality"

- **Decision**: line 169's exception reads "…or Opposition table), at any lethality: a "no, but"…". To pay for it,
  "(a technique, not words to say)" became "(a technique, never said)" and "Routine actions" became "Routine acts".
- **Rationale**: the clarification makes the exception hold at every lethality. Without the words, line 167 ("Follow
  the **lethality** setting") and `gritty`'s "death is always possible" (`kit/templates/campaign/player-options.md`)
  could be read as overriding it (second `/speckit-analyze`, U1).
