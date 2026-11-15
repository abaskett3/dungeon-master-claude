# Feature Specification: A Slimmer Resume (Companion Play Cards, Canon by Scene, Hook Recall and the Gate)

**Feature Branch**: `013-slimmer-resume`

**Created**: 2026-11-15

**Status**: Draft

**Input**: User description: "Issue #179, Phase D: steps 8, 9 and 10. Step 8, slimmer resume: a short play card per
companion, with the full sheet read on demand (about 6k to 8k saved), and canon read as Rulings, Prices and the
scene's matches (about 10k saved). Step 9 (lowest priority), hook recall as an assist: the UserPromptSubmit hook
injects canon lines for names in the player's message, once per session, supported by a small names-and-roles
register. Step 10, the gate: define it and how it is measured. Phase D needs a migration: name it and say what it
changes. Ship everything now, including the canon-reading change; the gate becomes a check after release, a
`review-transcript` review of the first 3 play sessions after release against the sessions before Phase D, and if slips
rise, canon goes back to being read whole. Measure and report the standing token cost of every prompt change and keep
`kit/dm-rules.md` growth to the minimum. Add no fallback for a missing `dmc`; do not touch issue #208 or #198 option A."

## Background

Issue #179 is a tracking issue in four phases; this spec is Phase D, its steps 8 to 10. Phase A shipped `dmc lookup` and
`dmc recall` for play only, and its decision "Lookup and recall are for play only until Phase D" kept the resume and the
full save as they were, behind a gate: canon reading would change only after `review-transcript` showed no rise in
slips with recall and lookup in use. Phase B shipped continuity links, the sequel import and legacy files. Phase C
changed one read of the resume (a module's area, by its heading) and amended that decision to say so; everything else
the resume reads waited for this phase.

### What a resume of Rorn's Landing reads today

Measured on 2026-11-15, read-only, on the maintainer's play folder (`D:\Claude\dmc`, Rorn's Landing at standard 5,
session 54). Tokens are bytes ÷ 4, the method of `dmc size`, so they are estimates. "dmc size" is
`dmc size --folder D:/Claude/dmc rorns-landing` from the repository build; the per-section figures were counted by a
script over the same files and agree with it within 0.1k a file.

| Read at resume | Issue (2026-11-10) | Today | How measured |
|---|---|---|---|
| Whole resume | about 76k | **69.5k** | `dmc size` total |
| Four companion sheets (Kibil, Kole, Pip, Ixenna), down to History | 12.4k | **15.2k** (dmc size: 3.8k + 4.1k + 3.7k + 3.9k = 15.5k) | bytes before `## History` ÷ 4 |
| Other NPC sheets read because the Present line names them (Hala, Tam, Dessa, Wenna, Brunna) | not measured | **9.9k** (dmc size 10.1k) | same |
| PC sheet, down to History | 5.4k | 5.2k | same |
| `story/canon.md`, read whole under its 19k limit | 15.5k | **18.4k** (dmc size 18.5k) | whole file ÷ 4 |

Canon's sections today: Rulings 2.9k (47 lines), Prices, debts and promises 2.5k (37), Timeline 5.6k (61), People 2.6k
(31), Factions and powers 1.9k (4), History and lore 1.4k (18), Places 0.9k (9), Items of note 0.2k (6). The two
always-read sections together are 5.4k; the rest, with the file's opening text, is 13.0k.

Within a companion sheet, the parts a resume reads now and a play card would leave out are large: "What they know" is
0.9k to 2.0k a sheet, "Playing them" 0.6k to 1.5k (mostly sample lines of speech), the stat block 0.5k to 0.7k.

### What the issue's savings come to, measured

- **Play cards.** The issue's 6k to 8k was reckoned on 12.4k. The four sheets now cost 15.2k. With a card of at most
  1k each (FR-003), the four cost at most 4k: a saving of **at least 11.2k**.
