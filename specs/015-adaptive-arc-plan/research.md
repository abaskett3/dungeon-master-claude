# Research: an arc plan built on a story structure, that DMC holds to and revises

Spec: [spec.md](spec.md). Every claim cites a file, a command or a source, or says "unverified" (Development 4).
Measurements were taken on `015-adaptive-arc-plan` at `5a03569` on 2026-11-15.

## R1. The Five Commandments of Storytelling

- **Decision**: the default structure and the template's example (spec clarification). One stage per beat, except
  progressive complication, over one to three stages.
- **Source**: Story Grid, "The Five Commandments of Storytelling" (storygrid.com/five-commandments-of-storytelling/),
  the publisher's own page for Shawn Coyne's method. Its one-line definitions:
  - Inciting incident: "destabilizes the protagonist by upsetting the balance of their life for good or for ill";
    either causal (an avatar's active choice) or coincidental (unexpected, random or accidental).
  - Turning point progressive complication: "a series of actions to restore balance" after the incident; the turning
    point is active (an avatar's action) or revelatory (a revelation).
  - Crisis: "a real choice between incompatible options with meaningful stakes".
  - Climax: "the active answer to the question raised by the crisis".
  - Resolution: "what happens as a result of the protagonist's choice during the climax".
- Story Grid's podcast pages (search of storygrid.com, 2026-11-15) add that a crisis is a best bad choice or
  irreconcilable goods, always binary, and must come directly from the turning point; progressive complications keep
  the character off balance until the turning point. That the five apply at every unit of story (scene, sequence, act,
  global) is stated by secondary sources (The Creative Penn, "How to write a scene that works", 2018); the Story Grid
  page above does not say it in those words. The guidance uses it only as advice for scenes, not as a rule.
- **Rationale**: five beats cost about what Freytag's five stages cost in the template (R9). The crisis is a dilemma
  the player chooses through, which is Dungeon Master 4: DMC plans the dilemma, never the answer. The climax as "the
  active answer" is the player's choice played out, so the plan can name the beat without scripting it.
- **Alternatives**: the Hero's Journey as the example (R2): twelve stages, longer, and its stages describe one hero's
  transformation, which fits a character arc better than a party's arc. Both named in the template: more bytes at every
  resume for a choice the guidance already covers.

## R2. The Hero's Journey

- **Decision**: covered in `dmc-new-campaign`'s guidance as the second structure, for an arc that is the PC's own
  change, and as a shape for character arcs under either structure. Its twelve stages are grouped into five to seven
  Arc plan stages, each naming the journey stages it covers.
- **Source**: Wikipedia, "The Writer's Journey: Mythic Structure for Writers" (Christopher Vogler, Michael Wiese
  Productions, 1992; 3rd edition 2007, ISBN 978-1-932907-36-0), which grew from Vogler's memo "A Practical Guide to
  *The Hero with a Thousand Faces*" on Joseph Campbell's book. The twelve stages: the ordinary world; the call to
  adventure; refusal of the call; meeting with the mentor; crossing the first threshold; tests, allies and enemies;
  approach to the innermost cave; the ordeal; reward; the road back; the resurrection; return with the elixir.
  Secondary sources (Film Courage, "12 Stages of the Hero's Journey", Vogler interview) group them as departure
  (1 to 5), initiation (6 to 9) and return (10 to 12). The Wikipedia page itself does not give the grouping.
- **Rationale**: the constitution asks for "at least" one of the two (Dungeon Master 1); naming both lets a player's
  premise pick the one that fits. A test campaign already planned in it ("The five stages and the hero's journey",
  `D:\Claude\test-campaign\adventures\rorns-landing\story\dm-notes.md` line 39), mixed with Freytag's stage names.
- **Alternatives**: Campbell's original seventeen stages (longer, and Vogler's version is the one writers use);
  leaving the Hero's Journey out (the constitution allows it, but the maintainer's own campaign used it).

## R3. Stages and beats

