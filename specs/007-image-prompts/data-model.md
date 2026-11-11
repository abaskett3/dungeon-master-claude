# Data Model: Image Prompts of the Story, in Saved Styles

Phase 1 of [plan.md](plan.md). Nothing here is a database: these are the files DMC reads and writes, and what `dmc`
checks.

## Image prompt (not stored)

The text DMC shows, once per style the request asks for.

| Part | Source | Rule |
|---|---|---|
| Label | The style's name, or a short name marked `unsaved` | FR-002 |
| Content | `game-state.md`, the latest session log (or the named past scene), the sheets, `minor-npcs.md` | FR-005 to FR-010, FR-011a |
| Style | Chosen in the order of FR-011 | FR-011 |
| Notes | At most the missing-details line (FR-009), the contradiction line (FR-010) and the nudge, once, after the last block | FR-002 |

The content has two rules:
- **Who and what is shown:** everyone present in a wide shot of the whole scene, unless the request says otherwise
  (FR-005).
- **What a figure may hold:** only what the files hold (FR-006, FR-007, FR-009), and nothing the character has not
  perceived (FR-008).

## Saved style (stored)

A line in the `## Image styles` section of `adventures/player-options.md`
([contracts/image-styles-section.md](contracts/image-styles-section.md)).

| Field | Rule |
|---|---|
| Name | Unique ignoring case; never `Last used`; the player's choice |
| Description | One line; the look (medium, palette, light, rendering, aspect ratio); never who is shown (FR-011a) |

What changes it:
- **A save** creates it (FR-012).
- **"Change it"**, or saying yes to the nudge after a variation, rewrites its description (FR-012, FR-015a).
- **A rename or a delete** (FR-012).

## Last used (stored)

The `- **Last used**:` line of the same section: a saved style's name, or `none`. A general file without the section
reads as `none`, and the first save writes the section from `templates/player-options.md`.

| Event | Last used becomes |
|---|---|
| A prompt in a saved style (named, as the default, or varied) | That style, if it differs (FR-015) |
| A prompt in a style described in the request, or one DMC chose | Unchanged (FR-014) |
| The style is saved | Its name (FR-012, User Story 3) |
| It is renamed | The new name |
| It is deleted | `none` (FR-012, User Story 3: the next default is Storybook until another saved style is used) |

## Built-in default (in the kit)

`Storybook`, described in the skill ([research.md](research.md) R4). It is not stored in the player's file and cannot be
deleted. If the player saves it, it becomes an ordinary saved style.

## Session state (not stored)

The styles DMC chose itself this session, so that "something different" is unlike each of them (FR-011 (1)). It lives
in the conversation only: a new session may offer one again.

## Appearance fact (stored, existing fields)

A visual detail the player settles after a prompt names it as missing (FR-009). It is written to an existing field, not a
new one:

- **A PC, NPC or companion:** the `- **Appearance**:` line of `character-sheets/<pc|npc>-<name>.md`.
- **A minor NPC:** the "Distinguishing trait or voice" cell of their row in `character-sheets/minor-npcs.md`.
