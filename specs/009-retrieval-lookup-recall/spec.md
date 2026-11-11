# Feature Specification: Look Up a Section and Recall a Fact, Without Reading Whole Files

**Feature Branch**: `009-retrieval-lookup-recall`

**Created**: 2026-11-10

**Status**: Draft

**Input**: User description: "Phase A of issue #179 (steps 1 to 3 of its plan): read-only retrieval over the markdown,
and the rules for when DMC uses it. Markdown stays the source of truth, play never depends on dmc, no stored index,
Native AOT. (1) Budgets enforced in play folders: dmc size and the lean-notes migration shipped in 0.3.1, so say what
remains (the player's update and migration, and a measurement of the resume against the issue's 76k). (2) dmc lookup
<source> <heading>: returns one section exactly, from a module digest, the rules reference or a campaign file. (3) dmc
recall <campaign> <terms>: keyword-ranked search (BM25 over line units, with stemming) across the campaign and whatever
it links to, ranked by authority: canon, archives, sheets, session logs, then the module as printed. Each hit carries
its campaign, file and session; output capped at about 1.5k tokens; import/ and exports/ excluded; long lines trimmed
around the match. Today a campaign links only through its single Predecessor line; the list of links is Phase B. The
kit's rules say when DMC uses lookup and recall in place of reading or Grep. Evidence and recall-test figures are in
issue #179."

## Background

Issue #179 is a tracking issue in four phases; this spec is Phase A, its steps 1 to 3. Its decision, made in the issue
and not reopened here, is that the markdown files stay the source of truth and `dmc` does the retrieval over them: no
vector database, no graph database, no SQLite store and no stored index. The whole corpus parses and indexes in well
under a second on every call, so an index built on the fly cannot go stale.

The issue measured the maintainer's play folder: a 39-session sequel (Rorn's Landing), its 46-session predecessor (The
Silver Egg, built on the module Peril in Pinebrook), and a 638-page ingested module.

- **A resume of the sequel reads about 76k tokens.** `dm-notes.md` alone is 20.4k against a 10k budget, `game-state.md`
  6.6k against 4k, `summary.md` 5.1k against 3k. The play folder runs `dmc` 0.2.1, which has no `size` command, so the
  resume's size check cannot run there, and the lean-notes migration (004) has not been applied.
- **Looking a fact up is expensive and unreliable.** DMC finds a returning subject by Grep across canon, the archives,
  the sheets and the session logs. Lines run to 1,576 characters in the module digests and 3,191 in DM notes, so one
  line-based hit can cost several hundred tokens. The predecessor holds 1.45 MB of markdown, including a 431 KB imported
  transcript that a search must leave out or rank last.
- **A large module's reference files exceed what one Read returns.** One monster file is about 37k tokens, over the
  25k Read cap; each stat block in it is its own section of about 650 tokens.
- **Keyword ranking finds most facts.** In the issue's recall test (30 fact lines drawn at random from both campaigns,
  two questions each, plain BM25 over line units across 1.55 MB, output capped at about 1.5k tokens), the answer was in
  the output for 28 of 30 named questions (29 with better word-stemming) and 22 of 30 indirect ones. Session logs were
  the weak spot: curated lines retrieve better than narrative. Reading and indexing everything took 0.2 to 0.5 seconds
  in a Python prototype.

Phase A adds no new campaign file and changes no file's shape, so it needs no migration. Phase B (continuity links,
the sequel import, legacy files) depends on issue #180 and comes later; Phase C (module lookup rules and ingest
checks) and Phase D (resume changes, behind a gate) build on the two commands specified here.

## Clarifications

### Session 2026-11-10

Neither question was settled in earlier conversations (a search of every transcript, the issue and `.docs/decisions.md`
found no decision); the user accepted the recommended answers.

- Q: The maintainer's rules reference is the SRD 5.2.1 PDF with extracted page text and an index of page ranges, not
  headed markdown. What does lookup do with it? → A: Lookup searches a markdown rules reference like any source; for a
  PDF it prints the matching rows of `rules/index.md` with their pages, and DMC reads them as today. It never cuts an
  entry out of extracted text, whose entry ends are guesses and whose digits can drop (FR-008). (Plan: when no index
  row matches, the pages whose extracted text holds the words, as numbers only.)
