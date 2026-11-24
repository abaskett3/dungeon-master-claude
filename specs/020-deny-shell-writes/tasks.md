---

description: "Task list for spec 020, deny the usual shell writes and the SendFeedback tool in play"
---

# Tasks: Deny the usual shell writes and the SendFeedback tool in play

**Input**: Design documents from `specs/020-deny-shell-writes/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/kit-settings-deny.md, quickstart.md

**Tests**: Requested by the spec (FR-006, FR-007). Each story's test is written first and fails before its change.

**Organization**: Tasks are grouped by user story. All three stories are P1 and touch `kit/settings.json` or
`ToolkitTests.cs`, so they run in order, not in parallel.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)

---

## Phase 1: Setup

**Purpose**: A known starting point.

- [X] T001 Run `dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~ToolkitTests"` on the branch before any
  change and note the pass count, so SC-005 (every present test passes unchanged) can be checked at the end.

---

## Phase 2: Foundational

**Purpose**: The rule list every story's test reads.

- [X] T002 In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, after `TheKitSettingsDenyShellDeletes`, add
  `static readonly string[] ShellWriteRules` holding, in the contract's order
  (`specs/020-deny-shell-writes/contracts/kit-settings-deny.md`): `Bash(sed -i*)`, `Bash(sed * -i*)`,
  `Bash(python *)`, `Bash(python3 *)`, `Bash(py *)`, `Bash(node *)`, `Bash(perl *)`, `Bash(tee *)`,
  `PowerShell(Set-Content *)`, `PowerShell(Add-Content *)`, `PowerShell(Out-File *)`, `PowerShell(python *)`,
  `PowerShell(py *)`, `SendFeedback`. Its XML doc comment says: the shell writes the `auto` mode invites and the
  feedback tool, issue #245; the usual forms only; not a redirect, `bash -c`, a full path, `cmd /c`,
  `powershell -c`, `cp`, or PowerShell's `Tee-Object`, `tee` and `ac` (spec Clarifications 2026-11-21).

**Checkpoint**: the list compiles; no test uses it yet.

---

## Phase 3: User Story 1 - A correction changes the sheet in a way the player can see (Priority: P1)

**Goal**: Claude Code refuses the usual shell writes in play, so DMC changes campaign files with Edit.

**Independent Test**: the new test's command checks pass against `kit/settings.json`; quickstart Q2 refuses
`sed -i` and `Set-Content` in a live session.

