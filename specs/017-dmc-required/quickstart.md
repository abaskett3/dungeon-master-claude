# Quickstart: validating that `dmc` is required

Headless runs use `D:\Claude\test-campaign`, never the player's own folder. Build first, then have the maintainer
write this branch's kit into the test folder from a terminal (`install` and `refresh` refuse inside a Claude Code
session; `.docs/development.md` § Trying a change):

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
artifacts/bin/dmc refresh D:\Claude\test-campaign
```

## 1. Tests

`dotnet test`: `TheKitCarriesNoRuleForPlayingWithoutDmc` (FR-001, FR-003; [research.md](research.md) R6),
`TheAlwaysLoadedRulesStayAtTheirLineAndByteCaps` at the new cap (FR-009), and the unchanged
`WithoutAWorkingDmcASessionIsToldOnceToReinstall` and reinstall-line assertion (FR-002).

## 2. The kit (SC-001, SC-002)

- `grep -rn -i -E 'If `dmc` is unavailable|Without `dmc`, skip|If `dmc` fails or the recall|Otherwise, or when it finds nothing|carry on without dice and maps' kit/`
  prints nothing. (`dmc-resume-campaign:14`'s "If `dmc` fails, Glob" stays: [research.md](research.md) R2, R3.)
- `wc -c kit/dm-rules.md` prints at most 32,506; `wc -l` the same line count as before (212 lines).

## 3. A missing `dmc` (SC-003)

In `D:\Claude\test-campaign`, with neither `artifacts/bin` nor `C:\Users\webhe\AppData\Local\Programs\dmc` on `PATH`:

```
claude -p --model sonnet "Let's continue the campaign east-hills."
```

Pass: the reply gives the install line and plays nothing: no scene, no roll, no map, no campaign file read past what
it took to find `dmc` missing. Record whether the start-of-session notice appeared (unverified under `claude -p`,
R9); the rule must hold either way.

Then put `artifacts/bin` back first on `PATH` and continue the same session (FR-003, US2 scenario 2):

```
claude -p --continue --model sonnet "I've reinstalled dmc."
```

Pass: DMC runs `dmc --version` and, once it runs, resumes the campaign. A `dmc` that goes missing mid-session (spec
Edge Cases) falls under the same line of `kit/dm-rules.md` and is covered by this run, not tested on its own.

## 4. A working `dmc` (SC-004)

With `artifacts/bin` first on `PATH`, in a scratch copy of one test campaign, ask for a roll, a recall of a returning
NPC and a map. Pass: `dmc roll` lines are pasted, `dmc recall` runs, and the map is drawn by `dmc`, as before.

## 5. Documents (SC-005)

`grep -rn -E 'never depends on `dmc`|play and saving still work' README.md .docs/` finds the phrase only in a
decision entry marked "Partly superseded".

## Results

Run 2026-11-17 on `017-dmc-required` (base `fbe3ede`).

- **Baseline (T002, T003):** `dotnet test` 2,155 passed, 8 skipped (symbolic-link tests), 0 failed. `kit/dm-rules.md`
  32,522 bytes, 212 lines; `dmc-draw-map` 11,377, `dmc-ingest-module` 9,827, `dmc-setup-rules` 3,671 bytes.
- **§ 1 Tests (T004, T010, T018):** `TheKitCarriesNoRuleForPlayingWithoutDmc` failed before the edits on the four
  files (six hits) and passes after. `ThePlayRulesNameRecallAndLookupAndKeepTheFallback` held the removed line-78 text
  and now holds the kept step (research.md R6). Full run: 2,156 passed, 8 skipped, 0 failed.
- **§ 2 Kit (T016, T019, T020; SC-001, SC-002):** the search prints nothing. `kit/dm-rules.md` 32,506 bytes (−16),
  212 lines; the cap is 32,506. Skills −274 bytes (`dmc-draw-map` −25, `dmc-ingest-module` −202, `dmc-setup-rules`
  −47). Pass.
- **§ 5 Documents (T020; SC-005):** the search finds the phrase only at `.docs/decisions.md:507`, in the entry marked
  "Partly superseded 2026-11-17" (line 511). Pass.
- **The test folder (T021):** the maintainer works remotely and could not run `dmc refresh` in a terminal, and it
  refuses inside a Claude Code session. Instead `D:\Claude	est-campaign` was copied to the session's scratchpad and
  this branch's `kit/` written into the copy as `GeneratedFiles.Table` maps it (`CLAUDE.md`, `.claude/settings.json`,
  `.claude/skills/`, `templates/`, `migrations/`); `cmp` and `diff -rq` confirmed the copy matches the branch. The
  real test folder was not changed.
- **§ 3 A missing `dmc` (T022; SC-003):** pass. With neither `dmc` folder on `PATH`, the `SessionStart` hook ran under
  `claude -p` and printed the notice (R9's unverified point: it does run). DMC ran `dmc campaign standard`, got
  `dmc: command not found`, and replied: "I can't start east-hills yet. `dmc` isn't installed or won't run here
  (`dmc: command not found`), and the campaign rules say to play nothing until `dmc --version` works", with both
  install lines; no scene, roll or map, and no file changed (it listed `adventures/` but read no campaign file).
  Then, with `artifacts/bin` on `PATH`, `--continue` "I've reinstalled dmc.": DMC ran `dmc --version` (`dmc
  0.0.0-dev`), then `dmc campaign standard` (`behind`, offered the migration), and resumed with a recap. Pass.
- **§ 4 A working `dmc` (T023; SC-004):** the roll and the map pass: `dmc roll` gave `1d20: [16] = 16`, pasted in the
  reply, and `dmc-draw-map` wrote the camp map and ran `dmc map ... --png --open`, with no text map. The recall was not
  exercised: asked about steward Corwin, DMC answered from canon, which the resume had read whole because `dmc recall
  east-hills --scene` said "story/canon.md is small: read it whole"; it did not run `dmc recall` for him. The answer
  matched canon. Line 78's "recall it first" is unchanged by this spec.