- Q: Does recall reach a predecessor's module (The Silver Egg's Peril in Pinebrook, from Rorn's Landing)? → A: Yes,
  ranked below every campaign file, so canon still wins. Recall already reads each predecessor's options, so its
  module costs nothing more, and a printed name or number only that module holds is otherwise out of reach. This
  takes the predecessor part of the issue's step 6 into Phase A; Phase B widens it to every kind of link (FR-011,
  FR-013).

The next five were found with `/speckit-clarify`'s taxonomy and checked against the transcripts, the issue, the
decisions and the recall-test prototype (the issue-179 session's `recall.py`, `sample.py`, `queries.py`). None was
settled in the user's words; the user accepted the answers below.

- Q: How much does authority count against keyword relevance? → A: A moderate weight per authority tier on the
  relevance score, tuned so the recall test's figures do not fall. Not a tie-break only (the prototype reached its
  figures with no weighting), and not hard tiers (a strong session-log match may beat a weak canon one) (FR-013).
- Q: Does a hit show context around its line? → A: No: one trimmed line with its labels and citation. The prototype's
  figures rest on single lines, and context would roughly halve the hits that fit (FR-014).
- Q: How are the commands written? → A: Recall takes an optional campaign, defaulting to the active one as `dmc size`
  and `dmc campaign standard` do, then the words. Lookup takes a source (a campaign folder, a module folder, or
  `rules`) and a heading; a name that is both a campaign and a module is refused in one line asking which (FR-024).
- Q: Are the prototype's matching settings the baseline? → A: Yes, as the plan's starting values, not spec numbers:
  its common-word list, its suffix stemming, skipping lines under 25 characters and headings, a 700-character trim and
  a 6,000-character cap. The plan must reproduce the test's figures with them before tuning (FR-012, FR-015, FR-016).
- Q: Is the recall test rebuilt as a repeatable script, and where does it live? → A: The harness goes in the
  repository and takes a questions file as an argument; the questions file, which quotes campaign facts, stays in the
  maintainer's play folder and is never committed (FR-025, SC-002).

Asked in `/speckit-clarify`:

- Q: Does Phase A change how the resume and the full save search, or only what DMC does in play? → A: Play only. The
  resume and the full save keep their current reads and searches; moving them to recall or lookup is Phase D's,
  behind its gate (FR-020a).
- Q: Must the play-folder update and size measurement be done before this PR merges? → A: No. The PR merges on CI and
  the synthetic tests; the size total and the recall test's figures go on issue #179 once the maintainer has updated
  and migrated the play folder (FR-001, SC-001, SC-002).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The play folder runs the size budgets that already exist (Priority: P1)

The maintainer, playing Rorn's Landing in their DMC folder, updates `dmc` from 0.2.1 to the current release. The next
resume runs the size check it already has, says the campaign is behind the kit's standard, and offers the migration.
The maintainer accepts; migration 004 trims DM notes and game state to their lean shape, adds the Predecessor line
naming The Silver Egg, and verifies that nothing was lost. A size report then shows what a resume reads now, against
the 76k the issue measured.

**Why this priority**: The issue's plan puts it first because it saves about 15k tokens on every resume with no new
feature. It also gives the sequel the Predecessor line that recall (User Story 3) follows. Nothing here is built: it is
the player's update, migration and measurement, and the toolkit's part is to say what to do and to record the result.

**Independent Test**: In the maintainer's play folder, update `dmc`, resume Rorn's Landing, accept the migration, and
run the size report. Pass: the campaign reports as current, the Predecessor line names `the-silver-egg`, and the
report's total is recorded beside the 76k baseline.

**Acceptance Scenarios**:

1. **Given** the play folder on `dmc` 0.2.1, **When** the maintainer updates `dmc`, **Then** the installed version is
   the latest release and the DMC folder's rules, skills and migrations are refreshed.
2. **Given** Rorn's Landing behind the kit's standard, **When** it is resumed, **Then** DMC says so in one line and
   offers the migration, and on the maintainer's word runs it with a checked backup first.
3. **Given** the migration verified, **When** the size report runs, **Then** its total is posted on issue #179, and
   each file over its budget is either within it after the next full save or listed with why
   it stays.

---

### User Story 2 - DMC reads one section, not a whole file (Priority: P1)

In play, DMC needs the stat block of a monster from the module, a keyed area of the current chapter, an entry of the
rules reference, or one section of a campaign file (a sheet's Playing them section, canon's Prices section). It asks `dmc` for
that section by its heading and gets exactly that section, from the heading down to the next heading of the same or a
higher level, and nothing else.

