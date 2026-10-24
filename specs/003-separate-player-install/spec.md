# Feature Specification: Separate the Player Install from the Toolkit Source

**Feature Branch**: `003-separate-player-install`

**Created**: 2026-10-23

**Status**: Draft

**Input**: User description: "read issue #67. I want to separate the raw code base from what the player installs to make development and playing more separate and less violate." ("violate" read as "volatile".)

**Source issue**: #67, *Ship DMC as one installed program: dmc carries the kit, the player's folder holds only their game*
(closed as not planned on 2026-10-23, before this spec). Its research is the starting point; its design is replaced by
the user's direction of 2026-10-24 (Clarifications).

## Clarifications

### Session 2026-10-23

- Q: When a player types a command to run a DMC skill, should they see `/dmc:resume-campaign` or `/resume-campaign`? → A: `/dmc:<skill>`. *Superseded on 2026-10-24: skills are named `dmc-<skill>`.*
- Q: Should this feature include a built-in update command, or should running the install step again be the update? → A: A built-in update command (`dmc update`) is part of this feature.
- Q: After the switch, should playing from the repository work only through the installed program, or also through the repository's launcher scripts? → A: Only through the installed program; the launcher scripts and the download script are removed.

### Session 2026-10-24

- Q: What is `dmc` responsible for? → A: Everything outside the conversation with Claude: installing, setting up, updating, starting play, dice, maps and the rest. The prompts (rules and skills) tell DMC which `dmc` commands to run.
- Q: How does the constitution treat installing `dmc`? → A: The constitution changes: players install Claude Code and `dmc`.
- Q: Where do the Dungeon Master files go? → A: `dmc` generates them at install, in a folder the player chooses (default `~/dmc`).
- Q: Is that folder also where the player's adventures live and where they play? → A: Yes, one folder: the generated files beside the player's `adventures/`, `feedback/`, `modules/` and `rules/`.
- Q: Where do the generated files come from? → A: They are built into the `dmc` program and written out by it.
- Q: Who does the install? → A: `dmc` installs itself: a one-line bootstrap downloads it and runs `dmc install`.
- Q: How does an update work? → A: `dmc` updates in place from a published release and rewrites the generated files when they are out of date. It never touches the player's files during an update or an install.
- Q: How are the player skills named? → A: With a `dmc-` prefix (`/dmc-resume-campaign`), so typing `/dmc` lists them all, with or without `dmc play`.
- Q: How is feedback kept, given that it tunes the generated files and an update replaces them? → A: Feedback entries stay in the player's `feedback/`, which updates never touch. Only developers tune the files, in the repository; a play session cannot edit the generated files. Player options are never touched.
- Q: Should this feature include a way for a player to send feedback to the developers? → A: No; a later issue.
- Q: How are downloads trusted, now that no checksum pin is committed in the repository? → A: Immutable releases, published only after the maintainer approves, with each download checked against the hash GitHub reports and the release's checksum list (option 3); and that checksum list signed, with the public keys built into `dmc`, so an update refuses anything not signed by the project's key (option 4). The private key is kept as a secret of the approval-gated release step.
- Q: What does a development build's `--program` flag become? → A: `--force`.

## Background

Today a player clones this repository and plays inside it. Their campaigns, feedback, modules and rules sit,
git-ignored, beside the source code, the tests, the developer documents and the developer skills. The Dungeon Master
rules are the repository's own instructions file, so the same file serves both the Dungeon Master and toolkit
development. Whatever is on the branch the player has checked out is what they play.

This feature splits the two. The player installs one program, `dmc`. It writes the Dungeon Master files (the rules,
the player skills, the templates and the play settings) into a **DMC folder** the player chooses, where the player's
own files also live and where they play. Releases update `dmc` in place, and `dmc` rewrites the Dungeon Master files
it owns, never the player's. The repository stays where the toolkit is developed, and where developers turn feedback
into better Dungeon Master files.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A new player installs DMC and plays without the repository (Priority: P1)

A player who has Claude Code runs one documented install line. `dmc` asks where to put the DMC folder (default
`~/dmc`), writes the Dungeon Master files there, creates the folders for the player's own files, and makes itself
available from any terminal. The player starts play and the Dungeon Master behaves exactly as it does today. They
never clone this repository, and the DMC folder holds no source code, tests, developer documents or developer skills.

**Why this priority**: This is the separation itself. Without it nothing else in the feature has a purpose, and with
it alone a player already gets a game that does not move under them.

