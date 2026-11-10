# Feature Specification: Measure What a Resume Reads, in Tokens

**Feature Branch**: `006-dmc-size`

**Created**: 2026-11-09

**Status**: Draft

**Input**: User description: GitHub issue #174, "dmc size: measure a campaign's resume files in tokens and replace the
230-line canon limit".

## Background

At the start of every session the `dmc-resume-campaign` skill reads a fixed set of campaign files (its step 2): both
player-options files, `game-state.md`, `story/summary.md`, `story/dm-notes.md`, the sheets of the PCs, the companions
and the NPCs present (each down to its history), the tail of the latest session log, and `story/canon.md`. Everything
it reads stays in DMC's context for the whole session, and DMC remembers best when that set is complete and current.

The third feedback review (PR #173, entry `2026-11-08-campaign-files-outgrow-resume-and-slips-rise-late-in-a-campaign`)
measured the maintainer's long campaign, Rorn's Landing, after about 30 sessions:

- A resume read about **73k tokens** of files. `story/dm-notes.md` was 94k bytes, around the Read tool's cap for one
  call (about 25k tokens), so it came back cut short and had to be read again in parts.
- The only size limit the toolkit enforces is canon's **230 lines** (`dmc-resume-campaign` step 2.7,
  `dmc-campaign-full-save` step 4). It was set in commit 2a37a3f with no recorded reason.
- **Lines are a poor stand-in for size.** Across the campaign's files a line ran from about 90 to about 307
  characters, so one file of 230 lines can be three times the size of another.
- The files that grew are `dm-notes.md` and `game-state.md`. They are always read whole, and the targets the full
  save gives them (about 150 and about 100 lines) are aims that nothing checks.

PR #173 changed the prompts so those files stop accumulating history, but nothing measures whether that holds. This
feature adds the measurement: a `dmc` command that reports, in estimated tokens, what a resume of a campaign reads,
where the bulk is, and what is at risk, and moves the size rules from lines to tokens so they measure size.

The command was proposed in the review session behind PR #173, where the maintainer set the principle it serves:
"Our core principle here is consistency." The review's finding was that every rule against the growth already
existed, and none of them was checked; a number printed by a tool is harder for DMC to wave through than "is it under
about 150 lines?".

## Clarifications

### Session 2026-11-09

The first four, the issue's open questions, were settled from the PR #173 review session (2026-11-09), where the
maintainer approved the analysis they rest on. The last three were asked and answered in `/speckit-clarify`.

- Q: What does a budget optimise for? → A: Consistency over token cost. Dead text (history, finished plans, copies of
  facts kept elsewhere) is cut; live facts are not pushed out of the always-read set to save tokens. So the canon
  limit is set high enough that the long campaign's canon stays read whole, not lowered (FR-010).
- Q: Where does the measurement run? → A: In the full save's consistency check (step 7), which cannot call the save
  complete while a file is over its budget unless it says why it could not trim further; in `review-transcript`, so
  slip counts can be compared before and after a change; and at resume, which already measures canon and says one line
  only when a file is near the Read cap (User Story 4, FR-015).
- Q: How are budgets set? → A: A fixed budget per file, in tokens, as measured starting points to be tuned against
  slip counts from `review-transcript`. Not a share of the context window, which `dmc` does not know (FR-007).
- Q: What happens to the other size targets, written in lines or words? → A: They all move to tokens with canon,
  because "lines are a poor stand-in for size" holds for every file, and the files that grew are the ones whose line
  targets nothing checked (FR-011).
- Q: Does a bloat flag (not an over-budget file) hold up the full save? → A: Yes. Like a file over its budget, the
  save is not complete until each flag is fixed or its report says why the flagged text stays (FR-015a).
- Q: Do the files the full save never trims (both player-options files, the session log tail, a module's progress
  and digest) get budgets? → A: No. They are measured, shown and counted in the total, and flagged only near the Read
  cap. The total is shown with no budget of its own: with every trimmable file within budget, a total flag would leave
  nothing to fix (FR-007).