**Why this priority**: The cheapest correct read is the one section that holds the answer. A whole reference file can
be over the Read cap (the 37k-token monster file), and Grep returns lines, not the stat block they belong to. This is
the command Phase C's module rules and Phase D's slimmer resume rest on.

**Independent Test**: Build fixtures (a module folder with chapter digests and a monster file whose stat blocks are
`##` sections with page references, a rules reference, and a campaign) and look up headings in each. Pass: each lookup
prints exactly the bytes of the section, with its file and line range, and each failure is one line.

**Acceptance Scenarios**:

1. **Given** a module whose `monsters.md` holds `## GOBLIN BOSS (PDF 212)`, **When** DMC looks up "goblin boss" in that
   module, **Then** it gets that section exactly: the heading line and every line down to the next heading of level 2
   or higher, unchanged.
2. **Given** a heading that appears in two files of the source, **When** DMC looks it up, **Then** it gets one line per
   match naming the file and the full heading, and no section, so it can ask again more precisely.
3. **Given** a heading that matches nothing, **When** DMC looks it up, **Then** it gets one line saying so, naming at
   most a few of the nearest headings in that source.
4. **Given** a section larger than one Read returns, **When** DMC looks it up, **Then** it gets one line naming the
   file and the section's line range instead of the text, so it can read it in parts.
5. **Given** a campaign as the source and a heading such as "Prices, debts and promises", **When** DMC looks it up,
   **Then** it gets that section of the file that holds it.

---

### User Story 3 - DMC recalls a fact from everything the campaign knows (Priority: P1)

A subject returns: an NPC from forty sessions ago, a price agreed in the predecessor campaign, a ruling. Today DMC
Greps several files and folders and reads what comes back, long lines included. Instead it asks `dmc` to recall the
words, and gets the best-matching lines from the campaign, its predecessor and its module, best first, each labelled
with its campaign, file and session, trimmed to the part that matches, in at most about 1.5k tokens.

**Why this priority**: This is where the slips that hurt come from (a name, a price, a ruling), and where the cost
grows with the campaign's length and its links. Ranking puts the curated lines (canon, archives, sheets) ahead of
narrative, which the recall test showed retrieve better.

**Independent Test**: Build a fixture chain: a campaign whose Predecessor line names a second campaign that uses a
module, with facts planted in canon, an archive, a sheet, a session log, the predecessor and the module, and noise in
`import/`, `exports/` and `backups/`. Recall each planted fact by its words and by a paraphrase. Pass: the planted
line is in the output, the labels are right, nothing from the excluded folders appears, and the output is within its
cap.

**Acceptance Scenarios**:

1. **Given** a fact in the campaign's `canon.md` and the same subject in a session log, **When** DMC recalls the
   subject, **Then** both may appear, and when they match equally well the canon line comes first.
2. **Given** a campaign whose Predecessor line names another campaign folder, **When** DMC recalls a subject only the
   predecessor holds, **Then** the hit is returned, labelled with the predecessor's name.
3. **Given** a hit inside a line of 3,000 characters, **When** it is shown, **Then** only the part around the matching
   words is shown, marked as trimmed, with enough of the line to read the fact.
4. **Given** a session-log hit, **When** it is shown, **Then** it carries its session and scene (`sN.M`); **Given** a
   canon or sheet line that cites a session, **Then** it carries that citation.
5. **Given** matching lines in `import/`, `exports/` or `backups/`, **When** DMC recalls, **Then** none of them appears.
6. **Given** more matches than fit, **When** DMC recalls, **Then** the output stops at its cap and its last line says
   how many more matched.
7. **Given** a hit from a file only the DM may see (DM notes and their archive, the module), **When** it is shown,
   **Then** it is marked as such, so DMC does not tell the player what the character has not learned.

---

### User Story 4 - DMC uses the two commands, and still plays without them (Priority: P2)

The kit's rules tell DMC when to look up a section and when to recall a fact in place of reading or Grep: when a
subject returns, when a stat block, area or rules entry is needed, and when a sequel's earlier campaign is involved.
If `dmc` is missing, fails, or returns nothing useful, DMC falls back to reading and searching as it does today, so play
never depends on `dmc`.

**Why this priority**: The commands save nothing until DMC uses them, but the rules that say so are loaded into every
session, so they must stay lean, and the fallback keeps the game running on any setup.

**Independent Test**: The toolkit tests check that the rules and skills name the commands correctly and that the
always-loaded rules grow by no more than the limit below. A live run in a scratch DMC folder checks that DMC recalls a
returning subject instead of reading whole files, and falls back when `dmc` is taken off the PATH.

