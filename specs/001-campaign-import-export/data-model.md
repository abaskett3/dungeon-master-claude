# Data Model: Campaign Export and Import

## Campaign (existing)

A folder `adventures/<name>/`, described in `.docs/campaign-files.md`.

- **name**: the folder name. A name given with `--as` must match `[a-z0-9][a-z0-9-]*`, the form `/new-campaign` makes and `dmc digest` recognises. A name taken from the manifest is used as it is, provided it is one path segment that passes the rules of research R6, so a hand-named folder with spaces or other letters survives a round trip.
- **files**: every file under the folder, at any depth, including files the toolkit did not write. Empty folders (for example `maps/` holding only `.gitkeep`, or a truly empty folder) are kept.
- **module**: present when `module-progress.md` exists; its name comes from the `**Module folder**:` line (research R8).

## Export file

A ZIP archive, `<name>-<YYYYMMDDHHmm>[-N].zip` by default: the local date and time of the export to the minute (`the-silver-egg-202610220605.zip`), with `-N` only when that name is taken (research R7). Import never reads the file's name; the campaign name comes from the manifest.

| Entry | Holds |
|---|---|
| `manifest.json` | The manifest, below. Always the first entry. |
| `campaign/<relative path>` | Each campaign file, unchanged. |
| `campaign/<relative path>/` | An empty folder of the campaign, as a directory entry. |
| `player-options.md` | The general `adventures/player-options.md`, unchanged, when it existed at export. |

Rules:

- Entry paths use `/`, UTF-8 names, no leading `/`, no `.` or `..` segments.
- Nothing else may be in the archive; an entry not listed in the manifest makes the archive invalid.
- Contents are stored byte for byte: no line-ending or encoding change.

## Manifest (`manifest.json`)

| Field | Type | Meaning |
|---|---|---|
| `formatVersion` | integer | 1 for this feature. A reader accepts every version up to its own. |
| `campaign` | string | The campaign folder name at export. |
| `exportedAt` | string | UTC time of export, ISO 8601 (`2026-10-21T19:04:05Z`). |
| `dmcVersion` | string | The exporting `dmc`'s version (`0.1.4`, or `0.0.0-dev`). |
| `module` | object or null | null for a campaign without `module-progress.md`; otherwise `{ "folder": "<module>" }`, with `folder` null when the progress file does not name one. |
| `playerOptions` | file entry or null | The general player options, when exported. |
| `files` | array of file entries | Every campaign file, sorted by path (ordinal). |
| `folders` | array of strings | Every empty campaign folder, relative path, sorted. |

**File entry**: `{ "path": "story/canon.md", "size": 18234, "sha256": "<64 lower-case hex>" }`. `path` is relative to `campaign/` (to the archive root for `playerOptions`).

Validation at import, all before anything is committed (FR-009):

1. `manifest.json` exists, parses, and has every field with the right type; otherwise "not a dmc campaign export".
2. `formatVersion` ≤ the reader's version; otherwise "made by a newer dmc".
3. Every path passes the rules of research R6, and no two differ only by case. The manifest declares at most 100,000 files and 1 GB in all (sizes added up, overflow counted as over); anything larger is refused as an entry that cannot be imported, so a hand-made manifest cannot promise an unbounded extraction.
4. The archive's entries and the manifest's lists match exactly, both ways.
5. While extracting, each file's byte count and SHA-256 match its entry; reading stops at the declared size.

## Import result (printed, not stored)

- the folder created and its file count;
- for the general player options, one of: written, kept the existing file, or none in the export;
- the module, when the campaign needs one that is not in `modules/` (or whose name is unknown);
- how to play it.

## State transitions of an import

```
check name free ─▶ extract to adventures/.dmc-import-<r>/ (verify) ─▶ move player-options.md if absent ─▶ move campaign/ to adventures/<name> ─▶ delete staging
        │                         │                                         │                                   │
        └── refuse: nothing made  └── refuse: staging deleted               └── (existing file: skip)           └── fails: move options back, delete staging, refuse
```

At every refusal, `adventures/<name>/` and an existing `adventures/player-options.md` are as they were before the run.
