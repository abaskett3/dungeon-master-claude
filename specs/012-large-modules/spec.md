# Feature Specification: Large Modules Cost One Area, Not One Chapter

**Feature Branch**: `012-large-modules`

**Created**: 2026-11-14

**Status**: Draft

**Input**: User description: "Issue #179, Phase C (step 7): large modules. Resume reads the module's index and the
current area's sections only. Stat blocks, items, spells and player options come through lookup. Ingest gains two
checks: one heading per stat block or area, and no reference file over the Read cap. No migration for campaigns. A
contained change to one skill and its checks. Phase D (companion play cards, canon reading, hook recall, the gate) is
out of scope. Markdown stays the source of truth, Native AOT, no stored index, one-line
errors and small output. Modules already ingested in players' folders must still work. modules/ is git-ignored and
copyrighted, so tests use synthetic fixtures."

## Background

Issue #179 is a tracking issue in four phases; this spec is Phase C, its step 7. Phase A shipped `dmc lookup`, which
prints one section of a module, the rules or a campaign file by its heading, and `dmc recall`, which searches a
campaign, the campaigns linked to it and their modules. Phase B shipped continuity links, the sequel import and legacy
files. Phase C uses what those two commands already do and changes one skill (`dmc-ingest-module`), one step of
another (`dmc-resume-campaign` step 9) and a check.

The issue measured a real large module: 638 pages, 1.96 MB of raw text, and 38 digest files of 1.44 MB (about 360k
tokens).

- **A resume is cheap but does not need to be dearer than it is.** The index is 3.3k tokens. A chapter digest is 5k to
  14k. The hub chapter is 13.8k, and the party returns to it between every excursion, so the resume that follows each
  return reads the whole hub chapter, not the one area the party is in.
- **A reference file can be too big to read.** One monster file is about 37k tokens, over the 25k the Read tool
  returns in one call; another is about 24k, close enough that bytes ÷ 4 undercounts it and the tool still cuts it.
  Each stat block in them is already its own `## NAME (PDF n)` section of about 20 lines (about 650 tokens), which is
  what lets `dmc lookup` print one stat block alone.
- **What the skill writes does not guarantee that.** The ingest skill's digest format puts a chapter's NPCs under one
  heading and gives a chapter's areas headings only by habit. Nothing checks that a stat block or an area has a
  heading of its own, or that a reference file stays readable. A module ingested without them cannot be looked up by
  area, NPC or stat block, and a file over the cap comes back cut when a lookup misses and DMC opens it, or when DMC
  reads it to edit it.
- **Lines run long.** Digest lines reach 1,576 characters, so a line-based search hit costs several hundred tokens.
  Phase C does not change that; the sections below are what keep a hit's cost small.
- **The source's text layer drops digits from tables**, which is why the digests were corrected from page images.
  Lookup never prints text cut from extracted PDF text (Phase A, and not changed here).

The issue's expected result for this phase: a large-module campaign adds about 3k (the index) plus the current area's
section, however large the book.

What Phase C must not do. Phase D (issue steps 8 to 10) changes what a resume reads of companions and canon, adds a
hook that recalls canon lines, and sets a gate; it waits for `review-transcript` to show no rise in slips. Phase C
changes how a resume reads a module and nothing else: it adds no companion play card, leaves the canon reading alone,
adds no hook, and sets no gate (FR-016).

## Clarifications

### Session 2026-11-14

Answered by the user:

- Q: Phase A's decision says "changing what a resume reads is Phase D's, behind its gate", but the issue puts the
  module part of the resume in Phase C. Which holds? → A: The issue. The gate (step 10) concerns canon reading only,
  and a module's area read is a different file with no companion or canon content. Phase C changes step 9 of the
  resume and nothing else; the decision is amended in `.docs/decisions.md` to say so (FR-016).
- Q: Does the file check cover chapter digests as well as reference files? → A: Yes, every markdown file of the
  module, by one number. The measured chapters are 5k to 14k and pass; one that is over the mark would cut the same
  way. The issue named reference files because they were the ones that failed (FR-006).
