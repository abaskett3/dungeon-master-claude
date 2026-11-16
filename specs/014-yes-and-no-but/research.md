# Research: "Yes, and…" and "No, but…"

All measurements on `main` at `adb0e07`, on a scratch copy of `kit/dm-rules.md`. R12 to R19 were added after the first
play test (`quickstart.md` § Results) and the spec's clarifications that followed it; where they change an earlier
item, that item says so.

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
  bytes, under the 560 the spec allows. **Changed by R12**: +719 bytes in all, the file at 30,636, the cap rise 593.
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
- **Changed by R14 to R17**: the play test found campaigns without the section, so the template changes and a
  migration gives every campaign one.

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
- **Cost**: +54 bytes over the earlier draft, with R11; the cap rises by 559 of the 560 allowed, leaving 1. (R12
  spends 34 more under the 593 the spec now allows.)

## R11. The exception says "at any lethality"

- **Decision**: line 169's exception reads "…or Opposition table), at any lethality: a "no, but"…". To pay for it,
  "(a technique, not words to say)" became "(a technique, never said)" and "Routine actions" became "Routine acts".
- **Rationale**: the clarification makes the exception hold at every lethality. Without the words, line 167 ("Follow
  the **lethality** setting") and `gritty`'s "death is always possible" (`kit/templates/campaign/player-options.md`)
  could be read as overriding it (second `/speckit-analyze`, U1).

## R12. "No, but" offers a way toward what the player wanted, in the same reply

- **Decision**: in line 166, "turn it aside, with another way forward" becomes "turn it aside and offer, in the same
  reply, a way toward what they wanted". +34 bytes: the file at 30,636 bytes and 212 lines (measured on a scratch copy
  of the branch at `8f21bbc`); the cap rises from 30,043 by 593, the spec's allowance (FR-008).
- **Rationale**: the play test's two misses (`quickstart.md` § Results, messages 9 and 10) turned the idea aside and
  offered nothing: "with another way forward" read as optional, and nothing tied the offer to the player's aim. "In
  the same reply" makes the offer due now; "toward what they wanted" points it at the aim (a way in, the saddle), as
  the crawlspace example does.
- **Alternatives considered**: "turn it aside, with another way toward what they wanted" (+16; leaves "in the same
  reply" to the example); "turn it aside and, in the same reply, offer another way toward what they wanted" (+40, one
  byte over 593 with nothing else cut); dropping the Arc plan's name from line 165 to pay for it (-38; the maintainer
  chose to keep it, R14).

## R13. A wrong premise keeps line 153's correction

- **Decision**: no rule change. The spec gains User Story 2 scenario 5 and an edge case; SC-002 does not count an
  idea that rests on a fact the character would know is different.
- **Rationale**: `kit/dm-rules.md:153` ("If something the player's decision rested on turns out wrong (a range, an
  ability, a position), stop and tell them before resolving what follows") fired four times in the test, each time
  with the ways forward, never as a story refusal. The player made a mistake about what is in front of the character;
  narrating Shale toward a pine that is not there would resolve an action the player did not mean. The maintainer
  agreed (spec § Clarifications, after the play test).

## R14. The template keeps Character arcs in every campaign

- **Decision**: in `kit/templates/campaign/story/dm-notes.md`, the Arc plan comment's first line gains "; in a module
  campaign, only Character arcs until then", and the Character arcs line gains "how the main plot meets it" (the
  exact wording is in [contracts/dm-notes-arc-plan.md](contracts/dm-notes-arc-plan.md)). +82 bytes in the comment.
- **Rationale**: the comment says the section is for "the current arc of a homebrew campaign, or the stretch after a
  module ends" (template line 14), so in a module campaign line 165's pointer would land in a section DMC is told is
  not for now. Only the Character arcs line is wanted during a module (spec edge case "A module campaign"); the rest
  stays as it is, and the full save's "Arc plan" step (`kit/skills/dmc-campaign-full-save/SKILL.md:44`) still plans the
  rest only for a homebrew campaign or after the module.
- **Cost**: the resume reads `dm-notes.md` whole (`kit/skills/dmc-resume-campaign/SKILL.md:25`), and `dmc size`
  counts its tokens as bytes / 4 of the lines read, comments included (`src/Dmc.Cli/Size/Measurement.cs:16`, `:86`;
  `ContentLines`, `:183`, leaves comments out only for flags). So a campaign holding the new comment pays about 21
  more tokens per resume (82 / 4). No skill changes.
