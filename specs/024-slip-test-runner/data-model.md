# Data model: The slip test runner

Every entity lives in the work folder; the file formats are in [contracts/slip-test.md](contracts/slip-test.md).

| Entity | Fields | Rules |
|---|---|---|
| Trial | mode (`fork`, `fresh`), folder, copy, runs, arms, pass line, resume message, effort | Locked by `prepare`; folder and copy distinct and not nested; runs 1 to 20; at least one arm |
| Arm | name, rules file or overlay folder | Name: letters, digits and `-`; the file or folder exists |
| Fork point | id, session id, message uuid; resolved: log path, the message's text, its time, model, effort, permission mode, kind | The message is a `user` record whose content is text the player typed (not a tool result, not meta, not a command) |
| Key row | point id, text | One row for every point |
| Allowed difference | check, name, reason | `check` one of `notice`, `hook`, `thinking` |
| Run | point, arm, run number, session id, model, effort, permission mode, Claude Code version, kind, tokens, cost, seconds | Unique by point, arm and run number |
| Export row | id, point, arm, run | Id unique, 6 hexadecimal characters |
| Score | id, slip (0 or 1), class, note | One per export row |

**State**: a trial moves from written, to locked (`prepare`), to run (`run`), to exported (`export`), to scored
(`score`). A change to a locked file sends it back: `run` and `score` refuse until a new work folder is prepared.
