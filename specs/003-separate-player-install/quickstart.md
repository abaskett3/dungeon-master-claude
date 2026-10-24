# Quickstart: validating the separated install

**Feature**: [spec.md](spec.md) | **Contracts**: [contracts/](contracts/)

How to prove each user story end to end. Commands are PowerShell; bash is the same with `/` paths.

## 1. A new player installs and plays (User Story 1)

On a machine (or fresh user account) with Claude Code and no repository:

1. Run the install line ([contracts/bootstrap.md](contracts/bootstrap.md)). Press Enter at `DMC folder [~/dmc]:`.
   Expect the `dmc install:` lines.
2. Open a new terminal: `dmc --version` prints the release; `dmc folder` prints `~/dmc`.
3. List `~/dmc`: `CLAUDE.md`, `.claude/`, `templates/`, `.dmc/`, and empty `adventures/`, `feedback/`, `modules/`,
   `rules/` ([data-model.md](data-model.md#dmc-folder)). No `src/`, `tests/`, `.docs/`, `review-*` or `speckit-*`.
4. `dmc play` from any folder. In the session:
   - type `/dmc`: the ten `dmc-*` skills are listed;
   - start a new campaign and play one scene: rolls from the Bash tool and from the PowerShell tool both run `dmc`
     bare; a map is drawn and the Stop hook opens it; files are saved under `adventures/`;
   - `/context`: compare with a session from the repository today (SC-005);
   - ask DMC to edit `CLAUDE.md` or a skill: refused; ask it to run `dmc update`: refused;
   - `/compact`: DMC still follows its rules and resumes from the files.
5. Quit, `cd ~/dmc; claude`: the same rules, skills and hooks load without `dmc play`.

## 2. The player's files survive (User Story 2)

1. In a DMC folder with campaigns, player options, feedback, a module and a rules file, record hashes:
   `Get-ChildItem adventures,feedback,modules,rules,exports -Recurse -File | Get-FileHash | Export-Csv before.csv`.
2. Run `dmc install --path <same folder>`, then `dmc refresh`, then (when a newer release exists) `dmc update`.
3. Hash again and compare: identical, nothing added or removed.
4. Add `.claude/settings.local.json` and `.claude/skills/my-skill/SKILL.md`; refresh; both are unchanged.

## 3. Released versions only, updated in place (User Story 3)

1. Change a line in the repository's `kit/dm-rules.md` on a branch; in the DMC folder, the session still has the
   released text.
2. With a newer release: `dmc update` prints `installed dmc X.Y.Z (was ...)` and the refresh line;
   `.dmc/manifest.json` names `X.Y.Z`. A second run says it is already the latest.
3. With `DMC_UPDATE_TEST_ASSETS` pointing at assets whose archive has one byte changed: `dmc update` refuses, and
   `dmc --version` and the generated files are unchanged.
4. Edit `templates/session-log.md` by hand and delete `.claude/skills/dmc-draw-map/SKILL.md`; `dmc play` refreshes
   first, restores the skill, replaces the template and keeps the edited one under `.dmc/replaced/`.

## 4. Feedback (User Story 4)

1. In play, record feedback ("feedback: ..."): the entry in `feedback/` has `dmc: X.Y.Z`.
2. `dmc update` (or a refresh): the entry is unchanged.
3. In the repository, run `review-feedback`: it reads the entries of the folder `dmc folder` prints, or of a path given.

## 5. A developer tries a change (User Story 5)

1. Edit `kit/dm-rules.md`; `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`.
2. `artifacts/bin/dmc install --path D:\scratch\dmc` (the installed release is left alone; R14). Play there with
   `cd D:\scratch\dmc; claude --model sonnet`: the edited rule applies.
3. Start `claude` in the repository root: `/context` shows the developer `CLAUDE.md` only; `/` lists `review-feedback`,
   `review-transcript` and the `speckit-*` skills, and no `dmc-*` skill. Open `kit/dm-rules.md` in that session: the
   Dungeon Master rules are not loaded as instructions.
4. `play.ps1`, `play.sh` and `tools/get-dmc.*` do not exist.

## 6. The maintainer's campaigns move (User Story 6)

1. Copy the repository's `adventures/`, `feedback/`, `modules/` and `rules/` somewhere safe.
2. Install, then run the move commands from `.docs/user-guide.md` into the DMC folder.
3. `dmc play`, resume the active campaign; the recap matches the files; play one scene.

## Automated checks

`dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin; dotnet test` runs the toolkit tests on `kit/`, the
install/refresh/play/update/folder tests and the bootstrap tests (research R15). A pull request that touches the
release workflow runs its dry run: every archive is installed with the bootstrap from local assets into a temporary
DMC folder and checked.
