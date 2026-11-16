# Feature Specification: "Yes, and…" and "No, but…"

**Feature Branch**: `014-yes-and-no-but`

**Created**: 2026-11-15

**Status**: Draft

**Input**: User description: "#216". Issue #216: teach DMC "Yes, and…" and "No, but…", so the player's choices change
the story and DMC never refuses out of character (constitution 3.0.0, Dungeon Master 4). A short definition and one
example of each, from the maintainer's description; weave what the character wants into the main plot. `kit/dm-rules.md`
is read on every turn, so the plan counts what it adds (Development 1). The details are in `.docs/decisions.md` § "The
constitution states principles of its own (version 3.0.0)", Limits.

## Background

### What the play rules say today

`kit/dm-rules.md` (212 lines, 29,917 bytes on `main` at `adb0e07`) has pieces of this, but no rule for answering a
choice that would break the rules, the lore or the story:

- § Fun first, line 166: "Say yes to creative ideas when you can. When a strict reading would kill the momentum over
  something minor, rule in favor of the cool idea and keep moving." This is "Yes" without the "and".
- § Fun first, line 169: "Recklessness is different from bad luck. Signal serious danger clearly in the fiction, once.
  If the player goes ahead anyway, let the consequences land in full, including character death."
- § Running a published module, line 115: "Let the player leave the script. Do not railroad."
- § Out of character, line 175: out of character, DMC answers "as a collaborator, frankly", rules included.
- Nothing says what to do when the player tries something the world cannot allow, or how to weave what the character
  wants into the main plot.

### The maintainer's examples

From `.docs/decisions.md` § "The constitution states principles of its own (version 3.0.0)":

- **Yes, and.** In Rorn's Landing, Shale wanted a permanent returning weapon, and DMC made the search for a runeforger
  part of the main plot. The player liked it.
- **The story adapts.** Hush was planned for the siege set-piece; Shale captured and executed her at s28.1, long
  before it. The arc plan changed and the narrative held.
- **No, but.** A party that attacks a major NPC far beyond it, such as Phillip Druskenvald on the train in The Crooked
  Moon, is stopped by his powers without being killed; he answers "Oh, I like the spunk" and sets them on the quest.
- The maintainer's definitions: "Yes, and" accepts an idea that fits the fiction and adds a helpful detail or hook (a
  player swings on a tavern chandelier to a balcony: the chain holds, and a guard's key ring hangs beside the ledge).
  "No, but" refuses an impossible action, or a fact that conflicts with the rules, physics or established lore, and
  offers an alternative so play does not reach a dead end (no secret door behind the cave wall, but a narrow crawlspace
  with a cold draught near the floor).

### The budget

`TheAlwaysLoadedRulesStayAtTheirLineCountAndGrowByAtMostFourHundredBytes`
(`tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1207`) holds `kit/dm-rules.md` at 212 lines and 30,043 bytes (spec 013,
FR-028). The file is at 212 lines and 29,917 bytes, so 126 bytes and no line are left (187 before PR #220 added 61
to line 58). A definition and an example of each does not fit in 126 bytes.

### What the first play test found

The headless play test of the first wording (2026-11-15, `quickstart.md` § Results) found:

- **"No, but" without the "but".** Of 4 ideas the world ruled out, 1 got a way forward in the fiction (a Fireball
  that fails, then "it heard you"). The hidden door and the cloak-flight were refused in the fiction with no way
  forward, so SC-002 was not met. The wording said "turn it aside, with another way forward", and DMC turned it aside
  but offered nothing toward what the player wanted.
- **Wrong premises are corrected out of character.** Four times the player's idea rested on a fact the character
  would know was different (where the groove started, a pine that is not there, a third potion when the sheet has
  two, which light calls Rorn). Each time DMC stopped and said so out of character before resolving, with the ways
  forward, as `kit/dm-rules.md` line 153 says: "If something the player's decision rested on turns out wrong (a range,
  an ability, a position), stop and tell them before resolving what follows." It never said the story does not allow
  it.
