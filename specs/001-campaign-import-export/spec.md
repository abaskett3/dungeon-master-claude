# Feature Specification: Campaign Export and Import

**Feature Branch**: `001-campaign-import-export`

**Created**: 2026-10-21

**Status**: Draft

**Input**: User description: "create commands that are able to import and export the campains from the dmc"

**Refinement (2026-10-22)**: "for the import and export they should be zipped files named for their adventure and the date, for example the-silver-egg-202610220605.zip"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Export a campaign to one file (Priority: P1)

A player wants to keep a copy of a campaign outside the toolkit folder: as a backup before a risky change, to move it to another computer, or to keep a finished campaign. They run one `dmc` command naming the campaign and get a single file holding everything the campaign needs to be resumed: its options, game state, character sheets, story files, session logs, maps and module progress.

**Why this priority**: Without export there is nothing to import. On its own it already gives the player a backup they can store anywhere, which protects against a lost or damaged folder.

**Independent Test**: Export a campaign, then compare the file's contents with the campaign folder: every campaign file is present and unchanged, and the campaign folder itself is untouched.

**Acceptance Scenarios**:

1. **Given** a campaign folder `adventures/the-silver-egg/`, **When** the player exports it, **Then** one ZIP file is written, named after the campaign and the date and time of the export, such as `the-silver-egg-202610220605.zip` for an export at 06:05 on 22 October 2026, and the command prints one short line naming the file and how many campaign files it holds.
2. **Given** the player names a destination, **When** they export, **Then** the file is written there instead of the default location.
3. **Given** a name that is not a campaign folder, **When** the player exports, **Then** nothing is written and one line says the campaign was not found and lists the campaigns that exist.
4. **Given** an export file of that name already exists, **When** the player exports, **Then** the existing file is not overwritten unless the player asks for that explicitly.
5. **Given** two exports of the same campaign in the same minute, **When** the second is written to the default location, **Then** it gets a distinct name (`the-silver-egg-202610220605-2.zip`) and the first is kept.

---

### User Story 2 - Import an exported campaign (Priority: P1)

A player has an export file, made on this computer or another, and wants to play that campaign here. They run one `dmc` command with the file. A new campaign folder appears under `adventures/` exactly as it was exported, and `/resume-campaign` picks it up with no other step.

**Why this priority**: It completes the round trip that makes export worth having: restoring a backup and moving a campaign between computers both need it.

**Independent Test**: Export a campaign, import the file under a new name, and compare the two folders file by file: they are identical. Resume the imported campaign and check the recap matches the original.

**Acceptance Scenarios**:

1. **Given** an export ZIP file and no campaign of that name, **When** the player imports it, **Then** a campaign folder of that name is created with every exported file, byte for byte, and one line names the new folder.
2. **Given** a campaign of that name already exists, **When** the player imports, **Then** nothing is changed and one line says so and how to import under another name.
3. **Given** the player gives a new name, **When** they import, **Then** the campaign is created under that name.
4. **Given** an export that carries the general player options and a computer with no `adventures/player-options.md`, **When** the player imports, **Then** that file is created from the export; **Given** the computer already has one, **Then** it is left exactly as it was, and the output says which happened.
5. **Given** a damaged, incomplete or unrecognised file, **When** the player imports it, **Then** no campaign folder is created, not even a partial one, and one line says why.
6. **Given** a file written by a newer version of the format than this `dmc` understands, **When** the player imports it, **Then** nothing is created and one line says to update `dmc`.

---

### User Story 3 - See what an export holds before importing it (Priority: P2)

Before importing a file, especially one from another computer or from someone else, the player wants to know what it is: which campaign, when it was exported, by which `dmc` version, how many files, and whether it depends on a published module that is not on this computer.

**Why this priority**: It makes import safe to try and explains a refused import, but the round trip works without it.

**Independent Test**: Run the inspect command on an export file and check its short summary against the campaign that was exported; check that nothing is written anywhere.

**Acceptance Scenarios**:

1. **Given** an export file, **When** the player inspects it, **Then** a few lines show the campaign name, export date, `dmc` version, file count, and the module it needs if any, and nothing is written.
2. **Given** an export of a module campaign whose module is not in `modules/` here, **When** the player inspects or imports it, **Then** they are told the module is missing and must be ingested from their own copy before play.

---

### Edge Cases

