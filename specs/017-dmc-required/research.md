# Research: `dmc` is a required part of DMC

Every finding cites a file on `017-dmc-required` (branched from `origin/main` at `fb71982`) or a command run on
2026-11-17. Anything not checked is marked unverified.

## R1. How the kit reaches a player

- **Decision:** A kit change reaches players only through a `dmc` release.
- **Rationale:** `kit/` is embedded in the program, and `dmc install`, `refresh` and `play` write it into the DMC
  folder: `kit/dm-rules.md` becomes `CLAUDE.md`, `kit/settings.json` becomes `.claude/settings.json`, `kit/skills/**`
  becomes `.claude/skills/**` (`src/Dmc.Cli/Kit/GeneratedFiles.cs:6-10`, `:29-32`). `dmc play` refreshes the folder
  when its manifest's version is not the program's (`src/Dmc.Cli/Commands/PlayCommand.cs:9-11`). So the kit a player
  runs always matches their `dmc`, and this change ships in the next release.
- **Alternatives considered:** none; this is how every kit change ships.

## R2. The kit lines that tell DMC how to work without `dmc`

- **Decision:** Six places change, all wording; no line is added.

| Place | Today | After |
|---|---|---|
| `kit/dm-rules.md:78` | "If `dmc` fails or the recall does not hold it, read its sheet …" | "If the recall does not hold it, read its sheet …" (FR-001, FR-004) |
| `kit/dm-rules.md:139` | "… \| sh`), and carry on without dice and maps until then. `dmc` also draws the maps and reads PDFs." | "… \| sh`), then play nothing until `dmc --version` runs. `dmc` also draws the maps and reads PDFs." (FR-002, FR-003) |
| `kit/skills/dmc-draw-map/SKILL.md:114` | "If `dmc` is unavailable, or the player options ask for text maps, draw the same grid …" | "If the player options ask for text maps, draw the same grid …" (FR-005) |
| `kit/skills/dmc-ingest-module/SKILL.md:26` | the bullet "If `dmc` is unavailable, fall back to the Read tool's `pages` parameter … and tell the player that a PDF over 100 MB needs `dmc`." | removed (FR-001) |
| `kit/skills/dmc-setup-rules/SKILL.md:17` | "(Without `dmc`, skip this step.)" | removed (FR-001) |
| `kit/skills/dmc-setup-rules/SKILL.md:42` | "Otherwise, or when it finds nothing, open the index …" | "When it finds nothing, open the index …" (FR-001, FR-004) |

- **Rationale:** Found by `grep -n -i -E 'dmc` (is )?(unavailable|missing|…)|without `dmc`|if `dmc`|fall ?back|never
  depends'` over `kit/`; `kit/dm-rules.md:154`'s "A fallback ("only if he fails")" is about a player's declared action,
  not `dmc`, and stays. Line 139's new ending carries FR-003 in the rules every turn loads, so no skill needs a line of
  its own. The same search also finds `kit/skills/dmc-resume-campaign/SKILL.md:14`, "If `dmc` fails, Glob
  `adventures/<campaign>/.closed`: if found, the same.", which stays (R3): it handles an error from a `dmc` that runs,
  such as one older than 0.1.4 that does not know `campaign` (`.docs/user-guide.md:449`), and keeps a closed campaign
  closed (FR-010); a missing `dmc` already stops play at line 139.
- **Errors after the edits (FR-010):** the clauses removed at `kit/dm-rules.md:78` ("If `dmc` fails or") and
  `dmc-setup-rules:42` ("Otherwise,") also covered a recall or lookup that reports an error. The clauses left cover it:
  an error prints no fact, so the recall "does not hold it" and the lookup "finds nothing", and the sheet, the index
  and the search follow as before. No kit change is needed for it.
