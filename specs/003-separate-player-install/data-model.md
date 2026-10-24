# Data Model: Separate the Player Install from the Toolkit Source

**Feature**: [spec.md](spec.md) | **Research**: [research.md](research.md)

There is no database. The data is the generated files, two small JSON records, and the folders that hold them.
Command behaviour is in [contracts/](contracts/).

## Kit (source, in the repository)

`kit/` holds the source of every generated file under names Claude Code does not load (research R2). Embedded in
`dmc` at build (R3).

| Source | Generated as | Holds | From today's |
|---|---|---|---|
| `kit/dm-rules.md` | `CLAUDE.md` | The Dungeon Master rules | `CLAUDE.md` less the toolkit clause and § Git and GitHub |
| `kit/settings.json` | `.claude/settings.json` | Permissions, `skillOverrides`, `disableBundledSkills`, `disableClaudeAiConnectors`, the four play hooks | `.claude/play-settings.json` + the play parts of `.claude/settings.json` |
| `kit/skills/dmc-<name>/` | `.claude/skills/dmc-<name>/` | The ten player skills, renamed | `.claude/skills/<name>/` |
| `kit/templates/**` | `templates/**` | Campaign, sheet, log, options, module-progress and feedback templates | `templates/` |

The ten player skills: `dmc-campaign-full-save`, `dmc-character-sheet-update`, `dmc-draw-map`, `dmc-ingest-module`,
`dmc-new-campaign`, `dmc-record-feedback`, `dmc-resume-campaign`, `dmc-rewind-campaign`, `dmc-setup-rules`,
`dmc-update-game-state`.

**Validation** (toolkit tests, R15): every file under `kit/` is embedded and mapped; nothing maps under a player
folder; every skill a kit prompt names exists with its `dmc-` name; no kit prompt names `./bin/dmc`, `get-dmc`,
`play.ps1` or a repository path; the deny rules of R6 are present; campaign allow rules do not start with `/`.

## DMC folder

Chosen at install (default `~/dmc`). A folder is a DMC folder when it has `.dmc/manifest.json`.

| Path | Owner | Written by |
|---|---|---|
| `CLAUDE.md`, `.claude/settings.json`, `.claude/skills/dmc-*/`, `templates/` | `dmc` | install, refresh, update |
| `.dmc/manifest.json` | `dmc` | install, refresh, update |
| `.dmc/replaced/<yyyyMMdd-HHmmss>/...` | `dmc` (kept for the player) | refresh, when a generated file was edited by hand |
| `adventures/` (campaigns, `player-options.md`, `.active-campaign`) | player | DMC in play; install only creates the empty folder |
| `feedback/` | player | `dmc-record-feedback` in play; install only creates the empty folder |
| `modules/`, `rules/` | player | DMC in play; install only creates the empty folders |
| `exports/` | player | `dmc campaign export` |
| anything else (for example `.claude/settings.local.json`, the player's own skill) | player | never written by `dmc` |

**Rule (FR-012):** install, refresh and update write only paths in the program's table and remove only paths in the
old manifest, and refuse any path under `adventures/`, `feedback/`, `modules/`, `rules/` or `exports/`.

### `.dmc/manifest.json`

```json
{
  "dmc": "0.2.0",
  "files": [
    { "path": "CLAUDE.md", "sha256": "..." },
    { "path": ".claude/settings.json", "sha256": "..." },
    { "path": ".claude/skills/dmc-resume-campaign/SKILL.md", "sha256": "..." }
  ]
}
```

Paths use `/`, relative to the DMC folder. Written last, after every file, so an interrupted refresh leaves the old
manifest and the next refresh repairs the folder.

### A generated file's states (refresh, research R4)

```text
missing                          -> write
same as the program's copy       -> leave
same as the manifest's hash      -> replace (an older generated version)
different from both              -> copy to .dmc/replaced/, then replace, and say so
in the old manifest, not in the program:
    unchanged                    -> remove
    edited                       -> move to .dmc/replaced/
exists, no manifest entry, at install -> refuse, naming it
```

## Installed program

| Platform | Program folder | On PATH as |
|---|---|---|
| Windows x64, Arm | `%LOCALAPPDATA%\Programs\dmc\` | the folder, in the user `Path` |
| Linux x64, Arm | `~/.local/share/dmc/` | the link `~/.local/bin/dmc` |

Contents: `dmc` (or `dmc.exe`), three native libraries, `dmc.json`. During an update also `*.old` and `.update-*/`,
removed by the next `dmc update`, `install` or `play`.

### `dmc.json`

```json
{ "folders": ["C:\\Users\\me\\dmc"] }
```

Written by `dmc install`; read by `DmcFolder.Find()` (R10), `dmc play`, `dmc update` (which refreshes every listed
folder that still exists) and `dmc folder`. Never replaced by an update.

### Program states (update, research R9)

```text
installed(X) --update, newer Y--> staging(Y): download, hash check, unpack, run new --version
staging(Y) --any failure--> installed(X)               (staging removed, one-line error)
staging(Y) --checks pass--> swapping                   (old files renamed *.old, new moved in)
swapping --failure--> installed(X)                     (moved back)
swapping --done--> installed(Y) --> new dmc refresh --all
installed(X) --update, X is latest--> installed(X)     ("already the latest")
```

## Feedback entry

A Markdown file in `feedback/`, from `kit/templates/feedback.md`. Gains one front-matter key, `dmc: <version>`, the
version number alone (`dmc: 0.2.0`, from `dmc --version` without the leading `dmc `) when it was recorded (R11). Never written by install, refresh or update.

## Developer instructions

The repository's `CLAUDE.md` after the split: the toolkit-work opening and § Git and GitHub. Loaded only by a Claude
Code session in the repository; holds no Dungeon Master section (toolkit test).

## Release archive

`dmc-vX.Y.Z-<rid>.zip` (Windows) or `.tar.gz` (Linux), for `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`: the
program and three native libraries, as today (the generated files are inside the program). Published, immutable, with
`SHA256SUMS.txt`, `SHA256SUMS.txt.sig` (ECDSA P-256 over the list, research R16), `install.ps1` and `install.sh`.

## Release keys

Two ECDSA P-256 public keys in `src/Dmc.Cli/Install/ReleaseKeys.cs` (active and backup). The active private key is the
secret `DMC_RELEASE_SIGNING_KEY` of the GitHub environment `release`; the backup private key is offline with the
maintainer. Changing the keys is a code change shipped in a release signed by a key the previous release trusts.
