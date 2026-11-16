# Quickstart: checking spec 015 works

What to run to show each success criterion, in order. The exact wording is in [contracts/](contracts/); the fields in
[data-model.md](data-model.md).

## Before

- Build `dmc` from this branch (`dotnet build` / publish as `.docs/development.md` says) and refresh the kit of the
  test DMC folder `D:\Claude\test-campaign` from it, so its `.claude/skills/`, `templates/` and `migrations/` are this
  branch's.
- Copy the inputs to the scratchpad first, and work on copies, never on the originals (the maintainer's notes for
  headless runs). Run `claude -p` in the test folder; set `CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0` when a run starts a
  subagent.

## 1. Toolkit and migration tests (SC-005, SC-006)

```powershell
dotnet test
```

Expected: all pass, among them `TheAlwaysLoadedRulesStayAtTheirLineAndByteCaps` (212 lines; the cap risen by at most
300), the new § Narration test, `MigrationGoldenTests` and `MigrationFilesTests` on fixture 008, and the
`arc-plan-guidance-current` tests.

## 2. The migration, live (SC-006)

Copy fixture 008's `before/` into a scratch DMC folder as a campaign, run `dmc-migrate-campaign` on it headlessly, and
compare with `after/`. Expected: only the two comments changed (the player's own comment kept), `dmc campaign verify`
passes, `dmc campaign standard` says current.

## 3. A new campaign (SC-001)

Create a homebrew campaign headlessly with a short brief (a premise, a PC, one companion). Expected in its
`story/dm-notes.md`: a Structure line; 5 to 7 stages, each naming its beat, what changes and "about N sessions"; the
current stage "current since s1"; every milestone an achievement "or a victory of the same weight"; every antagonist a
next move "by sN". `dmc size <campaign> --check` prints no Arc plan line.

## 4. An overtaken plan (SC-002)

On a copy of `rorns-landing` (session 54; Auntie Hush died at s28.1 and its stages still name her), migrate (006, 007,
008), then run one full save. Expected: no stage still ahead rests on Hush; every stage, thread and foreshadowing line
play did not contradict unchanged word for word (diff the section before and after); every milestone not yet awarded
anchored in the revised stages; a climax and a resolution still planned; `dmc size --check` prints no Arc plan line.

## 5. Five sessions in play (SC-003, SC-004)

On the same copy, play at least 5 sessions headlessly, each one a scripted player brief of several moves that includes
at least one scene played to its end with nothing open, one open exchange left hanging, and, in one session, a declined
jump offer; run a full save after each. Record per session in a results table here:

| Session | Jump offers (scenes ended / offered) | Offers in open exchanges, mid-scene, fights | Repeated after a decline | Antagonist move due / sign shown | Milestone by equivalent | Plan churned without cause | Hurried for budget |
|---|---|---|---|---|---|---|---|

Expected: every scene that ended with nothing open got an offer (at least 3 in the run); 0 in the other columns of
offers; a move that came due had its sign shown in the session after (at least 1); a milestone reached by an
equivalent was awarded at the next full save; no plan churned and no reply hurried for a budget.

## 6. After release (SC-007)

`review-transcript` on the first 5 sessions played on the release: the Arc plan true to play at each full save, no
milestone held back by a stage put off, no feedback entry about a jump offer.

## Results

### T001, T002: baseline and backups (2026-11-15)

- `kit/dm-rules.md` 212 lines, 30,631 bytes; `dmc-new-campaign` 105 lines, 12,920 bytes; `dmc-campaign-full-save`
  78 lines, 11,438 bytes; the template 3,697 bytes. The toolkit tests pass (101 of 101) once `artifacts/bin` is
  republished; before that, 3 fail only because the binary is older than the source.
- The test folder's `CLAUDE.md`, `adventures/.active-campaign` (`rorns-landing`), the two skills, the template,
  `migrations/` (002 to 006) and `adventures/rorns-landing/` (131 files) are backed up in the scratchpad folder
  `backup-015`.

### T003 to T010: template, check, migration 008, fixture, live run

- The template's Arc plan heading and comment measure 1,911 bytes and its Antagonists comment 239, as the contract
  says; the file is 4,130 bytes.
- The check's and the template's tests pass (51 of 51 in the filtered run); the migration tests pass on fixture 008
  (98 of 98); the Campaign tests pass (693, 4 skipped). A standard-7 campaign is reported `behind: standard 7, kit 8:
  008-arc-plan-structure.md`.
- **Live run** in `D:\Claude\test-campaign` with the dev `dmc` first on `PATH`: fixture 008's `before/` as
  `arc-mig-test` at standard 7, migrations 007 and 008 and the new template copied in. `/dmc-migrate-campaign` on
  `--model sonnet --effort high` showed What changes and "Declared drops: none", backed up on "yes", replaced both
  template comments and kept the player's own note. `dmc campaign verify` passed (78 of 78 units matched), the standard
  is current, and all four files are byte for byte the fixture's `after/`. Pass (SC-006, first half).

### T011 to T013: a new campaign (SC-001)

- Step 4a and step 4's sentence added to `dmc-new-campaign`: 127 lines, 16,010 bytes (+22 lines, +3,090; the plan
  counted 21 lines and +3,134 before step 4a's "Keep milestones and money on separate lines" was dropped, below).
  The toolkit tests pass (103 of 103, then 105 of 105 with the full save's and § Narration's tests).
- **First run** (`--model sonnet --effort high`, one headless message giving all of session zero): the Arc plan named
  the Five Commandments, six stages each naming its beat with "about N sessions", the first "current since s1", every
  antagonist's next moves "by sN", and the level 4 and 5 milestones "or a victory of the same weight" (level 6 said
  "however the player does it"). But Progression held milestones and money on one line of 490 characters, despite
  step 4a, because the template's Progression bullet asked for both. **Fix**: the template's Progression bullet is
  split into Progression and Money (the Arc plan comment is now 1,917 bytes, +375), and step 4a's sentence about
  separate lines is dropped, so the rule has one home.
- **Second run**, the same brief: Structure named; seven stages (progressive complication over three), each naming
  its beat with a budget, the first "current since s1"; every milestone "or a victory of the same weight"; money on its
  own line; no Arc plan line over 400 characters; every antagonist a next move "by sN" (the fourth entry, a witness,
  is marked "not an antagonist"). `dmc size --check` prints no Arc plan line; the section measures 1.5k, at its budget.
  Lines over 400 characters remain in other sections (the central conflict, Antagonists, Opposition, Secrets), as
  before this change. **Pass (SC-001).**
- The scratchpad was lost during the run; the backups were copied to `D:\Claude\backup-015` first, with both creation
  runs (`arc-new-test-1`, `arc-new-test-2`) and the migration test (`arc-mig-test`).