- Q: Does the kit repair an oversized reference file in an existing module, or only report it? → A: Report only. The
  check names the file and the repair; the player, or DMC on the player's word, splits it by hand as for any edit in
  `modules/`. Lookup searches every top-level file of the module, so a split breaks nothing, and with `dmc` installed
  an oversized file costs only when a lookup misses or DMC edits the file (FR-012).
- Q: Does a module campaign's resume say when its module fails the check? → A: Yes, one line through the
  `dmc size --check` the resume already runs, for the full save to act on and not mentioned to the player (FR-013).
- Q: Must play still work without `dmc`? → A: No. `dmc` is a required part of DMC; when it is missing or broken, DMC
  tells the player to reinstall it, as `kit/dm-rules.md` says. This spec adds no fallback for a missing `dmc`.
  Retiring the old principle "play never depends on `dmc`" everywhere else is issue #208, its own change. A lookup
  that finds no heading still falls back to searching: that is `dmc` finding nothing, not `dmc` missing.

Derived from the sources, not asked:
- Q: The issue says "no reference file over the Read cap". The cap is 25,000 tokens, yet a file of about 24k was cut.
  What number does the check use? → A: The mark `dmc size` and `dmc lookup` already use, 20,000 tokens or 1,600
  lines, which is 80% of the cap because bytes ÷ 4 undercounts. A reference file at the mark fails the check (FR-006).
- Q: Phase B's digest check already flags a file over 20k tokens and a subject with two headings. Is this a second
  rule? → A: The same rule for a second kind of file; the mark is the one `Budgets` holds, so there is no new number
  (FR-006).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A resume of a large-module campaign reads the index and one area (Priority: P1)

The party has been in the hub chapter of a 638-page module and has just come back from an excursion. The player
resumes. DMC reads the module's index and the section of the digest for the area the party is in, and nothing else of
the module. It does not read the hub chapter's other areas, the other chapters, or any reference file. When the party
walks into the next area, DMC looks that area up by its heading, as the rules already say.

**Why this priority**: This is the saving the issue sets for the phase. The hub chapter's 13.8k is paid at every
resume between excursions; the index and one area are a few thousand tokens however large the book. It is also the
phase's change to what DMC reads.

**Independent Test**: Build a synthetic module of the measured shape (a few dozen chapter digests, one chapter with
many keyed areas, reference files) and a campaign whose `module-progress.md` names the module and one area. Run
`dmc size` on it. Pass: its module lines are the index and that one area's section; the total does not change when the
module's other chapters are made ten times larger; and the area's section is a small part of its chapter's size.

**Acceptance Scenarios**:

1. **Given** a module campaign whose position names an area, **When** it is resumed, **Then** DMC reads
   `module-progress.md`, the module's `index.md` and that area's section of the chapter digest, and no other module
   file.
2. **Given** the party moves to another area, **When** DMC needs it, **Then** it looks the area up by its heading
   (`dmc lookup`), as `kit/dm-rules.md` § Running a published module says, and `module-progress.md` is updated to
   name the new area.
3. **Given** a module whose chapter status shows it finished, **When** the campaign is resumed, **Then** DMC reads
   neither the index nor any digest, as today.
4. **Given** a campaign whose `module-progress.md` still names a whole chapter section (any campaign made before this
   change), **When** it is resumed, **Then** DMC reads that section, as it does today, and nothing breaks.

---

### User Story 2 - Ingest writes a module that lookup can reach (Priority: P1)

The player ingests a long module. The skill writes each keyed area, each NPC, each stat block, each magic item and
each handout under a heading of its own, named as the module names it, with its page in the heading. When it finishes
or pauses, it checks the module and reports what fails: a section that holds more than one stat block or is too large
to be one area, and a file the Read tool cannot return in one call. DMC fixes each before it says the chapters are
ready.

**Why this priority**: Everything in User Story 1 and in Phase A's lookup rests on headings of the right size. A
module written without them looks fine and fails in play: the area cannot be found, or the stat block comes with
twenty others.