- **Alternatives considered**: a new line in the full save to keep Character arcs in a module campaign (a skill cost
  paid at every save, and line 165 already sends DMC there in play); leaving the comment (the module campaign case
  stays broken).

## R15. Migration 007: every campaign's DM notes get an Arc plan

- **Decision**: `kit/migrations/007-arc-plan-everywhere.md`, standard 7, `touches: [story/dm-notes.md]`, `drops: []`,
  `checks: [dm-notes-have-arc-plan]`. Its steps, in order, each checking first whether it is needed:
  1. A `## Arc plan` section already there: keep its content; if its comment is the template's earlier one, replace
     the comment with the current template's.
  2. No `## Arc plan`, but one `## ` section that lays out the campaign's arc under another heading (its stages,
     acts or chapters; Rorn's Landing's "The five stages and the hero's journey"): insert `## Arc plan` and the
     template's comment just before it, and demote that section's headings one level (`## ` to `### `, `### ` to
     `#### `), so it sits inside the Arc plan unchanged.
  3. Otherwise: add `## Arc plan` and the template's comment after `## The central conflict` (or after the file's
     opening lines when there is none), as the template orders it.
  4. Character arcs: if the Arc plan has no `Character arcs` line, add one with, for the PC and each companion in
     `game-state.md`'s Party status, what their sheets already say they want (the PC's goals; a companion's play
     card or "What they want"), and how the notes already tie it to the main plot, where they do; `none yet` when the
     files say nothing.
  5. Account: run the list form of verify, as `dmc-migrate-campaign` says; nothing is removed.
- **Rationale**: a heading is a content unit with its `#`s stripped (`src/Dmc.Cli/Campaign/ContentUnits.cs`,
  `Normalise`), and comments are not units (`StripComments`), so demoting headings and replacing the comment remove
  nothing and need no accounting line; added lines need none either. `dmc size` cuts sections at `## ` headings only
  (`Measurement.IsSectionHeading`, `src/Dmc.Cli/Size/Measurement.cs:213`), so a moved arc's subsections stay inside
  the Arc plan, where its budget (`Budgets.DmNotesArcPlan`, `src/Dmc.Cli/Size/Budgets.cs:21`) and the DONE-lines
  exemption (`Flags.cs:53`) apply. Moving only the stages section, not Milestones or income, keeps the judgment small.
- **Size**: the migration adds a few lines (the heading, the comment, Character arcs). A moved arc larger than the
  section's budget makes `dmc size` flag the Arc plan; the next full save cuts finished stages to a line, as the
  template says. `## What changes` says so (contract `migration-file.md`, rule "Size").
- **Alternatives considered**: renaming the old heading to `## Arc plan` (the heading's unit is lost, so DMC must
  account a `reworded` line; demoting keeps it); adding an empty Arc plan beside the old section (two places for the
  arc); moving Milestones and income too (more judgment, more risk, no need for line 165).
- **Spec change**: FR-005b's "A campaign that already has the section is left as it is" becomes "keeps its content;
  only the template's comment is brought up to date", so step 1 carries R14 to existing campaigns.

## R16. The shape check

- **Decision**: `dm-notes-have-arc-plan` in `src/Dmc.Cli/Campaign/MigrationChecks.cs`: `story/dm-notes.md` has a
  `## Arc plan` section (comments stripped, heading compared ignoring case, as `Section` does), and a line in it,
  outside comments, that starts with `Character arcs` after list marks and bold. A campaign with no `dm-notes.md` is
  not checked. Tests in `tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`: passes with both; fails with no section;
  fails with the section but only the comment's Character arcs line.
- **Rationale**: the development guide requires a check that fails on a file left in the old shape
  (`.docs/development.md:235`). Step 4 always writes the line (`none yet` at least), so the check can require it.

## R17. The golden fixture and the live run

- **Decision**: `tests/Dmc.Tests/Fixtures/migrations/007/`: `before/` a small campaign at standard 6 (`game-state.md`
  with a PC and one companion; their sheets with a goal each; `story/dm-notes.md` from the template without
  `## Arc plan`, its arc under `## The three acts` with `### ` acts); `after/` the same with `## Arc plan`, the new
  comment, the Character arcs line and the acts demoted; `accounting.tsv` with its comment lines only. The live run
  (`.docs/development.md:244`): `dmc install` refuses inside Claude Code (`src/Dmc.Cli/ClaudeSessionGuard.cs`, called
  from `InstallCommand.cs:54`), so the fixture's `before/` goes into the installed test folder
  `D:\Claude\test-campaign` as a scratch campaign, `kit/migrations/007-*.md` is copied into its `migrations/`, and a
  headless `claude -p` asks DMC to bring the campaign up to date, with `artifacts\bin` first on `PATH` (the `dmc` on
  `PATH` is release 0.4.0, which has no `dm-notes-have-arc-plan`). The scratch campaign, the migration copy and
  `.active-campaign` are removed or restored afterwards.