**Acceptance Scenarios**:

1. **Given** a returning NPC, **When** DMC prepares to bring them back, **Then** it recalls them before narrating, and
   reads their sheet's Playing them section (Voice, Names they use) by lookup or Read as today.
2. **Given** a monster stat block in a module, **When** a fight with it starts, **Then** DMC looks up that stat block
   instead of reading the whole reference file.
3. **Given** `dmc` fails or is missing, **When** DMC needs the fact, **Then** it searches and reads the files as today,
   and the game goes on.
4. **Given** a recall that does not hold the answer, **When** DMC still needs it, **Then** it recalls again with other
   words (a name for a role, a place for a person) or searches as today, and never treats "not found" as "not true".

### Edge Cases

- **The Predecessor line names a folder that is missing**: recall searches the rest and says so in one line, as the
  resume already does.
- **A Predecessor chain** (the predecessor has a predecessor): recall follows it to the end. A loop is followed once.
- **A campaign behind the standard has no Predecessor line**: recall covers the campaign and its module only.
- **A query of only common words** ("the", "and"): one line saying there is nothing to search for.
- **A word's other forms** ("ruling", "rulings"; "teach", "teaching"): matched by stemming. The prototype's simple
  stemmer misses some pairs ("horse" and "horses"); the fallback covers them (FR-021).
- **The imported transcript** in a predecessor's `import/`: never searched.
- **Headings inside fenced code blocks**: not headings.
- **A section with sub-sections**: returned whole, sub-sections included.
- **A file that cannot be read**: recall skips it and names it in one line; lookup fails in one line.
- **No active campaign, or a staging folder named**: refused in one line, as `dmc size` does.
- **A misspelled campaign name**: not a campaign folder, so it is read as a search word in the active campaign
  (FR-024). DMC names the campaign exactly, as the kit's rules say.
- **A hit in a line that is mostly a table row**: shown trimmed like any line.
- **The rules reference is a PDF with extracted text, not markdown**: lookup prints the matching index rows and their
  pages (FR-008).
- **A predecessor's module is the same as the campaign's own**: searched once, at the campaign's own module's rank.

## Requirements *(mandatory)*

### Functional Requirements

**Step 1: the existing budgets in the play folder**

- **FR-001**: The feature MUST record the steps that bring the maintainer's play folder up to the current release
  (update, resume, migration 004 with its backup and verify). Once the maintainer has done them, the size report's
  total for Rorn's Landing is posted on issue #179 beside the 76k baseline. No code is added for this step, and the PR
  does not wait for it.

**Lookup**

- **FR-002**: `dmc` MUST provide a read-only lookup that, given a source and a heading, prints exactly one section:
  the heading line and every line after it down to, and not including, the next heading of the same or a higher level,
  or the end of the file, byte for byte. It never writes, moves or deletes anything.
- **FR-003**: A source MUST be one of: an ingested module (its digest and reference files: `index.md`, `chapters/`,
  and the other markdown files at its top level, such as `monsters.md`, `npcs.md` and `items.md`), the rules reference, or a campaign (its markdown files, outside `import/`,
  `exports/` and `backups/`). A module's `source/` folder, its extracted text included, is not searched.
- **FR-004**: A heading MUST match when its text equals the request ignoring case, punctuation, extra spaces, markdown
  emphasis, and a trailing page reference in parentheses (`(PDF 212)`, `(p. 34)`). If no heading matches that way, a
  heading that contains every word of the request matches.
- **FR-005**: When more than one heading matches, the lookup MUST print one line per match (file and full heading),
  at most a few with a count of the rest, and no section.
- **FR-006**: When no heading matches, the lookup MUST fail in one line naming the source and at most a few of the
  nearest headings.
- **FR-007**: When the section is larger than one Read returns (the same mark `dmc size` uses), the lookup MUST print
  one line with the file and the section's line range instead of the text.
- **FR-008**: A rules reference kept as markdown MUST be searched by lookup like any other source. For a rules
  reference kept as a PDF (with or without extracted text), lookup MUST instead print the rows of `rules/index.md`
  that match the request, with their pages, for DMC to read as it does today. When no index row holds the request and
  the PDF's text was extracted, lookup prints instead the page numbers whose text holds it, at most ten, and no text.
  Lookup never cuts a rules entry out of
  extracted page text: an entry's end is a guess in plain text, and extracted text can drop digits.