**Independent Test**: On a machine with Claude Code and no copy of the repository, run the install line, accept the
default folder, start play and run one scene of a new campaign. Check the DMC folder's contents, that the Dungeon
Master follows its rules, that every player skill works and that dice and maps work.

**Acceptance Scenarios**:

1. **Given** a machine with Claude Code and no DMC, **When** the player runs the install line, **Then** `dmc` asks for
   the DMC folder, offering `~/dmc`, and accepts the default on Enter.
2. **Given** the install has finished, **When** the player opens a new terminal, **Then** `dmc` runs from any folder.
3. **Given** the chosen folder, **When** the install has finished, **Then** it holds the Dungeon Master rules, the
   player skills, the play settings, the templates, and empty `adventures/`, `feedback/`, `modules/` and `rules/`
   folders, and nothing else.
4. **Given** the DMC folder, **When** the player starts play (with `dmc play`, or with Claude Code started there),
   **Then** the session runs with the Dungeon Master rules, the player skills, the play settings (permissions, hidden
   skills, hooks) and dice, exactly as a session from the repository does today.
5. **Given** a play session, **When** the player types `/dmc`, **Then** every player skill is listed (for example
   `/dmc-resume-campaign`) and no developer skill (feedback review, transcript review, Spec Kit) exists.
6. **Given** a play session, **When** the Dungeon Master rolls dice or draws a map from either the bash or the
   PowerShell shell, **Then** `dmc` is found by its bare name.

---

### User Story 2 - The player's files survive every install and update (Priority: P1)

A player's campaigns, player options, feedback, modules and rules are never changed, moved or deleted by `dmc`
installing or updating. Only the files `dmc` itself generated are ever rewritten.

**Why this priority**: The user's hard rule. Losing a campaign or a feedback entry to an update would break the
first principle (the story stays consistent) and the trust that makes updating safe.

**Independent Test**: Fill a DMC folder with campaigns, player options, feedback entries, a module and a rules file;
record a hash of every file under the player's folders; run an update (and a repeat install over the same folder);
compare the hashes.

**Acceptance Scenarios**:

1. **Given** a DMC folder with player files, **When** `dmc` updates, **Then** every file under `adventures/`,
   `feedback/`, `modules/`, `rules/` and `exports/` is byte-for-byte unchanged and none is added or removed.
2. **Given** a DMC folder with player files, **When** the player runs the install again over the same folder,
   **Then** the same holds.
3. **Given** the general and the campaign player options, **When** `dmc` installs or updates, **Then** they are
   unchanged.
4. **Given** a file the player added among the generated files (their own skill, their own local settings), **When**
   `dmc` rewrites the generated files, **Then** that file is unchanged.

---

### User Story 3 - The player only ever plays a released version, and updates in place (Priority: P1)

A player's game changes only when they update. Work in progress in the repository never reaches them. `dmc update`
replaces the program from the latest published release and brings the generated files in line with it.

**Why this priority**: This is the "less volatile" half of the request, and how fixes reach players.

**Independent Test**: Install a release, change a prompt on a branch of the repository, and check play still uses the
released prompt. Publish a newer release, run `dmc update`, and check the program version and the generated files
both match it, and the player's files are untouched.

**Acceptance Scenarios**:

1. **Given** an installed release, **When** the repository changes on any branch, **Then** the player's sessions are
   unaffected until they update.
2. **Given** a newer release is published, **When** the player runs `dmc update`, **Then** the program is replaced in
   place, the generated files are rewritten to that release's, and the player is told the new version in one line.
3. **Given** the latest release is installed, **When** the player runs `dmc update`, **Then** it says so and changes
   nothing.
4. **Given** a download whose checksum does not match the release's published checksum, **When** `dmc update` runs,
   **Then** nothing is installed and the player is told why in one line.
5. **Given** a release whose checksum list is not signed by a project key built into `dmc`, **When** `dmc update`
   runs, **Then** nothing is installed and the player is told why in one line.
6. **Given** generated files that are out of date or damaged for the installed program (a file missing or changed),
   **When** the player starts play or runs the rewrite command, **Then** the generated files are rewritten to match
   the program before play starts.

---

### User Story 4 - Feedback stays with the player; developers tune the files (Priority: P2)

The player records feedback in play as today. The entries stay in their DMC folder across updates, each noting the
`dmc` version it was written against. A developer reviews those entries from the repository, changes the Dungeon
Master files there, and players get the improvement through `dmc update`. A play session cannot change the generated
files, so nothing tuned locally is ever lost.

