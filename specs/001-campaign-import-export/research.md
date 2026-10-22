# Research: Campaign Export and Import

Every decision below rests on the spec (`spec.md`), the constitution (`.specify/memory/constitution.md`) and the existing code and decisions it cites. No item is left as NEEDS CLARIFICATION.

## R1. One command with three verbs: `dmc campaign export|import|show`

- **Decision:** One new `ICommand`, `campaign`, with the verbs `export`, `import` and `show`, added to `CommandTable.All`.
- **Rationale:** It matches `dmc digest list|show` and `dmc pdf text|render|all|info`: one command per subject, verbs under it. It adds one word to the "try roll, context-check, ..." list instead of three, and `dmc campaign --help` documents all three in one place.
- **Alternatives:** Top-level `dmc export` / `dmc import` (shorter, but two new top-level words and no home for `show`); a skill only (`/export-campaign`), rejected because the work is file copying that must be exact and all or nothing, which a prompt cannot guarantee and which would spend the player's context (constitution II).

## R2. The export file is a ZIP with a manifest

- **Decision:** A standard ZIP (`System.IO.Compression`, Deflate) named `<campaign>-<YYYYMMDDHHmm>.zip` (R7), holding `manifest.json` at the top, the campaign under `campaign/`, and the general player options as `player-options.md` at the top when present.
- **Rationale:** ZIP is in the base class library, so it adds no package and is Native AOT and trim safe (constitution, Build constraints). It opens on every system without `dmc`, so a player can always get at their files by hand. Entry names are stored as UTF-8 with the language-encoding flag set, so names that are not ASCII survive between systems (FR-014). Deflate shrinks Markdown well.
- **Alternatives:** tar.gz (`System.Formats.Tar`, also in the BCL, but Windows has no double-click opener for it); a single JSON or Markdown bundle (files must be escaped, and binary files such as `live.png` would need base64); a plain copied folder (several files, no integrity check, and easy to drop back inside `adventures/` where DMC would search it, which `.docs/user-guide.md` § Your files warns against).

## R3. Integrity: SHA-256 per file in the manifest

- **Decision:** `manifest.json` lists every file with its path, size in bytes and SHA-256 (lower-case hex). Import checks the size and hash of every byte it extracts against the manifest, and refuses an archive with a file not in the manifest or a manifest file not in the archive.
- **Rationale:** ZIP's CRC-32 catches accidental damage only inside one entry; it cannot tell a missing entry, an extra one, or a manifest that does not match (FR-003, FR-009). `System.Security.Cryptography.SHA256` is in the BCL and AOT safe. Checking the size while reading also bounds a decompression bomb: extraction stops at the declared size.
- **Alternatives:** CRC-32 only (as above); one hash of the whole archive (a refused import could not say which file is wrong).

## R4. The manifest is read and written with a source-generated JSON context

- **Decision:** `manifest.json` is UTF-8 JSON, written and read through a `JsonSerializerContext` source generator. `formatVersion` is an integer, starting at 1. Import refuses a higher version with "update dmc" (spec, Story 2 scenario 6) and a missing or malformed manifest as "not a dmc campaign export".
- **Rationale:** Reflection-based `System.Text.Json` is not AOT safe and warnings are errors; the source generator is the documented AOT route. An integer version is the simplest rule a later format can follow: a reader accepts every version up to its own.
- **Alternatives:** `JsonDocument` reads by hand, as `TranscriptReader` does (workable, but writing needs `Utf8JsonWriter` by hand too, and a typed record is easier to keep in step with the data model).

## R5. Import is all or nothing through a staging folder in `adventures/`

- **Decision:** Import checks the target name is free, extracts into `adventures/.dmc-import-<random>/`, verifying as it goes, then commits with renames on one volume: first the general player options, moved only if `adventures/player-options.md` does not exist, then the campaign folder, `Directory.Move` to `adventures/<name>`, which fails if the name was taken meanwhile. If the folder move fails, the options file it just moved is moved back. The staging folder is deleted on success and on failure; if that fails, a line on stderr names it.
- **Rationale:** It follows `.docs/decisions.md` § `dmc pdf` output is all or nothing: a staging folder inside the destination, so every commit step is a rename on one volume, and a destination that cannot be written fails before any work. A directory rename is atomic on Windows, Linux and macOS when the target does not exist, so a resumed session either sees the whole campaign or none of it (FR-009, SC-003). The leading dot keeps the staging folder out of the `[a-z0-9][a-z0-9-]*` campaign names that `Session` and the skills recognise.
- **Alternatives:** Extract straight to `adventures/<name>/` and delete on failure (a crash leaves a half campaign that looks real); staging in the system temp folder (may be another volume, so the move becomes a copy and is no longer atomic).

## R6. Paths are checked before anything is written