- A campaign folder holding files the toolkit did not write (the player's own notes, an `import/` folder of an earlier chat export): they are exported and imported like any other file in the folder.
- A very large campaign (long session logs, an `import/` folder of several hundred KB): export and import still produce one file and one line of output, however many files there are.
- An export path inside `adventures/` itself, or inside the campaign being exported: refused, so an export never ends up inside the campaign or among the campaigns.
- A campaign or file name with spaces or characters that are not ASCII: kept exactly on import, on every supported system.
- An import that fails part way (disk full, permission denied): the partial folder is removed and the one-line error says what failed; an existing campaign is never touched.
- An import file that tries to place a file outside the new campaign folder (for example through `..` in a path): refused whole.
- The campaign being played is exported mid-session: the export holds what is on disk at that moment; the command says nothing about unsaved play, since saving as you go keeps the files current.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `dmc` MUST provide a command that exports one named campaign to a single ZIP file, which any standard ZIP tool can open.
- **FR-002**: The export MUST hold every file in the campaign folder, unchanged, with its path relative to the folder, so the campaign can be resumed from the file alone.
- **FR-003**: The export MUST record the campaign's folder name, the export date and time, the `dmc` version, a format version, the list of files, and a checksum for each file.
- **FR-004**: The export MUST NOT include anything outside the campaign folder: no ingested module content, no rules reference, no `feedback/`, and nothing from other campaigns. It MUST record the name of the module a module campaign uses, so import can say whether it is present.
- **FR-004a**: The export MUST also hold the general `adventures/player-options.md`, when it exists, apart from the campaign's own files. Import MUST write it only when `adventures/player-options.md` does not already exist, MUST never overwrite or merge into an existing one, and MUST say in its output which of the two happened.
- **FR-005**: Export MUST NOT change any file in `adventures/`.
- **FR-005a**: An import that fails MUST NOT write the general player options either (FR-009 covers both).
- **FR-006**: By default the export file MUST be written to an `exports/` folder at the toolkit root, beside `adventures/` and ignored by git like it, named `<campaign>-<YYYYMMDDHHmm>.zip` from the local date and time of the export (for example `the-silver-egg-202610220605.zip`); when that name is taken, `-2`, `-3` and so on are added before `.zip`. The player MAY name another destination: a folder, in which the default name is used, or a file path ending in `.zip`. An existing file MUST NOT be overwritten without an explicit request, and a default name is never overwritten.
- **FR-007**: `dmc` MUST provide a command that imports an export ZIP file as a new campaign folder under `adventures/`, by default under the folder name recorded in the file, or under a name the player gives. The campaign name MUST come from what the file records, never from the file's name, so an export the player has renamed still imports.
- **FR-008**: Import MUST NOT overwrite, merge into or otherwise change an existing campaign folder. If the target name exists, it MUST refuse.
- **FR-009**: Import MUST be all or nothing: it MUST check the format version, the file list and every checksum, and refuse any path that would land outside the new folder, before the campaign appears under its name; on any failure no folder of that name remains.
- **FR-010**: Import MUST NOT change `adventures/.active-campaign`; its output MUST say how to play the imported campaign.
- **FR-011**: When the imported campaign uses a module that is not in `modules/`, import MUST still succeed and MUST say in its output that the module must be ingested before play.
- **FR-012**: `dmc` MUST provide a way to show an export file's summary (FR-003's details, file count, module needed) without writing anything.
- **FR-013**: Every failure of these commands MUST be one line, `dmc <command>: <message>`, and successful output MUST be at most a few short lines, under the toolkit's existing output rules.
- **FR-014**: An export made on any supported system MUST import on every other supported system with identical file contents.
- **FR-015**: The commands MUST be documented in the `dmc` reference, the user guide and the campaign files document, and `/new-campaign` MUST keep its separate route for a campaign played elsewhere as a chat export or transcript, which this feature does not replace.

### Key Entities

- **Campaign**: one folder under `adventures/`, identified by its folder name, holding the files listed in the campaign files document.
- **Export file**: a single portable ZIP file, named `<campaign>-<YYYYMMDDHHmm>.zip` by default, holding a campaign's files and a manifest. It contains spoilers (`dm-notes.md`, NPC secrets, module progress) exactly as the campaign does.
- **Manifest**: the part of the export that describes it: format version, campaign name, export time, `dmc` version, module needed, and each file's relative path and checksum.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A player can back up a campaign, or restore it, with one command and no other step.
- **SC-002**: A campaign exported and re-imported is identical to the original in 100% of files, on every supported system and across systems.
- **SC-003**: In 100% of failed imports (damaged file, name taken, newer format, unsafe path, disk error), no campaign folder is created or changed.
- **SC-004**: A resumed imported campaign gives the same recap and present moment as the original did.
- **SC-005**: Each command prints no more than 5 lines on success and exactly 1 line on failure.
- **SC-006**: Exporting or importing the largest existing campaign takes under 5 seconds.

## Assumptions

- The commands are subcommands of `dmc`, run by the player in a terminal; DMC may also run them in play when the player asks (for example "back up my campaign"), since export only reads and import only creates a new folder.
- The export is a full copy for the player's own use (backup, another computer), spoilers included. A spoiler-free copy for sharing with another player is out of scope.
- Ingested modules and the rules reference are copyrighted and stay out of the export (FR-004); the player ingests their own copy on the new computer.
- Import never merges two copies of a campaign. Restoring a backup over a campaign means importing under a new name, or the player removing the old folder themselves, consistent with DMC never deleting campaign files.
- Files that `dmc map` regenerates (`live.html`, `live-data.js`, `live.png`, `live-source.txt`) are copied like any other file; leaving them out would save little and risk a missing file.
- Supported systems are the platforms `dmc` already ships for.
- The time in the file name is the player's local time, to the minute, with no separators, because that is the time the player will recognise when looking for a backup. The exact moment of export, in UTC, is recorded inside the file (FR-003).
