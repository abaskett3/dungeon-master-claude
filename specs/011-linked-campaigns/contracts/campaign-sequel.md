# Contract: `dmc campaign sequel`

PR 2. The exact parts of the sequel import (FR-011, FR-013, FR-015, FR-021 to FR-024). NAME is the sequel; the closed
campaign is the one its Continues line names. Every form writes only under `adventures/NAME/digests/` (only `--start`
writes at all); nothing is ever written to the closed campaign (FR-016).

```
dmc campaign sequel NAME --start
dmc campaign sequel NAME --list [--all]
dmc campaign sequel NAME --check
dmc campaign sequel NAME --sample [--round N]
```

## Refusals common to every form (exit 1, one line)

| Message | When |
|---|---|
| `NAME is closed: start a sequel to continue its story` | NAME is closed (the existing guard) |
| `NAME continues nothing: its Continues line is none` | No Continues link |
| `NAME continues OTHER, which is not closed: close it first with dmc-close-campaign` | The link names an open campaign |
| `OTHER's closing backup is missing: backups/<name> (it cannot be digested without it)` | The `.closed` mark names a file that is not there |
| `OTHER's closing backup fails its check: <reason>` | The archive fails the export check |
| `OTHER has changed since it closed: run dmc campaign verify OTHER` | `verify`'s closed path finds a difference |

## `--start`

Creates `digests/<OTHER>/` with `index.md` (Format 1, Campaign, Closing backup with SHA-256, Chain, `Carried: none`,
Files) and `spine.md`; copies each `digests/<X>/` that OTHER holds into `digests/<X>/` of NAME, byte for byte. Then
prints the parts plan. Part 1 is always the files only the brief needs, `player-options.md` and `story/dm-notes.md`
whole (FR-007's threads, house rules and requests; dm-notes' secrets too). Then the accounted sources and the session
logs of OTHER, in parts of about 100 KB, split only at a session log's boundary or a source's `## ` heading. A source
OTHER does not have is left out of the plan and counts no units.

```
started digests/the-silver-egg/ (spine: 46 sessions, 169 scenes; chain: none)
part 1: player-options.md, story/dm-notes.md (the brief)
part 2: story/canon.md, story/canon-archive.md 1-310
part 3: story/canon-archive.md 311-702, character-sheets/ (19 files)
part 4: story/sessions/session-001.md to session-009.md
...
```

Run again on an existing `digests/<OTHER>/`, it rewrites `index.md`'s generated lines (keeping Carried), `spine.md`
and the chain copies, and prints the plan again. It refuses if NAME already holds a different campaign's digest under
the same folder name: `digests/OTHER/ holds a digest of X: it cannot be started over for OTHER`. The plan prints a file
split at its headings as `path a-b`, and a run of sheets or session logs as `folder/ (N files)` when it is the whole
folder, else `folder/first to last`.

## `--list`

Each unaccounted unit, at most 40 lines without `--all`, then `(+N more: --all)`:

```
unaccounted: 7c1e09a2b4f6 story/canon-archive.md:212 | Kibil fell through the ice at Grey Tarn and nearly drowned (s9.3).
```

Prints `nothing unaccounted` when done.

## `--check`

Passes with one line, exit 0:

```
checked digests/the-silver-egg/: 2,841 units (2,610 copied, 148 merged, 83 dropped), 9 files, largest 18.2k tokens
```

Fails with one line per problem (at most 40, then `(+N more)`), exit 1, with `check failed: N problems` on standard
error:

| Line | Problem |
|---|---|
| `unaccounted: N units (dmc campaign sequel NAME --list)` | R9 |
| `accounting: <id>: <reason>` | An unknown id, a class not declared, an anchor not found in the digest |
| `copy differs: digests/<X>/<file>` | A chain copy is not byte-identical to OTHER's |
| `index: <line>` | A required index line missing, a Chain line that is not the chain copied, or its checksum differs from the closing backup |
| `too large: digests/<OTHER>/<file> <N>k tokens (split it by subject)` | Over 20k estimated tokens |
| `heading twice: digests/<OTHER>/<kind>: <heading>` | A subject heading appears twice across a kind's parts |

## `--sample`

```
sample round 1 of digests/the-silver-egg/ (seed 3f5a09c2d1e4b7a8+1): 17 copied facts, 3 session-only facts, 5 people
fact 1: places.md:44 | The tollhouse at Mardle Ford charges a copper per wheel (s4).
...
person 1: character-sheets/npc-kibil-rornson.md | Kibil Rornson (carried)
person 2: character-sheets/minor-npcs.md:9 | Old Hesketh
...
```

Facts come from the digest's subject files; people from the closed campaign's sheets and minor NPCs in its closing
backup. Exit 1 with `the digest has no fact lines` or `the closing backup has no characters` when either is empty.

## Help

`dmc campaign --help` gains the four forms in its usage block and one paragraph.
