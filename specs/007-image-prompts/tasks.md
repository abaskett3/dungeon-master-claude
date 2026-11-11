---

description: "Task list for image prompts of the story, in saved styles (dmc-generate-image-prompt)"
---

# Tasks: Image Prompts of the Story, in Saved Styles

**Input**: Design documents from `specs/007-image-prompts/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (dmc-generate-image-prompt.md,
image-styles-section.md), quickstart.md

**Tests**: The toolkit tests check the prompts and the template. The live runs of quickstart § 3 check what DMC does,
since CI cannot run a model.

**Organization**: Tasks are grouped by user story:
- US1 (P1): a prompt whose picture matches the story;
- US2 (P2): the chosen style without asking;
- US3 (P2): saving, switching and DMC-chosen styles.

There is no migration and no `dmc` code (research R9).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1 to US3, as in spec.md.
- Every path is relative to the repository root.

## Conventions for every task

These come from the repository, not from this feature. Each task below assumes them.

**Prompts** (`kit/`), per `.docs/development.md` § Writing prompts:
- Write for a reader with none of the history.
- Every `dmc` command, `templates/` path and `/skill-name` named must be real.
- No clause that repairs older files: no "if an older", "lacks it)" and so on (`MigrationPromptTests.CatchUpClause`).

**The rule each part follows:**
- **Content:** FR-005 to FR-010 and FR-011a.
- **Style:** FR-011 to FR-016 and FR-015a.
- **Writes:** FR-003.

Cite them in the skill only as behaviour, never by number.

---

## Phase 1: Setup

**Purpose**: A known-good start.

- [X] T001 From the worktree, run `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin` and `dotnet test`. Record any
  test skipped locally, so later runs can tell a new skip from an old one.
  Done 2026-11-10: 5 skipped on Windows, `OnLinuxAReleaseIsLinkedAndThePlayerIsToldWhenTheLinkFolderIsNotOnThePath`,
  `ASymbolicLinkToAFileInTheCampaignIsRefused`, `ASymbolicLinkToAFolderInTheCampaignIsRefused`,
  `AGeneralPlayerOptionsFileThatIsALinkIsRefused` and `ACampaignHoldingAPathThatCouldNotBeImportedIsRefused`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The styles section's shape. Every story that touches styles (US2 and US3) reads or writes it.

- [X] T002 Add `## Image styles` to `kit/templates/player-options.md`, after `## Style` and before `## Other requests`,
  exactly as `specs/007-image-prompts/contracts/image-styles-section.md` § "In `kit/templates/player-options.md`" gives
  it: the comment, then `- **Last used**: none`.
- [X] T003 Add a test to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs` that checks the template's section against
  `contracts/image-styles-section.md` § Rules:
  - the file has exactly one `## Image styles` heading;
  - its first bullet is `- **Last used**: none`;
  - it holds nothing but the comment, that line and blank lines.

  Run it: it passes after T002.

**Checkpoint**: The template ships the section. A new player's general file gets it from `templates/player-options.md`;
an older one has none until the first save.

---

## Phase 3: User Story 1 - A prompt whose picture matches the story (Priority: P1) MVP

**Goal**: `/dmc-generate-image-prompt`, or asking in words, gives one prompt of the moment. Everyone in it is as the
files describe them, sizes are comparisons, nothing the character has not perceived is shown, and nothing is assumed.

**Independent Test**: quickstart § 3, rows 1 to 8, on a scratch copy of Rorn's Landing. Until US2 lands, the style is
whatever the skill's default section says.

