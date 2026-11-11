# Research: Image Prompts of the Story, in Saved Styles

Phase 0 of [plan.md](plan.md). Each entry is a decision, why it was made, and what else was considered. The facts come
from the kit, the `dmc` source and the Rorn's Landing campaign in `D:\Claude\dmc` as they stood on 2026-11-10.

## R1. One player skill, invoked by its description

- **Decision:** One file, `kit/skills/dmc-generate-image-prompt/SKILL.md`. It has no `disable-model-invocation`, so DMC
  runs it when the player asks for an image prompt in words, as well as by `/dmc-generate-image-prompt`. Its sections
  follow the order of the work: what must not change, the scene, each figure, what stays hidden, missing details, the
  style, the saved styles, and what to show.
- **Rationale:** FR-001 asks for both ways in. Every player skill works this way except where the play settings deny
  one. One file keeps the rules in one home (Principle III).
- **Alternatives:** Two skills, one for content and one for styles. Rejected: a prompt always needs both, and the
  player would have to know which to call.

## R2. No line in the DM rules

- **Decision:** `kit/dm-rules.md` does not change. `## Skills` already says "Use this project's skills yourself whenever
  they apply", and Claude Code lists every skill's description in each session, so the description is the trigger. The
  only cost to every session is the description, about 400 bytes. The rest of the skill is read only when it runs.