- **Canon by scene.** The issue's 10k does **not** hold under the resume's present search rule. That rule searches the
  rest of canon for the names, places and threads in `game-state.md`'s Scene block, Active quests and Pending lines. In
  Rorn's Landing those names are the party and the places of every recent session, so the search matches **142 of the
  rest's lines, 12.3k of its 13.0k**. Rulings and Prices (5.4k) plus that match read 17.7k against 18.4k whole: a
  saving of about **0.7k**. Leaving the PC's and companions' names out of the search (they are on the cards) still
  matches 9.5k (saving 3.5k); searching only the scene's place, the two people at the ring (Hobb, Bruni) and the active
  quests' own names, leaving out Hala and the Birches, matches 4.6k (saving 8.4k). Only a bound on the matches reaches the issue's 10k: with the
  matches ranked and cut at 3k, canon costs 8.4k, a saving of **10.0k** (FR-008, a question for the user). These figures
  come from a case-insensitive search of canon outside the two always-read sections, with the distinctive words of
  `game-state.md`'s Scene block, Active quests and Waiting threads as on 2026-11-15 (all terms: high road, Hobb, grey
  stone, Tam, Hala, Kibil, Bruni, Ember, Shale, Varunhal, Kole, Pip, Ixenna, Dessa, Wenna, Landing, Fireball, Gloam,
  Greywall, blight, Mossmere, Hush, Elowen, Warden, tower, Kelderak, Teague, Draconic, Rorn, Riverhold, Orla, Erros,
  Peg Alder; the narrowest set: high road, Hobb, grey stone, Bruni, Ember, Gloam, Greywall, Warden, Teague). They
  estimate what a resume's search would return, not a run of the resume, and were not checked against a real resume.
- **Together**, with cards at 1k and canon's matches cut at 3k: the resume falls from 69.5k to about **48k**. Without
  the bound on canon it falls to about 58k.

### What has already been checked

- No evidence for the gate exists yet (verified by the user): the play folder has no `adventures/.reviewed-sessions`,
  no transcript review has been recorded, and no real play session has called `dmc recall` or `dmc lookup` (only
  `ba576cc7`, a sheet-building chat, did). The play folder's rules have named `dmc recall` since its refresh of
  2026-11-14; the sessions before that did not have it.
- `review-transcript` (`.claude/skills/review-transcript/SKILL.md`) looks for consistency problems and records them as
  feedback entries, but keeps no count of slips per session. The decision "Size rules are measured in tokens by
  `dmc size`" already says the budgets are "to be tuned against `review-transcript`'s slip counts", which do not exist.
- `kit/dm-rules.md` is 212 lines and 29,643 bytes (about 7.4k tokens), loaded into every play session. A toolkit test
  (`TheAlwaysLoadedRulesGrowByAtMostTwoLinesForLookupAndRecall`) allows it 214 lines.
- The `UserPromptSubmit` hook in `kit/settings.json` runs `dmc context-check`, which reads the hook's JSON, prints a
  line only when the context is large, and never fails.
- The highest migration is `kit/migrations/005-continuity-links.md`, so Phase D's is **006**.

## Clarifications

### Session 2026-11-15

Settled by the user before the spec was written:

- Q: What evidence is there for the gate? → A: None (see Background). The gate cannot be checked before the change.
- Q: Ship part of Phase D and hold the canon change behind the gate, or ship it all? → A: Ship everything now,
  including the canon-reading change.
- Q: Then what is the gate? → A: A check after release: a `review-transcript` review of the first 3 play sessions after
  release, compared with the sessions before Phase D. If slips rise, canon goes back to being read whole. Because the
  gate is checked after the merge, Phase D's PR says `Part of #179`, and the issue stays open until the gate check is
  recorded (FR-031).
- Q: Must play work without `dmc`? → A: No. DMC requires `dmc`; this spec adds no fallback for a missing `dmc`.
  Retiring "play never depends on `dmc`" is issue #208, and #198 option A is its own change; neither is touched here.
  A search that finds nothing still falls back to reading: that is `dmc` finding nothing, not `dmc` missing.

Settled in `/speckit-clarify`:

- Q: How are canon's scene matches limited (FR-008)? → A: Ranked as `dmc recall` ranks them, restricted to canon,
  and cut at 3k tokens (option c, about 10.0k saved). The subagent's 12.3k-of-13.0k match figure is an estimate from a
  search with the listed terms, not a measured resume.
