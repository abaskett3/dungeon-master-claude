# Feature Specification: Compare a campaign with any export of it, `dmc campaign compare` (issue #189)

**Feature Branch**: `claude/project-thread-xbww2p` (spec 033)

**Created**: 2026-11-27

**Status**: Draft

**Input**: Issue #189, "Verify a campaign against any export: a backup against the live campaign, an import against
its file". The maintainer chose it on 2026-11-27 as work a cloud session can finish alone, with no play run and no
overlap with #290 and #291.

## Background

`dmc campaign verify` does one job for an open campaign: it proves a migration lost no content. It needs a migration
to be pending (else `no migration is pending: ...`), and on a pass it writes `.standard`, a `.verified.tsv` beside the
backup, and removes the `.migrating` marks. For a closed campaign it is a byte check against the closing backup that
writes nothing (spec 010 research R7). Nothing answers "is this backup still my campaign, and if not, which files
changed?" outside a migration, nor "is this imported campaign still the file it came from?". An imported copy of a
closed campaign has a mark that names a closing backup its own `backups/` never got (exports leave `backups/` out),
so `verify` on it needs the copy in `exports/` named by hand.

## Clarifications

### Session 2026-11-27 (the open questions of #189, decided in this spec)

- Q: One verb with modes, or a separate verb? → A: A separate verb, `compare`. `verify` keeps its one meaning for an
  open campaign (the migration proof, which writes on a pass) and the migrate skill's calls are untouched. A verb that
  never writes cannot be mistaken for one that records a standard. `verify` would also pick the migration check for any
  backup behind the kit, so it could never compare a campaign with an older backup.
- Q: Should `import` record where a campaign came from? → A: Not in this change. It would be a new file in every
  imported campaign: a change to what campaign files hold, with its own migration question. The player names the file
  with `--with`.
- Q: Bytes only, or also content units? → A: Bytes (content, line endings aside), as the closed check and the restore
  warning compare. A backup taken before an edit the player made on purpose shows that file as changed, which is the
  answer to the question asked. Content units belong to the migration proof.
- Q: What should `restore`'s plan share with it? → A: The one comparison, `CampaignFiles.Differences`, which the
  restore's warning, the closed check and the sequel already use. `restore`'s plan output is unchanged.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Is my backup still my campaign? (Priority: P1)

The player (or DMC on the player's request) runs `dmc campaign compare NAME`. It compares the campaign with its latest
backup and says that every file matches, or names each file that changed, was added or is missing since.

**Independent Test**: contract tests on a sample campaign with a backup, edited after it.

**Acceptance Scenarios**:

1. **Given** a campaign and a backup taken after the last edit, **When** `compare NAME` runs, **Then** it prints
   `NAME matches <backup>: <n> files` and exits 0.
2. **Given** a file edited, one added and one deleted after the backup, **When** `compare NAME` runs, **Then** it prints
   `changed:`, `added:` and `missing:` lines by path and fails with `3 files differ from <backup>`, exit 1.
3. **Given** either case, **When** it has run, **Then** nothing in `adventures/` or `exports/` has changed.

### User Story 2 - Any export, named (Priority: P1)

`--with FILE` names the export: a file name in the campaign's `backups/`, or any path (a file in `exports/`, a copy on
another drive). An imported campaign is compared with the file it was imported from, even under another name.

**Acceptance Scenarios**:

1. **Given** a campaign imported `--as` a new name, **When** `compare NEW --with <the file>` runs, **Then** it matches,
   with a first line `note: <file> is an export of <old name>`.
2. **Given** a damaged file, **When** compare runs, **Then** it fails with the export check's one line and writes nothing.

### User Story 3 - A closed campaign finds its closing backup (Priority: P2)

For a closed campaign the default file is the closing backup its mark names, in `backups/`, else the copy of the same
name in `exports/`. `verify` on a closed campaign looks in the same two places, so an imported copy of a closed
campaign verifies with no `--backup`.

**Acceptance Scenarios**:

1. **Given** a closed campaign, **When** `compare NAME` runs, **Then** it matches the closing backup.
2. **Given** an import of the closing backup's copy in `exports/`, **When** `verify COPY` or `compare COPY` runs with no
   file named, **Then** each checks against the file in `exports/` and passes.

### Edge Cases

- No backup at all: `NAME has no backup: name an export with --with FILE`, exit 1.
- A closed campaign whose mark cannot be read: `adventures/NAME/.closed is unreadable: name the closing backup with --with`.
- A file of another campaign: compared anyway, with the `note:` line; the differences are the answer.
- More than 40 differences: the first 40, then `<n> more`; every one with `--all`.
- Line endings alone and the map page's derived files are no change (spec 018 FR-005).
- `.standard` and `.closed` are compared, so a migration since the backup shows as `changed: .standard`.
- An interrupted restore is put back first, as by every other campaign verb, and its line printed.
- A migration in progress: compare still uses the latest backup and never touches a mark.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `dmc campaign compare NAME [--with FILE] [--all]` compares every file of the campaign but `backups/`,
  with `.standard` and `.closed`, against the export's campaign files, by path and content.
- **FR-002**: It writes nothing in `adventures/` or `exports/` (an interrupted restore's recovery aside), pass or fail.
- **FR-003**: Default file: for an open campaign, the latest counted backup; for a closed one, the closing backup the
  mark names in `backups/`, else in `exports/`.
- **FR-004**: `--with` takes a file name in `backups/` or a path, as `verify --backup` does. On a closed campaign a
  bare file name is also looked for in `exports/`, for `--with` and `verify --backup` alike.
- **FR-005**: The export is checked whole before it is compared (sizes and SHA-256); it need not be of the same name;
  when it is another's, the first line is `note: <file> is an export of <campaign>`.
- **FR-006**: Output: `NAME matches <file>: <n> files` (exit 0), or up to 40 difference lines, then `<n> more`, and
  the failure `<n> files differ from <file>` (exit 1). `--all` prints every line.
- **FR-007**: `verify` on a closed campaign finds the closing backup as FR-003 says. Its migration proof is unchanged.
- **FR-008**: The comparison is `CampaignFiles.Differences`, shared with verify, restore and sequel.
- **FR-009**: The help text, `.docs/dmc-reference.md`, `.docs/user-guide.md`, `.docs/campaign-files.md`,
  `.docs/architecture.md`, `.docs/roadmap.md` and `.docs/decisions.md` describe it.

### Key Entities

- **Export**: a ZIP that `dmc campaign export`, `backup` or `close` wrote, with a manifest of sizes and checksums.

## Success Criteria *(mandatory)*

- **SC-001**: Each acceptance scenario is a passing test, in-process or on the built program.
- **SC-002**: A snapshot of `adventures/` and `exports/` before and after every compare test is equal.
- **SC-003**: No kit file changes: play does not call `compare`, so the change needs no slip test, and
  `kit/dm-rules.md` gains 0 bytes.

## Assumptions

- Play does not call `compare`; the migrate skill keeps calling `verify` as it does.
- Out of scope (#189): what a migration's `verify` proves or writes; checking a module against its PDF; recording an
  import's source.
