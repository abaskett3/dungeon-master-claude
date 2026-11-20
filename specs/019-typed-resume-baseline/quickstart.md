# Quickstart: check the typed-resume baseline

Commands are for Git Bash on Windows from the repository root. Use your own runtime identifier in place of `win-x64`.

## 1. Tests

```
dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~ContextCheck"
dotnet test tests/Dmc.Tests
```

Expected: every test passes. That includes the new tests for each row of
[contracts/context-check-baseline.md](contracts/context-check-baseline.md), and the present baseline, break-line and
sheet-line tests with no change to what they expect (SC-003).

## 2. The real log of the defect (SC-001)

Copy the log out of the Claude Code folder first. It quotes play, so it stays outside the repository.

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
mkdir -p "$CLAUDE_JOB_DIR/tmp/019" && cp ~/.claude/projects/D--Claude-dmc/082db04e-3cf5-4c9f-ba48-7f2aad013330.jsonl "$CLAUDE_JOB_DIR/tmp/019/typed.jsonl"
```

Cut the copy just before the player message that got the early break line. That is the fifth player message after
the resume, the first at about 154,000 tokens. Then run:

```
echo "{\"transcript_path\":\"$CLAUDE_JOB_DIR/tmp/019/cut.jsonl\"}" | artifacts/bin/dmc context-check
```

Expected: no `Context …k is large` line. On `main` the same input prints one. A test that reads a temporary log
can print the reading. It should show a baseline equal to the context of the last call before line 216, the first
play message.

## 3. The first typed message after `/clear` (SC-002)

Make a scratch DMC folder with a campaign whose Scene block's Present line names people with sheets (as in
`SheetReminderContract`). Then run the hook with `prompt` "Resume campaign." and a `transcript_path` whose log holds
only a `/clear` echo.

Expected: no `Sheets of …` header. With the same input and an empty `transcript_path`, the sheet lines print as
today.

## 4. Speed (SC-004)

Build a release build: `dotnet publish src/Dmc.Cli -r win-x64 -p:PublishAot=true -o artifacts/bin-aot`. This needs
the C++ build tools. If they are missing, CI's release build is the check, and the PR says so.

Time 20 runs each of the following, on this branch and on `main`:

- no log;
- the largest main play log in `~/.claude/projects/D--Claude-dmc/`, about 7 MB;
- a synthetic 18 MB log, made by appending that log's records after a typed-resume start.

```
time (for i in $(seq 20); do echo "{\"transcript_path\":\"<log>\"}" | artifacts/bin-aot/dmc context-check >/dev/null; done)
```

Expected: the time over the no-log run stays at about 20 ms for 18 MB. The present figure is 72 ms against 51 ms
(`.docs/dmc-reference.md:827`). The branch is no more than a few ms slower than `main` on each log. Record the
numbers in the PR. Update `.docs/dmc-reference.md` if they change.
