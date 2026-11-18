# Feature Specification: Git-friendly campaigns

**Feature Branch**: `018-git-friendly-campaigns`

**Created**: 2026-11-17

**Status**: Draft

**Input**: User description: "I want the adventures aka campaigns to be git friendly"

## Background

### Where campaigns live today

A player's campaigns are in `adventures/<campaign>/` of their **DMC folder**, which "is not a git checkout"
(`.docs/campaign-files.md:58`). The toolkit's own repository ignores `adventures/*` because campaigns are personal
(`.gitignore`), and that stays so: this feature is about the player's own copy of their campaigns, not the toolkit's
repository. Today a player who wants history, an off-machine copy or the same campaign on two computers has
`dmc campaign export` (a ZIP in `exports/`) and the migration backups, and nothing else.

### What gets in the way of putting a campaign in git

Measured on the maintainer's DMC folder, `D:\Claude\dmc`, on 2026-11-17 (not a git repository):

- **Large binaries that change often.** `adventures/rorns-landing/` is 4.2 MB, and most of that is
  `backups/` (three ZIPs of 716 KB, 741 KB and 783 KB, one per migration, each a whole copy of the campaign) and the
  map outputs `maps/live.png` (96 KB) and `maps/live-data.js` (54 KB), which `dmc map` rewrites every time anyone
  moves (`.docs/campaign-files.md:50-51,291`). `maps/live.html`, `maps/live-source.txt` and `maps/live.json` change at
  the same moments. Every redraw would be a new binary version in the history.
- **Mixed line endings.** Of rorns-landing's text files, nine have CRLF line endings and the rest LF (`file` on each
  `.md`, `.json`, `.tsv` and `.txt`). Git on Windows with its usual line-ending conversion would rewrite some of them
  on check-in or check-out, and every such file would show as changed in full.
- **Checks that hash the bytes.** `backups/<backup>.zip.verified.tsv` holds each file's SHA-256, and a restore warns
  about play since the migration when a file's hash differs (`src/Dmc.Cli/Campaign/CampaignRestorer.cs:100`). Export
  manifests hold SHA-256 too. A line-ending conversion by git changes the hash although nothing in the story changed.
- **Per-machine state beside the campaigns.** `adventures/.active-campaign`, `.reviewed-sessions`,
  `.transcript-review/` and the staging folders of a running import or restore (`.dmc-import-<id>/`,
  `.restore-<campaign>-<id>/`, `.replaced-<campaign>-<id>/`) are in `adventures/` (`.docs/campaign-files.md:17-22`).
  They describe one computer's play, not the campaign.
- **Search stops at ignored files.** DMC searches archives, session logs, digests and feedback with the Grep tool,
  which uses ripgrep, and ripgrep skips git-ignored files inside a git repository. This already broke feedback search
  once (`.docs/decisions.md:221-224`). An ignore rule that catches a file DMC searches would hide it from play without
  any error.
- **Long lines.** Campaign files keep a paragraph or a fact on one line; in rorns-landing 126 of its 144 text files have
  a line longer than 300 characters and the longest is 5,028. A one-word edit shows as a whole changed line in a diff.
- **What play may run.** The play settings deny `git clean` (`kit/settings.json:50,65`). Nothing else about git is
  set for play.

### Limits of this spec

Two campaigns played at once in two terminals is out of scope (`.docs/user-guide.md:400`); that holds here. The modules (`modules/`, copyrighted), the rules reference (`rules/`),
`feedback/` and `exports/` are not campaigns and sit outside the player's repository.

### Decisions of 2026-11-17

- **Q1, where the repository sits: B.** One repository for `adventures/`: all campaigns and the general player
  options, and only player data. Not the whole DMC folder (it holds copyrighted modules and files `dmc` writes) and not
  one per campaign.
- **Q2, what git does: A.** Git-ready only. The player runs git; DMC makes the files safe and steady to keep in git and
  the guide shows how. No commits by DMC, no play from more than one computer.
- **Plan amendment, approved 2026-11-17:** FR-004's migration is dropped, FR-003 covers `dmc`'s writes only, and FR-005 chooses
  line-ending-blind comparison (plan.md § Summary; research R1 to R4).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Put a campaign in git without noise (Priority: P1)

A player decides to keep their campaigns in git. They follow the user guide, make `adventures/` of their DMC folder a
git repository (one repository for all their campaigns and the general player options), and commit. After a session of play, the changes they see are the files the session really changed: the
session log, game state, canon and the sheets that changed. They do not see backup ZIPs, map renders, per-machine
state or files whose only change is their line endings.

**Why this priority**: Without it, the history is mostly binary noise and every commit is several megabytes, and
nothing else in this feature is worth having.

**Independent Test**: Make a repository as the user guide says, on a copy of rorns-landing, play one scene with a
map move, and list what git reports as changed.

**Acceptance Scenarios**:

1. **Given** a new repository made as the guide says, **When** the player commits for the first time, **Then** no
   backup ZIP, no `maps/live.*` render, no per-machine file and no staging folder is committed.
