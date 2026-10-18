# Decisions

This log is for contributors who want to know why the toolkit is the way it is before changing it. Each entry gives the decision, the date, the reason and where it was made. Dates are UTC, taken from GitHub. Newer entries are at the end; an entry that a later one replaced says so.

## .NET 10 Native AOT on every platform, Linux built on Ubuntu 22.04

- **Date:** 2026-10-14
- **Decision:** Replace the Python scripts and the two shell dice rollers with one .NET 10 program, `dmc`, compiled with Native AOT for each platform. Build Linux on Ubuntu 22.04. The floor for players is glibc 2.34. The self-contained single-file build stays as the local developer default only.
- **Reason:** Players then need nothing installed but Claude Code: no Python, no pip, no .NET runtime. .NET 10 is a long-term-support release. The Phase 0 spike found no AOT or trim warnings with the chosen libraries and start-up of 5 to 29 ms; single-file worked but started 15 to 20 times slower, which matters for a hook that runs before every message. An AOT binary runs only on the same or a newer Linux than it was built on, so Linux is built on the oldest Ubuntu runner, 22.04, which ships glibc 2.35. The 2.34 floor is not the runner's glibc but the newest glibc symbol version that `dmc` and its native libraries use, measured in the spike.
- **Source:** Issue #3 (plan and "Verified facts"), and its Phase 0 decision comment.

## One-line errors, and output kept small

- **Date:** 2026-10-14
- **Decision:** Every `dmc` failure is one line, `dmc <command>: <message>`, never a stack trace or the usage text. Output is UTF-8 with no byte-order mark and LF line ends. Each port kept its script's output exactly, apart from deliberate, listed differences.
- **Reason:** Everything `dmc` prints lands in DMC's context and is paid for in tokens. Identical output meant DMC read no more than before, and the prompts and tests depending on it kept working.
- **Source:** Issue #3 ("Keep errors to one line"); PR #8.

## Map scales of 5 ft and 30 ft, plus miles for overland maps; no 10 ft

- **Date:** 2026-10-14
- **Decision:** Every map is drawn to scale. `cell_ft` must be 5 (the default, the 5e grid), 30 (one turn of movement at speed 30, for fights and places too large for 5 ft squares), or 5280, 31680 or 316800 (1, 6 or 60 miles a square) for overland travel maps. `null`, 10 ft and any other value are refused. No rule ties token kinds to scales; that a fight never uses a mile scale is guidance in the `draw-map` skill.
- **Reason:** Sketch maps without a scale led to wrong distances in play and had to be redrawn mid-campaign. The plan in issue #3 expected a 10 ft scale; the user chose 5 ft and 30 ft instead, so both work for combat, and 30 ft is a choice made for this toolkit rather than a published scale. The mile scales follow the province, kingdom and continent scales of the Dungeon Master's Guide.
- **Source:** PR #5 and PR #7 (the pre-port step of issue #3).

## Map PNGs at 1:1

- **Date:** 2026-10-15
- **Decision:** `dmc map --png` draws the PNG at exactly the SVG's size. The Python renderer drew it at 4/3 of that.
- **Reason:** The PNG exists for DMC to look at, and an image costs tokens by its pixels. This was the one deliberate difference in the `map` port.
- **Source:** PR #9 ("user decision, to save image tokens").

## Intel Macs not built

- **Date:** 2026-10-15
- **Decision:** Releases are built for `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64` and `osx-arm64`, not `osx-x64`.
- **Reason:** The plan made Intel Macs optional; it would be the one target that neither the tests nor the spike ever ran, and the Intel runner image is the last of its kind. Adding it later is one matrix row.
- **Source:** PR #14 ("RIDs").

## The release opens its own pin pull request

- **Date:** 2026-10-15
- **Decision:** Pushing a `dmc-vX.Y.Z` tag on `main` is the only thing that publishes. The release workflow builds and installs every archive on its own platform before publishing, then writes `tools/dmc-release.json` from the published archives and opens the pull request that pins them. `get-dmc` checks every download against that pin's SHA-256 before running anything, and the launchers start Claude Code even when `dmc` cannot be had.
- **Reason:** A release and its pin cannot drift apart through a forgotten manual step, and a mismatch fails loudly instead of running the wrong program. Play must never depend on a download succeeding.
- **Source:** Issue #3 ("Build and release", "How a player gets `dmc`"); PR #14 and PR #15.

## Git rules for toolkit work in CLAUDE.md

- **Date:** 2026-10-15
- **Decision:** `CLAUDE.md` ends with a "Git and GitHub" section: never commit to `main`, one change per branch, never add personal or copyrighted folders, a full PR description checked after opening, `Part of #N` for a phase and `Closes #N` only for the last, one code review per PR, CI green before the user merges.
- **Reason:** The practice had lived only in session memory. A PR body had come out as `-`, earlier PRs named issue #3 with no keyword, and one PR had three full reviews. The section is short and last, so it costs little during play.
- **Source:** PR #10.

## The Python scripts deleted at the switch-over

