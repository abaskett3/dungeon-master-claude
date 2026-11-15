# Research: A Slimmer Resume (issue #179, Phase D)

Every figure below was measured on 2026-11-15 on the maintainer's play folder `D:\Claude\dmc` (read only), campaign
Rorn's Landing, with `dmc size` from the repository build, unless it says otherwise. Token figures are bytes ÷ 4.
**Unverified** marks what was not measured or run.

## R0. What the resume reads today, and where the saving is

`dmc size rorns-landing`: 69.5k in all. Companions (the `### ` blocks under Party status other than "With the party,
not companions"): Kibil 3.8k, Kole 4.1k, Pip 3.7k, Ixenna 3.9k, 15.5k, each read down to its history. NPCs the Present
line names: Brunna 0.7k, Dessa 2.0k, Hala 3.9k, Tam 2.3k, Wenna 1.2k, 10.1k (left as they are: FR-006a). Canon 18.5k,
read whole under the 19k limit; its largest sections are Timeline 5.6k, Rulings 2.9k, People 2.7k, Prices 2.6k.

- **Decision:** cards for companions (R1) and canon read as always-read sections plus a ranked scene read (R3).
- **Expected:** companions from 15.5k to at most 4k; canon from 18.5k to about 8.4k (5.4k always read + 3k matches);
  the resume from 69.5k to about 48k (SC-001 to SC-003).

## R1. The play card is a section of the sheet, first after the title block

- **Decision:** a `## Play card` section, the first `## ` section of a companion's sheet. The resume reads the sheet
  from the top to the line before the next `## ` heading after it: the title, the short top fields (Role, Species,
  Usually found, First appeared, Status) and the card. Its fields (FR-001): who they are in a line; appearance in
  short; voice with one sample line; names they use; attitude to the PC and what they want now; AC, max HP, speed,
  saves, senses, each attack, recharge abilities; standing traits that change play; items used in play. The fight
  numbers move from `## Stat block` into the card, which keeps only what the card does not hold (the source creature,
  spells in full, the rest of the gear): one copy of each number (FR-002).
- **Rationale:** one file per character, so the sheet update and the full save edit one place; the sheet's existing
  `## ` headings already let `dmc size` and the resume find the card's end. On Kibil's sheet the card's fields come
  from lines 1 to 19 and 53 to 71 (`## Playing them`, `## Stat block`); a hand estimate of the card is 500 to 600
  tokens.
- **Budget:** `Budgets.PlayCard`, 1,000 tokens, for the part the resume reads (FR-003).
- **Alternatives:** a card file beside the sheet (two places to keep in step); a card in `game-state.md`'s Party
  status (`game-state.md` is already at its 4k budget); a card as a comment block (the resume could not tell where it
  ends without a heading).

## R2. When the full sheet is read

- **Decision:** `kit/dm-rules.md` line 78's last sentence, "Before a recurring NPC speaks at length, reread their
  sheet's Voice and Names they use lines.", becomes one sentence that covers FR-005: read the sheet, a companion's
  past its play card, before a recurring NPC speaks at length or about the past or what they know, or uses a feature
  or number the card does not hold. Level-ups and sheet changes are already `dmc-character-sheet-update`'s, which reads
  the whole sheet before it edits.
- **Rationale:** FR-028 allows no new line; these moments arise in play, where no skill is loaded, so the rule must be
  in `dm-rules.md`. Byte cost measured in the PR (target under 120 bytes).

## R3. Canon by scene: `dmc recall NAME --scene`

- **Decision:** a new form of recall. `dmc recall [NAME] --scene` takes no words: it builds them from `game-state.md`
  (the Scene block's Location, Present, What is happening and The player was about to; the Active quests; the Pending
  section; comments left out), ranks the lines of `story/canon.md` outside the always-read sections with recall's own
  `Rank` (BM25, one file, so one weight), and prints the best in recall's hit format within 12,000 bytes (3k tokens),
  with recall's closing `(N more lines matched)` line. Below `Budgets.CanonWholeBelow` (6,000 tokens) it prints one
  line, `story/canon.md is small: read it whole`, and nothing else.
- **The resume's item 7** becomes: run `dmc recall <campaign> --scene`; on the "read it whole" line, read canon whole;
  otherwise read the always-read sections whole (Grep `^## ` and Read each, as today) and take the command's lines as
  the scene's matches. The Grep of joined terms, with its escaping rules, goes.
- **`dmc size`** measures canon as the always-read sections plus the bytes the scene read prints, by calling the same
  code (FR-009, SC-004).
- **Rationale:** the unbounded search matches an estimated 12.3k of the 13.0k searched (**unverified**: the specify
  session's script, not a resume), because the party's names are in most lines. BM25's idf lowers exactly those words,
  so the lines about the scene's distinctive names, places and threads come first. One call, the same ranking DMC
  already trusts for recall, and a number `dmc size` can reproduce; DMC no longer hand-builds a regular expression.
- **The floor:** at 6k, the always-read sections and the cut already cost about what reading whole does; the 19k limit
  and the 16k archive mark stay as the full save's archive rule.
- **Alternatives:** a new verb `dmc canon` (a second retrieval verb for one scope); words passed by DMC
  (`dmc size` could not know them); recall's own 6,000-byte cap (about 1.5k, too few lines for a scene); the archive in
  scope (it holds what can no longer come up; recall and the hook reach it).

## R4. `dmc size` reads what the resume reads

- **Decision:** a companion's sheet with a `## Play card` is measured to the end of the card, as a new part kind "to
  its play card", against `Budgets.PlayCard`; one without a card is measured down to its history as today and flagged
  `character-sheets/<file>: a companion with no play card: the full save writes one`. Canon at or over
  `Budgets.CanonWholeBelow` is measured as R3 says, shown `read in sections and by scene`. The PC's and other NPCs'
  sheets are unchanged (FR-006a, FR-032).
- **Markers:** `ReadSet.Markers` gains `## Play card` and `--scene`, so the toolkit test that holds the resume skill to
  the code's markers covers the new rules.

## R5. The hook: `dmc context-check` adds the canon reminder

- **Decision:** the existing `UserPromptSubmit` command, `dmc context-check`, also prints canon lines for names in the
  player's message. `kit/settings.json` does not change. From the hook's JSON it reads `prompt` and `cwd` beside
  `transcript_path` (Claude Code's documented `UserPromptSubmit` input; **unverified** on this machine until the
  headless test, quickstart step 5). Steps: the active campaign from `<cwd>/adventures/.active-campaign`; nothing if
  there is none, the folder is missing or `.closed`, or the prompt starts with `/`; the register from canon's People
  entries (R6); the registered names in the prompt, whole words, ignoring case; less the names already reminded in
  this session part; for each name left, the 3 best lines (recall's `Rank` with the name's words) from canon outside
  the always-read sections and from `story/canon-archive.md`; one header line naming them, then the lines in recall's
  hit format; 4,000 bytes at most. The break reminder, when due, prints first.
- **Once per session part:** the hook's output is recorded in the log as a `hook_success` attachment whose `content`
  is the printed text (verified in log `0b0c4b6b`, where the break reminder appears so, at about 145 ms a call). The
  reader looks, after the latest compaction boundary (the same one `TranscriptReader` uses), for header lines
  `Canon on <names>` and drops those names. No state is stored.
- **Never fails:** `ContextCheckCommand.NeverFails` already turns any failure into silence and exit 0; the canon part
  runs after the break line is built and writes only when complete.
- **Rationale:** one hook command, so the play settings, the toolkit tests of the hooks and the per-message process
  count stay the same.
- **Alternatives:** a second hook command (a second process per message); a state file of injected names (stored
  state, and a write in a hook); injecting the always-read sections or the scene's lines too (already read at resume).

## R6. The register: names inside canon's People entries

- **Decision:** a People entry's names are every bold span before its first colon outside brackets, plus the items of
  a bracket that starts `(also: ` right after the first bold span, split at commas and semicolons:
  `- **Captain Emmajeen Kole** (also: Kole, Em, the captain): captain of the Pinebrook guard…`. A name matches whole;
  so does each word of a bold name of three letters or more that is not in `ReadSet.CommonWords` (titles and small
  words). `also:` items match only as written.
- **Rationale:** Rorn's Landing's People entries already start `- **Name**`, and some already have a bracket of other
  matter right after the name (`- **Rorn** (properly **Hysvearorn**, "Soaring Silver")`), so a bare bracket cannot be
  told from a note. `also:` keeps the user's choice (bold name, then the other names in brackets) and is unambiguous.
- **Who writes it:** the full save's canon step and the sheet update's new-character step write a new People entry in
  this shape; `dm-rules.md` gains nothing (FR-019).

## R7. Migration 006: play cards and People names

- **Decision:** `kit/migrations/006-play-cards-and-names.md`, standard 6. `touches: [character-sheets/,
  story/canon.md]`. `drops: []`. `checks: [companions-have-play-card, people-entries-named]`. Steps: for each
  companion block in Party status, write the card on its sheet from the sheet's own lines, moving the fight numbers out
  of `## Stat block` (each moved or shortened line `reworded`, anchored on the card line); then give each People entry
  a leading bold name if it has none, and an `(also: …)` bracket with the other names and role words the campaign's
  files already use for that person (nothing invented).
- **Checks:** `companions-have-play-card` fails on a companion block whose sheet has no `## Play card` as its first
  `## ` section; `people-entries-named` fails on a list item under canon's `## People` that does not start `- **`.
- **Golden fixture:** `tests/Dmc.Tests/Fixtures/migrations/006/` (before, after, accounting), as 002 to 005.
- **Rationale:** FR-011 to FR-014; the canon-reading change and the hook need no migration of their own.

## R8. The slip line and the gate

- **Decision:** the session-log template's scene comment gains one bullet, `Slip: what was wrong, and the right fact
  (canon|companion|hook|other)`; `dm-rules.md` line 85, which already says "the files win. Correct course…", gains
  "and log a `Slip:` line" in the same sentence. `review-transcript` gains a section, "The slip tally", with the gate's
  procedure (FR-020 to FR-027).
- **The baseline** is counted once, in this PR, from every session log of The Silver Egg (46) and Rorn's Landing (54
  today), with each slip's category, and recorded in `.docs/decisions.md` (the totals) and this folder's
  `baseline.md` (per session). A rough search finds slip wording in 19 of The Silver Egg's logs and 18 of Rorn's
  Landing's (**unverified** as slips until read).
- **Rollback,** per category (FR-026): canon, `ReadSet.Canon` and resume item 7 go back to the old rule; companion,
  resume item 5 reads companions down to their history again and `dmc size` measures them so; hook, the canon part of
  `context-check` is removed. Each is a small change with no migration.

## R9. Standing cost

- **Decision:** the PR reports, from `git diff --stat` and byte counts of each kit file before and after: every
  session (`dm-rules.md`, skill descriptions), each resume (the resume skill, the reads), each full save, each sheet
  update, the migration, each player message (the hook at its cap and typical). `dm-rules.md` stays at 212 lines and
  grows by at most 400 bytes; the toolkit test is tightened to 212 lines and gains the byte cap (FR-028).