- **Rationale**: `MigrationGoldenTests` needs `before/`, `after/` and `accounting.tsv`, fails when a touched file is
  left as it was, and needs no drop in this fixture (`tests/Dmc.Tests/Toolkit/MigrationGoldenTests.cs`, the drop case
  needs one across all fixtures). No test pins the kit at standard 6 (searched `tests/` for `standard 6`, `kit 6`).
- **Unverified**: whether a model follows step 2's judgment on a real campaign's notes; the live run checks the
  fixture only.

## R18. The second play test

- **Decision**: rerun, in `D:\Claude\test-campaign` as before, 4 ideas the world rules out (none resting on a wrong
  premise, R13) after the R12 wording, and count them with the first test's attack on Rorn for SC-002's 5. Line 169
  did not change, so message 13's result stands; the "yes, and" sentences did not change, so T004's results stand.
  Then state a goal and save in the fixture campaign once migration 007's live run has given it an Arc plan
  (`quickstart.md` § 3b, step 4), to check SC-006's last sentence.
- **Rationale**: rerunning only what changed keeps the test's cost down; each `claude -p --resume` call re-reads the
  conversation.
- **Unverified**: whether the fixture campaign holds enough to resume and save; if not, a copy of Rorn's Landing is
  migrated instead, which is at standard 5 (its `.standard`), so 006 runs before 007, at a cost not measured.

## R19. Documents

- **Decision**: in addition to the first round's: `.docs/campaign-files.md` (the `story/dm-notes.md` row, line 73:
  the Arc plan's Character arcs in every campaign), `.docs/decisions.md` (the entry's Limits rewritten, migration 007
  named), `.docs/roadmap.md` (the row names migration 007), and `.docs/dmc-reference.md` § Migrations, whose list
  of checks (`MigrationChecks`, line 453) gains `dm-notes-have-arc-plan` (and the three checks of migrations 005 and
  006 it lacked). The user guide and `dmc-migrate-campaign` list no migrations by number or check, so they do not
  change. (Corrected by the convergence pass: this item first said the reference named no checks; the search had
  looked for the numbers and missed the list.)

## R20. Fixes from the code review of PR #221

The built-in code review ran once on the finished PR; the maintainer approved these fixes:

- **Line 169 names Antagonists, not the Opposition table** (the maintainer's choice): the table holds one row per enemy
  group the party may meet, so naming it would have spared the first fight against bandits or ghouls. -5 bytes: the
  file at 30,631, the cap rise 588 of the 593 allowed, about 179 tokens per turn.
- **The template gets `- Character arcs: none yet`** after the Arc plan comment: a new module campaign is recorded at
  standard 7 by `dmc-new-campaign`, and without the line it would lack the shape `dm-notes-have-arc-plan` checks.
  +28 bytes; the template's change is +110 in all, about 27 tokens per resume.
- **The check accepts a subheading** (`### Character arcs`, or `####` after step 2 demotes it): `#` joins the
  regex's leading marks, with tests for both.
- **Step 1 of migration 007** replaces only the template's earlier comment, leaves any other comment (it may hold the
  DM's notes; comments are not content units, so verify would not see them go), adds the comment where there is none,
  and treats a heading that only begins with Arc plan as step 2's case, since the check, like `dmc size`, matches the
  heading exactly.
- **One constant**: `MigrationChecks` uses `Flags.ArcPlan` (`src/Dmc.Cli/Size/Flags.cs`), now internal.
- **Wording**: the check's doc comment names § Fun first instead of a line number; the product overview no longer
  says DMC never answers out of character, or that any story-breaking choice gets "no, but".
- **Not changed**: the exception's place after the recklessness sentence ("at any lethality" covers every first attack,
  reckless or not, and the play test behaved so), and the canon record's reach on a later attack (the prompt hook
  prints up to 3 canon lines for each person the player names, `.docs/decisions.md` § "The prompt hook reminds DMC of
  canon for people the player names", and line 78 sends DMC to recall a returning NPC; not tested in play).