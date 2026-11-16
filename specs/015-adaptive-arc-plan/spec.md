# Feature Specification: An arc plan built on a story structure, that DMC holds to and revises

**Feature Branch**: `015-adaptive-arc-plan`

**Created**: 2026-11-15

**Status**: Draft

**Input**: User description: "#217 and #181 (causes 2 to 6), one spec, because both change the Arc plan in
`kit/templates/campaign/story/dm-notes.md`." From #217: arc guidance in `dmc-new-campaign` built on at least the Five
Commandments of Storytelling or the Hero's Journey (constitution, Dungeon Master 1), researched; the Arc plan
template's example shape changes from Freytag's to one of the two. From #181, causes 2 to 6, read against its
principle "structured and adaptive" (the structure is the constant, play decides the content, a re-plan is a revision,
not a restart; the arc still ends): (2) revise the remaining stages whenever an outcome makes them untrue; (3) time
each antagonist's next move in sessions played, not in-world dates; (4) when a stretch has delivered its moment, DMC
offers the jump in one line, and the player may decline; (5) milestones as achievements with an equivalent,
re-anchored on any re-plan; (6) a rough session budget per stage, compared at the full save, never a reason to rush
the player. Causes 1 and 7 are out of scope: they wait for sessions on the new release, and #181 stays open for them.
Count every line added to `kit/dm-rules.md` and to `dm-notes.md`. A template change ships a migration or says why none
is needed. Test in play over several sessions, not only with `dotnet test`.

## Background

### What the kit holds today (on `main` at `5a03569`)

- **The template.** `kit/templates/campaign/story/dm-notes.md` § Arc plan (lines 11 to 27, 1,572 bytes) is one
  comment and a `- Character arcs: none yet` line. Its Shape line gives Freytag's stages as the example ("exposition,
  rising action, climax, falling action, resolution", line 16). Its Progression line ties each milestone to "where it
  falls" (line 21). § Antagonists (line 31) asks for "what they are doing right now offscreen", with no timing.
- **Creating a campaign.** `kit/skills/dmc-new-campaign/SKILL.md` step 4 (line 96) says only "fill in the Arc plan for
  the first arc as the template's comment lays out". No structure is named or explained.
- **The full save.** `kit/skills/dmc-campaign-full-save/SKILL.md` step 5 (lines 42 to 47) plans an arc only when the
  Arc plan is missing or its last stage is done (line 44), awards a milestone "reached" in the plan (line 46), and has
  no check of pace, clocks or stages the story overtook. Line 49 deletes "plans the story has overtaken" but does not
  replace them.
