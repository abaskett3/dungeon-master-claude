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

## No deleting campaign files in play

- **Date:** 2026-10-17
- **Decision:** `CLAUDE.md` § Saving as you go forbids deleting or moving a file under `adventures/` unless the player asks for that file to go, and then DMC tells the player the command to run themselves. `.claude/play-settings.json` adds deny rules: `Bash(rm *)`, `Bash(rmdir *)`, `Bash(unlink *)`, `Bash(mv *)`, `Bash(git clean *)`, `Bash(find * -delete*)`, `Bash(xargs rm*)` and `Bash(xargs * rm*)` (Claude Code strips a bare `xargs` but not one with flags), and for PowerShell `Remove-Item` and `Move-Item` with `rm`, `del`, `erase`, `rd`, `ri`, `rmdir`, `mi`, `move` and `mv`, plus `git clean`, each written out although Claude Code matches a cmdlet's aliases. `ToolkitTests.ThePlaySettingsDenyShellDeletes` checks that the rules are present, that they match the commands they should and not `./bin/dmc` or read-only commands, and that none of them is in `settings.json` (toolkit work). The rules are in play-settings only, so toolkit work can still delete files. The `rewind-campaign` skill, which used to delete maps of places the party had never seen, now treats the agreed rewind as the player's request, lists those maps, asks the player to confirm and gives the delete command; until they are gone it records them in `game-state.md` Pending as "do not load or draw" (read by `resume-campaign` and `draw-map`) and leaves them out of its clean-up sweep. It never empties a file with Write as a way to delete it.
- **Reason:** In the issue #6 Phase 3 play test (session `a4c01d59`) DMC ran `rm` on three git-ignored campaign files the player still needed, and they had to be rebuilt from a transcript. The prompt said not to use shell commands on campaign files but never said not to delete. Claude Code's documentation says a `*` in a Bash or PowerShell rule matches any text, that a trailing ` *` also matches the bare command (only when it is the rule's only wildcard), that deny rules apply when any subcommand of a compound command matches (so `cd x && rm y` is caught), that wrappers such as `timeout` and leading `VAR=x` assignments are stripped before matching, and that PowerShell matching is case-insensitive and canonicalizes aliases.
- **Limits:** A deny rule matches the command text Claude writes. It does not stop `/bin/rm` or `bash -c 'rm ...'` (the documentation's own examples), `powershell -c "Remove-Item ..."` or `cmd /c del` run from Bash, `find -exec rm`, `cp` or a redirect over a file, a redirect that truncates a file, or a script or interpreter (`python -c`) that deletes. The Edit and Write tools can still overwrite a file. A `mv` or `Move-Item` by another spelling (`/bin/mv`, `bash -c`) is forbidden by the prompt only. The block was not tried against a live Claude Code session: the attempt to run a nested session was refused by the harness, so the matching is taken from the documentation (https://code.claude.com/docs/en/permissions) and the toolkit test checks only the rules' text and the documented matching, not Claude Code's own parser. The prompt clause is the first line of defence; the rules catch the usual forms.
- **Source:** Issue #36; PR #38.