- Q: What does the command print when DMC runs it in play (full save, resume)? → A: Nothing when all is well (canon
  under its limit, no file over budget, no flag); otherwise one short line per thing to act on. The full breakdown is
  printed only when run by hand (FR-013a).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See what a resume reads, and where the bulk is (Priority: P1)

A player, or the maintainer tuning DMC, wonders whether a campaign's files are getting too big (the player in the
feedback asked exactly this: "are your files getting too big?"). They run one command for the campaign and get, for
each file the resume reads, an estimated token count of the part the resume reads, the total, and for each file the
resume reads whole, its sections largest first. The answer to "what is making the resume heavy?" is on one screen.

**Why this priority**: This is the measurement itself. Every other story uses it, and without it the size of the
files is a guess.

**Independent Test**: Build a fixture campaign from the templates with files of known size, run the command, and
check that each file the resume reads is listed with bytes ÷ 4 of exactly the part the resume reads, that the total is
their sum, and that the sections of each always-read file are listed largest first.

**Acceptance Scenarios**:

1. **Given** an active campaign, **When** the player runs the command with no campaign named, **Then** it reports the
   active campaign; **Given** a campaign named, **Then** it reports that one.
2. **Given** a campaign, **When** the command runs, **Then** it lists the files resume step 2 reads, chosen by the same
   rules the resume uses (which sheets, which part of a sheet, which part of the session log, canon whole or not), and
   none that it does not read (the archives, the sheets of absent NPCs, older session logs).
3. **Given** each listed file, **When** the report is shown, **Then** it gives the estimated tokens of the part read
   and the total of all of them, and says once how the estimate is made (bytes ÷ 4).
4. **Given** a file the resume reads whole (`game-state.md`, `dm-notes.md`, `summary.md`, and canon when it is read
   whole), **When** the report is shown, **Then** that file's `## ` sections are listed by size, largest first, so a
   section like an Opposition table or a Mix tracker stands out.
5. **Given** a sheet whose history is its last section, **When** it is measured, **Then** only the part above the
   history counts; **Given** a sheet whose history is in the middle, **Then** the whole sheet counts, as the resume
   reads it whole.
6. **Given** the latest session log, **When** it is measured, **Then** the tail from the third-last scene counts, or
   the whole log when the last full save does not cover it, by the rule the resume uses.
7. **Given** a published-module campaign, **When** it is measured, **Then** `module-progress.md`, the module's
   `index.md` and the digest section it names are counted too, as the resume reads them.

---

### User Story 2 - The canon limit measures size, not lines (Priority: P1)

The resume reads canon whole while it is under a limit and past that reads only its always-read sections plus a
search; the full save archives canon down below a lower mark. Both now measure canon in estimated tokens using the
command, so a canon of long lines and a canon of short lines are treated by their real size.

**Why this priority**: It is the one size rule the toolkit enforces, and the issue's named change. A limit that
measures the wrong thing lets canon drift.

**Independent Test**: Give a fixture two canons with the same line count and different line lengths; check that the
command reports one over the limit and one under, that the resume skill reads the first in sections and the second
whole, and that the full save skill archives the first and not the second.

**Acceptance Scenarios**:

1. **Given** a canon under the token limit, **When** a session resumes, **Then** canon is read whole.
2. **Given** a canon over the token limit, **When** a session resumes, **Then** only its always-read sections are read
   whole and the rest is searched, as today past 230 lines.
3. **Given** a full save that finds canon over the limit, **When** it archives, **Then** it moves lines whose subject
   can no longer come up until canon is under the lower mark, and re-measures with the command.
4. **Given** the change is made, **When** the limit is chosen, **Then** it is high enough that Rorn's Landing's canon,
   about 15.5k tokens at 232 lines and about 17k once PR #173's fight stories and moved standing orders reach it, is
   read whole (a limit of about 18–20k), and the reason is recorded in `.docs/decisions.md`.