- **Play.** `kit/dm-rules.md` § Narration, line 195, keeps an open exchange open "until the player closes it" (PR #173)
  and has no rule for offering to move on once a stretch is done. The file is at its caps: 212 lines and 30,631 bytes,
  both held by `TheAlwaysLoadedRulesStayAtTheirLineAndByteCaps` (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1202`,
  `:1208`).
- **Sizes.** `dmc size` gives the Arc plan section a budget of 1,500 estimated tokens and `dm-notes.md` 10,000
  (`src/Dmc.Cli/Size/Budgets.cs:16`, `:22`); a section's size counts its comment (`Measurement.Sections`,
  `src/Dmc.Cli/Size/Measurement.cs:154`). `dm-notes.md` is read whole at every resume, comments included, so every byte
  the template adds is paid at every resume of a campaign that holds it.
- **Migrations.** The latest is `kit/migrations/007-arc-plan-everywhere.md` (PR #221), which gave every campaign an
  `## Arc plan` section and replaces the template's earlier comment when it finds it.

### What the long campaign showed (#181)

In a 39-session campaign the villain was executed at session 28; the later stages still described her, the campaign
was in stage two of five, the next level waited on one place the player chose to put off, and a threat "days away"
stayed away while five sessions covered one in-world day. The test campaign's copy, `D:\Claude\test-campaign\adventures\
rorns-landing\`, plans its arc under "The five stages and the hero's journey" (its `story/dm-notes.md`, line 39) and
is now at session 54.

### The two structures (to be researched in the plan)

- **The Five Commandments of Storytelling** (Shawn Coyne, *The Story Grid*): inciting incident, progressive
  complication (a turning point), crisis (a dilemma), climax (the choice and the action), resolution. They apply at
  every scale: a scene, a stage, an arc. Unverified until `research.md` cites a source.
- **The Hero's Journey** (Joseph Campbell, *The Hero with a Thousand Faces*; Christopher Vogler's twelve stages in
  *The Writer's Journey*): ordinary world, call to adventure, refusal, meeting the mentor, crossing the threshold,
  tests, allies and enemies, approach, ordeal, reward, the road back, resurrection, return with the elixir.
  Unverified until `research.md` cites a source.

## Clarifications

### Session 2026-11-15

- Q: Which structure is the template's example shape? → A: The Five Commandments (five beats, about the cost of
  Freytag's line; the crisis is a dilemma the player chooses through). `dmc-new-campaign`'s guidance covers both.
- Q: How does the jump offer fit `kit/dm-rules.md` at its caps (212 lines, 30,631 bytes)? → A: No new line; it goes
  into an existing § Narration bullet, and the byte cap may rise by at most 300 bytes.
- Q: Which migration path? → A: A migration that refreshes the Arc plan and Antagonists comments only; the next full
  save fills budgets, clocks and milestone equivalents.
- Q: When does DMC offer the jump: after any closed beat, at a scene's end, or only when a scene runs on past its
  point? → A: At the end of a scene whose point has landed, judged by `kit/dm-rules.md` line 205's test (no roll asked
  for, no NPC waiting on the character's reply, no choice in front of the player beyond what to do next); never
  mid-scene.
- Q: How do an arc's stages map onto the Five Commandments? → A: One stage per beat, except progressive complication,
  which may run over two or three stages; each stage names its beat.
- Q: When an antagonist's move comes due, how soon must the party feel it? → A: The move happens in the world at its
  session; the next session shows a sign of it where the party is (news, a consequence, a changed place, an NPC's
  reaction), not necessarily a confrontation.
- Q: When does a planned stage count as put off? → A: Either when the player says they will do it later or turns down
  its hook, or when its session budget, counted from when it became the current stage, runs out before it has
  started.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A new campaign's arc is built on a story structure (Priority: P1)

The player starts a homebrew campaign. DMC plans the first arc on the Five Commandments or the Hero's Journey, with
each stage named after the structure's beat it serves, so the story has a turning point, a crisis the player must
choose through, a climax and a resolution.

**Why this priority**: it is the constitution's rule (Dungeon Master 1), and Stories 2 to 5 revise this plan.

**Independent Test**: create a new homebrew campaign headlessly; its `story/dm-notes.md` Arc plan names the structure,
and every stage names the beat it serves and what changes in the story.

**Acceptance Scenarios**:

1. **Given** a new homebrew campaign, **When** DMC fills in the first Arc plan, **Then** the plan names its
   structure, each stage names its beat and what changes, and the arc ends in a resolution.
2. **Given** a new homebrew campaign, **When** DMC fills in Character arcs, **Then** each character's arc has a moment
   that tests it, placed in a stage (the template's existing rule), and the guidance says how the structure's beats
   serve a character arc.
3. **Given** a module campaign, **When** DMC creates it, **Then** no stages are planned while the module runs (the
   template's existing rule); the module's arc is followed instead (Dungeon Master 1, 3).
4. **Given** the full save plans the next arc (the last stage is done), **When** it plans, **Then** it uses the same
   guidance as `dmc-new-campaign`, read from its one home.

---

### User Story 2 - The plan is revised when play overtakes it (Priority: P1)

An outcome makes a stage untrue: an antagonist dies, the arc's goal changes, the player puts a planned stage off, or
the story moves in a way the plan did not expect. At the next full save DMC revises the remaining stages: it keeps
what still stands, changes only what is now untrue, makes everything new follow from what happened, and leaves an arc
that still escalates and still ends.

**Why this priority**: it is the failure #181 found (the plan described a dead villain for eleven sessions), and causes
3, 5 and 6 depend on a plan that is true.

**Independent Test**: on a copy of a campaign whose Arc plan describes an antagonist already dead, run a full save;
the revised plan names no dead antagonist in a stage still ahead, keeps every untouched stage word for word, and still
ends.

**Acceptance Scenarios**:

1. **Given** an Arc plan whose later stages rest on an antagonist who has died, **When** DMC runs the full save,
   **Then** those stages are revised so a new threat or goal grows from established events or planted threads, and
   stages, threads, foreshadowing and character arcs play did not contradict stay as written.
2. **Given** the player put a planned stage off (they said they would do it later or turned down its hook, or its
   session budget, counted from when it became the current stage, ran out before it started), **When** the full save
   runs, **Then** the stage is moved or reshaped
   so the arc does not wait on it, and what the stage was to give (a level, a reveal) is re-anchored (Story 4).
3. **Given** any revision, **When** it is written, **Then** the arc keeps its stakes, its theme and a planned ending:
   at least one escalating stage and a final stage remain.
4. **Given** a full save where nothing in play contradicts the plan, **When** it runs, **Then** the plan is left as it
   is (no churn).

---

### User Story 3 - Antagonists move on a clock of sessions played (Priority: P2)

Each antagonist's next move is timed in sessions played ("by session 14"), not in in-world dates, so a threat acts even
when five sessions cover one in-world day.

**Why this priority**: it is the clock that failed in #181, and it costs one short clause per antagonist.

**Independent Test**: after a full save, every antagonist in `story/dm-notes.md` has a next move with a session
number; on a copy where that session has passed, the next full save records that the move happened and names its
sign, and the session after shows it where the party is.

**Acceptance Scenarios**:

1. **Given** an antagonist, **When** the full save updates § Antagonists, **Then** their next move names the session
   by which it happens.
2. **Given** a move whose session has come, **When** the full save runs, **Then** the move happens in the world and
   the plan names the sign of it the next session shows where the party is (news, a consequence, a changed place, an
   NPC's reaction), in a form that fits wherever the player took the story; it need not be a confrontation.
3. **Given** that next session, **When** it is played, **Then** the sign appears in it.
4. **Given** an existing antagonist timed by an in-world date only, **When** the full save runs, **Then** the timing is
   restated in sessions.

---

### User Story 4 - Milestones are achievements, not places (Priority: P2)

Each milestone is written as an achievement with an equivalent ("level 6: break the cult's hold on the mill, or a
victory of the same weight"), so a large victory reached early, or by another road, earns the level. On every revision,
milestones not yet awarded are re-anchored to the revised plan.

**Why this priority**: in #181 the next level waited eleven sessions on one place the player chose to put off.

**Independent Test**: on a copy where the party won a victory the plan placed later, by another road, the next full
save awards the milestone (a Pending line in `game-state.md`) and marks it awarded.

**Acceptance Scenarios**:

1. **Given** a new Arc plan, **When** DMC writes Progression, **Then** each milestone names an achievement and says a
   victory of the same weight counts.
2. **Given** the party achieved a milestone's equivalent elsewhere, **When** the full save checks Progression,
   **Then** the milestone is awarded as line 46 of the full save already does for one reached.
3. **Given** a revision (Story 2), **When** it is written, **Then** every milestone not yet awarded is anchored to an
   achievement in the revised plan.

---

### User Story 5 - Each stage has a rough session budget (Priority: P3)

Each stage carries a rough budget of sessions ("about 4 sessions"). At the full save DMC compares sessions spent with
the budget. A stage over budget is a signal to tighten toward the next stage, or to revise the plan if the story has
moved on; it is never a reason to rush the player past what they chose.

**Why this priority**: it gives the pace check a measure, but a wrong budget harms nothing when Stories 2 to 4 hold.

**Independent Test**: on a copy where the current stage has run past its budget, the full save writes how the next
scenes tighten toward the next stage (or revises the plan), and no reply in the following session hurries an open
exchange or skips a choice.

**Acceptance Scenarios**:

1. **Given** a new or revised Arc plan, **When** DMC writes the stages, **Then** each stage still ahead has a rough
   session budget.
2. **Given** a stage over its budget, **When** the full save runs, **Then** it records, in the plan, how the next
   scenes tighten toward the next stage, or revises the plan (Story 2).
3. **Given** a stage over budget, **When** the next session is played, **Then** no open exchange is cut short and no
   choice is skipped because of the budget (`kit/dm-rules.md` line 195 still holds).

---

### User Story 6 - DMC offers to move on once a scene has delivered (Priority: P2)

When a scene has delivered its moment (its point has landed and nothing is half done, as `kit/dm-rules.md` line
205 tests for a break), DMC offers the jump in one line ("Skip to the morning?"), at the scene's end and never
mid-scene. The player may decline, and play goes on where it is.

**Why this priority**: without it DMC can only follow, and an open exchange rightly stays open (PR #173), so long
stretches of talk never end. It is the only change paid on every turn.

**Independent Test**: in a headless play test, a scene whose point has landed gets a one-line offer at its end; an
open exchange and a beat mid-scene get none; a declined offer is not repeated in that scene.

**Acceptance Scenarios**:

1. **Given** a scene whose point has landed, with no roll asked for, no NPC waiting on the character's reply and no
   choice open beyond what to do next, **When** DMC ends the beat, **Then** it offers the jump in one line, naming
   where it would jump to.
2. **Given** an exchange the character is still in (line 195's open exchange), **When** DMC ends the beat, **Then** it
   offers no jump.
3. **Given** the player declines, **When** play goes on, **Then** DMC does not offer again until another scene's
   point has landed.
4. **Given** the player accepts, **When** DMC jumps, **Then** a running clock or threat (Story 3) still acts across the
   jump.

---

### Edge Cases

- **A module campaign.** Stories 1, 2, 4 and 5 apply to a homebrew campaign or after the module ends; while the module
  runs the Arc plan holds Character arcs only (template, migration 007). Story 6 applies in every campaign. Story 3
  applies to a homebrew campaign's antagonists; a module's antagonists follow the module.
- **A revision must not override a choice.** A revision changes what the world does next, never what the player chose
  or what is already canon (Dungeon Master 4; § Principle of #181).
- **The arc's last stage is done.** The existing rule plans the next arc (full save line 44); a revision (Story 2) is
  for a plan that still has stages ahead.
- **A stage put off by the player** is not cancelled: it moves or changes shape; its foreshadowing and threads stay.
- **A milestone awarded already** is never re-anchored or awarded twice.
- **The jump offer and a running threat.** A jump never skips a clock that comes due inside it: the threat acts at its
  moment, and the jump stops there.
- **A sign is not a forced beat.** The sign of an antagonist's move (Story 3) is the world acting; it never decides a
  fight, check or cost the next session must hold, which is cause 1 and out of scope.
- **A fight or an initiative order** is never a scene to jump out of; § Narration line 196's rule for waiting holds.
- **Older campaigns' comments.** Migration 007 recognises the template's comment from before PR #221 by its opening
  words; a campaign may hold that one or PR #221's. This spec's migration (FR-011) must recognise both, and the one
  this spec writes.
- **The Arc plan grows.** Budget, clock and milestone clauses must fit the section's `dmc size` budget; a campaign
  near it (Rorn's Landing's arc section measures 1.9k) is trimmed by the full save's existing rules, not exempted.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `dmc-new-campaign` MUST carry researched guidance for designing an arc on the Five Commandments of
  Storytelling or the Hero's Journey: what each beat is, how stages map to beats (for the Five Commandments, as FR-002
  says), how a character arc rides on them, and
  how the plan stays loose enough for play to change it. The plan's `research.md` MUST cite the sources it rests on
  (Development 4). The skill is the guidance's one home (Development 6).
- **FR-002**: The template's Arc plan comment MUST name the structure in its Shape line instead of Freytag's stages,
  using the Five Commandments (inciting incident, progressive complication, crisis, climax, resolution) as its example:
  one stage per beat, except progressive complication, which may run over two or three stages, each stage naming its
  beat. It MUST point at `dmc-new-campaign` for the guidance, which covers both structures, instead of repeating it.
- **FR-003**: The full save MUST revise the remaining stages whenever an outcome makes them untrue (an antagonist dies,
  the arc's goal changes, the player puts a planned stage off, or the story moves in a way the plan did not expect),
  by the rules of a revision. A stage is put off when the player says they will do it later or turns down its hook,
  or when its session budget (FR-006), counted from when it became the current stage, runs out before it has started.
  The rules of a revision: keep what still stands; change only what is now untrue; make everything new follow from
  established events and planted threads; keep the stakes, the theme and a planned ending, with every beat not yet
  played still planned (at least one stage of progressive complication or crisis that escalates, and a climax and
  resolution). When it plans or revises, it follows FR-001's guidance from its home.
- **FR-004**: The template MUST ask for each antagonist's next move with the session by which it happens; the full
  save MUST, when that session has come, make the move happen in the world and name in the plan the sign of it the
  next session shows where the party is (news, a consequence, a changed place, an NPC's reaction; not necessarily a
  confrontation), and MUST restate any clock timed by an in-world date only in sessions.
- **FR-005**: The template MUST ask for each milestone as an achievement with an equivalent ("or a victory of the same
  weight"); the full save MUST award a milestone whose achievement or equivalent was reached, and MUST re-anchor every
  milestone not yet awarded on any revision. An awarded milestone is never awarded again.
- **FR-006**: The template MUST ask for a rough session budget for each stage still ahead; the full save MUST compare
  sessions spent in the current stage with its budget and, when it is over, record in the plan how the next scenes
  tighten toward the next stage, or revise the plan (FR-003). Nothing in this requirement may cut short an open
  exchange or skip a choice (`kit/dm-rules.md` line 195).
- **FR-007**: The play rules MUST tell DMC that at the end of a scene whose point has landed, judged by the test
  `kit/dm-rules.md` line 205 gives for a break (no roll asked for, no NPC waiting on the character's reply, no choice
  in front of the player beyond what to do next), it offers the jump in one line, naming where it would jump to; it
  never offers mid-scene; the rule points at line 205's test rather than copying it (Development 6); the player may decline; a declined offer is not repeated
  until another scene's point has landed; an open exchange and a fight get no offer; a clock that comes due inside a jump
  still acts.
- **FR-008**: `kit/dm-rules.md` MUST stay at 212 lines. FR-007 goes into an existing bullet of § Narration, and the
  byte cap in the toolkit test MAY rise by at most 300 bytes. The plan MUST look for cuts first and state the bytes added and the estimated tokens per turn
  (Core 3, Development 1). The test's comment MUST name this spec.
- **FR-009**: The plan MUST count every line and byte added to `kit/dm-rules.md`, to `dmc-new-campaign`, to
  `dmc-campaign-full-save` and to the template, and the estimated tokens each costs where it is paid (every turn, at
  campaign creation, at each full save, at each resume). The template's Arc plan section MUST stay within its `dmc size`
  budget when filled for a seven-stage arc (the most FR-002 allows), and the plan MUST show it with a `dmc size` result on a filled example.
- **FR-010**: Each rule MUST have one home (Development 6): the plan names it for each of FR-001 to FR-007 (template
  comment for what the plan holds, full save for when it is checked and revised, `dm-rules.md` for the jump offer,
  `dmc-new-campaign` for the structure guidance), and copies none.
- **FR-011**: The template change MUST ship a migration (Development 2; `.docs/development.md` § Changing a
  campaign-file standard) that replaces the Arc plan and Antagonists comments with the template's, recognising the
  comment from before PR #221 and PR #221's (and every earlier Antagonists comment), leaves any other comment (the
  player's or DMC's own notes) as it is, and changes no content line. Where a campaign has no `## Antagonists`
  section, it adds the template's, a heading and its comment with no content, so the full save's clocks have a place. Budgets, session clocks and milestone equivalents are filled at the campaign's next full
  save (FR-003 to FR-006), which has the story in context; the migration invents no plan. It names a shape check in
  `src/Dmc.Cli/Campaign/MigrationChecks.cs` that fails on a `dm-notes.md` still holding an older comment, and ships with
  a golden fixture and one live run.
- **FR-012**: The documents MUST be updated in the same PR (Documentation 4): at least `.docs/decisions.md`,
  `.docs/roadmap.md`, `.docs/campaign-files.md` for the Arc plan and Antagonists, and any document that names the
  example shape or lists the migrations. `decisions.md`'s Limits under "The constitution states principles of its own
  (version 3.0.0)" MUST drop the Dungeon Master 1 item this spec closes.
- **FR-013**: The PR MUST say `Closes #217` and `Part of #181`; #181 stays open for causes 1 and 7.

### Key Entities

- **Arc plan** (`story/dm-notes.md` § Arc plan): the structure named, stages each with its beat, what changes, its
  mix and a session budget; Character arcs; side quests; the party's gaps; Progression with milestones as achievements
  with equivalents; Wonder; the "Last played" line.
- **Antagonist** (`story/dm-notes.md` § Antagonists): what they want, what they are doing offscreen, and their next
  move with the session by which it happens.
- **Session number**: the number of the current `story/sessions/session-NNN.md`, the clock for budgets and antagonist
  moves.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A headless creation of a new homebrew campaign gives an Arc plan that names its structure, with every
  beat of it planned (for the Five Commandments, 5 to 7 stages: progressive complication over one to three), every
  stage naming its beat, what changes and a session budget, every milestone an achievement with an equivalent, and
  every antagonist a next move with a session (100% of each).
- **SC-002**: On a copy of a campaign whose Arc plan is overtaken (an antagonist dead, a stage put off), one full save
  leaves no stage still ahead resting on the dead antagonist (0), every stage and thread play did not contradict
  unchanged word for word, every milestone not yet awarded anchored in the revised plan, and a final stage (100%).
- **SC-003**: Played over at least 5 consecutive headless sessions on such a copy, with a full save after each: an
  antagonist move whose session comes in the run happens, and the session after it shows a sign of it where the party
  is (at least 1 of 1); a milestone reached by its equivalent is awarded at the next full save; and no full save churns a plan nothing
  contradicted.
- **SC-004**: In the same run, of the scenes whose point landed and that pass line 205's test at their end, each gets
  a one-line jump offer (all of them, at least 3); of the open exchanges, beats mid-scene and fights, none gets one (0);
  a declined offer is not repeated in the same scene (0); and no reply hurries an open exchange because a stage is over budget (0).
- **SC-005**: `kit/dm-rules.md` stays at 212 lines with its byte cap risen by no more than FR-008 allows; the filled
  seven-stage Arc plan is within its `dmc size` budget; the toolkit tests pass.
- **SC-006**: The migration (FR-011) passes its golden fixture on CI and one live run, with `dmc campaign verify`
  passing and no content line changed; the next full save after it fills budgets, clocks and equivalents (SC-002).
- **SC-007**: After release, a `review-transcript` review of the first 5 sessions on the new release finds the Arc
  plan true to play at each full save, no milestone held back by a stage the player put off, and no feedback entry
  about a jump offer.

## Assumptions

- The arc guidance lives in `dmc-new-campaign` (the constitution's Dungeon Master 1 names it); the full save points to
  it when it plans or revises, and reads only that part.
- Session numbers come from the session logs' file names, which every campaign has; no new `dmc` command is needed. A
  `dmc` count of sessions since the last fight, check or level (#179) is not part of this spec.
- The jump offer is a short question at the end of a beat, in or out of character as the moment suits, never a menu
  (§ Narration line 195's "without listing menu options" holds).
- Headless play tests run on copies of campaigns in `D:\Claude\test-campaign`, with inputs copied to the scratchpad, as
  earlier specs did; `rorns-landing` is the overtaken-plan case.
- The migration's number is the next after the highest in `kit/migrations/` when the plan is made
  (008 at `5a03569`).
- Causes 1 (a due beat delivered next session) and 7 (character arcs and NPC variety checked) are out of scope and stay
  in #181.