**Why this priority**: Feedback is how DMC improves. It must not be lost, and tuning must happen where it survives
releases. It ranks below the player's core path because only the maintainer reviews feedback today.

**Independent Test**: Record a feedback entry in play, update, and check the entry is unchanged and names the version
it was recorded under. In a play session, ask DMC to edit a generated file: it is refused. From the repository, run
the feedback review against the DMC folder's path and check it reads the entry.

**Acceptance Scenarios**:

1. **Given** a recorded feedback entry, **When** `dmc` updates, **Then** the entry is unchanged.
2. **Given** a new feedback entry, **When** it is recorded, **Then** it notes the `dmc` version in use.
3. **Given** a play session, **When** DMC tries to edit or write a generated file, **Then** the edit is refused.
4. **Given** the repository, **When** a developer runs the feedback or transcript review, **Then** it reads the
   feedback and campaigns of the DMC folder `dmc` has recorded, or of a folder the developer names.

---

### User Story 5 - A developer tries a change from source (Priority: P2)

The maintainer edits a rule or skill in the repository, builds `dmc`, and uses that build to write the changed files
into a scratch DMC folder, then plays there. The repository holds no campaigns, and Claude Code started in the
repository gets only the developer instructions and skills.

**Why this priority**: Development keeps working, apart from play. Only the maintainer uses it.

**Independent Test**: Edit a rule, build, run the build's install into a scratch folder, play, and check the edited
rule is used. Start Claude Code in the repository root and check it gets the developer instructions and not the
Dungeon Master rules.

**Acceptance Scenarios**:

1. **Given** an edited rule and a local build, **When** the developer installs that build into a scratch folder,
   **Then** a session there uses the edited rule.
2. **Given** the repository root, **When** a developer starts Claude Code there, **Then** the session gets the
   developer instructions and developer skills, and not the Dungeon Master rules or player skills.
3. **Given** the repository after the change, **When** a developer looks for another way to start play from it,
   **Then** there is none: no launcher script and no download script remain.

---

### User Story 6 - The maintainer moves their existing campaigns into a DMC folder (Priority: P2)

The maintainer installs DMC, moves their campaigns, feedback, modules and rules from the repository into the DMC
folder, and carries on each campaign exactly where it stopped.

**Why this priority**: The only current player has live campaigns in the repository; a one-time step for one person.

**Independent Test**: Follow the documented steps on a copy of the maintainer's files, resume a campaign and play one
scene.

**Acceptance Scenarios**:

1. **Given** campaigns in the repository's git-ignored folders, **When** the maintainer follows the migration steps,
   **Then** they are given the commands to move them; DMC never moves or deletes them itself.
2. **Given** the migrated DMC folder, **When** a campaign is resumed, **Then** the recap matches the files and play
   continues from where it stopped, with the active-campaign marker, player options and maps intact.

---

### Edge Cases

- The chosen folder already exists with files in it: the install adds the generated files and any missing player
  folders, changes nothing else, and refuses if a file it would generate is in the way and was not generated by
  `dmc` (it names the file).
- The chosen folder is the repository, or inside it: the install refuses.
- The install is run without a terminal to ask in: it uses the default folder, or the folder given on its command
  line.
- The player edited a generated file by hand: the rewrite replaces it and keeps a copy of the edited one, and says so
  in one line.
- A generated file from an older release is no longer part of the new one (a skill was removed or renamed): the
  rewrite removes it, but only if `dmc` generated it and it is unchanged.
- `dmc play` is run outside a DMC folder: it plays in the DMC folder recorded at install, or, if none is recorded,
  refuses in one line and says to run the install.
- The update is interrupted (no network, a failed checksum, a file in use): the installed program and the generated
  files stay as they were, and the player is told in one line what failed.
- A release's signature is missing, invalid or made with an unknown key: the update refuses and installs nothing.
- `dmc update` is asked for from inside a play session: refused; it is run from a terminal.
- A development build is asked to update: refused, it is not a release.
- Claude Code is not installed: starting play says so in one line.
- A context compaction in a play session: the rules still apply, as today.

## Requirements *(mandatory)*

### Functional Requirements

**`dmc` and the prompts**

- **FR-001**: `dmc` MUST provide every operation outside the conversation with Claude that installing, setting up,
  updating and playing DMC needs, in addition to its current commands. The rules and skills MUST tell DMC to run
  `dmc` commands for these, and MUST call `dmc` by its bare name.

