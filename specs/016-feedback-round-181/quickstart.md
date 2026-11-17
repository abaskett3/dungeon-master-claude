# Quickstart: validating the feedback round of #181

Headless runs use `D:\Claude\test-campaign`, from copies of rorns-landing, never the real folder; set
`CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0` for runs with subagents. Build first: `dotnet publish src/Dmc.Cli -r win-x64
-o artifacts/bin`, then `artifacts/bin/dmc refresh D:\Claude\test-campaign` from a terminal (`.docs/development.md` §
Trying a change).

## 1. Tests

`dotnet test`: the toolkit tests (`kit/dm-rules.md` at 212 lines and its new byte cap), the hook's contract
([contracts/context-check-sheets.md](contracts/context-check-sheets.md)), the map's
([contracts/map-aura-view.md](contracts/map-aura-view.md)), the size flags'
([contracts/size-arc-flags.md](contracts/size-arc-flags.md)), and the band at 50,000.

## 2. Three slips replayed (SC-001)

The files as they stood before each slip are not kept: the campaign has no history, and its one backup
(`backups/rorns-landing_backup_20261113212212.zip`) is from 2026-11-13, a migration's (which session it follows is
unverified). So each run is on a copy of today's files, with
the slip's cause seeded where it was a file's fault:

| Slip | Seed | Message | Pass |
|---|---|---|---|
| Flying, s59 | none (the sheet is right now: "read and right, still not used") | Hala is told to follow Kibil up | Hala does not fly |
| Socks, s51 | put "wool socks" back on her Appearance line, History saying they came off | run the full save, then a scene that describes her | the save fixes the line (FR-005); no socks |
| Kole's lodging, s55 | none | the evening at the Landing | Kole is not given the night watch or a room against canon |

Each run costs 150k+ tokens of context; run each once, and a second time only if the first fails.

## 3. A challenged slip (SC-002)

Seed a wrong fact in a reply, then challenge it twice: `ooc that's wrong, she can't fly` and in character "How would
she know that?". Pass: DMC quotes the files before answering, proposes one fix, and `git diff` of the copy shows only
`game-state.md` changed before the player agrees; the `ooc` run's hook output holds the out-of-character line.

## 4. Sarcasm (FR-018)

After an offer to skip ahead, send "No, Shale just skips it, forget the gold". Pass: one line asks; no file changes.

## 5. The player's side (User Story 4)

"Can he move up?" in a fight (no move); an enemy's turn that triggers Shale's reaction (DMC asks for the roll);
"let's eat" (everyone present eats); a player-written outline with Tam's lines (played, not logged as played).

## 6. Maps (User Story 5)

Draw a fight on the 16 × 44 road with a companion, a friendly NPC and a stone with a 10 ft rime aura in a view of
16 × 15; move the stone. Pass: different colours; the aura moved; the view's cell labels match the tracker.

## 7. The arc and a session start (SC-003, User Story 7)

On a copy of today's files (the s59 state is not kept, § 2; today's Arc plan still has Hush in the stages ahead,
`story/dm-notes.md:79` and `:84`): `dmc size rorns-landing --arc` prints the dead-name, current-stage, budget and clock
flags; run `/dmc-campaign-full-save`; run the check again (none, or each named with a reason); read the Arc plan (no
stage ahead rests on Hush; the stages play did not contradict unchanged); resume, and read the first three replies
(each ends on something Shale can see, hear or choose).

## 8. In play (SC-004, SC-005, after release)

`review-transcript` over the first 10 sessions on the release: corrections from inventing over the files per session
against 1.4 (research R1), target at most 0.7; break offers at most one per 50k band.

## Results

- **Baseline, 2026-11-16, `origin/main` at `42d4d0a`:** `dotnet test` 2,102 passed, 8 skipped, 0 failed (after
  `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`; the black-box tests refuse a stale binary).
- **Headless runs, 2026-11-16** (`claude -p`, Sonnet at high effort, the branch's `dmc` first on `PATH`, on a copy of
  rorns-landing at s63, standard 7; two pinned sessions, about $46). Session 1 resumed into Fight 16 and declined the
  migration mid-fight, so it ran the full save first:
  - **§ 7, the arc: pass.** `--arc` printed Hush, Brunna, no current stage and the wyvern's clock; after the full save
    it printed nothing. Stage 3 is "current since s50; budget about 20 sessions", Gloam's next move is "by s65" with a
    `Show next session` sign, Hush is out of the stages ahead, and Brunna is kept as "(a ghost, dead)", written on every
    line that names her. The session start (US7) was not reproduced: both resumes ended on a choice (a fight, then
    night), not on s60's dead time.
  - **§ 2, flying: pass.** Hala, joining the scene, scrambled up the blocks; the hook printed six people's sheet lines
    (about 7.5k bytes), hers with "no fly speed". **Kole: pass** (home to her own house). **Socks: pass** once seeded
    as a same-session change (the first seed put the contradiction in s50's History, outside A5).
  - **§ 3, a challenged slip: partial.** `ooc`: the hook's line fired, DMC searched the files and changed only
    `game-state.md`, but went straight to a replay without quoting the sheet. In character ("How would she know
    that?"): pass (ran `dmc recall`, quoted canon, one fix, no file changed).
  - **§ 3 again, after rewording B2 and the `ooc` line** (quote for and against first; no file until the player agrees),
    a fresh session, a wrong claim of the player's ("Linnet said they wouldn't be ready till the end of the week"):
    **the form passes.** The reply opened with the files (canon s56.2 "ready the 14th", the s61.1 log), then what
    supports the player and what goes against, one fix, and "I've changed no file". It also found a real slip of its own
    (the boots never collected in play). **The judgment half fails:** it offered to adopt the player's date ("I'd assume
    Saturday the 21st") although it had just said no line gives it.
  - **§ 4, sarcasm: partial.** DMC read it as sarcasm and did not act on it, but did not ask in one line.
  - **§ 5, the player's side: pass.** "Can Kibil move up?" moved nobody; Parry was asked for on the hit and DMC
    stopped; Shale's turn was held open for his unspent attack; the outline was played as a scene. "Let's eat": the
    table was set for everyone present, but the meal waited on a beat.
  - **§ 6, maps: partial.** Four kinds, four colours. The rime went into a `note`, not an `aura`, and DMC made a trimmed
    copy of the 16 × 44 road instead of a `view`: it read the skill but not `map-format.md`, which holds both fields'
    syntax.
  - **The map rerun (session 3, after the fix below): the aura passes** (`"aura": {"ft": 10, "name": "rime"}` on the
    stone); **the view was not exercised**: DMC fought on `high-road-clear.json`, the trimmed copy it had made in session
    2, whose notes say it is the same ground. The real campaign holds one such copy too (`high-road-fight.json`).
  - **Fixed after the run:** the map skill now shows `"aura": {"ft": 10, "name": "rime"}` and `"view": {"cols": "C-R", "rows": "3-17"}` and forbids a trimmed copy; `context-check` prints no break line once a reply in the band has offered the break (tests in `ContextCheckContract`).
  - **Also seen:** a break offer repeated in the band the player had declined (218k, then 222k; the rule at
    `kit/dm-rules.md:207` and the hook both said not to). The Present line in `game-state.md` names where absent
    people are ("The rest: … Kole her house"), so the hook printed five absent people's sheets. Kibil "hovers an inch"
    (his sheet: cannot hover), with his sheet read within 100k.
