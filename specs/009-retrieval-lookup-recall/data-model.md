# Data Model: Look Up a Section and Recall a Fact

Phase 1 of [plan.md](plan.md). Nothing here is stored: every value is built from the markdown on each call and
dropped when the command ends. The files are as [.docs/campaign-files.md](../../.docs/campaign-files.md) describes
them; this feature changes none of them.

## Source (lookup)

| Field | Rule |
|---|---|
| Kind | `campaign`, `module` or `rules` (R8) |
| Name | A folder in `adventures/` or `modules/`, or `rules`; a dot-prefixed folder is refused |
| File | Optional: one path inside it (`<name>/<path>`), which limits the search to that file |
| Files | Campaign: its `*.md` outside `import/`, `exports/`, `backups/` and dot folders. Module: `index.md`, `chapters/*.md`, other top-level `*.md`, never `source/`. Rules: its `*.md` other than `index.md`; and, when `rules/` holds a `*.pdf` or `extracted/text/` and no heading matches, `index.md` and `extracted/text/` (PDF, R9) |

A name that is both a campaign and a module is refused (FR-024).

## Heading and section

| Field | Rule |
|---|---|
| Level | 1 to 6, from the number of `#` (R7) |
| Text | The heading after the `#`s, as written |
| Key | The normalised text used for matching (R7) |
| File, first line | Where the heading is (1-based) |
| Last line | The line before the next heading of the same or a higher level, or the file's last line |
| Size | Estimated tokens (bytes ÷ 4) and lines; at the near-cap mark the section is not printed (FR-007) |

Headings inside fenced code blocks are not headings.

## Recall scope

An ordered list of **places**, each a campaign or a module with a **distance** (0 for the campaign, 1 for its
predecessor, and so on) and a **kind**. Built by R6:

1. the campaign (distance 0);
2. each predecessor in turn (distance 1, 2, …), stopping at `none`, a missing folder (a notice) or a folder already
   visited;
3. each place's module, if it has one, at that place's distance, once.

## Line unit

| Field | Rule |
|---|---|
| Place | The campaign or module it came from |
| File | Path relative to the place's folder |
| Line | 1-based line number in the file |
| Text | The line, trimmed of surrounding spaces |
| Words | Its stems (R2), with their counts |
| Tier | 1 to 5 for campaign files, or module (R1) |
| Weight | Tier weight × 0.95 per step of distance; module 0.80 × 0.95 per step |
| Scene | For a session log, the `sN.M` of the `### Scene M:` heading above it, N from `session-NNN.md` |
| Citation | The first `sN.M` (or `sN`) in the text, for other files |
| DM-only | True for `dm-notes.md`, `dm-notes-archive.md`, `module-progress.md`, every module line, and lines under a heading containing "Secret" in an NPC sheet |

HTML comment text is removed first; lines then under 25 characters, headings, table separators and fenced code are not units (R3).

## Hit

A unit with its **score** (BM25 × weight, R1) and its **shown text** (whole, or a 700-character window with `…`, R4),
printed as one line:

```text
<place> <file>:<line> [<sN.M>] [DM] | <shown text>
```

Hits are ordered by score (two decimals), then tier, distance, file and line. The output holds as many as fit in 6,000
bytes with the notices and the closing line (R5).

## Notice

One line before the hits for a predecessor or module that could not be found, or a file that could not be read; at most
three, then a count (R11).

## Recall-test question (harness only, not in the repository)

| Field | Rule |
|---|---|
| Id | Any text without a tab |
| Style | `named` or `indirect` |
| Question | The words passed to recall |
| Answer | A .NET regular expression, matched case-insensitively against the output |

Kept in the maintainer's DMC folder, never committed (FR-025, R12).