- **FR-009**: A printed section MUST be preceded by one header line naming the file and the section's first and last
  line.

**Recall**

- **FR-010**: `dmc` MUST provide a read-only recall that, given a campaign and some words, ranks the lines of every
  file in its scope by how well they match, and prints the best ones. It builds what it needs on every call and stores
  nothing.
- **FR-011**: The scope MUST be the campaign's markdown files; each campaign its Predecessor line names, followed to
  the end of the chain; and the digest and reference files of the module of each of those campaigns that has one (a
  predecessor's module included, as Peril in Pinebrook is for Rorn's Landing). It MUST leave out `import/`,
  `exports/`, `backups/`, staging folders, map JSON and map pages, and a module's `source/` folder.
- **FR-012**: Matching MUST be by words, with the forms of a word matched by stemming and common words ignored, and
  ranked by keyword relevance (BM25 over line units, as the issue's test used), so that a line holding the rarer words
  of the request ranks above one holding only common ones.
- **FR-013**: Ranking MUST also weigh authority, in this order: the campaign's `canon.md`; its archives
  (`canon-archive.md`, `dm-notes-archive.md`) and the sheets' `-history.md` files; its sheets and `minor-npcs.md`; its
  other story files (`summary.md`, `dm-notes.md`, `game-state.md`, `module-progress.md`, a place's notes); its session
  logs; then the predecessor campaigns in the same order, nearest first; then the modules as written, the campaign's
  own first, below every campaign file. Authority MUST act as a moderate weight per tier on the relevance score, not
  as a tie-break only and not as hard tiers: of two hits that match about equally well, the more authoritative comes
  first, and a strong match in a session log can still rank above a weak one in canon. The weights start at the plan's
  values. If the recall test (SC-002), run after the merge, shows figures below the prototype's, a follow-up change
  tunes them.
- **FR-014**: Each hit MUST be one line carrying the campaign's folder name, the file, the line number, and the
  session: the `sN.M` of the scene a session-log line sits in, or the session the line itself cites. A hit from DM
  notes, their archive, `module-progress.md`, an NPC sheet's Secrets section or the module MUST be marked as DM-only. A hit MUST NOT bring
  neighbouring lines with it; DMC looks up or reads the file when it needs more.
- **FR-015**: A line longer than a set width MUST be trimmed to the part around the matching words, with a mark where
  text was cut, so that a 3,000-character line costs no more than a short one.
- **FR-016**: The whole output MUST stay within about 1,500 estimated tokens (bytes ÷ 4, as `dmc size` counts). When
  more lines match than fit, the last line MUST say how many more matched.
- **FR-017**: A missing predecessor folder or an unreadable file MUST be named in one line and skipped; the rest is
  still searched. A request with no word left to search, a named staging folder, or no active campaign when none is
  named, MUST fail in one line.

**Both commands**

- **FR-018**: Both commands MUST follow the rules every `dmc` command follows: the DMC folder found the usual way, an
  optional DMC folder argument, UTF-8 output with LF line ends, failures as one line `dmc <command>: <message>` with the
  usual exit codes, and output kept small.
- **FR-019**: Both commands MUST build with Native AOT on every platform, with no new dependency that needs a network
  or a model, and with nothing written to disk.
- **FR-024**: Recall MUST take an optional campaign folder name, defaulting to the active campaign as `dmc size` does,
  then the words to search for. Lookup MUST take a source (a campaign folder name under `adventures/`, a module folder
  name under `modules/`, or the word `rules`) and then the heading. A name that is both a campaign and a module MUST be
  refused in one line asking which is meant.

**The recall test**

- **FR-025**: The repository MUST hold a repeatable recall test: a harness that runs recall for each question in a
  questions file given as an argument and reports, per question style, how many answers were in the output and how
  many were the first hit. The questions file quotes campaign facts, so it MUST live in the maintainer's play folder
  and never be committed. The harness MUST NOT run in CI; CI checks recall on synthetic fixtures. The maintainer runs
  it after the play folder's migration (the Predecessor line it adds is what reaches The Silver Egg) and posts the
  figures on issue #179; the PR does not wait for them.

**The kit's rules**

- **FR-020**: The DM rules and skills MUST say when DMC recalls (a returning subject, a fact from an earlier session or
  campaign, before stating a name, price or ruling it is not sure of) and when it looks up (a stat block, keyed area,
  item, rules entry or one section of a campaign file), in place of reading whole files or searching with Grep.
- **FR-020a**: These rules MUST apply to play only. `dmc-resume-campaign` and `dmc-campaign-full-save` MUST keep their
  current reads and searches (the resume's canon search when canon is over its limit, the full save's targeted
  checks): changing what a resume reads is Phase D's, behind its gate.
- **FR-021**: The same rules MUST keep the fallback: when `dmc` is missing or fails, or a recall does not hold the
  answer, DMC searches and reads the files as before; a fact not found is not a fact denied.
- **FR-022**: The rules loaded into every session (`kit/dm-rules.md`) MUST grow by no more than two lines; the detail
  lives in the skills that need it and in `dmc`'s help.
- **FR-023**: The documentation MUST describe both commands (`.docs/dmc-reference.md`), the decisions and their
  reasons (`.docs/decisions.md`), the status (`.docs/roadmap.md`), and what the player sees, if anything
  (`.docs/user-guide.md`). The PR says `Part of #179`.

### Key Entities

- **Source**: what a lookup searches: one ingested module, the rules reference, or one campaign.
- **Section**: a heading and the lines under it down to the next heading of the same or a higher level.
- **Recall scope**: the campaign, the chain of campaigns its Predecessor lines name, and the module of each of them,
  minus the excluded folders.
- **Line unit**: one line of a markdown file, the unit recall ranks. It carries its campaign, file, line number,
  authority tier and session.
- **Authority tier**: the rank a file's lines get for being curated (canon, archives, sheets) or narrative (session
  logs), own campaign or predecessor, campaign or module.
- **Hit**: a ranked line unit as printed, trimmed around its matching words.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After the update and migration, the size report for Rorn's Landing shows a resume of at most about 61k
  estimated tokens, against the 76k baseline (the issue's estimate of about 15k saved), or the report's flags say why
  not.
- **SC-002**: On the issue's recall test (30 facts, two questions each, the same fixed seed), run by the harness of
  FR-025 against the maintainer's play folder, the answer is in the output for at least 28 of 30 named questions and 22 of 30 indirect
  ones, the figures the prototype reached.
- **SC-003**: Every recall output on that test is within its cap of about 1,500 estimated tokens.
- **SC-004**: A recall over the measured play folder (both campaigns and the module, a few megabytes of markdown)
  completes in under one second with the release build.
- **SC-005**: A lookup of a stat block in the 37k-token monster file returns that stat block alone, about 650 tokens,
  where reading the file today takes two Reads and exceeds the cap.
- **SC-006**: The always-loaded rules grow by at most two lines.
- **SC-007**: With `dmc` taken off the PATH, a live run still brings back a returning NPC correctly by searching the
  files.

## Assumptions

- The issue's decision (markdown stays the truth, `dmc` retrieves, no stored index, no embeddings) is settled and not
  revisited here.
- "Phase A" adds no campaign file and changes no file's shape, so no migration ships with it (CLAUDE.md asks the PR to
  say so).
- The Predecessor line is the only link a campaign has today (migration 004 adds it where missing). The list of links
  (continues, side campaign of, shares a world with) is Phase B and will widen recall's scope without changing its
  output.
- Role aliases and the names-and-roles register (the issue's step 9) are not part of this phase; a paraphrase that
  recall misses is handled by the fallback (FR-021).
- Recall is for DMC, not the player: its output lands in DMC's context, which already holds the DM's secrets. Marking
  DM-only hits (FR-014) supports the existing rule "Say only what the listener could know"; it is not a new access
  rule.
- The recall test's questions were written by someone who knew the answers, so SC-002's figures are optimistic, as the
  issue says. Its fact set and questions live with the maintainer's private campaign data, so SC-002 is checked locally,
  after the merge if need be; CI checks the same behaviour on synthetic fixtures. SC-001 and SC-002 are therefore not
  merge conditions; SC-003 to SC-006 are checked before the merge, and SC-007 by a live run.
- Step 1's measurement uses the maintainer's play folder at `D:\Claude\dmc`, where `dmc` 0.2.1 is installed and Rorn's
  Landing has no `.standard` file and no Predecessor line.
- "About 1.5k tokens" and "a set width" are tuned in the plan against the recall test; the spec fixes only that both
  exist and the order of magnitude. The plan starts from the prototype's settings (its common-word list, suffix
  stemming, lines under 25 characters and headings skipped, a 700-character trim, a 6,000-character cap) and must
  reproduce the test's figures with them before tuning anything.
