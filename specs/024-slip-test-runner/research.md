# Research: The slip test runner

All probes ran on 2026-11-24 in a cloud container, Claude Code 2.1.331 on Linux, model Haiku, in a scratch folder
with a two-line `CLAUDE.md` and one text file. Nothing here quotes play. The maintainer's logs are on Windows and may
be from another Claude Code version: the pilot (FR-007) is what checks them.

## R1. An edited copy of a log can be continued, and the model sees the edit

- **Probe**: run `claude -p "Use the Edit tool to change notes.txt so it says bye."`; copy its log
  (`~/.claude/projects/<slug>/<id>.jsonl`) to a new name with a new `sessionId` in every record and a sentence added to
  the first user message ("the secret word is PELICAN"); run
  `claude -p --resume <new id> --fork-session "What is the secret word? Answer with one word."`.
- **Result**: the reply was `PELICAN`. The fork got a third session id and its own log, which holds a copy of the whole
  history and then the new turn. Cost: $0.0040 for the base run, $0.0046 for the fork (Haiku, 1M context, most input
  read from the cache).
- **Decision**: fork mode writes the base log itself (truncated, with a new id) and resumes it with `--fork-session`,
  so the base log the runner wrote is never changed by the run. The project folder's name (the slug) is the working
  directory with every character that is not a letter or a digit replaced by `-`: `/tmp/claude-0/-home-user/x/probe`
  became `-tmp-claude-0--home-user-x-probe`.

## R2. What the log records of a turn

- **Probe**: the record types of the two logs above, and of this session's own log.
- **Result**: one JSON object per line. `user` and `assistant` records carry `uuid`, `parentUuid`, `sessionId`, `cwd`,
  `timestamp` and `message` (`content` is a string, or a list of blocks of type `text`, `thinking`, `tool_use`,
  `tool_result`). `assistant` records also carry `message.model`, and in this version `effort`; `user` records carry
  `permissionMode`. What Claude Code adds around a turn is a separate `attachment` record, with `attachment.type` naming
  it: `environment`, `model`, `skill_listing`, `date`, `deferred_tools_delta`, `hook_additional_context`,
  `total_tokens_reminder`, `thinking_drop` and others. The project's `CLAUDE.md` is recorded once, as an attachment of
  type `instructions` whose `files[]` hold `path`, `type` and `content`.
- **Result**: the fork log records the attachments of the replayed turn after its user record (`environment`,
  `total_tokens_reminder`, `thinking_drop` in the probe).
- **Decision**: the notice check (FR-007 a) compares the attachment types of the replayed turn with those of the
  original turn (the records after the fork message, up to the next message the player typed), and the meta user
  records (`isMeta: true`) by their first line. The hook check (b) compares the text of every attachment whose type
  starts with `hook_`. The rules rewrite (FR-003) replaces `content` of the `instructions` file whose `path` is the DMC
  folder's `CLAUDE.md`; if a log has no such attachment (an older version), it replaces the recorded text wherever it
  occurs as a whole string value, and the pilot's check (c) shows whether it reached the request.
- **Not verified**: the maintainer's version may record a notice in another shape. The pilot reports any record type
  that the replayed turn has and the original turn has not.

## R3. A rewrite before a thinking block drops that block

- **Probe**: the fork of R1, whose history was edited before the base run's thinking block.
- **Result**: the fork log has a `thinking_drop` attachment: `thinkingBlocksSent: 1`, `newlyDropped.blockCount: 1`,
  reason `prefix_mismatch`. A thinking block's signature covers what came before it, so an edit earlier in the
  conversation makes Claude Code drop the block from the request.
- **Decision**: this is why #249 asks for an equivalent rewrite of the control and for the same number of thinking
  blocks in both arms. The control's rules file must differ from the recorded text (the present `kit/dm-rules.md`
  does, for any log older than the last kit change), or the control keeps blocks the treatment loses. The pilot's check
  (d) compares `thinkingBlocksSent` and the dropped count of each arm at each point. If the log has no `thinking_drop`
  record, the check reports "not recorded", which passes only when the allow file documents it.

## R4. Restoring the campaign files to the fork time

- **Source**: Claude Code keeps a backup of a file before the first edit in each turn, in
  `~/.claude/file-history/<session id>/<backup file name>`, and records it in a `file-history-snapshot` record of the
  session's log, `snapshot.trackedFileBackups` mapping the file's path to `backupFileName` (null when the file did not
  exist), `version` and `backupTime`. Claude Code's rewind uses these. **Not verified here**: this container keeps no
  file history for `-p` runs, so the format comes from the maintainer's logs in the pilot. The parser accepts absolute
  paths and paths relative to the session's `cwd`.
- **Decision**: a file's content at time T is the content of its first backup with `backupTime` after T, in any
  session of the DMC folder: that backup was taken before the first edit after T, so nothing changed the file between
  T and it. A file with no such backup has not been changed through Claude Code's tools since T, so its present content
  is right. A null `backupFileName` means the file did not exist then.
- **Limit**: a change made with a shell command leaves no backup. Spec 020 denies the usual shell writes; older logs
  hold some (#238).

## R5. The interval

- **Decision**: runs from one base session are not independent, and slips come in groups (#240 rule 4), so the unit
  is the fork point. The rate of an arm is slips over runs; its 95% interval is the 2.5th and 97.5th percentile of the
  rate over 10,000 bootstrap samples of the fork points (with replacement, seed 249). The difference between two arms
  is computed on the same resampled points, so it is paired. A pass line is a bound on that difference, written in the
  trial file before the run.
- **Alternatives**: a Wilson interval on runs treats runs as independent, which they are not; a mixed model needs a
  package and more points than a pilot has.

## R6. Language and place

- **Decision**: C#, a console project `src/Dmc.SlipTest` with assembly name `slip-test`, in `dmc.sln`, run with
  `dotnet run --project src/Dmc.SlipTest -- <command>`. Tests in `tests/Dmc.Tests/SlipTest/` on invented logs. The
  release workflow publishes only `src/Dmc.Cli`, so the program is never released.
- **Alternatives**: a PowerShell script beside `tools/recall-test.ps1`. The runner is about ten times its size and its
  parsing and statistics need tests in CI; the repository tests C#.

## R7. A dry run of the runner

- **Probe**: a two-line `CLAUDE.md` and one campaign file in `/home/user/slipdemo/DMC`, two turns of `claude -p` with
  Haiku, then `prepare`, `run --pilot` and `pilot` on the second message, with a control and a treatment rules file.
- **Result, first pilot**: fail. The replayed turn had two attachments the original turn had not: `instructions`
  (`changed: true`, `reason: session_start`), because the runner recorded the arm's text with its trailing newline
  and Claude Code records the file without it; and `environment`, whose only change was the scratchpad folder, which
  a cloud session names after the session id.
- **Decision**: the runner records the rules without trailing white space (`LogRewriter.Recorded`). The scratchpad
  change exists only in a cloud session and is documented in the dry run's `allow.tsv`; the maintainer's pilot shows
  whether a local session has it.
- **Result, second pilot**: pass (notice 2 documented, hook 2, rules 2, thinking 1: one block sent and one dropped
  in each arm). The full run, export and score worked end to end; the copy's Claude Code project folder was left
  empty. A fork run cost $0.004 to $0.0065 with Haiku on this tiny folder.