- **Decision**: one stage per beat; progressive complication may take one to three stages, each costing more than the
  last; so an arc has five to seven stages (spec clarification). Each stage line names its beat, what changes, its
  mix and a rough budget; the current stage names the session it began.
- **Rationale**: the beat names say where the crisis and climax fall, which is what a revision must keep (R4). The
  start session is needed to compare a budget (R7) and to tell when a stage was put off (spec clarification); without
  it the full save would have to infer it from the logs. A done stage keeps its one line ("what changed"), as now
  (`kit/templates/campaign/story/dm-notes.md` line 24).
- **Alternatives**: any number of stages each with its own five beats (Story Grid's "every scale"): more lines in a
  section read at every resume, and a plan that scripts scenes.

## R4. Revising a plan that play has overtaken

- **Decision**: the full save revises the stages still ahead whenever an outcome makes one untrue, by #181's rules:
  keep what play did not contradict, word for word; change only what is untrue; grow everything new from established
  events and planted threads; keep the stakes, the theme and every beat not yet played, through a resolution;
  re-anchor milestones not yet awarded (R6). Nothing untrue, nothing changed. The rules of a revision live in
  `dmc-new-campaign` step 4a with the rest of the arc guidance; the full save says when and points there (R10).
- **Sources**: #181 § Principle ("structured and adaptive"), agreed with the player. Justin Alexander, "Don't Prep
  Plots" (The Alexandrian, 2009-03-23): "Don't prep plots, prep situations"; the GM sets circumstances, the opponents'
  goals and resources, and reacts to what the players do. The guidance takes this as "each stage says what changes,
  not how the party gets there", and "plan the dilemma, never the answer".
- **What the kit does today**: the full save plans only when the Arc plan is missing or its last stage is done
  (`kit/skills/dmc-campaign-full-save/SKILL.md` line 44) and deletes "plans the story has overtaken" without
  replacing them (line 49). In the test campaign the villain Auntie Hush died at s28.1 and the stages still name her
  (`rorns-landing/story/dm-notes.md` lines 26, 53).
- **When a stage is put off** (spec clarification): the player says they will do it later or turns down its hook, or
  its budget, counted from when it became current, runs out before it starts.
- **Alternatives**: re-planning the whole arc (a restart, which #181 rules out); revising mid-session (the full save
  already has the story and the files in front of it, and a revision mid-play costs the player's time).

## R5. Antagonist clocks in sessions played

- **Decision**: each antagonist's next move names the session by which it happens (`by s14`). At the full save, a move
  whose session has come happens in the world; the Antagonists line records what changed and the sign of it the next
  session shows where the party is (news, a consequence, a changed place, an NPC's reaction), not necessarily a
  confrontation (spec clarification); the antagonist gets a next move. A clock timed only by an in-world date is
  restated in sessions.
- **Sources**: Dungeon World's fronts (Roll20 compendium, "Fronts", from the Dungeon World SRD): each danger has grim
  portents, steps toward an impending doom (3 to 5 for a campaign front), and fronts progress offscreen, whether or
  not the characters are there. Sly Flourish, "Fronts in D&D" (2013-03-11): "Each front moves forward, whether the PCs
  get involved or not." Neither says the portents advance per session; timing them in sessions is #181's change, from
  its evidence (a threat "days away" stayed away while five sessions covered one in-world day).
- **The clock's source**: the session number is the current `story/sessions/session-NNN.md` (the full save writes it,
  step 3, and stamps it on the Last full save line, step 8, `dmc-campaign-full-save/SKILL.md` lines 26, 74). No new
  `dmc` command; #179's session counts are out of scope.
- **Where the sign is kept**: on the antagonist's line, so the resume, which reads `dm-notes.md` whole, sees it before
  the next session; the full save after deletes it once shown. It is worded as an instruction ("Show next session:
  …"), not a note: #181's cause 1 is that DMC carries notes from save to save without acting on them. If the play test
  (SC-003) finds a sign not shown, that is evidence for cause 1, reported to #181, not a reason to widen this spec. A sign never decides a fight, check or cost the next
  session must hold: that is #181's cause 1, out of scope.
- **Alternatives**: in-world dates (what failed); Blades in the Dark's progress clocks (Roll20 compendium, "Progress
  clocks"), which tick on actions and consequences in play: more bookkeeping per exchange, and the play rules are at
  their cap.

## R6. Milestones as achievements with an equivalent

- **Decision**: each milestone names an achievement and the level it gives, "or a victory of the same weight". The
  full save awards one whose achievement or an equivalent was reached (line 46's Pending line and `dmc-character-sheet-
  update`, unchanged); a re-plan re-anchors every milestone not yet awarded; an awarded one is never awarded again.
- **Sources**: #181's evidence (the next level waited from session 28 on a place the player put off). The 5e DMG's
  milestone and story-based advancement (2014 DMG, about pp. 260 to 261) is unverified: no local rules reference
  (`dmc lookup rules milestone` finds nothing; `rules/index.md` is absent), and forum summaries only. The change does
  not rest on it.
- **Alternatives**: milestones per number of sessions (a level for showing up, against the story); experience points
  (a campaign's leveling is the player's choice in `player-options.md`, and this changes nothing for XP campaigns).

## R7. A rough session budget per stage

- **Decision**: each stage still ahead carries "about N sessions". The full save compares sessions since the current
  stage began with it; over budget, it writes in the stage how its next scenes tighten toward the next one, or revises
  (R4). It never cuts short an open exchange or a choice (`kit/dm-rules.md` line 195).
- **How long**: no source gives a session count per story beat. The guidance says to size a stage from how long this
  player's stages have run (About this player), with crisis and resolution usually one or two sessions; a judgment,
  not a source. A wrong budget harms nothing that R4 and R6 do not already catch.
- **Alternatives**: a fixed arc length (it would rush or pad); counting scenes (scenes vary more than sessions, and the
  logs number sessions).

## R8. The jump offer in `kit/dm-rules.md`

- **Decision**: one sentence appended to § Narration's bullet at line 195, after "the scene does not skip ahead until
  the player closes it (a running clock or threat still acts)", pointing at the break test of § Compaction and long
  sessions (line 205) rather than copying it (Development 6). Exact wording: [contracts/dm-rules-narration.md]
  (contracts/dm-rules-narration.md). 267 bytes, measured with `[Text.Encoding]::UTF8.GetByteCount`, about 67 tokens per
  turn by `dmc`'s bytes / 4 (`src/Dmc.Cli/Size/Measurement.cs`); within the 300 allowed (spec FR-008).
- **Placement**: line 195 already holds the open-exchange rule the offer must not break, so the two sit side by side;
  a new bullet would break the 212-line cap.
- **Cuts looked for**: line 195's "(a running clock or threat still acts)" is about an exchange held open, the new
  clause about a jump taken, so neither replaces the other; "Your own open notes stay in `dm-notes.md`" stops DMC
  listing its own plans as options; § Compaction's break test is needed for breaks. No cut was found that keeps every
  rule, so the byte cap rises by the sentence's size.
- **Alternatives**: a full save rule (too late: the stretch is in play); a hook (the context hook offers breaks by
  context size, not by story).

## R9. The template and its size

- **Decision**: the Arc plan comment's opening paragraph names the structure and `dmc-new-campaign` step 4a, and says
  the full save revises; Shape names the Five Commandments' beats, the budget and the current stage's start; Progression
  asks for achievements with "or a victory of the same weight"; the Antagonists comment asks for the next move and its
  session. Exact wording: [contracts/dm-notes-template.md](contracts/dm-notes-template.md).
- **Cost** (measured on the drafts): the Arc plan heading and comment grow from 1,542 to 1,911 bytes (+369); the
  Antagonists comment from 175 to 239 (+64). In all +433 bytes, about 108 tokens at every resume of a campaign that
  holds them (`dm-notes.md` is read whole, comments included).
- **A filled arc fits**: a seven-stage Arc plan filled with the new fields (three character arcs, three side quests,
  milestones, Last played) in a scratch DMC folder made from the templates measured `Arc plan 1.3k` against the 1.5k
  budget (`dmc size arc-example --folder <scratch>`, 2026-11-15), `dm-notes.md` 2k of 10k. Its one flag was a filled
  Progression line of 415 characters, over the 400 limit (`Budgets.LongLineCharacters`), which the full save already
  splits (step 5, "Split long lines"); the guidance says to keep milestones and money on separate lines.
- **Alternatives**: a budget rise for the Arc plan section (not needed: 1.3k measured); moving the guidance into the
  template (paid at every resume, and Dungeon Master 1 puts it in the skill).

## R10. Where the arc guidance lives

- **Decision**: a new step 4a, "Designing the arc", in `kit/skills/dmc-new-campaign/SKILL.md`, the guidance's one home
  (Dungeon Master 1, Development 6). Step 4 points to it. The full save's Arc plan and Revise checks say to follow
  "`dmc-new-campaign` step 4a, reading only that step"; the skill is at `.claude/skills/dmc-new-campaign/SKILL.md` in
  the DMC folder (`src/Dmc.Cli/Kit/GeneratedFiles.cs` line 10). Wording: [contracts/new-campaign-arc-guidance.md]
  (contracts/new-campaign-arc-guidance.md).
- **Cost** (measured on the contract's wording): step 4a is 21 lines and 3,068 bytes, and step 4's sentence grows by
  66, so `dmc-new-campaign` grows by 21 lines and 3,134 bytes (12,920 to about 16,054), about 784 tokens, paid when a
  campaign is created and when a full save plans or revises an arc, which reads only step 4a (about 767 tokens). A skill cross-reference by step is already used
  (`dmc-rewind-campaign` line 54: "the consistency check from `dmc-campaign-full-save` (step 7)").
- **Alternatives**: the guidance in the full save (paid at every full save, 11,438 bytes today); a separate kit file
  (a new file kind for one section; the constitution names the skill).

## R11. The full save's arc check

- **Decision**: step 5's check gains three bullets (Revise, Pace, Clocks), and Arc plan and Progression change in
  place. Wording: [contracts/full-save-arc-check.md](contracts/full-save-arc-check.md). Measured on the contract's
  wording: +1,082 bytes (the five bullets 1,691 against the two they replace, 609), about 270 tokens, on 11,438, paid
  at each full save; 3 lines added (one per new bullet).
- **Rationale**: the full save is where #181 places causes 2, 3, 5 and 6, and the only point where the whole session
  and the files are in front of DMC.

## R12. Migration 008

- **Decision**: `kit/migrations/008-arc-plan-structure.md`, standard 8: replace the Arc plan's and Antagonists'
  template comments with the template's, add the comment where a section has none, add an `## Antagonists` section
  (heading and comment, no content) after the Arc plan where there is none (after the central conflict, or the
  file's title, when there is no Arc plan), and change no content line. Budgets, clocks and
  milestone equivalents are filled at the next full save (spec clarification). Check `arc-plan-guidance-current`. Contract:
  [contracts/migration-008.md](contracts/migration-008.md).
- **Comment versions to recognise** (`git log -p -- kit/templates/campaign/story/dm-notes.md`): the Arc plan comment
  of PR #170 (`50e43d5`; opening "The current arc of a homebrew campaign, or the stretch after a module ends. Plan
  it") and of PR #221 (`cd4f755`; "...after a module ends; in a module campaign, only Character arcs until then"); the
  Antagonists comment of the first kit (`d4523c6`, "For each: what they want, what they are doing right now offscreen,
  what they know about the PC.") and of PR #170 (adds the names). Every version opens "The current arc of a homebrew
  campaign" or "For each: what they want", so the migration matches on those openings.
- **Order**: migration 007 shipped in 0.4.1 (`.docs/releases.md`; it had not when this was planned: 0.4.0 is `adb0e07`, before PR #221), so a campaign at standard 7 holds PR #221's comment, which 008 replaces. A campaign at
  standard 6 takes 007 then 008 in one run; 007 copies the template's comment, already the new one, so 008 finds
  nothing to do. A campaign that took 007 from a development build holds PR #221's comment, which 008 replaces.
- **The check** (`src/Dmc.Cli/Campaign/MigrationChecks.cs`): fails when `story/dm-notes.md` has an `## Arc plan`
  section with no comment naming "Five Commandments", or has no `## Antagonists` section, or one with no comment naming
  "next move". The existing `Section` helper strips comments (`Lines`, line 106), so the check reads the section raw.
- **Alternatives**: filling budgets and clocks in the migration (it would guess at pace and plans, inventing what the
  files do not say); no migration (old comments would keep Freytag's example and no clocks until edited by hand).

## R13. The play test

- **Decision**: on a copy of `D:\Claude\test-campaign\adventures\rorns-landing` (session 54; Hush dead at s28.1 and
  still in the stages; its arc under "The five stages and the hero's journey"; standard 5, behind by 006, by
  `dmc campaign standard rorns-landing` with `CLAUDE_PROJECT_DIR=D:\Claude\test-campaign`), with the test folder's kit
  refreshed from this branch's build, so migrations 006 to 008 run first. Then one full save (SC-002) and at least 5
  headless sessions with a full save after each (SC-003, SC-004), and a new homebrew campaign created headlessly
  (SC-001). Steps in [quickstart.md](quickstart.md).
- **How**: `claude -p` in the test folder, with `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0` when a subagent runs, and
  inputs copied to the scratchpad, as earlier tests did (the maintainer's memory notes for headless runs).

## R14. Documents

`.docs/decisions.md` (a new entry; the 3.0.0 entry's Limits drop the Dungeon Master 1 item, line 731),
`.docs/roadmap.md` (a row for #217 and #181's causes 2 to 6), `.docs/campaign-files.md` (the `dm-notes.md` row, line
73: structure, budgets, clocks, milestones; migration 008), `.docs/user-guide.md` § Talking to DMC (line 115: DMC may
offer to skip ahead when a scene is done, and you can say no), and any document that lists the migrations or their
checks (found by a search for `007-arc-plan-everywhere` and `dm-notes-have-arc-plan` at implementation).

## R15. After the first full save on a migrated campaign

- **What happened** (2026-11-15, `arc-rorns`, a copy of `rorns-landing` migrated 006 to 008): the full save cut the
  finished stages to a line each, but revised nothing (stages 3 and 4 still have Auntie Hush, dead at s28.1, as their
  villain), added no budgets, start session, milestone equivalents or clocks, and deleted the Arc plan's template
  comment to bring the section from 2.6k toward its 1.5k budget, so `arc-plan-guidance-current` failed.
- **Causes**: Revise read as "an outcome this session", and s54 had no scene; nothing told the full save to bring an
  older plan up to the template's shape, since Pace and Clocks only compare fields already there; and the section's
  budget counted the comment (about 480 tokens of 1,500), the obvious thing to cut.
- **Decision** (the maintainer's): the Arc plan's budget counts its content only (`Flags.ArcPlanContentTokens`,
  comments and heading left out; `Budgets.DmNotesArcPlan`'s comment says so); the comment still counts in
  `dm-notes.md`'s total. The full save's Arc plan bullet adds what the plan lacks from what the files say and never
  deletes a template comment; Revise acts on a stage that is untrue "whenever play made it so". Full save +263 bytes
  (12,783 in all, +1,345 on 11,438).
- **Rejected**: keeping the budget and forbidding the deletion (leaves the plan about 1k tokens, too little for a long
  campaign); raising the budget (the comment still competes with the plan).
