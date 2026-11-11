# Contract: `dmc lookup`

```text
dmc lookup SOURCE HEADING... [--folder PATH]
dmc lookup --help
```

Prints one section of a markdown file, exactly as written, found by its heading. Read-only: it never writes, moves or
deletes anything. Decisions: [research.md](../research.md) R7 to R10.

## Arguments

| Argument | Meaning |
|---|---|
| `SOURCE` | A campaign folder in `adventures/`, a module folder in `modules/`, or `rules`; optionally `/<path>` inside it to search one file (`new-road/story/canon.md`); a leading `adventures/` or `modules/` says which when a campaign and a module share a name |
| `HEADING...` | The heading's words, as one argument or several |
| `--folder` | The DMC folder. Default: the one `dmc folder` prints |

Options take unique prefixes and `--folder=PATH`, as every `dmc` command reading arguments like `argparse` does.

## What it searches

| Source | Files |
|---|---|
| A campaign | Its `*.md`, outside `import/`, `exports/`, `backups/` and dot folders |
| A module | `index.md`, `chapters/*.md` and the other top-level `*.md` (`monsters.md`, `npcs.md`, `items.md`); never `source/` |
| `rules`, markdown | Its `*.md` other than `index.md`, outside `extracted/` |
| `rules`, PDF | `rules/index.md` by lines, then the page files in `rules/extracted/text/` for page numbers only |

## Output

**One heading matches** (exit 0):

```text
modules/the-crooked-moon/monsters.md:412-431
## GOBLIN BOSS (PDF 212)
*Small Humanoid (Goblinoid), Neutral Evil*
...
```

The header is the file, relative to the DMC folder, and the section's first and last line. The section follows with its
line ends as LF and nothing else changed.

**Several match** (exit 0): one line each, at most 8, then `(+N more)`:

```text
adventures/new-road/character-sheets/npc-oswin.md:9: ## Playing them
adventures/new-road/character-sheets/npc-hedda.md:9: ## Playing them
```

**The section is too large to print** (20,000 estimated tokens or 1,600 lines; exit 0):

```text
modules/the-crooked-moon/chapters/04-the-hub.md:1-1750: section ## The Hub is 22.1k tokens, 1,750 lines: read it in parts
```

**`rules` is a PDF** (exit 0): the index lines that hold every word, at most 8,

```text
rules/index.md:17: | Combat: order, movement and position, attacks, mounted, underwater | 13–16 |
```

or, when no index line holds them and extracted pages exist, one line:

```text
rules: "fireball" is on PDF pages 131, 132 (read them in rules/SRD_CC_v5.2.1.pdf)
```

## Failures (one line on stderr)

| Exit | Message |
|---|---|
| 1 | `dmc lookup: no heading "<heading>" in <source> (nearest: <h1>; <h2>; …)` |
| 1 | `dmc lookup: <name> is both a campaign and a module: say adventures/<name> or modules/<name>` |
| 1 | `dmc lookup: no source <name>: not a campaign, a module or rules (campaigns: …; modules: …)` |
| 1 | `dmc lookup: <name> is not a source (a folder starting with a dot is a staging folder)` |
| 1 | `dmc lookup: no <path> in <source>` |
| 1 | `dmc lookup: rules: no rules reference in rules/ (set one up with /dmc-setup-rules)` |
| 1 | `dmc lookup: rules: no "<words>" in rules/index.md or its pages` |
| 1 | `dmc lookup: cannot read <path>: <reason>` |
| 1 | `dmc lookup: no DMC folder: run dmc install`, or `<path> is not a DMC folder` |
| 2 | A usage mistake: no source, no heading, an unknown option |
