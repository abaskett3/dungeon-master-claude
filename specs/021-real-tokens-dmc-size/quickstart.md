# Quickstart: check the change

Run from the repository root. `<dmc>` is `dotnet run --project src/Dmc.Cli --`.

## Q1. The tests

```
dotnet test tests/Dmc.Tests
```

Expected: all pass, among them the `Measurement` edge tests (a part of 4B + 4 characters is not over budget B × 1.6,
one of 4B + 5 is; research R3), `TheDocumentedBudgetsEqualTheCode` against the new table, and the test that no
`/ 4` of a byte sum is left in `src/` (FR-001, SC-006).

## Q2. No new budget flag on any campaign (SC-004)

Before the change (on `main`) and after it, for each campaign at hand:

```
<dmc> size rorns-landing --check --folder D:/Claude/dmc
<dmc> size the-silver-egg --check --folder D:/Claude/dmc
<dmc> size <each> --check --folder D:/Claude/test-campaign
```

Expected: the same lines, with new numbers, in the same order. Allowed differences: a near-cap line added for a file
the resume reads whole that is not canon. Not allowed: any budget, canon or section line that was not there before.
Record both outputs in the PR.

## Q3. The total (SC-002)

```
<dmc> size rorns-landing --folder D:/Claude/dmc
```

Expected: a total of about 95k (research R4), within 88k to 124k, with the new header and total line of
[contracts/dmc-size-output.md](contracts/dmc-size-output.md).

## Q4. The measure against real counts (SC-001, SC-003)

```
python -I D:/Claude/dmc-slip-analysis/spec021/fit2.py        # after fit.py has written rows.json
python -I D:/Claude/dmc-slip-analysis/spec021/resume_all.py <log of bf7609c5>
bash D:/Claude/dmc-slip-analysis/spec021/count.sh sonnet <copies of campaign files>
```

Expected: the figures of research R1, R2 and R4. These run on the player's logs and files, not in CI; the PR quotes
their output. `count.sh` makes one headless call per file plus a baseline.

## Q5. Near the cap (SC-005)

Make a scratch campaign whose `story/sessions/` last log is read whole and holds 46,000 characters (Read cost about
18.6k). Expected: `dmc size --check` prints its near-cap line; at 44,000 characters (17.8k) it does not. A canon of
70,000 characters (28k) prints no near-cap line.