- **Alternatives considered:** a line in `dmc-resume-campaign` for a missing `dmc` (rejected: the rule has one home,
  constitution Development 6, and `CLAUDE.md` is always loaded); rewording the start-of-session notice (rejected: FR-002
  keeps it, and `tests/Dmc.Tests/Toolkit/ToolkitTests.cs:348` asserts its text). Reversed after PR #233's code review: "so dice, maps and PDF reading will not work" implied the
  rest of play goes on, so the notice now says `dmc` is not found or will not start and DMC cannot play until it does;
  the test asserts the new text.

## R3. What stays

- **Decision:** The start-of-session hook (`kit/settings.json:101-108`), its test
  (`ToolkitTests.WithoutAWorkingDmcASessionIsToldOnceToReinstall`, `ToolkitTests.cs:336`), the reinstall lines in
  `kit/dm-rules.md:139` and their assertion (`ToolkitTests.cs:184`, which ends at "install.sh | sh`)" and so still
  holds), the hooks that swallow a missing `dmc` (`.docs/architecture.md:119`), lookup's next steps after no heading
  (`kit/dm-rules.md:113`, `kit/skills/dmc-resume-campaign/SKILL.md:34`) and the text-map player option
  (`kit/templates/player-options.md:31`) and the resume's closed-campaign guard ("If `dmc` fails, Glob
  `adventures/<campaign>/.closed`: if found, the same.", `kit/skills/dmc-resume-campaign/SKILL.md:14`, R2) are
  unchanged.
- **Rationale:** spec § What stays; FR-002, FR-004 to FR-006.

## R4. Errors from a working `dmc`

- **Decision:** No change. The kit already says what to do where a failure matters: `dmc-draw-map:95` (a refused map:
  fix and rerun), `dmc-resume-campaign:14` (`ahead`: `dmc` needs updating, one line, out of character) and `:34` (no
  heading: nearest heading, and a one-line notice), `dmc-migrate-campaign:44` and `:75-76` (a failed backup or check is
  said in plain words). `dmc`'s errors are one line (`.docs/architecture.md:118`; constitution § Build and Output
  Constraints).
- **Rationale:** The maintainer, 2026-11-17: errors are handled smoothly today, and an error is not a missing `dmc`
  (spec Clarifications, FR-010). A general five-kind rule was drafted (about 470 bytes, some 120 tokens a turn) and
  dropped. No feedback entry in `D:\Claude\dmc\feedback\` is about a `dmc` error handled badly (checked: the two entries
  that mention a `dmc` command's output are consistency slips, out of scope).

## R5. Cost (constitution Development 1)

- **Decision:** `kit/dm-rules.md` loses 16 bytes and no line: 15 for "`dmc` fails or " (line 78; "If " stays) and 1
  for the new ending of line 139 (48 bytes to 47). Today 32,522 bytes (`wc -c`), 212 lines; after, 32,506 bytes, 212
  lines (measured; the plan's first count of 19 also counted the "If " that stays). The
  cap in `ToolkitTests.cs:1211` (`DmRulesBytesAllowed = 32_522`) is lowered to the measured size, so the saving holds
  (FR-009). The skills lose their fallback clauses (`dmc-ingest-module` one bullet, `dmc-setup-rules` two clauses,
  `dmc-draw-map` one clause), paid only when they load. Measured: `dmc-draw-map` 11,377 → 11,352 bytes (−25),
  `dmc-ingest-module` 9,827 → 9,625 (−202), `dmc-setup-rules` 3,671 → 3,624 (−47); −274 in all.
- **Rationale:** `printf '%s' '<text>' | wc -c` on each clause; the final figures are taken from the edited files.
- **After PR #233's code review:** line 139's "If `dmc` is missing or does not run" became "If `dmc` is not found or
  will not start", so an error exit from a working `dmc` (for example `dmc recall`'s "nothing to search for") is not
  read as a missing one. It adds 4 bytes: 32,510, −12 against 32,522; the cap is 32,510.

## R6. Tests

- **Decision:** One new toolkit test, `TheKitCarriesNoRuleForPlayingWithoutDmc`, in a new partial file
  `tests/Dmc.Tests/Toolkit/DmcRequiredTests.cs`: no file under `kit/` holds the removed wording ("If `dmc` is
  unavailable", "Without `dmc`, skip", "If `dmc` fails or the recall", "Otherwise, or when it finds nothing", "carry
  on without dice and maps"), and `kit/dm-rules.md` holds "then play nothing until `dmc --version` runs". The phrases
  are the removed wording, not a looser "if `dmc` fails", which would also match the guard that stays (R2, R3).
  `DmRulesBytesAllowed` drops to the measured size with its summary extended.
- **Rationale:** The prompts are checked by toolkit tests (constitution § Development Workflow;
  `tests/Dmc.Tests/Toolkit/FeedbackRoundTests.cs` is the pattern: a partial of `ToolkitTests` per spec). One existing
  test asserts a removed line, which the first search here (`grep -rn -E 'unavailable|carry on without|Without
  \`dmc\`' tests/`) missed: `ThePlayRulesNameRecallAndLookupAndKeepTheFallback` (`ToolkitTests.cs:1324`) held "If
  `dmc` fails or the recall does not hold it". It now holds "If the recall does not hold it, read its sheet", the step
  FR-004 keeps. The test needs no tool, so Development 7 does not apply.