- **Date:** 2026-10-17
- **Decision:** Once the first release was pinned, point `CLAUDE.md`, the skills, the settings and the README at `./bin/dmc`, and delete `scripts/` (the four Python scripts, both dice rollers, `requirements.txt`), the Python tests and the parity check that had compared each port with its script.
- **Reason:** Each phase had kept the old script running beside the new subcommand so play never broke halfway. The switch-over waited for the pin (PR #17), because without a release DMC would have had no dice roller. The parity check's inputs were kept as the error-path guard's case lists.
- **Source:** PR #16 ("Closes #3"), merged after PR #17.

## Keep the missing-`dmc` notice at session start

- **Date:** 2026-10-17
- **Decision:** Keep the `SessionStart` hook in `.claude/settings.json` that runs `bin/dmc --version` and, when it fails, tells DMC in one line to run `get-dmc` before the first roll.
- **Reason:** Without it, a session started with plain `claude` and no working binary relies on DMC acting on a line in `CLAUDE.md`, which it would usually do only after a roll failed. It costs one shell start per session and prints nothing when `dmc` runs. It does not run `get-dmc` itself, which would touch the network on every start.
- **Source:** PR #16 (proposed under "For the user to decide"); kept by the user's decision after that PR merged.

## Developer builds report `0.0.0-dev` and live in `artifacts/bin`

- **Date:** 2026-10-17
- **Decision:** Every build that is not a release prints `dmc 0.0.0-dev`, a version no release has. Developers publish to `artifacts/bin/`; `bin/` is only the player's install, which `get-dmc` owns. The pin pull request no longer writes the project file's version, and the stale-binary warning ignores a development build.
- **Reason:** An old local build in `bin/` reported the release's version, so `get-dmc` kept it, and a newer subcommand failed with "unknown command". A version that can never match the pin makes `get-dmc` replace any stray build. Two earlier designs (a stamp file with the program's SHA-256, and a dev version sharing `bin/`) were dropped as costlier and incomplete.
- **Source:** PR #20.

## `dmc digest show`

- **Date:** 2026-10-17
- **Decision:** Rename `dmc digest digest` to `dmc digest show`, with no alias for the old spelling.
- **Reason:** The repeated word read badly, and the user asked for it to go. It needed a new release (0.1.1), since the skill calls the new name.
- **Source:** PR #18.

## Combat procedure rules from issue #6, Phase 1

- **Date:** 2026-10-17
- **Decision:** Put the fixes for combat-procedure problems found in reviewed play sessions into the prompts and templates:
  - in a fight, narrate before redrawing the map, with distances from `dmc map --measure`, and stop when something the player relied on turns out wrong;
  - the combat tracker lists every enemy attack with its reach or range and what it needs, and DMC says what an enemy can do from that list;
  - "wait for X" is a readied action, said back before confirming; an agreed plan is checked once and written down, and companions follow it unasked;
  - a question in reply to a request for a go-ahead is not a yes;
  - every roll the options let the player see is shown as `dmc`'s line with a label, target and result;
  - the tracker records the current turn's movement and actions, who has acted and what is readied;
  - resume and the full save compare the game state with companions' sheets too.
- **Reason:** Each rule had first been written into one player's options file after a mistake, where no other player or campaign would get it. By the test in the `review-transcript` skill (would it be wrong for any player?), they are toolkit rules, so each got one home in `CLAUDE.md`, a skill or a template.
- **Source:** Issue #6; PR #19 ("Part of #6").

## Skip the Gatekeeper and SmartScreen checks

- **Date:** 2026-10-17
- **Decision:** Do not test an unsigned `dmc` on a fresh Mac (Gatekeeper) or a fresh Windows PC (SmartScreen, Defender) before relying on it. Deal with any such problem if it is reported.
- **Reason:** The user's call. Archives fetched by `curl` or `Invoke-WebRequest` are expected to carry no quarantine mark, and the release workflow runs every archive on its own platform's runner, but a runner is not a fresh consumer machine. Code signing stays out of scope unless a problem appears.
- **Source:** Left open in issue #3's Phase 0 comment and in PR #14 ("Still open"); the user's decision on 2026-10-17, given in a session rather than in an issue or PR.

## Every change updates its documentation

