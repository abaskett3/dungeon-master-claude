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

(Filled in at implementation.)
