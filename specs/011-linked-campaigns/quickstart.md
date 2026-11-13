# Quickstart: validating linked campaigns and the sequel import

How to show each PR works. The automated tests are the tasks' business; these are the end-to-end runs, including the
live ones CI cannot do because no model runs there. Shapes and outputs are in [data-model.md](data-model.md) and
[contracts/](contracts/).

## Prerequisites

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
dotnet test
artifacts/bin/dmc install --path <scratch folder>     (in a terminal, outside Claude Code)
```

The scratch folder must be outside any checkout of this repository. Fixtures named below live in
`tests/Dmc.Tests/Fixtures/`.

## PR 1: links and recall

1. **A chain and a side campaign, by hand.** In the scratch folder, make campaigns `a` (closed), `b` (closed,
   Continues `a`), `c` (Continues `b`) and `d` (Side campaign of `c`), from the link fixtures. Run
   `dmc campaign links c`: four lines, `b continues 1 closed`, `a continues 2 closed`, `d side 1 open`, and `d`'s
   module if it has one. `dmc size c --check` prints no link line.
2. **Recall across the set.** `dmc recall c <a word only a's canon holds>` returns the line labelled `a (continues)`.
   A word only `d` holds is labelled `(side)`. Time it on the release build: under a second (SC-006, with the large
   fixture).
3. **The digest wins and appears once.** Copy the digest fixture into `c/digests/b/`. Recall a fact that is in both
   `b`'s canon and the digest: one hit, from `digests/b/`, labelled `(continues)`; `b`'s own copy is not printed.
4. **A missing folder with a digest.** Rename `b`'s folder away. Recall prints no notice for `b`; `dmc size c --check`
   prints none. Remove the digest too: one notice in each.
5. **Lookup.** `dmc lookup c <a person's name in the digest>` prints their section, or the list if `c` has a sheet of
   the same heading.
6. **Migration 005, live.** Make `old` (closed) and `seq` at standard 4 with `- **Predecessor**: old`. Start `dmc play`
   and ask DMC to bring `seq` up to date: no question about closing; `player-options.md` ends with the Continuity
   section, Continues `old`, and `dmc campaign verify seq` passed. Repeat with `old` open:
   - answer **yes** to closing: DMC closes `old` (the closing save, then `dmc campaign standard old` says `closed`),
     then migrates `seq`; `adventures/.active-campaign` names `seq` throughout;
   - answer **no**, then **yes** to "are you sure?": `seq` is migrated with Continues `old`; resume of `seq` says
     nothing about it;
   - answer **no** twice: DMC says `Migration stopped until the previous campaign is closed. You can close it by
     running: /dmc-close-campaign old`; `dmc campaign standard seq` still says `behind`, and no backup was taken.
   - with `old` behind: DMC migrates `old` first, then asks.
7. **Standing cost.** Measure bytes ÷ 4 of `kit/dm-rules.md`, the resume skill and the skill descriptions before and
   after; record the difference in the PR.

## PR 2: the sequel import

1. **The fixture, live.** Use the closable fixture campaign (one carried companion whose species default contradicts
   her history, a debt, a ruling, a secret never revealed and one revealed, a fact only in a session log, a test bout
   § How it ended names as not canon). Close it with `dmc-close-campaign`. Ask DMC for a sequel of it.
2. **What the player sees.** DMC asks which characters carry over, says it is importing part by part, and opens no
   scene until the sample test has passed. The main conversation reads no file of the closed campaign (check the
   transcript: only subagents read it, and DMC reads only the lines the sample comparison needs).
3. **The brief.** The sequel's canon Rulings and Prices, debts and promises hold the ruling and the debt with their
   sessions; its DM notes hold the never-revealed secret if it can still come up; its arc plan holds the open thread.
4. **The carried companion.** Her sequel sheet states her history's version of the trait, with the reason, and her
   voice, names used and appearance (SC-005). Later in the sequel, bring back a non-carried NPC from the closed
   campaign: the sheet DMC makes for them follows their history.
5. **The proof.** `dmc campaign sequel <sequel> --check` passes (SC-002, SC-009); `--sample` gives the same draw twice
   and a different one with `--round 2`. Delete one copied line from `people.md` and `--check` names it; restore it.
6. **The chain.** Close the sequel, start a sequel of it: `--start` copies the first digest unchanged and `--check`
   confirms it is identical; only the second campaign is digested.
7. **Resume.** Resume the sequel and read the transcript: no read under `digests/` or of the closed campaign (SC-001);
   `dmc size <sequel>` gives the same total with and without the digest folder.

## PR 2 follow-up: the maintainer's sequel, by hand (FR-029)

With the player, on local data, after The Silver Egg is closed (at Rorn's Landing's migration or before): run
`sequel rorns-landing --start`, build the digest with subagents as § 1c says but without a new campaign, account,
`--check`, `--sample`. Then rerun `tools/recall-test.ps1` on the play folder: answers in output at or above 28 of 30
named and 23 of 30 indirect, and the facts held in both places come from the digest (SC-004). Post the figures on
issue #179.

## PR 3: legacy files

1. **A side campaign's change.** With `c` and `d` from PR 1 open, play a scene in `d` that burns a bridge, and full-save:
   `d/legacy.md` exists, under 3k tokens, `Written: session N`.
2. **Resume the main campaign.** `dmc size c --check` prints `legacy: read adventures/d/legacy.md (written session N; record <campaign> sN)`; DMC reads
   that file only, and at the next full save the bridge is a canon line in `c` marked as world knowledge, and `c`'s
   `game-state.md` says `Legacies folded: d sN`. Resume again: no `legacy:` line (SC-008); `legacy due:`, which
   resume leaves for the full save, still prints, since `d` is open.
3. **A campaign alone** writes no `legacy.md` and prints nothing (SC-007).
4. **Two saves before the reader resumes.** Full-save `d` in two sessions, a change in each, without resuming `c`:
   the second save's `legacy due:` says to drop the lines from the session `c` last folded and earlier, and `d/legacy.md`
   holds both changes. Resume `c`, which reads both; after `c`'s next full save, `d`'s next save drops them.
