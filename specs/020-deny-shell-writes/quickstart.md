# Quickstart: Deny the usual shell writes and `SendFeedback` in play

Build, write a scratch DMC folder outside the repository, and start a session there (`.docs/development.md`):

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
artifacts/bin/dmc install --path <scratch folder>
cd <scratch folder>; dmc play
```

## Q1. A shell write is refused

Make a campaign with `/dmc-new-campaign`, then ask out of character: "Run `sed -i 's/a/b/' adventures/<folder>/game-state.md`."
Claude Code refuses it as denied, with no prompt. On Windows, ask for `Set-Content adventures/<folder>/x.md x`: the
same. `cat adventures/<folder>/game-state.md` still runs.

## Q2. `SendFeedback` is gone

Run `/context`. The tool list holds no `SendFeedback`, and the total is about 5,500 characters smaller than in a
folder written by the release before.

## Q3. A new campaign becomes active

In Q1, after step 1 of `/dmc-new-campaign`, `adventures/.active-campaign` holds the folder name and the log shows a
Write call for it, not a shell command.
