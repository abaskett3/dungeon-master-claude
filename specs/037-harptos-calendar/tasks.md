# Tasks: The Harptos calendar, with a year on every date

**Input**: [spec.md](spec.md), [plan.md](plan.md). Blocked on spec 036 merging (migration 011, `RecordDates.Months`).

## Phase 1: Setup

- [ ] T001 Rebase `claude/037-harptos-calendar` on `main` once spec 036 has merged
- [ ] T002 File the issue for this spec (the player approves its text first) and name it in spec.md

## Phase 2: The calendar

- [ ] T003 `src/Dmc.Cli/Calendar/Harptos.cs`: months, festival days, Shieldmeet, day of the year to and from a date, a date's text; `RecordDates.Months` reads from it
- [ ] T004 [P] Tests `tests/Dmc.Tests/Calendar/HarptosTests.cs`: Midsummer is day 213; the 30th of Hammer then Midwinter; Shieldmeet in 1492, not 1493; a date past the year's end rolls into the next
- [ ] T005 `src/Dmc.Cli/Calendar/OldCalendar.cs` and `Anchors.cs`: the 13 x 28 calendar, "the first/second month", "the Nth day of the new year", spans from `calendar.json`
- [ ] T006 [P] Tests: Day 1 (the 10th of Nightal) is the 12th of Nightal 1492; Day 49 is the 30th of Hammer 1493 and Day 50 Midwinter; the 28th of Kythorn is the 27th of Flamerule; the 18th of Eleasis is the 12th of Eleint; the 19th of Ches is the 25th of Tarsakh; a weekday is kept

## Phase 3: The draft and the run (User Stories 1 and 2)

- [ ] T007 `dmc calendar draft <campaign>`: the change list in groups, `?` for a date it cannot place; writes only `backups/<campaign>.calendar-draft.md`
- [ ] T008 `dmc calendar apply <campaign> --yes`: refuses a draft with `?` or without `approved:`; verify, backup, changes, new closing backup pair and `.closed` for a closed campaign; the accounting
- [ ] T009 [P] Tests: the draft of a fixture campaign matches its golden list; the run on a closed fixture leaves it closed, keeps the old closing backup, and `verify` passes against the new one; a moved line refuses the run with nothing written
- [ ] T010 The draft of both real campaigns, on scratch copies, matches `D:\Claude\037-calendar\changes.md` group by group (the prototype), with each difference explained

## Phase 4: The kit (User Story 3)

- [ ] T011 `kit/calendar.md`; `kit/dm-rules.md` § Time and distance; the game-state template's date line; `dmc-new-campaign`'s first date with a year
- [ ] T012 `kit/migrations/012-harptos-calendar.md` (standard 12), with the canon lines the player ruled, the weekday reference and the quarter-day rule, and its golden fixture
- [ ] T013 `calendar.json` for both campaigns from the player's rulings, written by the migration

## Phase 5: Polish

- [ ] T014 Docs: `.docs/campaign-files.md`, `dmc-reference.md`, `decisions.md`, `roadmap.md`
- [ ] T015 Publish, `dotnet test`, CI
- [ ] T016 Run migration 012 on scratch copies of both campaigns; then, on the player's word, on the real ones
- [ ] T017 The slip test cannot run (no campaign in play): say so in the PR, with what ran instead
- [ ] T018 `/speckit-converge` until a pass adds no tasks; the code review on the PR
