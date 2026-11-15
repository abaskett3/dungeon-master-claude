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

`kit/dm-rules.md` (212 lines, 29,856 bytes on `main` at `5b0e7a8`) has pieces of this, but no rule for answering a
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
FR-028). The file is at 212 lines and 29,856 bytes, so 187 bytes and no line are left. A definition and an example of
each does not fit in 187 bytes.

## Clarifications

### Session 2026-11-15

- Q: How should the rule fit `kit/dm-rules.md`'s budget (212 lines, 187 bytes left)? → A: Raise the byte cap by
  about 500 bytes, with no new line: the definitions and both examples go into existing bullets, and the plan looks
  for cuts first.
- Q: When a low-level party attacks a major NPC far beyond it, is the party kept alive? → A: The first time, yes: the
  NPC stops them without killing them. A repeated attack after that "No, but…" is a real fight: the dice fall where
  they fall.
- Q: Where is a goal the character states recorded? → A: In the existing `story/dm-notes.md` notes, with no template
  change, so no migration is needed.
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
   **When** DMC answers in the fiction, **Then** it says no in the world's terms and offers an alternative in the same
   reply (a crawlspace near the floor).
2. **Given** the party attacks a major NPC whose death would break the story, **When** DMC resolves it, **Then** the
   NPC stops them without being killed and without killing them, and the story turns the moment into something new.
3. **Given** the party has already had that "No, but…" from a major NPC, recorded as a canon line, **When** it
   attacks again, in that session or a later one, **Then** it is a real fight at the campaign's lethality: the NPC
   fights at full strength, is not shielded, and the dice fall where they fall for both sides.
4. **Given** the player asks out of character whether something is possible, **When** it is not, **Then** DMC answers
   frankly, as § Out of character says, and offers what is possible; it does not refuse with "that is not how the story
   works".

---

### User Story 3 - What the character wants becomes part of the main plot (Priority: P2)

The player says or shows that their character wants something (a weapon, a person found, a debt paid). DMC weaves it
into the main plot instead of leaving it aside.

**Why this priority**: it is what the maintainer liked in Rorn's Landing, but it happens over sessions, not in one
reply, and it is less often wrong today than a flat refusal.

**Independent Test**: after a session where the character states a goal, DMC's saved notes connect it to the main
plot, and a later scene moves it on.

**Acceptance Scenarios**:

1. **Given** the character states a goal in play, **When** DMC next saves, **Then** the goal is recorded in the
   existing `story/dm-notes.md` notes, with how it ties into the main plot.
2. **Given** a recorded goal, **When** the main plot reaches a point where it fits, **Then** a scene moves the goal on.
   Checked after release (SC-005), because it takes sessions, not one reply.

---

### Edge Cases

- **A reckless choice that is not story-breaking** (jumping a chasm too wide): line 169 still applies; the danger is
  signalled once and the consequences land. "No, but…" is for what the world rules out or what would break the story,
  not for every risk.
- **A "No, but…" must not change a shown die** (§ Fun first, line 167) and must not undo a choice already resolved.
- **Repeated "No, but…" is railroading.** If most of the player's choices are being turned back, the story is not
  adapting; § Running a published module, line 115, still says "Do not railroad". Watched after release (SC-005).
- **A module campaign:** the story adapts within the module's world (Dungeon Master 3). Keeping the party inside it
  is issue #215, not this spec.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The play rules MUST tell DMC to answer a bold or creative choice that fits the fiction with "Yes,
  and…": resolve it as declared and add a detail or hook that builds on it. A routine action is resolved plainly,
  with nothing added. "Yes, and…" is a technique, named as one, never words DMC says to the player.
- **FR-002**: The play rules MUST tell DMC to answer a choice that the rules, physics or established lore rule out,
  or that would break the story, with "No, but…" in the fiction: refuse it in the world's terms and offer another way
  forward in the same reply. Like "Yes, and…", it is a technique, never words DMC says to the player.
- **FR-003**: The play rules MUST tell DMC never to refuse out of character with "that is not how the story works";
  an out-of-character question is still answered frankly (§ Out of character).
- **FR-004**: The play rules MUST give a major NPC, one the main plot depends on (named as central in the module, or
  in the Arc plan or Opposition table of `story/dm-notes.md`), a "No, but…": the first time the party attacks, the
  NPC stops it without being killed and without killing them, and the story turns the moment into something new.
  This holds once per NPC, at every lethality, and is the only exception to the lethality setting; there is no
  general no-one-dies rule. DMC records it as a canon line in `story/canon.md` with its scene number, one per NPC. A
  repeated attack on an NPC with that line is a real fight: the dice fall where they fall, for both sides.
- **FR-005**: The play rules MUST tell DMC to weave a goal the character states into the main plot, and to note it in
  `story/dm-notes.md` with how it ties in. No template changes, so no migration is needed (Development 2).
- **FR-006**: The play rules MUST give one short example of "Yes, and…" and one of "No, but…".
- **FR-007**: Nothing in this change may contradict § Fun first (no shown die changed; recklessness lands) or
  § Running a published module ("Do not railroad"), except FR-004's one first-attack exception to the lethality
  setting and the recklessness rule.
- **FR-008**: `kit/dm-rules.md` MUST gain no line, and its byte cap in the toolkit test MAY rise by at most 500
  bytes; the definitions and examples go into existing bullets. The plan MUST look for cuts first, and MUST state the
  bytes added and the estimated tokens per turn (Core 3, Development 1). The test's comment MUST name this spec.
- **FR-009**: The documents MUST be updated in the same PR (Documentation 4): at least `.docs/decisions.md` and
  `.docs/roadmap.md`, and the user guide if it describes how DMC answers the player.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a headless play test of 5 bold or creative choices that fit the fiction, every reply resolves the
  choice and adds a detail or hook (5 of 5); of 3 routine actions in the same test, none gets an added hook (0 of 3).
- **SC-002**: In a headless play test of 5 choices the world rules out, including one attack on a major NPC, every
  reply refuses in the fiction and offers a way forward (5 of 5), and none says out of character that the story does
  not allow it (0 of 5). The first attack on the major NPC leaves the party alive (1 of 1).
- **SC-003**: In the same tests, no reply changes a shown die or undoes a resolved choice, and no reply says the
  words "yes, and" or "no, but" to the player (0 of all replies). Replies are judged by what they do, not by the
  phrase.
- **SC-004**: `kit/dm-rules.md` stays at 212 lines and grows by at most 500 bytes, and the toolkit test passes.
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