**Independent Test**: Build synthetic modules: one that follows the rules, one with two stat blocks under one heading,
one with a keyed area section far over the limit, one with a reference file over the mark. Run the check on each.
Pass: the good one prints nothing; each bad one is named by file and heading, one line per problem, and the line says
how to fix it.

**Acceptance Scenarios**:

1. **Given** a chapter digest where every keyed area has its own heading, **When** the check runs, **Then** it
   prints nothing for that file.
2. **Given** a reference file with two stat blocks under one heading, **When** the check runs, **Then** it names the
   file and the heading and says to give each stat block a heading.
3. **Given** a section of a chapter digest larger than the section limit, **When** the check runs, **Then** it names
   the file, the heading and the size, and says to give each area its own heading.
4. **Given** a reference file at or over the mark, **When** the check runs, **Then** it names the file and its size
   and says to split it by heading into more than one file, none over the mark.
5. **Given** an ingest paused after two chapters, **When** the skill says what is ready, **Then** it has run the check
   over those chapters and any failure is fixed or named first.

---

### User Story 3 - A module already in a player's folder is checked, not re-ingested (Priority: P2)

A player ingested a large module months ago. After updating, the player or DMC runs the check on it. It names the
reference file over the mark and any section too large to be one area, and prints nothing if all is well. The module
keeps working in the meantime: lookup and resume behave as they did, and no file in `modules/` is rewritten without
the player's word. The player is never told to re-ingest the book.

**Why this priority**: The maintainer's own measured module has a 37k and a 24k reference file today. A check that
only ran at ingest would never reach them, and modules are slow to ingest again (the source is a book, the work is
hours).

**Independent Test**: Build an old-shape synthetic module (a chapter-level pointer, an oversized reference file). Run
the check and `dmc size` on a campaign using it. Pass: the check names the oversized file; `dmc size` and a lookup of
a stat block in that file work exactly as before; nothing under `modules/` has changed.

**Acceptance Scenarios**:

1. **Given** an existing module with an oversized reference file, **When** the check runs, **Then** it names the file
   and the repair, and writes nothing.
2. **Given** an existing module with no problem, **When** the check runs, **Then** it prints nothing and exits 0.
3. **Given** a campaign on an existing module, **When** it is resumed, **Then** the resume works as before whether or
   not the module passes the check.
4. **Given** a module that fails the check, **When** the player asks DMC to fix it, **Then** DMC splits or
   re-heads the files by hand, as for any edit in `modules/`, and runs the check again (FR-012).
5. **Given** a campaign on a module that fails the check, **When** it is resumed, **Then** `dmc size --check` prints
   one line saying so, which the resume leaves for the full save and does not mention to the player (FR-013).

---

### User Story 4 - The rules say which part comes from where (Priority: P2)

The kit's rules say what DMC reads of a module at a resume (the index and the current area), what it looks up in play
(an area, an NPC, a stat block, an item, a spell or a player option the module adds), and what it opens in `source/`
(read-aloud text, exact numbers, puzzle solutions). When a lookup finds no heading, DMC searches the module's files.

**Why this priority**: The saving only appears if DMC does what the rules say, and the rules loaded into every session
must stay lean. Most of this is already written (Phase A); the resume step and the ingest skill are what change.

**Independent Test**: The toolkit tests check that the resume step, the ingest skill and the module rules name real
commands, paths and headings, and that `kit/dm-rules.md` grows by no more than two lines. A live run in a scratch DMC
folder (with a synthetic module) checks that DMC reads the index and one area at a resume and looks up a stat block at
the start of a fight.

**Acceptance Scenarios**:

1. **Given** a fight with a module creature, **When** it starts, **Then** DMC looks the stat block up in the module
   and does not read the whole reference file.
2. **Given** a module item, spell or player option the module adds, **When** it comes up, **Then** DMC looks it up
   by its heading in the module's reference files, not by reading the file.
3. **Given** a lookup that finds no heading, **When** DMC still needs the answer, **Then** it searches the module's
   files with the Grep tool, or opens the cited pages in `source/`, and never treats "not found" as "not there".

### Edge Cases

