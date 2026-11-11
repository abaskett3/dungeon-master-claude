# Contract: `dmc recall`

```text
dmc recall [NAME] WORDS... [--folder PATH]
dmc recall --help
```

Finds the lines that best match some words across a campaign, its Predecessor chain and their modules, best first, in
about 1,500 estimated tokens. Read-only: it builds its index in memory on every call and stores nothing. Decisions:
[research.md](../research.md) R1 to R6, R10, R11.

## Arguments

| Argument | Meaning |
|---|---|
| `NAME` | A campaign folder in `adventures/`. Taken as the campaign only when it names one and at least one more argument follows; otherwise every argument is a word and the campaign is the active one (`adventures/.active-campaign`) |
| `WORDS...` | What to search for, as one argument or several |
| `--folder` | The DMC folder. Default: the one `dmc folder` prints |

## What it searches, best first

The campaign's markdown outside `import/`, `exports/`, `backups/` and dot folders; then each campaign its Predecessor
lines name, to the end of the chain; then each of those campaigns' modules (digests and reference files, never
`source/`). Within that, a line's relevance (BM25 over its words) is weighted by its file's authority: canon, then the
archives and history files, the sheets, the other story files, the session logs; a predecessor a little below the
campaign; a module below every campaign file.

## Output (exit 0)

```text
old-road: module folder modules/fen-crossing/ not found, not searched
new-road story/canon.md:88 s31.4 | The ferryman Oswin charges 3 sp a head, 1 gp for a cart (s31.4).
old-road story/canon-archive.md:186 s12.3 | Oswin owes the party a free crossing for the eel; …he keeps the tally on the hut door…
new-road story/sessions/session-031.md:57 s31.4 | Oswin taps the tally on the door and waves the cart aboard…
old-road story/dm-notes-archive.md:40 DM | Oswin is the smugglers' lookout and…
(12 more lines matched)
```

- **Notices** first, at most three, then `(+N more skipped)`: a predecessor not in `adventures/`, a module folder not
  found, a file that cannot be read.
- **A hit**: the campaign (or `modules/<name>`), the file inside it with its line, the session (`sN.M` of the scene for
  a session log, or the first citation in the line), `DM` for a line only the DM may see, then `|` and the line, whole
  up to 700 characters, otherwise a 700-character window around its matching words with `…` where it was cut.
- **The last line**, when more lines matched than fit, `(N more lines matched)`.
- **Nothing matched**: the one line `nothing matched: <words>`.
- The whole output, notices and last line included, is at most 6,000 bytes.

## Failures (one line on stderr)

| Exit | Message |
|---|---|
| 1 | `dmc recall: no active campaign: name one (dmc recall NAME WORDS)` |
| 1 | `dmc recall: no campaign <name> in adventures/` (the active campaign's folder is missing) |
| 1 | `dmc recall: <name> is not a campaign (a folder starting with a dot is a staging folder)` |
| 1 | `dmc recall: nothing to search for in "<words>": every word is a common one` |
| 1 | `dmc recall: no DMC folder: run dmc install`, or `<path> is not a DMC folder` |
| 2 | A usage mistake: no words, an unknown option |
