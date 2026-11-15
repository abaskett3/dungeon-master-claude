# Quickstart: validating "Yes, and…" and "No, but…"

## 1. Build and the toolkit tests

```text
dotnet test tests/Dmc.Tests --filter "FullyQualifiedName~Toolkit"
```

Pass: the line and byte cap test passes at 212 lines and the new cap (SC-004), and the new § Fun first test passes.
Three toolkit tests fail locally when `artifacts/bin/dmc.exe` is older than `kit/dm-rules.md`
(`WithoutAWorkingDmcASessionIsToldOnceToReinstall`, `PlayRunsTheBreakReminderBeforeEachMessageAndItNeverGetsInTheWay`,
`PlayRedrawsTheMapAfterEachReplyAndOnlyRefusalsAreHeard`): run `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`
first. The whole suite (`dotnet test tests/Dmc.Tests`) also runs the migration tests: `MigrationGoldenTests` and
`MigrationFilesTests` on fixture 007, and the check's tests in `MigrationChecksTests` (SC-006).

## 2. The wording

Compare `kit/dm-rules.md` lines 165, 166 and 169 with [contracts/dm-rules-fun-first.md](contracts/dm-rules-fun-first.md),
the template's Arc plan comment with [contracts/dm-notes-arc-plan.md](contracts/dm-notes-arc-plan.md), and
`kit/migrations/007-*.md` with [contracts/migration-007.md](contracts/migration-007.md).

## 3. Headless play test

In `D:\Claude\test-campaign`: record `adventures/.active-campaign`, and copy the campaign under test and the folder's
`CLAUDE.md` to the scratchpad first; restore all three afterwards. The folder's `CLAUDE.md` is `kit/dm-rules.md`
word for word, so a rules-only change is copied in by hand (`dmc refresh` refuses inside Claude Code); a change to a
template, a skill or a migration needs the maintainer's `<worktree>\artifacts\bin\dmc.exe refresh D:\Claude\test-campaign`
in their own terminal, or the changed files copied in by hand. The folder is not trusted, so its own allow list is
ignored: pass `Edit(adventures/**)` too, or DMC cannot write canon or save. Then, from that folder, pinning the session:

