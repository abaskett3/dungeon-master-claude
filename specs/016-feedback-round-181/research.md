# Research: The feedback round of #181

Line numbers are `origin/main` at `0cfdfa1` (PR #228 merged) unless a line says otherwise. Campaign evidence is
`D:\Claude\dmc\adventures\rorns-landing` as it stood on 2026-11-16 (sessions to s62).

## R1. The baseline for SC-004

- **Decision:** count `Slip:` lines in the session logs, s55 to s59: 10 slips in 5 sessions, 7 of them inventing over
  the files (s55: the stew and lantern, Kole's watch; s56: Tam's table; s59: Hala's "thwee", Kole and the boots, Dessa's
  and Tam's distrust, the second supper). **Baseline 1.4 per session; target at most 0.7** over the first 10 sessions
  on the release (SC-004).
- **Rationale:** the logs hold a `Slip:` line in their template from s55 (`session-055.md:21`); s49 to s54 log one slip
  in six sessions (s52) although the feedback records at least two there (grey hood s49, socks s51), so earlier logs
  undercount. s60 and s61 add 4 more of the kind (Dessa's full name, Linnet's heel, Peg's shells, and Dessa's OOC
  answer).
- **Alternatives considered:** the feedback entries per session (fewer, and recorded only when the player asks); the
  spec 013 gate's categories (its "other" holds conduct and maps too).

## R2. Why PR #228's revision did nothing

- **Finding:** `specs/015-adaptive-arc-plan/quickstart.md:108-128`: two full saves on an overtaken copy cut finished
  stages but kept Auntie Hush (dead at s28.1) in stages 3 and 4 and in Antagonists, and added no budget, start session
  or clock. The second "treated a session with no scene as nothing new to record and trimmed for size only".
- **Cause:** step 5's arc checks (`kit/skills/dmc-campaign-full-save/SKILL.md:42-50`) are judgments written "in a few
  lines of `dm-notes.md` and not out loud"; nothing shows whether they ran. What the save does act on is
  `dmc size --check`: step 7 (line 71) tells it to act on every line printed, and to name any that stays and why
  (line 81). This is #181's cause 1, "a due item is only a note".
- **Decision (approved 2026-11-16, on the condition that the player never acts on or sees them):** Arc plan flags in
  `src/Dmc.Cli/Size/Flags.cs`, printed only by a new `dmc size <campaign> --arc`
  ([contracts/size-arc-flags.md](contracts/size-arc-flags.md)): a dead person named ahead, no current stage, no budget
  on it, over its budget, and an antagonist with no `by sN`. Not in `--check`, because the resume runs `--check` and
  tells the player some of its lines (`kit/skills/dmc-resume-campaign/SKILL.md:18`), and the save's report names any
  `--check` line left (`kit/skills/dmc-campaign-full-save/SKILL.md:81`), which would also spoil the Arc plan. Step 5
  gains one sentence: run `--arc`, fix each line in `dm-notes.md`, never mention them to the player.
- **Conflict with a decision:** #181 decided "the fix goes in the full-save skill only, with no template change". The
  flags keep "no template change" and add no migration, but they are code. The issue's own principle ("the fixes must
  change how DMC works, not only add wording") and #228's result argue for them.
- **Feasibility, checked:** Hush's sheet says `- **Status**: DEAD (s28.1, …)`; 1 other dead sheet is a ghost still in
  play ("dead (a ghost, bound to her post…)"), so the flag asks for a revision or a reason, never deletes.
- **What the code can tell (decided 2026-11-16, /speckit-analyze U1 and U2):** a line naming the dead that also says
  `dead` counts as known. Today's `dm-notes.md:75` ("Hush is dead (s28.1), so these need Gloam behind them") passes;
  `:79` and `:84` ("Hush prepares", "Hush's last illusions") fire, matched by last word as `ReadSet.Mentions` does
  (`src/Dmc.Cli/Size/ReadSet.cs:319`). Missed: a stale line that says `dead` for another reason. No file records that a
  module is finished, so the flags run only when the Arc plan holds a stage line, which the full save writes only for a
  homebrew campaign or a finished module (`kit/skills/dmc-campaign-full-save/SKILL.md:44`). Unverified: whether any
  module campaign holds stages mid-module. The campaign's
  Arc plan has no `current since sN` and no `about N sessions` yet (grep of `story/dm-notes.md`).
- **Alternatives considered:** more wording in step 5 (what #228 tried); a separate arc subagent (more cost, the same
  judgment); a template field per stage (a migration, ruled out).

## R3. Why session 60 had dead time (FR-031)

- **Finding:** transcript `af50ebc1` (the play folder's project log): the resume read the files and listed "Today's
  open items": the ring, Linnet's fitting, Pip's errands, the magic question; the climb is tomorrow. Every one was a
  chore with no conflict, and nothing in the plan was due that day (the Siege is set for the 28th, play is on the 13th).
  To the player's small moves ("laying in bed studying his notebook", "goes downstairs", "looks out the window") DMC
  replied three times on atmosphere ("The bread is nearly done, and the oven door ticks as it heats") and then waited.
  Asked, it named the rule it broke (`kit/dm-rules.md:195`).
- **The clarify guess was half right:** something was pending, but nothing was due. The stale clock (R2) is the cause
  that a change can reach; the beat rule exists.
- **Decision:** no new `kit/dm-rules.md` wording for User Story 7. The clocks flag (R2) makes each antagonist's next
  move carry a session, and #228's Clocks then put a `Show next session:` sign where the party is. The quickstart tests
  a session start after a full save (scenario 7). If replies still end on atmosphere, that goes to a later round with
  the evidence.
- **Also in this transcript, for B:** asked "why does Dessa keep using my full name", DMC answered from memory
  ("weighty moments") before reading her sheet, which shows the habit in casual lines; it said it would record the
  change and did not until the player said "You changed no files". FR-010 covers both.

## R4. The hook's sheet lines (S1, FR-008)

- **Decision:** a second part of `dmc context-check`'s reminder, beside the canon lines
  (`src/Dmc.Cli/Retrieval/CanonReminder.cs`), with its own header and a cap of 10,000 bytes
  ([contracts/context-check-sheets.md](contracts/context-check-sheets.md)). Who is present comes from the same matching
  the resume uses (`src/Dmc.Cli/Size/ReadSet.cs:222-223`, `PresentText` and `Mentions`), so the two never disagree
  (Development 6). The lines are the field lines FR-008 names, from either sheet shape.
- **A gap found (approved 2026-11-16):** "once per person per session part" fires on the first message after a
  resume, when the resume has just read the same sheets (`ReadSet.cs:174-226`). The slips it targets came 131k to 332k
  tokens in (#181), long after. So the reminder fires again for a person when their lines were last shown more than
  **100,000 tokens** ago in this part, and skips anyone whose sheet the resume read within that distance. Cost: about
  2.5k tokens per 100k for 8 people with five companions (spec § Edge Cases), about 2.5%.
- **Rationale:** the hook already reads the log for reminded names after the latest compaction (`Reminded`,
  `CanonReminder.cs:141`) and the context size for break offers (`BreakReminder`), so the distance needs no new input.
- **Alternatives considered:** once per part as the spec says (fires only when it is least needed); on every message
  (about 2.5k tokens a message); only on the person's first entry to the scene (misses a long scene).

## R5. The out-of-character line (B-hook, FR-011)

- **Decision:** printed by `dmc context-check` when the prompt starts with `ooc` (any case, then a space, colon or end)
  or `((`, not by a separate `grep` hook as the issue drafted.
- **Rationale:** `context-check` already reads the prompt from the hook's JSON (`HookInput.Field`, `CanonReminder.cs:41`)
  and has unit tests; a shell `grep` of raw JSON breaks on escaped quotes and has none. The play settings keep one
  `UserPromptSubmit` hook (`kit/settings.json`).
- **Conflict with the issue:** its B-hook "needs no code". Since S1 changes `context-check` anyway, the code is one
  method and its tests; the line and its trigger are as the issue tested them (224 bytes).

## R6. Companions and migration (FR-032)

- **Decision:** no migration. The template comment (`kit/templates/campaign/game-state.md:20`) gains "or directed by
  the player in a fight", and the full save's sheet pass (A5) moves anyone under "With the party, not companions" whom
  the player directed in a fight that session into Party status.
- **Rationale:** the file's shape does not change (the same two places); a misfiled character is moved the next time
  they fight, and rorns-landing's one known case (Hala) was moved by the player on 2026-11-15. Moving a character in is
  safe: a companion with no card is read down to its History (`ReadSet.cs:356-366`) until the save writes one.
- **Alternatives considered:** migration 009 asking a model to judge each character's fights (a judgement no shape
  check can verify, `src/Dmc.Cli/Campaign/MigrationChecks.cs`).

## R7. The session log's Present line (A3, FR-003)

- **Decision:** the scene block in `kit/templates/session-log.md:13-22` gains a `- Present:` line; no migration.
- **Rationale:** each session's log is created from the template (`kit/skills/dmc-resume-campaign/SKILL.md:52`), so new
  logs get it; old logs keep their shape, and the "who could know" check reads the scene where there is no line (spec §
  Edge Cases).

## R8. The break band (D2, FR-020)

- **Decision:** `BreakReminder.Band` 40,000 → 50,000 (`src/Dmc.Cli/ContextCheck/BreakReminder.cs:18`). `Margin`
  (line 15, the first offer after the resume) stays at 40,000: "the break points stay as they are".
- **Documents:** `.docs/decisions.md:145` (amend), `.docs/dmc-reference.md:813`, `.docs/user-guide.md:187` (the repeat
  offer; line 184, the first offer, stays). `.docs/roadmap.md:20` is the 0.1.3 history and stays; a new row records this.

## R9. The map's aura and view (S2, FR-025)

- **Decision:** two optional fields in the map format (`kit/skills/dmc-draw-map/map-format.md`): a token's `aura` and a
  map's `view` ([contracts/map-aura-view.md](contracts/map-aura-view.md)). Drawn by `src/Dmc.Cli/Maps/SvgDrawing.cs`,
  read by `PyJson.cs`, checked by `MapChecker.cs`, listed by `Distances.cs`.
- **Migration:** none; both fields are optional and old maps draw as before (#226).

## R10. The cost (Development 1)

Measured on the drafts as written (2026-11-16, `git diff` against `42d4d0a`); the maintainer approves each prompt
clause's wording before it is committed (#181 § Process), and the figures are measured again after.

| Where | Change | Bytes, measured | Paid |
|---|---|---|---|
| `kit/dm-rules.md` | A1, A2, A3 pointer, A4, B1, B2, C1 to C3, D1, E1, G, H, FR-018; still 212 lines | +1,608 (estimated +1,350; +63 of it the B2 rewording after the headless run), about 402 tokens; cap 30,914 → 32,522 | every turn |
| `dmc context-check` | S1 sheet lines, capped at 10,000 bytes; the `ooc` line | rorns-landing as it stands (4 companions present): 5,490 bytes, about 1,370 tokens, at most once per 100k tokens a person; the `ooc` line 229 bytes, about 57 tokens | per message, when due |
| `dmc-campaign-full-save` | A5, FR-032's move, the `--arc` sentence | +604 | each full save |
| `dmc-character-sheet-update` | A6 (the rule moved to cover every character) | +24 | each sheet update |
| `dmc-record-feedback` | B3 | +169 | each feedback record |
| `dmc-draw-map` SKILL.md | F1 to F4 (F2 and F3 by `view`; no stopgap, since S2 ships with them); the syntax of `aura` and `view` after the headless run | +587 | each map |
| `dmc-draw-map` map-format.md | the aura and view | +1,279 | read only when the full format is needed |
| `templates/session-log.md` | A3, A7 | +178 | each new log; the resume reads the last log |
| `templates/campaign/game-state.md` | FR-032 (comment) | +38 | every resume, new campaigns only |
| `dmc size --arc` | the arc flags | a line per flag; 5 on rorns-landing as it stands | full save only |

## R11. Delivery

- **Decision (the maintainer, 2026-11-16):** one PR for the whole spec, `Closes #181`.
- **Cost accepted:** A and B ship only when the maps and the arc are done and tested; one review covers all of it.
- **Alternatives considered:** three PRs (play, maps, arc), so that A and B ship first.

## R12. #193

- **Decision:** stays open (spec § Assumptions); judged after SC-004's count.
