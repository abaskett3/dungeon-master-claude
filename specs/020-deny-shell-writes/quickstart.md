# Quickstart: check the change

## Q1. The toolkit tests

```
dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~ToolkitTests"
```

Expected: `TheKitSettingsDenyShellWritesAndSendFeedback` and the new kit-prompt check pass, and every present
toolkit test passes unchanged (SC-005).

## Q2. The rules in a live session (as research R2)

In an empty scratch folder with a `.claude/settings.json` copied from `kit/settings.json`, and a file `f.txt` holding
`aaa`:

```
claude -p "Run exactly these, each as its own tool call, and report whether each ran: (1) Bash: sed -i 's/a/b/' f.txt (2) PowerShell: Set-Content g.txt hello (3) Bash: sed -n 1p f.txt" --model haiku --max-turns 6 < /dev/null
```

Expected: (1) and (2) refused, `f.txt` still `aaa`, no `g.txt`; (3) runs and prints `aaa`.

## Q3. `SendFeedback` in an interactive play session (by hand)

Publish and write a scratch DMC folder (`.docs/development.md` § Building, "Trying a change"), start `dmc play` there
and run `/context`. Expected: `SendFeedback` is not among the tools. If it is listed, the rule still refuses the call,
and the PR drops the token saving from its claims (spec SC-003).

## Q4. A new campaign starts

In the same scratch folder, start a new campaign. Expected: DMC writes `adventures/.active-campaign` with Write, and
no command is refused.