5. **Given** the shipped kit, **When** its skills and documents are searched, **Then** no rule still decides by canon's
   line count.

---

### User Story 3 - Trouble is flagged before it shows up in play (Priority: P2)

The report flags the conditions that, in the feedback, came before the slips: a file close to the Read tool's cap for
one call, a file over its budget, canon over its limit, and the known bloat patterns from the campaign-file
standards. A player or maintainer sees what to fix while the story is still consistent.

**Why this priority**: The numbers alone need reading; a flag says when to act. It turns the measurement into a
warning.

**Independent Test**: Give a fixture a `dm-notes.md` just under and just over the cap margin, a `game-state.md` just
under and just over its budget, a Mix tracker of several dated lines, and an Opposition row with none left alive;
check each flag appears when it should and not otherwise.

**Acceptance Scenarios**:

1. **Given** a file whose part read is within the margin of the Read tool's cap, **When** the report is shown, **Then**
   it is flagged and the flag says the file may be cut short at resume.
2. **Given** a file over its budget, **When** the report is shown, **Then** it is flagged with its budget beside it.
   **Given** a file without a budget (a player-options file, the session log tail, a module file), **Then** it is
   shown and counted in the total but flagged only near the Read cap.
3. **Given** canon over its limit, **When** the report is shown, **Then** canon is shown against its limit, in tokens,
   and the report says it will be read in sections.
4. **Given** a `dm-notes.md` with more than the single "Last played" line or a per-session Mix tracker, an
   Opposition row with none left alive, or an Arc plan stage marked done that keeps more than its one line, **When**
   the report is shown, **Then** each is flagged as a likely leftover, naming the section.
5. **Given** a campaign with nothing to flag, **When** the full report is shown, **Then** it says so in one line;
   **When** DMC runs the play form, **Then** it prints nothing.
6. **Given** any flag, **When** the command runs, **Then** it changes no file: it reports, and the full save or the
   player acts.

---

### User Story 4 - The measurement runs where it helps (Priority: P2)

The command is called where a size question already arises, so nobody has to remember to run it: the full save
checks against it, the resume uses it for the canon decision and warns in one line when a file is near the Read cap,
and `review-transcript` records it with the slips.

**Why this priority**: A check nobody runs catches nothing. The review found that every size rule already existed
and none was followed, because DMC judged them itself. Each place it runs in play costs some context, so it says
nothing when nothing is wrong.

**Independent Test**: For each place chosen, run that step on a fixture with a flagged file and one without, and
check what it says and that a campaign with nothing flagged adds nothing to the conversation.

**Acceptance Scenarios**:

1. **Given** a full save, **When** it reaches its consistency check, **Then** it measures with the command rather
   than a line count, keeps trimming dead text while a file is over its budget or a bloat flag stands, and does not
   call the save complete until every file is within its budget and every flag is fixed, or its report names what is
   left and why it stays (every subject in the file still live, or the flagged text still needed).
2. **Given** a resume, **When** a file the resume reads is near the Read tool's cap, **Then** DMC says so in one line
   out of character; **When** nothing is flagged, **Then** nothing about size is said.
3. **Given** a `review-transcript` run, **When** it records feedback about context, **Then** it records the campaign's
   current sizes from the command alongside the slips it counts, so a later review can compare slip counts before and
   after a size change.
4. **Given** a resume, **When** it decides whether to read canon whole, **Then** it uses the same run of the command
   as the warning, so the resume runs it once.

---

### Edge Cases

- **No active campaign and none named.** The command fails in one line saying which campaign it looked for and how to
  name one.
- **A campaign that does not exist**, or a folder name starting with a dot (a staging folder left by an interrupted
  command). One-line error; nothing is measured.
- **A file the resume reads is missing** (no `dm-notes.md`, no session log yet, no canon). It is listed as missing,
  not counted, and not an error: a new campaign has few files.
- **A sheet named in `game-state.md` that has no sheet file**, or a Present line the command cannot match to a sheet.
  It is listed as not found, so a reader can see the measurement may be short.