- **Not every campaign has an Arc plan.** `## Arc plan` came into the template with commit `50e43d5`, after which no
  migration added it, so older campaigns lack it; the test campaign plans its arc under "The five stages and the
  hero's journey" instead. The template's comment keeps the section for "the current arc of a homebrew campaign, or the
  stretch after a module ends", and the full save adds it only then (`kit/skills/dmc-campaign-full-save/SKILL.md`,
  step "Arc plan"). So line 165's "through the Arc plan's Character arcs" pointed at a section many campaigns do not
  have. In the test the goal went to an open thread in `dm-notes.md`, tied to the main plot.

## Clarifications

### Session 2026-11-15

- Q: How should the rule fit `kit/dm-rules.md`'s budget (212 lines, 187 bytes left)? → A: Raise the byte cap by
  about 500 bytes, with no new line: the definitions and both examples go into existing bullets, and the plan looks
  for cuts first.
- Q: When a low-level party attacks a major NPC far beyond it, is the party kept alive? → A: The first time, yes: the
  NPC stops them without killing them. A repeated attack after that "No, but…" is a real fight: the dice fall where
  they fall.
- Q: Where is a goal the character states recorded? → A: In the existing `story/dm-notes.md` notes, with no template
  change, so no migration is needed. (Superseded after the play test, below: the Arc plan's Character arcs, with a
  template change and a migration.)
- Q: How does DMC decide which NPCs count as "major"? → A: An NPC the main plot depends on: named as central in the
  module, or in the Arc plan or Opposition table of `story/dm-notes.md`.