- [X] T004 [US1] Create `kit/skills/dmc-generate-image-prompt/SKILL.md`, with the front matter exactly as
  `contracts/dmc-generate-image-prompt.md` § Front matter gives it, and these sections for the content:
  1. **"Change nothing in the game"**: no time passes, no roll, and no change to `game-state.md`, canon or the maps.
     The session log takes at most its one out-of-character line, without the prompt text.
  2. **"The scene"**: the current moment from `game-state.md` and the latest log, or a past moment the player names,
     found under `story/sessions/` (research R7). Unless the request says otherwise, everyone present, in a wide shot
     of the whole scene. Content the player supplies is kept; say in one line where it contradicts the story, and
     offer the closest version that agrees.
  3. **"Each figure"**, from the sheet or the `character-sheets/minor-npcs.md` row:
     - species (never another, and no trait of another), sex, age, build, colouring, clothing, gear, and marks and
       injuries at that moment;
     - size as a comparison with someone or something present, with a size category turned into a comparison by the
       rules' sizes (PR #186: by comparing the figures' size categories, the same size drawn the same, a smaller one
       smaller, a larger one bigger);
     - a creature named by its kind, never "pet";
     - a crowd described as a crowd, of the place's people.
  4. **"What stays out"**: point at the DM rules' "Say only what the listener could know", and list the visual forms
     from research R8 (an unspotted enemy, or its shadow, eyes or tracks; a disguised being's true form; a secret from
     `story/dm-notes.md` or a sheet; an item's hidden nature; anything from `modules/`; a future scene). A refused part
     gets one line that reveals nothing more.
  5. **"Never assume"**: a detail no file holds is left out and named in one line after the block. A detail the player
     then gives is written to the sheet's Appearance line, or to the minor-NPC row's "Distinguishing trait or voice"
     cell, and confirmed in a line (research R6).
  6. **"What to show"**: the reply shape of `contracts/dmc-generate-image-prompt.md` § What the reply looks like.

  For now, a short **"Style"** section saying: use the built-in Storybook description (research R4, word for word). US2
  replaces it.
- [X] T005 [P] [US1] Add `/dmc-generate-image-prompt` to the skill table in `README.md`, after
  `/dmc-migrate-campaign`: "Write a prompt for an image model of a moment in your game, true to the story and free of
  spoilers, in a style you choose or saved".
- [X] T006 [US1] Run `dotnet test --filter FullyQualifiedName~Toolkit`. Fix the skill for any finding: the skill's name,
  the README list, the paths, the commands, a `/word-word` read as a skill name, or a catch-up clause.
- [X] T007 [US1] Run quickstart § 3, rows 1 to 8, live: publish, make a scratch DMC folder in a terminal outside Claude
  Code, copy Rorn's Landing in, then `dmc play`. Revise `kit/skills/dmc-generate-image-prompt/SKILL.md` for anything
  DMC got wrong, and record what it got wrong for the PR (SC-001 to SC-003).
  **Skipped** by the user's decision (2026-11-10): no live runs before the PR; bugs found in play are fixed after.

**Checkpoint**: Prompts are true to the files and spoiler-free, in Storybook.

---

## Phase 4: User Story 2 - The style the player chose, without asking every time (Priority: P2)

**Goal**: A request naming no style gets the last saved style used, or Storybook when none is saved.

**Independent Test**: quickstart § 3, rows 9 and 12, plus User Story 2's scenarios: no saved style gives Storybook and
one line saying styles can be saved; Last used is followed; a described style does not change it.

- [X] T008 [US2] In `kit/skills/dmc-generate-image-prompt/SKILL.md`, replace the interim "Style" section with
  **"Choose the style"**, in the order of FR-011:
  1. A style described in the request, as given. A variation of a saved style ("the same style, but more abstract")
     counts as described too, and so does a style DMC chooses when asked for something different without a
     description.
  2. A saved style the request names.
  3. The style on `- **Last used**:` in `adventures/player-options.md` § Image styles.
  4. Storybook, whose description is kept in this section (research R4).

  Also say:
  - The style holds the look only (medium, palette, light, rendering, aspect ratio). Where a style's words say who is
    shown or how wide the shot is, the content rules win; scene details such as rain or night are content.
  - With no saved style, add one line saying which style was used and that "save that as <name>" keeps a look.
- [X] T009 [US2] In the same file, add **"The last used style"**: after a prompt in a saved style (named, used as the
  default, or varied), set `- **Last used**:` to that style's name when it differs. Never set it for a style described
  in the request or one DMC chose. The section's shape is in `templates/player-options.md`.
- [X] T010 [US2] Rerun T006's filter, then quickstart § 3, rows 9 and 12, live. Revise the skill for anything DMC got
  wrong (SC-004).
  **Skipped** by the user's decision (2026-11-10): no live runs before the PR; bugs found in play are fixed after.

**Checkpoint**: The default follows the player without being repeated.

---

## Phase 5: User Story 3 - Saving, switching and DMC-chosen styles (Priority: P2)

**Goal**: The player saves a look, manages saved styles, varies one without changing it, and can ask DMC to choose a
different style.

**Independent Test**: quickstart § 3, rows 10, 11 and 13, plus User Story 3's scenarios 3, 4 and 6: save after
refining; list, rename, change and delete; an unknown name.

- [X] T011 [US3] In `kit/skills/dmc-generate-image-prompt/SKILL.md`, add **"Saved styles"**, for the requests in
  `contracts/dmc-generate-image-prompt.md`'s table:
  - **"save that as <name>"** saves the last prompt's style, refinements included, as
    `- **<Name>**: <description>` on one line, and makes it the last used.
  - **"list"**, **"rename"**, **"change"** and **"delete"** each work on one style. A rename keeps the style's place and
    updates Last used if it named that style. Deleting the last used sets it to `none`.
  - **A name that is not saved** gets one line listing the saved names, and no prompt.
  - **Two similar names** get one line asking which.
  - **The rules for a name** (`contracts/image-styles-section.md` § Rules): unique ignoring case, never `Last used`,
    and no `*`.

  If the general file has no `## Image styles` section, the first save writes it as `templates/player-options.md`
  gives it. Each action is confirmed in one line. A campaign's `player-options.md` never holds styles.
- [X] T012 [US3] In the same file, add **"A variation"**: use it for this prompt only and leave the saved style
  unchanged. Say in one line that it is unchanged, for example "I haven't changed Concept art; say if you want this
  change saved into it". On a yes, rewrite that style's description and confirm in a line.
- [X] T013 [US3] In the same file, add **"Something different"**: when asked for a different style without a
  description, choose one clearly unlike the last prompt's style and every style DMC chose earlier in this session.
  Describe it in full in the prompt, and label it with a short name marked unsaved. Do not save it, and do not change
  Last used.
- [X] T014 [US3] Rerun T006's filter, then quickstart § 3, rows 10, 11 and 13, and User Story 3's scenarios 3, 4 and 6,
  live. Revise the skill for anything DMC got wrong.
  **Skipped** by the user's decision (2026-11-10): no live runs before the PR; bugs found in play are fixed after.

- [X] T015 [US3] Run quickstart § 2 live: move Rorn's Landing's two styles by asking, on the scratch copy.
  **Skipped** by the user's decision (2026-11-10): no live runs before the PR; bugs found in play are fixed after.

**Checkpoint**: Every style request in the contract's table works. The skill is complete.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: The documents, the full run, the review and the PR.

- [X] T016 [P] Update `.docs/user-guide.md` with a short section on image prompts: how to ask, what comes back, styles
  (the default, saving, switching, varying, "surprise me"), that nothing the character has not seen is shown, and that
  missing details are asked for, never made up.
- [X] T017 [P] Update `.docs/campaign-files.md`: the general `player-options.md` row gains Image styles (optional,
  written by `/dmc-generate-image-prompt` when the player saves or uses a style). Update `.docs/architecture.md`'s
  Skills row: twelve procedures, naming `dmc-generate-image-prompt`.
- [X] T018 [P] Add to `.docs/decisions.md` (Decision, Reason, Limits, Source):
  - "Image styles live in the general player options";
  - "Image prompts state facts only; a missing detail is asked for, never assumed";
  - "Image styles need no migration" (research R9: an optional section; one campaign's notes moved by asking).
- [X] T019 [P] Add a row to `.docs/roadmap.md` for issue #178: status, and what ships (the skill and the optional
  section).
- [X] T020 Publish, run `dotnet test`, and fix any failure. Confirm nothing new is skipped beyond T001's list.
- [ ] T021 Run `/speckit-converge` until it adds no tasks, then Claude Code's built-in code-review command once on the
  finished branch. Present its fixes for approval before committing them.
- [ ] T022 Write the PR description per `CLAUDE.md` § Git and GitHub:
  - what changed, and why (the Rorn's Landing problems);
  - the sources (spec, research, issue #178);
  - how it was tested (T020, and the live runs T007, T010, T014 and T015, with what DMC got wrong);
  - the documents changed;
  - `Closes #178`;
  - the migration line: none needed, because the section is optional and no campaign file changes shape (research R9).

  Check it with `gh pr view <n> --json body`. Ask the user to merge only once CI passes.

---

## Dependencies & Execution Order

- **Setup (T001)**: first.
- **Foundational (T002, T003)**: before US2 and US3. US1 does not need it.
- **US1 (T004 to T007)**: after Setup. It is the MVP.
- **US2 (T008 to T010)**: after US1 (the same file) and Foundational.
- **US3 (T011 to T015)**: after US2 (the same file).
- **Polish (T016 to T022)**: after every story.

### Parallel Opportunities

- T005 beside T004.
- T016 to T019 together.

The three stories write one skill file, so they run one after another.

## Implementation Strategy

### MVP First (User Story 1)

1. T001, then T004 to T007: prompts true to the files and spoiler-free, in Storybook.
2. Stop and validate with quickstart § 3, rows 1 to 8. This fixes what the player reported: Shale's height, Kibil's
   size, Pepper, the half-elves and the spoilers.

### Incremental Delivery

1. US1: the content.
2. Foundational and US2: the section and the default.
3. US3: managing styles, and Rorn's Landing's styles moved by asking.
4. Polish.

There is one PR for the whole feature (one issue, #178).

---

## Notes

- [P] tasks touch different files and depend on no unfinished task.
- A kit change reaches players only with a new `dmc` release and a refresh of their DMC folder. Say so in the PR.
- `install` and `refresh` refuse inside Claude Code. The live runs need a terminal outside the session.
- Never run a live test on `D:\Claude\dmc` itself: always on a copy.
- Commit after each task or logical group. The user merges.

## Phase 7: Convergence

- [X] T023 In `kit/skills/dmc-generate-image-prompt/SKILL.md` § Saved styles, let a save also take a style the player gives in words or points at (an old note in a campaign's `player-options.md`), word for word, and drop the old note, and any `story/dm-notes.md` line that only repeats it, when the player asks, per spec Assumptions and quickstart § 2 (partial)