- **A keyed area that is large by nature** (a dungeon level of 80 rooms keyed in one table): the check flags a section
  over the limit; the fix is a heading per room, or per group of rooms the module itself groups, and the check does
  not insist on one heading per row of a table.
- **A chapter's overview, timeline and milestones**: the resume reads the area, not these, so a scripted event the
  area depends on must be named in the area's section or in `module-progress.md`. The position names more than one
  heading when the area needs them.
- **A heading that two files share** (a creature in a chapter's text and in the monster file): lookup lists both,
  as Phase A does; the check does not flag it. Nor does it flag a subheading each entry repeats under its own parent
  (the measured module's `### PLAYER-SAFE` under every entry of one file); it flags a heading repeated under the
  same parent in one file.
- **A module ingested in part**: the check covers the chapters that exist, and the index's ingest status says the rest
  are not written; an unwritten chapter is not a failure.
- **A chapter digest over the mark**: flagged by the same rule; the fix is to split it into two files at an area
  boundary and to update the index and `module-progress.md`'s pointer. A finished campaign is never affected.
- **A module folder the check cannot read, or a name that is not a module**: one line, exit 1, as every `dmc` failure.
- **A module's `source/` folder** (the PDF, the extracted text, the page images): never read or counted by the check.
- **A sequel's digest or a campaign file**: not module files; this check does not cover them (Phase B's check covers
  digests).
- **Headings inside fenced code blocks or HTML comments** (an ASCII map sketch): not headings, as for lookup.

## Requirements *(mandatory)*

### Functional Requirements

**Resume**

- **FR-001**: At a resume of a module campaign whose chapter status is not finished, `dmc-resume-campaign` MUST have
  DMC read `module-progress.md`, the module's `index.md` and the digest sections `module-progress.md` names under
  "Digest to read before continuing", and no other module file. When the chapter status shows the module finished,
  it reads neither the index nor a digest, as today.
- **FR-002**: The position MUST be able to name the area itself: the heading of the keyed area the party is in, with
  more than one heading when the area needs them (an area and the scripted event it depends on). `module-progress.md`'s
  template and the rules for keeping it MUST say so, and DMC MUST update the pointer when the party moves to another
  area (the rule already says to update the file when the party enters or finishes an area).
- **FR-003**: A pointer that names a whole chapter section (every campaign made before this change) MUST still work,
  and DMC reads what it names. A pointer whose heading matches nothing in the digest MUST NOT stop the resume: DMC finds the
  nearest heading of that chapter file, reads that section, and says out of character that the pointer needs fixing.
- **FR-004**: `dmc size` MUST measure the module part of a resume as the resume now reads it: the index and the
  sections the pointer names, so that its report and its total are the same as what DMC reads, and a pointer that
  names a heading it cannot find says so in one line.

**Ingest and its checks**

- **FR-005**: `dmc-ingest-module` MUST have DMC write every keyed area, every NPC, every stat block, every module
  item, spell, rule or player option, and every handout under a heading of its own, named as the module names it with
  its page in parentheses (`## NAME (PDF n)`, the shape `dmc lookup` already matches), so that a lookup by name prints
  that one thing. A chapter's NPCs are no longer written under one heading.
- **FR-006**: A check MUST report, for an ingested module, each markdown file of the module (its `index.md`, its
  chapter digests and its reference files, never `source/`) that is at or over the mark `dmc size` and `dmc lookup`
  already use (20,000 estimated tokens or 1,600 lines), with the file, its size and the repair: split it by heading
  into more than one file, none at the mark. No new number is introduced for this.
- **FR-007**: The same check MUST report each section of a chapter digest or reference file that is larger than the
  section limit, and each section that holds more than one stat block, naming the file, the heading and the size or
  count, and saying to give each area or stat block a heading of its own. The section limit is 3,000 estimated
  tokens of a heading's own text, recorded with the other budgets (research R2: the measured module's largest section
  is 2,428). What counts as a stat block is the lines a 5e stat block always has, armor class followed by hit points
  (research R4).
