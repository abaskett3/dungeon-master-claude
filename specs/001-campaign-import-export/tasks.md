---

description: "Task list for Campaign Export and Import (dmc campaign export|import|show)"
---

# Tasks: Campaign Export and Import

**Input**: Design documents from `specs/001-campaign-import-export/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/campaign-command.md, quickstart.md

**Tests**: Included. The plan and research R11 call for MSTest cases in `tests/Dmc.Tests/Campaign/`, and the constitution (V) requires CI to pass before merge. Within each story the tests come first and must fail before the implementation.

**Organization**: Tasks are grouped by user story. US1 (export) and US2 (import) are both P1; US3 (show) is P2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: US1, US2 or US3, as in spec.md
- Every path is relative to the repository root, `D:\Claude\dungeon-master-claude`.

## Conventions for every task that writes C#

These come from the repository, not from this feature; each task below assumes them.

- Namespace `Dmc.Campaign` for `src/Dmc.Cli/Campaign/`, `Dmc.Commands` for `src/Dmc.Cli/Commands/`, `Dmc.Tests.Campaign` for `tests/Dmc.Tests/Campaign/`.
- An XML doc comment on every type and member, private ones included (`CLAUDE.md` § Git and GitHub, Documentation). Warnings are errors, and Native AOT and trim analyzers are on: base class library only, no reflection, no new package (plan, Technical Context).
- Every failure is `throw new DmcException(...)` (see `src/Dmc.Cli/DmcException.cs`), which prints `dmc campaign: <message>` as one line on stderr with exit code 1, or 2 for an argument mistake. The exact messages are the table in `contracts/campaign-command.md`; copy them character for character.
- Library code takes the toolkit root as a parameter (as `MapRedraw` does), never `Toolkit.Root`, so tests aim it at a temporary folder. Only `CampaignCommand` passes `Toolkit.Root`.
- Text is never converted: no line-ending or encoding change to any campaign file. Read and write bytes.

## Phase 1: Setup

**Purpose**: The one repository-level change the feature needs before code, and a known-good baseline.

- [X] T001 Add `exports/*` to `.gitignore`, in the same style as the `adventures/*` block (a comment line saying exports hold campaign data and spoilers, then `exports/*`). No `.gitkeep`: the folder is created on first export. Then run `git status` and confirm a file placed in `exports/` does not show.
- [X] T002 Run `dotnet build` and `dotnet test` on the branch before any change, and record in the task notes which tests are skipped locally (the black-box tests need `dotnet publish src/Dmc.Cli -r <rid> -o artifacts/bin` first, see `.docs/development.md`). A failure here is a pre-existing problem to report, not to fix in this feature.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The manifest, the path rules, the argument parser, the archive reader, the empty command and the test fixtures that all three verbs use.

**CRITICAL**: No user story starts until this phase is complete.

- [X] T003 [P] Create `src/Dmc.Cli/Campaign/Manifest.cs` with the manifest records from `data-model.md` § Manifest, serialised as camelCase JSON: `formatVersion` (int), `campaign` (string), `exportedAt` (string, UTC ISO 8601 `2026-10-21T19:04:05Z`), `dmcVersion` (string), `module` (object `{ "folder": string|null }` or null), `playerOptions` (file entry or null), `files` (array of file entries, "sorted by path (ordinal)"), `folders` (array of strings, "every empty campaign folder, relative path, sorted"). A file entry is `{ "path": "story/canon.md", "size": 18234, "sha256": "<64 lower-case hex>" }`. Add `const int CurrentFormatVersion = 1`. Add a `JsonSerializerContext` subclass with `[JsonSerializable]` for the manifest type (source generator, research R4: reflection-based `System.Text.Json` is not AOT safe). Missing or wrongly typed fields must make deserialisation fail or return null so `ExportSummary` (T008) can report "not a dmc campaign export"; check how `src/Dmc.Cli/Dmc.Cli.csproj` configures `System.Text.Json` before choosing options.
- [X] T004 [P] Create `src/Dmc.Cli/Campaign/ArchivePaths.cs`, an `internal static` class applying research R6 to an entry path, returning the reason a path is refused or null. Rules, all of them on every operating system: relative only; `/` separators only (a `\` is refused); no leading `/`, no drive or root (`C:`), no empty, `.` or `..` segment; none of `<>:"|?*` or a control character (code below U+0020) in any segment; no segment ending in `.` or a space; no segment whose name before the first `.` is, ignoring case, `CON`, `PRN`, `AUX`, `NUL`, `COM1`-`COM9` or `LPT1`-`LPT9`. Add a second method that takes a whole list of paths and returns the first pair that are equal when case is ignored (ordinal ignore-case). Add a method to turn a validated entry path into a path under a given folder, which also checks the result still starts with that folder (defence in depth against "zip slip"). The reason strings are used verbatim inside the contract's `<path> holds an entry that cannot be imported: <entry> (<reason>)` line, so keep each short and lower case, e.g. `contains ..`, `reserved name CON`, `duplicate when case is ignored`.
- [X] T005 [P] Create `tests/Dmc.Tests/Campaign/ArchivePathsTests.cs` (MSTest, `[TestClass]`): one case per rule in T004 with an accepted and a refused example, including `../x`, `a/../b`, `/abs`, `C:/x`, `a\b`, `a//b`, `con.txt`, `NUL`, `Com1.md`, `aux`, `trailing.`, `trailing `, `a:b`, a control character, an accepted non-ASCII name (`Château/Écu.md`), an accepted name with spaces, and `Notes.md` against `notes.md` as the case-insensitive duplicate. Include one test that the combine method refuses to resolve outside the folder. These tests run the same on every system because the rules do not depend on the system (research R11, "Cross-system").
- [X] T006 [P] Create `tests/Dmc.Tests/Campaign/CampaignFixtures.cs`, a static helper (like `tests/Dmc.Tests/Digest/DigestFixtures.cs`) for the other Campaign tests, using only `System.IO` and `System.IO.Compression` so it does not depend on the code under test. It provides: (1) a temporary toolkit root with `adventures/`, deleted by the test afterwards; (2) `WriteCampaign(root, name)` that writes a sample campaign with nested folders, a UTF-8 file with CRLF line endings, a file with LF endings, a file with a non-ASCII name and one with spaces in its name, a binary file with bytes 0x00-0xFF (a stand-in for `maps/live.png`), an empty folder, a `maps/.gitkeep`, and a `module-progress.md` holding the line `**Module folder**: modules/lost-mine-of-phandelver/` (copy the exact shape from `templates/module-progress.md`); (3) a way to write a general `adventures/player-options.md`; (4) `BuildArchive(path, ...)`, which writes a ZIP by hand from a campaign folder with a correct `manifest.json` first, and overloads or a mutator for the **crafted** archives the refusal tests need: not a ZIP, no `manifest.json`, malformed manifest, a manifest missing a field, `formatVersion` 2, an entry `campaign/../evil.txt`, an entry not in the manifest, a manifest file not in the archive, a wrong `sha256`, a wrong `size`, an entry larger than its declared size, two paths differing only in case, and a reserved name.
- [X] T007 Create `src/Dmc.Cli/Campaign/CampaignArguments.cs`, parsing the three verbs the way `src/Dmc.Cli/Digest/DigestArguments.cs` does with `src/Dmc.Cli/Argparse.cs` (same `argparse` messages, a mistake is one line and exit code 2, never the usage text). Expose `public static readonly string[] Commands = ["export", "import", "show"]` (the name `ToolkitTests` reads, see T010) and three records: `ExportRequest(string Name, string? Out, bool Force)`, `ImportRequest(string File, string? As)`, `ShowRequest(string File)`. Grammar from the contract's Usage block: `export NAME [--out PATH] [--force]`, `import FILE [--as NAME]`, `show FILE`. No verb or an unknown verb throws exit 2 with `give export, import or show (try dmc campaign --help)`. `--as` that does not match `[a-z0-9][a-z0-9-]*` throws exit 2 with `--as must be lowercase letters, digits and hyphens, starting with a letter or digit`. A missing `NAME` or `FILE` and an unknown option use the existing `Argparse` messages unchanged.
- [X] T008 Create `src/Dmc.Cli/Campaign/ExportSummary.cs`, "what show prints, shared with import's checks" (plan). It opens an export file and returns the manifest plus the archive's entry list after the checks of `data-model.md` § Validation 1-4, in this order, all before anything is written: (a) the file cannot be opened: `cannot read <path>: <reason>` (use the exception's message without a trailing period or newline); (b) not a ZIP, `manifest.json` missing or not parseable, or a field missing or of the wrong type: `<path> is not a dmc campaign export`; (c) `formatVersion` greater than `Manifest.CurrentFormatVersion`: `<path> was made by a newer dmc (format <n>): update dmc and try again`; (d) every manifest path (files, folders, `playerOptions`) passes `ArchivePaths` and no two differ only by case (campaign files and folders compared together): `<path> holds an entry that cannot be imported: <entry> (<reason>)`; (e) the archive's entries and the manifest's lists match exactly both ways: every entry is `manifest.json`, `player-options.md` (only when `playerOptions` is set), `campaign/<file>` for a listed file, or `campaign/<folder>/` for a listed folder; otherwise `<path> is damaged: <entry> is not in the manifest` or `<path> is damaged: <entry> is missing`. It also exposes the module state: `None`, `Named(folder)` or `Unnamed`, and for `Named` whether `modules/<folder>/index.md` exists under a given toolkit root (research R8; contract show line 3 and import line 3). Never extract file contents here. Entry names are read as UTF-8 (open the `ZipArchive` with `Encoding.UTF8` entry-name encoding).
- [X] T009 Create `src/Dmc.Cli/Commands/CampaignCommand.cs` implementing `ICommand` as `DigestCommand` does: `Name` is `campaign`; `Summary` is `export or import a whole campaign as one file: dmc campaign export|import|show NAME-or-FILE [--out PATH] [--force] [--as NAME]`; a `Help` text for `dmc campaign --help` giving the three usage lines, what each verb does, the options, and a note that an export holds spoilers (all copied from the contract and spec; keep `.Replace("\r\n", "\n")` as in `DigestCommand`); `Run` calls `CampaignArguments.Parse` and prints help on `--help` (exit 0). Route each verb to its own private method (`RunExport`, `RunImport`, `RunShow`) that for now throws `DmcException("campaign <verb> is not built yet")`, so the project compiles; each user story task (T016, T022, T026) replaces one of them. Register it in `src/Dmc.Cli/CommandTable.cs` by adding `new CampaignCommand(),` after `new DigestCommand(),` and before `new PlayCommand(),`, so `dmc --help` lists it (dmc-reference line 22 shows the order).
- [X] T010 Wire the guards that every command goes through, in tests: (1) in `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` add `["campaign"] = CampaignArguments.Commands` to the `Choices` dictionary (and the `using Dmc.Campaign;`); (2) create `tests/Dmc.Tests/Campaign/CampaignCases.cs`, a static class with `ErrorCases()` and `DmcOnlyErrorCases()` returning `IEnumerable<CommandCase>` like `DigestCases`, starting with the argument errors from T007: no verb, unknown verb, `export` without `NAME`, `import` without `FILE`, `show` without `FILE`, an unknown option, `import x.zip --as Bad_Name`; (3) in `tests/Dmc.Tests/TokenCost/ErrorPathTests.cs` add `CampaignErrors` test data and `EveryErrorOfCampaignPrintsOneLine`, following `EveryErrorOfDigestPrintsOneLine` exactly (`AssertOneLine`, and also assert the working folder holds nothing new). The black-box binary resolves `Toolkit.Root` to the real repository (`Toolkit.FindRoot`), so a `CommandCase` must never get as far as writing under the real `adventures/` or `exports/`: cases here fail on arguments or on a file in the case's own temporary folder.

**Checkpoint**: `dotnet build` is clean; `dmc campaign --help` works; the argument-error cases pass.

---

## Phase 3: User Story 1 - Export a campaign to one file (Priority: P1) MVP

**Goal**: `dmc campaign export NAME [--out PATH] [--force]` writes one ZIP holding the campaign, a manifest and, when present, the general player options, and changes nothing in `adventures/`.

**Independent Test**: Export a sample campaign made by `CampaignFixtures`, open the ZIP with `System.IO.Compression` and compare it with the folder: every file present with identical bytes, every checksum right, the source folder untouched (spec US1 Independent Test).

### Tests for User Story 1

> Write these first and see them fail.

- [X] T011 [US1] Create `tests/Dmc.Tests/Campaign/CampaignExportTests.cs` with the happy-path cases, calling `CampaignExporter.Export(root, request)` in-process against a temporary root: the archive's first entry is `manifest.json`; every campaign file is under `campaign/` with bytes identical to the source (CRLF files stay CRLF, the binary file stays binary); every manifest `size` and `sha256` is right; empty folders appear in `folders` and as directory entries; `manifest.campaign`, `dmcVersion`, `exportedAt` (UTC, `...Z`) and `formatVersion` 1 are set; `module.folder` is `lost-mine-of-phandelver` for the fixture campaign, `module` is null without `module-progress.md`, and `module` is `{ "folder": null }` when the progress file has no `**Module folder**:` line; the general `adventures/player-options.md` is `player-options.md` and `playerOptions` in the manifest when it exists and absent when it does not; the source folder and `adventures/` are unchanged afterwards (compare file lists, bytes and modification times); the default destination is `<root>/exports/<name>-<yyyyMMddHHmm>.zip` (local time, for example `the-silver-egg-202610220605.zip`) created on first use, a second export in the same minute is `-2` and a third `-3` (pass a fixed `now` so the test does not straddle a minute), and an export a minute later gets the new stamp with no suffix; the stamp in the name and `exportedAt` come from the same `now` (research R7); `--out` as an existing folder uses the default file name inside it; `--out` as a path ending in `.zip` uses that path; the file count the exporter returns equals the number of campaign files; a file that is not toolkit-written (an `import/` folder of an earlier chat export) is exported like any other.
- [X] T012 [US1] Add the refusal cases to `tests/Dmc.Tests/Campaign/CampaignExportTests.cs`, each asserting the exact message from the contract and that nothing was written (no `exports/` folder created, no `.partial-*` file left): no such campaign with campaigns present (`no campaign "x" in adventures/ (there are: a, b)`, names sorted, folders starting with `.` such as `.dmc-import-*` not listed) and with none (`(there are none)`); `--out` inside `adventures/` (`will not write an export inside adventures/: choose another --out`), including a path inside the campaign being exported; `--out` file that exists without `--force` (`<path> exists: add --force to replace it, or choose another --out`) and the same with `--force` replacing it; `--force` has no effect on the default name (the next free `-N` is still used); a symbolic link inside the campaign (`<relative path> is a link: exports hold only the campaign's own files`; create it with `File.CreateSymbolicLink`, and skip the case with `Assert.Inconclusive` only where the system refuses to create links locally, but fail on CI per constitution V and `.docs/development.md` § Skip locally, fail on CI); a failure while writing (make the destination folder read-only or the target a directory) leaves no partial file.

### Implementation for User Story 1

- [X] T013 [US1] Create `src/Dmc.Cli/Campaign/CampaignExporter.cs` (public static class or sealed class with `Export(string root, ExportRequest request, DateTime? now = null)` returning a result record: archive path, campaign file count, whether the general player options were included). Part 1, lookup and destination: the campaign is the folder `<root>/adventures/<name>` (refuse a name that is not a single folder name, or does not exist, with the "no campaign" message listing the existing non-dot folders); resolve the destination per research R7 and the contract (default `<root>/exports/`, created only when about to be written; `--out` an existing folder gets the default file name inside it; `--out` ending in `.zip` is the file; refuse any destination whose full path is inside `<root>/adventures/`, compared with the system's path comparison and after `Path.GetFullPath`); the default name is `<name>-<local yyyyMMddHHmm>.zip` (for example `the-silver-egg-202610220605.zip`, from `now` converted to local time, formatted with the invariant culture), with `-2`, `-3`, ... before `.zip` until free; read the clock once (`now ?? DateTime.UtcNow`) and pass that one value on to part 3 for `exportedAt`; an explicit file that exists is refused unless `Force`.
- [X] T014 [US1] In `src/Dmc.Cli/Campaign/CampaignExporter.cs`, part 2, reading the campaign: walk the folder at any depth in ordinal order; refuse the first symbolic link or junction (any file or directory whose `FileSystemInfo.LinkTarget` is not null, which covers symbolic links and junctions on every system; `FileAttributes.ReparsePoint` was dropped because it also marks files that are not links, such as cloud placeholders; name it by its path relative to the campaign) with the contract message; record every directory with no files or directories inside as an empty folder; read **each file fully into memory once** (campaigns are a few MB at most), take its size and SHA-256 (`System.Security.Cryptography.SHA256.HashData`, lower-case hex) from those bytes, and use the same bytes for the archive, so a file changed mid-session cannot make the manifest disagree with the archive; also run every relative path through `ArchivePaths` and refuse a campaign holding a path that could not be imported (a clear one-line message naming it, e.g. `<path> cannot be exported: <reason>`), so export never writes an archive its own import would refuse; read the module per research R8: if `module-progress.md` exists, find the `**Module folder**:` line (`modules/<module>/`, exact shape from `templates/module-progress.md`) and take `<module>`, else a `{ folder: null }` module; read the general `adventures/player-options.md` bytes if the file exists.
- [X] T015 [US1] In `src/Dmc.Cli/Campaign/CampaignExporter.cs`, part 3, writing: build the manifest (`CurrentFormatVersion`, campaign name, the clock reading from part 1 (T013) in UTC formatted `yyyy-MM-ddTHH:mm:ssZ`, never a second `DateTime.UtcNow`, the version `dmc` reports for `--version`: find where `src/Dmc.Cli/Program.cs` or `CommandLine.cs` gets it and reuse it, `0.0.0-dev` for a dev build; files and folders sorted ordinally); write to `<target>.partial-<random>` in the destination folder (research R7) with `ZipArchive` in `Create` mode and UTF-8 entry names: `manifest.json` first, then `player-options.md`, then `campaign/<path>` for each file in manifest order, then `campaign/<folder>/` directory entries, each file entry written as raw bytes (Deflate is fine; never a text writer); dispose the archive, then rename the partial file to the target (`File.Move`, with overwrite only for an explicit `--force` file); on any exception delete the partial file and throw `DmcException` with `could not export: <reason>`. Never touch anything under `adventures/`.
- [X] T016 [US1] In `src/Dmc.Cli/Commands/CampaignCommand.cs` replace the `export` stub: call `CampaignExporter.Export(Toolkit.Root, request)` and print the single contract line `exported <name>: <n> files[ and the general player options] to <absolute path>` to `io` stdout, LF terminated. Run `dotnet test --filter FullyQualifiedName~CampaignExport` and make T011 and T012 pass.
- [X] T017 [US1] Add the black-box export cases to `tests/Dmc.Tests/Campaign/CampaignCases.cs` (`ErrorCases()`): `export no-such-campaign-zz9` (a name that cannot exist in the real `adventures/`; the message lists whatever campaigns exist, which is one line either way). Keep every other export refusal in the in-process tests of T012, because the published binary works on the real repository root.

**Checkpoint**: `dmc campaign export` works end to end; US1 is a usable backup on its own. The archive can be opened by any ZIP tool.

---

## Phase 4: User Story 2 - Import an exported campaign (Priority: P1)

**Goal**: `dmc campaign import FILE [--as NAME]` verifies the whole archive, then creates `adventures/<name>` with every file byte for byte, all or nothing, never touching an existing campaign, an existing general player options file or `.active-campaign`.

**Independent Test**: Build an archive with `CampaignFixtures` (not with the exporter, so this story does not depend on US1), import it under a new name and compare the two folders file by file; import each crafted bad archive and check that no folder and no staging folder remains (spec US2 Independent Test).

### Tests for User Story 2

- [X] T018 [P] [US2] Create `tests/Dmc.Tests/Campaign/CampaignRoundTripTests.cs`, calling `CampaignImporter.Import(root, request)` in-process: import of a fixture archive under its recorded name and under `--as`, then compare the folders file by file with `Assert` on bytes (CRLF kept, binary kept, non-ASCII and spaced names kept), including empty folders; the result reports the folder created and the file count; `adventures/.active-campaign` is not created or changed (write one first with other content, and also run once with none); the general player options: written when `adventures/player-options.md` does not exist (result says written), left byte for byte as it was when it does (result says kept), and nothing about it in the result when the export has none; the module: result says not present when `modules/<folder>/index.md` does not exist and says nothing when it does, and the unnamed-module variant (`this is a module campaign, but its module-progress.md names no module folder`); a name taken from the manifest that is a hand-named folder with spaces and capital letters is accepted (data-model, Campaign.name); and, once US1 is merged in this branch, one full round trip `Export` then `Import --as other` giving identical bytes, plus the same on a fixture whose export came from `CampaignExporter` (SC-002).
- [X] T019 [P] [US2] Create `tests/Dmc.Tests/Campaign/CampaignImportRefusalTests.cs`, one test per row of the contract's failure table for import, each asserting the exact message and, after it: `adventures/<target>` does not exist, no `adventures/.dmc-import-*` folder remains, an existing `adventures/player-options.md` is unchanged, and **no `adventures/player-options.md` was created** when the archive had one but the import failed (FR-005a). Cases: file missing and unreadable; not a ZIP; no `manifest.json`; malformed manifest; manifest missing a field; `formatVersion` 2; name taken (create the folder first, with a file inside, and check the folder is unchanged afterwards; also a pre-existing **empty** folder of that name, because `Directory.Move` onto an empty folder succeeds on Linux and macOS in the OS call: the import must still refuse); `--as` taken; unsafe path `campaign/../evil.txt` (and assert `evil.txt` was not written next to `adventures/` or anywhere); a reserved name; two paths equal when case is ignored; an entry not in the manifest; a manifest file not in the archive; a wrong `sha256`; a wrong `size`; an entry whose real content is larger than its declared size (extraction stops at the declared size and refuses, research R3); a write failure in the middle (make the staging extraction fail, for example by pre-creating a directory where a file must go) which gives `could not import: <reason>` and removes the staging folder; and a commit failure after `player-options.md` was moved (simulate by making the target name appear between the check and the move) which moves the options file back and removes `adventures/player-options.md` that this run created.

### Implementation for User Story 2

- [X] T020 [US2] Create `src/Dmc.Cli/Campaign/CampaignImporter.cs` with `Import(string root, ImportRequest request)` returning a result record (folder name created, file count, general-options outcome `Written | Kept | None`, module state for the output). Part 1, check and extract: `ExportSummary` (T008) first, so nothing is created for an archive that fails checks 1-4; choose the name (`--as` or the manifest `campaign`; refuse a manifest name that is not one safe path segment per `ArchivePaths`); refuse when `<root>/adventures/<name>` exists, file or folder, with `adventures/<name> already exists: import under another name with --as NAME`; create `<root>/adventures/.dmc-import-<random>/` and extract the campaign entries into `<staging>/campaign/` and `player-options.md` into `<staging>/player-options.md`, resolving each destination through `ArchivePaths` (T004); for each file, copy the entry stream while counting bytes and hashing, stop and refuse when the count passes the declared size, and refuse on a final size or SHA-256 that differs, naming the entry (`<path> is damaged: <entry> <what differs>`); create each manifest folder; set every extracted file's last-write time to the manifest's `exportedAt` (so a file does not look newer than a map's picture to `dmc redraw`), and set no attributes. On any exception delete the staging folder (`Directory.Delete(recursive: true)`) and rethrow as `could not import: <reason>` unless it is already a `DmcException`; if deleting the staging folder fails, write the extra stderr line naming it as `dmc pdf` does (read how `src/Dmc.Cli/Pdf/` reports its leftover staging folder and use the same wording).
- [X] T021 [US2] In `src/Dmc.Cli/Campaign/CampaignImporter.cs`, part 2, commit and roll back (research R5; data-model § State transitions): if the export has `player-options.md` and `<root>/adventures/player-options.md` does not exist, `File.Move` it there (no overwrite) and remember that it was moved; if the file appears meanwhile, treat the result as Kept; then `Directory.Move` `<staging>/campaign` to `<root>/adventures/<name>`; check `Directory.Exists(target)` immediately before the move as well, since the move onto an existing empty folder can succeed on Linux and macOS. If the folder move fails, move the options file back to the staging folder (or delete it if that fails), delete the staging folder and throw `could not import: <reason>`. On success delete the now-empty staging folder (a failure to delete it is the extra stderr line from T020, not an error). Staging is inside `adventures/` so every step is a rename on one volume. Never write `adventures/.active-campaign` (FR-010).
- [X] T022 [US2] In `src/Dmc.Cli/Commands/CampaignCommand.cs` replace the `import` stub: call `CampaignImporter.Import(Toolkit.Root, request)` and print the contract's lines, in order, LF terminated: `imported <name>: <n> files to <absolute path to adventures/<name>>`; then, only when the export held a general options file, `general player options: written to adventures/player-options.md` or `general player options: kept your existing adventures/player-options.md`; then, only when the module is missing or unnamed, `module <folder> is not in modules/: ingest it from your own copy before play` or `this is a module campaign, but its module-progress.md names no module folder`; then `to play it, start a session and ask DMC to resume <name>`. At most four lines (SC-005). Run `dotnet test --filter FullyQualifiedName~Campaign` and make T018 and T019 pass.
- [X] T023 [US2] Add black-box import cases to `tests/Dmc.Tests/Campaign/CampaignCases.cs` that fail **before** the importer creates any folder, so they never touch the real `adventures/`: a missing file; a text file that is not a ZIP; a ZIP with no manifest; `formatVersion` 2; a manifest naming `../x` (all as seeds written into the case's temporary folder with `System.IO.Compression`, not with `CampaignFixtures` if that makes the dependency awkward). Cases that reach the name check or extraction (name taken, damaged contents) stay in the in-process tests of T019.

**Checkpoint**: Import works from any archive that passes the checks; US1 plus US2 give the full backup, restore and move-computer round trip (SC-001, SC-002, SC-003).

---

## Phase 5: User Story 3 - See what an export holds before importing it (Priority: P2)

**Goal**: `dmc campaign show FILE` prints up to three lines about an archive and writes nothing.

**Independent Test**: Run `show` on a fixture archive and compare the lines with the contract; check that the file system is unchanged afterwards (spec US3 Independent Test).

### Tests for User Story 3

- [X] T024 [P] [US3] Create `tests/Dmc.Tests/Campaign/CampaignShowTests.cs`: against a fixture archive in a temporary root, assert the exact lines of the contract's show block: `<name>: exported <yyyy-MM-dd HH:mm> UTC by dmc <version>, format <n>`; `<n> files, <size>, plus the general player options` (the clause absent without a general options file; size is the total of the manifest's file sizes in KB or MB with one decimal, `1.4 MB`; pick and document the cut-over: below 1024 bytes print `<n> bytes`, below 1 MiB print KB, otherwise MB); line 3 absent without a module, `module: <folder> (not in modules/)` when `modules/<folder>/index.md` does not exist, `module: <folder> (in modules/)` when it does, and `module: unknown (module-progress.md names no module folder)` for the unnamed case. Assert that `show` creates and changes nothing anywhere (snapshot the root's file list with sizes and times before and after), that a damaged-content archive (wrong hash inside) still shows because `show` only reads the manifest, but a missing, non-ZIP or newer-format archive gives the same one-line messages as import (T008), and that the output is at most three lines.

### Implementation for User Story 3

- [X] T025 [US3] Add the formatting to `src/Dmc.Cli/Campaign/ExportSummary.cs`: a method returning the show lines from the manifest and the toolkit root exactly as T024 specifies (UTC time from `exportedAt`, culture-invariant number formatting so `1.4 MB` never prints as `1,4 MB`). The module-missing wording for import (T022) must come from the same module-state values so the two commands cannot disagree.
- [X] T026 [US3] In `src/Dmc.Cli/Commands/CampaignCommand.cs` replace the `show` stub: call `ExportSummary` on the file with `Toolkit.Root` and print its lines. Run `dotnet test --filter FullyQualifiedName~CampaignShow` and make T024 pass.
- [X] T027 [US3] Add black-box `show` cases to `tests/Dmc.Tests/Campaign/CampaignCases.cs`: a missing file, a file that is not a ZIP, a newer-format archive; and one success check in `tests/Dmc.Tests/Campaign/CampaignCommandContract.cs` (create it here, following `tests/Dmc.Tests/Digest/DigestCommandContract.cs`: an abstract class run once in-process and once against the published binary) that `show` on a fixture archive in the case's own temporary folder prints the contract lines and exits 0 under both runners, and that `export`/`import` `--help` text lists all three verbs and the options. Success checks of `export` and `import` stay in-process (they would write to the real repository when run through the binary).

**Checkpoint**: All three stories work independently; `dotnet test` passes with the black-box binary published.

---

## Phase 6: Polish and Cross-Cutting Concerns

**Purpose**: Documentation that goes with the change (constitution IV; plan, Constitution Check IV), and the checks the spec's success criteria name.

- [X] T028 [P] Update `.docs/dmc-reference.md`: add `campaign` to the command list at line 22 using the same text as `CampaignCommand.Summary`; add a `dmc campaign` section modelled on the `dmc digest` section (usage block, one paragraph per verb, the output lines, the failure table, the guarantees, all taken from `contracts/campaign-command.md`); and add `campaign` to the no-command message at line 38 (`dmc: no command given (try roll, context-check, map, pdf, digest, play, --version)`), which `CommandLine.cs` builds from the command table, so the table order decides where it goes. `CLAUDE.md` is not changed (plan, Constitution Check II).
- [X] T029 [P] Update `.docs/user-guide.md` § Your files: how to back up a campaign, restore one under a new name, and move to another computer, with the three commands; that the export holds spoilers; that a module is not exported and must be ingested again; and that `/new-campaign` remains the route for a chat export or transcript (FR-015). Point to `dmc-reference.md` for details instead of repeating them.
- [X] T030 [P] Update `.docs/campaign-files.md`: add `exports/` to the layout block beside `.transcript-review/` (line 14 shows the style) and say what it holds and that it is ignored by git.
- [X] T031 [P] Update `.docs/decisions.md` with the decisions and reasons from research R2 (ZIP with a manifest), R3 (SHA-256 per file), R5 (staging folder in `adventures/` and rename commit) and R6 (path rules applied on every system), as separate entries in the file's existing format.
- [X] T032 [P] Update `.docs/roadmap.md`: a status row for campaign export and import in the format of the existing rows ("Changes the binary; ships in the next release", with the issue number if one exists) and, once the PR is open, its PR number under the changes list.
- [X] T033 [P] Update `.docs/architecture.md`: add `campaign` to the command list on line 15 and to the diagram on line 34.
- [X] T034 [P] Update `README.md`: add `campaign` to the `bin/` line in the layout (line 169) and an `exports/` line beside it, and, if the README has a "your files" or backup passage, point it at the user guide.
- [X] T035 [P] Bring `specs/001-campaign-import-export/plan.md` § Project Structure into line with what was built: `tests/Dmc.Tests/Campaign/` also holds `CampaignExportTests.cs`, `CampaignShowTests.cs` and `CampaignFixtures.cs`. Change nothing else in it.
- [X] T036 Run `dotnet build` (zero warnings: warnings are errors), `dotnet publish src/Dmc.Cli -r <rid> -o artifacts/bin` for the local runtime identifier (confirms Native AOT and the trim analyzers accept the JSON source generator and `System.IO.Compression`), then `dotnet test` with the binary published so no black-box test is skipped. Fix any `ToolkitTests` failure about commands, paths or documentation that these changes caused. CI will also run Linux, Windows and macOS (constitution V).
- [X] T037 Walk through `specs/001-campaign-import-export/quickstart.md` by hand with the published binary on a copy of a real campaign: steps 1-7, including the minute-stamped name, a second export in the same minute giving `-2`, the import of a renamed copy, `git status` showing nothing new, the folder comparison, the damaged-byte refusal and a resume of the imported copy. Delete `adventures/silver-egg-copy`-style test folders and test exports yourself afterwards (DMC never deletes campaign files).
- [X] T038 Check the measurable criteria: SC-005 (each command prints at most 5 lines on success and exactly 1 on failure, from the quickstart run), SC-006 (export and import of the largest existing campaign each under 5 seconds, timed), and SC-002 on the real campaign (every file's bytes identical, `diff -r` or the PowerShell `Compare-Object` from the quickstart).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: none. T001 and T002 can run at once.
- **Foundational (Phase 2)**: needs Phase 1. T003, T004, T005, T006 and T007 can run in parallel with each other; T008 needs T003 and T004; T009 needs T007 (and each verb in it is finished by T016, T022 and T026); T010 needs T007 and T009, so it comes last.
- **User Stories (Phase 3+)**: all need Phase 2. US1, US2 and US3 do not depend on each other: US2 and US3 test against archives built by `CampaignFixtures`, not by the exporter. They touch the same two shared files (`CampaignCommand.cs`, `CampaignCases.cs`), so the tasks that edit those (T016/T022/T026, T017/T023/T027) are done one after another.
- **Polish (Phase 6)**: needs the stories you ship. Documentation tasks T028-T035 can run in parallel; T036-T038 run last, in order.

### Within Each Story

- Tests first, and they must fail before the implementation.
- `CampaignExporter` and `CampaignImporter` parts are in one file each, so they are sequential: T013 then T014 then T015, and T020 then T021.
- The verb in `CampaignCommand` comes after its library code.

### User Story Dependencies

- **US1 (P1)**: starts after Phase 2.
- **US2 (P1)**: starts after Phase 2. The round-trip test in T018 that uses the exporter needs US1's `CampaignExporter` (T015).
- **US3 (P2)**: starts after Phase 2; shares `ExportSummary` with US2 (T008 in the foundation).

### Parallel Opportunities

- Phase 2: T003, T004, T005, T006, T007 together; then T008 and T009 as their inputs finish; T010 last.
- After Phase 2, one person per story: T011/T012 (US1), T018/T019 (US2) and T024 (US3) touch different files.
- Phase 6: T028-T035 together.

### Parallel Example: Foundation

```text
Task: "Create src/Dmc.Cli/Campaign/Manifest.cs"            (T003)
Task: "Create src/Dmc.Cli/Campaign/ArchivePaths.cs"        (T004)
Task: "Create tests/Dmc.Tests/Campaign/ArchivePathsTests.cs" (T005)
Task: "Create tests/Dmc.Tests/Campaign/CampaignFixtures.cs"  (T006)
```

### Parallel Example: Tests of the three stories

```text
Task: "tests/Dmc.Tests/Campaign/CampaignExportTests.cs"        (T011, T012)
Task: "tests/Dmc.Tests/Campaign/CampaignRoundTripTests.cs"     (T018)
Task: "tests/Dmc.Tests/Campaign/CampaignImportRefusalTests.cs" (T019)
Task: "tests/Dmc.Tests/Campaign/CampaignShowTests.cs"          (T024)
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 and Phase 2.
2. Phase 3 (export). **Stop and validate**: open the ZIP with a normal ZIP tool and compare it with the folder. That is already a backup the player can use.

### Incremental Delivery

1. Setup and Foundational: the command exists and answers `--help`.
2. Add US1 (export), test it, then US2 (import), which completes the round trip that gives the feature its value (SC-001, SC-002). Together they are the release-worthy scope.
3. Add US3 (show), which makes import safe to try.
4. Polish: documentation and the AOT and cross-platform checks, then one `/code-review` of the finished PR and the PR description checked with `gh pr view <n> --json body` (`CLAUDE.md` § Git and GitHub).

### Notes

- [P] tasks are different files with no unfinished dependency.
- The published binary resolves the toolkit root to the real repository, so tests that create or import campaigns run in-process against a temporary root; the black-box cases only exercise argument errors and failures that happen before anything is written.
- Commit after each task or logical group, on branch `001-campaign-import-export`, never on `main`.
- Do not touch `CLAUDE.md`, any skill, or any file under `adventures/`, `modules/` or `rules/`.

### Implementation notes (2026-10-22)

- **T002 baseline** (before any change, `dotnet build` then `dotnet test --no-build`): 0 warnings; 392 passed, 461 skipped, 0 failed. The 461 skips are the black-box tests, which need `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin` first. With the binary published (T036/T038 run): 1108 passed, 3 skipped, 0 failed. The 3 skips are local only: two symbolic-link cases (this Windows account may not create symbolic links; they fail on CI per constitution V) and one case that needs a file name with a backslash (Linux and macOS only). The junction case runs here.
- **T036 is left open on one point.** `dotnet build` is clean (0 warnings, with the AOT, trim and single-file analyzers on) and the full suite passes with the published binary, but `dotnet publish -p:PublishAot=true` stops on this machine with "Platform linker not found" (the Visual Studio C++ workload is not installed), so the Native AOT compile itself was not run here. CI (Linux, Windows, macOS) is where it runs.
- **T037 step 6** (asking DMC to resume the imported copy) was not run in a play session. The copy was compared with `diff -r` against the original and was byte-identical (78 files), so a resume reads the same files. Steps 1-5 and 7 were run by hand on the real campaign: export, a second export in the same minute (`-2`), `show`, import of a renamed copy under `--as silver-egg-copy`, a repeat import refused, a flipped byte and a truncated file refused with no folder left, `git status` showing nothing from `exports/`; the copy and the test exports were then deleted.
- **T038:** each command printed at most 5 lines on success (export 1, import 3, show 3) and exactly 1 on failure; export took 0.13 s and import 0.14 s for the largest campaign here (78 files, 1.3 MB); `diff -r` found no difference.
- **Not in the task list, added with the code:** `--out` that is neither an existing folder nor a `.zip` name is refused (`<path> is not an existing folder or a file name ending in .zip: ...`), and a read or write failure on export is `could not export: <reason>`; both are now rows of the contract's failure table.

## Phase 7: Convergence

- [X] T039 (run last, after T040 and T041 are done and committed, since T040 changes the resume wording it tests) Resume an imported copy in a play session and compare its recap and present moment with the original's: export a campaign, import it under a new name, ask DMC to resume it, and record the result in the Implementation notes; T037 was ticked without this step per SC-004 and US2 Independent Test (partial)
- [X] T040 Decide how a leftover `.dmc-import-*` staging folder (left when the import process is killed or cleanup fails) is kept out of the session-start list of campaigns in `CLAUDE.md` § Starting a session and the `resume-campaign` skill: either skip folders starting with a dot there in one clause, or record in `.docs/decisions.md` that the limit is accepted; the edge case says a failed import's partial folder is removed (partial)
- [X] T041 Bring the artifacts in line with the shipped, documented behaviour: in `data-model.md` § Validation add the 100,000-file and 1 GB manifest caps; in `contracts/campaign-command.md` add the link refusal for the campaign folder and the general `player-options.md` and the uniform file times; amend the T014 note (links found with `LinkTarget`, not `ReparsePoint`) and the T020 note ("do not set timestamps"); correct quickstart step 3 ("four-line report" is three lines when the module is installed); each is recorded in `.docs/decisions.md` already per tasks T014, T020, data-model, contract, quickstart (unrequested)

- **T040 (issue #122):** option 1 chosen by the owner: `CLAUDE.md` § Starting a session and the `resume-campaign` skill now list only the folders of `adventures/` whose names do not start with a dot, so a leftover `.dmc-import-*` folder is never offered as a campaign; `.docs/decisions.md` (the import entry's Limits) says so. This is the one place this feature touches `CLAUDE.md` and a skill, by the owner's decision; `dmc pdf`'s `.dmc-partial-` folder lives in its output folder, not in `adventures/`, and is not part of this.
- **T039 (issue #121):** run by the owner in a play session after T040 and T041 were committed: an imported copy of `the-silver-egg` (`artifacts/bin/dmc campaign export`, then `import --as silver-egg-copy`) was resumed by DMC and the check passed (SC-004, User Story 2 Independent Test). The owner reported the result as "passed"; no further detail was recorded.
- **T036 (closed):** the Native AOT compile that could not run locally ran in the `dmc-v0.1.4` release workflow (run 39892982744), which publishes with `-p:PublishAot=true` on win-x64, win-arm64, linux-x64, linux-arm64 and osx-arm64; every build job passed, and the pin PR #155 was merged. CI on PR #120 passed the tests on Linux, Windows and macOS, including the symbolic-link cases that are skipped locally.

## Phase 8: Convergence

- [X] T042 In `plan.md` § Constitution Check, row II, replace "`CLAUDE.md` is not changed" with the shipped fact: T040 (owner's decision, issue #122) added one clause to `CLAUDE.md` § Starting a session and to the `resume-campaign` skill so a leftover `.dmc-import-*` folder is not listed as a campaign, which stays lean (82 bytes in a file loaded every session); `.docs/decisions.md` records it per Constitution II and the plan's Constitution Check (contradicts)
- **T042:** `plan.md` Constitution Check row II now says what shipped: `CLAUDE.md` gained one clause in T040 (the file grew by 82 bytes, from 27,871 to 27,953, in commit 3b056a2).