**The generated files**

- **FR-002**: The repository MUST hold, in one distinct place, the source of every file `dmc` generates: the Dungeon
  Master rules, the player skills, the templates and the play settings. Each `dmc` build MUST carry those files
  inside the program.
- **FR-003**: The Dungeon Master rules MUST be separated from the developer instructions. The repository's own
  instructions file MUST hold only what toolkit development needs.
- **FR-004**: The player skills MUST be named `dmc-<skill>` (for example `dmc-resume-campaign`), and every prompt
  that names one MUST use that name. The developer skills (feedback review, transcript review, Spec Kit) MUST NOT be
  generated into a DMC folder.
- **FR-005**: The play settings MUST refuse DMC's edit and write tools on every generated file, and no rule, skill or
  template may tell DMC to write one. A generated file changed any other way MUST be put back by the next `dmc play`,
  with the changed copy kept (FR-015).
- **FR-006**: The generated files MUST name each other (templates, skills) by paths relative to the DMC folder, and
  MUST name no repository path.

**The DMC folder**

- **FR-007**: `dmc install` MUST ask for the DMC folder, offering `~/dmc` as the default, unless a folder is given on
  its command line; with no terminal to ask in, it MUST use the default.
- **FR-008**: `dmc install` MUST write the generated files into the DMC folder, create any missing `adventures/`,
  `feedback/`, `modules/` and `rules/` folders, and write nothing inside those folders.
- **FR-009**: `dmc install` MUST record the DMC folder so that other `dmc` commands can find it from any directory.
- **FR-010**: `dmc install` MUST make `dmc` available by its bare name in new terminals for the current user, without
  administrator rights.
- **FR-011**: `dmc play` MUST start Claude Code in the DMC folder (the current folder if it is one, else the recorded
  one), with `dmc` reachable by bare name from every shell the session and its hooks use, and keep today's model,
  effort, argument pass-through and exit-code behaviour. Claude Code started in the DMC folder without `dmc play`
  MUST still load the rules, skills and settings.

**Protecting the player's files**

- **FR-012**: No install, update or rewrite MUST ever create, change, move or delete anything under `adventures/`,
  `feedback/`, `modules/`, `rules/` or `exports/`, or any file in the DMC folder that `dmc` did not generate.
- **FR-013**: `dmc` MUST record which files it generated, with their content, so that a rewrite changes only those
  files and can tell when one was changed by hand. A hand-changed generated file MUST be kept as a copy before it is
  replaced.

**Updating**

- **FR-014**: `dmc update` MUST replace the installed program in place with the latest published release for this
  machine, after verifying that the release's checksum list carries a valid signature from one of the project keys
  built into `dmc`, and that the download matches both that list and the hash GitHub reports for the file; and then
  rewrite the generated files to that release's. It MUST say in one line which version is now installed, or that
  the latest is already installed. If any step before the replacement fails, the program and the generated files MUST stay as they were.
- **FR-015**: `dmc` MUST be able to rewrite the generated files on demand, and `dmc play` MUST do so before starting
  Claude Code when they do not match the program (missing, changed, or from another version).
- **FR-016**: A play session MUST NOT be able to run `dmc update`, the install or the rewrite of the generated files. The
  program itself MUST refuse them when run from inside a Claude Code session, in one line, whatever the command's
  spelling; the play settings' deny rules are a second guard.

**Feedback**

- **FR-017**: Feedback entries MUST stay in the player's `feedback/` (protected by FR-012), and each new entry MUST
  record the `dmc` version in use.
- **FR-018**: The feedback and transcript review skills MUST stay developer skills in the repository and MUST read
  the DMC folder `dmc` recorded, or a folder the developer names.

**Development**

- **FR-019**: A developer MUST be able to install a local build into any folder, so a change can be tried from source
  without a release.
- **FR-020**: After the change, the only way to play MUST be a DMC folder written by `dmc`. The repository's launcher
  scripts, the script that downloads `dmc` into a checkout, and the mechanisms that keep a checkout and its downloaded
  `dmc` in step MUST be removed, along with every prompt, test and document that names them.
- **FR-021**: The toolkit tests MUST check the generated files' source as they check the prompts today, and MUST fail
  if a generated file would land in a player folder.

**Installing and switching over**

- **FR-022**: The player MUST be able to install with one documented line per operating system, which downloads the
  release for their machine, verifies it against the release's checksum list and the hash GitHub reports (the list alone, said in one line, when
  GitHub's limit on requests without sign-in is reached), and hands over to `dmc install`.

