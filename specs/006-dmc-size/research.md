# Research: Measure What a Resume Reads, in Tokens

Phase 0 of [plan.md](plan.md). Each section is one decision: what was chosen, why, and what else was weighed. The
measurements are of Rorn's Landing (`D:\Claude\dmc\adventures\rorns-landing`) on 2026-11-09, after PR #173 merged
but before any migration (issue #172), so the files still hold what PR #173 stops new campaigns accumulating.

## Measurements

Bytes on disk, tokens estimated as bytes ÷ 4.

| File, as the resume reads it | Bytes | ≈ Tokens | Lines | Note |
|---|---|---|---|---|
| `story/dm-notes.md` | 81.8k | 20.4k | 297 | Opposition 17.9k bytes, the stages 11.2k, Open threads 11.1k, Mix tracker 9.7k, About this player 7.9k; 69 lines over 400 characters; 10 DONE/RESOLVED |
| `story/canon.md` | 61.9k | 15.5k | 226 | Prices, debts and promises 16.4k bytes, Timeline 15.6k, Rulings 9.3k; 45 lines over 400 characters |
| `game-state.md` | 26.6k | 6.6k | 148 | Party status 8.5k bytes, Plan and standing orders 6.1k; 21 lines over 400 characters |
| `story/summary.md` | 20.3k | 5.1k | 85 | |
| PC sheet, down to its history | 21.6k | 5.4k | | whole sheet 31.5k bytes |
| Companion sheets, down to their history | 7.2k–14.1k | 1.8k–3.5k | | Kibil, Pip, Ixenna, Hala, Kole |
| Both player-options files | 10.7k | 2.7k | | |
| Session log tail (session 39, one scene) | 3.3k | 0.8k | | |

The feedback entry's resume (session 31) read about 73k tokens; this resume would read about 62k. The difference is
mostly `dm-notes.md` (94k → 82k bytes), which DMC trimmed in later full saves. It is still at 20.4k, the near-cap mark
below.

## R1. The estimate: bytes ÷ 4, of the bytes on disk

- **Decision:** A file's tokens are its size in bytes on disk divided by 4, rounded down, for exactly the part the
  resume reads. The report says so once. A part of a file is measured as the UTF-8 bytes of its lines, line ends
  included, as they are on disk (a CRLF file counts its CRs).
- **Rationale:** It is the method the issue and the review used, so the review's numbers and the command's agree. It
  needs no tokenizer, which `dmc` cannot ship (no network, Native AOT, nothing for players to install). The error is
  known and one-sided enough for a flag with a margin (R3).
- **Alternatives considered:** A real tokenizer: not available offline in an AOT binary, and the Read tool's own count
  is not public. Characters ÷ 4: nearly the same for English text and less honest about accented or non-Latin text,
  which costs more tokens per character, not fewer. Words × 1.3: no better, and harder to apply to tables.

## R2. Where the read rules live: the skill, mirrored in code and checked by a test

- **Decision:** `dmc-resume-campaign` step 2 stays the home of the read rules. The command implements the same rules
  in one class (`ReadSet`), and a toolkit test reads step 2 and checks that the markers the code relies on are still
  there, word for word: `^### Scene [0-9]+:`, the history headings (`## History`, `## Notes`,
  `## History with the PC`), "through scene", "With the party, not companions", **Rulings**, **Prices, debts and
  promises** and "Always read". A change to step 2 that drops one of them fails the build until the code changes too.
- **Rationale:** The rules are read by a model at every resume and by the program here; a model cannot run C#, and
  the program cannot read prose. The test is the cheapest way to keep two copies from drifting, which is the
  constitution's Principle III concern.
- **Alternatives considered:** Moving the rules into `dmc` and having the skill say "read what `dmc size` lists":
  shorter skill, but it makes the resume depend on the command running, and a resume that cannot list its files
  cannot start. Kept as a possible later change.

## R3. The near-cap flag: 20k estimated tokens, or 1,600 lines

- **Decision:** A file whose part read is 20,000 estimated tokens or more (80,000 bytes), or 1,600 lines or more, is
  flagged as near the Read cap.
- **Rationale:** The Read tool returns about 25k tokens or 2,000 lines in one call. The feedback's `dm-notes.md`,
  94k bytes (23.5k by the estimate), was cut, so the estimate undercounts by at least 7% on this kind of text. 20k
  leaves 20% under the cap; the 1,600 lines leaves the same share under the line limit. Today's `dm-notes.md`, at
  20.4k, is flagged, which is right: it is one session's growth from being cut again.
- **Alternatives considered:** 22.5k (90%): too close given a 7% undercount already seen. 15k: flags files that read
  fine.

## R4. Budgets: fixed numbers in `dmc`, starting points from the review, one exception from the measurements

- **Decision:** These budgets, in estimated tokens, fixed for every player:

  | What | Budget | Replaces |
  |---|---|---|
  | `game-state.md` | 4k | "under about 100 lines" |
  | `story/dm-notes.md` | 10k | "the whole file under about 150 lines" |
  | its **About this player** section | 2k | "under about 40 lines" |
  | its **Arc plan** section | 1.5k | "about 25 lines" |
  | `story/summary.md` | 3k | "under about 600 words" |
  | a PC sheet, the part read | 6k | "a sheet under about 150 lines" |
  | any other sheet, the part read | 4k | the same |
  | `story/canon.md` | the limit in R5 | "about 230 lines" |

  The numbers live in one place, a `Budgets` class in `dmc`, and the skills and templates stop stating sizes: they
  say that the full save checks them with `dmc size`. `.docs/campaign-files.md` lists them for readers, and
  `.docs/decisions.md` records why.
- **Rationale:** The review's starting points (dm-notes 10k, game-state 4k, summary 3k, a sheet 4k), approved with the
  analysis. One change from them: a PC sheet's read part is 5.4k on the long campaign and holds what a PC needs
  (features, spells, inventory), so 4k would flag a healthy sheet at every save, and a flag that cannot be fixed is
  noise (the reason the files that the full save does not trim have no budget). Companion sheets measure 1.8k–3.5k,
  under 4k. The two section budgets convert the existing line targets at about 200 characters a line. The summary's
  3k is looser than "600 words" (about 1k): the review chose 3k against a summary that was 5k, and the older target,
  never met, is what the review called a guess. Putting the numbers in `dmc` gives each one home and takes them out of
  the play context: the skills lose their numbers.
- **Alternatives considered:** Budgets as a share of the model's context window: `dmc` does not know the window
  (clarification). Budgets stated in the skills: a second home for each number, and words in play.

## R5. The canon limit: 19k to read whole, 16k to archive down to

- **Decision:** Resume reads canon whole while it is under 19,000 estimated tokens. When the full save finds it over
  19k, it archives lines whose subject can no longer come up until canon is under 16k.
- **Rationale:** The clarified rule: high enough that the long campaign's canon stays read whole. It is 15.5k now and
  the review expected about 17k once fight-story Timeline lines and moved standing orders reach it; 19k leaves about
  2k of headroom over that. 16k keeps the old ratio (200 ÷ 230 ≈ 0.87; 16 ÷ 19 ≈ 0.84), so a save's additions do not
  push canon straight back over.
- **Alternatives considered:** 15k, the issue's "behaviour stays the same" figure: it would switch the long campaign
  to the searched read as soon as the review's additions land, which the review rejected for consistency. 20k: the
  review's upper bound; 19k is chosen so canon plus `dm-notes.md` at budget stays under 30k, two reads well within one
  cap each.

## R6. Bloat flags: markers from the templates, kept to what is reliable

- **Decision:** The play form and the full report flag, each as one line per kind and file (with a count, not one line
  per match):
  - **dm-notes, Last played:** more than one line starting `Last played`, or a section named `Mix tracker`.
  - **dm-notes, Opposition:** table rows whose "How many (alive / started)" cell starts with `0 /` or `0/`.
  - **dm-notes, DONE or RESOLVED:** lines holding the word `DONE` or `RESOLVED` (upper case, whole word) outside the
    Arc plan, where a finished stage is legitimately one line marked done.
  - **dm-notes, Arc plan:** the section over its 1.5k budget (R4), which is how a done stage kept in full shows.
  - **canon, Prices, debts and promises:** the section over 3k, a sign that paid debts and kept promises were not
    deleted (on the long campaign it is 16.4k bytes, about 4.1k tokens, the largest section).
  - **long lines:** the number of lines over 400 characters in the content (under a `## ` heading, outside comments)
    of a file the full save trims. Not the preamble or comments, which hold the templates' guidance (the canon
    template's preamble is one 673-character line), and not a session log or digest, which is prose nobody splits.
- **Rationale:** Each marker is written by the templates or the full save, so a match is likely to mean what the flag
  says. Counting per file and kind keeps the play form to a few lines on a campaign with hundreds of matches (the long
  campaign has 69 long lines in one file). Detecting "a done stage that keeps more than its one line" by structure is
  unreliable in free text, so the section's size stands in for it.
- **Alternatives considered:** Flag overtaken plans: they have no marker. One line per long line: the long campaign
  would print 135 lines. Lowercase `done`: matches ordinary prose.

## R7. Two forms and an exit code: full report by hand, `--check` in play, 0 when measured

- **Decision:** `dmc size [NAME]` prints the full report. `dmc size [NAME] --check` prints only the lines to act on,
  at most 12 (then `... and N more: run dmc size NAME`), and nothing when all is well. Both exit 0 whenever the
  campaign was measured, flags or not; exit 1 with one line for a failure (no campaign, no DMC folder), exit 2 for a
  usage mistake.
- **Rationale:** DMC acts on what it reads, and the lines say what to do; silence means canon is read whole. A
  non-zero exit for a flag would show as a tool error in Claude Code and break the error-path guard's rule that a
  failure is one line (a flagged report can be several). The 12-line cap bounds the play cost on a badly overgrown
  campaign.
- **Alternatives considered:** Exit 1 when over budget (the review's first idea, so the save "cannot pass"): the
  save's rule (FR-015a) does that job in words, and a model reads output more reliably than an exit code. A `--json`
  form for issue #172: the migration's verification is `dmc` code and can call the library in-process.

## R8. Finding the campaign and the DMC folder

- **Decision:** The DMC folder is `--folder PATH` when given (as `dmc digest` takes it), else `DmcFolder.Require()`.
  The campaign is `NAME` when given, else the first line of `adventures/.active-campaign`. A name must be a folder in
  `adventures/` whose name does not start with a dot.
- **Rationale:** In play the current folder is the DMC folder, so `dmc size --check` is enough. `review-transcript`
  runs in this repository and already passes `--folder` to `dmc digest`. The dot rule matches resume step 1, which
  skips staging folders.
- **Alternatives considered:** Requiring `NAME` always: the full save and resume know it anyway, but the player
  asking "are your files too big?" should not need to.

## R9. Which sheets: Party status blocks and names in the scene

- **Decision:** Every `character-sheets/pc-*.md`; the sheet of each `### ` block under `game-state.md`'s
  **Party status** except **With the party, not companions**; and the sheet of each NPC whose name appears in the
  Scene block's **Present** line or in the **Combat tracker** section. A block or a name is matched to a sheet by the
  sheet's title (`# Name`): the whole title, its first word or its last word, as a whole word, case-insensitive, words
  under three letters not tried. A Party status block
  with no matching sheet is reported as not found. `minor-npcs.md` and the `<sheet>-history.md` files the full save moves old history to are never counted, and a
  Party status block in brackets (the template's `(character name)`) is a placeholder, not a companion.
- **Rationale:** These are resume step 2.5's rules. Matching on the first word of the title catches "Kibil" for
  "Kibil Rornson", and the last word catches "Anselm's shrine" for "Brother Anselm". A Present line can also list who is off scene, and the command cannot tell; counting such a sheet
  overstates the read, which errs toward a flag rather than away from one.
- **Alternatives considered:** Matching file names: sheets are named `npc-<slug>.md`, and slugs drop words. Parsing
  "Off scene:": that wording is one campaign's habit, not a template's.

## R10. Session log and module files

- **Decision:** The latest log is the `story/sessions/session-NNN.md` with the highest number. Its tail runs from the
  third-last line matching `^### Scene [0-9]+:` to the end; a log with fewer than three such lines counts whole. It
  also counts whole when the **Last full save** line in `game-state.md` does not end "through scene N", names an
  earlier session than the log's, or N is below the log's last scene. For a module campaign (`module-progress.md`
  exists), the command counts `module-progress.md`, the module's `index.md` (from **Module folder**), and the section
  named in **Digest to read before continuing** (a path under the module's `chapters/`, then the section name; the
  section is the heading that holds the name, to the next heading of the same or a higher level; the whole file when no
  heading matches, said so). When its **Chapter status** marks every chapter complete, only `module-progress.md`
  counts, as resume step 2.9 says.
- **Rationale:** Resume step 2.6 and 2.9, followed as written. `modules/` sits in the DMC folder, not the campaign.
- **Alternatives considered:** Leaving module files out (the issue's list omits them): the resume reads them, so the
  measurement would be short for every module campaign.

## R11. What changes in the prompts, and what it costs in play

- **Decision:**
  - `dmc-resume-campaign` step 2: before reading, run `dmc size <campaign> --check` once. Item 7 reads canon whole
    unless the output says canon is over its limit (replacing the Grep line count); a near-cap line means "read that
    file in parts if it comes back cut", and DMC tells the player in one line out of character. Other lines are for
    the full save and are not mentioned.
  - `dmc-campaign-full-save`: steps 1, 2, 4, 5 and 6 drop their line and word targets for "within the budget
    `dmc size` checks in step 7"; step 4 archives when `dmc size` says canon is over its limit, down to the mark it
    gives; step 7 runs `dmc size <campaign> --check`, fixes what it can and runs it again, and the save is complete
    only when it prints nothing or the step 8 report names what is left and why (FR-015a).
  - Templates `game-state.md`, `story/dm-notes.md`, `story/summary.md`: the "under about N lines/words" clauses go;
    one-fact-per-line and the other content rules stay.
  - `review-transcript` (this repository's skill): in § Context and tokens, run `dmc size --folder <dmc folder>
    <campaign>` and record the result with the date in the context entry.
- **Rationale:** One command replaces a Grep call at resume and two counts in the save, and the numbers leave the
  skills, so play reads fewer words than today. `kit/dm-rules.md` does not change.
- **Alternatives considered:** Running the command from a hook at session start: the hook runs before the campaign is
  known after a `/clear`, and its output would arrive before the resume needs it.