- Q: Where does DMC record that a major NPC already gave the party its one "No, but…"? → A: A canon line in
  `story/canon.md`, with its scene number, per NPC (for example "X stopped the party's attack and spared them
  (s12.3)").
- Q: Does "Yes, and…" add a detail or hook to every choice, or only to bold or creative ones? → A: Only bold or
  creative choices; routine actions are resolved plainly.
- Q: Does DMC say "yes, and" or "no, but" to the player? → A: No. They are concepts, techniques DMC uses, never
  words it says. The rule names them as techniques, and its examples show the technique without the words.
- Q (from `/speckit-analyze`): Does the first-attack exception hold at `gritty` lethality, where "death is always
  possible"? → A: Yes. There is no general no-one-dies policy; the first attack on a major NPC is the one exception,
  at every lethality, once. After it, the dice fall where they fall, for both sides: the NPC fights at full strength
  and is not shielded either.
- Q: PR #220 grew `kit/dm-rules.md` by 61 bytes after the plan, so the wording needs a cap rise of 559, over the 500
  agreed. Cut words, or raise the allowance? → A: Raise the allowance to 560 bytes and keep every word.

### Session 2026-11-15, after the play test

- Q: "No, but" refused the hidden door and the cloak-flight with no way forward. How should line 166 change? → A:
  "turn it aside and offer, in the same reply, a way toward what they wanted": the offer comes in the same reply, and
  it serves what the player was after.
- Q: Line 165 names the Arc plan's Character arcs, which older campaigns lack and module campaigns keep only for after
  the module. Drop the section's name, or keep it? → A: Keep it, and add a migration that gives every campaign an
  `## Arc plan` section, so the goal always has its place.
- Q: When the player's idea rests on a fact the character would know is different (a position, what they carry), is
  line 153's out-of-character correction right, or should it be a "no, but" in the fiction? → A: Line 153's correction
  is right: the player made a mistake about what is in front of their character, and it is not a story refusal. The
  spec says so as an edge case, and SC-002 does not count such ideas.
- Q: The way-forward wording costs 34 bytes, so the cap rises by 593, over the 560 allowed. Cut, or raise? → A: Raise
  the allowance to 593 bytes.
- Q: After the second wording, the second play test gave SC-002 4 of 5 (with the first round's attack on Rorn): one
  ruled-out idea got a hook instead of a way forward, and the byte allowance is spent. Accept, rerun, or reword? → A:
  Accept 4 of 5 (2026-11-15) and keep the wording; whether "no, but" offers a way forward in real play is left to the
  after-release review (SC-005).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A bold choice that fits the fiction gets "Yes, and…" (Priority: P1)

The player tries something bold that the fiction allows. DMC lets it happen and adds a detail or hook that rewards it,
instead of only allowing it.

**Why this priority**: it is most of what the player does, and it is what makes a choice feel like it mattered.

**Independent Test**: in a headless play test, the player swings on a chandelier to reach a balcony; DMC's reply lets
it succeed (or rolls for it, as the rules require) and adds one new detail or hook the player can use.

**Acceptance Scenarios**:

1. **Given** a scene where the player's bold or creative action fits the fiction and the rules, **When** the player
   declares it, **Then** DMC resolves it as declared (§ The player decides for their character) and adds one detail
   or hook that builds on it.
2. **Given** a routine action (opening a door, buying rations), **When** the player declares it, **Then** DMC
   resolves it plainly and adds nothing.
3. **Given** an action that needs a roll, **When** the roll fails, **Then** the failure still moves the scene forward,
   and no shown die is changed.

---

### User Story 2 - A choice that would break the story gets "No, but…" in the fiction (Priority: P1)

The player tries something the world cannot allow: an impossible action, a fact that conflicts with established lore,
or a blow that would break the story. DMC refuses it inside the fiction and offers another way forward. It never says
out of character "that is not how the story works".

**Why this priority**: without it, DMC either says no flatly or lets one choice break the campaign, and both make the
game worse (Core 2).

**Independent Test**: in a headless play test, the level 1 party attacks a major NPC far beyond it; the NPC stops
them in the fiction, nobody's story ends, and the scene offers a way forward.

**Acceptance Scenarios**:

1. **Given** the player asks for something the established world rules out (a secret door behind a solid wall),
   **When** DMC answers in the fiction, **Then** it says no in the world's terms and, in the same reply, offers a way
   toward what the player wanted (a crawlspace near the floor, for a way in).
2. **Given** the party attacks a major NPC whose death would break the story, **When** DMC resolves it, **Then** the
   NPC stops them without being killed and without killing them, and the story turns the moment into something new.
3. **Given** the party has already had that "No, but…" from a major NPC, recorded as a canon line, **When** it
   attacks again, in that session or a later one, **Then** it is a real fight at the campaign's lethality: the NPC
   fights at full strength, is not shielded, and the dice fall where they fall for both sides.
4. **Given** the player asks out of character whether something is possible, **When** it is not, **Then** DMC answers
   frankly, as § Out of character says, and offers what is possible; it does not refuse with "that is not how the story
   works".
5. **Given** the player's idea rests on a fact the character would know is different (a tree that is not there, a
   potion the sheet does not hold), **When** DMC answers, **Then** it says so out of character before resolving, with
   the ways forward, as `kit/dm-rules.md` line 153 says. This is not a story refusal and not a "No, but…".

---

### User Story 3 - What the character wants becomes part of the main plot (Priority: P2)

The player says or shows that their character wants something (a weapon, a person found, a debt paid). DMC weaves it
into the main plot instead of leaving it aside.

**Why this priority**: it is what the maintainer liked in Rorn's Landing, but it happens over sessions, not in one
reply, and it is less often wrong today than a flat refusal.

**Independent Test**: after a session where the character states a goal, DMC's saved notes connect it to the main
plot, and a later scene moves it on.

**Acceptance Scenarios**:

1. **Given** the character states a goal in play, **When** DMC next saves, **Then** the goal is recorded in the Arc
   plan's Character arcs in `story/dm-notes.md`, with how it ties into the main plot, in a homebrew campaign and in a
   module campaign alike.
2. **Given** a recorded goal, **When** the main plot reaches a point where it fits, **Then** a scene moves the goal on.
   Checked after release (SC-005), because it takes sessions, not one reply.
3. **Given** a campaign whose `story/dm-notes.md` has no `## Arc plan` section, **When** the player brings its files up
   to date (`dmc-migrate-campaign`), **Then** it gains the section; an arc already planned under another heading moves
   into it unchanged, and Character arcs holds what the files already say the PC and each companion want.

---

### Edge Cases

- **A reckless choice that is not story-breaking** (jumping a chasm too wide): line 169 still applies; the danger is
  signalled once and the consequences land. "No, but…" is for what the world rules out or what would break the story,
  not for every risk.
- **A "No, but…" must not change a shown die** (§ Fun first, line 167) and must not undo a choice already resolved.
- **Repeated "No, but…" is railroading.** If most of the player's choices are being turned back, the story is not
  adapting; § Running a published module, line 115, still says "Do not railroad". Watched after release (SC-005).
- **A module campaign:** the story adapts within the module's world (Dungeon Master 3). Keeping the party inside it
  is issue #215, not this spec. While the module runs, its Arc plan holds Character arcs only; the rest of the plan
  (stages, mix, side quests and the others) is still for a homebrew campaign or after the module ends.
- **A wrong premise is not a "No, but…".** When the idea rests on a fact the character would know is different, line
  153's out-of-character correction comes first (User Story 2, scenario 5). "No, but…" is for what the world rules out
  that the character could not have known (no door behind the granite, a cloak that does not fly).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The play rules MUST tell DMC to answer a bold or creative choice that fits the fiction with "Yes,
  and…": resolve it as declared and add a detail or hook that builds on it. A routine action is resolved plainly,
  with nothing added. "Yes, and…" is a technique, named as one, never words DMC says to the player.
- **FR-002**: The play rules MUST tell DMC to answer a choice that the rules, physics or established lore rule out,
  or that would break the story, with "No, but…" in the fiction: refuse it in the world's terms and, in the same
  reply, offer a way toward what the player wanted. Like "Yes, and…", it is a technique, never words DMC says to the
  player. An idea that rests on a fact the character would know is different is not one of these: line 153's
  out-of-character correction stays as it is.
- **FR-003**: The play rules MUST tell DMC never to refuse out of character with "that is not how the story works";
  an out-of-character question is still answered frankly (§ Out of character).
- **FR-004**: The play rules MUST give a major NPC, one the main plot depends on (named as central in the module, or
  in the Arc plan or Opposition table of `story/dm-notes.md`), a "No, but…": the first time the party attacks, the
  NPC stops it without being killed and without killing them, and the story turns the moment into something new.
  This holds once per NPC, at every lethality, and is the only exception to the lethality setting; there is no
  general no-one-dies rule. DMC records it as a canon line in `story/canon.md` with its scene number, one per NPC. A
  repeated attack on an NPC with that line is a real fight: the dice fall where they fall, for both sides.
- **FR-005**: The play rules MUST tell DMC to weave a goal the character states into the main plot, and to note it in
  the Arc plan's Character arcs in `story/dm-notes.md` with how it ties in.
- **FR-005a**: The template `kit/templates/campaign/story/dm-notes.md` MUST say that Character arcs is kept in every
  campaign, a module campaign included, while the rest of the Arc plan stays for a homebrew campaign or after the
  module ends.
- **FR-005b**: A migration MUST bring every campaign's `story/dm-notes.md` to an `## Arc plan` section (Development 2):
  added from the template where it is missing; an arc already planned under another heading moved into it with its
  content unchanged; and Character arcs filled from what the campaign's files already say the PC and each companion
  want, inventing nothing. A campaign that already has the section keeps its content; only the template's comment is
  brought up to date (FR-005a). The migration names a shape check
  in `src/Dmc.Cli/Campaign/MigrationChecks.cs` that fails on a `dm-notes.md` with no `## Arc plan` section, and ships
  with a golden fixture and one live run, as `.docs/development.md` § Changing a campaign-file standard says.
- **FR-006**: The play rules MUST give one short example of "Yes, and…" and one of "No, but…".
- **FR-007**: Nothing in this change may contradict § Fun first (no shown die changed; recklessness lands) or
  § Running a published module ("Do not railroad"), except FR-004's one first-attack exception to the lethality
  setting and the recklessness rule.
- **FR-008**: `kit/dm-rules.md` MUST gain no line, and its byte cap in the toolkit test MAY rise by at most 593
  bytes (560 before the play test; the way-forward wording added 34); the definitions and examples go into existing
  bullets. The plan MUST look for cuts first, and MUST state the bytes added and the estimated tokens per turn (Core 3,
  Development 1). The test's comment MUST name this spec.
- **FR-009**: The documents MUST be updated in the same PR (Documentation 4): at least `.docs/decisions.md` and
  `.docs/roadmap.md`, the user guide if it describes how DMC answers the player, `.docs/campaign-files.md` for the
  Arc plan, and any document that lists the migrations or their checks.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a headless play test of 5 bold or creative choices that fit the fiction, every reply resolves the
  choice and adds a detail or hook (5 of 5); of 3 routine actions in the same test, none gets an added hook (0 of 3).
- **SC-002**: In a headless play test of 5 choices the world rules out, including one attack on a major NPC, every
  reply refuses in the fiction and, in the same reply, offers a way toward what the player wanted (5 of 5), and none
  says out of character that the story does not allow it (0 of 5). The first attack on the major NPC leaves the party
  alive (1 of 1). An idea that rests on a fact the character would know is different is not one of the 5: it gets
  line 153's correction (User Story 2, scenario 5), and the test replaces it with another.
- **SC-003**: In the same tests, no reply changes a shown die or undoes a resolved choice, and no reply says the
  words "yes, and" or "no, but" to the player (0 of all replies). Replies are judged by what they do, not by the
  phrase.
- **SC-004**: `kit/dm-rules.md` stays at 212 lines, its byte cap rises by at most 593 bytes, and the toolkit test
  passes.
- **SC-006**: The migration's golden fixture passes on CI (`MigrationGoldenTests`, `MigrationFilesTests`, and the new
  check's test), and one live run on its `before/` campaign leaves what `after/` holds: a `## Arc plan` section with
  Character arcs, an arc planned under another heading moved in unchanged, and nothing lost (`dmc campaign verify`
  passes). After it, a goal stated in play lands in Character arcs (User Story 3, scenario 1).
- **SC-005**: After release, a `review-transcript` review of the first 3 play sessions finds no flat out-of-character
  refusal of a player's choice, and, for each goal the character stated in them, a later scene that moves it on (or
  says the main plot has not yet reached a point where it fits). The review counts the "No, but…" and "Yes, and…"
  answers in each session, judged by what they do, not by their words, and flags a session where "No, but…" answers
  outnumber "Yes, and…" ones, as railroading.

## Assumptions

- The rule lives in `kit/dm-rules.md`, because it applies on any turn of any campaign; a skill loads too rarely.
- The examples are the maintainer's (chandelier, crawlspace), not the campaign examples, which are personal and
  git-ignored.
- The maintainer's note that D&D Beyond says improv's "yes, and" can clash with continuity is unverified, and the
  spec does not rest on it.
- Headless play tests run in `D:\Claude\test-campaign`, as earlier specs did.
- The migration's number is the next after the highest in `kit/migrations/` when the plan is made (007 at
  `adb0e07`); a PR that adds another first would take it.
- The full save's step that adds a missing Arc plan for a homebrew campaign
  (`kit/skills/dmc-campaign-full-save/SKILL.md`) stays; after the migration it finds the section present.
