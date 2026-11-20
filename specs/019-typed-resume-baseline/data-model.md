# Data model: The break baseline after a typed resume

No stored data changes. No campaign file, template or settings file changes either. The entities below are what
the hook reads from a Claude Code log. They live only in memory, for the length of one `dmc context-check` run.

## Log part

The records of one log after its latest `compact_boundary` system record. A `/clear` starts a new log, so the whole
log is one part when it has no boundary.

- **Boundary**: a record with `type` `system` and `subtype` `compact_boundary` (`TranscriptReader.Classify`). It
  starts a new part and resets every value below.

## Player message

A user record that `TranscriptReader.IsPlayerMessage` accepts. It is not a sidechain, meta or compact-summary record,
and it has a text block that `UserText.IsTyped` accepts. This definition does not change (FR-006).

## Turn

A player message and every record after it, up to the next player message in the same part. The records before the
first player message belong to no turn. Turns are numbered from 1 within a part.

## Main call

An assistant record that is not a sidechain and has a non-zero context, which `Classify` reports as
`RecordKind.Assistant`. Its context is input plus cache read plus cache creation tokens.

## Resume call

An assistant record that is not a sidechain and holds a `tool_use` block named `Skill`, whose `input.skill` is
`dmc-resume-campaign` or `resume-campaign` (research R1). Its usage does not matter.

## Resume turn

A turn numbered 1, 2 or 3 that holds the part's first resume, a resume call. The first resume may instead be
before the first player message (a resume call or the slash command's echo) or a slash command in turns 1 to 3;
then no turn is a resume turn (the code review of PR #271).

## Baseline (derived)

| Case | Baseline |
|---|---|
| The part has a resume turn | The context of the last main call of that turn |
| Otherwise, a call came before the first player message | The context of the last such call (unchanged) |
| Otherwise, a player message came | The context of the first main call after it (unchanged) |
| No player message yet | The latest context (unchanged) |

## The state of the forward pass (per part)

| Value | Starts at | Changes on |
|---|---|---|
| `before` | 0 | each main call before the first player message (unchanged) |
| `baseline` | 0 | the first player message (set to `before`), or the first main call after it while still 0 (unchanged) |
| player messages seen | 0 | each player message, counted up to 4 |
| resume seen | false | the part's first resume: a resume call or a slash echo before the first player message or in turns 1 to 3 (true) |
| resume in this turn | false | the first resume when it is a resume call in turn 1 to 3 (true); the next player message (false) |
| last call in this turn | 0 | each main call while the turn is the resume turn |
| resume baseline | 0 | the close of a resume turn (set to that turn's last call) |
| settled | false | `baseline` > 0, no resume turn open, and a resume seen or four player messages seen. Past this, only possible boundary lines are classified |

A turn closes on the next player message or at the end of the log. A boundary resets every value to its start.

## Sheet lines: when the part prints nothing

| Hook input | Log has a main call after its latest boundary | Sheet lines |
|---|---|---|
| Names a fresh log (no call and no boundary) | No | None (FR-005) |
| Names a log whose latest record is a boundary | No | As today (review of PR #271) |
| Names a log | Yes | As today |
| Names no log | — | As today |
| Names a log that is missing or cannot be read | — | The whole command prints nothing (unchanged) |