- **Date:** 2026-10-17
- **Decision:** `CLAUDE.md` § Git and GitHub requires every change to update the documentation in the same PR: an XML doc comment on every type and member added or changed (private ones included; the build catches only missing public ones, from PR #24), a header comment on every script and workflow, and the `.docs/` documents that describe the change, including `roadmap.md` and this log. A change to `CLAUDE.md`, a skill or a template counts. The PR description says which documents changed, or why none needed to, and the review checks the documentation is current.
- **Reason:** The user asked for everything to be documented in code and in product-level documents in `.docs/`, and for that to hold for every change from now on. `CLAUDE.md`'s git section is what all toolkit work, including subagents, follows, and it costs nothing during play.
- **Source:** PR #22.

## Labelled rolls: a natural 20 or 1 decides an attack only, and the old line is kept

- **Date:** 2026-10-17
- **Decision:** `dmc roll` takes `label:dice@AC<n>` or `@DC<n>` and prints the existing line with the label before it and the verdict after (`Pip, shot: 2d20kl1+5: [17, 9] keep [9] +5 = 14 vs AC 14, hit`). Against an AC, a natural 20 hits and a natural 1 misses whatever the total, only when exactly one d20 counts (a single d20, or the kept die under advantage or disadvantage), and the line says `(natural 20)` or `(natural 1)`. A DC gets no such rule, since 5e gives no automatic success or failure to checks and saves. Meeting the target succeeds. The line keeps the old format rather than the shorter one sketched in issue #6 (`... 2d20kl1+5 [17, 9] keep 9 +5 = 14 ...`), so the line is what `dmc roll` always printed and what Phase 1 already put in `CLAUDE.md`; the cost is a double colon after the label.
- **Reason:** Rolls went unshown when many came in one reply, because DMC rebuilt each line by hand. With `dmc` printing the whole line, DMC pastes it unchanged. The natural 20 and 1 rule is in the SRD for attack rolls only ("If the d20 roll for an attack is a 20, the attack hits regardless of any modifiers or the target's AC", and likewise a 1 misses).
- **Source:** Issue #6 finding 4; PR #25; SRD 5.1, "Making an Attack".

## Pasting `dmc`'s line unchanged, and house rules stated after it

- **Date:** 2026-10-17
- **Decision:** `CLAUDE.md` § Dice has DMC paste `dmc`'s line unchanged, one per roll. If a campaign's house rule changes a verdict, DMC pastes the line unchanged and states the ruling after it.
- **Reason:** The line is the record of what was rolled, and `dmc` follows strict 5e, so it cannot know a house rule. Editing the line would make it say something no die produced.
- **Source:** Review of PR #25.

## Turn order on the map: initiative by token id

- **Date:** 2026-10-17
- **Decision:** `live.json` takes optional `"initiative"` (the creatures in turn order) and `"current"`. Each entry is a token's `id`, preferred because ids are unique, or the name of exactly one token; a name shared by several tokens is refused with their ids. An entry that matches no token is an error, `current` must be in `initiative`, and both keys are checked only when drawing, so `--measure` is never blocked by a stale order. A scene does not inherit the keys from its base place. In a fight DMC sets `current` once per reply, in the same edit as any moves, and runs `dmc map` once, last, only if something on the map changed.
- **Reason:** The player lost track of whose turn it was and when each creature acts. Names need not be unique ("Goblin" twice), so matching by name alone drew the wrong token or none. An unmatched entry is an error so that a typo is caught instead of drawing silently; the cost is that a creature that is not a token (a lair action) cannot be listed. Redrawing once per reply, not once per monster turn, keeps the cost of a fight down.
- **Source:** Issue #6 finding 5; PR #25 and its review.

## The build checks that code is documented

- **Date:** 2026-10-17
- **Decision:** `Directory.Build.props` turns on `GenerateDocumentationFile`, so the compiler reports CS1591 (no XML comment on a visible type or member) and checks that comments are well formed. In `src/Dmc.Cli` that is a build error, through its existing `TreatWarningsAsErrors`. In `tests/Dmc.Tests`, comment correctness (CS1570 to CS1574, CS1580, CS1584, CS1587, CS1589, CS1590) is an error in every file, and CS1591 is an error except in the files that hold MSTest test classes (`*Tests.cs`, `*Contract.cs`, `*Cases.cs`), where `tests/Dmc.Tests/.editorconfig` turns it off. `PublishDocumentationFile` is false, so `dmc.xml` stays out of the publish output and the release archives. Private and internal members are documented too, but the compiler cannot check them.
- **Reason:** The user asked for everything to be documented in code, and for it to stay so; a check that fails the build is what keeps it. The test classes are the exception because MSTest needs public test methods, and a test whose name says what it proves needs no comment: checking them would have flagged 382 members meant to have none. Scoping the exemption by file, not switching CS1591 off for the whole test project, keeps `Support/`, the fixtures and `Bootstrap.cs` checked. The documentation file is kept out of publishing so the archives stay at the four files `release.yml` insists on.
- **Source:** PR #24.

## The break alert is relative to where the resume left the context

- **Date:** 2026-10-17
- **Decision:** `dmc context-check` starts its bands at the larger of 150,000 tokens and the session's baseline plus 40,000, then every 40,000 after that. The baseline is the context of the last call before the first player message in the part of the log after the latest compaction boundary (the end of the recap after `/clear` and `/resume-campaign`), else of the first reply to that message. It uses no resume marker, so it holds however the files were loaded, and a resume the player runs mid-session cannot move it up. Injected user records (interruptions, shell-command records, command echoes, system reminders) and every text block of a message are handled by one classifier shared with `dmc digest`. The size is read from the end of the log and the baseline from the start, parsing only what can settle it, so an 18 MB log costs about 20 ms over process start. 150,000 stays as the floor. A declined break is offered again at the next band, counted from the first one. The line says "Context 163k is large", and `CLAUDE.md` § Compaction suggests a break "when the context has clearly grown large" instead of "when a session has clearly run long".
- **Reason:** A flat 150,000 fired one or two messages after `/resume-campaign` in three play-test sessions, because a resume added 96,000 to 142,000 tokens. Taking the break then reloaded the same files and put the context back near the threshold. What a break saves is the conversation grown since the files were loaded, so that is what the alert measures. The player asked to change the alert rather than their own habits. The baseline comes from what the hook can read, the log. After a compaction the log keeps going, so only the part after the boundary counts, and `/clear` starts a new log. The cause is context size, not session length, so the wording says so.
- **Source:** Issue #35 and its numbers (sessions `65b51f17`, `a4c01d59`, `bb8f7d9a`).
## A lighter resume: canon is searched past the archive level, the session log is read from its tail

- **Date:** 2026-10-17
- **Decision:** `resume-campaign` reads `story/canon.md` whole only while it is under about 230 lines (counted with the Grep tool). Past that it reads two sections whole whatever the scene, **Rulings** and **Prices, debts and promises** (the sections whose omission causes contradictions), and searches the rest for the names, places and threads in `game-state.md`'s Scene block, Active quests and Pending lines. Timeline is searched, not always read: it is the largest section in the one campaign measured, not short. It reads the last three scenes of the latest session log (found by `### Scene N:` headings with a number, so the template's commented example is not counted), or the whole log when the last full save does not cover it (its stamp now ends "through scene N"). It reads sheets only for the PCs, the companions (a block under Party status) and the NPCs present; the game-state template and `update-game-state` gain a "With the party, not companions" block so that a rescued NPC or a guest is not read as a companion. `campaign-full-save` step 4 splits long canon lines, then counts and archives down to about 200, never moving the two always-read sections, and may keep a long canon when every subject is live; its consistency check re-counts. Resume never asks for live subjects to be archived. The numbers and the read rules live in the two skills; the canon template only marks the always-read sections.
- **Reason:** In the issue #6 Phase 3 play test a resume added 96,000 to 142,000 tokens. Step 7 said to read canon "all", which contradicted `CLAUDE.md` § Accuracy ("For a large `canon.md`, search it instead of reading it all"); the archive step at a full save was a conditional that was easy to skip; the latest session log was read whole (in one resume twice); and sheets of NPCs who were not in the scene were read. Measured on the real campaign by bytes divided by 4, the resume's file reads fell from about 76,300 tokens to about 54,500 to 59,300 (see the PR). The line count is a weak proxy for cost (this campaign's canon lines average over 300 characters), so the save also splits long lines. Rulings and promises are always read because a search by scene names would miss a house ruling or a debt that the current scene does not mention.
- **Source:** Issue #35; PR #40 and its review.