- Q: Where is the slip count taken from? → A: From the campaign files, which already record slips ("DMC's slip, fixed
  in play", "(s41.1 slip)", "corrected s39.1 … was DMC's error"), each checked against its transcript. From Phase D on,
  DMC writes each slip in the session log as one line in one shape (FR-033), so later counts need no reading by hand.
- Q: What is the baseline? → A: Every session log of The Silver Egg (46) and Rorn's Landing (54) before Phase D, as
  slips per session in each category. The after set is the first 3 qualifying sessions after Phase D (FR-022).
- Q: When has a category risen? → A: When its average per session after is higher than the baseline's. No margin.
- Q: Where does the names-and-roles register live? → A: Inside canon's People entries: the bold name, then the other
  names and role words in brackets, for example `**Captain Emmajeen Kole** (Kole, Em; the captain)`.
- Q: Play cards for NPCs who are not companions? → A: Companions only, as the issue says.
- Q: What rolls back if a category rises? → A: That category only: canon is read as before Phase D; companions are read
  down to their history again (the cards stay on the sheets); the hook stops injecting canon lines. No migration.
- Q: The card budget, the closing PR, and recall's effect on the comparison? → A: Accepted as the Assumptions state
  them: 1k a card; the gate-record PR closes #179 even after a rollback; the comparison cannot separate Phase D from
  recall.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A companion costs a play card at resume, and its full sheet is read when play needs it (Priority: P1)

The player resumes Rorn's Landing, with four companions in the party. For each, DMC reads a short play card at the top
of the sheet: who they are, how they look and talk, the names they use, how they stand with the PC and what they want
now, and the numbers a fight needs. It does not read what they know, their relationships or their full sample lines.
When a companion is asked about the past, speaks at length, or needs a number or feature the card does not hold, DMC
reads that companion's full sheet first, as it already does before a recurring NPC speaks at length.

**Why this priority**: It is the largest saving that does not depend on a search: at least 11.2k on the measured
campaign, every resume. It changes the party's sheets, which the migration (User Story 2) brings to existing campaigns.

**Independent Test**: Make a campaign from the templates with a PC and two companions whose sheets have a play card,
and run `dmc size` on it. Pass: each companion's line is the card only, within its budget; the PC's sheet is read down
to its history as today; and a companion sheet with no card is read down to its history and flagged.

**Acceptance Scenarios**:

1. **Given** a companion whose sheet has a play card, **When** the campaign is resumed, **Then** DMC reads the sheet
   from the top through the card and no further, and `dmc size` measures the same part.
2. **Given** the player asks a companion what they saw last month, **When** DMC answers in that companion's voice,
   **Then** it has first read the companion's full sheet (and searched for the subject), so the answer holds only what
   the companion witnessed or was told.
3. **Given** a fight starts, **When** a companion acts, **Then** its AC, HP, attacks, saves and standing traits come
   from the card, and a feature not on the card is read from the sheet before it is used.
4. **Given** a character becomes a companion in play, **When** their sheet is next written, **Then** it gains a card;
   until then the resume reads that sheet down to its history, as today.
5. **Given** a companion levels up or a card fact changes, **When** the sheet is updated, **Then** the card is updated in
   the same edit and the full save checks it against `game-state.md`.

---

### User Story 2 - An existing campaign gets its cards through migration 006 (Priority: P1)

The player updates DMC. At the next resume DMC says the campaign's files are behind and offers to bring them up to date.
On a yes, `dmc-migrate-campaign` backs the campaign up, follows migration 006, writes a play card on each companion's
sheet from what the sheet already says (and the register's shape in canon, if the register is chosen to live there),
and `dmc campaign verify` proves that nothing was lost.

**Why this priority**: Without it the saving reaches only new campaigns; the measured campaign is 54 sessions in.

**Independent Test**: The golden fixture for 006: a campaign at standard 5 with companion sheets in the old shape;
`after/` with their cards. Pass: `verify` passes on `after/`, fails with a line removed, and the shape check fails on a
companion sheet left without a card.

**Acceptance Scenarios**:

1. **Given** a campaign at standard 5, **When** the player accepts the migration, **Then** each companion named under
   Party status gets a card built only from its own sheet, and no line of the old files is lost.
2. **Given** a sheet whose facts do not fill a card field, **When** the migration writes the card, **Then** the field
   says `none` or is left for play, and nothing is invented.
3. **Given** a closed campaign, **When** migrations are offered, **Then** it is never migrated (as today).

---

### User Story 3 - Canon is read as Rulings, Prices and the scene's matches (Priority: P1)

At resume DMC reads canon's Rulings and Prices, debts and promises whole, and any section marked "Always read", then
the canon lines that match the scene it is about to run, and nothing else of canon. When a subject comes up later that
it has not read, it recalls or searches for it before stating anything, as the rules already say.

**Why this priority**: The user chose to ship it now. It is the second large saving, but only with a bound on the
matches (FR-008): under the present search rule it saves about 0.7k on the measured campaign.

**Independent Test**: A campaign whose canon has the always-read sections, a scene naming two people and a place, and
other lines that name none of them. Pass: `dmc size` reports the always-read sections plus the matching lines (within
the bound, if one is set), and its total for canon equals what the resume step reads.

**Acceptance Scenarios**:

1. **Given** a canon of any size above the floor in FR-007, **When** the campaign is resumed, **Then** DMC reads the
   always-read sections whole and the scene's matches, and not the rest.
2. **Given** a ruling or a debt the current scene does not mention, **When** the campaign is resumed, **Then** it is
   still read, because it is in an always-read section.
3. **Given** the party meets someone the scene did not name, **When** that person speaks, **Then** DMC has recalled or
   searched for them first (`kit/dm-rules.md` § Accuracy).
4. **Given** a small canon below the floor, **When** the campaign is resumed, **Then** it is read whole.

---

### User Story 4 - The gate is checked after release, and a rise in slips rolls the change back (Priority: P2)

After the play folder runs Phase D and the campaign has been migrated, the player plays as usual. Once three
qualifying play sessions have been played, the maintainer runs `review-transcript` over them, counts the slips their
session logs record (checked against the transcripts) by what caused them, compares each category's average per
session with the baseline counted from every earlier session log of The Silver Egg and Rorn's Landing, and records the
result on issue #179. If a category rose, that part of Phase D is rolled back.

**Why this priority**: It is the check the issue sets for changing what a resume reads. It runs after the merge, so it
does not block shipping, but issue #179 stays open until it is recorded.

**Independent Test**: Follow the gate's procedure with three recorded sessions of Rorn's Landing standing in for the
after set. Pass: two people following it count the same slips in the same categories, and the procedure names the
result and the action without a further decision.

**Acceptance Scenarios**:

1. **Given** three qualifying sessions after Phase D, **When** the gate is run, **Then** it reports slips per session
   for each category, baseline and after, and whether the after average is higher.
2. **Given** canon slips rose past the threshold, **When** the result is recorded, **Then** a change restores canon
   reading as it was before Phase D, with no migration.
3. **Given** no category rose, **When** the result is recorded, **Then** the PR that records it closes issue #179.

---

### User Story 5 - The hook reminds DMC of canon for names the player uses (Priority: P3)

The player writes "We should ask Bruni about the housing." Before DMC answers, the prompt hook adds the canon lines
about Bruni that the resume did not read, once for this session, with where each came from. The next message that
names Bruni adds nothing.

**Why this priority**: The issue ranks it lowest. A hook that matches the player's raw words found the answer about 75%
to 80% of the time in the issue's recall test, so it assists the rules and does not replace them.

**Independent Test**: Feed the hook a prompt naming two people in a test campaign's register, then the same prompt
again. Pass: the first prints their canon lines within the cap, the second prints nothing, and a prompt with no
registered name prints nothing.

**Acceptance Scenarios**:

1. **Given** a message naming someone in the register for the first time this session, **When** it is sent, **Then**
   the hook adds that person's canon lines, from the sections the resume does not read whole and from the canon
   archive, within the cap, each with its file and session.
2. **Given** that name again later in the session, **When** a message names them, **Then** the hook adds nothing for
   them.
3. **Given** no active campaign, a closed campaign or no registered name, **When** a message is sent, **Then** the hook
   adds nothing, and the context-size note works as today.
4. **Given** `dmc` fails inside the hook, **When** a message is sent, **Then** the message goes through as normal and
   nothing is printed.

---

### Edge Cases

- **A companion sheet with its history in the middle** (sections after it): the card is still the first section, and
  the resume reads only the card.
- **A Party status block that is not a character** (Rorn's Landing has `### The Kelderak treasure (s48.3)` there):
  it has no sheet, so it gets no card; the migration names it and writes nothing for it.
- **A companion built on the PC template** (`templates/pc-sheet.md` allows "Companion"): it gets a card too.
- **A companion who leaves the party**: the card stays; the sheet is read at resume only by the rules for NPCs present,
  and then down to its history (FR-006a).
- **"With the party, not companions"** (Hala in Rorn's Landing): not a companion, so no card (FR-006a); read by
  the rule for NPCs present.
- **A card over its budget**: `dmc size --check` flags it for the full save, as for any budget.
- **A scene that names no one** (a lone journey): the search uses the place and the active quests; the always-read
  sections are read as always.
- **A search pattern rejected** (a name with special characters): the terms are searched one at a time, as today.
- **A standing fact outside the always-read sections** (in Rorn's Landing, "Hala is not immune to cold" lives in
  People and on Pending): it is read only when its subject matches the scene. Facts that must hold in every scene belong
  in Rulings, on a card or in Pending; the full save says so (FR-010).
- **A name shared by two people, or a common word as a name** ("Hush", "Peg"): the hook prints both people's lines
  within the cap, and the register's words are matched whole, not inside other words.
- **Compaction mid-session**: the injected lines were compacted away, so a name is injected again after the compaction
  boundary.
- **Fewer than three qualifying sessions for a long time**: the gate waits; issue #179 stays open.
- **A slip that is about a fact the files never held**: not counted by the gate; it is a bookkeeping finding for
  `review-transcript` as today.

## Requirements *(mandatory)*

### Functional Requirements

**Play cards (step 8)**

- **FR-001**: Each companion's sheet MUST begin with a play card: a section placed first on the sheet, holding what DMC
  needs to play the companion in an ordinary scene and in a fight: who they are in a line; appearance in short; voice
  in a line with one sample; the names they use; attitude to the PC and what they want now; AC, max HP, speed, saves,
  senses, each attack (to hit, damage, reach or range, anything it needs), recharge abilities; standing traits that
  change play (an immunity, acting on the PC's turn, who rolls their dice); and the items they use in play. It MUST
  hold whatever the resume's state check (section 3) compares with `game-state.md`.
- **FR-002**: Each number on the card (AC, HP, attack bonuses, DCs) MUST be the sheet's only copy of it; the rest of the
  sheet keeps what the card does not hold. A card line that shortens a longer line elsewhere on the sheet MUST agree
  with it.
- **FR-003**: The part of a companion's sheet the resume reads (the top of the sheet through the card) MUST have a
  budget in `dmc size`, 1k tokens by default (Assumptions), flagged when over like every budget.
- **FR-004**: The resume MUST read each companion's sheet through its card and no further. A companion sheet with no
  card MUST be read down to its history, as today, and `dmc size --check` MUST name it for the next full save.
- **FR-005**: DMC MUST read a companion's full sheet before: the companion answers about the past or about what they
  know; speaks at length; uses a feature, spell or number not on the card; levels up or has the sheet changed. The rule
  lives in the skills that already govern those moments; `kit/dm-rules.md`'s existing line "Before a recurring NPC
  speaks at length, reread their sheet's Voice and Names they use lines" may be reworded but MUST NOT become more lines.
- **FR-006**: `dmc-character-sheet-update` MUST write a card when a character becomes a companion and keep it current
  in the same edit as the fact it holds; the full save's sheet step MUST keep each card within its budget, and its
  consistency check MUST compare the cards' HP and items with `game-state.md`, as it does the sheets today.
- **FR-006a**: Play cards are for companions only. Every other sheet the resume reads (an NPC the Present line or the
  combat tracker names, someone "With the party, not companions" who is present) is read down to its history as today,
  and so is the PC's sheet.

**Canon by scene (step 8)**

- **FR-007**: Above a small floor (at most 6k tokens, set with the other budgets), the resume MUST read canon as: the
  sections whose omission causes contradictions (Rulings; Prices, debts and promises; any section whose comment says
  "Always read"), whole; then the scene's matches (FR-008); and nothing else of canon. Below the floor canon is read
  whole. The 19k limit and the 16k archive mark stay as the full save's archive rule.
- **FR-008**: The scene's matches MUST be the canon lines outside the always-read sections that match the present
  search terms (the Scene block, Active quests and Pending), ranked as `dmc recall` ranks them, restricted to canon,
  and cut at 3k tokens. (Unbounded, the present terms match an estimated 12.3k of the 13.0k searched in Rorn's Landing
  and save about 0.7k; the cut saves about 10.0k.) DMC MUST search again whenever a subject comes up that it has not
  read, as resume step 7 and `kit/dm-rules.md` § Accuracy already say.
- **FR-009**: `dmc size` MUST measure canon by the same rule the resume uses, including the scene's matches, so its
  total is what a resume reads; today it counts only the always-read sections when canon is over its limit.
- **FR-010**: The full save MUST say, in its canon step, that a fact which must hold in every scene (a standing ruling,
  an immunity, a house rule) belongs in an always-read section, on a card or under Pending, not only in a section that
  is searched; it MUST NOT move existing lines between sections to do so outside migration 006 or the player's word.

**Migration 006**

- **FR-011**: The change MUST ship migration `kit/migrations/006-<slug>.md`, standard 6, which writes a play card on
  each companion's sheet (each `### ` block under `game-state.md`'s Party status other than "With the party, not
  companions") from that sheet's own lines, and brings canon's People entries to the register's shape (FR-018). It changes nothing else in the campaign. Its `touches` are `character-sheets/`,
  `story/canon.md` and `story/canon-archive.md`.
- **FR-012**: Migration 006 MUST name a shape check that fails on a companion sheet with no card and one that fails on a
  People entry not in the register's shape, MUST declare no drops (it moves and shortens, and accounts each moved or
  reworded line as `reworded` with an anchor), and MUST have a golden fixture, as `.docs/development.md` § Changing a
  campaign-file standard says.
- **FR-013**: Migration 006 MUST build a card only from the companion's sheet and the campaign's own files, write
  `none` for a field nothing fills, and invent nothing; a sheet whose shape it does not recognise is skipped and
  named, as the migrate skill says.
- **FR-014**: The canon-reading change and the hook MUST need no migration: canon's sections do not change (the
  People entries' shape is migration 006's), and a campaign at standard 5 still resumes correctly, reading uncarded companions
  down to their history.

**Hook recall and the register (step 9)**

- **FR-015**: The `UserPromptSubmit` hook MUST, in the same hook that checks the context size, add the canon lines for
  each registered name in the player's message that has not been injected yet in this session part (since the start of
  the log or its latest compaction boundary, the same boundary `dmc context-check` uses). Lines come from canon's
  sections that the resume does not read whole, then from `story/canon-archive.md`; never from `dm-notes.md`, sheets,
  digests, other campaigns or modules.
- **FR-016**: The hook's output MUST be small: at most 3 lines a name and 4,000 bytes (about 1k tokens) a message, each
  line with its file and session; nothing at all when no name is new, when there is no active campaign, when the
  campaign is closed, or when the message is a command. It MUST be labelled as a reminder of what canon says, so DMC
  does not take it as all there is about the subject.
- **FR-017**: The hook MUST never fail or block a message: any failure prints nothing and exits as `dmc context-check`
  does today. It MUST add no noticeable delay (SC-007).
- **FR-018**: The names-and-roles register MUST be a convention inside canon's People entries: each entry starts with
  the person's bold name, followed in brackets by the other names and role words the player may use, for example
  `**Captain Emmajeen Kole** (Kole, Em; the captain)`. No new file. A name is matched whole and case-insensitive;
  each word of a bold name counts, except words in a short list of titles and common words.
- **FR-019**: Whoever writes a People entry (the play rules' "add one line to canon right away", the sheet update for a
  new character, the full save's canon step) MUST write it in the register's shape, without a new line in
  `kit/dm-rules.md`.

**The gate (step 10)**

- **FR-020 (what a slip is)**: A slip is a statement DMC made in play (narration, an NPC's speech, a recap, an
  out-of-character answer, a number used in a roll or a ruling) that contradicts what the campaign files held at that
  moment, or that shows DMC did not know a fact the files held (it asked the player for it, or stated something else
  over it). The same wrong fact counts once per reply and again in a later reply. Not slips: a fact the files did not
  hold, the player's taste (`review-transcript` section 5), a slip DMC corrected within the same reply, and dice or
  arithmetic errors that are not about a recorded fact.
- **FR-021 (categories)**: Each slip MUST be put in one category: **canon** (the fact was in `canon.md` or its archive,
  outside the always-read sections); **companion** (the fact was on a companion's sheet); **hook** (an injected line
  was wrong, out of date or misled DMC); **other** (anything else, including Rulings and Prices). Canon, companion and
  hook are the gate's categories; other is recorded but does not decide the gate.
- **FR-022 (the sessions)**: The **baseline** is every session log, before Phase D, of The Silver Egg (46 logs) and
  Rorn's Landing (54 logs as of 2026-11-15), the slips counted from what those logs and the campaign's other files
  record (FR-023). The **after** set is the first 3 qualifying play sessions of the measured campaign (Rorn's Landing,
  or the active campaign if it has changed) after migration 006 passed on it: a session that starts with a resume, has
  at least 10 player messages, and is mostly play, not an import, sheet building, a backup or a migration. A session is
  one `story/sessions/session-NNN.md`.
- **FR-023 (the comparison)**: A slip is counted where the campaign files record it: the session log, a sheet's
  correction note, canon's `(changed sN: …)` note, the notes about the player in `dm-notes.md`. Before Phase D these use
  mixed wording ("DMC's slip, fixed in play", "(s41.1 slip)", "corrected s39.1 … was DMC's error"), read by hand once
  for the baseline; from Phase D on, the session log's `Slip:` lines (FR-033). Each counted slip in the after set MUST
  be checked against its transcript (`dmc digest show`) for what happened and its category; a slip the transcript shows
  that the files never recorded is reported with the result but does not decide the gate, since the baseline could not
  count such slips. The result is a table: category, baseline slips and sessions, after slips and sessions, average per
  session for each.
- **FR-024 (the threshold)**: A category has risen when its average slips per session in the after set is higher than
  its baseline average. There is no margin. The hook category has a baseline of zero, so any hook slip is a rise.
- **FR-025 (who and when)**: The maintainer runs the gate in this repository with `review-transcript`, once the third
  after-session has been played, and records the table and the verdict as a comment on issue #179 and in
  `.docs/decisions.md` and `.docs/roadmap.md`.
- **FR-026 (the rollback)**: If a category rises, only that category is rolled back: **canon**, canon is read as before
  Phase D (whole under its limit, the always-read sections and the scene search above it); **companion**, companions are
  read down to their history again, and the cards stay on the sheets; **hook**, the hook stops injecting canon lines and
  keeps its context-size note. A rollback MUST need no migration: the cards and the register stay valid campaign
  content, and the standard stays 6. Recall and lookup in play (Phase A) are not rolled back.
- **FR-027**: `review-transcript` MUST gain what the gate needs to be repeatable: a tally of slips per session by the
  categories of FR-021, taken from the files as FR-023 says and checked against the transcripts, written with the
  review's report. This is toolkit work and costs play nothing.
- **FR-033 (the slip line)**: When DMC owns a slip in play (the player corrects it, or DMC finds it), it MUST write one
  line in the current session log's scene, `Slip: <what was wrong, and the right fact> (canon|companion|hook|other)`,
  in the category of FR-021. The shape lives in the session log template; the rule that triggers it is the existing
  "own a slip" practice and MUST NOT add a line to `kit/dm-rules.md` (it may extend an existing one within FR-028's
  bytes). The standing cost is about 30 tokens.

**Cost and documents**

- **FR-028**: `kit/dm-rules.md` MUST gain no line (it stays at 212) and at most 400 bytes; its toolkit test MUST be
  tightened to hold 212 lines.
- **FR-029**: The PR MUST report, in bytes ÷ 4, the change in standing cost of every prompt it changes, by when it is
  paid: every session (`kit/dm-rules.md`, skill descriptions), each resume (the resume skill and what it reads), each
  full save, each sheet update, each migration (006 and the migrate skill), and each player message (the hook's output,
  at its cap and typically).
- **FR-030**: The documents that describe what changed MUST change in the same PR: `.docs/campaign-files.md` (the card,
  the canon read, the register), `.docs/dmc-reference.md` (`dmc size`'s new lines, the hook), `.docs/decisions.md` (a
  decision for each of: cards, canon by scene, the hook and register, the gate; and an amendment of "Lookup and recall
  are for play only until Phase D" and of "Size rules are measured in tokens by `dmc size`", whose canon reason this
  reverses), `.docs/roadmap.md` (Phase D shipped, the gate pending), and the templates a companion's sheet and canon are
  built from.
- **FR-031**: Phase D's PR MUST say `Part of #179`. The PR that records the gate's result (and makes any rollback) MUST
  say `Closes #179`, whether the gate passed or rolled a part back.
- **FR-032 (what Phase D must not do)**: It MUST NOT change the PC sheet's read, the budgets of `game-state.md`,
  `dm-notes.md` or `summary.md`, recall's ranking or weights, lookup, the module reads of Phase C, or the sequel and
  legacy rules of Phase B; MUST NOT add a fallback for a missing `dmc`; and MUST NOT touch issue #208 or #198 option A.

### Key Entities

- **Play card**: the first section of a companion's sheet; what a scene and a fight need; within its budget; the only
  copy of the numbers it holds.
- **Scene's matches**: the canon lines outside the always-read sections that the resume reads because they match the
  scene it is about to run; bounded as FR-008 decides.
- **Names-and-roles register**: for each person, the names and role words the player may use; where it lives is FR-018.
- **Slip**: FR-020, in one category of FR-021.
- **Baseline and after sets**: every earlier session log of both campaigns, and three qualifying sessions after
  Phase D (FR-022).
- **Slip line**: `Slip: <what> (<category>)` in a session log (FR-033).
- **Gate result**: the table of FR-023, the verdict of FR-024, the action of FR-026, recorded as FR-025 says.
- **Migration 006**: standard 6; companion play cards (and the register's shape if it lives in canon).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a copy of Rorn's Landing migrated to standard 6, with no other file changed, `dmc size` reports the four
  companions at 4k or less in all (15.5k today).
- **SC-002**: On the same copy, `dmc size` reports canon at 9k or less (18.5k today).
- **SC-003**: The resume of that copy totals 50k or less by `dmc size` (69.5k today).
- **SC-004**: `dmc size`'s canon and companion lines equal what the resume step reads, checked on a test campaign by
  comparing its report with the parts the skill names.
- **SC-005**: `kit/dm-rules.md` stays at 212 lines and grows by at most 400 bytes (about 100 tokens a session); the
  resume skill and the full-save skill each grow by at most 1,000 bytes (about 250 tokens where they load).
- **SC-006**: Migration 006's golden fixture passes on CI, and a live run on a copy of Rorn's Landing verifies with no
  unaccounted line.
- **SC-007**: The prompt hook, with recall added, prints at most 4,000 bytes a message and nothing on a message with no
  new name, and takes under half a second on Rorn's Landing on the maintainer's machine.
- **SC-008**: The gate's result is recorded on issue #179 within a week of the third after-session, with the table of
  FR-023, and either no gate category rose or the rollback of FR-026 has shipped.
- **SC-009**: Across the after set, no gate category rises (FR-024): the story stays as consistent as before while the
  resume costs about a third less.

## Assumptions

- **The card budget is 1k tokens.** The issue's figure implies about 1.1k to 1.6k a card; a hand estimate of Kibil's
  card (who he is, voice, names, attitude, the fight numbers and standing traits) is 500 to 600 tokens. 1k leaves room
  and gives the 11.2k saving. It lives with the other budgets and is tuned by the gate's slip counts.
- **The card is a section of the sheet, not a file of its own**, so the sheet stays the one place a companion lives,
  and the full save and sheet update edit one file.
- **The register holds people only** (the issue's "names and roles"), not places or items.
- **"Once per session" means once per name per session part**, counted from the start of the log or its latest
  compaction boundary, because after a compaction the injected lines are gone from the context.
- **The hook skips the always-read sections**, which the resume read whole, so it never repeats them.
- **The comparison cannot separate Phase D from recall**: the baseline sessions were played before the play rules named
  `dmc recall`, and the after sessions have both. The categories count slips about the facts Phase D stopped reading,
  which is the nearest the evidence allows. This follows the user's decision.
- **The baseline counts slips the files recorded, so the after set does too** (FR-023): a slip only a transcript shows
  is reported but not counted, or the after set would look worse for being read more closely. The Silver Egg was played
  on older rules and may have a higher rate, which flatters the after set; the result reports Rorn's Landing's own
  average beside the combined one (unverified until the baseline is counted).
- **"After release" means after the play folder runs a kit with Phase D and the campaign is at standard 6**; the
  maintainer's play folder runs repository builds, so this need not wait for a tagged release.
- **The gate-record PR closes #179 even on a rollback**: the issue's plan is then done, with evidence either way. A
  later attempt at what was rolled back gets its own issue.
- **The floor below which canon is read whole** is set in the plan from a measurement, no higher than 6k, where the
  partial read saves nothing worth its searches.
- **Token figures are bytes ÷ 4**, as `dmc size`; they undercount, so they are compared with each other, not with the
  Read tool's cap.
