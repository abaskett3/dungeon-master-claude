# Quickstart: validating "Yes, and…" and "No, but…"

## 1. Build and the toolkit tests

```text
dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Toolkit"
```

Pass: the line and byte cap test passes at 212 lines and the new cap (SC-004), and the new § Fun first test passes.
Three toolkit tests fail locally on `main` before this change (`WithoutAWorkingDmcASessionIsToldOnceToReinstall`,
`PlayRunsTheBreakReminderBeforeEachMessageAndItNeverGetsInTheWay`, `PlayRedrawsTheMapAfterEachReplyAndOnlyRefusalsAreHeard`);
they are not this feature's, and CI is the check.

## 2. The wording

Compare `kit/dm-rules.md` lines 165, 166 and 169 with [contracts/dm-rules-fun-first.md](contracts/dm-rules-fun-first.md).

## 3. Headless play test

In `D:\Claude\test-campaign`: record `adventures/.active-campaign` and copy the campaign under test to the
scratchpad first, and restore both afterwards. The maintainer refreshes the folder from the build in their own
terminal: `<worktree>\artifacts\bin\dmc.exe refresh D:\Claude\test-campaign`. Then, from that folder:

```text
$env:CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0
claude -p "/dmc-resume-campaign" --model sonnet --effort high --allowedTools "Bash(dmc:*)" "Bash(dmc *)" "PowerShell(dmc *)"
claude -p --continue "<one player message>" --model sonnet --effort high --allowedTools "Bash(dmc:*)" "Bash(dmc *)" "PowerShell(dmc *)"
```

Send one message per call, in this order:

| # | Kind | Message (adapt to the scene) | Pass |
|---|---|---|---|
| 1–5 | Bold, fits the fiction | the chandelier swing; a leap onto a cart; a bluff at a gate; a thrown torch at a rope bridge; a climb up a tavern sign | Resolved, plus one detail or hook (SC-001) |
| 6–8 | Routine | open the door; buy rations; sit down to eat | Resolved plainly, nothing added (SC-001) |
| 9–12 | Ruled out by the world | a secret door behind solid rock; flying without magic; a fact against established canon; casting a spell the sheet lacks | Refused in the fiction, with a way forward; no out-of-character "the story does not allow it" (SC-002) |
| 13 | Major NPC, first attack, at `gritty` | attack an NPC from the campaign's Arc plan or Opposition table, far beyond the party | The NPC stops it, no one dies, a canon line with `sN.M` is written (SC-002, FR-004) |
| 14 | Major NPC, again, at `gritty` | attack the same NPC again | A real fight at the campaign's lethality; the NPC is not shielded and the dice fall where they fall (US2 scenario 3) |

Before message 13, set **Lethality** to `gritty` in the campaign's `player-options.md`: if no one dies there, the
exception holds at every lethality (FR-004). The campaign copy restores it afterwards.

Across all replies: no shown die changed, no resolved choice undone, and no reply says the words "yes, and" or "no, but" to the player; each reply is judged by what it does, not by the phrase (SC-003). An out-of-character "can my
character cast fireball?" for a character without it still gets a frank no (US2 scenario 4).

Which major NPC to use is chosen from the test campaign's files when the test runs (research R9).

## 4. After release

A `review-transcript` review of the first 3 play sessions finds no flat out-of-character refusal of a player's
choice, and, for each goal the character stated, a later scene that moves it on, or a note that the main plot has not
reached a point where it fits. It counts the "No, but…" and "Yes, and…" answers in each session, judged by what they
do, not by their words, and flags a session where "No, but…" answers outnumber "Yes, and…" ones (SC-005).

## Results

Filled in by T004, T007 and T009: one line per message, with pass or fail and the reply's key words.
