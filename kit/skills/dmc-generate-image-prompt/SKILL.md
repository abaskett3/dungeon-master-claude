---
name: dmc-generate-image-prompt
description: Write a prompt for an image-generation model of a moment in the campaign, true to the campaign files and with nothing the character has not seen, in the player's chosen or saved style. Use whenever the player asks for an image prompt or a picture prompt of a scene, a moment, a pose or a character, and when they ask to save, switch, list, change or delete an image style.
---

# Image prompt

An image prompt is text the player pastes into an image-generation model outside DMC; DMC makes no image itself. It has two parts, and each has its own rules:

- **The content**: who and what is in the picture, and how wide the shot is. It comes from the campaign files and the request, never from a style.
- **The style**: how the picture looks (medium, palette, light, rendering, aspect ratio). It comes from the request, a saved style or the built-in default.

## Change nothing in the game

A prompt is out of character. No time passes, nothing is rolled, and `game-state.md`, `story/canon.md` and the maps do not change. Write nothing to the session log: it keeps out what the player says out of character, and it never holds a prompt. The only writes are the saved styles (§ Saved styles, § The last used style) and an appearance fact the player gives (§ Never assume).

With no campaign loaded, ask which campaign the picture is from, or write the prompt from the player's description alone and say in a line that it is not checked against any campaign.

## The scene

- **When**: the current moment, from `game-state.md` (place, time, light, weather, who is present) and the end of the latest session log. For a past moment the player names ("the reunion at the Landing"), find it under `story/sessions/` by the place or the people, read that scene, and take who was there, their injuries and the time from it. Where a figure looked different then, the sheet's History says how. Show nothing the character learned only later.
- **Who and how wide**: unless the request says otherwise, everyone present, in a wide shot of the whole scene. The player may ask for fewer figures, a closer shot, a pose or another subject.
- **Content the player supplies** ("Shale riding Kibil over the river") is kept. Every figure is still as the files describe them. Where the scene contradicts the story, say so in one line and offer the closest version that agrees ("Kibil can't carry Shale yet: he's hound-sized. Shale running beside him?").

## Each figure

Describe every named figure from their sheet in `character-sheets/`, or their row in `character-sheets/minor-npcs.md`:

- **Species**, exactly as the files give it, with no trait of another species: a human gets no pointed ears, and a half-elf is a half-elf only if the sheet says so. Then sex where the files give it, age, build, colouring, clothing, gear, and marks and injuries at that moment (injuries and conditions from `game-state.md`).
- **Size** as a comparison with someone or something in the picture: "a stocky dwarf, about chest-high to the humans beside him", "the size of a big hound". Use the comparison the files give, in the same words each time, so a size holds from picture to picture. A size given only as a category (Small, Medium) is turned into a comparison by the sizes in the campaign's rules; that follows from the rules and is not a choice.
- **A creature** is named by its kind (a cat, a dog, a wyrmling) with its row's details, never only as "her pet".
- **A crowd** is described as a crowd of the place's people, with no species the setting's files do not put there.

## What stays out

The picture is something you tell the player, so the DM rules' "Say only what the listener could know" governs it: it holds only what the character has perceived at that moment. In a picture a spoiler takes these forms, and none of them appears, not even as a hint:

- an enemy the party has not spotted, or its shadow, eyes or tracks;
- a disguised being's true form or colours;
- a secret from `story/dm-notes.md` or from a sheet's secrets;
- an item's hidden nature (a glow, a curse);
- anything from `modules/`;
- a scene that has not happened.

Read `story/dm-notes.md` and the sheets' secrets only to know what to leave out. A part of the request that would show one of these is declined in one line that reveals nothing more ("Shale hasn't seen that yet"), and the rest of the picture is written as the character knows it.

## Never assume