## `dmc pdf` output is all or nothing, through a staging folder inside the output folder

- **Date:** 2026-10-17
- **Decision:** `text`, `render` and `all` make the output folder first, write into `<out>/.dmc-partial-<random>`, and move every file into place only when the whole run has succeeded, printing the report lines then. A page whose picture would be over 65,500 pixels on a side at the requested `--dpi` is refused up front, before anything is drawn or made. Before moving, the commit checks for the obstacles it can foresee (a file where a folder must go, a folder where a file must go) and refuses without moving anything. A failed run deletes the staging folder and every folder it made, item by item, and never deletes or replaces anything already in the output folder. If the clean-up fails, a line on stderr names the leftover folder; if the files were all moved but the staging folder cannot be deleted, the run still succeeds and says so after its report. A successful run still replaces files of the same name and leaves other files alone. `info` is unchanged. The code is `Pdf/OutputStaging.cs`; no test hook is in the binary.
- **Reason:** A run that failed on page 40 left pages 1 to 39 and the text files in `extracted/`, which looks like a finished extraction missing its last pages, and `ingest-module` could digest a book with pages missing. The staging folder is inside the output folder, not beside it (the first version put it beside), because then a run needs write access only where it writes, every move is a rename on one volume even if the output folder is a symbolic link or a junction, and an output path that cannot be used fails when the run starts, not after rendering. Moving file by file, not swapping folders, keeps the old merge behaviour and the promise that existing files survive a failed run. The size check is up front because it is the one failure that can be known before any work, and the tests had been using it to force a late failure; the late failure is now tested by an obstacle at the commit, which is real and needs no hook, and `OutputStaging` has its own unit tests.
- **Costs:** A re-run keeps its new files in the staging folder while the old ones are still in place, so it briefly needs disk space for both sets. The move is many renames, so a failure in the middle of it (the disk fills, a file is read-only) can still leave some files moved. A killed process, or a file another program holds open, can leave the `.dmc-partial-` folder behind; it is safe to delete, and the user is told where it is whenever the program is still running to tell them.
- **Source:** Issue #30; the review of PR #33.
## A bad token count is refused when the log is read, and stops the whole digest

- **Date:** 2026-10-17
- **Decision:** `Session` checks the token counts of every assistant record as it reads it: each of the four counts must be a number, and finite, and the context reading (input plus the two cache counts) must be finite. A record that fails is reported by the existing catch as `<file>.jsonl line N: <message>`, exit 1, like any other shape error. A bad log stops the whole run, `list` and `show` alike, before anything is printed or written; it is not skipped.
- **Reason:** A non-finite count had been found only when the statistics were worked out, after `Session` had finished, so it escaped as an unexpected failure, with earlier sessions' files already written, and `list` and `show` could disagree about the same log. Checking where the record is read fixes all of it at the root: one place, one message form with the line number, and `list` and `show` refuse exactly the same logs. A first version caught the error in a wrapper around the reporting; it was dropped as treating the symptom. Stopping rather than skipping follows from how `Session` already treated every other bad log, and a skipped session would leave `stats.md` quietly short of one and the `review-transcript` skill reading it as complete. `show` already read every session before writing, so no build-ahead of the output was needed.
- **Limits:** Sums across many calls are not checked: counts that are each finite but add up past about 1.8e308 would still fail in the statistics as an unexpected failure. No real log has such numbers.
- **Source:** Issue #29; the review of PR #32.
## No deleting campaign files in play

