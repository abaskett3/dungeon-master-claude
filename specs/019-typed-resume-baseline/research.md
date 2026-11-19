# Research: The break baseline after a typed resume

All file and line references were read on branch `019-typed-resume-baseline` at `68c3396` (`origin/main`).
The Technical Context has no open question. This file records each design choice and what it rests on.

## R1. What a resume call looks like in the log

**Decision**: A resume call is a record with `"type": "assistant"` and no truthy `isSidechain`. Its `message.content` is
an array that holds a block with `"type": "tool_use"`, `"name": "Skill"` and an `input.skill` that is exactly
`dmc-resume-campaign` or `resume-campaign`. An `args` field may also be present and does not matter.

**Rationale**: Log `082db04e` lines 38 and 42 hold exactly `"name":"Skill","input":{"skill":"dmc-resume-campaign"}`,
both with `"isSidechain":false`. The same log's call to `dmc-record-feedback` shows that an `args` field can come with
the skill name. `resume-campaign` is the name from before the rename. The present tests already
accept it for the slash command (`ContextCheckTests.cs:141-149`).

**Alternatives considered**: matching any skill whose name ends in `resume-campaign` was rejected, because a plugin
could carry a name like that. Matching the words "Resume campaign" in the player's message was also rejected: players
use other words, and the call is what loads the files.

## R2. Where the check runs

**Decision**: The check goes into the forward pass that `TranscriptReader.BaselineOf` already makes
(`TranscriptReader.cs:156-201`), as a separate helper beside `Classify`. `Classify` itself does not change.

**Rationale**: `Classify` is shared by four scans: `LatestContext`, `OfferedSince`, `BaselineOf` and
`SheetReminder.Seen` (`SheetReminder.cs:247`). Widening its result would make each of them parse more. The helper
is cheap. It runs only while the pass is inside the first three turns of a log part, and it parses only lines that
hold the bytes `resume-campaign` or a `\u` escape. This follows the rule that `CouldBeRelevant` keeps
(`TranscriptReader.cs:389-407`): a byte test may say no only to a line that cannot match.

The helper does not need the record's usage. A tool-use record whose usage is zero still marks its turn. The
baseline comes from the last call of that turn, which `Classify` already gives.

**Alternatives considered**: a second pass over the log was rejected, because it costs a second read of the file
on every message.

## R3. Bookkeeping within a log part

**Decision**: The pass keeps four values for the current log part. A compaction boundary resets all of them.

- The number of player messages seen, counted up to 4.
- Whether the current turn holds a resume call.
- The context of the last call in the current turn.
- The resume baseline: 0, or the last call of the latest resume turn.

The current turn closes on the next player message, or at the end of the log. If it held a resume call and is among
the first three turns, its last call becomes the resume baseline. A later resume turn among the first three
overwrites the value (FR-002). The pass returns the resume baseline when it is greater than 0. Otherwise it returns
what the present method gives (FR-003).

**Rationale**: This is the smallest change that keeps the present method intact in every other case. The two
existing variables `before` and `baseline` keep their meaning.

**Alternatives considered**: taking the larger of the present baseline and the resume baseline was rejected. The
rule in the issue is that the resume turn sets the baseline, and a typed resume's present baseline is the low value
that this change exists to replace.

## R4. What "settled" means for the cheap skip

**Decision**: Today the pass stops classifying lines once the baseline is known. After that it looks only for lines
that could be a compaction boundary (`TranscriptReader.cs:166-168`). With this change the baseline is settled only
when both hold: the present baseline is known, and the fourth player message of the part has been seen. If no
fourth message has come yet, the end of the log settles it.

**Rationale**: A resume call can come up to the third turn, so the pass cannot skip lines before that. The extra
cost is the classification of at most three turns. In a slash-command session those turns are short play turns. In
a typed-resume session the first turn holds the resume's tool results, which the present pass already classifies
when the slash command puts them before the first message. The cost therefore stays close to what was measured:
72 ms against 51 ms for a run with no log, on an 18.3 MB log (`.docs/dmc-reference.md:827`). Quickstart step 4
measures it again (SC-004).

## R5. No sheet lines before DMC's first reply

**Decision**: `SheetReminder.For` returns `Found.None` when the hook input names a log and `contextNow` is 0.
`ContextCheckCommand.Run` passes `reading.Size` as `contextNow` (`ContextCheckCommand.cs:57`).
`TranscriptReader.LatestContext` gives 0 exactly when the log has no assistant call after its latest compaction
boundary (`TranscriptReader.cs:132-146`).

**Rationale**: The size is already read before the sheet part runs, so the check costs nothing. A log that is missing
or cannot be read makes `TranscriptReader.Read` throw, and the whole command then prints nothing
(`ContextCheckContract.cs:185`, `NothingForAMissingLog`). So "a log is named and the size is 0" means "the log was read and has
no reply since the boundary". A hook input with no log keeps today's lines. The sheet tests depend on that: most of
them pass no `transcript_path` (`SheetReminderContract.cs:57-58`), and `ACompactionStartsAfresh` has a call after its
boundary (`:190-195`), so their expected results stay the same (SC-003).

The `contextNow == 0` branch of `SheetReminder.Recent` (`:202-207`) can then no longer be reached from `For`,
because `Seen` runs only when a log is named. `Seen` keeps its own contract, and only the doc comments change.

**Alternatives considered**:

- Suppressing the lines only after `/clear` and not after a compaction was rejected. `kit/dm-rules.md:202` tells DMC
  to re-run the resume after a compaction, which reads the sheets.
- Suppressing them only on the turn that holds a resume call was rejected. The hook runs before that turn, so it
  cannot know.

## R6. The limit after an automatic compaction

**Decision**: The documents state the effect as unverified. If DMC reloads after a compaction with a `Skill` call of
the resume in one of the next three turns, that turn now sets the baseline. The limit written at
`.docs/dmc-reference.md:825` then no longer applies in that case. The limit is "the baseline is the small first
reply and the 150,000 floor applies".

**Rationale**: Plan F found one compaction in 114 play logs, with no player message after it (#238). So no log shows
how DMC reloads after one. The documents must not claim the fix covers this case.

## R7. A slash resume after a typed message (not adopted)

A player could type "hi", then run `/dmc-resume-campaign` as the second thing they send. The slash command's echo is
not a player message (`UserText.IsTyped`), and it produces no `Skill` tool-use record, so FR-001 does not see it. The
baseline would stay at the first reply. Counting the echo `<command-name>/dmc-resume-campaign</command-name>` in the
first three turns would cover this at no cost. It is outside the rule of issue #244, so it is not adopted here. It is
raised with the maintainer as a possible addition to FR-001. It needs a one-line spec change and one test.

## R8. Effect on the break line's other parts

`BreakReminder.For` and `BandOf` take the baseline as given, so they do not change (FR-004). `OfferedSince` reads the
band from the same baseline, so one offer per band holds at the new bands. `CanonReminder` receives
`sheets.WithoutFields`, which is empty on a message that gets no sheet lines. On that message it therefore covers
only the people the message names. This matches the spec's FR-005.

## R9. Tests

The new tests go into `ContextCheckTests.cs`, which holds the baseline tests (`:123-245`), and into
`SheetReminderContract.cs`. They need no external tool, so no `TestEnv.Need` (Development 7). The log fixtures are
built with the existing helpers `Assistant`, `User`, `Command`, `ToolResult` and `Boundary`. One new helper writes an
assistant record with a `Skill` tool-use block and can mark it as a sidechain record.
