---
description: "Task list for spec 025, count every slip in every story session"
---

# Tasks: Count every slip in every story session

**Input**: `specs/025-slip-count/spec.md`, `plan.md`

- [X] T001 [US1] `kit/templates/session-log.md`: the `conduct` category in the `Slip:` line.
- [X] T002 [US1] `kit/dm-rules.md`: the correction paragraph logs every slip, conduct included; the file does not
  grow (32,507 bytes).
- [X] T003 [US2] Test first: `DigestLibraryTests.ThePlayLineGivesTheDenominatorOfASlipCount`.
- [X] T004 [US2] `src/Dmc.Cli/Digest/Session.cs` (`Efforts`, `Kind`, `PlayTurns`) and `Report.cs` (`Play`, called
  from `Stats`).
- [X] T005 [US2] `.claude/skills/review-transcript/SKILL.md` § 7: the count for every story session and its line.
- [X] T006 [US3] § 8: the #179 gate count, conduct left out.
- [X] T007 Documents: `.docs/campaign-files.md`, `.docs/dmc-reference.md`, `.docs/roadmap.md`, `.docs/decisions.md`.
- [X] T008 `dotnet test` passes with a fresh publish.
