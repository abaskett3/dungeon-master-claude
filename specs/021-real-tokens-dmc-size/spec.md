# Feature Specification: Real tokens in `dmc size`

**Feature Branch**: `021-real-tokens-dmc-size`

**Created**: 2026-11-24

**Status**: Draft

**Input**: User description: "Issue #246 (Slips 0.3, part of #239, plan #238): make dmc size report real tokens
instead of bytes / 4 (src/Dmc.Cli/Size/Measurement.cs:16, :83, :86). First correct the flag for a file near the Read
tool's limit with the measured cost of a Read call (about one token per 2.3 to 2.6 characters in four logs, line
numbers and the tool record included). Then measure the tokens of file text alone for the budgets, and decide in the
spec whether the budgets keep their numbers. If they do, state what the full save does with the difference, since
trimming may cause the false sheet lines of #262 (not verified). A budget change changes what campaign files hold, so
it needs a migration or the reason none is needed (constitution Development 2). Update the toolkit tests that pin
budget numbers. Success: dmc size agrees, within a stated margin, with the context growth a Claude Code log shows after
a resume (two logs added 109k and 112k tokens; #237 measured 88k to 124k, median 106k; dmc size says about 57k). Amend
the decision "Size rules are measured in tokens by dmc size" in .docs/decisions.md. Evidence:
D:\Claude\dmc-slip-analysis."

## Background

Every token figure `dmc` prints or checks is the file's bytes divided by 4: the part read, the whole file and each
section in `dmc size` (`src/Dmc.Cli/Size/Measurement.cs:16`, `:83`, `:86`), and the same division in eight other places
(`src/Dmc.Cli/Size/Flags.cs:160`, `src/Dmc.Cli/Size/ReadSet.cs:485`, `src/Dmc.Cli/Modules/ModuleCheck.cs:74`, `:91`,
`src/Dmc.Cli/Campaign/SequelDigest.cs:258`, `src/Dmc.Cli/Retrieval/Lookup.cs:143`,
`src/Dmc.Cli/Retrieval/CanonReminder.cs:69`, `src/Dmc.Cli/Retrieval/SceneRead.cs:47`). Every number in
`src/Dmc.Cli/Size/Budgets.cs` is in that unit, except the ones that say they are bytes, lines or characters.

### What the logs show (`D:\Claude\dmc-slip-analysis`)

- **The cost of a Read call.** Batches of reads in four Claude Code logs cost one token for each 2.3 to 2.6 characters
  of tool result (`plan.md` § 4.1; `reviews/r1-evidence.md:34`: `bf7609c5` 2.42 to 2.58, `082db04e` 2.36 to 2.56,
  `f8f2df71` on Opus 2.35 to 2.56, `65839fe0` 2.33 to 2.46; script `reviews/work/ratio.py`). That figure is the growth
  of the context over a batch, so it holds Read's line-number prefixes and the call's own record as well as the file
  text (`reviews/r3.md:248-250`). The prefixes change the bytes by only about 2% (`r1-evidence.md:34`); their share in
  tokens is not measured.
- **The resume.** `dmc size` reports about 57k for the long campaign's resume files. Two resumes grew the context by
  about 112k (`bf7609c5`: 36,382 before, 148,764 at the recap) and 109k (`082db04e`: 37,008 before, 146,190 after).
  Issue #237 measured 88k to 124k over all resumes, median 106k. That growth also holds the resume skill's text, the
  `dmc` commands' outputs and DMC's own replies, not only the files.
- **Resumes read different amounts.** Three resumes of one campaign read the five companion sheets in three ways; the
  first and the third differ by about 25k tokens (`plan.md` § 4.1, `reviews/work/sheetreads.py`). A whole-resume
  figure therefore cannot match `dmc size` closely until resumes read the same lines; that is another step of the plan
  (Slips 0.4), not this one.
- **The near-cap flag fires late.** It fires at 20,000 estimated tokens, 80% of the Read tool's cap of about 25,000
  (`Budgets.cs:71-75`). At 2.4 characters a token, a part shown as 15k is already about 25k real tokens. The decision
  that set it already saw "a 23.5k file was cut" (`.docs/decisions.md:453`).

### The risk this spec must not create

If the budgets kept their numbers but were measured in real tokens, every file would hold roughly 40% less text, and
each campaign's next full save would trim to fit. The full save rewrites and shortens sheet lines to keep them in a
budget, and that is the probable source of the false sheet lines of issue #262 (not verified: no known false line has
been traced to a full save). The play cards already measure 792 to 987 of their 1,000 (issue #262).

## Clarifications

### Decisions made in this spec

- **The budgets keep the text they allow, not their numbers.** Each budget, limit and threshold that is in tokens is
  restated in real tokens of file text, so that it allows the same text it allows now (rounded up to a round number,
  never down). No file that is inside a budget today is outside it after the change, so no full save trims more, and
  the risk above is not created. Lowering or raising any budget is a separate decision, to be made against slip counts,
  and is out of scope. (The plan's measurement makes this exact: each number becomes 1.6 times its old value, see
  research R3.)
- **Canon is not flagged near the Read cap** while it is within its own limit. Its limit (now about 30k real tokens)
  is above the Read cap, as it already was in real tokens; `dmc-resume-campaign` step 7 already reads canon in two
  reads by line range when the Read tool says it is too large. A near-cap flag on canon would ask the full save to
  trim canon below its limit, which is the extra trimming this spec rules out. (Added at planning, research R5.)
- **No migration is needed** (constitution Development 2): no template, no file shape and no amount of text a campaign
  file may hold changes. The PR says so, and gives the check of SC-004 as the evidence.
- **Two measures, each where it belongs.** The flags about the Read tool's cap use the cost of a Read call (file text,
  line numbers and the tool record). The budgets use the tokens of file text alone. The report's resume total uses the
  cost of a Read call, since that is what the resume pays. (The plan measured the line numbers as close to free, so a
  Read call costs the text's tokens and about 160 more: research R1, R2.)
- **No new step for the player** (constitution Development 3): the count works offline, with no key, account or
  download. It is an estimate from the file's text, calibrated against real token counts during this work; it is not
  an exact tokenizer.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The near-cap warning comes before the file is cut (Priority: P1)

A campaign file grows until one Read call can no longer return it whole. Today `dmc size` warns only after the real
cost has already passed the cap, so the resume reads a cut file and DMC plays from half of it. After the change, the
warning appears while the file still fits, with enough room for one more session's growth.

**Why this priority**: A cut file is the one size problem that silently loses facts at resume. The issue names it as
the first fix.

**Independent Test**: Measure the near-cap flag on files whose real Read cost is known: one under the cap by a
session's margin, one over it.

**Acceptance Scenarios**:

1. **Given** a file the resume reads whose Read call costs about 20,000 real tokens (about 48k characters at the
   costliest measured rate), **When** `dmc size --check` runs, **Then** it flags the file as near the Read cap. Today
   it does not (48k bytes ÷ 4 is 12k, under the 20k mark).
2. **Given** a file whose Read call costs well under the mark, **When** `dmc size --check` runs, **Then** it is not
   flagged near the cap.
3. **Given** a module file or an ingested section near the cap, **When** `dmc module check`, `dmc lookup` or the sequel
   digest check runs, **Then** each uses the same Read cost measure and the same mark as `dmc size`.

---

### User Story 2 - The resume total says what the resume really costs (Priority: P1)

The player and each plan read `dmc size`'s total as the cost of a resume. Today it says about 57k where the resume
really adds about 110k, so a plan that trims "to 50k" aims at half of the real figure. After the change, the total is
the real cost of reading those files, within a stated margin, and the report says what the total leaves out.

**Why this priority**: Every plan of the slip work uses this number (issue #237's "trimmed set of about 50k" was
computed with it).

**Independent Test**: Take a Claude Code log of a resume, find the Read calls of the read set, and compare their
context growth with what `dmc size` reports for the same text.

**Acceptance Scenarios**:

1. **Given** the text a log's Read calls returned for the resume files, **When** `dmc size`'s measure is applied to it,
   **Then** the result is within the margin of SC-001 of the context growth those calls caused.
2. **Given** a campaign, **When** `dmc size` prints its report, **Then** the total line says it counts the files read,
   and that the resume also pays for the skill text, the commands' outputs and the replies.

---

### User Story 3 - The budgets measure real tokens and hold the same text (Priority: P2)

A full save checks each file against its budget and trims what is over. After the change the budgets are in real
tokens, so the numbers mean what they say, but a file that fits today still fits, and the full save trims nothing it
would not trim today.

**Why this priority**: Correct numbers are worth having only if they do not set off a round of trimming in every
campaign, which may write false lines (#262).

**Independent Test**: Run `dmc size --check` on every campaign at hand with the old and the new measure and compare
the budget flags.

**Acceptance Scenarios**:

1. **Given** a campaign whose files are all inside their budgets today, **When** `dmc size --check` runs after the
   change, **Then** no budget flag appears that did not appear before.
2. **Given** a file over its budget today, **When** `dmc size --check` runs after the change, **Then** it is still over
   it, unless it is within about 1% of the budget and its CR line ends or non-ASCII characters make the difference.
   Those count as bytes today and not as characters after the change, so such a file is trimmed less, never more
   (research R3). (Corrected at implementation: the comparison of quickstart Q2 found one such file.)
3. **Given** canon at a given size, **When** the resume decides whether to read it whole, or the full save whether to
   archive it, **Then** the decision is the same as before the change, or, by the same 1%, canon is read whole a little
   longer.

### Edge Cases

- **A file near a budget's edge.** Rounding a restated budget up may let a file that was a few tokens over now be
  inside. That is allowed (it trims less, never more); a file inside before is never outside after.
- **Text that is not English prose.** Tables, dice notation, numbers, names and non-ASCII characters cost more tokens a
  character than prose. The calibration uses real campaign files, which hold all of these; the stated margin covers
  what is left.
- **A different model.** The four logs give the same range on Sonnet and Opus. If a model DMC plays on counts tokens
  differently, the figures drift; the decision's limits say how to measure again.
- **An empty or missing file** still counts 0.
- **Byte caps that are not token figures** (`SceneReadBytes`, Recall's output cap, `LongLineCharacters`, `NearCapLines`)
  are in bytes, characters or lines and do not change.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Every token figure `dmc` prints or checks MUST come from one measure with one home, replacing each
  bytes ÷ 4 named in the Background. No other code divides bytes into tokens.
- **FR-002**: The measure MUST offer two figures for a text: the tokens of the text alone, and the cost of reading it
  with one Read call (the text, Read's line-number prefixes and the call's record).
- **FR-003**: Each flag about the Read tool's cap (`dmc size`'s near-cap flag, `dmc module check`, `dmc lookup`'s
  read-in-parts line, the sequel digest's "too large" check) MUST compare the Read cost with the mark. The mark MUST
  leave room for one session's growth below the cap at the costliest rate the logs measured (2.3 characters a token).
  Canon within its limit is not flagged near the cap (Clarifications).
- **FR-004**: Each budget, limit and threshold in tokens (`Budgets.cs`: `GameState`, `DmNotes`,
  `DmNotesAboutThePlayer`, `DmNotesArcPlan`, `Summary`, `PcSheet`, `OtherSheet`, `PlayCard`, `CanonLimit`,
  `CanonWholeBelow`, `CanonArchiveMark`, `CanonPricesSection`, `Legacy`, `ModuleSection`) MUST be restated in real
  tokens of file text, allowing the same text as now, rounded up to a round number and never down.
- **FR-005**: The budgets MUST be compared with the tokens of file text alone, not the Read cost.
- **FR-006**: The report's per-file and per-section figures MUST be the tokens of file text (the unit of the budgets);
  the resume total MUST be the Read cost of the files read, and the report MUST say which is which and what the total
  leaves out.
- **FR-007**: `dmc` MUST count tokens offline, with no key, account, network call or download, and install and
  `dmc update` MUST gain no step.
- **FR-008**: The calibration (the files and logs it used, the real counts, the ratios and their spread) MUST be
  recorded in the feature's research, so that it can be measured again for another model.
- **FR-009**: The toolkit and unit tests that pin budget numbers or bytes ÷ 4 figures (among them the test that keeps
  `.docs/campaign-files.md`'s budget table equal to `Budgets.cs`, and the tests under `tests/Dmc.Tests/Size/`,
  `Modules/`, `Retrieval/`, `Campaign/SequelDigestTests.cs` and `ContextCheck/CanonReminderContract.cs`) MUST be updated
  to the new measure and numbers, and a test MUST check FR-001 (no bytes ÷ 4 left).
- **FR-010**: The documents that state the unit or the numbers MUST be updated: `.docs/campaign-files.md` (the budget
  table and "bytes ÷ 4"), `.docs/dmc-reference.md` § size, `dmc size`'s help text, the XML comments of `Budgets.cs` and
  `Measurement.cs`, and any kit text that states a token figure (for example "near 20k tokens" in
  `kit/skills/dmc-new-campaign/SKILL.md:47`).
- **FR-011**: The decision "Size rules are measured in tokens by `dmc size`" in `.docs/decisions.md` MUST be amended:
  the measure, the two figures, why the budgets keep their text and not their numbers, why no migration is needed, and
  the limits (an estimate, calibrated on named models).
- **FR-012**: The PR MUST state that no migration is needed and why (constitution Development 2), with SC-004's result.

### Key Entities

- **Text tokens**: the estimated tokens of a file's text alone; the unit of every budget.
- **Read cost**: the estimated context growth of reading a part with one Read call; the unit of the near-cap flags and
  the resume total.
- **Calibration**: the measured relation between a file's text and its real token counts, with its spread, recorded
  with its sources.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For each Read batch of the resume in at least the four logs of the ratio study, the Read cost `dmc`
  computes from the text those calls returned is within 10% of the context growth the log shows for that batch.
- **SC-002**: For the two logs `bf7609c5` and `082db04e`, the measure applied to the text the resume's tool calls
  returned is within 5% of the resume's context growth less DMC's own output, and the remaining growth is measured and
  stated in the research. (Planning measured +1.7% and −1.6%; tool results were about 96% of the 112k and 109k.) On
  the long campaign today, `dmc size`'s total is within the range #237 measured (88k to 124k), no longer about half of
  it. (Refined at planning: the logs hold the text each call returned, so no files need restoring; research R4.)
- **SC-003**: On real campaign files, the text-token estimate is within 10% of a real token count for each file of the
  long campaign's read set over 1,000 tokens.
- **SC-004**: On every campaign at hand (`D:\Claude\dmc` and `D:\Claude\test-campaign`), `dmc size --check` prints no
  budget or canon flag after the change that it did not print before. Only near-cap flags may be added. A flag may go
  only as acceptance scenario US3-2 says.
- **SC-005**: A part other than canon within its limit whose Read cost is 20,000 real tokens or more is flagged near
  the cap, and no file in the logs that
  was cut by the Read tool would have gone unflagged.
- **SC-006**: CI passes, and no code divides bytes by 4 to get tokens.

## Assumptions

- A real token count of text is available to the developer during this work, to calibrate the measure: the token
  counting of the Claude API, or failing that a fit from the logs, where the Read results' text and the context growth
  are both recorded. Which one is a planning question; neither is used at run time. Not verified: no API key is set in
  this environment.
- The Read tool's cap is about 25,000 real tokens of tool result per call (`Budgets.ReadCapTokens`), as the existing
  decision states; this spec does not re-measure it.
- One calibration serves the models DMC is played on now (Sonnet and Opus, which the four logs show give the same
  range). A model with a different tokenizer would need a new calibration.
- The figures are compared against the logs' tool-result text, which the logs hold, so the check of SC-001 needs no
  restored campaign files; SC-002 needs the files as they stood, from Claude Code's file history or the logs' Read
  results.
- Whether the budgets should be lower or higher is out of scope: they stay where they are in the text they allow.
  Making resumes read the same lines (Slips 0.4) is out of scope.
- Every number above with a file and line was read in the code or the evidence folder on 2026-11-24; the 2% share of
  line numbers in bytes and the cause of the false sheet lines are taken from the analysis, as it marks them.