```text
$env:CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0
claude -p "/dmc-resume-campaign" --session-id <uuid> --model sonnet --effort high --allowedTools "Bash(dmc:*)" "Bash(dmc *)" "PowerShell(dmc *)" "Edit(adventures/**)"
claude -p --resume <uuid> "<one player message>" --model sonnet --effort high --allowedTools "Bash(dmc:*)" "Bash(dmc *)" "PowerShell(dmc *)" "Edit(adventures/**)"
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

An idea that rests on a fact the character would know is different (a tree that is not there, a potion the sheet does
not hold) gets line 153's out-of-character correction (US2 scenario 5): it passes if the correction offers the ways
forward, and is not counted in SC-002; send another ruled-out idea in its place.

## 3a. The second play test (after research R12)

Only what changed since § Results' first run is rerun (research R18), with § 3's method:

| # | Kind | Message (adapt to the scene) | Pass |
|---|---|---|---|
| 9′–12′ | Ruled out by the world, none resting on a wrong premise | e.g. a hidden door in plain rock; flying without magic; lifting something far too heavy; talking to a creature that cannot understand | Refused in the fiction and, in the same reply, a way toward what the player wanted (SC-002) |

SC-002's five are these four and the first run's message 13 (line 169 has not changed). The goal check (US3 scenario
1, SC-006) runs after § 3b: a goal stated in a migrated campaign lands in the Arc plan's Character arcs with how it
ties into the main plot.

## 3b. Migration 007, live

`dmc install` refuses inside Claude Code, so the live run uses the installed test folder (research R17):

1. Record `adventures/.active-campaign`. Copy fixture 007's `before/` to `adventures/arc-test/` with `.standard` `6`,
   and `kit/migrations/007-*.md` into the folder's `migrations/` (and the changed template into its `templates/`).
2. With `<worktree>\artifacts\bin` first on `PATH` (the installed `dmc` has no `dm-notes-have-arc-plan`), make
   `arc-test` active and ask DMC, headless, to bring the campaign's files up to date (`dmc-migrate-campaign`).
3. Pass: `dmc campaign verify arc-test` passes; the result matches `after/` (the Arc plan, the comment, the Character
   arcs line, the acts demoted, nothing else changed). Fix the migration's prose for anything DMC got wrong.
4. § 3a's goal check runs on the migrated `arc-test`: resume it, have the character state a goal, run the full save.
   Only if `arc-test` is too thin to resume, migrate a copy of the play-test campaign instead (standard 5, so 006
   then 007; its "The five stages and the hero's journey" moves inside the Arc plan by step 2).
5. Remove `arc-test`, the copied migration and template, and restore `.active-campaign` and the play-test campaign.

## 4. After release

A `review-transcript` review of the first 3 play sessions finds no flat out-of-character refusal of a player's
choice, and, for each goal the character stated, a later scene that moves it on, or a note that the main plot has not
reached a point where it fits. It counts the "No, but…" and "Yes, and…" answers in each session, judged by what they
do, not by their words, and flags a session where "No, but…" answers outnumber "Yes, and…" ones (SC-005).

## Results

Filled in by T004, T007 and T009: one line per message, with pass or fail and the reply's key words.

Run on 2026-11-15 in `D:\Claude\test-campaign`, campaign `rorns-landing` (session 54, the high road after Fight 12,
level 7), one pinned `claude -p` session (`--session-id`, then `--resume`) on `--model sonnet --effort high`.
Changes from § 3:

- The rules were copied into the folder's `CLAUDE.md` by hand instead of with `dmc refresh`. The folder's `CLAUDE.md`
  is `kit/dm-rules.md` word for word, and `dmc refresh` refuses to run inside Claude Code.
- `Edit(adventures/**)` was added to `--allowedTools`. The folder is not trusted, so the allow list in its
  `.claude/settings.json` is ignored, and without that entry DMC could not write canon or save.

### T004: "yes, and" (lines 166's first three sentences only)

| # | Kind | Message | Result |
|---|---|---|---|
| 1 | Bold | rides his shield like a sled down the road on a rimed slab | Pass. Acrobatics DC 10 rolled (20). Kibil flies after him, Hala stands up on the rise, and the slab will not stop on its own |
| 2 | Bold | hooks Varunhal into the frost to spin the slab to a stop, bows to Hala | Pass. It stops a hand's breadth from the rise; Hala bows back, then looks at his pocket (the Ember he promised her) |
| 3 | Bold | the fair trick: "finds" the Ember behind Hala's ear | Pass. The trick works; the Ember stops her shivering; "You bwought it back. And you bwought *you* back." |
| 4 | Bold (invalid) | throws a torch into the groove, from the rise | Not counted: the message was wrong about the scene (the groove starts 240 yards back; his torches are unlit). DMC said so out of character before resolving and offered the ways forward (`kit/dm-rules.md:153`). It did not say the story forbids it |
| 4b | Bold | walks back up, flings Pip's torch into the groove's start | Pass. The rime does not melt a pace past the flame; a cold draught leans the flame down the groove from the north; Kibil: "it stiww come" |
| 5 | Bold (invalid) | climbs the tallest pine by the road | Not counted: no pine stands there. DMC said so out of character and offered the west slope, a boulder, or Kibil (line 153 again) |
| 5b | Bold | climbs the west slope for a look north | Pass. From a shelf 40 feet up: the groove runs into a saddle of the Greywall, the snowline is lower than this morning, and a dark fleck stands on the snow (a Perception check) |
| 6 | Routine | climbs down, hands Pip his torch back | Pass. Resolved plainly; Pip asks what he saw |
| 7 | Routine | eats a strip of dried meat | Pass. Resolved plainly; Kibil begs; Kole asks about the fleck Shale played down |
| 8 | Routine | feeds Kibil, sits on a boulder to eat | Pass. Resolved plainly, with the scene's weather and light |

SC-001: 5 of 5 bold choices got a detail or hook; 0 of 3 routine actions got one. No reply said "yes, and".
Separately, one reply (rolling the two Perception checks) showed no dice and no narration, only "both missed"; that is
not this feature's.

### T007: "no, but" (line 166 whole, line 169's exception)

Run in the same session as T004 for messages 9 to 12, with line 165 (T008) also in place for the goal and the save
below; then, after a full save, in a second session (resumed) at **Lethality** `gritty` for messages 13 and 14. The
major NPC was **Rorn**, the adult silver dragon, named in the campaign's five stages (its arc plan, from before the
template's `## Arc plan` section existed); the main antagonist Hush is dead (s28.1) and Gloam cannot be put in a scene.

| # | Kind | Message | Result |
|---|---|---|---|
| 9 | Ruled out | pries at a roadside boulder for a hidden dwarven door | Partial. Refused in the fiction (Stonecunning: natural granite, no chisel work), with a clue (the rock holds no rime, so the cold is the stone-thing's own); no way forward toward what he wanted |
| 10 | Ruled out | spreads his cloak and tries to fly to the saddle | Partial. Refused in the fiction (a four-foot fall into the heather, unhurt; Kibil: "Cwoak is *not* wings!"); no way forward offered |
| 11 | Ruled out | drinks "that third potion of greater healing" (the sheet has two) | Pass on the ban, not in the fiction. Out of character, as line 153 asks when a premise is wrong: two potions, not three, and the real choice; never "the story does not allow it" |
| 12 | Ruled out | casts Fireball (a fighter) | Pass. Nothing comes out of his palm; Ixenna: "It wants a *slot*"; then the wind drops dead for a breath and Kole says "it heard you" |
| OOC | Out of character | "is there any way Shale can cast fireball?" | Pass. A frank no ("not as Shale stands"), with three real ways to fire (a necklace of fireballs, alchemist's fire, Ixenna) |
| 13 | Major NPC, first attack, `gritty` | hurls Varunhal at Rorn's eye and charges, ordering the party in | Pass. DMC first confirmed the attack on a non-hostile (line 155). Rorn caught the axe, stopped the charge with a claw, struck no one: "I will allow this once … I will not allow it twice. Who has put this in you?" Canon line 285: "**Shale attacks Rorn** (s55.1 …) … allows it **once** … **not twice** … Nobody was hurt." |
| 14 | Major NPC, again, `gritty` | attacks her again | Pass, with a note. Fight 13, initiative and every attack shown; Rorn is not shielded (Kole's crit 21, Shale 8). She answers with her paralyzing breath (DC 20; all four fail) and does not strike; DMC offers to run the saves on while "Rorn waits and does not attack". The dice fell, but she chose not to kill, so `gritty` was not tested by damage |

SC-002: of the 4 ruled-out choices played (9 to 12), 1 was refused in the fiction with a way forward (12), 2 were
refused in the fiction with no way forward (9, 10), and 1 was refused out of character under line 153 (11); 0 of 4
said out of character that the story does not allow it. The first attack on the major NPC left everyone alive (1 of
1). No die was changed and no resolved choice undone; no reply said "no, but" or "yes, and".

Out-of-character premise checks: messages 4, 5, 11 and the lantern (session 2) each rested on a wrong fact (a
position, a pine, a potion count, which light calls Rorn). Each time DMC stopped and said so before resolving, as
`kit/dm-rules.md:153` asks, and offered the ways forward. That rule and the new "no, but" overlap: when the player's
idea rests on a fact the world contradicts, DMC uses line 153 out of character, not "no, but" in the fiction.

### T009: a goal the character wants (line 165)

Shale told Kole: "When this is done, I want one thing for myself. A hearth-stone for the Landing, cut from Kelderak
stone by my own hand." After `/dmc-campaign-full-save`:

- `story/dm-notes.md:211`, in "Left open after the Heartpine": the goal, with its payoff tied to the main plot in
  Stage 5 ("or as the tower's last gift": a spare Kelderak block, lit from the Ember's light once the tower burns),
  "Never offered unprompted before then".
- `story/canon.md:211` and Shale's sheet (Long-term goals) hold it too.

Pass for User Story 3 scenario 1 (recorded in `dm-notes.md` with how it ties in). This campaign has no `## Arc plan`
section: it predates the template's (commit `50e43d5`), so the goal could not go in the Arc plan's Character arcs.

### Second round (T020): "no, but" with a way toward what the player wanted (research R12)

Run on 2026-11-15 in the same folder and campaign, restored to its state before the first round (session 54, the
high road after Fight 12), in a new pinned session, with line 166's new wording in the folder's `CLAUDE.md`. None of
the four rested on a wrong premise.

| # | Ruled out | What Shale wanted | Result |
|---|---|---|---|
| 9′ | a hidden dwarven entrance in the west slope | a way into the mountain, where the stone came from | Pass. Twenty minutes of knocking: granite, no chisel work. In the same reply Pip: "it didn't come out of the hill. The groove runs in from the *north* … from somewhere up the mountain" |
| 10′ | flying to the saddle on his cloak | to see the saddle now | Pass, weakly. One heartbeat of lift, then a skid down the road, unhurt; the way is only implied: Kibil "only goes up on your word" |
| 11′ | shouldering a cart-bed of granite to carry home | a sample for Bruni | Pass. It comes up to his knees; Pip: Hobb's crews have "timber sledges and ponies at the high camp", "or there's this size, sir, if Bruni only wants a piece with the grain". An out-of-character line after it gives his lifting limit, a rules note, not a refusal |
| 12′ | questioning the rime | where its mistress is and how many more | Partial. The rime does not answer, and melts where the Ember's light reaches it; the wind cuts out for a breath, as if listening. A hook, but no way toward where she is or how many |

SC-002 after the second round: 4 of 5 with the first round's message 13 (9′, 10′, 11′ and the attack on Rorn; 10′
only by implication), 0 of 5 saying the story does not allow it, no die changed, and no reply saying "no, but". Not
met: 12′ answered with a hook instead of a way forward.

The maintainer accepted 4 of 5 on 2026-11-15, with the wording as it stands (the byte allowance is spent); whether
"no, but" offers a way forward in real play is left to the after-release review (SC-005).

### T026 to T028: migration 007, live

Run on 2026-11-15 in `D:\Claude\test-campaign` with the dev `dmc` (`artifacts\bin`) first on `PATH`; fixture 007's
`before/` as campaign `arc-test` at standard 6, migration 007, the current template, and the kit's
`dmc-campaign-full-save` and `dmc-character-sheet-update` (each one line newer than the folder's) copied in by hand.

- **T026**: `dmc campaign standard arc-test` printed `behind: standard 6, kit 7: 007-arc-plan-everywhere.md`. The
  campaign and toolkit tests pass (783, 4 skipped); none assumed standard 6.
- **T027, first run**: `/dmc-migrate-campaign`, then yes. DMC showed What changes and "nothing" dropped, backed up,
  and `dmc campaign verify` passed (76 of 76 lines matched). The result matched `after/` except the Character arcs
  line's form (`Wren, a ferry …; … Mara Quill, …`), every fact in it from the files. Step 4 was given an exact form
  (`Name: what they want; how the main plot meets it.`) and the fixture's line follows it.
- **T027, second run** (fresh `before/`, the revised step 4): verified again (76 of 76); identical to `after/` but
  for the Character arcs line, which DMC wrote as a nested list, one `Name: want; tie` item per character. Kept: it
  passes `dm-notes-have-arc-plan` and holds only facts from the files.
- **T028, the goal**: the migrated `arc-test` resumed (it named its missing canon, summary and logs, and recapped
  both wants from Character arcs). Wren said she will take the ferry "all the way down the Marrow to the sea, where my
  mother came from" with Tobin as first deckhand. After `/dmc-campaign-full-save`, `story/dm-notes.md`'s Arc plan
  read: "Wren: wants a ferry of her own on the Marrow, to sail it down to the sea where her mother came from (s6.1),
  with Tobin as first deckhand; the ferry rights are on the table in Act 2. Test: the Drowned King offers the rights
  at a price, or Tobin is put at risk." Pass (US3 scenarios 1 and 3, SC-006).
- **T029**: the folder restored: `CLAUDE.md`, the template, both skills, `.active-campaign` and `rorns-landing`
  (131 files, 0 differences) match their backups; migration 007 and `arc-test` were moved to the scratchpad.
