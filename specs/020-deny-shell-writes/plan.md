# Implementation Plan: Deny the usual shell writes and the SendFeedback tool in play

**Branch**: `020-deny-shell-writes` | **Date**: 2026-11-21 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/020-deny-shell-writes/spec.md` (issue #245, Phase 0 of #238)

## Summary

In the `auto` permission mode, Claude Code tells the model it may change files with `sed`, heredocs or short
scripts, and DMC did that to campaign files about 45 times in 5 logs. The kit also leaves Claude Code's
`SendFeedback` tool on, which DMC called twice in play. The change:

- adds 13 command rules and `SendFeedback` to the deny list of `kit/settings.json`;
- makes `dmc-new-campaign` step 1 name the Write tool for `adventures/.active-campaign`;
- adds a toolkit test for the rules and a check that no kit prompt names a command they deny.

Two headless runs showed that the `auto` mode runs `sed -i` and `Set-Content` with no prompt, and that the rules
refuse them (research R2). A third showed that a tool denied by its bare name leaves the model's tool list (R1).

## Technical Context

**Language/Version**: JSON settings, Markdown skill text, C# tests on .NET 10 (`tests/Dmc.Tests`)

**Primary Dependencies**: Claude Code's permission rules (2.1.317 checked); no new package

**Storage**: None

**Testing**: MSTest, `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`; a headless check by hand (quickstart Q2, Q3)

**Target Platform**: A DMC folder on Windows and Linux, in Bash and PowerShell

**Project Type**: Kit change (settings and one skill) with a toolkit test

**Performance Goals**: N/A

**Constraints**: No kit step and no `dmc` command DMC runs may be refused; toolkit work in this repository keeps
every command (none of the rules in `.claude/settings.json`)

**Scale/Scope**: `kit/settings.json`, `kit/skills/dmc-new-campaign/SKILL.md`, `ToolkitTests.cs` and four documents

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| Core 1 Consistency | Names which files hold new state | Pass. No new state. Changes to campaign files go through Edit, which the player sees |
| Core 2 Fun | Says what the player gains | Pass. A correction's change to a sheet is shown as a diff; no Claude Code feedback draft holds the player's words |
| Core 3 Low cost | Cost counted as Development 1 says | Pass. The `SendFeedback` definition (about 5,500 characters, slip analysis) leaves each play session's context (R1; not measured for that tool, quickstart Q3) |
| Development 1 Count the cost | Lines added to `kit/dm-rules.md` and each skill | Pass. 0 lines to `kit/dm-rules.md`. `dmc-new-campaign`: one phrase in an existing line (about 22 bytes, paid only at a campaign's start). The resume's reads do not change, so no `dmc size` result |
| Development 2 No change breaks a campaign | Names the migration, or why none | Pass. No campaign file or shape changes (R7) |
| Development 3 One step | Install or update gains a step | Pass. `dmc update` rewrites the settings and the skill (R7) |
| Development 4 Grounded | Every claim cites a file or says "unverified" | Pass. See research.md; R1 for `SendFeedback` itself and R3's alias matching are marked not verified |
| Development 6 One home | Names the home of each rule | Pass. The deny rules' home is `kit/settings.json`; the reason and limits go in `.docs/decisions.md`; the rule for how DMC changes campaign files stays at `kit/dm-rules.md:63`, unchanged |
| Development 7 Tests skip locally | A test that needs a tool calls `TestEnv.Need` | Pass. The new test reads files only |
| Dungeon Master 1 to 4 | — | Not touched |
| Documentation 1 to 3 | Concise; player steps; simple developer English | Pass. The user guide line names the writes in player terms |
| Documentation 4 | The PR names the documents it changed | Planned. See Documents below |
| Build: AOT, one-line errors, small output | — | Pass. No code in `src/` changes |

No violations. Complexity Tracking stays empty.

**Re-check after Phase 1**: unchanged. The design adds settings entries, one phrase and one test, and no file.

## Design

1. **`kit/settings.json`**: append to `permissions.deny`, after the shell delete rules, the 13 command rules of
   [contracts/kit-settings-deny.md](contracts/kit-settings-deny.md) in its order, then `SendFeedback` after
   `ShareOnboardingGuide` with the other bare tool names.
2. **`kit/skills/dmc-new-campaign/SKILL.md:14`**: "then write the folder name as the only line of
   `adventures/.active-campaign`" becomes "then, with the Write tool, write the folder name as the only line of
   `adventures/.active-campaign`".
3. **`tests/Dmc.Tests/Toolkit/ToolkitTests.cs`**, beside `ShellDeleteRules` (R6):
   - `ShellWriteRules`: the 13 command rules and `SendFeedback`, with an XML doc comment that names issue #245 and
     the limits.
   - `TheKitSettingsDenyShellWritesAndSendFeedback`: each rule is in the kit's deny list; the "Takes" commands of the
     contract are `Denied` in their shell; the "Does not take" commands and `cat a.md`, `ls adventures`,
     `dmc roll 1d20`, `Get-ChildItem adventures` are not; none of the rules is in `.claude/settings.json`.
   - `NoKitPromptNamesACommandTheShellWriteRulesDeny` (FR-007): every code span and code-block line of
     `KitPrompts()` is not `Denied` in either shell by a rule of `ShellWriteRules`. With a doc comment.
4. **Documents** (FR-008):
   - `.docs/decisions.md`: a new entry, "Deny the usual shell writes and the SendFeedback tool in play (issue #245,
     spec 020)": the decision, the reason (the logs, the `auto` text, R2), the limits (spec Edge Cases, R3) and the
     source.
   - `.docs/user-guide.md:58`: "block shell deletes (`rm`, `Remove-Item` and their aliases)" becomes "block shell
     deletes (`rm`, `Remove-Item` and their aliases) and the usual shell writes (`sed -i`, scripts, `Set-Content`),
     so DMC changes your files in a way you can see".
   - `.docs/architecture.md:15`: the deny rules' list gains "the usual shell writes, Claude Code's `SendFeedback`".
   - `.docs/roadmap.md`: a row for issue #245, spec 020, and the date of the status line.

## Project Structure

### Documentation (this feature)

```text
specs/020-deny-shell-writes/
├── spec.md
├── plan.md              # This file
├── research.md          # R1 to R7
├── data-model.md        # The deny rule
├── quickstart.md        # Q1 to Q4
├── contracts/
│   └── kit-settings-deny.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
kit/
├── settings.json                        # 14 deny entries added
└── skills/dmc-new-campaign/SKILL.md     # step 1 names Write
tests/Dmc.Tests/Toolkit/ToolkitTests.cs  # ShellWriteRules and two tests
.docs/
├── decisions.md
├── user-guide.md
├── architecture.md
└── roadmap.md
```

**Structure Decision**: no new file outside `specs/`; each change goes in the file that already holds its kind of
rule.

## Complexity Tracking

None.