- **FR-008**: The same check MUST report a heading repeated at the same level under the same parent heading in one
  file. It MUST NOT report a heading two files share, or one repeated under different parents (research R5).
- **FR-009**: The check MUST print one line per problem, at most a dozen then a count of the rest, nothing when the
  module is sound, and fail in one line (exit 1) for a module that is missing or cannot be read. It never writes,
  moves or deletes anything, builds what it needs in memory and stores nothing, and a measured module exits 0 whatever
  it found, as `dmc size` does.
- **FR-010**: `dmc-ingest-module` MUST run the check at its verify step, over every chapter written so far, and when
  it pauses or finishes, and MUST have DMC fix each problem before it tells the player which chapters are ready. A
  chapter fixed by splitting or re-headed is checked again.
- **FR-011**: The skill MUST say what shape the check holds a module to (one heading per area, NPC, stat block, item
  and handout; no file at the mark; no section over the limit), so that DMC writes that shape in the first place and
  the check confirms it. It adds no way to run the check without `dmc`: `dmc` is a required part of DMC (issue #208).

**Modules already ingested**

- **FR-012**: The check MUST work on a module ingested before this change, with no change to the module. Nothing under
  `modules/` is rewritten, moved or deleted by `dmc install`, `dmc refresh`, `dmc play` or the check, and no campaign
  or module is migrated. A player is never told to re-ingest a book because of this change. The check only reports:
  the kit adds no tool that splits or rewrites a module's files. The repair is the player's, or DMC's on the player's
  word, by editing the files as for any file in `modules/`, then running the check again.
- **FR-013**: `dmc size --check` MUST print one line for a module campaign whose module fails the check, naming the
  module and the number of problems and saying which command lists them; it prints nothing for a sound module or a
  finished one. The resume leaves the line for the full save and does not mention it to the player, as for the other
  size lines; the full save's consistency check reports it until the module is fixed.
- **FR-014**: A module ingested before this change MUST work as it did: `dmc lookup` finds its headings, and the
  resume reads what its pointer names (FR-003).

**The kit's rules**

- **FR-015**: The kit's rules and skills MUST say what DMC reads of a module at a resume (FR-001), what it looks up
  in play (an area, an NPC, a stat block, an item, a spell or a player option the module adds), what it opens in
  `source/` (read-aloud text, exact stat blocks and numbers, puzzle solutions, trap details and treasure, as
  `kit/dm-rules.md` § Running a published module says), and what to do when a lookup finds no heading: search the
  module's files, and open the cited pages. `kit/dm-rules.md` MUST grow by no more than two
  lines; the detail lives in the resume skill, the ingest skill and the template.
- **FR-016**: The change MUST NOT alter what a resume reads of canon, companions, sheets, notes or any other file;
  MUST NOT add a companion play card, a hook that recalls canon, or a gate; and MUST NOT change `dmc recall` or what
  `dmc lookup` searches or prints. The `.docs/decisions.md` entry "Lookup and recall are for play only until Phase
  D" MUST be amended to say that Phase C changes the module part of the resume.
- **FR-017**: The documentation MUST describe the check and the resume's module read (`.docs/dmc-reference.md`,
  `.docs/campaign-files.md`), the decisions and their reasons (`.docs/decisions.md`), the status
  (`.docs/roadmap.md`) and what the player sees (`.docs/user-guide.md` § Published modules). The PR says `Part of
  #179`, and that it adds no migration: no file's required shape changes (a pointer that names a chapter section
  stays valid), only what a new ingest writes and what the resume reads of it.

**Constraints**

- **FR-018**: Anything added to `dmc` MUST build with Native AOT on every platform, with no dependency that needs a
  network or a model, store no index, keep every failure to one line `dmc <command>: <message>`, and keep its output
  small. Tests use synthetic fixtures only: nothing under `modules/` is committed (it is copyrighted), and CI checks
  the check on fixtures.

### Key Entities

- **Module**: an ingested published adventure in `modules/<name>/`: `index.md`, `chapters/`, reference files
  (`monsters.md`, `npcs.md`, `items.md`, and any other top-level markdown) and `source/`.
