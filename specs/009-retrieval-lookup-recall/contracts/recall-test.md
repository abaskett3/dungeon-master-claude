# Contract: the recall-test harness

```text
pwsh tools/recall-test.ps1 -Questions <file> [-Campaign <name>] [-Dmc <path>] [-Folder <DMC folder>]
```

Runs `dmc recall` once per question and counts how often the answer is in the output and in the first hit. For the
maintainer's local check of SC-002; it never runs in CI and writes nothing. Decisions: [research.md](../research.md)
R12.

## Parameters

| Parameter | Meaning |
|---|---|
| `-Questions` | The questions file (below). Required |
| `-Campaign` | The campaign to recall in. Default: the active campaign |
| `-Dmc` | The `dmc` to run. Default: `artifacts/bin/dmc` if it exists, else `dmc` on the PATH |
| `-Folder` | The DMC folder, passed to `dmc recall --folder`. Default: none (`dmc` finds it) |

## The questions file

UTF-8, tab-separated, one question per line; blank lines and lines starting with `#` are skipped.

```text
# id	style	question	answer
1	named	What does Oswin charge for a cart?	1 gp
1	indirect	what does the ferryman want for the wagon	1 gp
```

`style` is `named` or `indirect`. `answer` is a .NET regular expression, matched case-insensitively. The file quotes
campaign facts, so it lives in the maintainer's DMC folder (suggested: `recall-test/questions.tsv`) and is never
committed.

## Output

```text
 1 named    HIT  first  What does Oswin charge for a cart?
 1 indirect HIT        what does the ferryman want for the wagon
...
named: answer in output 28/30; first hit 21/30
indirect: answer in output 22/30; first hit 11/30
```

Exit 0 when every question ran; 1, with one line, when the file cannot be read, a line is malformed, or `dmc recall`
fails for a reason other than "nothing matched".
