# Contract: `dmc module check` and the module line of `dmc size`

## `dmc module check NAME [--folder PATH]`

Reads every checked file of `modules/<NAME>/` (data-model.md § Module) once, in memory, and prints its problems
(data-model.md § Module problem). Writes, moves and deletes nothing.

| Case | Output | Exit |
|---|---|---|
| Sound module | nothing | 0 |
| Problems | one line each, at most `Budgets.CheckLines` (12); past it, the last line is `... and <n> more` | 0 |
| `--help` | the usage text | 0 |
| No `modules/<NAME>/`, or it holds no checked file | `dmc module: no module <NAME> (modules: <a>, <b>, …)` | 1 |
| A file cannot be read | `dmc module: cannot read <path>: <reason>` | 1 |
| Unknown verb or option, a second name, no name | one line, argparse style, as `dmc size` | 2 |

- `NAME` may be written `modules/<NAME>`; a name starting with `.` is refused as for lookup.
- Problems are printed in file order (`SourceFiles.Module`'s ordinal order), and within a file: the file line, then
  section lines by line number. Paths use `/` on every system.
- An unfinished ingest is not a problem: a chapter the index lists but no file holds is not reported.
- `--folder` as for `dmc size`.

## `dmc size [NAME] --check`, the module line

After the flags and before the `notice:` and `legacy:` lines, for a module campaign that is not finished and whose
module folder exists:

```
module: modules/<name> has <n> problems: run dmc module check <name> (not the full save's to trim)
```

`1 problem` in the singular. Nothing for a sound module, a finished one, a homebrew campaign or a module not
ingested. The full report prints the same line after its flags.

## `dmc size`, the pointer

- A pointer in the new form (`§ H1; § H2`) is measured as the sections lookup prints, each once, as one part per
  heading: `modules/<m>/chapters/<file> § <heading>`.
- A heading that matches nothing adds a flag that holds the save:
  `module-progress.md: no heading "<heading>" in chapters/<file> (nearest: <a>; <b>): name one`, and the chapter is
  measured whole, as today.
- The old forms are measured as today.

## `dmc lookup`

No change to what it searches, how it matches or what it prints (spec FR-016). Its matcher is factored into
`Lookup.FindSection` so that `dmc size` uses the same one (research R8).