- **Decision:** Every entry path is checked at import before extraction starts: relative, `/` separated, no `..`, `.` or empty segment, no drive or root, no character or name Windows cannot store (`<>:"|?*`, control characters, a trailing dot or space, `CON`, `NUL` and the other reserved names), and no two paths equal when case is ignored. Any failure refuses the whole archive with one line naming the first bad path. Export refuses a campaign holding a symbolic link or junction, naming it.
- **Rationale:** A crafted archive must not place a file outside the new folder (spec edge case, "zip slip"). Checking the Windows rules on every system makes FR-014 true in both directions: an export made on Linux that could not be extracted on Windows is refused everywhere with the same message. A symbolic link would export a file from outside the campaign (FR-004); Spec Kit's `allow_symlinks: false` shows the same caution.
- **Alternatives:** Check per system only (an archive would import on one machine and not another); sanitise bad names (silent renames break the "byte for byte" promise).

## R7. Export writes the archive through a temporary file and never overwrites by default

- **Decision:** The archive is written as `<target>.partial-<random>` in the destination folder, then renamed. The default destination is `<toolkit root>/exports/`, created on first use, and the default name is `<campaign>-<YYYYMMDDHHmm>.zip`: the local date and time of the export to the minute, digits only, for example `the-silver-egg-202610220605.zip` for 06:05 on 22 October 2026 (spec FR-006, refinement 2026-10-22). When that name is taken (two exports in one minute) the next free of `-2`, `-3`, ... is added before `.zip`; a named `--out` file that exists is refused unless `--force` is given. A destination inside `adventures/` is refused. The name uses local time; the manifest records the exact UTC time. The name and the minute come from one clock reading, the same one the manifest's `exportedAt` is taken from, so the two never disagree about which export a file is.
- **Rationale:** The player asked for the date and time in the name. Several backups on one day then sort in order and each says when it was made, without opening it. A digits-only stamp has no `:` (which Windows cannot store) and sorts correctly as text. A half-written `.zip` must never sit under the final name (the same reasoning as R5). Numbering a taken default name means a second export in the same minute just works, while an explicit path the player chose is never replaced silently (FR-006). Refusing `adventures/` keeps exports out of the folder DMC searches. Local time is the time the player recognises when looking for a backup. Import never reads the file name: the campaign name comes from the manifest (FR-007), so a renamed export still imports.
- **Alternatives:** Date only, `<campaign>-<YYYY-MM-DD>.zip` (the first design; replaced at the player's request, since a day's backups were told apart only by `-2`, `-3`); seconds in the stamp (longer, and two exports in one minute are rare enough for `-2`); UTC in the name (sorts the same everywhere, but is not the time the player sees on the clock); ISO 8601 with separators, `2026-10-22T06-05` (readable, but not what was asked).

## R8. The module a campaign needs is read from `module-progress.md`

- **Decision:** If `module-progress.md` exists, export reads its `**Module folder**:` line (`modules/<module>/`, as `templates/module-progress.md` writes it) and records `<module>` in the manifest. If the line is missing or empty, the manifest records the campaign as a module campaign with the module unknown. Import and `show` check `modules/<module>/index.md` and say when it is missing (FR-011).
- **Rationale:** It is the one place the toolkit already records which module a campaign uses; no new file or field is needed (constitution III).
- **Alternatives:** Ask the player at export (needless when the file says it); copy the module (forbidden: copyrighted, FR-004).

## R9. Output

- **Decision:** Success prints one to four lines on stdout; every failure is one line on stderr, `dmc campaign: <message>`, through `DmcException`. Exit codes follow `.docs/dmc-reference.md`: 1 for a failure the player can fix, 2 for an argument mistake. Arguments are parsed with `Argparse`, like `digest` and `pdf`.
- **Rationale:** Constitution, Build and Output constraints; spec FR-013 and SC-005. The exact lines are in `contracts/campaign-command.md`.

## R10. How the player starts playing an imported campaign

- **Decision:** Import does not touch `adventures/.active-campaign` (FR-010) and its last line says: `to play it, start a session and ask DMC to resume <name>`.
- **Rationale:** `resume-campaign` step 1 already uses "the campaign the player named" before `.active-campaign`, and writes the choice; no prompt change is needed. Switching the active campaign behind the player's back could surprise a player in the middle of another campaign.

## R11. Tests

- **Decision:** MSTest, in `tests/Dmc.Tests/Campaign/`: library tests that call the export and import code in-process against a temporary toolkit root (round trip byte for byte, every refusal leaving the target untouched, the general options rule, the path rules of R6 on every system, module detection), plus `CampaignCases` run by the existing one-line error guard and command contract against the published `dmc`. CI runs them on Linux, Windows and macOS as it does every test.
- **Rationale:** The pattern of `DigestCases`/`DigestLibraryTests` and `MapRedraw`, whose library takes the root as a parameter so a test can aim it anywhere without a test hook in the binary (`.docs/decisions.md` § `dmc pdf` output is all or nothing: "no test hook is in the binary"). Constitution V: nothing here needs an optional tool, so nothing skips.
- **Cross-system:** A fixture archive written by the tests on one system is not available on another in one CI run, so the cross-system promise is tested through its causes: entry names written with `/` and UTF-8, the Windows name rules applied on every system, and file contents never transformed (no line-ending conversion).

## R12. Release

- **Decision:** The feature changes the binary, so players get it with the next release after merge and its pin; the roadmap row says so, like earlier binary changes.
- **Rationale:** `.docs/releases.md` and the roadmap's existing rows for binary changes.