- **FR-023**: The README MUST be written for players (install Claude Code, run the install line, play), with
  developer setup moved to the developer documents.
- **FR-024**: The documentation MUST give the maintainer the commands to move their existing campaigns, feedback,
  modules and rules into a DMC folder; DMC MUST NOT move or delete them itself.
- **FR-025**: The constitution MUST be amended so that players install Claude Code and `dmc`, with the decision log
  recording this route, the decisions it supersedes, and why the alternatives (a Claude Code plugin, a separate player
  repository, an MCP server, an Agent SDK app) were not taken.

**Release trust**

- **FR-026**: Releases MUST be immutable once published (their files and tag cannot be changed), and MUST be published
  only after the maintainer approves the release step. Every file is attached before the release is published.
- **FR-027**: Each release MUST carry a signature of its checksum list, made with the project's private key, which is
  available only to the approval-gated release step. `dmc` MUST carry two public keys (an active one and an offline
  backup), so that a lost or retired active key does not strand installed copies.
- **FR-028**: `dmc update` MUST refuse a release whose signature is missing or invalid, or whose version is not newer
  than the installed one, and no setting or environment variable MUST be able to switch those checks off in a release
  build.

### Key Entities

- **Generated files**: the Dungeon Master rules, player skills, templates and play settings, carried inside `dmc` and
  written into a DMC folder. Owned by `dmc`; rewritten on update; never edited in play.
- **DMC folder**: the folder the player chose at install (default `~/dmc`). Holds the generated files, a record of
  what was generated, and the player's own folders. Play happens here.
- **Player files**: everything under `adventures/` (campaigns and player options), `feedback/`, `modules/`, `rules/`
  and `exports/`, and anything else the player added. Never touched by `dmc`'s install or update.
- **Installed program**: `dmc` and its native libraries in a per-user location, on the user's PATH, with a record of
  the DMC folder.
- **Feedback entry**: a file in `feedback/` written by the feedback skill, naming the `dmc` version; read by
  developers, never by the player's own sessions to change files.
- **Developer instructions**: the repository's own instructions file after the split; loaded only in the repository.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A new player goes from "Claude Code installed" to the first scene of a new campaign with one install
  line, one answer (or Enter for the default folder) and one command to start play, without cloning the repository.
- **SC-002**: Across an install, a repeat install and an update, 0 player files are created, changed, moved or deleted.
- **SC-003**: A DMC folder contains 0 source, test, developer document or developer skill files.
- **SC-004**: Typing `/dmc` in a play session lists all player skills and 0 developer skills.
- **SC-005**: The context used at the start of a play session is no larger than today's.
- **SC-006**: A change pushed to any branch of the repository changes 0 sessions for a player until they update.
- **SC-007**: After any successful update, the program version and the generated files belong to the same release in
  100% of cases.
- **SC-008**: Every Dungeon Master behaviour that works from the repository today (resume, saves, dice, maps,
  feedback recording, rewind, module ingest, rules setup, hooks) works the same in a DMC folder.
- **SC-009**: The maintainer resumes every existing campaign after migration with no campaign file lost or rewritten,
  and plays one scene in a fresh DMC folder and one in the migrated one before the change is finished.

## Assumptions

- The generated files are small (well under a megabyte), so carrying them inside the program does not matter to its
  size.
- One DMC folder per install is the normal case. A second folder can be made by running the install again with
  another path; updates rewrite every recorded folder that still exists.
- The program itself lives in a standard per-user location (not inside the DMC folder), on the supported platforms
  only: Windows x64 and Arm, Linux x64 and Arm.
- Updates are trusted through the project's signing key; the first install, which fetches the install line from the
  release itself, is trusted through HTTPS, immutable releases and the hash checks, as almost every scripted installer
  is. Today's checksum pin committed in the repository goes away.
- The maintainer turns on immutable releases and creates the approval-gated release environment and its key secret in
  the repository settings; these are settings, not code, and the release documentation says how.
- Package managers (winget, Scoop, Homebrew) are out of scope.
- A way for players to send feedback to the developers is out of scope (a later issue).
- The change needs a release of `dmc`: the next one after the current.
- Work is tracked by one issue and delivered in phases, each its own PR, per `.docs/development.md` § Spec-driven
  changes. #67 is closed, so the tracking issue is #67 reopened or a new one; the user decides.
- The toolkit continues to assume one human player, and nothing here stops more being added later.
