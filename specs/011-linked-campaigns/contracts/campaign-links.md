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
  `digest` when a campaign nearer the start holds its digest (a `digests/<campaign>/` folder with a `.md` file in it).

The link lines are read outside fenced code blocks and HTML comments, a comment over several lines included, the same
way for `dmc` and the migration check `continuity-section`.
- `modules/<name> module <distance>`: each module the set reaches whose folder exists, once.
- A campaign with no links prints `no links`.

## Notices

| Line | When |
|---|---|
| `notice: <campaign>: <line> <name> is not in adventures/` | A linked folder is missing and no campaign in the set holds its digest |
| `notice: <campaign>: <line> <value> is not a campaign folder name` | A value with a separator, a drive or a leading dot |
| `notice: <campaign>: <line> names the campaign itself` | A self-link |
| `notice: <campaign>: <line> has more than one name; only <first> is followed` | A second name for Continues or Side campaign of, on the same line or on a second line of that name |

At most three notices, then `(+N more)`, as recall does. A Continues link to an open campaign is never reported
(FR-004); `dmc-new-campaign` and migration 005 check it with `dmc campaign standard <other>`.

## In `dmc size NAME --check`

Resume and the full save already run `dmc size <campaign> --check`, which prints nothing when all is well, so the link
lines come through it and play makes no new command call (FR-031). After the size flags, and only when there is
something to say (nothing at all for a campaign with no links and no news, SC-007):

| Line | When | Who acts on it |
|---|---|---|
| `notice: ...` as above, ending ` (not the full save's to trim)`; the `(+N more)` line ends so too | As above, for a line in NAME's file or an open campaign's: a closed campaign's file can no longer be changed, so its notices are left to recall and `dmc campaign links` | Resume tells the player in one sentence; the full save leaves it |
| `legacy: read adventures/<campaign>/legacy.md (written session N; record <campaign> sN) (not the full save's to trim)` | PR 3: a campaign in the set whose legacy's Written session is later than the one NAME's `game-state.md` Legacies folded line records for it, or none is recorded; `written unknown` when its Written line is unreadable. Not for a campaign whose digest a campaign nearer NAME holds (the digest carries it), and nothing at all when NAME is closed | Resume reads that file; the full save, at its own check, folds it and records its session (it acts on this line, though the line ends in `(not the full save's to trim)`) |
| `legacy due: at the full save, update this campaign's legacy.md for open linked campaigns <names>: add this save's changes and drop the lines from session N and earlier (not the full save's to trim)`, or `...: add this save's changes and keep every line` | PR 3: whenever the set holds an open campaign other than NAME, so that every full save, two in one session included, brings the file up to date. N is the lowest session the open campaigns in the set (NAME aside) record for NAME on their `game-state.md` Legacies folded lines; `keep every line the budget allows` when any of them records none | The full save adds its changes, drops the change lines from session N and earlier (none for `keep every line the budget allows`), and the oldest lines past the budget, once per save, then leaves the line; resume ignores it ("for the full save") |

The notices keep their cap of three; the legacy lines are not capped. Both legacy lines end `(not the full save's to
trim)`: the full save acts on them first, at step 7, and then leaves them at its re-check, as it leaves a session log's
line, instead of naming them in its report at every save.

## Failures

One line, exit 1, in the form every `dmc campaign` verb uses: `dmc campaign: no campaign "NAME" in adventures/ (there
are: …)`; `dmc campaign: no active campaign: name one (dmc campaign links NAME)`. Usage mistakes exit 2.

## Callers

- `dmc-new-campaign` § 2 and § 1c, after writing the Continuity section: `dmc campaign links <folder>`, relaying each
  `notice:` line.
- `dmc-resume-campaign` § 2, through the `dmc size --check` it already runs: each `notice:` line in one sentence; for
  each `legacy:` line, read that file (PR 3).
- `dmc-campaign-full-save` (PR 3), through its `dmc size --check`: on `legacy due:`, bring `legacy.md` up to date,
  keeping the lines the line names; on each `legacy:` line, fold that file into canon and record the session it gives.
  The line, not the model's memory of reading it, says what to fold, and it prints until the fold is recorded.
