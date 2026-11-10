# Quickstart: validating `dmc size`

How to show the feature works, end to end. The interface is in [contracts/dmc-size.md](contracts/dmc-size.md), the
rules in [data-model.md](data-model.md).

## Prerequisites

- The .NET SDK the repository builds with (see `.docs/development.md` § Requirements).
- A build published to `artifacts/bin`, since the contract tests run the program itself:

  ```
  dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
  ```

  Use your runtime identifier (`linux-x64`, ...) in place of `win-x64`.

## 1. The automated checks

```
dotnet test
```

These must pass. Each maps to the spec:

| Check | Where | Spec |
|---|---|---|
| The read set of fixture campaigns: every file the resume reads, none it does not, the right part of each (sheet to its history, history in the middle, log tail, whole log after an unsaved session, canon whole and in sections, a module campaign, a finished module) | `tests/Dmc.Tests/Size/SizeLibraryTests.cs` | US1, FR-002, SC-001 |
| Each count is bytes ÷ 4 of exactly the part read; sections largest first | the same | FR-003 to FR-005 |
| Each flag fires on the fixture built for it and on no other: near the cap (tokens and lines), over budget, section over budget, canon over its limit, Last played log, finished Opposition rows, DONE lines outside the Arc plan, Prices section, long lines | the same | US3, FR-006 to FR-009, SC-009 |
| Two canons of the same line count and three times the size: one read whole, one in sections | the same | SC-005 |
| Arguments, output and exit codes, in-process and through the binary; `--check` silent when all is well, one line per flag, capped at 12 | `tests/Dmc.Tests/Size/SizeCommandContract.cs` | FR-013, FR-013a, SC-007, SC-008 |
| Every failure is one line, no stack trace, writes nothing | `tests/Dmc.Tests/TokenCost/ErrorPathTests.cs` | FR-013 |
| No byte-order mark, LF line ends | `tests/Dmc.Tests/TokenCost/OutputBytesTests.cs` | FR-013 |
| The command writes nothing (a hash of the fixture before and after) | `SizeCommandContract.cs` | FR-012 |
| The prompts name `dmc size` as a real subcommand, allowed in play and in the repository; resume step 2 still holds every marker `ReadSet` relies on; no size rule in `kit/` is in lines or words | `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` | FR-002, FR-014, SC-006 |

## 2. By hand, on a scratch campaign

1. Make a scratch DMC folder from the build (outside this repository) and start a campaign in it, as
   `.docs/development.md` § Building describes, or copy a fixture from the tests.
2. Run the full report:

   ```
   artifacts/bin/dmc size --folder <scratch folder>
   ```

   **Expect:** the active campaign's files in resume order, each with its tokens, the total, the largest sections,
   and `nothing to flag` on a new campaign.
3. Run the play form:

   ```
   artifacts/bin/dmc size --folder <scratch folder> --check
   ```

   **Expect:** no output, exit code 0.
4. Grow `story/dm-notes.md` past 80,000 bytes (paste text), add a second `Last played` line and an Opposition row
   `| Goblins | 0 / 4 | ... |`, then run step 3 again.

   **Expect:** four lines (near the Read cap, over its 10k budget, the Last played log, the finished Opposition row)
   and exit code 0.
5. Run a failure: `artifacts/bin/dmc size --folder <scratch folder> no-such-campaign`.

   **Expect:** one line, `dmc size: no campaign no-such-campaign in adventures/`, exit code 1.

## 3. On the long campaign (the maintainer's)

Read-only: the command writes nothing.

```
dmc size --folder D:\Claude\dmc rorns-landing
```

**Expect** (SC-003, SC-004, SC-008), against the measurements in [research.md](research.md):

- `story/dm-notes.md` about 20.4k, flagged near the Read cap and over its 10k budget, with Opposition and the stages
  among its largest sections and the Mix tracker flagged.
- `story/canon.md` about 15.5k against its 19k limit, `read whole`.
- The full report about 40 lines or fewer.

## 4. In play

After `dmc update` brings the new kit to a DMC folder:

1. **Resume** a campaign with nothing flagged. **Expect:** DMC says nothing about size, and reads canon whole.
2. **Resume** the long campaign. **Expect:** one line out of character that `dm-notes.md` is near the Read cap, and
   nothing about budgets.
3. **Full save** on the long campaign. **Expect:** it trims, runs `dmc size --check` again, and either it prints
   nothing or the save's report names each file or flag still standing and why it stays (SC-010).
4. **Ask** DMC "are your files too big?". **Expect:** it runs the full report and answers from it (SC-002).