## R7. Documents

- **Decision:**
  - `README.md:11`: "Without `dmc`, play and saving still work, but …" becomes that DMC needs `dmc` to play and says
    how to reinstall if it goes missing.
  - `.docs/architecture.md:120`: the principle "Play never depends on `dmc` to start" is replaced by "`dmc` is
    required": Claude Code still starts without it, so the notice can be shown, but DMC does not play until `dmc` runs;
    an error `dmc` reports is not a missing `dmc`.
  - `.docs/user-guide.md:65` and `:450`: DMC tells you to reinstall and does not play until `dmc` runs.
  - `.docs/decisions.md`: a new entry, "`dmc` is a required part of DMC (issue #208)", with the date of the decision
    (2026-11-14), the clarifications of 2026-11-17, the reason, the cost and the source; "Partly superseded 2026-11-17"
    lines on § Retrieval runs over the markdown (its "play never depends on `dmc`" clause and the fallback in its
    Limits) and § Lookup and recall are for play only until Phase D ("with the fallback to searching", where it meant a
    failing `dmc`).
  - `.docs/roadmap.md`: a row for spec 017, "for the next release".
- **Rationale:** FR-007; constitution Documentation 2 (README and user guide are for players) and Documentation 4.
  `.docs/decisions.md` § A module check (line 681) already names #208 and stays as written. Issue #179's Plan
  principles live in a closed issue and are left as the record.

## R8. Migration and install

- **Decision:** No migration; no new install or upgrade step.
- **Rationale:** No template, and no skill rule for a campaign file's shape, changes (FR-008; constitution Development
  2). The change only removes wording, so install and `dmc update` are unchanged (Development 3).

## R9. The headless play tests (SC-003, SC-004)

- **Decision:** Run `claude -p` in the test folder `D:\Claude\test-campaign` (a DMC folder with campaigns, kept for
  headless tests), after the maintainer has run `artifacts/bin/dmc refresh D:\Claude\test-campaign` from a terminal
  with this branch's build. SC-003 runs with both `dmc` folders (`artifacts/bin` and the installed
  `C:\Users\webhe\AppData\Local\Programs\dmc`, `which dmc`) off `PATH`; SC-004 with `artifacts/bin` first on `PATH`,
  as spec 016's runs did (`specs/016-feedback-round-181/quickstart.md`).
- **Rationale:** The kit reaches a folder only through `dmc` (R1), and `install` and `refresh` refuse inside a Claude
  Code session (`CLAUDECODE` is set; `.docs/development.md` § Trying a change), so the maintainer runs the refresh.
  At implementation the maintainer was remote, so the runs used a scratch copy of the test folder with this branch's
  kit written in as `GeneratedFiles.Table` maps it (quickstart.md § Results). Whether the `SessionStart` hook runs
  under `claude -p`, unverified when planned, was checked: it does, and the notice appeared.
