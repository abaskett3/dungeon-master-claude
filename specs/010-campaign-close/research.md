# Research: Close a Campaign for Good

**Feature**: `010-campaign-close` | **Date**: 2026-11-11 | **Spec**: [spec.md](spec.md)

The spec's clarifications settled the four questions that needed the user (a new skill; no reopening; the closing
records in the files that already hold their kind; secrets gathered back at close). What is left is how to build it.
Each decision below rests on the code as it is on `origin/main` at `4e974ba` and on the recorded decisions it cites.

## R1. The closed mark is `adventures/<campaign>/.closed`

- **Decision**: a file named `.closed` in the campaign folder, beside `.standard`. One line, then a newline:
  `<UTC time, yyyy-MM-ddTHH:mm:ssZ> <closing backup file name>`, for example
  `2026-11-12T03:10:00Z the-silver-egg_closed_20261111191000.zip`. Only `dmc campaign close` writes it, through a
  temporary file renamed into place as `StandardRecord.Write` does. Nothing removes it except the close's own rollback
  of the same run. Its presence is the whole test; its content serves `verify` (R7) and the reader.
- **Rationale**: FR-010 and the `.standard` decision (`.docs/decisions.md`, the migrations entry): a fact a program
  must trust lives in a file only `dmc` writes. A dot file travels with an export (`CampaignExporter.Walk` takes every
  file but `backups/`; `.standard` already round-trips), costs no context (resume never reads it; `dmc campaign
  standard` reports it), and DMC can test it without `dmc` (a Glob for `.closed`). Naming the backup in the mark lets
  `verify` find the closing backup without guessing, even after more files are added to `backups/` by hand.
- **Alternatives considered**: a line in `game-state.md` (rejected by the same reasons as for `.standard`: read every
  resume, edited by DMC and the player); a field in `.standard` (would change a file every migration verb reads and
  whose format is one integer, R2 of spec 005); the closing backup's presence as the mark (a file in `backups/` can be
  removed by hand, and a missing `backups/` after a copy would silently reopen the campaign).

## R2. DMC's edits cannot set or clear the mark

- **Decision**: add `Edit(/adventures/*/.closed)` to the deny list of `kit/settings.json`. Claude Code applies an
  `Edit(...)` rule to every tool that edits files, `Write` included (`.docs/decisions.md`, amendment A13 of spec 005),
  and a deny beats the `Edit(adventures/**)` allow. Deleting it is already denied (`rm`, `Remove-Item`, `mv` and the
  rest are in the deny list).
- **Rationale**: FR-010 says the mark "cannot be set or cleared by DMC's edits". A rule in a prompt alone would make
  that a request, not a fact. `.standard` has no deny rule today because a wrong `.standard` is caught by `verify`; a
  wrong `.closed` would freeze or unfreeze a campaign with nothing to catch it.
- **Alternatives considered**: a PreToolUse hook that refuses any edit under a closed campaign. It would make FR-017
  exact, not just the mark, but it starts a `dmc` process on every Edit and Write of every play session, for a state
  that is entered once per campaign and is already reported by `verify` (R7) when broken. Rejected for now; recorded in
  the decision so a later feedback review can revisit it if a closed campaign is ever edited in play.

## R3. Closing is one verb, `dmc campaign close NAME [--yes]`, with a plan by default

- **Decision**: a new verb of the existing `campaign` command. Without `--yes` it runs every check and prints what it
  would do (`would close <name>: <N> files to backups/<file> and exports/<file>`), and writes nothing. With `--yes` it
  closes, in this order:
  1. Recover an interrupted restore (`CampaignRestorer.Recover`), as every migration verb does first.
  2. Refuse unless `StandardRecord.Status` is `Current`: already `closed` first (`<name> is already closed: start a
     sequel to continue its story`), then `unfinished` (finish or restore the migration), `behind` (bring it up to
     date first), `ahead` (update dmc).
  3. Fix the closing backup's one file name from the closing moment (R4), and refuse if a file of that name is already
     in `backups/` or `exports/` (`<name> cannot be closed: <folder>/<file> already exists`). Nothing is renamed to
     get round a clash: a file already there is a leftover the player should look at, not one to work around.
  4. Write `.closed` (R1), naming that file.
  5. Write the closing backup into `backups/` under exactly that name with the exporter (R4), never replacing a file,
     and check it whole (`ExportSummary.Open` and `ReadCampaign`, as `CampaignBackup.Take` does).
  6. Copy it to `exports/` in the DMC folder under the same name (R5) through a `.partial-` file, never replacing a
     file, and check the copy whole and its SHA-256 equal to the first.
  7. Print one line: `closed <name>: <N> files to backups/<file> and exports/<file>`.

  The plan (no `--yes`) runs steps 1 to 3 and prints the name step 3 fixed. Any failure from step 4 on removes what this run wrote (the copy, the backup, then the mark, in that order) and
  throws one `DmcException`: `could not close <name>: <step>: <reason>; it is still open`.