- **Date:** 2026-10-17
- **Decision:** `CLAUDE.md` § Saving as you go forbids deleting or moving a file under `adventures/` unless the player asks for that file to go, and then DMC tells the player the command to run themselves. `.claude/play-settings.json` adds deny rules: `Bash(rm *)`, `Bash(rmdir *)`, `Bash(unlink *)`, `Bash(mv *)`, `Bash(git clean *)`, `Bash(find * -delete*)`, `Bash(xargs rm*)` and `Bash(xargs * rm*)` (Claude Code strips a bare `xargs` but not one with flags), and for PowerShell `Remove-Item` and `Move-Item` with `rm`, `del`, `erase`, `rd`, `ri`, `rmdir`, `mi`, `move` and `mv`, plus `git clean`, each written out although Claude Code matches a cmdlet's aliases. `ToolkitTests.ThePlaySettingsDenyShellDeletes` checks that the rules are present, that they match the commands they should and not `./bin/dmc` or read-only commands, and that none of them is in `settings.json` (toolkit work). The rules are in play-settings only, so toolkit work can still delete files. The `rewind-campaign` skill, which used to delete maps of places the party had never seen, now treats the agreed rewind as the player's request, lists those maps, asks the player to confirm and gives the delete command; until they are gone it records them in `game-state.md` Pending as "do not load or draw" (read by `resume-campaign` and `draw-map`) and leaves them out of its clean-up sweep. It never empties a file with Write as a way to delete it.
- **Reason:** In the issue #6 Phase 3 play test (session `a4c01d59`) DMC ran `rm` on three git-ignored campaign files the player still needed, and they had to be rebuilt from a transcript. The prompt said not to use shell commands on campaign files but never said not to delete. Claude Code's documentation says a `*` in a Bash or PowerShell rule matches any text, that a trailing ` *` also matches the bare command (only when it is the rule's only wildcard), that deny rules apply when any subcommand of a compound command matches (so `cd x && rm y` is caught), that wrappers such as `timeout` and leading `VAR=x` assignments are stripped before matching, and that PowerShell matching is case-insensitive and canonicalizes aliases.
- **Limits:** A deny rule matches the command text Claude writes. It does not stop `/bin/rm` or `bash -c 'rm ...'` (the documentation's own examples), `powershell -c "Remove-Item ..."` or `cmd /c del` run from Bash, `find -exec rm`, `cp` or a redirect over a file, a redirect that truncates a file, or a script or interpreter (`python -c`) that deletes. The Edit and Write tools can still overwrite a file. A `mv` or `Move-Item` by another spelling (`/bin/mv`, `bash -c`) is forbidden by the prompt only. The block was not tried against a live Claude Code session: the attempt to run a nested session was refused by the harness, so the matching is taken from the documentation (https://code.claude.com/docs/en/permissions) and the toolkit test checks only the rules' text and the documented matching, not Claude Code's own parser. The prompt clause is the first line of defence; the rules catch the usual forms.
- **Source:** Issue #36; PR #38.

## The map is redrawn by a Stop hook after the reply

- **Date:** 2026-10-17
- **Decision:** DMC no longer runs `dmc map` to redraw during a fight. It edits `maps/live.json` and finishes its reply; the play settings' `Stop` hook then runs the new `dmc redraw`, which draws the page again if `live.json` has changed since the last drawing. A fight always uses `live.json` (`CLAUDE.md` § Maps). `dmc redraw` also follows a map DMC drew by hand while `live.json` is unchanged: `dmc map` records, in `maps/live-source.txt`, the file it drew and that file's base place (path relative to `maps/`, resolved by the same `MapChecker.BasePath` as loading a map), so telling whether anything changed takes modification times, not a parse. If `live.json` is newer than the picture it is drawn even when the record names another file, and the record is rewritten. A page with no record follows `live.json` alone. If a `live.png` sits beside the page it is refreshed too. A refused file makes `dmc redraw` exit 2 with one line on stderr, which Claude Code gives to DMC to correct; the line says to fix it now and that a file still refused is reported again when the next reply ends. `stop_hook_active` true means Claude Code is already continuing, so the file is drawn if it is now fine but a refusal is not reported a second time: nothing is lost, because a refused file stays newer than the picture and the next turn's Stop hook finds it again. It prints nothing and exits 0 when no campaign is active, no page has been drawn, or nothing changed, and it reads stdin only when that is not a terminal. The hook command lets only exit 2 through, so an older `dmc` (unknown command, exit 1), a missing `bin/dmc` and a broken one stay silent. `CLAUDE.md` § Maps shrinks accordingly, and keeps one line: if a correction moves someone already narrated, tell the player. The remote player's picture, which a hook cannot send, stays a prompt rule in the `draw-map` skill: write the text, then `dmc map --png`, then send `live.png`; for a remote player the order depends on DMC alone, and the docs say so. Issue #6 stays open until a play test verifies all of this.
- **Reason:** The first fix, rewording § Maps to say the narration must be reply text written before the `dmc map` call, relied on the model doing something it had failed to do in all 6 map updates of the Phase 3 play test: Claude Code shows reply text after the tool calls that precede it in a turn, and DMC wrote no text before them. A rule the model already broke 6 times out of 6 is not a fix; a hook that runs after the reply is by construction later than the text. A new subcommand, and not an option such as `dmc map --pending`, because 0.1.2 treats an unknown option as a usage mistake with exit code 2, which a Stop hook turns into feedback that blocks the reply; an unknown command exits 1, which the hook command maps to 0. A modification-time comparison with `live-data.js` is the cheapest test of "changed" and needs no extra state beyond the record. Fights are pinned to `live.json` so that DMC never has to tell the hook which file a fight is in, and the hand-drawn case is covered by the record.
- **Costs and limits:** It needs release 0.1.3 and a pin before it does anything, and then only in sessions started with the play settings; elsewhere the map is not redrawn unless DMC runs `dmc map` itself after its reply text (the `draw-map` skill says so). It does not cover text maps or the remote player's picture. The first drawing of a map is still made by DMC with `dmc map`, before the narration. A file edited in the same clock tick as the last drawing is missed until its next edit. The Stop hook's behaviour (`stop_hook_active`, exit 2 as feedback) is taken from Claude Code's hooks reference, https://code.claude.com/docs/en/hooks; it was tested with a copy of the published `dmc` run by bash as the hook command, not inside a live Claude Code session.
- **Source:** Issue #6 and its Phase 3 comment (sessions `65b51f17` and `a4c01d59`, 2026-10-17); PR #37 and its two reviews.

## Feedback about DMC is recorded outside the campaign and tuned in one place

- **Date:** 2026-10-17
- **Decision:**
  - There are two kinds of feedback. Player options (how this player wants the game run) stay in `player-options.md`, as one-line preferences. DMC feedback (how DMC operates, such as a mistake, an inconsistency or too much context) goes to `feedback/` at the toolkit root. The test is `review-transcript`'s: would the same behaviour have been wrong for a different player with different preferences?
  - `feedback/` is git-ignored, keeping only `.gitkeep`, like `adventures/`, `modules/` and `rules/`, because entries quote the player and can contain spoilers. Turning an entry into a GitHub issue is a later step the worker takes with the user.
  - One file per entry, `feedback/YYYY-MM-DD-<slug>.md`, with a `status:` line in the frontmatter, and no index file. A repeat adds an occurrence to the existing entry and does not make a new file.
  - Nothing in `feedback/` is read during play except the titles `record-feedback` searches for a duplicate. Resume and full save never touch it.
  - If the player wants a change applied in this campaign now, DMC also writes one preference line in the campaign's `player-options.md`; the entry names it and the worker removes it once the fix ships.
  - No new `dmc` code. DMC has the date but no clock, so each entry records the real date, the story session and the player's exact words; a worker finds the moment with `dmc digest list --campaign <c> --after <date>` and a search of the `-turns.txt` file for the quote.
  - DMC records when the player gives feedback about DMC, corrects a DMC mistake that would be wrong for any player, or when its own slip forced a rewind or a rebuilt file. It never records the player's tastes there, and never edits the toolkit mid-game.
  - `review-feedback` is the only skill that tunes DMC: it reviews entries, finds the cause in the prompts, proposes changes and, on the user's word, files issues or makes PRs. `record-feedback` and `review-transcript` only produce entries, so `review-transcript` lost its Apply step. A toolkit test keeps `gh issue` and `gh pr` out of every other skill.
- **Reason:** Feedback had landed wherever DMC happened to be writing, in six places inside one campaign, several of them loaded at every resume, so a worker had to comb play files and the play files carried notes that were not about the game. One home per kind of feedback costs play nothing in context, keeps the entries understandable without the adventure, and shows by its occurrences how often a problem was raised. Keeping the tuning in one skill stops two skills proposing different fixes for the same cause.
- **Limits:** `Edit(feedback/**)` must be in `.claude/settings.json` for recording to raise no prompt in play. The existing scattered feedback in the first campaign is migrated by hand, with the player, after merge; that is local data and not part of the PR.
- **Source:** Issue #43.

## CI does not run for changes that are not code

- **Date:** 2026-10-17
- **Decision:** `.github/workflows/tests.yml` has a `paths` filter on both `push` and `pull_request`: everything runs it except `*.md` and `*.txt` files, and `CLAUDE.md`, `README.md`, `.claude/skills/**` and `templates/**` are put back, because the toolkit tests read them. The same list is written out for both events.
- **Reason:** A documentation-only change started three platform builds of a Native AOT program and all the tests for nothing. The user asked for CI to skip anything that is not technically code. The prompts are the exception: `ToolkitTests` checks that every `dmc` command, skill name and path in them is real, so a broken prompt must not merge unseen.
- **Limits:** The filter is untested until a documentation-only PR shows the skip. `release.yml` keeps its own filter. `.docs/` files are not tested for content, only checked to exist when a prompt names them, so deleting or renaming one that a prompt names, together with a Markdown-only change, would not be caught.
- **Source:** the user, in the session that closed issue #43.
## feedback/ has a .ignore file so the entries can be searched

- **Date:** 2026-10-18
- **Decision:** `feedback/.ignore` holds `!*` and is tracked (`!feedback/.ignore` in `.gitignore`, LF in `.gitattributes`). The entries stay git-ignored, but ripgrep treats a `.ignore` file as overriding `.gitignore`.
- **Reason:** The Grep tool uses ripgrep, which skips git-ignored files. A search of `feedback/` returned nothing although ten entries matched, so the duplicate check in `record-feedback` and the listing in `review-feedback` could not see any entry. Naming one file directly worked, which is why it went unnoticed. After adding the file the same search found all ten.
- **Limits:** The same ripgrep behaviour hides any directory search under `adventures/`, `modules/` and `rules/`. A search of one named file is not affected. This is not changed here.
- **Source:** The first run of `review-feedback`, 2026-10-18.

## Feedback has one route; player options are a different thing

- **Date:** 2026-10-18
- **Decision:** A player option is a preference about the player's own game that the player wants changed. Feedback is something wrong with DMC that DMC should change. Feedback goes to `feedback/` through the `record-feedback` skill and nowhere else: not `game-state.md`, `dm-notes.md`, `canon.md`, the session logs, a `player-options.md`, or a note on another entry. The skill is invoked, not run by hand. Its classify step stays (the test: would the same behaviour have been wrong for a different player?); its stopgap step is removed, so a change the player wants now is a preference and goes to the player options. A message with both is handled as two separate items, without a reference from one to the other. `CLAUDE.md` § Player options says so in two lines. Replaces the stopgap part of the decision on recording feedback outside the campaign.
- **Reason:** The player asked for always and only one route, so feedback is never lost. DMC recorded one complaint as an entry, a note on an entry, a "stopgap" note in `game-state.md` and a possible option line, and ran the skill's steps by hand on some turns. Each extra route is a place feedback can be left behind or contradict the entry. A preference and a defect in DMC are different things and keep different homes.
- **Limits:** Existing stopgap lines in a campaign (and a sentence in the first campaign's `game-state.md`) are left until `review-feedback` closes the entries that name them. The skill relies on DMC invoking it.
- **Source:** the user, 2026-10-18; `feedback/2026-10-18-one-way-to-give-feedback.md`.

## Six small prompt fixes from the first feedback review

- **Date:** 2026-10-18
- **Decision:** Six entries in `feedback/` are fixed together, each in the one place it belongs:
  - `CLAUDE.md` § Narration: a list of what is left or what to do holds only threads the player raised and setups awaiting payoff, sized to the story timeline if one is set; never upkeep or threads already answered; each option is checked against canon first; DMC's own open notes stay in `dm-notes.md`.
  - `templates/campaign/player-options.md`: a "Story timeline" line.
  - `templates/campaign/game-state.md`: a "Reserved by the player" section, so DMC does not suggest spending an item the player is keeping for a named purpose.
  - `CLAUDE.md` § Saving as you go: files are changed with Edit, and Write only creates a new file.
  - `templates/session-log.md`: only what a character said or did in the fiction is logged, so an out-of-character aside is not later known by an NPC.
  - `CLAUDE.md` § Saving as you go: in every turn the reads, rolls and saves come first and the reply is last, with no tool call after it. § Maps points to it instead of repeating it.
- **Reason:** Each traced to something the prompts said, left out or made expensive: no rule on what to offer, no home for a timeline or a reserved item, "Edit or Write" with no guard, no marker separating out-of-character talk in the log, and the reply-last order stated only for combat. `CLAUDE.md` grows by about three lines, paid in every exchange; the reply-order rule replaces words in § Maps.
- **Limits:** Whether Claude Code can hide diffs and commands is a display setting outside the toolkit and was not looked up. The cause of the out-of-character entry and of the overwritten `game-state.md` was inferred from the prompts, not from a transcript.
- **Source:** `feedback/2026-10-18-dmc-offers-its-own-loose-ends-as-things-to-do.md`, `2026-10-17-quests-not-sized-to-story-timeline.md`, `2026-10-17-dmc-suggests-spending-an-item-the-player-is-saving.md`, `2026-10-17-game-state-overwritten-and-rebuilt.md`, `2026-10-17-ooc-statement-logged-as-in-fiction.md`, `2026-10-17-narration-split-by-tool-output.md`.

## Every resume starts a new session

- **Date:** 2026-10-18
- **Decision:** Every `/resume-campaign` ends the previous session and starts a new one, whatever brought it (a `/clear`, a new day, a compaction). Resume reads the previous log first, then closes it (fills an empty "Session end" block from `game-state.md`), creates the next `session-NNN.md`, and sets `Current session` to the new number only. If the most recent log has no scenes yet it is reused, so two resumes in a row do not leave an empty log. Scenes number from 1 in each session, and a citation is `sN` for a session or `sN.M` for a scene. Existing logs and `(s8)` citations are unchanged and keep their meaning.
- **Reason:** The toolkit never said what makes a session new, and a planned break always meant "keep using the current one", so one campaign's "session 8" grew to 38 scenes and 22 full saves over four days. A resume is the one moment DMC always passes through when context is reloaded, so tying the boundary to it needs no judgment, no asking and no counting, and keeps the log that the next resume reads short.
- **Limits:** A compaction in the middle of a scene also starts a new session, because the compaction hook runs the resume. The new log is then short and the scene continues in it. A session is therefore a stretch between resumes, not necessarily a sitting. Old logs are not split; the Silver Egg's session 8 stays as it is.
- **Source:** The user, 2026-10-18; `feedback/2026-10-17-session-boundary-undefined.md`.

## A fight is planned with an ending

- **Date:** 2026-10-18
- **Decision:** `CLAUDE.md` § Sizing fights gets a "How it ends" bullet: before initiative DMC knows how the fight ends and where the party can rest afterwards. An enemy fights to the end, flees for good, or can be caught within the fight. A creature that flees, heals, regenerates or calls for help needs a reason and an end DMC can state (where it goes, what it does there, when it stops); without one it does not do it. No enemy returns healed in the same fight, a call for help brings only what was counted, and one fight does not run into the next with no break. § Fun first now says "flee for good at half strength".
- **Reason:** In one campaign a troll fled at low HP, healed out of sight and returned, and called a second troll, so one fight ran thirteen rounds with no rest against enemies too big for level 2. The player's diagnosis was that this is a planning problem, not a chase problem: nothing made DMC plan how the fight ends and where the party can rest, and one enemy could pull in the whole dungeon. § Sizing fights already counted fights against rests across the adventure, but never asked how a single fight ends, and § Fun first listed fleeing as a tool with no limit. A narrow "no Dash chases" rule was rejected because several small decisions (a flee threshold, regeneration with a hiding place, a fast Dash, darkness, a reinforcement called by a roar) caused it.
- **Limits:** About three lines are added to `CLAUDE.md`, paid in every exchange. The rule relies on DMC planning before initiative; nothing checks it. It has not been tried in play.
- **Source:** The user, 2026-10-18; `feedback/2026-10-14-endless-combat-fleeing-and-healing-enemies.md`.

## Resume and full save are bounded

- **Date:** 2026-10-18
- **Decision:** Every file that resume reads whole has a size to aim for, enforced at the full save, and the two procedures stop re-reading files they already hold. `game-state.md` under about 100 lines; `dm-notes.md` under about 150 lines with the notes about the player as lasting statements under about 40; each sheet under about 150 lines, with its history as the last section (`## History`) and old history moved to `character-sheets/<name>-history.md`. Resume reads a sheet down to its history. The full save's consistency check uses targeted Greps and short Reads instead of reading every changed file, the PC's sheet and every companion's sheet again, and archives canon with one Edit per file instead of one per line. All targets are in lines, after splitting long lines, because the files have lines of 1,000 characters and more.
- **Reason:** A review of twelve play sessions found that every resume added 104k to 165k tokens and that the full save was 8% to 34% of all context read. The causes were files that only grew (a 37,000-character PC sheet, 36,000 to 40,000 characters of DM notes, 29,000 of game state at the time), read whole at every resume, and a consistency check that read them all again. Only `canon.md` and the session log had a size rule or a partial read. Targets in lines, not characters, because there is no cheap way to count characters and a line count is already how `canon.md` is bounded.
- **Limits:** The targets are guesses; the first campaign's files are far over them and will shrink only as full saves trim them. A sheet that keeps its history in the middle is read whole until a full save moves it. Nothing enforces the targets except the skill telling DMC to count lines. A `dmc` command that reports file sizes would be more reliable and was not built.
- **Source:** `feedback/2026-10-18-resume-costs-over-120k-tokens-in-a-mature-campaign.md` and `feedback/2026-10-18-full-save-is-the-largest-context-cost-after-the-resume.md`.

## The Stop hook opens the first map of a fight

- **Date:** 2026-10-18
- **Decision:** `dmc redraw` also draws `live.json` when no page exists yet, and opens `live.html` in the browser (as `dmc map --open`) whenever it draws `live.json` and the page did not show it: no page, or `live-source.txt` names another file. It does not open on a redraw of a page already showing the scene. `CLAUDE.md` § Maps and `draw-map` tell DMC to write `live.json` and let the hook draw and open it, never to run `dmc map --open` for a fight.
- **Reason:** In the issue #6 verification play test (session `77c6aab6`) DMC ran `dmc map --open` mid-reply to start a fight, so the map opened before the narration. Opening on every redraw would add a tab per reply; opening only when no page exists would miss a fight that follows a place map, where the old tab may be closed. Opening on a switch costs at most one extra tab per fight.
- **Limits:** Needs release 0.1.4 and its pin. Remote play (sending the picture) is unchanged: the hook cannot send files. A stale `live.json` with no page is drawn and opened at the next reply.
- **Source:** Issue #42.
