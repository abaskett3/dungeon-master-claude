# Contract: `dmc context-check`, the baseline and the first message

This contract covers what this feature changes in the hook's behaviour. Everything else about the hook stays as
`.docs/dmc-reference.md` § `context-check` and `specs/016-feedback-round-181/contracts/context-check-sheets.md`
describe it.

## Input

Unchanged. The hook's JSON on stdin holds `prompt`, `cwd` and `transcript_path`.

## Output

Unchanged in form. It prints the same lines in the same order: the break line, the canon reminder, the sheet lines,
then the out-of-character line. A failure still prints nothing and exits 0.

## Changed behaviour

### C1. Baseline after a resume call in the first three turns

For each log below, the reading is `(size, baseline)`. Here `S` is a main `Skill` call of `dmc-resume-campaign`,
`A(n)` is a main call at context `n`, `U` is a player message, `R` is a tool result and `B` is a boundary.

| Log | Reading | Rule |
|---|---|---|
| `U S(37000) R A(120000) A(146000) U A(150000) U A(154000)` | (154000, 146000) | Resume in turn 1 |
| `U A(30000) U S(32000) R A(140000) U A(150000)` | (150000, 140000) | Resume in turn 2 |
| `U S(37000) S(38000) R A(146000) U A(150000)` | (150000, 146000) | Two resume calls in one turn |
| `U S(37000) R A(146000) U A(150000) U S(151000) R A(250000) U A(260000)` | (260000, 146000) | Only the first resume counts (review of PR #271) |
| `U(cmd /dmc-resume-campaign) A(100000) U A(130000) U S(131000) R A(200000) U A(210000)` | (210000, 100000) | A resume before the first message is the first one |
| `U A(30000) U(cmd /dmc-resume-campaign) A(140000) U S(141000) R A(240000) U A(250000)` | (250000, 30000) | A slash resume in turn 1 is the first one |
| `U S(37000) R A(146000)` | (146000, 146000) | The resume turn is still running |
| `U A(30000) U A(31000) U A(32000) U S(33000) R A(140000) U A(150000)` | (150000, 30000) | Turn 4 does not count |
| `U Ss(37000) A(40000) U A(50000)` (sidechain `S`) | (50000, 40000) | A sub-agent's call does not count |
| `U A(250000) B A(30000) U S(31000) R A(120000) U A(130000)` | (130000, 120000) | Turns count from the boundary |
| `U(cmd /dmc-resume-campaign) A(142000) U A(150000)` | (150000, 142000) | Slash command, as today |
| `U S(37000) …` with the skill `resume-campaign` | as with `dmc-resume-campaign` | The old name |

With the first row, the first band is 186,000 and no break line prints at 154,000. Today the reading is
(154000, 37000), so the band is the 150,000 floor and the line prints (SC-001).

### C2. No sheet lines in a fresh log (no reply and no compaction boundary)

| `transcript_path` | Log | Present people with sheets | Sheet lines |
|---|---|---|---|
| set | `/clear` echo only | 3 | none |
| set | `… B` (boundary last) | 3 | as today (review of PR #271) |
| set | `… B A(60000)` | 3 | as today |
| empty | — | 3 | as today |

The canon reminder gets no people from the sheet part on a message that prints no sheet lines.

## Unchanged

The bands are `max(150000, baseline + 40000)`, then every 50,000. The one-offer-per-band check, the break line's
wording, the canon reminder's own matching, the out-of-character line and the exit code all stay as they are.