- **`game-state.md` missing its Party status or Present line.** Only the PC sheets are counted, and the report says the
  companions and the NPCs present could not be told.
- **Canon over the limit.** Its search results depend on the scene and cannot be measured in advance: the report
  counts the always-read sections and says the search is not included.
- **A session log with no scene headings, or fewer than three.** It counts whole, as the resume reads it.
- **A module chapter not yet ingested** (no digest section). The report says so; nothing else is affected.
- **Text in other scripts or with accents.** Such characters take more than one byte, so bytes ÷ 4 overstates them a
  little; the method line covers it, and the flags keep a margin.
- **A file larger than the Read tool can return in one call.** It is flagged, and the report still gives its size.
- **Line endings.** A file saved with Windows line endings counts its extra bytes; the estimate is stated as an
  estimate.
- **The command is run in a play session.** The play form prints nothing when all is well and one line per item
  otherwise, since everything it prints lands in DMC's context (FR-013a). A player who asks DMC "are your files too
  big?" gets the full report, run by hand.
- **`review-transcript` reviewing old sessions.** The command measures the files as they are now, not as they were
  when each session was played; the record says when it was measured.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The toolkit MUST provide a `dmc size` command that measures one campaign: the one named, or the active
  campaign (`adventures/.active-campaign`) when none is named.
- **FR-002**: The command MUST list each file `dmc-resume-campaign` step 2 reads, choosing the files and the part of
  each by the rules that step gives (the sheets of the PCs, the companions and the NPCs present, each down to its
  history; the session log's tail or whole log; canon whole or its always-read sections; the module files for a
  module campaign). The rules stay in the skill; the command follows them and its tests check it against the skill.
- **FR-003**: For each listed file the command MUST give an estimated token count of the part the resume reads,
  computed as bytes ÷ 4, and the report MUST state the method once.
- **FR-004**: The command MUST give the total of the listed files.
- **FR-005**: For each file the resume reads whole, the command MUST list its `## ` sections by estimated tokens,
  largest first. To keep the output small it MAY show only the largest few sections of each file and say how many
  are left out.
- **FR-006**: The command MUST flag any file whose part read is within a margin of the Read tool's cap for one call,
  saying that the file may be cut short at resume.
