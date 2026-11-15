# Contract: `dmc context-check`, the canon reminder

Runs as today from the `UserPromptSubmit` hook (`dmc context-check 2>/dev/null || true`); `kit/settings.json` is
unchanged. Spec FR-015 to FR-019; research R5, R6.

## Input

The hook's JSON on standard input: `transcript_path` (as today), `prompt` (the player's message) and `cwd` (the DMC
folder the session runs in).

## Output

1. The break reminder line, when due, as today.
2. Then, when the prompt names people in the register not yet reminded in this session part:

```text
Canon on Kole, Bruni (a reminder of some lines, not all canon says; recall for more):
rorns-landing story/canon.md:72 s1 | Captain Emmajeen Kole: captain of the Pinebrook guard, Shale's friend and second…
rorns-landing story/canon-archive.md:40 s23.2 | Kole owes Bruni's crew a cask for the wall repair…
rorns-landing story/canon.md:231 s51.2 | Bruni's crew starts on the east housing row on the 12th…
```

- At most 3 lines a name, 4,000 bytes in all (the header counted); names in the order the prompt names them.
- Lines from canon outside the always-read sections, then `story/canon-archive.md`; never other files or campaigns.
- A name in a `Canon on …` header recorded in the log after its latest compaction boundary is not reminded again.

## Silence (nothing printed, exit 0)

No `prompt`, `cwd` or active campaign; the campaign folder missing or holding `.closed`; a prompt starting with `/`; no
registered name in the prompt, or none new; no canon; any error. The break line still prints when due.

## Cost

Typical: nothing (no new name). At the cap: about 1,000 tokens once. Time: under half a second on Rorn's Landing
(SC-007).