- **Keyed area**: a place the module numbers and describes; in a digest it has one heading, with its page.
- **Stat block**: one creature's or character's rules entry; in a reference file it has one heading, with its page.
- **Position**: `module-progress.md`'s record of the chapter, the area and the digest headings the next resume reads.
- **Mark**: the size at which a file or a section is near what the Read tool returns in one call (20,000 estimated
  tokens or 1,600 lines).
- **Section limit**: the size above which one heading holds too much to be one area or one stat block.
- **Module check**: the report of the files over the mark, the sections over the limit or holding several stat
  blocks, and the headings repeated in one file.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a synthetic module built to the measured one's shape (38 digest files, one chapter of 13k tokens with
  many keyed areas, a 37k-token reference file), a resume reads the index and one area: no more than the index plus
  about 2k tokens beyond it, where reading the hub chapter's section cost 13.8k.
- **SC-002**: That total does not change when every other chapter of the fixture is made ten times larger.
- **SC-003**: A lookup of one stat block from the fixture's 37k-token file returns about 650 tokens, as in Phase A.
- **SC-004**: The check names every planted problem in the fixtures (a file over the mark, a section over the limit, a
  heading with two stat blocks, a repeated heading) and nothing in a sound fixture, with one line each.
- **SC-005**: On the maintainer's own module (run locally after the merge, not in CI), the check names its three reference files
  at the mark (37.3k, 24.1k and 21.8k, research R0) and nothing else, and nothing is written to `modules/`; after the repair the player chooses, it names none.
- **SC-006**: A check over the module of the measured size (about 1.5 MB of markdown) completes in under one second
  with the release build.
- **SC-007**: On a fixture campaign whose module has an oversized reference file, `dmc size --check` prints exactly
  one module line; after the file is split by hand, it prints none.
- **SC-008**: `kit/dm-rules.md` grows by at most two lines, and no existing campaign needs a migration.
- **SC-009**: A fixture module in the old skill's format (a chapter's NPCs under one heading, written with several
  stat blocks) fails the check on that heading, and the same fixture in the revised format passes it.

## Assumptions

- The issue's decision (markdown stays the truth, `dmc` retrieves, no stored index, no embeddings) and Phases A and B
  are settled and not revisited. Lookup's matching and its refusal to print text cut from extracted PDF text are
  unchanged, so the issue's open question, whether lookup should fall back to a module's extracted page text, stays
  out of scope.
- The module's index stays read whole at every resume of an unfinished module; at 3.3k tokens for a 638-page book it
  is the "about 3k" the issue expects. Shrinking it is not part of this phase.
- No campaign migration is needed: `module-progress.md` keeps its shape and a pointer that names a chapter section
  stays valid (the issue's phase table says none for campaigns). A change to the template's comment and the rules for
  keeping the pointer is what tells new campaigns to name the area; the PR says so, as CLAUDE.md asks of a template
  change.
- The check is a command of `dmc` that DMC and the player can run on any module, and ingest runs it. Its name and
  where it lives were left to the plan: it is `dmc module check NAME` (research R1), with the section limit in
  research R2.
- Resume reads the position's headings with `dmc lookup`, and `dmc size` resolves them with lookup's own matcher, so
  its report matches what DMC reads (FR-004, research R8).
- A repair by splitting a file changes paths: `module-progress.md` and the index name chapter files, but reference
  files are found by lookup across the module's top-level markdown, so splitting `monsters.md` into two files breaks
  no pointer. Splitting a chapter digest does, and the check says to update the pointer.
- `modules/` is the player's own copy of a book: personal, copyrighted and git-ignored, so the spec, the plan and the
  tests quote none of it. Synthetic fixtures stand in, written by the tests.
- One campaign is played at a time in a DMC folder; a module can be shared by campaigns (a chain), and the check
  covers the module once, not per campaign.
- The 5e stat block lines (armor class, hit points) are enough to recognise a stat block in modules for the editions
  DMC supports (2014 and 2024); a module in another system, or one that prints stat blocks another way, relies on the
  section limit alone.
