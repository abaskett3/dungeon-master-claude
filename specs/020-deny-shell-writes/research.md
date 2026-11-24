# Research: Deny the usual shell writes and the SendFeedback tool in play

Checked on 2026-11-21 on branch `020-deny-shell-writes` (from `origin/main` at `24efdcb`), Claude Code 2.1.317,
PowerShell 7.7.0. The headless runs were in folders under the session's scratchpad, never in a DMC folder or this
repository; each had only a `.claude/settings.json`.

## R1. A tool denied by its bare name leaves the model's tool list

- **Decision:** deny `SendFeedback` by its bare name in `permissions.deny`, as the kit already denies `NotebookEdit`
  and `ReportFindings` (`kit/settings.json`).
- **Evidence:** `claude -p ... --output-format stream-json --verbose` prints the session's tool list in its first
  record. With no rules it listed 54 tools, `NotebookEdit` and `WebSearch` among them. With
  `"deny": ["SendFeedback", "NotebookEdit", "WebSearch"]` it listed 52, and neither of the two. So Claude Code drops
  a tool denied by its bare name from the list the model gets, and the model pays nothing for its definition.
- **Not verified:** `SendFeedback` itself is not in a headless session's list (54 tools, with or without the rule),
  so the run cannot show it leaving. It is in an interactive session's list (the play logs `108c0877` and
  `2163a49a` call it). That the same rule drops it there follows from the general behaviour above, and is checked by
  hand in quickstart Q3. The size, about 5,500 characters, is the slip analysis's figure
  (`D:\Claude\dmc-slip-analysis\plan.md`), not measured again here.
- **Alternatives considered:** none. A hook or a prompt line cannot remove a tool definition.

## R2. The command rules refuse a real call in the `auto` mode

- **Decision:** the rules of FR-001 and FR-002 as written in the issue.
- **Evidence:** two headless runs on Haiku in `auto` mode (the tool list's `permissionMode`), each told to run
  `sed -i 's/a/b/' f.txt` and `sed -e 's/a/c/' -i f.txt` in Bash and `Set-Content g.txt hello` in PowerShell:
  - with `Bash(sed -i*)`, `Bash(sed * -i*)` and `PowerShell(Set-Content *)` denied, all three were refused, `f.txt`
    stayed `aaa` and no `g.txt` was made;
  - with no rules (the control), all three ran, `f.txt` became `bca` and `g.txt` held `hello`.
  So the `auto` mode runs these writes with no prompt, and the deny rules stop them. The delete rules of PR #38 were
  never tried live (`.docs/decisions.md:175`); these are.
- **Alternatives considered:** leaving `auto` mode (rejected in the issue: 274 permission prompts in 53 of 114 logs,
  also on the player's phone); a rule per file (`Edit` rules do not cover shell commands).

## R3. The PowerShell aliases and `Tee-Object` stay out

- **Decision:** not denied (spec Clarifications, 2026-11-21, answer B). Recorded among the limits.
- **Evidence:** `Get-Alias` in PowerShell 7.7.0 gives `ac` → `Add-Content` and `tee` → `Tee-Object`; `sc` is no
  longer an alias. Claude Code canonicalizes PowerShell aliases (`.docs/decisions.md:174`), so `ac x` may already be
  matched as `Add-Content x`; not verified, and not relied on.

## R4. No kit step needs a denied command

- **Decision:** no kit prompt changes besides `dmc-new-campaign` step 1.
- **Evidence:** `grep -rnE '\b(python3?|py|node|perl|tee|sed|Set-Content|Add-Content|Out-File)\b' kit --include=*.md`
  finds nothing. The only kit step that writes a file with no tool named is `dmc-new-campaign` step 1
  (`kit/skills/dmc-new-campaign/SKILL.md:14`, "write the folder name as the only line of
  `adventures/.active-campaign`"); log `9eec3bdf` shows it done with `Set-Content`. The other skills only read that
  file (`grep -rn active-campaign kit`). `Edit(adventures/**)` is on the allow list, and Claude Code applies `Edit`
  rules to Write too (`.docs/decisions.md:471`), so Write needs no prompt there.
- **The test:** a new check reads every code span and code-block line of the kit prompts and asserts no deny rule of
  this spec takes it, so a later step that names such a command fails the build, not a play session.

## R5. Hooks and `dmc` are not affected

- **Decision:** nothing to change.
- **Evidence:** the hooks of `kit/settings.json` run as commands of the harness, not as the model's tool calls, and
  use `dmc`, `echo` and `printf` only. DMC runs `dmc` through `Bash(dmc *)` and `PowerShell(dmc *)`; no new rule
  starts with `dmc`, and the existing test
  `EveryDmcCommandInThePromptsIsARealSubcommandAndTheOnesDmcRunsNeedNoPermissionPrompt`
  (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs:133`) keeps checking that.

## R6. The test follows the delete rules' test

- **Decision:** a rule list `ShellWriteRules` and a test `TheKitSettingsDenyShellWritesAndSendFeedback`, beside
  `ShellDeleteRules` and `TheKitSettingsDenyShellDeletes` (`ToolkitTests.cs:548-601`). It uses the same helpers:
  `Denied`, `PatternRegex` (`ToolkitTests.cs:77`) and `Settings(DevSettings)`.
- **Evidence:** `TheKitSettingsKeepEverythingPlayNeeds` (`ToolkitTests.cs:498`) already fails if a bare-name deny
  takes a tool play needs; `SendFeedback` is not in its list, so it passes. `NamesACommand` treats every new rule as
  one command, so none trips the tool guard.

## R7. Why no migration and no new step

- No campaign file, template or rule for a file's shape changes (Development 2).
- `dmc update` and `dmc refresh` rewrite `.claude/settings.json` from `kit/settings.json` and the skills from
  `kit/skills/` (`.docs/dmc-reference.md:935`), so an existing DMC folder gets the rules with no new step
  (Development 3). The change reaches players in the next release, because `kit/` is embedded in `dmc`.
