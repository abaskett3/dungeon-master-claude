# Quickstart: validating lookup and recall

Phase 1 of [plan.md](plan.md). How to check the feature works, from the automated tests to the maintainer's play
folder. The commands' interfaces are in [contracts/](contracts/); the decisions in [research.md](research.md).

## 1. Build and run the tests

```text
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
dotnet test
```

Expected: every test passes, with the usual skips. The new tests are in `tests/Dmc.Tests/Retrieval/`; the error-path
and output-bytes guards and the toolkit tests gain rows for the two commands. The timing test (SC-004) runs against the
AOT build in CI (`-p:PublishAot=true`).

## 2. Try the commands on a scratch DMC folder

Make a scratch folder outside any checkout (`artifacts/bin/dmc install --path <scratch>`), copy the synthetic fixture
campaigns the tests build (or any campaign of your own) into its `adventures/`, then:

```text
artifacts/bin/dmc lookup <module> goblin boss --folder <scratch>
artifacts/bin/dmc lookup <campaign>/story/canon.md prices, debts and promises --folder <scratch>
artifacts/bin/dmc lookup rules grappled --folder <scratch>
artifacts/bin/dmc recall <campaign> ferry price cart --folder <scratch>
```

Check against the contracts: the section exactly; the list of matches when a heading repeats; one line when nothing
matches; for recall, labels, the `DM` mark, trimming, the `(N more lines matched)` line, and output within 6,000 bytes.

## 3. Play with it (SC-007 and User Story 4)

In the scratch folder, `dmc play`, resume a campaign, and bring back an NPC from an early session. Check that DMC
recalls before narrating and does not read whole archives. Then take `dmc` off the PATH for the session and bring back
another: DMC searches the files as before and the game goes on.

## Step 1: the maintainer's play folder (after the merge, posted on issue #179)

These change nothing in the repository and the PR does not wait for them (spec § Clarifications).

1. In a terminal, `dmc update` (the play folder has 0.2.1). `dmc --version` shows the latest release; `dmc refresh`
   if the update did not refresh the DMC folder.
2. `dmc play` in `D:\Claude\dmc`, resume Rorn's Landing. The resume says the campaign is behind the kit's standard and
   offers the migration. Accept: `dmc-migrate-campaign` takes a checked backup, applies migrations 002 to 004, and
   verifies. Check that `player-options.md` now has `- **Predecessor**: the-silver-egg` (set it with the player's word
   if the migration left `none`).
3. `dmc size rorns-landing`. Post the total on issue #179 beside the 76k baseline (SC-001), with the flags if any.

## The recall test (SC-002, SC-003; after Step 1)

1. Once, convert the prototype's `queries.py` into `D:\Claude\dmc\recall-test\questions.tsv` in the shape of
   [contracts/recall-test.md](contracts/recall-test.md): two lines per fact (`named`, `indirect`), the answer regex
   from each tuple. Never commit it.
2. Run `pwsh tools/recall-test.ps1 -Questions D:\Claude\dmc\recall-test\questions.tsv -Campaign rorns-landing`.
3. Pass: `named` at least 28/30 and `indirect` at least 22/30 in the output, every output within its cap. Post the two
   summary lines on issue #179. If a figure falls, a follow-up change tunes the weights (R1) or the trim (R4), and the
   test runs again.
