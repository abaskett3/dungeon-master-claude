# Feature Specification: The feedback round of #181

**Feature Branch**: `016-feedback-round-181`

**Created**: 2026-11-15

**Status**: Draft

**Input**: User description: "on #181". Issue #181: fix every open feedback entry in the DMC folder's `feedback/` and
the arc plan going stale. The fixes must change how DMC works, not only add wording. The most damaging are **A**, DMC
inventing where the files already have the answer, and **B**, DMC overcorrecting after a slip. The issue holds the
evidence, the draft plan (A1 to A7, S1, B1 to B3, the B-hook, C1 to C3, D1, D2, E1, F1 to F4, S2, G, H, R1 to R3) and
the maintainer's decisions of 2026-11-15; this spec turns them into requirements and leaves the wording to the plan.

## Background

### The two things that matter

`kit/dm-rules.md:5-8`: the story and the game state stay consistent and accurate, and the player has fun. A breaks the
first every time it happens and the second through the correction that follows. The maintainer, 2026-11-15: "If this
happens there is no point to anything we do."

### The feedback

`D:\Claude\dmc\feedback\` holds 20 open entries (checked 2026-11-15), all from one campaign, rorns-landing, on releases
0.2.1 to 0.4.1. Issue #181 lists 18 of them, recorded 2026-11-09 to 2026-11-15 (sessions 37 to 59). Two were
recorded later, in session 60, and are not in the issue:

- `2026-11-15-dead-time-played-out-and-beats-end-with-nothing-pending` (pacing): routine moments played beat by beat,
  replies ending on atmosphere or "anything from you?" with no decision in them.
- `2026-11-15-sarcastic-reply-acted-on-as-a-literal-instruction` (conduct): a sarcastic reply to an offer to skip ahead
  was taken literally; DMC skipped most of a day and wrote it into `game-state.md` and the session log before asking.

### What causes A

The issue's table, from the play transcripts (§ What causes A): **the fact was never read** (grey hood, s49; Kole's
watch and lodging, s55); **the file contradicted itself** (socks, s51; flying, s59; cold immunity, 2026-11-09);
**read and right, still not used** (flying, s59); **no way to check** (the count, s59; the scene heading holds only
title, location and in-world time, `kit/templates/session-log.md:13`); **pure invention** (the host and table, s55 and
s56; "a hand's width", s56). Context size and the model were ruled out: 581 of 631 player turns were past 150k tokens,
and the same kind of slip came on Sonnet 5.5 and Opus 5.5.

### Open work that touches this spec

- **PR #228** (spec 015, merged 2026-11-16 at `0cfdfa1`, `Closes #217`, `Part of #181`) added **Revise**, **Pace** and
  **Clocks** to `kit/skills/dmc-campaign-full-save/SKILL.md` step 5 (lines 44-50), the jump offer to `kit/dm-rules.md`
  § Narration, a template change and migration 008. Those are #181's R1 to R3. Its play test of an overtaken plan
  (SC-002) was **not met**: two full saves on a copy of rorns-landing cut the finished stages but revised nothing.
  The PR leaves the revision to #181. The `kit/dm-rules.md` byte cap now stands at 30,914
  (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1209`).
- **What the resume reads of each sheet (spec 013).** A companion, a block under `game-state.md`'s Party status, is
  read through its play card only; an NPC named on the Present line or in the Combat tracker is read down to its
  History (`src/Dmc.Cli/Size/ReadSet.cs:174-226`); anyone else is not read (`.docs/decisions.md:684`). Nothing reads
  the sheet of someone who joins the scene mid-session. Its Limits line: a fact off the card "relies on DMC noticing the
  moment". Its gate (`.docs/decisions.md:721`): companion slips 0, but slips by everyone else rose from 0.23 to 1.0 per
  session, which "does not decide the gate". In rorns-landing 21 of 25 sheets have no play card (measured 2026-11-15).
- **The session-60 rules already exist.** `kit/dm-rules.md:180`: "A joke, sarcasm or complaint is never an event in
  the fiction; if unquoted text could be either, treat it as out of character and ask." `kit/dm-rules.md:195`: a beat
  with no decision in it is closed in a line. Both slips broke a rule DMC had, so new wording alone does not fix them.
- **#193** (open): widen the recall trigger and add `dmc recall --brief`. It overlaps A4 and S1.
- **#226** (closed): the map's moving aura and view window, replaced here by S2.

## Clarifications

### Session 2026-11-15 (decisions recorded in #181)

- Cost: raise `kit/dm-rules.md`'s byte cap by the measured amount and keep every clause.
- Break offers: the band grows from 40k to 50k tokens (`src/Dmc.Cli/ContextCheck/BreakReminder.cs:18`); playing on
  after an offer counts as declining it; the break points stay as they are.
- Arc: the fix goes in the full-save skill only, with no template change and so no migration. (Changed 2026-11-16:
  `dmc size --arc` flags for the full save; still no template change or migration. See Session 2026-11-16.)
- Conduct after a slip (the maintainer's principle): DMC offers sustainable, realistic fixes that get to the core of
  the problem and work with the release and file versions it runs on; it does not assume a fix, does not change files
  without the player's agreement, and is not a yes-man.
- The arc (the maintainer's principle): structured and adaptive. A beat that is due comes, and only its form adapts;
  a re-plan is a revision, not a restart.

### Session 2026-11-15 (this spec)

- Q: Does the arc work build on PR #228 or on `main`? → A: On #228, after it merges. This spec makes the full save
  act on #228's Revise, Pace and Clocks; it does not rewrite them. Planning the arc part waits for the merge; A to H
  do not.
- Q: Is the dead-time entry in scope? → A: Yes, as User Story 7, tied to the arc work. The plan first checks the
  session-60 transcript for why nothing was pending; the unverified guess is the stale arc. No new wording until the
  cause is found.
- Q: Is the sarcasm entry in scope? → A: Yes, with B (FR-018): a clause on `kit/dm-rules.md:180`, not a new rule.
- Q: Which lines does the hook show for each named person in the scene (S1)? → A: Looks or Appearance, Usually
  found, Speed, Traits or Standing traits, and Items, from either sheet shape; cap about 10k bytes, in the Present
  line's order (first "most recently added first", which no file records; changed 2026-11-16); anyone past the cap is covered by FR-002; a crowd counts as nobody (FR-008). SC-004 counts slips by everyone,
  not only companions.
- Q: Should anyone the player directs in a fight be a companion? → A: Yes (FR-032). Hala fights as Shale's companion
  by the player's ruling (rorns-landing `canon.md:38`, s41.1) but is filed under "With the party, not companions"
  (`game-state.md:65`), so she had no play card and was read only when on the Present line (down to her History;
  corrected 2026-11-16, the clarify session first said she was never read). The player has since moved her into Party
  status (`game-state.md:63`); until the next full save writes her card, the resume reads her sheet down to its History
  every time (`ReadSet.cs:356-366`).

### Session 2026-11-16 (the plan)

- Q: The arc fix as `dmc size` flags, against #181's "full-save skill only"? → A: Yes, but the player never acts on
  them or sees them: they print only for the full save (`dmc size <campaign> --arc`), which fixes them in
  `story/dm-notes.md` without a word to the player and leaves them out of its report (FR-026).
- Q: The hook's sheet lines once per session part, or again after a distance? → A: Again once 100,000 tokens have
  passed since they were last shown or the sheet was read (FR-008).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - DMC says what the files say about the people in the scene (Priority: P1)

The player is in a scene with characters whose sheets, history and logs already settle what they wear, own, can do,
know and where they live. DMC's narration and their lines agree with those files. Where the files are silent, DMC may
invent, and logs what it invented.

**Why this priority**: A. Nine entries, and the first of the two things that matter.

**Independent Test**: Replay the three slips the issue names (flying, s59; socks, s51; Kole's lodging, s55) headless in
`D:\Claude\test-campaign` from copies of the files as they stood, and check each reply against the files.

**Acceptance Scenarios**:

1. **Given** a character whose files show they cannot yet do something their species usually can (fly, resist cold),
   **When** a reply touches it, **Then** DMC narrates what the files show, never the species or class default.
2. **Given** play settled that a character took off, gave away or lost an item, or moved house, **When** the change is
   recorded, **Then** the standing line on their sheet (and stat block) says so, not only a History entry.
3. **Given** a character joins the scene, **When** DMC adds them to the Scene's Present line, **Then** DMC has read
   their sheet down to its History in that session part before it voices them.
4. **Given** a reply depends on who was present at an earlier scene ("how did she know?", a head count), **When** DMC
   checks, **Then** the scene's own record lists who was there, without rereading the whole scene.
5. **Given** a fact about an established character that no file holds (a number, a duty, a choice), **When** a reply
   needs it, **Then** DMC leaves it vague or asks, and never invents a specific figure.
6. **Given** people on the Present line, **When** the player sends a message, **Then** their standing lines are in
   front of DMC before it writes the reply, unless shown or read within the last 100,000 tokens of the session part.

---

### User Story 2 - A correction fixes the fact and nothing else (Priority: P1)

When the player says a fact is wrong, or asks how someone knew something, DMC reads the files, quotes them, says what it
thinks, proposes one fix and waits for the player's agreement before changing any file beyond the play state. It never
rewrites the player's words, never adds a ruling or a canon line to make its slip true, and when asked why it slipped
names the step it skipped, never "a habit" or a promise.

**Why this priority**: B. Four entries; it follows every A.

**Independent Test**: Headless, challenge DMC on a seeded slip in character and out of character, and check the reply
and the file changes against the scenarios.

**Acceptance Scenarios**:

1. **Given** DMC stated a wrong fact, **When** the player points it out, **Then** DMC quotes the files before it
   answers, and the only file changes before the player agrees are to the play state.
2. **Given** a slip, **When** DMC corrects it, **Then** the player's own text is unchanged and no new ruling or canon
   line justifies the slip.
3. **Given** the player disagrees with DMC's reading of the files, **When** DMC answers, **Then** it says what it
   thinks and why, and does not agree at once.
4. **Given** an out-of-character message (starting with `ooc` or `((`), **When** it reaches DMC, **Then** the rule of
   scenario 1 is in front of DMC at that moment.
5. **Given** the player asks why a slip happened, **When** DMC answers, **Then** it names the skipped step and the
   moment, with no promise.

---

### User Story 3 - The arc plan follows play (Priority: P2)

After a full save, an Arc plan whose stages play has overtaken (a villain dead, a goal changed, a stage running far
over its sessions, a clock timed only in in-world dates) is revised: what still stands is kept, what is untrue is
changed, and the plan still ends.

**Why this priority**: the issue's arc section. PR #228 wrote this guidance and its test showed DMC does not act on it.

**Independent Test**: Run a full save headless on a copy of today's rorns-landing (the s59 state is not kept; Auntie
Hush dead since s28.1, Stage 2 since s12, Stage 3 labelled level 7, the Siege timed on the 28th), and read the Arc plan after.

**Acceptance Scenarios**:

1. **Given** a remaining stage built on an antagonist who is dead, **When** the full save runs, **Then** that stage no
   longer depends on them, and the stages play did not contradict are unchanged.
2. **Given** the current stage has run past its planned sessions, **When** the full save runs, **Then** the stage says
   how its next scenes tighten toward the next one, without rushing the player.
3. **Given** a clock or an antagonist's next move timed only by an in-world date, **When** the full save runs,
   **Then** it is also timed in sessions played.

---

### User Story 4 - DMC never acts for the player's side (Priority: P2)

A question is answered and waited on, not taken as a yes. In combat DMC resolves only what the player declared for
their character or a companion they direct. A reaction the player takes during an enemy's turn is the player's roll. A
general "us" or "let's" covers everyone present unless the player excepts someone. A scene the player writes out with
other characters' lines is an outline to play. A sarcastic, ironic or emotionally charged reply whose literal reading
would change time, state or the plot is asked about before anything is committed.

**Why this priority**: C, G, H and the session-60 sarcasm entry; they take a choice from the player.

**Independent Test**: Headless, send seeded messages for each case and check that nothing the player did not declare
is resolved, rolled or written.

**Acceptance Scenarios**:

1. **Given** the player asks "can he move up?", **When** DMC answers, **Then** the companion does not move until the
   player says so.
2. **Given** an enemy's turn triggers the player character's reaction, **When** the player takes it, **Then** DMC asks
   for the roll and stops.
3. **Given** an ambiguous or sarcastic reply that would skip time, **When** DMC reads it, **Then** DMC asks in one line
   and writes nothing to the files until the player confirms.
4. **Given** the player writes a scene outline with other characters' lines, **When** DMC reads it, **Then** DMC plays
   the scene, or asks if it is unclear, and does not log it as played.

---

### User Story 5 - Fight maps show the fight (Priority: P3)

Friendly creatures the player does not direct are coloured differently from companions. A fight on a large saved place
is drawn at the size of the part in play. When the player says the map is not zoomed, the map itself is fixed, not
replaced by a cropped picture. An aura or other moving hazard moves with its creature.

**Why this priority**: F and S2; three entries, all about remote play's only view of a fight.

**Independent Test**: Draw a fight with a companion, a friendly NPC and a creature with an aura on a large saved map,
move the creature, and check the drawn map.

**Acceptance Scenarios**:

1. **Given** a companion and a friendly NPC the player does not direct, **When** the map is drawn, **Then** they have
   different colours.
2. **Given** a creature with an aura moves, **When** the map is redrawn, **Then** the aura is around its new cell.
3. **Given** a fight in a small part of a large saved place, **When** the map is drawn, **Then** it shows that part
   with the place's own cell labels.

---

### User Story 6 - Fewer break offers, and clean fiction (Priority: P3)

Break offers come about every 50k tokens of play rather than 40k, and playing on after one counts as declining it. No
real-world word (a holiday, a brand, our month names where the world has its own) appears in the fiction; one caught
while writing is rewritten, never annotated.

**Why this priority**: D and E; one entry each, costs to fun rather than to consistency.

**Independent Test**: `dotnet test` for the band; a headless check of a reply that would use a real-world date word.

**Acceptance Scenarios**:

1. **Given** DMC offered a break and the player played on, **When** the band's next point is reached, **Then** at most
   one offer is made in that band.
2. **Given** a line that would name a real-world holiday, **When** DMC writes it, **Then** the fiction uses the world's
   own word, with no out-of-character note.

---

### User Story 7 - Routine time closes in a line (Priority: P3)

When nothing is pending, DMC closes routine moments in a line and moves time to the next decision, event or
consequence, never ending a reply on atmosphere or an empty "anything from you?". Asked whether the character
perceives anything, DMC answers with a roll or "nothing here".

**Why this priority**: the session-60 entry `2026-11-15-dead-time-played-out-and-beats-end-with-nothing-pending`. The
rule exists (`kit/dm-rules.md:195`) and was not followed, so the fix rests on its cause (FR-031).

**Independent Test**: Headless, start a session on a copy of rorns-landing with nothing pending after the arc fix, and
read the first replies.

**Acceptance Scenarios**:

1. **Given** nothing is pending, **When** a routine stretch begins, **Then** DMC's reply ends on something the
   character can see, hear or choose.
2. **Given** the player asks whether the character sees anything, **When** nothing is there, **Then** DMC says so,
   or asks for a roll with its DC.
3. **Given** a full save has run with the arc fix, **When** the next session starts, **Then** something is pending.

---

### Edge Cases

- A sheet whose standing line and History disagree from before this release: the full save's check (FR-005) fixes it
  when that sheet next changes; sheets not touched are not swept.
- A character on the Present line with no sheet (a bit part): the standing-line reminder prints nothing for them and
  DMC invents within the canon, then logs it.
- The Present line holds many people, or a crowd (the whole town at rorns-landing s1.1): the reminder covers the
  named people with sheets, in the Present line's order, up to its cap of about 10k bytes (from 4,000,
  `src/Dmc.Cli/Retrieval/CanonReminder.cs:19`); a crowd is nobody; anyone past the cap is read by FR-002 when DMC
  brings them forward. Measured on rorns-landing: these lines are 240 to 770 bytes for townsfolk and 1,300 to 2,300
  for companions, so 8 people with five companions is about 10k bytes, about 2.5k tokens each time the lines refresh (FR-008).
- A sheet of the older shape (no play card): the reminder takes the same lines from its Appearance, Usually found and
  Stat block.
- A character filed under "With the party, not companions" whom the player directs in a fight: FR-032 makes them a
  companion.
- An in-character challenge ("How did you know there were three?") does not start with an out-of-character marker:
  the B-hook stays silent, and B rests on the rule alone.
- Old session logs have no Present line on their scenes: the "who could know" check falls back to reading the scene.
- A campaign started before linked-campaign recall (PRs #187, #195): the player's NPC audit Pending line stands.
- A module campaign: User Story 3 applies only once its module is finished, as the full save's Arc plan rule says.

## Requirements *(mandatory)*

### Functional Requirements

**A. Using the files (§ What causes A)**

- **FR-001** (A1): When play settles what someone can or cannot do, wears, owns or where they live, DMC MUST rewrite
  that standing line of their sheet, and the stat block where it applies, not only add a History entry.
- **FR-002** (A2): When DMC adds someone to the Scene's Present line, it MUST read their sheet down to its History in
  that session part before voicing them.
- **FR-003** (A3): Each scene in a session log MUST record who was present, and the "who could know" check
  (`kit/dm-rules.md` § Accuracy, "Say only what the listener could know") MUST point to it.
- **FR-004** (A4): A species or class MUST never be the source of what an established character can do; if their files
  do not show it, they cannot do it yet. DMC MUST never invent a number about an established character.
- **FR-005** (A5): The full save MUST check each sheet changed in the session and fix any standing line or stat block a
  History entry overturns.
- **FR-006** (A6): The character-sheet skill's species-default rule MUST apply to every character, not only one carried
  from a linked campaign.
- **FR-007** (A7): A reply that matters to a relationship MUST be quoted in the log, never summarised.
- **FR-008** (S1): Before each player message, the context check MUST put in front of DMC, for each named person on
  `game-state.md`'s Present line or in its Combat tracker who has a sheet, not only those the message names, their Looks or Appearance, Usually
  found, Speed, Traits or Standing traits, and Items lines, from either sheet shape; again only once 100,000 tokens of
  this session part have passed since their lines were last shown or their sheet was read; in the Present line's order,
  within a cap of about 10k bytes. A crowd prints nothing. A sheet with none of these lines
  falls back to today's canon lines.
- **FR-032**: A companion MUST be anyone travelling with the party as one of its own or directed by the player in a
  fight (`kit/templates/campaign/game-state.md:20`). The plan says whether existing campaigns need a migration to
  re-check who is filed where (constitution Development 2).

**B. After a slip**

- **FR-009** (B1): A correction MUST fix only the fact that was wrong; it MUST NOT reword the player's text or add a
  ruling or canon line to make the slip true.
- **FR-010** (B2): When the player says a fact is wrong or asks how someone knew, DMC MUST read the files and open its reply
  by quoting what they say, for and against, say what it thinks, propose one fix, and change no file until the player
  agrees; the play state may note the fix as waiting on them (changed 2026-11-16 after the headless `ooc` run, quickstart
  § Results: an unconfirmed fix written to the play state was read as settled at the next resume).
- **FR-011** (B-hook): An out-of-character message (starting with `ooc` or `((`, `kit/templates/player-options.md:18`)
  MUST put FR-010 in front of DMC at that moment, in one line, and add nothing to other messages.
- **FR-012** (B3): Asked why a slip happened, DMC MUST name the step it skipped and the moment, never "a habit" or a
  promise.

**C, G, H and the sarcasm entry. The player's side**

- **FR-013** (C1): A question about an action MUST be answered and waited on, not taken as the action.
- **FR-014** (C2): In combat DMC MUST resolve only what the player declared for their character or a companion they
  direct, never choosing its action, target or ammunition.
- **FR-015** (C3): A reaction the player takes during an enemy's turn MUST be rolled by the player: DMC asks and stops.
- **FR-016** (G): A general "us" or "let's" MUST cover everyone present unless the player excepts someone.
- **FR-017** (H): A scene the player writes out with other characters' lines MUST be played as an outline; if unclear,
  DMC asks.
- **FR-018** (`2026-11-15-sarcastic-reply-acted-on-as-a-literal-instruction`): a reply that may be sarcastic, ironic
  or venting, whose literal reading would change time, the files or the plot, MUST be asked about in one line, and
  nothing written to the files until the player confirms. A plain yes goes through. Home: a clause on
  `kit/dm-rules.md:180`.

**D and E**

- **FR-019** (D1): Playing on after a break offer MUST count as declining it.
- **FR-020** (D2): The break band MUST be 50,000 tokens, and every document that states 40,000 MUST say 50,000.
- **FR-021** (E1): The fiction MUST hold no real-world words; one caught while writing MUST be rewritten, never
  annotated.

**F. Maps**

- **FR-022** (F1): `ally` MUST be for companions the player may direct and `neutral` for any other friendly creature.
- **FR-023** (F2): A fight on a large saved place MUST be drawn from a trimmed copy no bigger than the part in play,
  made before cells go into the tracker.
- **FR-024** (F3): When the player says the map is not zoomed, DMC MUST fix the map itself, never send a crop instead.
- **FR-025** (F4, S2): An aura or other moving hazard MUST move with its creature; `dmc map` MUST support an aura tied
  to a token and a view window that keeps the place's own cell labels.

**The arc**

- **FR-026**: Building on PR #228's Revise, Pace and Clocks after it merges, the full save MUST actually revise
  remaining stages an outcome made untrue, keeping what still stands; compare the current stage's sessions with its
  plan; and time each clock and next move in sessions played. The plan finds why #228's test revised nothing and fixes
  that cause; it does not restate #228's wording. No template change and no migration beyond #228's. The cause's fix
  (research R2): `dmc size <campaign> --arc` flags an overtaken plan for the full save only; the save fixes each flag in
  `story/dm-notes.md` and never shows or puts one to the player, in play or in its report.
- **FR-031**: Before any wording for User Story 7, the plan MUST check the session-60 transcript for why nothing was
  pending, and fix that cause. Checked (research R3): chores were pending but nothing was due that day, because the
  clocks ran on a far in-world date; the fix is FR-026's clocks flag, with no new `kit/dm-rules.md` wording.

**Cost and checks**

- **FR-027**: `kit/dm-rules.md` MUST add no lines; its byte cap
  (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1209`) MUST be raised by the final measured amount only. The issue's draft
  measured +1,196 bytes, about 299 tokens per turn, against `b07eb3d`; after #228 the cap starts from 30,914.
- **FR-028**: The plan MUST measure the cost of FR-008 per message, of the B-hook, and of each skill and template
  change, as constitution Development 1 asks.
- **FR-029**: Each change's home MUST be the one the issue names, or the plan says why another (constitution
  Development 6).
- **FR-030**: Each feedback entry this spec fixes MUST get its status set when the PR merges, as `review-feedback`
  does.

### Key Entities

- **Standing line**: a line of a character's sheet that says what is true now (clothing, gear, abilities, where they
  live, where they are usually found), as against the History, which says what happened.
- **Companion**: anyone travelling with the party as one of its own or directed by the player in a fight (FR-032);
  has a block under Party status and a play card the resume reads.
- **Present line**: the list of who is in the scene, in `game-state.md`'s Scene section
  (`kit/templates/campaign/game-state.md:14`), and, new here, on each scene of the session log.
- **Feedback entry**: a file in the DMC folder's `feedback/` with a status; this spec's scope is the open ones.
- **Arc plan**: the stages, beats, budgets and clocks in `story/dm-notes.md`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In the headless replay of the three slips (flying, socks, Kole's lodging), DMC's reply agrees with the
  files in 3 of 3.
- **SC-002**: In a headless challenge of a seeded slip, in and out of character, DMC quotes the files before answering
  and changes no file but the play state before agreement, in every run.
- **SC-003**: A full save on a copy of today's rorns-landing (the s59 state is not kept; Hush is still in the stages
  ahead) leaves no remaining stage built on Auntie Hush, keeps the
  stages play did not contradict, and times every clock in sessions.
- **SC-004**: Over the first 10 sessions played on the release, `review-transcript` counts at most half as many
  corrections from inventing over the files per session as over sessions 55 to 59 (1.4 per session, research R1;
  earlier logs have no `Slip:` line), counting slips by everyone, companions or not, and no correction that grows a ruling or canon line to make a
  slip true.
- **SC-005**: Break offers come at most once per 50k-token band in those sessions.
- **SC-006**: Every feedback entry in scope is fixed, or the PR says why not.
- **SC-007**: `dotnet test` passes, with `kit/dm-rules.md` at its line count and its new byte cap.

## Assumptions

- The session-log template is copied at each new session (`kit/skills/dmc-resume-campaign/SKILL.md:52`), so FR-003
  reaches new logs without a migration; old logs keep their shape. The plan confirms this (constitution Development 2).
- #193 stays open: this spec fixes A by putting the facts in front of DMC (FR-002, FR-008) and keeping sheets true
  (FR-001, FR-005), not by widening recall. Whether #193 is still needed is judged from SC-004's count.
- The voice-drift entry's first cause, the sheet left in the earlier campaign, is already addressed by linked recall
  (`kit/dm-rules.md:71`; PRs #187, #195) and the rebuilt sequel import (PR #196); this spec fixes its second cause
  (FR-007).
- SC-004's halving target is a proposal; the maintainer may set another when the baseline is counted.
- Not verified (from the issue): the second knowledge slip (Kole and the cobbler's order, s59); whether DMC keeps the
  Present lines up in play; FR-008's token cost per message.
- The headless replays may cost 150k+ tokens of context each; they run in `D:\Claude\test-campaign` from copies.