2. **Given** a committed campaign, **When** a scene is played that moves a token and changes the game state,
   **Then** git reports only the campaign's text files that the scene changed, and `maps/live.json` if it is kept.
3. **Given** a committed campaign on Windows, **When** the player checks it out again or commits again without
   playing, **Then** git reports no change.

---

### User Story 2 - DMC's own checks still work on a git copy (Priority: P1)

A player who keeps their campaign in git clones it, or checks out an older commit, and then resumes, migrates,
verifies, exports, restores or searches as before. Every `dmc` command and every skill works as it does in a folder
that is not under git.

**Why this priority**: Core 1 and Development 2 of the constitution. A campaign that git keeps but DMC can no longer
check or search is a broken campaign.

**Independent Test**: Run the existing campaign commands and a play resume against a campaign copied out of a git
clone, on Windows with git's usual line-ending setting, and compare with the same campaign outside git.

**Acceptance Scenarios**:

1. **Given** a campaign checked out from git, **When** the player runs a verify or a restore that compares hashes,
   **Then** no file is reported as changed unless its content changed.
2. **Given** `adventures/` as a git repository with the ignore rules the guide gives, **When** DMC searches an
   archive, a session log or a digest in play, **Then** it finds what it would find outside git. (`feedback/`,
   `modules/` and `rules/` are outside the repository, so its rules do not reach them.)
3. **Given** a campaign checked out from git, **When** the player resumes it, **Then** the resume reads and reports
   the same as for the same files outside git.

---

### User Story 3 - Files DMC writes are stable between saves (Priority: P2)

Every text file `dmc` writes in a campaign uses LF, git stores every text file with LF, and a save that changes
nothing about a fact leaves its line unchanged, so a diff between two commits shows what happened in the story.

**Why this priority**: It is what makes the history readable, and it helps anyone who compares two versions of a
campaign, with or without git.

**Independent Test**: Run a full save twice in a row on a copy of a campaign with no play between them and compare
the files byte for byte; check the line endings of every text file after a session.

**Acceptance Scenarios**:

1. **Given** a campaign with mixed line endings, **When** it is committed, **Then** git stores every text file with
   one line-ending style, and `dmc` treats a file whose only change is its line endings as unchanged.
2. **Given** a full save just made, **When** a second full save runs with no play between them, **Then** no file
   changes except for the lines that record when the save ran.
3. **Given** a map file DMC saves, **When** it is saved again with the same content, **Then** its bytes are the same.

---

### User Story 4 - The player runs git; the guide shows how (Priority: P3)

Git is the player's own tool, for history and an off-machine copy. DMC and `dmc` run no git command, and play is the
same with or without it. The user guide tells a player who is not a developer how to make the repository, what it
leaves out and why, when to commit (after a full save, and straight after a migration), how to keep it private, and how
to move their campaigns to a new computer.

**Why this priority**: The first three stories make a campaign safe to keep in git; without the guide most players
would not know how to start.

**Independent Test**: A person who has not used git before follows the guide on a copy of a campaign, commits after
a session, and resumes the campaign from a clone in a second DMC folder.

**Acceptance Scenarios**:

1. **Given** the guide, **When** a player follows it on a new computer with a fresh `dmc install`, **Then** they end
   with their campaigns cloned into `adventures/` and the campaign resumes.
2. **Given** a campaign in git, **When** the player plays, saves, rewinds or migrates, **Then** DMC runs no git
   command and does what it does today.

---

### Edge Cases

- **A campaign already under git with CRLF files.** Adding the line-ending rules to an existing repository makes git
  re-store those files once; the guide says to commit straight after.
- **A player who wants the backups in git.** The backups are never deleted by this feature; ignoring them only keeps
  them out of the history. The player can choose to commit them. Closing backups are kept in git by default (FR-001).
- **`maps/live.json`.** It is the scene on the player's screen and part of resuming exactly where play stopped, so
  it is not per-machine state; whether it is kept in git follows from Core 1 (it is kept).
- **Two computers.** Playing the same campaigns from two computers in turn is out of scope (Q2). A player who does it
  anyway risks merge conflicts in files DMC treats as its memory; the guide says to keep one computer as the place
  of play and to use the repository as its copy.
- **A checkout with no map renders.** `dmc redraw` decides from `maps/live-source.txt` and modification times
  whether to redraw (`.docs/campaign-files.md:291`); on a fresh checkout the renders are missing, and the map must be
  drawn again before the player sees it.
- **A new computer.** The repository holds only `adventures/`, so a new computer needs `dmc install` first to make
  the DMC folder, and the clone goes into its `adventures/`; `modules/` and `rules/` come back the way they came the
  first time (ingest and setup).
- **Closed campaigns and sequels.** The closing backup (`backups/<campaign>_closed_<stamp>.zip`) is the record of a
  closed campaign and is "never counted, pruned or removed" (`.docs/campaign-files.md:28`). A sequel reads it, so it
  is kept in git, and a sequel can start on a new computer.
- **Spoilers.** `story/dm-notes.md`, the archives, NPC sheets and `module-progress.md` hold spoilers, and a module
  campaign quotes the module. The guide says a repository with a campaign in it should be private.
