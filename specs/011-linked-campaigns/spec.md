# Feature Specification: Linked Campaigns and the Sequel Import

**Feature Branch**: `011-linked-campaigns`

**Created**: 2026-11-11

**Status**: Draft

**Input**: User description: "Issue #179 Phase B (Part of #179): steps 4 to 6 of the issue's plan. (4) Continuity
links: the single Predecessor line becomes a list of links (continues, side campaign of, shares a world with). dmc
follows them, so one recall covers the whole chain. "Continues" always names a closed campaign (issue #180). (5) A
sequel imports its closed predecessor, the way a module is ingested: dmc-new-campaign does it with subagents, part by
part, as its section 1b does for a transcript. It writes a brief folded once into the sequel's always-read files
(rulings, prices, debts, promises, house rules, player requests, deferred threads into the arc plan, and a sheet per
carried character built from their own history before species or class defaults). It also writes a digest in the
sequel's folder that is searched and never read at resume: canon, archive and sheet-history lines copied word for word
under one heading per subject, session-only facts, a scene spine, every line keeping its sN.M, and DM secrets marked
revealed or never revealed. Chains copy the predecessor's existing digests unchanged. Proof that nothing was lost: an
accounting of every line checked against the closing backup, plus a sample test of who, what, when, where, why and
how. Recall ranks the digest ahead of the predecessor's folder. Resume reads no earlier campaign. (5a) A legacy file
(about 3k cap) for linked campaigns still in play, written at each full save. At resume, dmc names any linked campaign
whose legacy file is newer than this campaign's last save. (6) Modules inside a chain: digests stay shared, canon wins
over the printed module, and recall reaches every linked campaign's modules. Migration: the link line only. The one
existing sequel gets its digest by hand, with the player. Settle the issue's open questions: where the links live, the
digest's files, headings and size limits, how a format change reaches copies already made, how the sample test is
drawn and what counts as a pass, and how recall labels side-campaign facts."

## Background

Issue #179 measured the maintainer's play folder: a 39-session sequel (Rorn's Landing) whose 46-session predecessor
(The Silver Egg) was itself built on a 638-page published module. Phase A (spec 009) gave DMC `dmc lookup` and
`dmc recall`, and recall already follows a single `- **Predecessor**:` line in `player-options.md` to the end of the
chain. Issue #180 (spec 010) gave a campaign a permanent close: a closing full save that keeps every fact where a sequel
looks first, a `.closed` mark, and a closing backup that is never pruned. This phase builds on both.

What the issue found, and what this phase answers:

- **The carry-over is where facts were lost.** The sequel names its predecessor about 50 times; the carry-over was done
  by hand into canon. A carried character's new sheet was written from her species' defaults ("immune to cold")
  against her own history, and the pointer to her earlier sheet was not followed (feedback of 2026-11-09). The fact
  survived on the new sheet; the reason did not.
- **Why and how are scattered.** The predecessor answers who, what, when and where well (a canon line carries its
  session, day, place and speaker; a scene heading carries a place and a time). Why and how sit in the least-read
  places: for that character's cold, 4 lines in `canon.md`, 21 in `canon-archive.md`, 10 in her sheet's history and
  more in 6 session logs.
- **The predecessor is large and partly noise.** 1.45 MB of markdown, including a 140 KB canon archive, 322 KB of
  session logs and a 431 KB imported transcript. Recall already excludes `import/`; it still ranks the predecessor's
  own canon, sheets and session logs against the sequel's.
- **One line is not enough links.** A sequel continues one campaign, but the same world can hold a side campaign or a
  one-shot played alongside. Today nothing links them, so a fact established in one is invisible to the other.
- **The Silver Egg is not yet closed.** Its ending is prose only, so the one real sequel's predecessor would have to be
  closed with `dmc-close-campaign` before a digest of it could be checked against a closing backup.

## Clarifications

### Session 2026-11-11 (settled in this specification)

The issue left these open for the specs. Each is settled here with its reason; `/speckit-clarify` may revisit them.

- Q: Where do the continuity links live? → A: In the campaign's `player-options.md`, as a `## Continuity` section of
  three lines (Continues, Side campaign of, Shares a world with), replacing the Predecessor line. Resume already reads
  the file, the player can read and edit it, and it travels with every export. Unlike `.closed` or `.standard`, nothing
  in it is a fact a program must trust: whether a linked campaign is closed is read from that campaign's own mark.
- Q: The digest's files, headings and size limits? → A: A folder per digested campaign under `digests/` at the top of
  the sequel's folder (FR-013): an index, one file per kind of subject (people, places, factions, items, the world),
  the scene spine, the secrets, and the accounting. One `## ` heading per subject. No digest file over the near-cap
  margin `dmc size` uses (20,000 estimated tokens, below the 25,000 Read cap); a kind that would pass it is split into
  parts by subject. Lines copied word for word keep their length.
- Q: How does a change to the digest's format reach copies already made? → A: Each digest records its format version
  in its index. A digest is never rewritten by play, and recall and lookup read every format version. A format change
  that older digests cannot meet ships a migration that rebuilds them from their campaign's closing backup, which is
  kept for good and cannot change because the campaign is closed; every copy of that digest in a later sequel is
  rebuilt the same way, so the copies stay identical.
- Q: How is the sample test drawn, and what counts as a pass? → A: `dmc` draws it, reproducibly, seeded from the
  closing backup (FR-024). For the drawn facts and people, a fresh context holding only the digest answers who, what, when,
  where, why and how. A question the predecessor's own files answer must get the same answer from the digest; a
  question they do not answer is not counted. The test passes only with no miss; after a fix it is drawn again.
- Q: How does recall label a fact from a side campaign? → A: Every hit names its campaign and, for any campaign but
  the one recalled in, how it is linked (FR-018). The play rules say that a fact from a side campaign or a campaign in
  the same world is a fact of the world, not something this campaign's characters know, unless this campaign's own
  files say they learned it.

### Session 2026-11-11 (the user's answer)

- Q: What does the migration write when a Predecessor line names a campaign that is not closed? → A: It stops and
  warns the player that continuing the earlier campaign would cause discrepancies, then asks whether to close it. On
  yes, the migration closes it and goes on. On no, it asks a simple "are you sure?" once, then goes on. (Option B of
  the specify session, as the user changed it.)
- Q: What happens when the player answers no to "are you sure?"? → A: The migration stops and says "Migration stopped
  until the previous campaign is closed. You can close it by running:" followed by the close command for the
  predecessor (the user, 2026-11-11).
- What follows from these answers, as written here: the link is written as Continues when the migration goes on; the
  question is asked before the backup, so a stop leaves the campaign `behind` and playable, with the migration offered
  again at the next resume, rather than `unfinished`, which would block play; the close question states that
  closing is permanent, so that the close skill does not ask a second time; and once the player has confirmed, nothing
  repeats the warning at resume or in recall (FR-004).

### Session 2026-11-11 (`/speckit-clarify`)

- Q: Should Phase B ship as one pull request, or as several pull requests built from this one spec? → A: One spec,
  three PRs, each `Part of #179` (FR-032).
- Q: Should secrets the closed campaign never revealed be carried into the sequel's DM notes, or stay only in the
  digest? → A: Only the unrevealed secrets that can still come up in the sequel, as DMC judges at import, go into the
  sequel's DM notes § Secrets; every secret stays in the digest with its outcome (FR-007).
- Q: For the sample test, should one fresh context answer all 25 drawn items, or each get its own? → A: One fresh
  context, holding only the digest, answers all 25; DMC then compares the answers with the closed campaign's files
  (FR-024).
- Q: When a character from the closed campaign who was not carried over turns up later in the sequel, is their new
  sheet built the same way? → A: Yes: whenever a sheet is made for a character from a linked campaign, DMC looks them up
  in the digest (or the linked campaign) first and builds from their history, with defaults only where it says
  nothing (FR-008a).

### Session 2026-11-11 (`/speckit-analyze`)

- Q: How much of each character sheet must the accounting cover? → A: Every line of every sheet, not only its history
  (FR-021). A dead character's stat block is dropped under a declared class; a living character's is copied under
  their heading; a sheet's own secrets go to the digest's secrets (FR-012). Otherwise a returning character's
  appearance, voice and names, which their new sheet is built from (FR-008a), would go unproved.
- Q: Where does the sample test draw its items from? → A: The 20 facts from the digest (copied and session-only lines
  in proportion), since session-only facts are in no accounted source; the 5 people from the closed campaign's sheets
  and minor NPCs in its closing backup, so a person the digest lost can be drawn (FR-024).
- Q: How does the spine copy a scene heading? → A: Word for word, parsing nothing (FR-011). The Silver Egg, the first
  campaign, predates the `(place, time)` form: of its 169 scene headings about 31 fit it, and Session 4 is "(void)" with
  no scenes.
- Q: How do resume and the full save learn of link problems and legacy files without a new command call each
  session? → A: Through `dmc size <campaign> --check`, which both already run and which prints nothing when all is
  well; `dmc campaign links` only lists the set (FR-031).
- Q: When the migration closes an open predecessor, how is the close pointed at it? → A: By name, never by switching
  the active campaign, so an interruption leaves the sequel active and its migration offered again (FR-028a).
- Q: How does `dmc` tell whether a linked campaign's legacy is news to this campaign? → A: By session numbers: the
  legacy says which of its campaign's sessions wrote it, and this campaign's `game-state.md` records the session of each
  legacy it folded in (FR-026). Dates and times cannot order them, since DMC writes dates without times; every campaign
  in the maintainer's folder has session numbers.

### Session 2026-11-13 (`/speckit-analyze`, before PR 3)

- Q: How far back does a legacy file reach? → A: It is a running list, one line per change with its `sN.M`. At each
  full save the writer adds that save's changes and drops only the lines every open campaign in its linked set has
  folded in: `dmc` names the lowest session any open reader has folded, and the writer drops that session's lines and
  earlier ones; `dmc` says `keep every line` while any of them has folded nothing. Closed campaigns never fold again,
  so they do not hold lines back. Past its budget the oldest lines go anyway; the writer's canon still holds them for recall (FR-025). Rewriting the file whole would lose the first of
  two saves made before a reader resumes, and the writer cannot see what each reader folded, since each records it
  only in its own `game-state.md`.
- Q: Must legacies stay right when two linked campaigns are played at the same time, in two terminals? → A: No; that
  is not a use case (the user, 2026-11-13). One campaign is played at a time, so a legacy's session number always
  orders it, and the stamp needs no scene.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A sequel starts from a closed campaign and loses nothing (Priority: P1)

The player has closed a campaign and wants to carry on the story. DMC starts a new campaign that continues it. Before
the first scene, DMC imports the closed campaign part by part, without reading it into the conversation, and writes two
things: a short brief folded into the sequel's own always-read files, and a digest of everything else in the sequel's
folder. Every character the player carries over gets a sheet in the sequel, built from that character's own history,
so a trait the earlier campaign gave a reason for keeps the reason. `dmc` then proves that every fact line of the
closed campaign is in the digest, merged into another line, or dropped for a stated reason, and a sample test shows
that the digest alone answers who, what, when, where, why and how for facts and people drawn at random.

**Why this priority**: The carry-over is where the issue found facts lost, and it happens once, at the start of a
sequel, when getting it wrong costs the rest of the campaign. Everything else in this phase serves it.

**Independent Test**: Close a small fixture campaign with known facts (one carried companion whose species default
contradicts her history, a debt, a ruling, a secret never revealed, a fact that lives only in a session log). Start a
sequel of it. Check that the brief's facts are in the sequel's always-read sections, the companion's sheet states her
history's version, the digest holds every canon, archive and history line or an accounting for it, `dmc` reports the
accounting complete against the closing backup, and the sample test passes.

**Acceptance Scenarios**:

1. **Given** a closed campaign with a closing backup, **When** the player starts a new campaign that continues it,
   **Then** DMC links the new campaign to it as Continues, imports it part by part with a fresh context per part, and
   reads none of the closed campaign's files into the main conversation.
2. **Given** the import has run, **When** the sequel's always-read files are read, **Then** they hold the closed
   campaign's rulings, prices, debts and promises still owed, house rules and the player's standing requests, each with
   its original session, the sequel's arc plan holds the threads the closed campaign left open, and its DM notes hold
   the unrevealed secrets that can still come up.
3. **Given** a companion carried into the sequel whose species gives a default ("immune to cold") and whose history
   says otherwise or gives a reason, **When** the sequel's sheet for her is written, **Then** it states what her history
   says, with the reason, and her voice, the names she uses and her appearance are on the sheet itself, not behind a
   pointer to the old sheet.
4. **Given** the digest is written, **When** `dmc` checks it against the closed campaign's closing backup, **Then**
   every line of the accounted sources (FR-021) is found word for word in the digest, or accounted as merged with an
   anchor that is in the digest, or dropped under a declared class with a reason, and the check fails naming each line
   that is none of these.
5. **Given** the accounting passes, **When** the sample test runs, **Then** each drawn fact and person is answered from
   the digest alone, and the import is finished only when no answerable question is missed.
6. **Given** an attempt to link Continues to a campaign that is not closed, **When** `dmc-new-campaign` is asked to do
   it, **Then** it refuses, says why, and offers to close that campaign first with `dmc-close-campaign`.
7. **Given** a sequel in play with a digest, **When** it is resumed, **Then** the resume reads no file of the closed
   campaign and no file of the digest.

---

### User Story 2 - One recall covers the whole chain, with the digest first (Priority: P1)

In a sequel, the player brings back someone from the earlier campaign. DMC recalls them, and one `dmc recall` searches
the sequel, its digests, every campaign it is linked to and all their modules. A fact the digest holds comes from the
digest, ahead of the same or a weaker line in the closed campaign's own folder, which stays searchable as the last
resort. Each hit says which campaign it comes from and how that campaign is linked, so DMC knows whether the
characters in play know it.

**Why this priority**: The digest is worth building only if recall finds it first. Recall is how play reaches
everything resume does not read.

**Independent Test**: In a fixture chain of three campaigns (C continues closed B, B continues closed A, both digests
in C), recall a fact known only to A, a fact in both B's canon and B's digest, and a fact only C holds. Check the
first is found and labelled as coming from A's digest, the second appears once, from the digest, and the third ranks as
it does today.

**Acceptance Scenarios**:

1. **Given** a sequel with a digest of its predecessor, **When** DMC recalls a fact the digest and the predecessor's
   canon both hold, **Then** the line appears once in the output, from the digest.
2. **Given** a chain A ← B ← C where C holds A's and B's digests, **When** DMC recalls in C, **Then** the search covers
   C, both digests, B's and A's own folders and the modules of all three, in one call, within the output cap.
3. **Given** a hit from a campaign other than the one recalled in, **When** it is printed, **Then** it names that
   campaign and its link (continues, sequel, side or world; a digest hit, its digested campaign's), and a hit from this campaign carries no
   label beyond its file.
4. **Given** a linked campaign whose folder is missing from `adventures/` but whose digest the sequel holds, **When**
   DMC recalls, **Then** the digest is searched and no notice is printed for the missing folder; if there is no digest
   either, one notice names the missing campaign, as today.

---

### User Story 3 - Campaigns in one world stay in step (Priority: P2)

The player runs a side campaign or a one-shot alongside the main campaign, in the same world, with different
characters. What one establishes about the world (a town burned, a ruler deposed, a price changed) should not be
contradicted by the other. Each campaign still in play writes a short legacy file at each full save: what changed in
the shared world. When the player resumes the other campaign, `dmc` names any linked campaign whose legacy this
campaign has not yet folded in, and DMC reads that one file and folds what changed into this campaign's canon.

**Why this priority**: It extends the links to campaigns that are both open, which the sequel import cannot cover. It
matters only to players who run more than one campaign in a world.

**Independent Test**: Link a fixture side campaign to a fixture main campaign. Full-save the side campaign with a world
change; resume the main campaign and check that one line names the side campaign's legacy and that DMC folds the change
into canon at its next full save; resume again and check that nothing is named.

**Acceptance Scenarios**:

1. **Given** a campaign linked to at least one open campaign, **When** it is full-saved, **Then** its legacy file gains
   what this save changed in the shared world, keeps every earlier change a linked open campaign has not yet folded in,
   stays within its budget, and is stamped with the session that wrote it and the date.
2. **Given** campaign X and a linked open campaign Y whose legacy X has not folded in, **When** X is resumed,
   **Then** one line names Y and its legacy file, and DMC reads that file and no other file of Y.
3. **Given** X's next full save after reading Y's legacy, **When** it completes, **Then** the world changes it named are
   canon lines in X, marked as learned from the world rather than seen by X's characters unless they were, X records
   which session's legacy of Y it folded in, and the next resume names nothing.
4. **Given** a campaign with no links, or whose only link is Continues to a closed campaign, **When** it is full-saved
   or resumed, **Then** no legacy file is written, nothing is named, and the session pays nothing for this feature
   beyond the kit's skill text.

---

### User Story 4 - The existing sequel moves to the new links (Priority: P2)

The maintainer's sequel has the old single Predecessor line. A migration moves that line, and that of any campaign
created before this change, to the new Continuity section, and nothing else. The one existing sequel's digest is built
once, by hand, with the player, after its predecessor is closed; the kit gains no path for digesting into a sequel
already in play.

**Why this priority**: Without it the one real sequel stops being linked when recall stops reading the old line.

**Independent Test**: The migration's golden fixture: a campaign at the previous standard with a Predecessor line
naming a closed campaign, and one with `none`; after the migration each has the Continuity section with the right
values, `verify` passes, and the shape check fails on a file left with the old line.

**Acceptance Scenarios**:

1. **Given** a campaign whose Predecessor line names a folder, **When** it is migrated, **Then** its `player-options.md`
   has a Continuity section whose Continues line names that folder, the Predecessor line is gone, and nothing else in
   the campaign changed.
2. **Given** a campaign whose Predecessor line is `none`, **When** it is migrated, **Then** its Continuity section's
   three lines are `none`.
3. **Given** a Predecessor line naming a campaign that is not closed, **When** it is migrated, **Then** the migration
   stops at that step and warns the player that the earlier campaign is still open, and that playing it on would
   make its story and the sequel's disagree; then asks once whether to close it, saying that closing is permanent.
   - On yes, the predecessor is closed with `dmc-close-campaign` (the yes stands for that skill's own question), and
     the migration goes on, writing Continues.
   - On no, the migration asks once more, simply, whether the player is sure. On yes it goes on, writing Continues
     to the open campaign. On no, it stops and says: `Migration stopped until the previous campaign is closed. You
     can close it by running: /dmc-close-campaign <predecessor>`, with the predecessor's folder name. The campaign is
     left as it was before the migration began.
4. **Given** a closed campaign, **When** migrations are run, **Then** it is not migrated (as today), and recall still
   reads its old Predecessor line, so a closed sequel in a chain keeps its link.

---

### User Story 5 - Modules inside a chain stay shared (Priority: P3)

A sequel, its predecessor and a side campaign may each be built on a published module, the same one or different ones.
The module's digest in `modules/` is shared and never changed by a campaign; what a party changed lives in that
campaign's canon, and canon wins over the module as printed. Recall reaches the modules of every linked campaign, and
ranks every campaign's line above the module's.

**Why this priority**: Recall already reaches a predecessor's module (Phase A). This extends it to every link and
writes down the rule that was only implied.

**Independent Test**: A fixture where the predecessor's canon says the party burned an inn the module describes intact.
Recall the inn in the sequel: the canon line (or its digest copy) ranks above the module's line, and both are labelled.

**Acceptance Scenarios**:

1. **Given** linked campaigns built on modules, **When** DMC recalls, **Then** each module is searched once, labelled
   with its folder, whichever campaign it came through.
2. **Given** a campaign fact and a module line about the same subject, **When** both match about equally, **Then** the
   campaign's line ranks first.
3. **Given** a sequel import of a campaign built on a module, **When** the digest is written, **Then** it holds no copy
   of the module's text, and the closed campaign's `module-progress.md` deviations and module NPC statuses are in the
   digest.

---

### Edge Cases

- **A loop of links** (A shares a world with B, B with A, or a longer ring): each campaign is visited once.
- **A campaign linked to itself**, or a link naming something that is not a plain folder name: ignored with one
  notice, as Phase A does for a bad Predecessor value.
- **Two Continues values**, or two Side campaign of values: the campaign is refused at creation; a hand edit that adds a
  second one gets a notice naming the line, and only the first value is followed.
- **A link to a missing folder**: one notice, unless the campaign holds that campaign's digest, which then stands in for
  it.
- **A migration's predecessor that cannot be closed yet** (it is behind the current standard, a migration of it is
  unfinished, or it is mid-fight): the close skill's own refusal is relayed. For `behind`, the predecessor is migrated
  first, then closed, then this migration goes on; for anything else, the player is told why and the migration stops
  before its backup, changing nothing, until it is run again.
- **Migrating every campaign at once** when one is the other's predecessor: the predecessor is migrated before its
  sequel, so that it can be closed when the sequel's migration asks.
- **A closed predecessor whose closing backup is missing or fails its check**: the import refuses, since nothing can be
  accounted against it; the player is told which file is missing.
- **A closed campaign that itself holds digests** (the middle of a chain): its digests are copied into the new sequel
  unchanged and checked to be identical to its copies; only the campaign just closed is digested anew.
- **A digest copy in a sequel that differs from its source** (hand-edited): `dmc` reports it on the next check of the
  sequel's digests, naming the file; the copy is restored from the source or rebuilt from the closing backup.
- **An import interrupted part way** (the session ends, a subagent fails): the progress is recorded in the sequel so the
  import resumes at the next part, as section 1b does; the sequel's first scene waits until the import, the accounting
  and the sample test are done.
- **No subagents available**: one part per session, with the progress file, as section 1b does.
- **A character from a linked campaign returns mid-sequel and needs a sheet**: built from the digest's history first
  (FR-008a); where neither the digest nor the linked campaign has them, the sheet is written as for any new character.
- **A carried character whose history and a species or class default disagree and the history is silent on why**: the
  history's statement wins; the sheet does not invent a reason.
- **A secret revealed in the closed campaign**: it is no longer a secret in the sequel; it goes in the digest's secrets
  with `revealed sN`, and what the characters learned of it is in the brief or the digest's subject lines.
- **A fact the closed campaign never settled** (a known inconsistency left open): the digest carries the reading the
  closing save settled, and the accounting drops the other reading as superseded, with the line it was superseded by.
- **A test fight or other non-canon file** the closing save named in § How it ended: left out, accounted as dropped
  under its declared class.
- **Two linked campaigns played at the same time** (two terminals): out of scope. The player plays one campaign at a
  time, and each resume starts a new session, so session numbers order every legacy.
- **A side campaign closed while the main campaign is in play**: its last legacy, written at the closing save, stays
  and is read once at the main campaign's next resume; recall still reaches its folder.
- **Two full saves of a campaign before a linked campaign resumes**: the legacy keeps both saves' changes, since the
  writer drops only lines older than every open reader's last fold (FR-025).
- **The legacy file of a campaign grows over its budget** (a linked campaign left unplayed for long): `dmc size` flags
  it as any budgeted file, and the full save drops its oldest lines until it fits. A reader that had not folded them
  misses them in the legacy; the writer's canon still holds them, and recall reaches it.
- **Recall in a campaign with a very large linked set**: the output cap still holds; the time stays within SC-006.

## Requirements *(mandatory)*

### Functional Requirements

**Continuity links**

- **FR-001**: A campaign's `player-options.md` MUST hold a `## Continuity` section with three lines, `- **Continues**:`,
  `- **Side campaign of**:` and `- **Shares a world with**:`, each `none` or folder names in `adventures/`; Continues
  and Side campaign of take at most one name, Shares a world with takes any number, comma-separated. The section
  replaces the Predecessor line, and the campaign template ships it with `none` on each line.
- **FR-002**: Continues MUST name a closed campaign. `dmc-new-campaign` MUST refuse to write a Continues link to a
  campaign that is not closed, and offer to close it first.
- **FR-003**: `dmc` MUST treat links as joining campaigns both ways: a campaign is linked to every campaign it names and
  to every campaign in `adventures/` that names it. The linked set is every campaign reachable through links from the
  campaign in question, each visited once, with its distance in steps and the link by which it was first reached.
- **FR-004**: `dmc` MUST report, in one line each and only when there is something to report, a link naming a missing
  folder (unless the campaign holds its digest), a link that is not a plain folder name, a campaign linked to itself,
  and a second name for a single-valued line (on that line or on a second line of the same name). A campaign with no
  problem and no news prints nothing. A Continues link to a campaign that is not closed is reported only where a link
  is being written (`dmc-new-campaign` and the migration, FR-028); once the player has kept it (FR-028), resume and
  recall say nothing about it, and recall follows it like any other link. In play these lines come from
  `dmc size <campaign> --check`, which resume already runs (FR-031); it names only the lines of campaigns that are not
  closed, since a closed campaign's file can no longer be changed (recall and `dmc campaign links` still name them).
- **FR-005**: A closed campaign whose files still have the Predecessor line MUST keep its link: `dmc` reads that line as
  Continues wherever it finds no Continuity section.

**The sequel import**

- **FR-006**: `dmc-new-campaign` MUST, for a campaign that continues a closed one, import that campaign before the first
  scene, in parts of bounded size, the first holding the files only the brief needs (FR-007), each in a fresh context
  (a subagent), run one after another, reading the closed
  campaign's files only from those contexts, and record its progress in the sequel so an interrupted import resumes at
  the next part. Without subagents it MUST do one part per session, as section 1b does.
- **FR-007**: The import MUST write a brief into the sequel's own files, once: the closed campaign's rulings, prices,
  debts and promises still owed into the sequel's canon sections that resume always reads; its house rules and the
  player's standing requests into the sequel's `player-options.md`; its open and deferred threads into the sequel's
  arc plan; each secret marked `never revealed` or `abandoned` that can still come up in the sequel (its keeper or its
  stakes are still in the world) into the sequel's `story/dm-notes.md` § Secrets, a secret whose subject is gone or
  settled staying in the digest only; and its ending into the sequel's summary premise. Each brief line keeps its
  original session reference and names the campaign it came from.
- **FR-008**: The import MUST write a sheet in the sequel for each character the player carries over (PC or companion),
  built from that character's own sheet and history first and from species or class defaults only where the history
  says nothing. The sheet MUST hold the character's voice, the names they use and their appearance itself, not as a
  pointer to the earlier sheet. Where the history and a default disagree, the sheet states the history's version and,
  where the history gives one, its reason.
- **FR-008a**: The same rule MUST hold after the import: whenever DMC makes a sheet in a campaign for a character from a
  campaign in its linked set (an NPC who was not carried and returns later, say), it first recalls or looks up that
  character in the digest, or in the linked campaign where there is no digest, and builds the sheet from their history,
  with species or class defaults only where it says nothing. The rule is a clause of `dmc-character-sheet-update`,
  which loads only when a sheet is written, so it adds nothing to an ordinary session's standing cost.
- **FR-009**: The import MUST write a digest of the closed campaign in the sequel's folder, which reorganises and does
  not condense: every line of the accounted sources (FR-021) that carries a fact is copied word for word under the
  heading of the subject it is chiefly about (a person, place, faction or item; or the world, for rulings, prices,
  lore and the ending); a line equally about two subjects may be copied under both. Each line keeps its `sN` or `sN.M`.
- **FR-010**: The digest MUST add, as lines under their subject, the facts that live only in a session log, each with
  its `sN.M`, found by the import's contexts as they read the logs.
- **FR-011**: The digest MUST hold a scene spine: for each session, its log's title and each scene heading's text,
  word for word and in order, with the session log's file name, so the timeline points back to the full log. `dmc`
  MUST write the spine from the closed campaign's session logs, exactly, and parse nothing in a heading, since older
  campaigns' headings do not all give a place and a time; a log with no scene headings gets its session line alone.
- **FR-012**: The digest MUST keep the DM's secrets apart from the facts the characters know, each secret with its
  outcome as the closing save wrote it (`revealed sN`, `never revealed` or `abandoned`), and recall MUST mark every hit
  from the secrets as DM-only. A sheet's own `## Secrets` lines are secrets too: they go to the digest's secrets, under
  the character's name, and never among the subject lines.
- **FR-013**: A digest MUST live in `digests/<campaign>/` at the top of the sequel's folder: an index (the campaign it
  digests, its closing backup's file name and checksum, the format version, where it sits in the chain, and the files);
  one file per kind of subject (people, places, factions, items, the world); the spine; the secrets; and the
  accounting. Each subject has one `## ` heading. No digest file may exceed the near-cap margin `dmc size` uses
  (20,000 estimated tokens, below the 25,000 Read cap), so none is ever cut when read; a kind that would is split into
  parts, each holding whole subjects.
- **FR-014**: The digest MUST leave out: plans the story overtook, scene and combat state, dead characters' stat blocks,
  maps, `import/`, `exports/`, `backups/`, dice bookkeeping in the logs, any copy of a module's text, and whatever the
  closing save named as not canon. Only the reading the closing save settled is carried; a superseded reading is
  accounted as dropped.
- **FR-015**: In a chain, the import MUST copy the digests the closed campaign already holds into the sequel's
  `digests/` unchanged, and `dmc` MUST check each copy is identical to the closed campaign's; only the campaign just
  closed is digested anew.
- **FR-016**: Neither the import nor anything after it may change any file of the closed campaign.

**Recall, lookup and resume**

- **FR-017**: `dmc recall` MUST search the campaign, its digests, every campaign in its linked set and every module of
  each, each module once. It MUST rank a digest's line above an equally matching line from the digested campaign's own
  folder, and every campaign's line above an equally matching module line, as today.
- **FR-018**: Each hit MUST name the campaign or module it comes from; a hit from any campaign but the one recalled in,
  or from a digest, MUST also say how it is linked: `continues`, `sequel`, `side` or `world` (research R2), a digest's
  hit carrying its digested campaign's. Lines whose text is identical MUST count as one line, ranked and printed once,
  so a digest's copy and its source are not both printed; that line is DM-only when any copy is.
- **FR-019**: `dmc lookup` MUST be able to print one subject's section of a digest by its heading.
- **FR-020**: The play rules MUST say, in the lines that already say where facts come from, that a fact from a side
  campaign or a same-world campaign is a fact of the world, not something this campaign's characters know unless this
  campaign's files say they learned it; that a fact from a continued campaign is known to the characters who were
  there; and that a fact from a sequel (recalled in an earlier campaign the player kept open) has not happened yet.
  `kit/dm-rules.md` MUST grow by no more than one line.
- **FR-020a**: Resume MUST read no file of another campaign and no digest file, except the legacy files `dmc` names
  (FR-026). `dmc size` MUST leave the digest out of the resume total. Each digest file MUST be checked against the
  near-cap margin (FR-013) when the digest is written, by the digest's own check, since a digest never changes afterwards.

**Proof that nothing was lost**

- **FR-021**: The accounted sources are, from the closed campaign as its closing backup holds it: `story/canon.md`,
  `story/canon-archive.md`, every line of every file in `character-sheets/` (each sheet whole, each `*-history.md`
  and `minor-npcs.md`), the secrets of `story/dm-notes.md`, `story/summary.md` § How it ended, and
  `module-progress.md` if it exists.
- **FR-022**: Each non-blank line of the accounted sources MUST be, in the digest: found word for word (after the
  normalisation migrations' verify uses); or accounted as merged, with an anchor found in the digest; or accounted as
  dropped under a declared class (FR-014) with a reason. The accounting is a file in the digest's folder.
- **FR-023**: `dmc` MUST check the accounting against the closing backup, not the live folder, and fail with the lines
  that are unaccounted, naming each; the import is not finished until it passes. The check also passes or fails a
  digest copied from a chain (FR-015) by identity with its source.
- **FR-024**: `dmc` MUST draw the sample test reproducibly, seeded from the closing backup (the same backup gives the
  same draw, and a redraw after a fix gives a new one): 20 fact lines from the digest's subject files, split between
  lines copied from the accounted sources and session-only facts (FR-010) in proportion; and 5 people from the closed
  campaign's own sheets and minor NPCs in the closing backup, at least one of them a carried character, so that a
  person the digest lost is drawn as readily as one it kept. The sample tests that the digest answers the six
  questions; the accounting (FR-022, FR-023) is what proves that no line was lost. One fresh context
  (a subagent), given only the digest and never any file of the closed campaign, answers who, what, when, where, why
  and how for every drawn item; DMC, in the main conversation, then compares each answer with the closed campaign's
  files, reading only the lines the comparison needs. A question the closed campaign's files answer is a miss if the digest's answer is missing or
  different; a question they do not answer is not counted. The test passes only with no miss; after a fix, it is drawn
  and run again.

**Legacy files**

- **FR-025**: A campaign with at least one open campaign in its linked set MUST bring its legacy file up to date at
  each full save, the closing save included: what changed in the shared world that the linked campaigns would need,
  in the world's terms, one line per change with its `sN.M`, stamped with the session that wrote it (and the date, for
  people), within a budget of about 3k tokens that `dmc size` checks. The file is a running list: the save adds its
  own changes and drops the lines `dmc` names (`drop the lines from session N and earlier`), N being the lowest
  session of this campaign's legacy that the open campaigns in the linked set have folded in (FR-026), so a line every
  open reader has folded is dropped and no other is; while any of them has folded none, `dmc` says `keep every line`. Over budget, the oldest lines go until it fits. A campaign whose linked set holds
  no open campaign writes none.
- **FR-026**: At resume, `dmc` MUST name, one line each, every open or closed campaign in the linked set whose legacy
  was written in a later session of that campaign than the one this campaign last folded in, or that this campaign has
  never folded in, and nothing when there is none. DMC reads each named legacy file, and at the next full save folds
  what changed into canon (marked as world knowledge unless the characters saw it) and records, in `game-state.md`,
  the session of each legacy it folded. Session numbers decide, not dates or times, which DMC does not write reliably
  (Rorn's Landing's last full save line is `2026-11-11, session 47, through scene 2`), so two campaigns played the
  same day are still told apart.

**Modules in a chain**

- **FR-027**: A module's own folder MUST never be changed by a campaign or an import. Canon, and the digest of a closed
  campaign, win over a module as printed, and the play rules already say so; this phase adds no rule for it beyond
  FR-017's ranking.

**Migration and the existing sequel**

- **FR-028**: A migration MUST move a campaign's Predecessor line to the Continuity section: Continues gets the
  Predecessor's value, the other two lines `none`. It touches `player-options.md` only, and names a shape check that
  fails on a file still holding the Predecessor line.
- **FR-028a**: When the Predecessor names a campaign that is not closed, the migration MUST stop at that step, warn the
  player that the earlier campaign is still open and that continuing it would make the two stories disagree, and ask
  once whether to close it, saying that closing is permanent. On yes, it MUST close that campaign by following
  `dmc-close-campaign` (the yes answers that skill's question, so it is not asked again) and then go on. On no, it MUST
  ask once more whether the player is sure: on yes it goes on; on no it MUST stop and say `Migration stopped until the
  previous campaign is closed. You can close it by running: /dmc-close-campaign <predecessor>`, with the
  predecessor's folder name. When the migration goes on, the link is written as Continues. These questions MUST be
  asked before the migration's backup, so that a stop changes nothing: the campaign stays `behind`, still playable,
  and the migration is offered again at the next resume. The closing is the close skill's work on the other campaign,
  not a change this migration makes, so the migration's `touches` stay `player-options.md`.
- **FR-029**: The kit MUST gain no path for digesting into a sequel already in play. The maintainer's one existing
  sequel gets its predecessor's digest once, by hand, on local data, with the player, after the predecessor is closed,
  using the same `dmc` checks.

**Documentation and cost**

- **FR-030**: Each PR (FR-032) MUST update `.docs/` (campaign files, the `dmc` reference, the user guide, the
  architecture, the roadmap and the decisions) and every skill and template it touches, for what it ships.
- **FR-031**: The three PRs together MUST NOT add more than about 150 tokens to an ordinary play session's standing
  cost (rules, skill descriptions, resume and full-save text read every session), measured as bytes ÷ 4; each PR
  reports its share. The links add no command call to a session: what resume and the full save need from them
  (FR-004, FR-025, FR-026) is printed by `dmc size <campaign> --check`, which both already run.

**Delivery**

- **FR-032**: The feature MUST ship as three PRs from this spec, in this order, each saying `Part of #179` (Phases C and
  D remain, so none closes the issue), each working and documented on its own:
  1. **Links and recall**: the Continuity section and its template (FR-001 to FR-005); recall and lookup over the
     linked set and over digests, with the ranking, labels and duplicate removal (FR-017 to FR-019, FR-027); the play
     rules' line (FR-020); `dmc size` leaving digests out of the resume total (FR-020a); and the migration (FR-028,
     FR-028a). A digest written by hand in the shape of FR-013 is searched and looked up correctly after this PR, which
     is what the by-hand digest of Rorn's Landing (FR-029) needs.
  2. **The sequel import**: `dmc-new-campaign`'s import with the brief, carried characters' sheets and the digest
     (FR-006 to FR-016), the sheet rule for returning characters (FR-008a), and `dmc`'s spine, accounting check and
     sample draw (FR-021 to FR-024), which FR-029's by-hand step also uses.
  3. **Legacy files**: FR-025 and FR-026.

### Key Entities

- **Continuity link**: a relation from one campaign to another: Continues (the other is closed and this one carries its
  story on), Side campaign of (a campaign played alongside a main one, in its world), Shares a world with. Written in
  the campaign's `player-options.md`; followed both ways by `dmc`.
- **Linked set**: every campaign reachable from a campaign through links, with distance and how each was reached.
- **Brief**: the part of a closed campaign folded once into a sequel's always-read files and carried characters'
  sheets: what play needs every session.
- **Digest**: a closed campaign's facts, reorganised by subject, in the sequel's `digests/<campaign>/`: searched and
  looked up, never read at resume, never changed by play.
- **Scene spine**: each session's title and scene headings, word for word, with the log they come from.
- **Accounting**: one line per line of the accounted sources that is not copied word for word: merged with an anchor,
  or dropped with a class and a reason.
- **Sample test**: a reproducible draw of facts and people, each answered from the digest alone on six questions.
- **Legacy file**: a running list of what changed in the shared world, brought up to date by a linked campaign still
  in play at each full save, stamped, read once by each linked campaign that has not yet folded it in, and trimmed
  only of lines every open linked campaign has folded.
- **Closing backup**: the checked export spec 010 writes when a campaign closes, kept for good; the reference the
  accounting is checked against.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A sequel's resume reads no earlier campaign and no digest: the resume total `dmc size` reports for a
  sequel is unchanged by adding its digests (0 tokens added), and the resume reads no file outside the campaign except
  a legacy file `dmc` named.
- **SC-002**: For the fixture and for the maintainer's sequel, `dmc`'s accounting check reports every line of the
  accounted sources copied, merged or dropped with a reason: 0 unaccounted lines.
- **SC-003**: The sample test passes with 0 misses on the fixture and on the maintainer's sequel, over all six questions
  for each of the 20 facts and 5 people drawn.
- **SC-004**: On the maintainer's play folder with The Silver Egg digested into Rorn's Landing, `tools/recall-test.ps1`
  puts the answer in the output for at least as many questions as Phase A measured (28 of 30 named, 23 of 30
  indirect), and for each fact that is in both the digest and The Silver Egg's own folder, the hit printed comes from
  the digest.
- **SC-005**: A carried character's sequel sheet contradicts her own history in 0 places on the fixture (the cold case
  reproduced), and her voice, names used and appearance are on the sheet. The same holds for a fixture NPC who was not
  carried and is given a sheet when they return in a later session of the sequel.
- **SC-006**: One recall over a chain of three campaigns plus one side campaign and one same-world campaign, at the
  measured sizes (about 3 MB of markdown and a large module), returns within 1 second and within the output cap.
- **SC-007**: A campaign with no links pays nothing in play: no legacy file, no line at resume, no notice in recall,
  and its standing cost rises by no more than FR-031's 150 tokens of kit text.
- **SC-008**: A linked open campaign with a legacy this campaign has not folded in is named at resume in exactly one
  line, and is not named again after this campaign's next full save.
- **SC-009**: Every digest file is under the near-cap margin of 20,000 estimated tokens, and every subject in it has
  exactly one heading.

## Assumptions

- The closed campaign was closed with `dmc-close-campaign` (spec 010), so its closing backup exists, every secret has
  its outcome, its loose ends and each fact's reason are canon lines, and its sheets keep their whole history. A
  campaign ended by hand must be closed that way first.
- The Silver Egg is closed before its digest is built for Rorn's Landing: at Rorn's Landing's migration (FR-028a), or
  otherwise as part of the by-hand step (FR-029).
- The sample test's size (20 facts and 5 people) is a starting value; it can be tuned like the recall weights. The six
  answers are judged by DMC against the closed campaign's files, so the test checks the digest, not DMC's judgment; a
  model's "nothing is missing" is not the proof, which is why the word-for-word accounting comes first.
- Parts of about 100 KB, as section 1b uses for a transcript, suit the import too.
- Whole character sheets, `minor-npcs.md` and `module-progress.md` are accounted, beyond the issue's canon, archive and
  sheet history, since they hold facts about people (appearance, voice, names, relationships) and what the party
  changed in a module.
- Two open campaigns in one world are rare; the legacy design favours zero cost for the common single campaign over
  convenience for that case.
- Writes through the CLI, embeddings, and changing what resume reads (Phase D) are out of scope, as the issue says.
  Module lookup rules and ingest checks are Phase C.
