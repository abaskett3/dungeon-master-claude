# Roadmap

This document is for anyone who wants to know what is finished, what comes next and what has been set aside. Status is as of 2026-10-17; the PRs and issues named are on GitHub.

## Status by area

| Area | Status | Notes |
|---|---|---|
| Move the tooling to one .NET 10 program, `dmc` (issue #3) | Done | Closed by PR #16. Phases below. |
| Releases 0.1.0, 0.1.1 and 0.1.2 | Done | Published and pinned (PR #17, PR #21, PR #26). See [releases.md](releases.md). |
| `dmc digest show` (PR #18) | Done | Shipped in 0.1.1. |
| Combat procedure in the prompts and templates, issue #6 Phase 1 (PR #19) | Done | Prompts and templates only. |
| Developer builds as `0.0.0-dev` in `artifacts/bin` (PR #20) | Done | Shipped in 0.1.1. |
| Documentation rule (PR #22) | Done | In `CLAUDE.md` § Git and GitHub. |
| Product documents in `.docs/` (PR #23) | Done | This folder. |
| Code documentation: XML doc comments, script and workflow headers (PR #24) | Done | Every type and member of `src/Dmc.Cli` documented, and the build now fails on a missing visible comment there; scoped to the test project. See [development.md](development.md). |
| `dmc roll "label:expr@target"`, issue #6 Phase 2a (PR #25) | Done | Shipped in 0.1.2. |
| Turn order drawn on the map, issue #6 Phase 2b (PR #25) | Done | Shipped in 0.1.2. |
| Play one fight and review it, issue #6 Phase 3 | Next | Then close issue #6. The first play test's findings became issue #35. |
| Break alert relative to the resume, issue #35 part 1 | Done, in the next release | `dmc context-check` starts its bands at the larger of 150,000 and the post-resume baseline plus 40,000; wording says the context is large. Changes the binary, so it ships in the release after 0.1.2. |
| Lighter resume, issue #35 part 2 | Next | Prompts only: search a large `canon.md`, read the tail of the session log, sheets of those present only. |
| `dmc pdf` output is all or nothing, issue #30 (PR #33) | Done (PR #33) | Changes the binary; ships in the next release, 0.1.3. |
| `play.ps1` exits with a real code when `claude` cannot be started, issue #28 (PR #31) | Done (PR #31) | Launcher only, so it needs no release. |
| `dmc digest` refuses a log with a bad token count as one error naming the file and line, issue #29 (PR #32) | Done (PR #32) | Changes the binary; ships in the next release, 0.1.3. |
| No deleting or moving campaign files in play, issue #36 (PR #38) | Done | A clause in `CLAUDE.md` § Saving as you go, deny rules for shell deletes and moves in `.claude/play-settings.json`, and a toolkit test. Not a complete block; see [decisions.md](decisions.md). |
| Play one fight and review it, issue #6 Phase 3 | Next | Then close issue #6. |
| Lighter resume, issue #35 part 2 | Done | Prompts and templates only: canon past about 230 lines is read as its always-read sections plus a search, the session log is read from its tail (whole after an unsaved session), sheets are limited to the PCs, the companions and those present, and every full save splits and counts canon. Closes issue #35 together with the break alert change (part 1, which ships in the release after 0.1.2). |
| `dmc pdf` output is all or nothing, issue #30 (PR #33) | Done (PR #33) | Changes the binary; ships in the next release, 0.1.3. |
| Play fights and review them, issue #6 Phase 3 | Done | Two sparring bouts reviewed. Readied actions, shown rolls, enemy options and movement held; the map was drawn before the narration in all 6 updates. |
| Redraw the map after the reply with a Stop hook, `dmc redraw`, issue #6 (PR #37) | Done (PR #37), not yet verified in play | Changes the binary and the play settings; it works only from release 0.1.3, once that is pinned. Prompt wording alone had failed 6 times out of 6. |
| The Stop hook opens the first map of a fight, issue #42 | Done, in the next release | `dmc redraw` draws `live.json` when there is no page yet and opens the page when it was not showing the scene; changes the binary, so it needs release 0.1.4 and its pin (written by the release workflow). |
| Check the hook and the plan / standing-orders rule in play, then close issue #6 | Next | See [Next](#next-issue-6). |
| Feedback about DMC recorded outside the campaign, issue #43 | Done (PR #44) | `record-feedback` and `review-feedback` skills, `feedback/`, `templates/feedback.md`; `review-transcript` now records entries and proposes no fixes. The migration of the first campaign's scattered feedback is done afterwards with the player, on local data. See [decisions.md](decisions.md). |
| CI skips changes that are not code | Done | `tests.yml` ignores `*.md` and `*.txt` unless they are the prompts the tests read. See [decisions.md](decisions.md). |
| `feedback/` searchable by the Grep tool | Done | `feedback/.ignore` overrides the git-ignore for ripgrep. See [decisions.md](decisions.md). |
| One route for feedback about DMC | Done | `record-feedback` is the only route; no stopgap step. See [decisions.md](decisions.md). |
| Six small prompt fixes from the first feedback review | Done | Offers, story timeline, reserved items, Edit not Write, out-of-character remarks in the log, reply last in every turn. See [decisions.md](decisions.md). |
| Every resume starts a new session | Done | Resume closes the last session and creates the next log; scenes number per session, cited `sN.M`. See [decisions.md](decisions.md). |
| Fights are planned with an ending | Done | `CLAUDE.md` § Sizing fights "How it ends"; fleeing is "for good". See [decisions.md](decisions.md). |
| Resume and full save bounded | Done | Size targets enforced at the full save; sheets read down to their history; consistency check uses targeted reads. Not yet measured in play. See [decisions.md](decisions.md). |
| Player messages that are not actions | Done | `CLAUDE.md` § Out of character (a message mid-task is answered first; a joke or complaint is never an event) and § Accuracy (do not re-ask what the character already told an NPC). See [decisions.md](decisions.md). |
| Split the combat tracker into `combat.md` | Not planned unless needed | |
| A Stop hook that warns about unshown rolls | Not planned unless needed | |
| Gatekeeper and SmartScreen checks on fresh machines | Skipped | The user's call; see [decisions.md](decisions.md). |

## Done

### Issue #3: one native CLI

| Phase | What | PRs |
|---|---|---|
| Phase 0 | Spike: Native AOT works on Windows, Linux and macOS with the chosen libraries; decision to ship AOT everywhere, Linux on Ubuntu 22.04 | Decision comment on issue #3 |
| Maps to scale | Every map drawn to a standard scale, in the Python renderer before the port | #5, #7 |
| Phase 1 | Skeleton: `dmc` with `roll` and `context-check`, the test project, the parity check, CI on three systems | #8 |
| Phase 2 | `dmc map`, the map format moved to `map-format.md`, Playwright tests | #9 |
| Phase 3 | `dmc pdf` | #11, #12 |
| Phase 4 | `dmc digest` | #13 |
| Phase 5 | `dmc play`, `get-dmc`, the release pin, the bootstrap launchers and the release workflow | #14, #15 |
| Phase 6 | Switch-over: prompts, settings and README point at `dmc`; the Python scripts, Python tests and parity check deleted | #16 (closes #3) |

### Since issue #3

- **0.1.0** published and pinned (PR #17), which let the switch-over merge.
- **PR #18:** `dmc digest digest` renamed to `dmc digest show`.
- **PR #19:** issue #6 Phase 1, the combat-procedure rules in `CLAUDE.md`, the skills and the templates.
- **PR #20:** developer builds report `0.0.0-dev` and publish to `artifacts/bin`, so `get-dmc` replaces any stray build in `bin/`.
- **0.1.1** published and pinned (PR #21).
- **PR #22:** the rule that every change updates its documentation.
- **PR #23:** these product documents.
- **PR #24:** XML doc comments throughout the code, header comments on scripts and workflows, and the build check for missing comments.
- **PR #25:** issue #6 Phase 2, labelled rolls and the turn order on the map.
- **0.1.2** published and pinned (PR #26).
- **PR #37:** issue #6 Phase 3 fix, not yet verified in play. `dmc redraw` and a `Stop` hook in the play settings draw the map after DMC's reply instead of DMC running `dmc map` before it; `dmc map` records what it drew in `maps/live-source.txt`. `CLAUDE.md` § Maps is shorter, and the remote-player picture rule is in the `draw-map` skill (for a remote player the order still depends on DMC writing the text first). Part of issue #6.
- **PR #33:** `dmc pdf` writes into a staging folder inside the output folder and moves the files into place only when the whole run has succeeded; a page too big for a JPEG is refused up front (issue #30).
- **PR #31:** `play.ps1` no longer exits with a stale code when `claude` is missing or cannot run: 127 if it is not found, 1 with a message if it cannot be started (issue #28).
- **PR #32:** `dmc digest` checks token counts as it reads each record, so a `NaN`, an `Infinity` or a text `output_tokens` is reported with its file and line by `list` and `show` alike, before any output (issue #29).
- **PR #38:** issue #36, `CLAUDE.md` forbids deleting or moving a file under `adventures/` unless the player asks, `play-settings.json` denies `rm`, `mv`, `Remove-Item`, `Move-Item`, `git clean`, `find -delete` and their aliases, and `rewind-campaign` lists stale maps for the player to delete instead of deleting them. Closes issue #36.

## Next: issue #6

Issue #6 fixes combat-procedure problems found by reviewing play sessions. Phase 1 (the prompts and templates, PR #19) is done. So is Phase 2, shipped in release 0.1.2 (PR #26), on which `CLAUDE.md` § Dice and § Maps now depend:

- **Labelled rolls (PR #25).** `dmc roll "label:expr@target"` prints a line ready to paste, for example `Goblin, shortbow: 1d20+4: [11] +4 = 15 vs AC 14, hit`, with a natural 20 or 1 deciding an attack. The bare syntax keeps working, and `dmc roll --help` is new. Described in [dmc-reference.md](dmc-reference.md#label-and-target).
- **Turn order on the map (PR #25).** `live.json` gains `"initiative"` (the order, by token id) and `"current"` (whose turn it is), and `dmc map` draws the turn order beside the grid with the current turn marked. Described in [campaign-files.md](campaign-files.md#the-map-files).

What the Phase 3 play test (two sparring bouts) found, and what remains:

- **Held:** readied actions said back with the blocking rules first, every DMC roll shown as `dmc`'s labelled line, enemy options stated with nothing used off the map, movement consistent, the turn order on the map.
- **Failed:** the map was redrawn and the picture sent before the narration in all 6 map updates, because reply text written after a tool call appears after it and DMC wrote none before `dmc map`. Rewording `CLAUDE.md` could not be relied on, so PR #37 moves the redraw into a `Stop` hook (`dmc redraw`), which runs after the reply. It is **not yet verified**.
- **Not exercised:** the plan and standing-orders rule.

What remains:

1. **Release 0.1.3 and its pin.** PR #33 (`dmc pdf` output all or nothing) and PR #37 (`dmc redraw`) change the binary. The Stop hook does nothing until the pinned `dmc` is 0.1.3 or later, and with an older one it stays silent and play carries on.
2. **Play test, then close issue #6.** In a fight, check that every map update appears after the text that describes it, that a refused map file is reported and corrected, and, for a remote player (where nothing but DMC's own ordering applies), that `live.png` is sent after the text; and exercise the plan and standing-orders rule. Review the session with `/review-transcript`.
3. **Follow-ups from the same review, tracked separately:** issue #35 (the break alert fires right after a resume; make it relative and make the resume cheaper) and issue #36 (DMC deleted campaign files; PR #38 addresses it).

Each of these updates [dmc-reference.md](dmc-reference.md), [campaign-files.md](campaign-files.md) (for `live.json`) and this roadmap, under the documentation rule.

## Not planned unless needed

- **A separate `combat.md`.** Moving the combat tracker out of `game-state.md` into its own small file, rewritten whole once per reply, would make fight bookkeeping cheaper and immune to stale edits. Issue #6 lists it as an option; Phase 1 instead asked for one edit per reply that replaces the tracker block. The Phase 3 play test showed the bookkeeping holding, so it is not needed yet; revisit if a later test shows it still costs too much.
- **A Stop hook for unshown rolls.** A hook that warns when a reply leaves out the rolls made in that turn. The Phase 3 play test showed every roll shown, so the rule in `CLAUDE.md` § Dice and the labelled rolls of Phase 2a are enough and no hook is needed for rolls. (The `Stop` hook that exists, `dmc redraw`, is for the map.)

## Skipped

- **Gatekeeper and SmartScreen checks.** Testing the unsigned `dmc` on a fresh Mac and a fresh Windows PC was left open through issue #3 and skipped by the user's decision; problems will be handled if they are reported.
