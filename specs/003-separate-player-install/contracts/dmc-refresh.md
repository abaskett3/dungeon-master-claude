# Contract: `dmc refresh [folder | --all]` and `dmc folder`

## `dmc refresh`

Rewrites the generated files of a DMC folder from the copy inside this `dmc` (FR-013, FR-015). Research R4; states in
[data-model.md](../data-model.md#a-generated-files-states-refresh-research-r4).

- **Which folder:** the one given, or with `--all` every folder in `dmc.json` that still exists, or else the folder
  `dmc folder` resolves.
- **What it writes:** only the paths in the program's table; removes only paths in the folder's old manifest; writes the
  new manifest last. It refuses any path under `adventures/`, `feedback/`, `modules/`, `rules/` or `exports/` (a program
  error; cannot happen with a tested table).
- **Hand-edited files** go to `.dmc/replaced/<yyyyMMdd-HHmmss>/<path>` before being replaced or removed.
- **The folder must be a DMC folder** (has `.dmc/manifest.json`); `install` writes the first manifest.

### Output (stdout, at most two lines per folder)

| Case | Output |
|---|---|
| Nothing to do | nothing |
| Files rewritten | `dmc refresh: <folder>: <n> files updated for dmc X.Y.Z` |
| Hand edits kept | an extra line: `dmc refresh: <folder>: <n> edited files kept in .dmc/replaced/<stamp>/` |

### Errors (one line on stderr, `dmc refresh: <message>`, exit 1)

`cannot run inside a Claude Code session: run it in a terminal` (`CLAUDECODE` set; research R6);
`<folder> is not a DMC folder: run dmc install`; `no DMC folder: run dmc install`; `cannot write <path>: <reason>`
(the old manifest stays, so the next refresh repairs the folder).

## `dmc folder`

Prints the DMC folder `DmcFolder.Find()` resolves (research R10), one line, exit 0; or `dmc folder: no DMC folder: run
dmc install` on stderr, exit 1. Used by the developer skills `review-feedback` and `review-transcript` and by
`dmc digest` when no folder is given.
