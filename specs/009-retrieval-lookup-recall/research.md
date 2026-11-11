# Research: Look Up a Section and Recall a Fact

Phase 0 of [plan.md](plan.md). Each entry is a decision, why, and what else was weighed. The prototype named below is
the issue-179 session's `recall.py`, `recall_alias.py`, `sample.py` and `queries.py` (scratchpad of session
`962b38f5`), which produced the recall-test figures in issue #179 and the spec.

## R1. Ranking: BM25 over line units, with a weight per authority tier

- **Decision:** Okapi BM25 with the prototype's parameters (k1 = 1.4, b = 0.6) over line units, multiplied by a weight
  for the line's authority tier. Starting weights:

  | Tier | Files | Weight |
  |---|---|---|
  | 1 | `story/canon.md` | 1.30 |
  | 2 | `story/canon-archive.md`, `story/dm-notes-archive.md`, `character-sheets/*-history.md` | 1.20 |
  | 3 | `character-sheets/*.md` (other than history files), `minor-npcs.md` | 1.15 |
  | 4 | the other campaign markdown: `summary.md`, `dm-notes.md`, `game-state.md`, both `player-options.md`, `module-progress.md`, `maps/*.md` | 1.05 |
  | 5 | `story/sessions/*.md` | 1.00 |

  A predecessor's lines get the same tier weight times 0.95 per step back the chain. A module's lines get 0.80, and a
  predecessor's module 0.80 × 0.95 per step. Scores are compared to two decimals; a tie goes to the higher tier, then the
  nearer campaign, then file and line order, so the output is the same on every run.
- **Rationale:** The clarification asked for a moderate weight, tuned so the test's figures do not fall. The prototype
  reached 28/30 and 22/30 with no weighting, and a 1.3 : 1.0 spread lets a canon line win only against a session-log
  line that matches about as well. The weights are constants in one class. The harness (R12) checks them on the play
  folder after the migration, which the PR does not wait for; if the figures fall below the prototype's, a follow-up
  change tunes the weights (or the trim, R4) until they do not.
- **Alternatives:** Tier first, score second (rejected in the clarification: a strong session-log match must be able to
  win). A tie-break only (rejected: it would never lift a canon line over a slightly better narrative one, and curated
  lines are what the test showed to be right more often). Learned weights (no training data; out of scope).

## R2. Words: the prototype's tokens, common-word list and stemming

