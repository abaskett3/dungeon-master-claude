# Contract: `slip-test`

A developer tool for the maintainer's computer. It never runs in CI, and nothing it reads or writes goes into git.

```
dotnet run --project src/Dmc.SlipTest -- <command> --work <folder> [options]
```

Every command prints one line on a failure the user can fix, with no stack trace, and exits 1. Exit 0 means the
command did its work (for `pilot`, that every check passed).

## The work folder

One folder per trial, outside the repository (suggested: `slip-test/<trial>/` in the DMC folder).

| File | Written by | Holds |
|---|---|---|
| `trial.json` | the maintainer | the trial (below) |
| `points.tsv` | the maintainer | the fork points: `id<TAB>session id<TAB>message uuid`; `#` lines and blank lines skipped |
| `key.tsv` | the scorer, before the runs | `id<TAB>what counts as a slip at this point`; one row for every point |
| `context/<arm>/<point>.md` | the maintainer, optional | the text a hook puts next to the player's message at that point in that arm (spec 029) |
| `allow.tsv` | the maintainer, optional | documented pilot differences: `check<TAB>name<TAB>reason`, `check` one of `notice`, `hook`, `thinking` |
| `lock.json` | `prepare` | the SHA-256 of `trial.json`, `points.tsv`, `key.tsv` and every context file (by its path in the work folder), and the resolved points |
| `runs.tsv` | `run` | one row per finished run (below) |
| `runs/<arm>/<point>-<n>.jsonl` | `run` | the fork's log |
| `runs/<arm>/<point>-<n>.json` | `run` | `claude`'s JSON output |
| `base/<arm>.jsonl`, `base/<arm>-adventures/` | `run`, fresh mode | the arm's base session and the `adventures/` it left |
| `pilot.md` | `pilot` | the checks, numbers and type names only |
| `export/replies.md` | `export` | the replies, shuffled, each under `## <id>` with its point id and its row of the key; the arm and run are hidden |
| `export/scores.tsv` | `export`, then the scorer | `id<TAB>slip<TAB>class<TAB>note`, slip `0` or `1` filled in by the scorer |
| `export/.map.tsv` | `export` | `id<TAB>point<TAB>arm<TAB>run`; the scorer does not open it |
| `result.md` | `score` | the result (below) |

### `trial.json`

```json
{
  "mode": "fork",
  "folder": "D:\\Claude\\DMC",
  "copy": "D:\\Claude\\DMX",
  "runs": 2,
  "arms": {
    "control": "arms/control.md",
    "treatment": "arms/treatment.md"
  },
  "pass": { "arm": "treatment", "against": "control", "below": 0 },
  "resume": "/dmc-resume-campaign",
  "effort": null,
  "context": { "treatment": "context/treatment" }
}
```

- `mode`: `fork` or `fresh`.
- `folder`: the DMC folder that played. `copy`: the copy folder, trusted in Claude Code, with a path of the same
  length when possible (the paths in the log are rewritten to it, and a path of another length changes the token
  count). Neither may be inside the other.
- `runs`: runs per point and arm, 1 to 20.
- `arms`: name to a rules file (fork mode, the whole text of the DMC folder's `CLAUDE.md`) or an overlay folder (fresh
  mode, files laid over the copy at the same relative paths). Paths are relative to the work folder. One arm measures a
  base rate.
- `pass`, optional: the trial passes when the upper end of the 95% interval of `arm`'s rate minus `against`'s is below
  `below`. Without it, `score` reports and gives no verdict.
- `resume`, fresh mode: the message that starts each base session.
- `effort`, optional: the effort to use when the log does not record one.
- `context`, optional, fork mode only (spec 029): arm name to a folder, relative to the work folder, that holds
  `<point id>.md` for every point. Before each run of that arm, `run` adds one `UserPromptSubmit` hook to the copy's
  `.claude/settings.local.json` that runs `slip-test context <file>`, which prints the file as the hook's `additionalContext`
  in Claude Code's JSON form, so Claude Code records the text as a hook output next to the kit's. A blank file adds no
  hook; a file over 10,000 characters is refused, because Claude Code shortens a longer hook output. Before and after every fork run, of any arm,
  the copy's `.claude/settings.local.json` is put back as the DMC folder has it. `prepare` refuses a missing folder or
  file and a DMC folder's `.claude/settings.local.json` that is not a JSON object, and locks the files with `trial.json`, `points.tsv` and `key.tsv`.

### `runs.tsv`

`point  arm  run  session  model  effort  permission  claude  kind  input  output  cache_read  cache_write  cost_usd  seconds`

`kind` is the original session's kind when its log names one, else empty. `claude` is `claude --version`.

## Commands

### `prepare`

Reads `trial.json`, `points.tsv` and `key.tsv`; finds each point's log in the DMC folder's Claude Code project folder
(`~/.claude/projects/<slug>/<session id>.jsonl`); checks that each message is a text message the player typed; checks
the arms exist; copies the DMC folder to the copy folder (refusing a copy folder that exists and is not empty, unless
it holds a `.slip-test` marker from an earlier `prepare`); writes `lock.json`.

### `run [--pilot]`

Refuses if a locked file changed. For each point, each run number, and the arms in a random order: skips a run that
`runs.tsv` already holds; restores the copy (fork mode: `adventures/` from the DMC folder, then the restore to the fork
time; fresh mode: the arm's base `adventures/`); writes the base log into the copy's project folder; runs `claude`;
copies its log into `runs/`; deletes both logs from the project folder; appends to `runs.tsv`. The base log holds only the fork message's chain of `parentUuid`s (an abandoned branch after a rewind, or a subagent's records, is left out) and every record with no uuid. A run that Claude Code reports as an error (`is_error`) is not recorded: the runner deletes every log in the copy's project folder and stops, and the next `run` repeats it. `--pilot` stops after
the first four point-and-arm pairs. Prints one line per run: `point arm run: <seconds>s $<cost>`.

### `pilot`

Checks every run of the first four pairs and writes `pilot.md`:

```
check     runs  pass  documented  fail
notice    4     3     1           0
hook      4     4     0           0
rules     4     4     0           0
thinking  2     2     0           0
verdict: pass
```

then, for each failure or documented difference, one line `check point arm run: <type name or count>`. No text of play.

When an arm has a context folder, the hook check leaves out every hook output whose text is that arm's text at the
point, and a `context` row follows `hook`: a run passes when that text is one of its hook outputs, or the file is
blank.

### `export`

Writes `export/replies.md`, `export/scores.tsv` and `export/.map.tsv` from every run in `runs.tsv`. Ids are 6
random hexadecimal characters; the order is random. A reply is the text blocks of the assistant records after the
replayed message, joined by a blank line; tool calls, tool results, thinking and attachments are left out. A fork log that does not hold the replayed message is refused.

### `score`

Refuses if a locked file changed, or a row of `scores.tsv` has no `0` or `1`, or an id is unknown, scored twice or not scored. Writes `result.md`:

```
arm        runs  points  slips  rate   95% interval
control    100   50      21     0.210  0.140 to 0.290
treatment  100   50      9      0.090  0.040 to 0.150
treatment - control: -0.120, 95% interval -0.210 to -0.030
pass line: treatment - control below 0: pass
cost: 200 runs, $4.12, 1,234,567 input tokens (98% from the cache), 45,678 output tokens
```
