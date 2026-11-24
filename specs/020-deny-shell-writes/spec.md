# Feature Specification: Deny the usual shell writes and the SendFeedback tool in play

**Feature Branch**: `020-deny-shell-writes`

**Created**: 2026-11-21

**Status**: Draft

**Input**: User description: "Issue #245 (Slips 0.2, part of #239, plan #238): deny the usual shell writes (sed -i,
python/python3/py/node/perl, tee, PowerShell Set-Content/Add-Content/Out-File/python/py) and Claude Code's
SendFeedback tool in kit/settings.json; make dmc-new-campaign step 1 name the Write tool for adventures/.active-campaign;
add a toolkit test (like TheKitSettingsDenyShellDeletes) that the kit settings hold these deny rules. Keep the auto
permission mode."

## Background

`kit/dm-rules.md:63` tells DMC to read campaign files with the Read tool, change them with Edit, and use no shell
commands or scripts on them. Two things from outside the kit work against that rule (issue #245):

- **The `auto` permission mode.** In 9 of 114 play logs, Claude Code's text for this mode tells the model it may "read
  files with cat, head, or sed -n" and make changes "with sed, heredocs, or short scripts instead of the dedicated Read,
  Edit, or Write tools".
- **Claude Code's `SendFeedback` tool.** Its description tells the model to draft feedback about Claude Code when the
  user is frustrated, and not to announce it. The kit does not deny it.

### What the logs show (the slip analysis of 2026-11-18, `D:\Claude\dmc-slip-analysis\plan.md` § 0.3)

- DMC changed campaign files with shell commands about 45 times in 5 logs, with no request from the player; about 20
  of those were story files. 4 of the 5 logs carry the `auto` text; of the 105 logs without it, 1 has such a change.
  The rules of this spec match 43 of the 45.
- DMC called `SendFeedback` twice in play (logs `108c0877`, `2163a49a`), in the middle of a correction. Each call keeps
  a draft on the player's computer that holds the player's words and campaign names. Nothing shows a draft was sent.
- The tool's definition adds about 5,500 characters to every play session.
- In log `9eec3bdf` DMC wrote `adventures/.active-campaign` with `Set-Content` during `dmc-new-campaign` step 1, which
  names no tool for that step.

The precedent is issue #36 (PR #38): deny rules for the shell deletes and moves, tested by
`TheKitSettingsDenyShellDeletes` (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs:563`), with their limits recorded in
`.docs/decisions.md:173-175`.

## Clarifications

### Session 2026-11-21

- Q: Should the PowerShell rules also deny the write cmdlets' aliases and `Tee-Object` (in PowerShell 7.7, `ac` is
  `Add-Content` and `tee` is `Tee-Object`; `sc` is no longer an alias), as the delete rules of PR #38 list every
  alias? → A: No. Keep the issue's list; the aliases and `Tee-Object` are recorded as limits (Edge Cases, FR-008).
- Q: Should a test check that the kit never sets the permission mode or `remoteControlAtStartup` (FR-004)? → A: No;
  the maintainer does not care about that here. FR-004 is removed and nothing checks it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A correction changes the sheet in a way the player can see (Priority: P1)

The player corrects a fact. DMC goes to change a character sheet and reaches for `sed -i` or a short Python script, as
the `auto` text invites. Claude Code refuses the command, and DMC makes the change with Edit, which the player sees as
a diff.

**Why this priority**: an unseen shell change to a story file is the slip the issue names, and the player cannot check
or undo what they did not see.

**Independent Test**: a toolkit test feeds the rules the commands from the logs (`sed -i ...`, `sed -e x -i file`,
`python script.py`, `py -c ...`, `node x.js`, `perl -pi -e ...`, `tee file`, `Set-Content`, `Add-Content`, `Out-File`)
and checks each is denied in its shell.

**Acceptance Scenarios**:

1. **Given** the kit's settings, **When** DMC runs `sed -i 's/a/b/' adventures/x/characters/hala.md` in Bash,
   **Then** Claude Code refuses it.
2. **Given** the kit's settings, **When** DMC runs `Set-Content adventures/x/session-log.md ...` or
   `python fix.py` in PowerShell, **Then** Claude Code refuses it.
3. **Given** the kit's settings, **When** DMC runs a command play needs (`dmc roll 1d20`, `ls adventures`,
   `sed -n 1,20p file`, `cat a.md`, `Get-ChildItem adventures`), **Then** it is not refused.

---

### User Story 2 - DMC cannot draft Claude Code feedback in play (Priority: P1)

In play DMC has no `SendFeedback` tool: it is not in its tool list, so it is neither called mid-correction nor paid for
in each session's context.

**Why this priority**: the drafts hold the player's words and campaign names, and the tool costs about 5,500
characters in every session whether it is called or not.

**Independent Test**: a toolkit test checks that `kit/settings.json` denies `SendFeedback` by its bare name; a headless
session in a scratch DMC folder shows whether the tool is gone from the model's tool list (see Assumptions).

**Acceptance Scenarios**:

1. **Given** a DMC folder written from the kit, **When** a play session starts, **Then** `SendFeedback` cannot be
   called.
2. **Given** a developer session in this repository, **When** it starts, **Then** `SendFeedback` is not denied there:
   `.claude/settings.json` holds none of this spec's rules.

---

### User Story 3 - Starting a campaign still works (Priority: P1)

The player starts a new campaign. Step 1 of `dmc-new-campaign` tells DMC to write `adventures/.active-campaign` with
the Write tool, so the new deny rules do not refuse the step.

**Why this priority**: one log already did this step with `Set-Content`; with the rule in place, an unnamed tool would
turn a refusal into a stalled campaign start.

**Independent Test**: read `kit/skills/dmc-new-campaign/SKILL.md` step 1: it names the Write tool for that file. A test
checks that no kit prompt names a command these rules deny.

**Acceptance Scenarios**:

1. **Given** the new skill text, **When** DMC runs step 1, **Then** it writes `adventures/.active-campaign` with Write.
2. **Given** every kit prompt, **When** the test reads them, **Then** none tells DMC to run a denied command.

### Edge Cases

- `sed -n` (a read) and a bare `sed 's/a/b/' file` that prints to the screen are not denied; only `-i` (in place) is.
  `sed -i*` also covers `-i.bak` and `-i -e`; `sed * -i*` covers `-i` after another flag.
- Commands that only start with the same letters (`pytest`, `nodemon`, `teed`, `pyright`, `Set-ContentX`) are not
  denied: each rule has a space before its `*`.
- A redirect (`> file`), `bash -c '...'`, a full program path (`/usr/bin/python3`), `cmd /c`, `powershell -c`, `cp`,
  `awk -i inplace`, `-i` bundled with a flag other than `-E` or `-r` (`sed -ni`), and in PowerShell `Tee-Object`, its
  alias `tee` and the alias `ac` (of `Add-Content`) are not caught (Clarifications, 2026-11-21). A read that holds
  ` -i` after the first word (`sed -n '/ -id/p' f`) is refused too, and DMC reads with the Read tool anyway. The rules
  take the usual forms, not every form (the limit
  recorded for the delete rules at `.docs/decisions.md:175` applies here too).
- The hooks and `dmc` itself run outside the model's tool calls, so the rules do not apply to them.
- The player's own typed `!` commands are theirs to run and are not a target of this change.
- A future kit step that needs one of these commands fails the toolkit test, not a play session.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `kit/settings.json` MUST deny these Bash commands: `sed -i*`, `sed * -i*`, `sed -Ei*`, `sed -ri*`,
  `sed --in-place*`, `sed * --in-place*`, `python *`, `python3 *`, `py *`, `node *`, `perl *`, `tee *`. (The four
  bundled and long sed forms were added 2026-11-24 after the code review of PR #272: `sed -Ei` ran live with the
  first two rules alone.)
- **FR-002**: `kit/settings.json` MUST deny these PowerShell commands: `Set-Content *`, `Add-Content *`, `Out-File *`,
  `python *`, `py *`, `python3 *`, `node *`, `perl *`. (The last three were added 2026-11-24 after the code review
  of PR #272: `node -e` ran live from PowerShell.)
- **FR-003**: `kit/settings.json` MUST deny the `SendFeedback` tool by its bare name.
- **FR-004**: Removed 2026-11-21 (Clarifications): the kit's permission mode is not a requirement of this change.
- **FR-005**: `kit/skills/dmc-new-campaign/SKILL.md` step 1 MUST name the Write tool for writing
  `adventures/.active-campaign`.
- **FR-006**: A toolkit test MUST check that the kit settings hold every rule of FR-001 to FR-003; that the commands of
  User Story 1 are denied in their shell; that the commands of User Story 1 scenario 3 and the Edge Cases' look-alikes
  are not; and that `.claude/settings.json` holds none of the rules.
- **FR-007**: No kit prompt may tell DMC to run a command these rules deny; the existing test that every command the
  prompts name needs no permission prompt keeps passing.
- **FR-008**: The documents MUST describe the change in their homes: a new entry in `.docs/decisions.md` with the
  reason, the source and the limits (the forms of the Edge Cases that are not caught, the PowerShell aliases and
  `Tee-Object` among them); the sentence in `.docs/user-guide.md` that lists what the folder's settings block;
  the list of deny rules in `.docs/architecture.md` (the settings row of its table); the status in
  `.docs/roadmap.md`. Other places point to these and copy nothing.

### Key Entities

- **Deny rule**: one entry of `permissions.deny` in `kit/settings.json`, a bare tool name or `Tool(pattern)`.
- **Shell write**: a command that changes a file's contents through Bash or PowerShell instead of Edit or Write.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every command in the test's list of User Story 1 is refused. (The slip analysis counted 43 of the 45
  unasked shell writes in the logs as taken by these rules; that figure is cited, not measured again here.)
- **SC-002**: Every command a kit prompt tells DMC to run is still allowed with no permission prompt.
- **SC-003**: A play session's context is about 5,500 characters smaller, if Claude Code removes a denied tool from the
  model's tool list (to confirm, see Assumptions); in any case it gains nothing.
- **SC-004**: 0 lines are added to `kit/dm-rules.md`; `dmc-new-campaign` changes by one phrase in step 1.
- **SC-005**: Every present toolkit test passes with no change to its expected result.

## Assumptions

- The list of rules is the issue's, taken as given (Clarifications, 2026-11-21); the PowerShell aliases and
  `Tee-Object` are left out and named among the limits.
- Not verified yet: that Claude Code drops a tool denied by its bare name from the tool list it sends to the model, and
  so saves the 5,500 characters. The plan checks it with a headless session in a scratch DMC folder. If the tool stays
  listed, the deny still stops the call and the token saving is dropped from the claims.
- Deny rules apply in every permission mode, `auto` included (the delete rules of PR #38 rest on the same fact).
- No campaign file, template or rule for a file's shape changes, so no campaign migration is needed (Development 2).
  `dmc update` rewrites `.claude/settings.json` and the skills from the kit, so the change reaches existing DMC folders
  with no new step (Development 3).

## Out of Scope

- A guard that depends on the player's intent, or a gate on sheet changes while a correction is open (#257).
- Blocking every possible shell write (redirects, `bash -c`, full paths, aliases).
- The player's own setup (plugins, the global `CLAUDE.md`); that is the "Work for the player" list of #239.
- The other Phase 0 work (#246 to #248).