- **Decision:** A word is a run of letters, digits and apostrophes in the lower-cased line (typographic
  apostrophes folded to `'`), longer than one character and not in the prototype's common-word list (about 100 English
  function words plus *say, said, tell, told, get, got, one, thing, things*). Stemming is the prototype's "better
  stemming" variant (`recall_alias.py`): drop a trailing `'s`, then the first of `ation`, `ion`, `ing`, `ed`, `es`, `s`
  that leaves at least four letters. Non-ASCII letters (accented names) are kept as letters, so "Rörn" is one word, and apostrophes at a word's ends are dropped, so `'Silver Hand'` and `Knights'` match `silver` and `knight` (both changes from the prototype; the second from PR #187's code review).
  Known gaps, kept for now: a word and its plural can stem apart ("horse" stays, "horses" becomes "hors"), and short
  stems are left whole ("ruled"); the test measured the prototype with these gaps.
- **Rationale:** These are the settings the spec's figures came from (29/30 named with the better stemming). The
  clarification fixed them as the plan's starting values, to be reproduced before anything is tuned. A small suffix
  stripper needs no dictionary, no package and nothing AOT could object to.
- **Alternatives:** A Porter or Snowball stemmer (more code, and the test showed the gain is small: one question). The
  prototype's role aliases (`recall_alias.py`'s table) are left out: they were hand-written for the test's questions,
  and the names-and-roles register is the issue's step 9 (Phase D).

## R3. Line units: what is ranked

- **Decision:** Each line of a file in scope, trimmed of spaces, is a unit unless it is under 25 characters, a heading,
  wholly inside an HTML comment (comment text is removed first, on one line or across several, since the templates'
  guidance and examples live in comments; fences and comments are found in one pass, an opener inside an inline code
  span or one that never closes is text, and `<!-->` closes at once), a table separator (`|---`), or inside a fenced code block. A unit keeps its campaign, file,
  1-based line number, tier, the scene it sits in (session logs) and the first `sN.M` citation it holds.
- **Rationale:** The prototype's filter. Short lines and headings carry no fact by themselves; a stat block's name is a
  heading, which lookup (R7) finds and recall need not.
- **Alternatives:** Paragraph or scene units (rejected in the issue: line units ranked best, and a scene is too big for
  the cap). Keeping headings (adds noise; their words recur in every line under them).

## R4. Trimming a long line around its match

- **Decision:** A unit of up to 700 characters is shown whole. A longer one shows a 700-character window placed to hold
  the most distinct matched words (the window starting at the first matched word whose window holds the most), widened
  to word boundaries, with `…` where text was cut on either side.
- **Rationale:** FR-015 asks for the part around the match; the prototype cut the first 700 characters, which is the
  same window whenever the match is early and better otherwise. The harness checks that the figures do not fall.
- **Alternatives:** A sentence window (sentence ends are unreliable in these files: lists, tables, semicolons). A
  smaller window (fits more hits, but the prototype's 700 is what the figures rest on).

## R5. The cap and the "more" line

- **Decision:** The whole output, labels included, stays within 6,000 bytes of UTF-8 (about 1,500 estimated tokens by
  `dmc size`'s bytes ÷ 4). Hits are added best first until the next would pass the cap; then one line,
  `(N more lines matched)`, ends the output. The notice lines of R9 count toward the cap.
- **Rationale:** FR-016. The prototype capped the hit text at 6,000 characters without labels; counting the labels
  costs about one hit in ten. If the harness shows a fall, the trim (R4) is lowered before the cap is raised.
- **Alternatives:** A fixed number of hits (a short line and a 700-character line would cost the same slot).

## R6. Recall's scope and how links are read

- **Decision:**
  - **The campaign**: every `*.md` under `adventures/<campaign>/`, recursively, except `import/`, `exports/`,
    `backups/` and any folder whose name starts with a dot. Map JSON and the map page files are not markdown and are
    never read.
  - **Predecessors**: the campaign's `player-options.md` line `- **Predecessor**: <value>`. The value is read with
    backticks, a trailing `/`, a leading `adventures/` and an HTML comment removed; `none` or empty means no link. The
    chain is followed with a set of folders already visited, so a loop is followed once.
  - **Modules**: each campaign in the chain whose `module-progress.md` has `- **Module folder**: `modules/<name>/``.
    The module's `index.md`, `chapters/*.md`, `monsters.md`, `npcs.md`, `items.md` and any other `*.md` at its top
    level, never `source/`. A module reached twice is searched once, at its nearest rank.
- **Rationale:** FR-011 and the second clarification. The Predecessor line's shape is the template's
  (`kit/templates/campaign/player-options.md` line 13); migration 004 adds it to older campaigns.
- **Alternatives:** Searching every campaign in `adventures/` (would mix unrelated campaigns' facts). Reading the
  module's extracted page text (rejected: digits drop; the digests were corrected from page images).

## R7. Lookup: headings, sections and matching

- **Decision:**
  - **Headings** are ATX headings (`#` to `######` after at most three spaces, then a space) outside fenced code
    blocks (```` ``` ```` or `~~~`). Setext headings are not used in the kit's templates and are not recognised.
  - **A section** runs from its heading to the line before the next heading of the same or a higher level, or to the end
    of the file.
  - **Matching** compares a normalised form: lower case, markdown emphasis and links' brackets removed, a trailing
    parenthesis that starts with `PDF`, `p.`, `pp.` or `page` removed, punctuation turned into spaces, spaces
    collapsed. Exact normalised equality first; if no heading is equal, every heading containing every word of the
    request (R2's words and stems).
  - **One match** prints the section. **Several** print one line each (`<file>:<line>: <heading>`), at most 8, then
    `(+N more)`. **None** fails with the source and the five headings sharing the most words with the request.
  - **Too large**: a section of 20,000 estimated tokens or 1,600 lines (`Budgets.NearCapTokens`, `NearCapLines`, the
    mark `dmc size` uses) prints one line with the file and its line range instead.
- **Rationale:** FR-002 to FR-009. The two-step match lets "goblin boss" find `## GOBLIN BOSS (PDF 212)` and "prices"
  find `## Prices, debts and promises` without fuzzy matching that could pick the wrong stat block.
- **Alternatives:** Fuzzy or edit-distance matching (a near miss would print a wrong stat block as if it were the right
  one, against "consistency of numbers above all"). Matching the first heading found (silently wrong on duplicates).

## R8. Lookup's sources and narrowing to one file

- **Decision:** `SOURCE` is a campaign folder in `adventures/`, a module folder in `modules/`, or `rules`, optionally
  followed by `/<path>` inside it (`rorns-landing/story/canon.md`, `the-crooked-moon/monsters.md`) to search that file
  only. A name that is both a campaign and a module fails in one line naming both and the narrowing form; a name that is
  neither fails naming the campaigns, modules and `rules` it could be (at most a few of each). A campaign as source
  searches the same files as R6's campaign part; a module, R6's module part.
- **Rationale:** FR-024 and FR-005: when two files hold the same heading, the narrowing form is how DMC asks again
  precisely, and it is also how a skill names "the Playing them section of this sheet".
- **Alternatives:** A `--file` option (one more option to learn; the path form reads as what it is).

## R9. A rules reference kept as a PDF

- **Decision:** `rules/`'s markdown other than `index.md` (outside `extracted/`) is searched like any source. It is a
  PDF reference when a `*.pdf` is anywhere under it (an `extracted/` folder alone can be a leftover, `dmc-setup-rules` says); then an exact markdown heading wins, and otherwise, when no markdown heading matches (or there is no
  markdown), lookup searches `rules/index.md` by lines: every line (a table
  row, a list item, a paragraph) that holds every word of the request is printed as `rules/index.md:<n>: <line>`,
  trimmed as R4 does, at most 8. If no index line matches and `rules/extracted/text/page-NNN.txt` exist, it prints one
  line naming the pages whose text holds the request as a phrase (case and spacing ignored), at most 10, for DMC to read
  in the PDF. It never prints text from the extracted pages.
- **Rationale:** The first clarification: index rows and pages, never an entry cut out of extracted text. The index's
  sections and letter breakdowns do not name single entries ("Fireball" is in no row), so without the page step the
  most common rules lookup, a named spell or condition, would always come back empty; a page number is a pointer, not
  content, so a dropped digit in the extracted text cannot become a stated rule.
- **Alternatives:** Index rows only (would fail on most named entries). Returning the entry text (rejected in the
  clarification).

## R10. Command lines, output and exit codes

- **Decision:**
  - `dmc recall [NAME] WORDS... [--folder PATH]`. The first argument is the campaign when it names a folder in
    `adventures/` and at least one more argument follows; otherwise every argument is a word and the campaign is the
    active one. The words may come as one quoted argument or several.
  - `dmc lookup SOURCE HEADING... [--folder PATH]`; the heading's words may be one argument or several.
  - Both parse arguments through `Argparse` like `dmc size`, take `--help`, find the DMC folder the usual way, and print
    UTF-8 with LF line ends. A recall hit is one line:
    `<campaign> <file>:<line> [<sN.M>] [DM] | <text>`. Lookup prints a header line `<file>:<first>-<last>` and then
    the section, its line ends turned to LF and nothing else changed.
  - Exit 0: a recall (with hits, or with the one line `nothing matched: <words>`), a lookup's section, its list of
    matches, its too-large line, and the rules index lines or pages. Exit 1, one line: no heading matched, unknown
    source or campaign, a staging folder, no words left to search, a file lookup cannot read. Exit 2: a usage mistake.
- **Rationale:** FR-018 and FR-024, following `dmc size` (R7 of spec 006: DMC acts on what it reads, and a non-zero
  exit shows as a tool error, so an answer that is not a failure exits 0). The campaign rule lets DMC pass the campaign
  explicitly, which the kit's rules will tell it to do, while a bare `dmc recall Kibil notepad` works by hand.
- **Alternatives:** A required campaign (the issue's signature; the clarification chose the optional one). A
  `--campaign` option (more typing for DMC on every call).

## R11. Notices: what was skipped

- **Decision:** Before the hits, one line per problem: `<campaign>: Predecessor <value> is not in adventures/, not
  searched`, `<path>: cannot be read (<reason>), skipped`, `<module>: module folder not found, not searched`. At most
  three; a fourth becomes `(+N more skipped)`.
- **Rationale:** FR-017: the rest is still searched, and DMC learns once that part of the story could not be checked,
  as the resume already says for a missing predecessor.

## R12. The recall-test harness

- **Decision:** `tools/recall-test.ps1 -Questions <file> [-Campaign <name>] [-Dmc <path>] [-Folder <DMC folder>]`, a
  PowerShell 7 script with a header comment. The questions file is tab-separated, UTF-8, one question per line:
  `id<TAB>style<TAB>question<TAB>answer`, where `style` is `named` or `indirect` and `answer` is a .NET regular
  expression matched case-insensitively against the output; `#` starts a comment line. The script runs
  `dmc recall <campaign> <question>` for each, and prints one line per question (`HIT`, `MISS`, and whether the answer
  is in the first hit) and, per style, `answer in output a/n; first hit b/n`. It writes nothing. The questions file
  lives in the maintainer's DMC folder (suggested: `recall-test/questions.tsv`), converted once from the prototype's
  `queries.py` (30 facts × 2 styles).
- **Rationale:** FR-025 and the fifth clarification. Judging by the answer's regex, not the gold file and line, keeps
  the test valid after migration 004 moves and rewrites lines. PowerShell is what the repository's other developer
  scripts use, and needs nothing installed for players.
- **Alternatives:** An MSTest test gated on an environment variable (would skip in CI forever, against "tests skip
  locally and fail on CI"); Python (not a toolkit requirement).

## R13. Performance and AOT

- **Decision:** Read every file in scope with one pass each, tokenise into an in-memory inverted index (a dictionary
  from stem to postings), score only the units that hold a query word. No `Regex` on the hot path except
  source-generated ones; no reflection; no new package. A contract test times a recall over a synthetic 3 MB fixture
  with the published AOT build and asserts under one second (SC-004).
- **Rationale:** The prototype indexed 1.55 MB in 0.2 to 0.5 s in Python; compiled code is several times faster, and
  the measured play folder with its module is a few megabytes. `.docs/development.md` requires timing tests to use the
  AOT build.

## R14. The kit's rules: where they change, and how little

- **Decision:** Edit lines in place, adding none to `kit/dm-rules.md` if possible and at most two (FR-022):
  - § Accuracy and consistency, item 1, the campaign files (line 71): "search" becomes "recall (`dmc recall <campaign> <words>`)"; the Predecessor
    sentence says recall covers it.
  - § 3, the 5e rules (line 73): look the entry up with `dmc lookup rules <entry>` before the index.
  - "Look before you bring something back" (line 78): recall the subject first; read the sheet or look up its Playing them
    section (Voice, Names they use); if `dmc` fails or the recall does not hold it, search the files as before, and "not found" is not
    "not true" (FR-021).
  - § Running a published module, "Run from the files, not from memory" (line 113): look up the area, NPC or stat block in the module's digest with `dmc lookup <module>
    <heading>`; the cited pages in `source/` stay the authority for read-aloud text.
  - `dmc-setup-rules` § Using it in play: lookup first, then the index as today.
  - `dmc-new-campaign` and the campaign template's Predecessor comment: "recalled" for "searched".
  - Not changed (FR-020a): `dmc-resume-campaign`, `dmc-campaign-full-save`, and the migrations.
- **Rationale:** Constitution II and III: the always-loaded rules stay lean, and each rule keeps its one home. A
  toolkit test pins `dm-rules.md` at no more than two lines over its count before this change (212), and the existing
  test that every `dmc` command a prompt names is real covers the new names.

## R15. Step 1 (the play folder) and where its results go

- **Decision:** The steps are written in [quickstart.md](quickstart.md) § Step 1. The maintainer runs them when they
  can; the size total and the harness's figures go on issue #179 (second `/speckit-clarify` answer). The PR does not
  wait for them.
- **Rationale:** FR-001, SC-001, SC-002; the second clarification answer.