- **FR-007**: Each file the resume reads that the full save trims (`game-state.md`, `dm-notes.md`, `summary.md`,
  canon and the sheets) MUST have a budget in estimated tokens, fixed for every player, and the command MUST flag
  each file over its budget. The files the full save never trims (both player-options files, the session log tail, a
  module's progress and digest) have no budget: they are shown, counted in the total and flagged only near the Read
  cap (FR-006). The total has no budget of its own. The budgets are starting points measured on the long campaign, recorded with their reason in `.docs/decisions.md` and
  tuned later against `review-transcript`'s slip counts. The review's starting points: `dm-notes.md` about 10k,
  `game-state.md` about 4k, `summary.md` about 3k, the read part of each sheet about 4k (a PC's sheet 6k, after measurement: research R4), and canon by its limit
  (FR-010). Every budget MUST sit well under the Read tool's cap, so no file is paged at resume.
- **FR-008**: The command MUST flag the bloat patterns that the campaign-file standards say should not stay in a
  file read whole:
  - in `dm-notes.md`: more than the one "Last played" line, or a per-session Mix tracker (the older form of it);
    Opposition rows with none left alive (`0 / N` in "How many (alive / started)"); an Arc plan stage marked done that
    keeps more than its one line, detected as the Arc plan section over its own budget (research R6); and lines
    marked DONE or RESOLVED outside the Arc plan, which by definition record what has happened;
  - in canon: a **Prices, debts and promises** section large for its kind, which suggests debts paid or promises kept
    that were not deleted;
  - in the content of any file the full save trims (under a `## ` heading, outside comments): lines over about 400
    characters, against the rule of one fact per line.

  Each flag names the file and section and says it is likely, since the command cannot judge the story.
- **FR-009**: The command MUST report canon against its limit in estimated tokens, and say whether the resume reads
  it whole.
- **FR-010**: The canon rule in `dmc-resume-campaign` step 2.7 and `dmc-campaign-full-save` steps 4 and 7 MUST measure
  canon in estimated tokens with `dmc size`, replacing the line count: a limit under which resume reads canon whole,
  and a lower mark the full save archives down to. The limit MUST be high enough that the long campaign's canon,
  about 17k once PR #173's additions reach it, stays read whole (about 18–20k), because canon is nearly all live facts
  and a searched canon depends on DMC searching when a subject comes up. The reason MUST be recorded in
  `.docs/decisions.md`. Each number has one home, as `.docs/development.md` § Writing prompts requires.
- **FR-011**: The other size targets the skills give in lines or words (`game-state.md` about 100 lines,
  `dm-notes.md` about 150, a sheet about 150, the notes about the player about 40, the summary about 600 words) MUST
  move to the token budgets of FR-007, so every size rule is measured the same way, by the command. Rules that are
  not about size (one fact per line, split a line that has grown into a paragraph) stay.
- **FR-012**: The command MUST NOT write, move or delete anything: it reads the campaign and prints a report.
- **FR-013**: Every failure MUST be one line, `dmc size: <message>`, and the report MUST be small, because what the
  command prints lands in DMC's context when DMC runs it.
- **FR-013a**: The command MUST have two forms. The full report (FR-003 to FR-009) is what a person running it by hand
  gets. The form DMC runs in play (the full save, the resume) prints nothing when all is well (canon under its limit,
  no file over its budget, no flag), and otherwise one short line per thing to act on, for example "canon 21k over its
  19k limit: read in sections", "dm-notes.md 22k near the Read cap" or "dm-notes.md Opposition: 3 rows with none
  alive". Silence means canon is read whole.
- **FR-014**: DMC MUST be able to run the command during play without a permission prompt, as it runs the other
  `dmc` commands it uses, and every prompt that names it MUST name a real subcommand, as the toolkit tests check.
- **FR-015**: The command MUST run in the full save's consistency check, at resume (once, for both the canon
  decision and the near-cap warning) and in `review-transcript`, as User Story 4 gives. Wherever it runs in play, a
  campaign with nothing flagged MUST add nothing to the conversation beyond what that step already says.
- **FR-015a**: The full save MUST NOT report itself complete while the command shows a file over its budget or any
  bloat flag of FR-008, unless its report names each one and says why it stays: a file that could not be trimmed
  further because every subject left in it is live, or flagged text that is still needed (an enemy that fled and may
  return). A long file of live facts is legitimate, as a long canon is today.
- **FR-016**: The command MUST give its per-file measurements in a form the campaign migration of issue #172 can use
  for its size verification, without that feature depending on this one or this one on it.
- **FR-016a**: If issue #172's campaign migration (`specs/005-campaign-migrations/`) has merged before this feature's
  PR, this PR MUST add the `dmc size` step to `kit/skills/dmc-migrate-campaign/SKILL.md`, as that feature's decision
  C1 requires, and move the migration's size check from line counts against the old targets to `dmc size`. If this
  PR merges first, the migration's PR does it instead.
- **FR-017**: The command MUST be tested on fixture campaigns built from the templates (real campaigns are git-ignored),
  on every platform CI builds, covering each rule of FR-002, each flag of FR-006 to FR-009 and each failure.
- **FR-018**: The documentation MUST change in the same pull request: `.docs/campaign-files.md` and
  `.docs/architecture.md` (the canon rule), `.docs/user-guide.md` (the command), `.docs/decisions.md` (the token
  limit and its reason, the estimate method), and `.docs/roadmap.md`.

### Key Entities

- **Resume read set**: the files, and the part of each, that `dmc-resume-campaign` step 2 reads for one campaign at
  this moment.
- **File measurement**: one file of the read set, the part read (whole, down to the history, tail, always-read
  sections), its estimated tokens, and, for a file read whole, its sections by size.
- **Estimate**: bytes ÷ 4, the method the review used, stated in the report.
- **Flag**: a condition worth acting on (near the Read cap, over the budget, canon over its limit, a bloat pattern),
  with the file and section it concerns.
- **Canon limit**: the token size under which the resume reads canon whole, and the lower mark the full save archives
  down to.
- **Report**: in its full form, the per-file measurements, the total, the sections of the files read whole, and the
  flags; in its play form, only the lines to act on, or nothing.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a fixture campaign, 100% of the files the resume reads are listed, none it does not read is listed,
  and each count equals bytes ÷ 4 of exactly the part read.
- **SC-002**: A player can answer "which file and which section make the resume heavy?" from one command, without
  opening a file.
- **SC-003**: On the measured long campaign as it was in the feedback, `dm-notes.md` is flagged as near the Read cap
  and the Opposition table and Mix tracker are among its largest sections listed.
- **SC-004**: On the long campaign, canon is read whole under the token limit, both as it is now and after PR #173's
  additions reach it.
- **SC-005**: Two canons with the same line count and a threefold difference in size are treated by size: the larger
  is read in sections and archived, the smaller is not.
- **SC-006**: After the change, 0 size rules in the kit, and in the documents' account of current behaviour, are counted in
  lines or words (`.docs/decisions.md` keeps the old rules as history).
- **SC-007**: For a campaign with nothing flagged, each step that runs the command in play adds 0 lines about size to
  what DMC says.
- **SC-008**: The full report for the long campaign fits in a short screen (about 40 lines or fewer), and the play
  form prints 0 lines for a campaign with nothing to act on and 1 line per item otherwise.
- **SC-009**: Each flag appears on the fixture built to trigger it and on no fixture built not to, in 100% of cases.
- **SC-010**: A full save of a campaign with a file over its budget or a bloat flag never ends with a report that says
  it is complete without naming that file or flag.

## Assumptions

- **The estimate is bytes ÷ 4**, as the issue and the review used. It is an estimate: the 94k-byte `dm-notes.md`
  measured about 23.5k by it, under the Read tool's cap of about 25k tokens, yet the read was cut short, so the real
  count was higher. The near-cap flag therefore keeps a margin, about 80% of the cap (about 20k estimated tokens, 80k
  bytes), and the plan may tune it with the reason recorded.
- **The read set is the resume skill's**, not a second copy of its rules. If the skill's step 2 changes, the command
  changes with it in the same pull request; a test reads the skill so the two cannot drift silently.
- **The flags only report.** Deciding whether a fight is over or a plan is overtaken is a story judgment; the command
  matches the markers the templates use (the "Last played" line, the alive / started count, a stage marked done) and
  calls what it finds likely. Overtaken plans have no marker and are not flagged. Trimming stays with the full save.
- **No migration is needed.** The change alters when canon is archived, not what a campaign file holds: the next
  full save archives against the new limit. If issue #172's migrations land first, this pull request says so in its
  migration line.
- **Maps, archives and older logs are not counted**, since the resume does not read them (`dmc map` opens the live
  map; it is not read into context).
- **The budgets are not derived.** Unlike 230 they are measured, and the review gave them as starting points to tune.
  The review's earlier total of about 50k (what PR #40 achieved) is not kept as a budget: the canon limit was raised
  for consistency, and the per-file budgets are what DMC can act on. The total is still shown, so the trend is
  visible.
- **Measuring does not shrink a file.** What removes dead text is PR #173's prompt changes and the full save; the
  command makes them checkable. A campaign whose files are already over budget is trimmed at its next full save, or by
  the migration of issue #172.
- **Players install nothing new**: the command is part of `dmc` (Principle II of the constitution).
- **Issue #172** (campaign migrations, `specs/005-campaign-migrations/`) may use this command for its size
  verification; its spec already falls back to line counts if this is not built, and this spec does not wait for it.
