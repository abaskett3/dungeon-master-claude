# Research: Linked Campaigns and the Sequel Import

Phase 0 of [plan.md](plan.md). Each item settles one question the spec leaves to the plan, with what was read in the
code and the prompts. The spec's own clarifications are not repeated here.

## R1. Where the link logic lives

- **Decision:** One new class, `Dmc.Campaign.CampaignLinks`, reads a campaign's Continuity section (or, failing that,
  its old Predecessor line, FR-005) and builds the linked set. `RecallScope.Build` stops walking the Predecessor chain
  itself and asks `CampaignLinks` for the set. A new read-only verb, `dmc campaign links [NAME]`, prints it with its
  notices. In play, the notices (and in PR 3 the legacy lines, R12) come from `dmc size <campaign> --check`, which
  resume and the full save already run and which prints nothing when all is well, so play makes no new command call.
- **Rationale:** Recall, the new verb and the legacy check (R12) all need the same set, so it has one home
  (constitution III). It goes in `Campaign/` beside `ClosedCampaign`, because it reads other campaigns' state (closed
  or open), not text to rank. The verb belongs to `dmc campaign`, which already holds every verb about a campaign's
  standing (`standard`, `close`), and keeps `dmc` to its existing commands (spec 010's structure decision).
- **Alternatives considered:** Keeping the walk inside `RecallScope` (the legacy check would need a second copy);
  folding link problems into `dmc campaign standard`'s output (its output is a state the skills branch on, and resume
  says nothing on `current`; mixing link notices into it would break that contract); a `--check` form of the new verb
  run at every resume and full save (a command call each, an estimated 30 to 50 tokens, against FR-031's 150).
  `dmc size --check` is a list of lines for DMC to act on, each kind with its reader, so link lines fit it; notices
  end with its existing `(not the full save's to trim)` marker, so the full save's rule for its lines is unchanged.

## R2. How links are followed

- **Decision:** Breadth-first from the campaign, each campaign visited once (case-insensitive folder names, as
  today). The edges out of a campaign are the names on its own three lines; the edges into it are found by reading the
  Continuity section (or Predecessor line) of every folder in `adventures/` whose name does not start with a dot.
  Distance is the number of steps on the first path found. Each campaign reached gets one **link label**, from the
  recalled campaign's side:
  - `continues`: every step on the path is a Continues link followed backwards (an earlier campaign of this chain);
  - `sequel`: every step is a Continues link followed forwards (recalling in a closed campaign, or in an open one a
    sequel was kept on the player's word, FR-028a);
  - `side`: a single Side campaign of step, either way;
  - `world`: anything else (Shares a world with, or a mix of kinds).
- **Rationale:** FR-003 asks for links both ways, so a main campaign reaches its side campaigns without naming them.
  The labels are what FR-020's play rule needs: only `continues` facts can be known to characters who were there; the
  rest are facts of the world. Four labels cover every case and keep a hit line short (R6).
- **Cost:** Reading every campaign's `player-options.md` on each recall: the maintainer's folder has 2; a large folder
  might have 20, each a few kilobytes, which is noise against the megabytes recall already reads (SC-006).
- **Alternatives considered:** Links one way only (the main campaign would never see its side campaign's world
  changes in recall); a separate label per relation pair (`main`, `side campaign of my predecessor`, ...), which is
  longer for no change in what DMC does with the hit.

## R3. The Continuity section's shape

- **Decision:** In `player-options.md`, after `## Rules`, a `## Continuity` section with exactly three list lines,
  `- **Continues**: <name or none>`, `- **Side campaign of**: <name or none>`, `- **Shares a world with**: <names,
  comma-separated, or none>`. Values are read as recall reads the Predecessor value today: HTML comments, backticks, a
  leading `adventures/` and slashes removed. A second name on a single-valued line is a notice, and only the first is
  followed (spec edge case).
- **Rationale:** Its own section keeps links out of the edition and house-rules lines, and lets a toolkit test find
  it by heading. One line per kind is what the player reads and what a migration can write without judgment.
- **Alternatives considered:** A `links.md` file (one more file per campaign for three lines, and resume would have
  to read it too); a dmc-only dot file like `.closed` (links are the player's intent, not a fact `dmc` must trust, and
  hand-editing them is legitimate).

## R4. Recall's ranking with digests

- **Decision:** A new tier, `Tier.Digest`, for any file under `digests/<name>/` of a campaign in scope, with the
  archive's weight (1.20) times 0.95 per step of the **digested** campaign's distance (its distance in the linked set,
  or one more than the campaign holding the digest when that campaign is missing). A campaign whose digest is held by
  a campaign nearer in the set is a **last-resort** place: its own files' weights are multiplied by 0.85. Worked
  example, Rorn's Landing continuing a closed Silver Egg:
  - a Silver Egg canon line in the digest: 1.20 × 0.95 = 1.14;
  - the same line in The Silver Egg's own canon: 1.30 × 0.95 × 0.85 = 1.05;
  - a Rorn's Landing canon line: 1.30; its session log line: 1.00.

  `digests/*/secrets.md` lines are DM-only (`LineUnits`' DM-file test gains the path), and so are the lines under a
  digest's `## Module` heading, which copies the DM-only `module-progress.md`. `digests/*/index.md` is not searched:
  it holds no facts, and its Carried line would rank at the archive's weight. Nor is `digests/*/spine.md` (T103): its
  scene headings are in the digested campaign's logs too, and on the maintainer's play folder its short lines, at the
  digest's weight, crowded a fact out of the output (the lease question of T077: 27 of 30 named with it, 28 without). A campaign holds a digest once
  `digests/<name>/` has a `.md` file in it; an empty folder stands in for nothing.
- **Rationale:** FR-017 needs a digest line to beat the same or a weaker line from the digested campaign's folder at
  equal relevance, while the sequel's own canon still wins over both. The factor keeps the folder searchable as the
  last resort (the issue's step 5) rather than dropping it. Using the archive's weight places the digest where its
  lines came from: curated facts that resume does not read.
- **Alternatives considered:** Leaving the predecessor's folder out of scope once digested (loses the last resort the
  issue keeps, and the session logs' full text); a digest weight above canon (would let an old fact outrank the
  sequel's own newer one about the same subject, the opposite of "canon wins").

## R5. Duplicate lines

- **Decision:** Before ranking, the line units whose normalised text (`ContentUnits.Normalise`, the migrations' rule)
  is the same become one: the copy with the highest weight (then the ranking's tie order), DM-only when any copy is.
  The others are not ranked, printed or counted in the closing `(N more lines matched)` line.
- **Rationale:** The digest copies lines word for word, so without this a recall prints the digest's line and the
  source's line one after the other and halves the useful hits within the 6,000-byte cap. Because the digest ranks
  above its source (R4), the copy kept is the digest's. Merging before ranking, not when printing, keeps the copies
  from counting against their own words' rarity: a fact held by its campaign and by two later digests would otherwise
  rank about 10% lower than a line found once. A copy keeps any copy's DM mark, so a DM-only line is never printed
  as safe through its copy. Reusing the migrations' normalisation keeps one definition of "the same line"
  (constitution III).
- **Alternatives considered:** Skipping a duplicate when printing (the first version: the copies still lowered their
  words' weight and filled the closing count).

## R6. The hit line's label

- **Decision:** A hit is `<place>[ (<label>)] <file>:<line>[ <session>][ DM] | <text>`. `<place>` is the campaign or
  `modules/<name>` as today; `(<label>)` follows any campaign but the one recalled in, with R2's label. A digest's
  hits are in the recalled campaign's place, and their file, `digests/<name>/<kind>.md`, says they come from a digest
  and of which campaign; they take the digested campaign's label as well, after the file and line rather than after
  the place, so the label is not read as the holder's, and the play rule reads the same in both cases. Example lines are in [contracts/recall-and-lookup.md](contracts/recall-and-lookup.md).
- **Rationale:** FR-018. The label costs 8 to 12 bytes a hit from another campaign, and nothing for the campaign's own
  hits, which are most of them. A digest's path already names it; repeating the campaign name would cost more bytes.

## R7. Lookup and size need almost nothing

- **Decision:** No change to `dmc lookup`'s resolution: a digest is a campaign file under `SourceFiles.Campaign`, so
  `dmc lookup <sequel> <subject>` already finds a digest heading (with a list when the sequel's own sheet has the same
  heading), and `dmc lookup <sequel>/digests/<name>/people.md <subject>` names one file. Tests are added for both.
  `dmc size` builds its resume set from the resume skill's rules (`ReadSet`), which never name `digests/`, so the
  digest is already outside the resume total; a test pins it. The Read-cap check of each digest file moves to the
  digest's own check (R10), which runs when the digest is written, since a digest never changes afterwards; FR-020a is
  amended to say so.
- **Rationale:** Measured in the code rather than assumed: `SourceFiles.Campaign` excludes only `import/`,
  `exports/`, `backups/` and dot folders, and `ReadSet.Build` lists named files only.

## R8. The new verb for the sequel import's exact parts

- **Decision:** `dmc campaign sequel NAME` with four forms, each about the digest of the campaign NAME continues:
  - `--start`: checks NAME's Continues link names a closed campaign whose closing backup passes its check, and that
    the closed campaign's files still match it (`verify`'s closed path); creates `digests/<closed>/` with its
    `index.md`; copies each `digests/*/` the closed campaign holds, byte for byte; writes `spine.md` from the closed
    campaign's session logs; prints the parts plan, whose part 1 is always `player-options.md` and
    `story/dm-notes.md` whole, the files only the brief needs (FR-007). Safe to run again at any point, after an
    interruption or to restore a chain copy: it rewrites only what it writes itself (the index's lines but Carried,
    the spine, the chain copies) and never touches the subagents' files; it refuses only when `digests/<closed>/`
    holds a different campaign's digest (its index's Campaign line names another).
  - `--list`: the accounted units (FR-021) not yet found in the digest or the accounting, with ids, as `verify --list`.
  - `--check`: the whole proof: accounting complete, every copied digest identical to its source, every digest file
    under the Read cap, one heading per subject, the index's checksum equal to the closing backup's. Prints
    `checked ...` or the failures.
  - `--sample [--round N]`: the draw (R11).
- **Rationale:** Every exact part is `dmc`'s and every judgment DMC's, as for migrations (spec 005) and closing
  (spec 010). The name avoids `dmc digest`, which already exists and digests Claude Code transcripts. The sequel is the
  argument because every write is in it; the closed campaign is never written (FR-016) and the writers' closed-campaign
  guard (`ClosedCampaign.Refuse`) still stands behind that.
- **Alternatives considered:** A top-level `dmc sequel` command (the campaign verbs are where players and DMC already
  look); four verbs (`sequel-start`, ...), which grows `campaign --help` for one feature.

## R9. The accounting

- **Decision:** The accounted sources (FR-021) are read from the **closing backup** (the ZIP `BackupFolder.Closing`
  names), not the live folder. A unit is a non-blank line, normalised and given an id as `ContentUnits` does for
  migrations; every line of every file in `character-sheets/` counts (each sheet whole), and in `dm-notes.md` only
  § Secrets, in `summary.md` only § How it ended. A source the closed campaign does not have counts no units: an
  older campaign may lack one (The Silver Egg, the first, predates several). A unit is accounted when its normalised text is
  in a digest file (or contained in a longer digest line, at 20 characters or more, as `verify` allows), or has a line
  in `digests/<closed>/accounting.tsv`: `<id>\tmerged\t<anchor>` (the anchor, 8 characters or more, must be in the
  digest) or `<id>\tdropped:<class>\t<reason>`. The declared classes are `superseded` (a reading the closing save
  replaced; the reason names the line that replaced it), `not-canon` (what § How it ended names as not canon),
  `structure` (a source's own section headings, table headers and template lines with no fact), `bookkeeping`
  (module-progress pointers and stamps, such as "Digest to read before continuing") and `stat-block` (a dead
  character's stat block; the reason names the sheet). A living character's stat block is copied under their heading,
  and a sheet's `## Secrets` lines go to `secrets.md`.
- **Rationale:** FR-022, FR-023. `AccountingFile` already parses this shape; it gains `merged` as the keyword for
  this file (migrations keep `reworded`), so the parser stays one class. Checking the backup means a hand edit to the
  closed folder after closing cannot pass as accounted.
- **Alternatives considered:** Accounting every line of every file (DM plans and game state are carried by the brief
  or overtaken; making DMC account for each would cost thousands of lines for nothing a sequel asks). Accounting only
  a sheet's history, as first planned: a returning character's appearance, voice and names, and the sheet's own
  secrets, would go unproved (the analyze session's clarification).

## R10. The digest's own checks

- **Decision:** `sequel --check` fails when: a unit is unaccounted (R9); a copied digest differs in any byte from the
  closed campaign's copy, or its index names a closing backup whose checksum differs from that campaign's; a digest
  file is over the Read cap (`Budgets.NearCapTokens`, 20k, the margin `dmc size` uses, so a file is never cut); a
  subject heading (`## ` in `people*`, `places*`, `factions*`, `items*`, `world*`) appears twice across a kind's parts;
  or `index.md` lacks a required line. The checksum is SHA-256, as export manifests use.
- **Rationale:** SC-002, SC-009 and the chain edge cases, all checkable without a model.

## R11. The sample draw

- **Decision:** The seed is the first 8 bytes of the closing backup's SHA-256 plus the round number (default 1). The
  draw takes 20 fact lines from the digest's subject files: fact lines are split into copies (a match for an accounted
  unit) and session-only lines (no match), and the 20 are split between the two in proportion, rounded, at least 1 of
  each when both exist. It takes 5 people from the closing backup: the `# ` title of each `character-sheets/*.md` but
  `*-history.md` and `minor-npcs.md`, and the Name cell of each row of `minor-npcs.md`'s table. At least one of the 5
  is a name on the index's Carried line. `dmc` prints the 25 items, each with its file and line; DMC hands only the digest folder's path
  and the items to one fresh subagent (clarification 3), then compares its answers with the closed campaign's files.
- **Rationale:** FR-024: reproducible (same backup, same round, same draw), and a redraw is a new round. Facts come
  from the digest's lines, which keeps session-only facts in, since the sources do not hold them. People come from the
  closed campaign's own sheets, so a person the digest lost or scattered without a heading is as likely to be drawn as
  one it kept. The accounting, not the sample, proves that no source line was lost.
- **Alternatives considered:** A random seed recorded in a file (not reproducible from the backup alone); drawing
  facts from the sources (would never test a session-only fact); drawing facts from the session logs too (narrative
  lines the digest rightly left out would count as misses); drawing people from the digest's headings (could never
  draw a person the digest lost).

## R12. The legacy file

- **Decision:** `legacy.md` at the top of a campaign's folder, written from a new `kit/templates/legacy.md` (its
  guidance is in an HTML comment, so it costs nothing when the file is read). Its stamp line is
  `- **Written**: session N, <yyyy-MM-dd>`: the session number orders it, the date is for people. The reader's full
  save records the session of each legacy it folds, on one `game-state.md` line only a linked campaign has:
  `- **Legacies folded**: side-quest s14, old-west s3`.
  `dmc size NAME --check` prints, for each campaign Y in the set whose digest no campaign nearer NAME holds (a digest
  carries what Y's legacy says), with a `legacy.md` whose Written session is later
  than the one NAME's Legacies folded line records for Y, or with none recorded, `legacy: read adventures/<campaign>/legacy.md (written session N; record <campaign> sN) (not the full save's to trim)`; an unreadable Written line prints it too, as `written unknown` (a false
  alarm costs a read; a missed one costs a contradiction). It prints `legacy due: at the full save, update this campaign's legacy.md for open linked campaigns <names>: add this save's changes and drop the lines from session N and earlier` (or `keep every line the budget allows`) whenever the set holds an open campaign other than NAME, so
  every full save brings the file up to date, two in one session included (a test on the Written session would miss
  the second, since resume can reuse a session log with no scenes and Current session need not move); the full save
  acts on it once, before its own size check, and leaves it after; resume ignores it as one for the full save
  (contracts/campaign-links.md).
  The file is a running list, one line per change with its `sN.M`, oldest first. The save adds its changes and drops
  only the lines from the session named and earlier: the lowest session that the open campaigns in the set other
  than NAME record for NAME on their Legacies folded lines, or `keep every line the budget allows` while any of them records none.
  Closed campaigns are left out, since they never fold again. Over budget, the oldest lines go until it fits.
  Budget: 3,000 estimated tokens, a new `Budgets` entry and a `dmc size` flag for the file.
- **Rationale:** FR-025, FR-026, SC-007, SC-008. Session numbers are the one order DMC always writes; every campaign
  in the maintainer's folder has them (Current session lines, `session N` in each Last full save line, numbered and
  titled logs), and the two things compared are new, so no older file lacks them. Recording what was folded, rather
  than when this campaign last saved, names exactly the legacies not yet taken in. Two campaigns played at once, in
  two terminals, are out of scope (spec Clarifications, 2026-11-13). The file is outside `story/` so the resume's reads, which name files,
  never pick it up by accident. A running list rather than a rewrite, because a rewrite loses the first of two full
  saves made before a reader resumes; and `dmc` names what to keep, because the writer cannot see what each reader
  folded (each records it only in its own `game-state.md`) and `CampaignLinks` already reads every campaign in the
  set. Past the budget, a reader left unplayed for long misses the oldest lines in the legacy; the writer's canon still
  holds them and recall reaches it, which costs nothing in play, where holding every line would let the file grow
  without bound.
- **Alternatives considered:** File modification times (exports, imports and copies change them); comparing the
  legacy's date and time with the reader's last full save or session start (DMC writes dates without times: Rorn's
  Landing's Last full save line is `2026-11-11, session 47, through scene 2`, and session logs record only the date
  played, so two campaigns played the same day cannot be ordered); a record file per pair (one line in
  `game-state.md` does it).

## R13. Asking before the migration's backup

- **Decision:** The migration file format gains one optional section, `## Before the backup`, between
  `## What changes` and `## Steps`. `dmc-migrate-campaign` § 2 gains one sentence: after the player says yes, follow
  each pending migration's `## Before the backup`, if it has one, before step 3; if it says to stop, stop, since
  nothing has changed. Migration 005 uses it for FR-028a: the check of the Predecessor's campaign with
  `dmc campaign standard <predecessor>`, the warning, the close question, the "are you sure?", and the stop line. On a
  yes to closing, it follows `dmc-close-campaign` for the predecessor, naming it, with its step 2 skipped (the
  question just asked is that question), and goes on; `adventures/.active-campaign` is never changed. A refusal from the close skill's step 1 is relayed and the migration stops, changing nothing. On `behind`, it follows this skill for the predecessor
  first. `MigrationFile.CheckHeadings` already allows other headings between the five required ones, so the parser
  needs no change; a toolkit test pins that `## Before the backup`, when present, sits between `## What changes` and
  `## Steps`, and the format contract and `.docs/development.md` § Changing a campaign-file standard describe it.
- **Rationale:** The clarification requires the questions before the backup, so a stop leaves the campaign `behind`
  and playable. A section in the migration file keeps this migration's judgment in its own file (constitution III),
  and the skill change is generic. The close skill already takes a named campaign, but the full save it follows works
  on the one `.active-campaign` names; so the full save's campaign line gains "or the campaign the skill that called it
  names" (about 10 tokens). An interruption mid-close then leaves the sequel active, and its migration is offered
  again at the next resume. Switching `.active-campaign` to the predecessor and back was rejected: an interruption
  would leave the predecessor active, and the next session would resume it, perhaps half-closed.
- **Alternatives considered:** Putting the check in the skill's step 2 by name (the skill would carry one migration's
  logic for ever); asking in `## Steps` (after the backup, so a stop leaves the campaign `unfinished` and blocks play).

## R14. Migration 005

- **Decision:** `kit/migrations/005-continuity-links.md`, `touches: [player-options.md]`, `drops: []` (its
  `## Declared drops` says none), `checks: [continuity-section]`. Step: if the file has no `## Continuity` section, add it after `## Rules` with the
  three lines, Continues taking the Predecessor value (or `none`); then delete the Predecessor line. Accounting: the
  Predecessor line's unit is `reworded`, anchored on the new Continues line. `continuity-section` fails when a
  `**Predecessor**` line is present, the section is missing, or any of its three lines is missing. A golden fixture
  `tests/Dmc.Tests/Fixtures/migrations/005/` with two campaigns' worth of cases is not possible (a fixture is one
  campaign), so it holds the case with a predecessor; the `none` case is a unit test of the check.
- **Rationale:** FR-028; the spec 005 contract. No drop: the line's value survives on the new line.

## R15. Where the import's instructions live

- **Decision:** A new section `## 1c. A sequel` in `dmc-new-campaign`, after § 1b, which it follows for the parts and
  the subagent rules and points to for them (constitution III). It covers: confirm the closed campaign
  (`dmc campaign standard <it>` says `closed`), ask which characters carry over, write the Continuity line,
  `sequel --start`, the parts in order (each subagent reads its part of the closed campaign and writes only into
  `digests/<closed>/` and, for the brief, the named sequel files), `sequel --list` and the accounting, `sequel --check`,
  `sequel --sample` and the one subagent, then session zero for what the brief does not cover. Progress goes to
  `import/progress.md`, as § 1b does. The sheet rule (FR-008, FR-008a) is one clause in `dmc-character-sheet-update`,
  its one home, which § 1c's subagents follow for the carried characters and DMC follows for anyone returning later. The brief's always-read targets are canon's Rulings and Prices, debts and promises,
  the campaign's player options (House rules, Other requests), DM notes (Arc plan, Secrets) and the summary.
- **Rationale:** The skill already owns starting a campaign and an import; play pays for § 1c only when the skill
  loads, which is once per campaign.

## R16. Standing cost (FR-031)

- **Decision:** Measured per PR as bytes ÷ 4 of what an ordinary session loads: `kit/dm-rules.md` (one line rewritten
  in place, at most one line added), the resume skill (the Predecessor sentence deleted; one clause on `notice:` lines
  in the size check it already runs; PR 3 adds one for `legacy:` lines), the full save (PR 3: one clause, about 50 to
  90 tokens since it also keeps lines and folds `legacy:` lines; measured at T090, and if it runs over, its detail
  moves to the template's comment, read only when the file is written), skill descriptions (none added: `dmc-new-campaign`'s description gains a few words for sequels). Expected
  total under 120 tokens. No command call is added (R1), and `dmc size NAME --check` prints no link line for a
  campaign with no links and no news.
- **Rationale:** Constitution II; spec SC-007.
