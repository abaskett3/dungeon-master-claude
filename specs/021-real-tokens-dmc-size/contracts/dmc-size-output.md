# Contract: what changes in `dmc` output

Only the unit and the numbers change. Every line's shape, order, exit code and the `--check` and `--arc` forms are as
`.docs/dmc-reference.md` § size, § module and § lookup state them now, except as below.

## `dmc size` (the full report)

- **Header**: `<name>: what a resume reads, in estimated tokens (bytes / 4)` becomes
  `<name>: what a resume reads, in tokens (estimated: 2 for 5 characters)`.
- **Each file's figure**, each section's and each budget shown is in text tokens (data-model.md). Budgets show their
  new numbers (`budget 6.4k` for `game-state.md`, `limit 30.4k` for canon).
- **The total line** gives the Read cost and what it leaves out:
  `  total <T>   to read, with <N> Read calls; DMC's own replies are not counted`
  where `<T>` is the sum of the parts' Read cost and `<N>` the calls it counts. A part outside the total (a campaign's
  own `legacy.md`) is still left out.
- The `near the Read cap` note in the table follows the flag's rule (no note on canon within its limit).

## `dmc size --check` (the form DMC runs in play)

The flag lines keep their words; the numbers in them change unit:

```
story/canon.md 33.9k is over its 30.4k limit: a full save archives it down to 25.6k (move about 8.3k)
story/dm-notes.md 18.4k is near the Read cap (25k): it may be cut short at resume
story/dm-notes.md 18.2k is over its 16k budget
```

The near-cap line's figure is the part's Read cost; the budget line's figure is its text tokens, so the two can differ
by 160 for one file. A near-cap line never names canon within its limit.

## `dmc module check`

The file line keeps its words, with the file's Read cost as `<t>` and the new mark:
`<file> <t> tokens, <n> lines: at the Read cap mark (18k): split it by heading into smaller files beside it`.
A heading over `ModuleSection` shows its text tokens against `4.8k`.

## `dmc lookup`

The read-in-parts line, `<where>: section <heading> is <t> tokens, <n> lines: read it in parts`, compares and shows
the section's Read cost against 18k.

## `dmc campaign sequel --check`

`too large: digests/<other>/<file> <t> tokens (split it by subject)` compares the file's Read cost with 18k; the
`largest` figure of the `checked` line is a Read cost too.

## Help text

`dmc size --help`: "its size in estimated tokens (bytes / 4)" becomes "its size in tokens (estimated at 2 for 5
characters, which matches what Claude Code counts within a few percent)". `Summary` is unchanged ("in estimated
tokens").