- **Rationale**: FR-011 asks for one command and an order in which a failure leaves the campaign open. The plan form is
  `restore`'s pattern (decision of 2026-11-09): the close skill runs the plan first, before any closing save, so a
  campaign that cannot be closed (behind, unfinished) is found before DMC spends a full save on it; and a mistaken
  `dmc campaign close` typed by DMC changes nothing without `--yes`. The mark goes before the backup so the backup holds
  the campaign "as closed" (spec Assumptions). Removing the mark last on a rollback means there is never a moment when
  a closing backup exists for a campaign without a mark on disk that a reader could take for finished.
- **Alternatives considered**: separate verbs (`backup --closing`, `mark`) that the skill strings together (each step
  exact, but the order and the rollback would be the skill's, which is the judgment/exact split the wrong way round);
  writing the mark last (then the closing backup would not hold it, and an import of the closing backup would make an
  open campaign, against FR-018).

## R4. The closing backup's name keeps it out of every count

- **Decision**: `<campaign>_closed_<yyyyMMddHHmmss>.zip` (local time, as backups are named), with no `-2`, `-3` suffix:
  the closer fixes the name once (R3 step 3) and refuses a clash rather than choosing another name (the user,
  2026-11-11: a closed campaign cannot be closed again, and close checks and fails properly). It is written by a new
  `CampaignExporter.ClosingBackup` beside `Backup`, which takes that exact file name, fixes the destination to
  `backups/` and never replaces a file. The mark and the backup therefore always name the same file. `BackupFolder.Parse` matches only `_backup_`, so `List`, `AskLine`, `PruneSelection`, `Resolve` and
  `Marked` never see it, with no change to them. `BackupFolder` gains `ClosedName` and `ParseClosed` so the name has
  one home.
- **Rationale**: issue #180 noticed that files which do not match the backup pattern are already left alone; the test
  for SC-002 proves it stays so. Keeping the time format of backups makes the two sort together in a file listing.
- **Alternatives considered**: a `closing/` subfolder (one more path rule for import and export to know); a companion
  `.closing` file beside an ordinary backup (a counted backup could then be pruned with its companion by `--prune`, the
  very loss this prevents).

## R5. The second copy goes to `exports/` in the DMC folder

- **Decision**: the same file name in `exports/`, made if missing; a file already there is refused by the plan (R3
  step 3), never replaced or renamed around. Byte-for-byte the same file: copied, not exported again, and checked by SHA-256.
- **Rationale**: it is the default home of an export outside a campaign (`.docs/dmc-reference.md` § campaign); it is
  never under `adventures/`, so recall and DMC's searches never see it twice; install, refresh and update never touch
  it (`.docs/architecture.md`). Copying instead of exporting twice guarantees "the same file" (US3.3) and that both
  copies hold the same `exportedAt`.
- **Alternatives considered**: a folder outside the DMC folder (a user's Documents, a cloud folder): `dmc` has no
  setting for one, and the user guide already tells players to keep a further copy elsewhere; a second export (two
  files that differ in their timestamps and checksums).

## R6. `dmc campaign standard` reports `closed` first

- **Decision**: `StandardState` gains `Closed`, checked in `StandardRecord.Status` before the `.migrating` mark and the
  comparison. The line is `closed: <UTC time>`, from the mark. `ImportStandardLine` prints
  `<name> is closed: start a sequel to continue its story` for an imported closing backup. `Refresher.BehindLine`
  counts only `Behind` and `Unfinished`, so a closed campaign drops out of "campaigns are behind" with no change.
  `--init` refuses a closed campaign (FR-015). A mark that cannot be read is still a closed campaign: the state is the
  file's presence; only `verify` needs the content, and says when it cannot read it.
- **Rationale**: FR-013 ("ahead of any other status"). Resume already runs this command first and branches on the first
  word (`dmc-resume-campaign` § 1), so the closed branch costs one clause there and no new call. When `dmc` fails,
  resume checks for the file itself with Glob before going on (FR-010: DMC can test the mark without `dmc`, and play
  never depends on `dmc` to start); always globbing instead was rejected as a tool call on every resume for what
  `dmc` already reports.

## R7. `verify` on a closed campaign checks bytes against the closing backup and writes nothing

- **Decision**: when the campaign is closed, `verify` does not run the migration check. It opens the backup the mark
  names (or `--backup`), checks it whole, and compares every campaign file but `backups/` with the backup's manifest by
  path and SHA-256. Unchanged: `verified <name>: closed, <N> files match <backup file>`, exit 0. Otherwise up to 40
  lines `changed: <path>`, `added: <path>`, `missing: <path>` (all with `--all`), then the one-line failure
  `verification failed: <n> files differ from <backup file>`, exit 1. `--list` and `--accounting` are refused for a
  closed campaign in one line. It never writes `.standard`, a `.verified.tsv` or anything else.
- **Rationale**: the spec's first assumption ("verify on a closed campaign checks and reports, and writes nothing"),
  and its edge case (a hand edit outside play is reported by `verify` against the closing backup). Today's verify would
  throw "no migration is pending" on a current campaign, so a closed one needs its own path. A content-unit check
  would miss added lines; for a frozen campaign the right test is the bytes.
- **Alternatives considered**: a new verb (`dmc campaign check`): one more name for players to learn, when the issue
  says "`verify` still works".
- **Deferred** (the user, 2026-11-11): verifying against any other export (a backup against the live campaign, an
  import against its file, an imported closed copy finding its source in `exports/`) is issue #189, which reuses this
  byte check. Here, a mark naming a backup that is not in `backups/` gets the existing `no backup <file>` line from
  `BackupFolder.Resolve`, and `--backup` names the file.

## R8. Every `dmc` writer refuses a closed campaign in one place

- **Decision**: a new `ClosedCampaign` class in `src/Dmc.Cli/Campaign/` holds the mark's name, its reading and writing,
  and one guard, `ClosedCampaign.Refuse(root, path, command)`, which finds whether a path lies under
  `adventures/<campaign>/` of a campaign with a mark and throws
  `<name> is closed: start a sequel to continue its story` (the `dmc <command>:` prefix comes from `CommandLine`, as
  for every failure). Called by: `campaign backup`, `restore` (plan and `--yes`), `standard --init`, `map` (the map file's
  folder), `pdf --out` and `digest show --out`. `close` checks the mark itself and says
  `<name> is already closed: start a sequel to continue its story`, the same advice in words that fit a second close
  (the user, 2026-11-11). `verify` takes R7's path instead. `redraw` calls `ClosedCampaign.IsClosed` in `MapRedraw.Pending` and returns null, so it prints nothing and
  exits 0 (FR-015).
- **Rationale**: FR-015 names the writers; the code search for `File.Write`, `File.Move`, `Directory.Create` and the
  rest found exactly these paths into a campaign folder (`Digester` writes to `adventures/.transcript-review` by
  default, which is no campaign, but `--out` can point anywhere). One guard means one message and one test of it.
  `CampaignRestorer.Recover` needs no guard: close refuses while a restore is staged, so a closed campaign never has
  anything to recover.
- **Alternatives considered**: a check in each command's own words (several messages for one rule, against Principle
  III); a check in `CommandLine` before every command (it cannot know which argument is a path into a campaign).

## R9. The kit: one new skill, three short edits, no template change

- **Decision**:
  - **`kit/skills/dmc-close-campaign/SKILL.md`** (new). Its description carries FR-001's two moments to suggest
    closing (the main story has ended; the last open thread is closed), since it is the one part of the skill every
    session loads; no memory of a declined suggestion is kept, because the two moments are each passed once. Its steps: 0, not mid-roll (as the migrate skill's step 0);
    1, run `dmc campaign close <c>` (the plan) and stop on a refusal, offering `dmc-migrate-campaign` for `behind`;
    2, ask once (FR-002), naming an open scene or fight if there is one; 3, the closing save: follow
    `dmc-campaign-full-save` steps 1 to 8 with only these differences: step 4 prunes and splits but archives nothing,
    and sweeps for the reason and the means of each fact; step 4 also writes each loose end as a canon line in the
    character's terms, promises and debts under **Prices, debts and promises**; step 5 gathers every secret from
    `dm-notes-archive.md` back into § Secrets and marks each `revealed sN`, `never revealed` or `abandoned`, and moves
    no secret out; step 6 replaces § Where things stand with § How it ended (R10); step 7 leaves the size lines for
    `summary.md`, `dm-notes.md` and `canon.md` (a closed campaign is never resumed into play); 4, run
    `dmc campaign close <c> --yes`; a rerun after a failed step 4 still runs steps 1 and 2 but skips step 3 when
    `summary.md` already has § How it ended and every secret has an outcome; 5, tell the player out of character that it is closed, where the two copies are,
    and that a sequel can start from it (`dmc-new-campaign`, with its Predecessor line).
  - **`dmc-resume-campaign`** § 1: one clause for `closed`: say it is closed, read nothing for play, start no session
    log, change none of its files, and offer a sequel. A closed campaign stops the skill there.
  - **`dmc-migrate-campaign`** § 1: `closed` is never migrated; say so and go on to the next campaign.
  - **`kit/dm-rules.md`**, the Accuracy paragraph that already names a sequel's earlier campaign: one clause, "a
    campaign whose folder holds `.closed` is closed: never change its files". This is FR-017's home in play, where DMC
    is in a sequel and could be asked to fix its predecessor.
  - **No template changes.** § How it ended is written only by the close skill; secret outcomes are a word at the end
    of an existing line. `dmc-campaign-full-save` gains nothing (FR-003).
- **Rationale**: the clarified answers (spec § Clarifications) and Principle III: the close skill points to the full
  save for every step it does not change. With no template change and no change to what an open campaign's files
  hold, no migration is needed (FR-020): campaigns in play are unchanged until closed, and the close writes what it
  needs.
- **Alternatives considered**: putting the "never change a closed campaign" rule only in the close and resume skills
  (neither is loaded in a sequel's session, where the risk is); a template section for the ending (a change to what
  campaign files hold, which would need a migration for every campaign, for a section that is empty until the end).

## R10. The ending record's shape

- **Decision**: § How it ended in `story/summary.md`, in place of § Where things stand: how the story ended, in a
  short paragraph; the in-world date; one line per PC and companion with their final level, condition and where they
  are; one line per recurring NPC with where they are; and, only when there is any, what in the campaign folder is not
  canon (test bouts, imports), named so a sequel's digest leaves it out. Older arcs stay compressed as every full save
  leaves them.
- **Rationale**: FR-008 and FR-009; `summary.md` is already "a briefing for a DM taking over", which is what a sequel's
  digest (issue #179 Phase B) will read first. The not-canon line is the issue's one-off instruction for The Silver Egg
  made general, because any campaign can hold test files.

## R11. Measured cost to play

- **Decision**: measured as bytes ÷ 4 (`.docs/decisions.md`, the skill descriptions entry) in the PR, for: the new
  skill's description (every session); the resume clause; the dm-rules clause. The migrate skill's clause is paid only
  while migrating; the close skill's body only while closing. Target (SC-006): under 100 tokens in all for an ordinary session,
  and 0 for an ordinary full save. The description keeps FR-001's two moments short (about: suggest only when the main
  story ends or the last open thread closes), because it is the largest of the three.

## R12. Tests and what CI cannot run

- **Decision**: MSTest in `tests/Dmc.Tests/Campaign/`, temporary DMC folders from `Support/DmcFolders`:
  `CampaignCloseTests` (plan and `--yes`, every refusal, the line); `CloseFailureTests` (an injected failure at each of
  steps 3 to 5 through an internal fault seam on `CampaignCloser`, SC-004); `ClosedCampaignFreezeTests` (checksums of
  every file before and after every writing verb, `map`, `redraw`, `pdf --out`, `digest show --out`, `lookup`, `recall`,
  `export`, `show`, `verify`, SC-001); `ClosingBackupTests` (10 rounds of backup and `--prune old` on an open copy of
  the same files, then the closing name untouched, SC-002; import of the closing backup makes a closed campaign,
  FR-018); `StandardTests` and `CampaignVerifyClosedTests` (R6, R7). Toolkit tests: the skill list in `ToolkitTests`
  gains `dmc-close-campaign`; the settings test checks the new deny rule; the existing prompt checks prove the
  commands and skill names the kit names are real (FR-021).
- **Not on CI**: the closing save and resume are model behaviour (SC-003, SC-005). They are checked by a live run in a
  scratch DMC folder on a copy of a finished test campaign (quickstart.md, scenarios 5 and 6), recorded in the PR, as
  spec 005 did for its migrations.
