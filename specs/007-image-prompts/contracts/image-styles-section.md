# Contract: `## Image styles` in `adventures/player-options.md`

This is the shape the template ships and the skill reads and writes. The section is optional: a general file without it
has no saved styles, and the skill adds it, as the template gives it, when the player first saves a style.

## In `kit/templates/player-options.md`

The section goes after `## Style` and before `## Other requests`:

```markdown
## Image styles

<!-- Saved looks for image prompts (/dmc-generate-image-prompt), one line each: - **Name**: the look, in words for an image model (medium, palette, light, rendering, aspect ratio). Who is in the picture comes from the story, never from a style. "Last used" is the style a prompt gets when the player names none; with none saved, DMC uses its built-in Storybook style. Say "save that as <name>" after a prompt you like. -->

- **Last used**: none
```

## Filled in

```markdown
## Image styles

<!-- (as above) -->

- **Last used**: Looney Tunes
- **Looney Tunes**: classic golden-age Looney Tunes cartoon, bold ink outlines, squash-and-stretch poses ... 16:9
- **Concept art**: fantasy video game concept art, painterly and abstract ... cinematic 16:9
```

## Rules

1. **The heading.** The section's heading is exactly `## Image styles`, and there is one such heading in the file.
2. **Last used.** The first bullet is `- **Last used**: ` followed by a saved style's name, exactly as written in that
   style's line, or `none`.
3. **The styles.** Every other bullet is one saved style, `- **<Name>**: <description>`, on one line:
   - The name holds no `*`, is not `Last used`, and is unique within the section, ignoring case.
   - The description is not empty.
4. **What else may be there.** Nothing but the comment and blank lines.
5. **Order.** Styles are kept in the order they were saved. A rename keeps the style's place.

A campaign's `player-options.md` holds no image styles (FR-013).
