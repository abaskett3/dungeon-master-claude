# Tasks: Deny the usual shell writes and `SendFeedback` in play

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md)

## Phase 1: Tests first

- [x] T001 [US1] [US2] Add `tests/Dmc.Tests/Toolkit/ShellWriteDenyTests.cs` with `ShellWriteRules` and
  `TheKitSettingsDenyShellWritesAndSendFeedback`: each rule of FR-001 and `SendFeedback` in `kit/settings.json`, the
  commands they take and the reads they do not, none in `.claude/settings.json`, no `defaultMode` or
  `remoteControlAtStartup` (FR-003, FR-005)
- [x] T002 [US3] Add `TheSkillsWriteTheActiveCampaignWithTheWriteTool` to the same file (FR-004)

## Phase 2: The kit

- [x] T003 [US1] [US2] Add the thirteen shell rules after the shell deletes and `SendFeedback` after
  `ShareOnboardingGuide` in `kit/settings.json` (FR-001, FR-002)
- [x] T004 [US3] `kit/skills/dmc-new-campaign/SKILL.md` § 1 and `kit/skills/dmc-resume-campaign/SKILL.md` § 1: "with
  the Write tool" (FR-004)

## Phase 3: Documents

- [x] T005 `.docs/user-guide.md` step 2 of `dmc play`: the shell writes are blocked
- [x] T006 `.docs/architecture.md` § Settings and hooks row; `.docs/development.md` Toolkit test row
- [x] T007 `.docs/decisions.md`: a new entry with the reason, the rejected option and the limits
- [x] T008 `.docs/roadmap.md`: a row for spec 020

## Phase 4: Checks

- [ ] T009 CI: the full test suite passes (the authoring sandbox had no .NET SDK)
- [ ] T010 Quickstart Q1 to Q3 in a scratch DMC folder
- [ ] T011 Run `/code-review` once on the PR; set the roadmap row's status to Done with the PR number once merged
