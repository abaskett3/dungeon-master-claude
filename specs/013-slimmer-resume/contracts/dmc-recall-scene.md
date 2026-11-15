# Contract: `dmc recall NAME --scene`

```text
dmc recall [NAME] --scene [--folder PATH]
```

The canon lines the resume reads for the scene it is about to run (spec FR-007 to FR-009; research R3). Read-only.

## Arguments

| Argument | Meaning |
|---|---|
| `NAME` | A campaign folder in `adventures/` (default: the active campaign). With `--scene`, a lone argument is the campaign |
| `--scene` | Build the words from `game-state.md` and search only canon outside its always-read sections |
| `--folder` | The DMC folder, as for recall |

Words together with `--scene` are a usage mistake (exit 2): `dmc recall: --scene takes no words`.

## Output (exit 0)

Canon under `Budgets.CanonWholeBelow` (6,000 tokens):

```text
story/canon.md is small: read it whole
```

Otherwise the hits, best first, in recall's hit format (the campaign label, file and line, session, text trimmed around
the match as recall trims it), within 12,000 bytes, then `(N more lines matched)` when some did not fit:

```text
rorns-landing story/canon.md:131 s48.3 | The Kelderak treasure is held in the council strongroom until the hall votes…
rorns-landing story/canon.md:228 s51.2 | Bruni's crew starts on the east housing row on the 12th, three houses a week…
(41 more lines matched)
```

- `nothing matched: the scene` when no line of the searched sections holds a word of the scene.
- A `game-state.md` with none of the fields: `nothing to search for: game-state.md names no scene, quest or pending
  line`, exit 0, so the resume reads the always-read sections only and searches as play needs.

## Failures (exit 1, one line)

- No canon: `dmc recall: no story/canon.md in <campaign>`.
- Unreadable file: as recall.

## What the resume does with it

Item 7: on the "small" line, read canon whole; otherwise read the always-read sections whole and take these lines as
the scene's matches; search again (recall) whenever a subject comes up that it has not read.
