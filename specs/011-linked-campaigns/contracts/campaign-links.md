# Contract: `dmc campaign links`, and the link lines of `dmc size --check`

PR 1 (legacy lines: PR 3). Read-only: writes nothing, anywhere.

```
dmc campaign links [NAME]
```

NAME defaults to the active campaign, as other `campaign` verbs do. A closed campaign is allowed (reading it changes
nothing).

## Output

One line per campaign in the linked set, nearest first, then by name; then the modules; then the notices.

```
the-silver-egg continues 1 closed
side-quest side 1 open
modules/the-crooked-moon module 1
notice: rorns-landing: Shares a world with old-west is not in adventures/
```

- `<campaign> <label> <distance> <open|closed|missing>[ digest]`: `missing` when its folder is not in `adventures/`;
  `digest` when a campaign nearer the start holds its digest.
- `modules/<name> module <distance>`: each module the set reaches whose folder exists, once.
- A campaign with no links prints `no links`.

## Notices

| Line | When |
|---|---|
| `notice: <campaign>: <line> <name> is not in adventures/` | A linked folder is missing and no campaign in the set holds its digest |
| `notice: <campaign>: <line> <value> is not a campaign folder name` | A value with a separator, a drive or a leading dot |
| `notice: <campaign>: <line> names the campaign itself` | A self-link |
| `notice: <campaign>: <line> has more than one name; only <first> is followed` | A second value on Continues or Side campaign of |

At most three notices, then `(+N more)`, as recall does. A Continues link to an open campaign is never reported
(FR-004); `dmc-new-campaign` and migration 005 check it with `dmc campaign standard <other>`.

## In `dmc size NAME --check`

Resume and the full save already run `dmc size <campaign> --check`, which prints nothing when all is well, so the link
lines come through it and play makes no new command call (FR-031). After the size flags, and only when there is
something to say (nothing at all for a campaign with no links and no news, SC-007):

| Line | When | Who acts on it |
|---|---|---|
| `notice: ...` as above, ending ` (not the full save's to trim)` | As above | Resume tells the player in one sentence; the full save leaves it |
| `legacy: <campaign> legacy.md (written session N) (not the full save's to trim)` | PR 3: a campaign in the set whose legacy's Written session is later than the one NAME's `game-state.md` Legacies folded line records for it, or none is recorded; `written unknown` when its Written line is unreadable | Resume reads that file; the next full save folds it and records its session |
| `legacy due: open linked campaigns <names>: rewrite legacy.md` | PR 3: the set holds an open campaign, and NAME's `legacy.md` is missing or its Written session is earlier than NAME's Current session | The full save rewrites it; resume ignores it ("for the full save") |

The notices keep their cap of three; the legacy lines are not capped.

## Failures

One line, exit 1: `dmc campaign links: no campaign NAME in adventures/`; `dmc campaign links: no active campaign: name
one`. Usage mistakes exit 2.

## Callers

- `dmc-new-campaign` § 2 and § 1c, after writing the Continuity section: `dmc campaign links <folder>`, relaying each
  `notice:` line.
- `dmc-resume-campaign` § 1, through the `dmc size --check` it already runs: each `notice:` line in one sentence; for
  each `legacy:` line, read that file (PR 3).
- `dmc-campaign-full-save` (PR 3), through its `dmc size --check`: on `legacy due:`, rewrite `legacy.md`.