- **Rationale:** FR-004 and SC-005 allow at most a pointer, and none is needed. The spoiler rule ("Say only what the
  listener could know", `dm-rules.md` line 81) stays where it is, and the skill points at it instead of restating it.
- **Alternatives:** A line under `## Skills` naming the skill. Rejected: it would repeat the description that Claude
  Code already lists.

## R3. The styles section in the general player options

- **Decision:** `kit/templates/player-options.md` gains `## Image styles` after `## Style`. It holds one bullet per
  line:
  - `- **Last used**: <name>`, or `none`.
  - One `- **<Name>**: <description>` line per saved style.

  Names are unique ignoring case, and each description is one line. A comment explains the section and names the
  built-in default. The exact shape is in [contracts/image-styles-section.md](contracts/image-styles-section.md).
- **Rationale:** FR-013 wants a section with a fixed shape. One line per entry is how the file already writes every
  preference, can be checked by a test, and fits how a style is used: words handed to an image model in one
  piece. The heading is "Image styles" rather than "Styles" because the file already has `## Style`, for narration.
  The section is optional (R9).
- **Alternatives:**
  - A `### <Name>` sub-heading per style. Rejected: it doubles the lines and invites multi-paragraph styles.
  - A separate file, `adventures/image-styles.md`, which resume would not read. Rejected: the player chose the player
    options (clarification of 2026-11-10). R5 weighs the cost.

## R4. The built-in default, "Storybook"

- **Decision:** The skill holds the built-in default's description, under the name `Storybook`:

  > a high-fantasy storybook illustration: rich painted colour over soft ink linework, warm golden light, gentle
  > textured brushwork in the manner of a classic illustrated fairy-tale book, detailed inviting backgrounds,
  > expressive faces, 3:2

  It is never written into the player's file unless the player saves it (spec, Assumptions).
- **Rationale:** FR-011 (4) asks for a high-fantasy storybook style that ships with the kit. Following FR-011a, it
  describes the look and the aspect ratio only: who is in the picture and how wide the shot is stay with the content.
- **Alternatives:** No aspect ratio. Rejected: both of the player's own styles end with one ("16:9"), so a style that
  gives none would read as unfinished next to them.

## R5. What saved styles cost a resume

- **Decision:** Accept the cost, and add no budget for `adventures/player-options.md` (`BudgetKind.None` in
  `src/Dmc.Cli/Size/ReadSet.cs` stays).
  - The resume reads the general file whole (step 2.1), so each saved style costs about 100 to 200 tokens a session.
  - In Rorn's Landing the two styles already cost that from the campaign's `player-options.md`. Moving them is neutral,
    and dropping the `dm-notes.md` lines that repeat the default is a saving.
  - `dmc size` already lists the general file, so a long list of styles would show.
- **Rationale:** The styles are the player's own data, not toolkit rules (Principle II is about toolkit work). A budget
  would refuse something the player asked to keep.
- **Alternatives:** Have the resume skip the section. Rejected: a Read cannot skip part of a file, and having DMC read
  the file in parts would cost more than it saves.

## R6. Content from the files, and only facts

- **Decision:** The skill builds the content from the files in the order the dm-rules give for truth:
  - **The scene:** `game-state.md` (place, time, light, weather, who is present) and the tail of the latest session log.
  - **Each figure:** its sheet's species, sex, age, Appearance and gear, or its row in `minor-npcs.md`.
  - **Injuries and conditions:** from `game-state.md` for the current moment, from that scene for a past one (R7).

  Two rules govern it:
  - **Size:** stated as a comparison with someone present. Where the files give none, or only a size category (Small,
    Medium), the species' usual height from the campaign's rules is stated against the others present: a dwarf and a
    human are both Medium, so the category alone is not enough. That is a rule, not a choice (spec, Assumptions).
  - **Missing details** (FR-009): the prompt leaves out a detail the files do not hold. DMC names it after the block,
    and writes what the player then says to the sheet's Appearance line, or to the "Distinguishing trait or voice" cell
    of a minor NPC's row.
- **Rationale:** The clarification of 2026-11-10: "consistency rooted in fact", not assumption. Both sheet templates
  have an Appearance line (`kit/templates/npc-sheet.md`, `pc-sheet.md`), and the minor-NPC table has a description cell.
- **Alternatives:** A new "Look" section in the sheets, for image prompts only. Rejected: it would be a second home for
  appearance, and a campaign-file standard change for no gain.

## R7. Past moments

- **Decision:** For a past moment, the skill finds it in `story/sessions/` (searching for the place or the people the
  player names), reads that scene, and takes who was there, injuries and the time from it. A sheet's history, or the
  `character-sheets/<sheet name>-history.md` file the full save moves older history to, gives what a figure looked like
  then, when it has changed since. Anything the log does not say is a missing detail (R6).
- **Rationale:** FR-005. The files keep only the current state, so the logs are the only record of how things stood.

## R8. What stays out

- **Decision:** The skill points at the dm-rules rule and lists the visual forms a spoiler takes, because those are new
  to pictures:
  - a hidden or unspotted enemy, or its shadow, eyes or tracks;
  - a disguised being's true form or colours;
  - a secret from `story/dm-notes.md` or a sheet;
  - an item's hidden nature (a glow, a curse);
  - anything from `modules/`;
  - a future scene.

  The test for each is "has the character perceived this?". A refused part of a request gets one line that reveals
  nothing more ("Shale hasn't seen that yet").
- **Rationale:** FR-008. The picture is something DMC tells the player, so the existing rule covers it. Only the
  visual forms are new.

## R9. No migration

- **Decision:** The feature adds no migration and changes no `dmc` code.
  - The `## Image styles` section is optional. A general file without it has no saved styles, and the skill writes the
    section when the player first saves a style (FR-017).
  - Rorn's Landing's two style notes move once, in play, when the player asks DMC to save them.
- **Rationale:** Image styles are not story lore or canon (the user's ruling, 2026-11-10), so moving them needs no
  migration's proof that nothing was lost; and the feature is image prompts. No campaign file changes shape, and an
  optional section that an older file can simply lack is the case `.docs/development.md` § Changing a campaign-file
  standard names as needing no migration. Writing the section on the first save is the skill's ordinary write, not a
  repair of an older file, so the toolkit's catch-up rule does not apply. One campaign with two notes does not justify
  the cost of a migration that changes a file outside the campaign:
  - a new `general` front-matter key;
  - the general file included in `verify`;
  - a guarded general-file restore in `restore`;
  - a fixture.
- **Alternatives:** A migration (005) moving the notes into the general file, with `verify` and `restore` extended to
  that file. Rejected as out of scope.

## R10. Tests and live runs

- **Decision:**
  - **Toolkit tests** (`ToolkitTests`): the new skill is listed in the README, and its paths, commands and skill names
    are real; it has no catch-up clause; the template's section has the contracted shape.
  - **Live runs**, from [quickstart.md](quickstart.md), since CI cannot run a model: the SC-001 to SC-004 set, at least
    10 prompts with hidden enemies and a disguised NPC.
- **Rationale:** Principle V. The tests are pure .NET, with no outside tool, so nothing skips.
