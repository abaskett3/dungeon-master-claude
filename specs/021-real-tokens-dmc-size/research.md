# Research: Real tokens in `dmc size`

All measurements were run on 2026-11-24. The scripts are kept with the slip analysis's evidence, in
`D:\Claude\dmc-slip-analysis\spec021\` (`fit.py`, `fit2.py`, `resume_all.py`, `count.sh`), and read the Claude Code
logs under `~/.claude/projects/` (`D--Claude-dmc`, `X--Claude-dmc`, `D--Claude-dmc-play-dungeon-master-claude`: 194
logs). The logs and campaign files are the player's and stay outside the repository.

## R1. The cost of a Read call, from the logs

**Question**: how much context does one batch of Read calls add, and how much of that is Read's line-number prefixes
and the call's record?

**Method** (`fit.py`, `fit2.py`): every batch of tool results between two API calls that holds only Read results, of
3,000 characters or more, with no system reminder and the same model on both sides: 611 batches (313 on Sonnet, 298
on Opus). Growth is the next call's context less this one's, less this call's output tokens. The fit is
growth = a × characters of the text (prefixes removed) + b × lines + c × calls. To test out of sample, the fit uses
the batches of every other log and the error is measured on the rest.

**Findings**:

- **The line numbers cost close to nothing.** With lines as a separate term, their coefficient is −0.99 tokens a line
  on Sonnet and 0.08 on Opus, so the prefixes are absorbed in the text's rate. Read's prefixes are about 2% of the
  bytes (`reviews/r1-evidence.md:34`) and their cost cannot be told from the text's at this precision.
- **Characters alone predict well.** Pooled: 0.398 tokens a character (2.51 characters a token) and 161 tokens a
  call. Out of sample, on batches of 10,000 characters or more (198): median error 2.1%, 90th percentile 4.8%. On
  batches of 3,000 to 10,000 characters (87): median 8.4%, 90th percentile 23.4%, where the per-call record is a large
  share and varies (130 on Sonnet, 203 on Opus).
- **Sonnet and Opus give the same text rate** (0.399 and 0.396 tokens a character).
- **Counting by character class** (letters, digits, spaces, punctuation, non-ASCII) lowers the small batches' error
  (median 5.4%) but not the large ones', and has outliers up to 274% where characters alone have 57%. Not worth a
  model that is harder to explain.

**Decision**: one rate, 0.4 tokens a character (2.5 characters a token), for every token figure; a Read call costs its
text's tokens and 160 more.

**Alternatives**: a rate per model (they are equal); a character-class model (rejected above); a real tokenizer
(none is published for these models, and a call to the token-counting API needs a key and a network, which
Development 3 rules out at run time).

## R2. The tokens of file text alone, counted for real

**Question**: does the rate of R1 hold for a file's text alone, outside a Read call (the unit of the budgets)?

**Method** (`count.sh`): with no API key in this environment (`env` has no `ANTHROPIC_API_KEY`), the count comes from
Claude Code's own usage figures: one headless call (`claude -p --output-format json`) whose prompt is a short
instruction followed by the file's text, less a baseline call with the instruction alone. Four files of the long
campaign (Rorn's Landing), copied to the scratchpad first, on Sonnet and on Opus.

**Findings**:

| File | Characters | Real tokens | Characters a token | 0.4 a character, error |
|---|---|---|---|---|
| `story/canon.md` | 78,283 | 30,698 | 2.55 | +2.0% |
| `story/dm-notes.md` | 33,959 | 12,969 | 2.62 | +4.7% |
| `game-state.md` | 15,574 | 6,698 | 2.33 | −7.0% |
| `character-sheets/pc-shalefire-stoutheart.md` | 32,999 | 13,762 | 2.40 | −4.1% |

The counts are identical on Sonnet and Opus, so the two use the same tokenizer. `game-state.md`, with its tables and
numbers, is the costliest: 0.43 tokens a character. The four files had no CR in their text as Python read it
(characters and UTF-8 bytes differ by 0 to 4).

**Decision**: the text's tokens are estimated at 0.4 a character, the same rate as R1. The spread of real files is
−7% to +5%; the margin of SC-003 (10%) holds.

## R3. The budgets in the new unit

**Question**: what number keeps each budget allowing the same text (spec Clarifications)?

**Finding**: the old measure is floor(bytes ÷ 4). The new one is 0.4 × characters, so a budget B in the old unit is
1.6 × B in the new one, exactly, with no rounding: every budget is a multiple of 5 (`Budgets.cs`).

**Rounding, so no file is newly over**: the new count is written as 2 × floor(characters ÷ 5). For a budget B, a part
is over in the new unit when 2 × floor(c ÷ 5) > 1.6 × B, that is c ≥ 4B + 5; it was over in the old one when
floor(b ÷ 4) > B, that is b ≥ 4B + 4. A part's characters c are never more than its UTF-8 bytes b (a UTF-16 unit
takes one to three bytes, and a surrogate pair four), so c ≥ 4B + 5 gives b ≥ 4B + 5: **every part over a budget in
the new unit was over it in the old one.** The same holds for the "at or over" checks (canon's limit and
read-whole decision: c ≥ 76,000 gives b ≥ 76,000). The other direction does not hold, and need not: a file with CRLF
line ends or non-ASCII characters measures a little smaller, so it trims less, never more. The count moves in steps
of 2 tokens, which no report shows.

**Line ends**: characters are counted as the text the Read tool returns, one character a line end; a CR before the
LF is not counted. Not verified: whether the Read tool keeps a CR. It does not matter for R3's proof, which needs only
c ≤ b.

**Checked at implementation** (quickstart Q2, 2026-11-24): the old and the new `dmc size` were run on the same frozen
copy of `D:\Claude\dmc` (the live folder changed during the run, as a game was being played) and on the 22 campaigns
of `D:\Claude\test-campaign`, comparing every flag line of the full report.

- No budget, canon or section flag was added.
- One near-cap flag was added: The Silver Egg's `story/dm-notes.md`, 54k characters, about 21.8k tokens of Read cost.
- One budget flag went: Rorn's Landing's `game-state.md`. It is 16,133 bytes, of which 140 are CRs and 2 belong to
  one em dash, so 15,991 characters: 6,396 tokens, 4 under the new 6,400, where its bytes were 33 over the old 4,000.
  CRLF campaign files on Windows measure about 1% smaller than their bytes, so a file that close to a budget is
  trimmed less, never more. The real count of the file is about 6.9k (R2's rate for this file), so the estimate's −7%
  on `game-state.md` is the larger part. The spec's US3-2 was corrected to say so.

| Member of `Budgets.cs` | Old (bytes ÷ 4) | New (tokens) |
|---|---|---|
| `GameState` | 4,000 | 6,400 |
| `DmNotes` | 10,000 | 16,000 |
| `DmNotesAboutThePlayer` | 2,000 | 3,200 |
| `DmNotesArcPlan` | 1,500 | 2,400 |
| `Summary` | 3,000 | 4,800 |
| `PcSheet` | 6,000 | 9,600 |
| `OtherSheet` | 4,000 | 6,400 |
| `PlayCard` | 1,000 | 1,600 |
| `CanonLimit` (and `CanonWholeBelow`, which equals it) | 19,000 | 30,400 |
| `CanonArchiveMark` | 16,000 | 25,600 |
| `CanonPricesSection` | 3,000 | 4,800 |
| `Legacy` | 3,000 | 4,800 |
| `ModuleSection` | 3,000 | 4,800 |

Not changed, because they are not in tokens: `SceneReadBytes` (12,000 bytes, now "about 4.8k tokens"), Recall's
output cap (`src/Dmc.Cli/Retrieval/Recall.cs:18`, bytes, now "about 2.4k"), `LongLineCharacters` (400),
`NearCapLines` (1,600), `CheckLines` (12). `ReadCapTokens` (25,000) was already in real tokens (R5).

**Alternatives**: keep the numbers in the new unit (rejected in the spec: every file would hold about 40% less text and
every full save would trim); round each new number to a whole thousand (rejected: rounding down makes files newly
over, and rounding up loosens budgets the review chose).

## R4. The resume's real growth, and the remainder

**Question** (SC-002): how much of a resume's context growth is the text its tool calls returned, and does the measure
match it?

**Method** (`resume_all.py`): for each log, from the first API call to the recap (the first call that writes over
1,500 characters of reply with no tool call), the growth of every batch with tool results against 0.4 × its
characters (Read prefixes removed) + 160 a result; the rest is DMC's own output and the batches with no tool result.

| Log | Resume growth | Tool batches' growth | Estimate | Error | The rest |
|---|---|---|---|---|---|
| `bf7609c5` | 112,382 | 108,345 | 110,172 | +1.7% | 4,037 |
| `082db04e` | 109,182 | 103,573 | 101,875 | −1.6% | 5,609 |
| `f8f2df71` (Opus) | 128,528 | 99,728 | 105,800 | +6.1% | 28,800 |
| `65839fe0` | 70,679 | 46,976 | 48,186 | +2.6% | 23,703 |

**Findings**:

- In the two logs SC-002 names, the text that tool calls returned is about 96% of the growth, and the measure matches
  it within 2%. The rest (4k to 6k) is DMC's own output. The skill text came before the first call (in the 36k
  baseline, `plan.md` § 4.1). In the other two logs the resume ran longer before its recap, so more of the growth was
  DMC's own replies.
- **Why `dmc size` said 57k**: the unit explains most of it (57k × 1.6 is about 91k, and 21 Read calls add about
  3.4k). The rest is that resumes read more than `dmc size` counts. In `bf7609c5`, the five companion sheets came back
  at 15.5k to 18.5k characters each (whole), where `dmc size` counts them through their play cards (about 1k each in
  the old unit); that is the 25k variation of the plan's § 4.1, for Slips 0.4. Not verified for each file of each log.
- **Expected total today**: the long campaign's 56.9k (`dmc size rorns-landing`, 2026-11-24) becomes about 95k,
  within #237's range of 88k to 124k. The exact figure is a quickstart check (Q3).

## R5. The Read cap and the near-cap mark

**Question**: what mark warns before a part is refused?

**Findings**:

- `Budgets.ReadCapTokens` is 25,000 and the decision of 2026-11-10 counts it as real tokens (a 94k-byte `dm-notes.md`,
  23.5k in the old unit, was cut: 94k × 0.4 is about 37.6k real). No log of the 194 holds the Read tool's
  "exceeds maximum allowed tokens" refusal for a file read (searched 2026-11-24), so the cap is taken from the
  existing decision, not re-measured.
- The old mark, 20,000 in the old unit, is about 32,000 real tokens: past the cap, so the flag came after the cut.
- The estimate is at most 7.5% under the real count on real files (R2, `game-state.md` at 0.43 a character). A mark of
  **18,000** in Read cost flags every part of 18,000 / (0.4 / 0.43), about 19,350 real tokens, or more: every part of
  20,000 real tokens is flagged (SC-005), and at least 5,600 real tokens remain under the cap, a few sessions of a
  file's growth.

**Canon**: canon is read whole below its limit, which in real tokens is 30,400, over the cap. That was already so in
the old unit (76,000 bytes is about 30k real), and `dmc-resume-campaign` step 7 already says to read canon "in two
reads by line range if the Read tool says it is too large" (`kit/skills/dmc-resume-campaign/SKILL.md:32`); `bf7609c5`
read it in two calls. A near-cap flag on canon would be a flag the full save is told to act on (`Flags.cs:89-92`, the
flag is "trimmed" for a file with a budget kind), so it would trim canon below its limit: the extra trimming the spec
rules out. **Decision**: canon within its limit gets no near-cap flag; over its limit it has the archive flag already.

**Other files the corrected mark reaches** (files over 44 KB in `D:\Claude\dmc`, 2026-11-24): module files and
chapters of `the-crooked-moon` (50k to 149k bytes), the sequel digest of `the-silver-egg` in Rorn's Landing
(`people.md` and `people-2.md` to `people-6.md`, 50k to 80k bytes; `world.md` 55k), and one session log of The Silver
Egg (85k, already over the old mark). `dmc module check` runs only in `dmc-ingest-module`, and the sequel digest's
check only at a sequel import, so play does not run them and no campaign stops working. A file over 25k real tokens
is already refused by the Read tool today and read in parts. A re-run of `dmc module check` on The Crooked Moon will
now report its large files: that is correct (they are past the Read cap) and is recorded in the decision's limits.

## R6. One home for the measure

**Question**: where do the bytes ÷ 4 live, and what replaces them?

**Finding** (grep of `src/` for `/ 4`, 2026-11-24): `Measurement.cs:16`, `:83`, `:86`; `Flags.cs:160`;
`ModuleCheck.cs:74`, `:91`; `SequelDigest.cs:258`; `Lookup.cs:143`; `CanonReminder.cs:69`; `SceneRead.cs:47`; `ReadSet.cs:485` (canon's read-whole test; its `dmc recall --scene` output is summed with `Recall.Bytes` at `:488`). Each
sums the bytes of lines, or a file's length on disk (`SequelDigest.cs:258`, `file.Length`).

**Decision**: `Measurement` gains the one conversion, `Measurement.Tokens(long characters)` = 2 × (characters ÷ 5),
and `Line` gains its character count; every caller sums characters and converts once. A toolkit test fails on any
`/ 4` next to a byte sum in `src/` (FR-001), with an XML doc comment that names this spec.

## R7. Tests that pin the numbers

**Finding** (grep of `tests/`, 2026-11-24): `ToolkitTests.TheDocumentedBudgetsEqualTheCode`
(`tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1543`) reads `.docs/campaign-files.md` against `Budgets`; it needs no change
of code, only the document's table. `SizeCommandContract.cs:65` and `:353` pin the header "in estimated tokens
(bytes / 4)". Fixtures that size a file to sit just under or over a budget (`tests/Dmc.Tests/Size/SizeFixtures.cs`,
`SizeLibraryTests.cs`, `Modules/ModuleCheckTests.cs`, `Retrieval/LookupLibraryTests.cs:194`,
`Retrieval/SceneReadTests.cs`, `ContextCheck/CanonReminderContract.cs`, `Campaign/SequelDigestTests.cs`) compute bytes
from the old unit. **Decision**: each fixture states its size through `Measurement.Tokens` or the inverse (characters =
tokens × 5 ÷ 2), so a later rate change moves them with it, and the edge tests of R3 (4B + 4 and 4B + 5 characters)
become unit tests of `Measurement`.

## R8. No migration

A campaign file's template, shape and allowed text do not change (R3), so a campaign needs nothing. `dmc update`
replaces the binary; the first `dmc size` after it prints new numbers for the same files and no new budget flag
(SC-004). The PR says so (FR-012).