A visual detail no file holds (hair colour, a cloak's colour, a pet's breed) is not a fact of the game, and choosing one is not consistency. Leave it out of the prompt and name the missing details in one line after the block. When the player then gives one ("his hair is copper-red"), write it to the figure's `- **Appearance**:` line (a PC, NPC or companion), or to the "Distinguishing trait or voice" cell of their row in `character-sheets/minor-npcs.md`, and confirm it in a line. Every later prompt uses it.

## Choose the style

Take the first of these that applies:

1. **A style described in the request**, used as given ("as a charcoal sketch"). A variation of a saved style ("the same style, but more abstract") counts as described: the saved style with the change (§ A variation). So does a style you choose when the player asks for something different without describing it (§ Something different).
2. **A saved style the request names** ("in Concept art"), from the `## Image styles` section of `adventures/player-options.md`.
3. **The last used style**: the style named on that section's `- **Last used**:` line.
4. **Storybook**, the built-in default, when the Last used line says `none` or the section is missing:

   > a high-fantasy storybook illustration: rich painted colour over soft ink linework, warm golden light, gentle textured brushwork in the manner of a classic illustrated fairy-tale book, detailed inviting backgrounds, expressive faces, 3:2

A style holds the look only. Where a style's words say who is shown or how wide the shot is ("a wide shot showing everyone present"), the content rules above win, so a change of style never adds, drops or changes a figure. Scene details given with a style (rain, night) are content. Use the player's words as given, a living artist or a studio included, and add no model-specific syntax (weights, negative prompts, flags) unless the style or the request asks for it.

## The last used style

After a prompt in a saved style (named, used as the default, or varied), set `- **Last used**:` to that style's name if it is not already. A style described in the request, or one you chose, never changes it. The section's shape is in `templates/player-options.md`.

## Saved styles

Saved styles live only in the general `adventures/player-options.md`, never in a campaign's `player-options.md`. Each is one line, `- **<Name>**: <description>`, kept in the order saved, below the Last used line. A name is unique ignoring case, is never `Last used` and holds no `*`; a description is one line saying how the picture looks. If the general file has no `## Image styles` section, the first save writes it as `templates/player-options.md` gives it, after `## Style`.

| The player says | Do |
|---|---|
| "save that as Woodcut" | Save the last prompt's style, refinements included, under that name, and make it the last used |
| "save this style as Woodcut: ...", or "save the Concept art style from my notes" | Save the words the player gives, or the note's style word for word, under that name. Make it the last used only if Last used is `none` or the note calls it the current default. On the player's word, remove the old note, and any `story/dm-notes.md` line that only repeats it |
| "list my styles" | List the saved names, marking the last used |
| "rename Woodcut to Ink" | Rename it in place; if Last used named it, rename it there too |
| "change Woodcut to ..." | Rewrite its description |
| "delete Woodcut" | Remove its line; if it was the last used, set Last used to `none`, so the default is Storybook until another saved style is used |

Confirm each in one line. A name that matches no saved style gets one line listing the saved names and no prompt, until the player picks or describes a style. Two saved names the request could mean get one line asking which.

## A variation

A request that varies a saved style ("the same style, but more abstract") uses the saved style with the change, for that prompt only, and the saved style is unchanged. Say so in one line ("I haven't changed Concept art; say if you want this change saved into it"). On a yes, rewrite that style's description with the change and confirm in a line.

## Something different

When the player asks for a different style without describing one ("try something else", "surprise me"), choose one clearly unlike the last prompt's style and every style you chose earlier in this session. Describe it in full in the prompt, and label it with a short name marked unsaved, so the player can keep it with "save that as <name>". Do not save it, and do not change Last used.

## What to show

One prompt per request, out of character, in this shape and nothing else:

```text
<the style's name, or "<short name> (unsaved)">
> <the prompt: the content, then the style, as one block of prose ready to copy>
```

After the block, at most: one line naming missing details, one line on a contradiction, and one line of nudge. The nudge follows a variation (§ A variation), or a prompt in Storybook with no saved styles: which style it used, and that "save that as <name>" keeps a look. Follow the request on language or length for that prompt. The reply never mentions anything § What stays out keeps out. Then go back to the fiction.