- [X] T003 [US1] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, add `[TestMethod] public void
  TheKitSettingsDenyShellWritesAndSendFeedback()` with an XML doc comment (spec 020 FR-006, issue #245). It reads the
  kit's deny list as `TheKitSettingsDenyShellDeletes` does and asserts: every rule of `ShellWriteRules` is in it;
  `Denied("Bash", c)` for each Bash "Takes" command of the contract (`sed -i 's/a/b/' f`, `sed -i.bak 's/a/b/' f`,
  `sed -i -e x f`, `sed -e x -i f`, `sed -E -i 's/a/b/' f`, `python fix.py`, `python -c "x"`, `python`,
  `python3 fix.py`, `py fix.py`, `py -c "x"`, `node x.js`, `node -e "x"`, `perl -pi -e 's/a/b/' f`, `tee f`,
  `tee -a f`); `Denied("PowerShell", c)` for `Set-Content f x`, `set-content -Path f -Value x`, `Add-Content f x`,
  `Out-File f`, `python fix.py`, `py fix.py`; `!Denied("Bash", c)` for `sed -n 1,20p f`, `sed 's/a/b/' f`,
  `sed -n 1p f`, `pythonw x`, `pytest`, `python3x`, `pyright`, `nodemon x`, `perldoc x`, `teed x`, `cat a.md`,
  `ls adventures`, `dmc roll 1d20`; `!Denied("PowerShell", c)` for `Get-Content f`, `Set-ContentX f`, `Out-Host`,
  `pytest`, `Get-ChildItem adventures`, `dmc roll 1d20`. Run it: it fails on the missing rules.
- [X] T004 [US1] In `kit/settings.json`, append to `permissions.deny`, right after `"PowerShell(git clean *)"`, the
  13 command rules of T002 in the same order. Keep the file's two-space indent.
- [X] T005 [US1] Run the T003 test; every command assertion passes (the `SendFeedback` part still fails until T007).

**Checkpoint**: the shell writes are denied.

---

## Phase 4: User Story 2 - DMC cannot draft Claude Code feedback in play (Priority: P1)

**Goal**: `SendFeedback` is not offered to DMC in play, and toolkit work keeps every rule off.

**Independent Test**: the T003 test passes in full; quickstart Q3 shows no `SendFeedback` in `/context`.

- [X] T006 [US2] In `TheKitSettingsDenyShellWritesAndSendFeedback` (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`),
  assert that no rule of `ShellWriteRules` is in `Settings(DevSettings)`'s deny list (as
  `TheKitSettingsDenyShellDeletes` ends), with the message `.claude/settings.json applies to toolkit work and must
  not deny {rule}`.
- [X] T007 [US2] In `kit/settings.json`, add `"SendFeedback"` to `permissions.deny` right after
  `"ShareOnboardingGuide"`.
- [X] T008 [US2] Run the T003 test and `TheKitSettingsKeepEverythingPlayNeeds`; both pass.

**Checkpoint**: the kit settings hold all 14 rules of the contract.

---

## Phase 5: User Story 3 - Starting a campaign still works (Priority: P1)

**Goal**: no kit step names a command the rules deny, and `dmc-new-campaign` step 1 names Write.

**Independent Test**: the new prompt check passes; quickstart Q4 starts a campaign with no refusal.

- [X] T009 [US3] In `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`, add `[TestMethod] public void
  NoKitPromptNamesACommandTheShellWriteRulesDeny()` with an XML doc comment (spec 020 FR-007): for each file of
  `KitPrompts()`, take every code span (text between single backticks) and every line inside a fenced code block,
  trimmed; for each that starts with a letter, assert that no `Bash(...)` or `PowerShell(...)` rule of
  `ShellWriteRules` matches it (use `PatternRegex`), with the file and the text in the message. Run it; it passes
  (research R4). If it fails on text that is not a command DMC runs, let through only that exact text, name it in
  the comment, and record it in `specs/020-deny-shell-writes/research.md` R4; never skip a whole file or folder.
- [X] T010 [US3] In `kit/skills/dmc-new-campaign/SKILL.md:14`, change "then write the folder name as the only line
  of `adventures/.active-campaign`" to "then, with the Write tool, write the folder name as the only line of
  `adventures/.active-campaign`". No other line changes.

**Checkpoint**: all three stories are done.

---

## Phase 6: Polish and documents

- [X] T011 In `.docs/decisions.md`, add at the end the entry "## Deny the usual shell writes and the SendFeedback
  tool in play (issue #245, spec 020)" with **Date** 2026-11-21; **Decision** (the 13 command rules and
  `SendFeedback` in `kit/settings.json`, `dmc-new-campaign` step 1 names Write, the `auto` mode stays, the two tests);
  **Reason** (the `auto` text in 9 of 114 logs, about 45 unasked shell writes in 5 logs, 43 matched; 2 `SendFeedback`
  calls mid-correction with drafts holding the player's words; research R1 and R2, the first live check of deny
  rules); **Rejected** (leaving `auto`: 274 prompts in 53 of 114 logs; denying the PowerShell aliases, spec
  Clarifications); **Limits** (spec Edge Cases; `SendFeedback` leaving the tool list is inferred, research R1);
  **Source** (issue #245, Phase 0 of #238; `specs/020-deny-shell-writes/`).
- [X] T012 [P] In `.docs/user-guide.md:58`, change "block shell deletes (`rm`, `Remove-Item` and their aliases)" to
  "block shell deletes (`rm`, `Remove-Item` and their aliases) and the usual shell writes (`sed -i`, scripts,
  `Set-Content`), so DMC changes your files in a way you can see".
- [X] T013 [P] In `.docs/architecture.md:15`, change "the deny rules (shell deletes, edits of the generated files,"
  to "the deny rules (shell deletes, the usual shell writes, Claude Code's `SendFeedback`, edits of the generated
  files,".
- [X] T014 [P] In `.docs/roadmap.md`, add a row after the spec 019 row: "Deny the usual shell writes and the
  `SendFeedback` tool, issue #245, spec 020 (slip plan #238, Phase 0) | Done (PR #<number>), for the next release |
  Kit settings and one skill line; reaches a DMC folder with `dmc update`. See decisions.md." Set the status line's
  date to the day the PR is opened. Right after T018 opens the PR, a commit replaces `<number>` with its number (as
  spec 019's `4cc2477` did).
- [X] T015 Run `dotnet test tests/Dmc.Tests`; every test passes, and the toolkit pass count is T001's plus 2
  (SC-005).
- [X] T016 Run quickstart Q2 in a scratch folder under the session's scratchpad with `kit/settings.json` as its
  `.claude/settings.json`; `sed -i` and `Set-Content` are refused and `sed -n` runs. Record the result in the PR.
- [X] T017 Ask the maintainer to run quickstart Q3 and Q4 in a scratch DMC folder (they need an interactive session).
  If `SendFeedback` is still listed in `/context`, drop the token saving from the PR and from the T011 entry.
  Done 2026-11-22 in `D:\Claude\test-campaign`, with the dev build's `dmc play`. Q3: `/context` doesn't list tools one
  by one (System tools 10.8k), and asked directly, the session said `SendFeedback` isn't in its tool list. Q4: DMC
  made `adventures/test-campaign` and set `.active-campaign` with Edit (the file already held a name), and none of
  the new rules refused anything. One compound Bash command (`cp -r templates/campaign … && dmc campaign … ; cat … ;
  ls …`) was refused, and DMC did the same in PowerShell. A headless replay refuses it under `main`'s kit settings
  too (`subcommandResults`), so the refusal was there before this change.
- [X] T018 Run `/speckit-converge` until a pass adds no tasks; then open the PR with `Closes #245` and `Part of #239`
  on their own lines, and check its body with `gh pr view <n> --json body`.

---

## Dependencies and order

- T001 → T002 → US1 (T003 to T005) → US2 (T006 to T008) → US3 (T009, T010) → Polish (T011 to T018).
- US1 and US2 both edit `kit/settings.json` and the same test method, so they are sequential. US3 touches a
  different test and the skill, and could follow T002 directly.
- T012, T013 and T014 edit different documents and can run in parallel.

## Parallel example

```text
After T011: T012 (.docs/user-guide.md), T013 (.docs/architecture.md), T014 (.docs/roadmap.md) at once.
```

## Implementation strategy

One PR, as the plan says. US1 alone is the MVP: it stops the shell writes that the issue names. US2 and US3 are
small and ship with it; nothing is released in parts.

---

## Phase 7: Convergence

- [X] T019 In `.docs/roadmap.md`, set the status line's date ("Status is as of 2026-11-21") to the day the PR is
  opened, 2026-11-23, per plan: roadmap status date and T014 (partial)
- [X] T020 In `.docs/decisions.md`, in the Limits of the spec 020 entry, replace "checked by hand in `/context`
  (spec 020 quickstart Q3)" with the result: on 2026-11-22 an interactive play session, asked directly, said
  `SendFeedback` was not in its tool list (`/context` doesn't list tools one by one), per FR-008, SC-003 and T017
  (partial)

---

## Phase 8: Code review fixes (PR #272)

The built-in code review found 8 possible problems; each was checked by hand, two of them with a live headless
replay under the kit settings in `auto` mode. Applied with the maintainer's yes (2026-11-24):

- [X] T021 Deny the bundled and long sed forms (`Bash(sed -Ei*)`, `Bash(sed -ri*)`, `Bash(sed --in-place*)`,
  `Bash(sed * --in-place*)`) in `kit/settings.json`, with test cases, per FR-001: `sed -Ei` ran live with the first
  two rules alone.
- [X] T022 Deny `PowerShell(python3 *)`, `PowerShell(node *)` and `PowerShell(perl *)`, with test cases, per FR-002:
  a PowerShell `node -e` write ran live.
- [X] T023 Name the forms still not caught (`sed -ni`, `awk -i inplace`) and the one read refused (` -i` after sed's
  first word) in the spec's Edge Cases, the contract and the `.docs/decisions.md` Limits, per FR-008.
- [X] T024 Add the shell write rules, `SendFeedback` and the prompt check to the Toolkit row of `.docs/development.md`.
- [X] T025 Split each code span and code-block line into the commands of its chain or pipeline before matching, in
  `NoKitPromptNamesACommandTheShellWriteRulesDeny`, per FR-007.
- Not changed: a pipeline ending in `Set-Content` or `Out-File` was said to get through, but Claude Code refused both
  live (it checks a pipeline part by part; now in the Limits). A hook in place of rules matched by name belongs to
  #257. Merging the setup of the new test with `TheKitSettingsDenyShellDeletes` would change a present test (SC-005).
- After the fixes the live replay refuses `sed -Ei`, the PowerShell `node -e` write and both pipelines.