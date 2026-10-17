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
| Lighter resume, issue #35 part 2 | Done | Prompts and templates only: canon is searched past about 230 lines, the session log is read from its tail, sheets are limited to the PCs, the party and those present, and every full save checks canon's size. Closes issue #35 together with the break alert change (part 1, which ships in the release after 0.1.2). |
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

## Next: issue #6

Issue #6 fixes combat-procedure problems found by reviewing play sessions. Phase 1 (the prompts and templates, PR #19) is done. So is Phase 2, shipped in release 0.1.2 (PR #26), on which `CLAUDE.md` § Dice and § Maps now depend:

- **Labelled rolls (PR #25).** `dmc roll "label:expr@target"` prints a line ready to paste, for example `Goblin, shortbow: 1d20+4: [11] +4 = 15 vs AC 14, hit`, with a natural 20 or 1 deciding an attack. The bare syntax keeps working, and `dmc roll --help` is new. Described in [dmc-reference.md](dmc-reference.md#label-and-target).
- **Turn order on the map (PR #25).** `live.json` gains `"initiative"` (the order, by token id) and `"current"` (whose turn it is), and `dmc map` draws the turn order beside the grid with the current turn marked. Described in [campaign-files.md](campaign-files.md#the-map-files).

What remains:

1. **Phase 3, check it worked.** Play one fight, run `/review-transcript` on that session, and check that the map never ran ahead of the narration, every enemy option was stated, readied actions were said back, every roll was shown and movement stayed consistent.
2. **Close issue #6.**

Each of these updates [dmc-reference.md](dmc-reference.md), [campaign-files.md](campaign-files.md) (for `live.json`) and this roadmap, under the documentation rule.

## Not planned unless needed

- **A separate `combat.md`.** Moving the combat tracker out of `game-state.md` into its own small file, rewritten whole once per reply, would make fight bookkeeping cheaper and immune to stale edits. Issue #6 lists it as an option; Phase 1 instead asked for one edit per reply that replaces the tracker block. Revisit if Phase 3 shows bookkeeping still costs too much.
- **A Stop hook for unshown rolls.** A hook that warns when a reply leaves out the rolls made in that turn. Issue #6 makes it optional, only if the rule in `CLAUDE.md` § Dice and the labelled rolls of Phase 2a are not enough.

## Skipped

- **Gatekeeper and SmartScreen checks.** Testing the unsigned `dmc` on a fresh Mac and a fresh Windows PC was left open through issue #3 and skipped by the user's decision; problems will be handled if they are reported.