- **The Grep tool and ignore rules.** Any file the guide's rules ignore must be one DMC never searches in play.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The toolkit MUST give the player a ready set of git ignore rules for their campaigns that leaves out the
  migration backups (keeping each closed campaign's closing backup), the map renders (`maps/live.png`, `maps/live-data.js`, `maps/live.html`, `maps/live-source.txt`),
  per-machine state (`.active-campaign`, `.reviewed-sessions`, `.transcript-review/`) and the import and restore
  staging folders, and that keeps every file the resume reads and every file DMC searches in play. The rules are for a repository at
  `adventures/`. `dmc` never writes `adventures/` (`.docs/campaign-files.md:58`), and this feature keeps that: the
  plan decides how the player gets the rules there (for example a copy in the DMC folder's `templates/` that the guide
  tells them to copy).
- **FR-002**: The toolkit MUST give the player line-ending rules for git so that campaign files are stored and checked
  out with one line-ending style on every platform DMC supports.
- **FR-003** *(amended by the plan, research R1)*: Every text file `dmc` writes in a campaign MUST use LF on every
  platform. DMC's own writes in play are left as they are; git and `dmc` absorb the difference (FR-002, FR-005).
- **FR-004** *(amended by the plan, research R4)*: No migration changes line endings. Git stores text with one style
  on commit (FR-002), and `dmc` ignores line endings when it compares (FR-005); DMC's own shell writes in play would
  undo a migration.
- **FR-005**: Every check that compares a campaign file with a backup or a verified record (the restore's count, the
  closed-campaign check of verify and the sequel import) MUST give the same result for a file whose only difference is
  its line endings (plan: contract `line-ending-compare.md`).
- **FR-006**: A file `dmc` writes MUST have the same bytes when written twice from the same content (no timestamps or
  reordering that change on their own), except files whose purpose is to record a time.
- **FR-007**: No ignore rule the toolkit gives MAY hide a file the Grep tool must find in play; a toolkit test MUST
  check the rules against the files the resume reads and the searches the prompts name.
- **FR-008**: The user guide MUST say, for a player who is not a developer, how to keep campaigns in git, what is
  left out and why, that the repository should be private, and how to bring a campaign to a new computer.
- **FR-009**: Nothing in this feature MAY delete or move a player's file; files that git ignores stay on disk.
- **FR-010**: Install and upgrade MUST stay one step each, and git MUST NOT become something a player has to install
  to play (Development 3).
- **FR-011**: DMC's prompts, skills and `dmc` MUST NOT run git commands or depend on git being present: the player
  runs git. Commits made by DMC, and playing one campaign from more than one computer, are out of scope.

### Key Entities

- **Campaign file**: a file under `adventures/<campaign>/` that is part of the campaign's memory (the files listed in
  `.docs/campaign-files.md` § What each file holds, the saved maps and `maps/live.json`). Kept in git.
- **Derived file**: a file DMC can make again from campaign files: the map renders. Left out of git.
- **Backup**: a ZIP under `backups/` with its `.tsv` records, a whole copy of the campaign at a point in time. A
  migration backup is left out of git; a closing backup is kept.
- **Per-machine state**: what one computer's play needs and another's does not: the active campaign, the reviewed
  sessions, the transcript review output, staging folders. Left out of git.
- **Ignore and line-ending rules**: the files the toolkit gives the player to put at the root of their repository,
  `adventures/`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a copy of rorns-landing put in git as the guide says, the first commit is at most 25% of the
  campaign folder's size on disk (today 4.2 MB, of which about 2.2 MB is backups and 150 KB map renders).
- **SC-002**: After one played scene with a map move, git reports changes only in campaign files that the scene
  changed: zero derived files, zero backups, zero per-machine files.
- **SC-003**: After a clone or a fresh checkout on Windows and on Linux, git reports zero changed files, and a
  verify against the latest backup reports zero files changed by line endings alone.
- **SC-004**: Every search the play prompts name finds the same results inside a git repository with the given rules
  as outside one (a toolkit test, 100% of the named searches).
- **SC-005**: Two full saves in a row with no play between them change zero bytes outside the lines that record the
  save's time.
- **SC-006**: A person new to git follows the guide on a copy of a campaign, without help, and resumes it from a
  clone on a second folder.

## Assumptions

- The player owns their git hosting, if any, and keeps the repository private; DMC sends nothing anywhere itself.
- Git is optional. A player who never uses git sees no difference in play.
- Keeping one fact or paragraph per line stays as it is: rewrapping campaign files to shorten diffs would cost
  tokens in every read and change what the size checks measure. Long lines are a known limit of diffs, not something
  this feature changes.
- The backups and exports keep doing their job (a whole campaign in one checked file); this feature does not replace
  them with git history.
- `maps/live.json` and the saved place maps (`maps/<place>.json`) are campaign files and are kept; only the renders
  are derived.
- The toolkit's own repository keeps ignoring `adventures/`, `modules/`, `rules/`, `feedback/` and `exports/`.
- Two campaigns played at once in two terminals stays out of scope.
