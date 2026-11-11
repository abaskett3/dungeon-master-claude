# Contract: the `dmc-generate-image-prompt` skill

What the skill must do, as the player sees it. This is what `kit/skills/dmc-generate-image-prompt/SKILL.md` is written to,
and what the live runs in [../quickstart.md](../quickstart.md) check. The rules behind each point are in
[../spec.md](../spec.md).

## Front matter

```yaml
---
name: dmc-generate-image-prompt
description: Write a prompt for an image-generation model of a moment in the campaign, true to the campaign files and with nothing the character has not seen, in the player's chosen or saved style. Use whenever the player asks for an image prompt or a picture prompt of a scene, a moment, a pose or a character, and when they ask to save, switch, list, change or delete an image style.
---
```

There is no `disable-model-invocation`: DMC runs it when the player asks in words (FR-001).

## What the player can ask, and what comes back

| Request | Result |
|---|---|
| `/dmc-generate-image-prompt`, or "an image prompt of this" | One prompt of the current moment: everyone present, a wide shot of the whole scene, in the last saved style used, or Storybook |
| "...of the reunion at the Landing" | The same, of that past moment, from its session log |
| "...of Shale and Kibil at the gate" | That content. The figures are still as the files describe them; one line if the scene contradicts the story |
| "...as a charcoal sketch" | That style, unsaved |
| "...in Concept art" | That saved style; it becomes the last used |
| "the same style, but more abstract" | The varied style, and one line saying the saved style is unchanged and can be updated on the player's word |
| "something different" / "surprise me" | A style DMC chooses, unlike the last prompt's and any it chose this session, named and marked unsaved |
| "save that as Woodcut" | The last prompt's style saved under that name, now the last used; one line |
| "list my styles" / "rename X to Y" / "change X to ..." / "delete X" | Done, confirmed in one line |
| "...in Storybook and in Woodcut" | One label and prompt per style |
| "save that as Woodcut" when Woodcut (any case) is already saved | One line asking whether to replace it |
| A style name that is not saved | One line naming the saved styles; no prompt |
| A part of the picture the character has not perceived | One line declining it that reveals nothing more; the rest of the picture as the character knows it |

## What the reply looks like

````text
<one line: the style's name, or "<short name> (unsaved)">
```text
<the prompt alone, one paragraph: the content, then the style>
```
<at most: one line naming missing details; one line on a contradiction; one line of nudge after a variation or a first default>
````

The prompt sits alone in a fenced code block, so a copy takes the prompt and nothing more. A request for the same
picture in several styles gets one label and block per style. The reply is out of character. Nothing else is printed, and the reply never mentions anything in the hidden list
below.

## What the skill reads

- `game-state.md`: place, time, light, weather, who is present, injuries and conditions (for the current moment; a
  past moment takes them from its scene).
- `character-sheets/<sheet name>-history.md`, for a past look the sheet's own History no longer holds.
- The latest session log, or the past scene the player names (found under `story/sessions/`).
- The sheets of everyone shown, and `character-sheets/minor-npcs.md`.
- `adventures/player-options.md` § Image styles.

The skill also uses `story/dm-notes.md` and the sheets' secrets, for one purpose only: to know what must stay out.

## What the skill writes, and nothing else

- `adventures/player-options.md` § Image styles: a save, a change, a rename, a delete, and the Last used line
  (FR-012 to FR-015a).
- An appearance fact the player settles: the sheet's Appearance line, or the minor-NPC row's description (FR-009).
- At most the one out-of-character line the session log already takes for a request. It never holds the prompt's text.

It never writes `game-state.md`, canon, a map or a roll, and it never passes time (FR-003).

## Hidden: never in a prompt, never hinted (FR-008)

What the character has not perceived at that moment, by the dm-rules rule "Say only what the listener could know":

- an unspotted enemy, or its shadow, eyes or tracks;
- a disguised being's true form;
- a secret from `story/dm-notes.md` or a sheet;
- an item's hidden nature;
- anything from `modules/`;
- a future scene.

## Never assumed (FR-009)

A detail no file holds (hair colour, the colour of a cloak, a pet's breed) is left out of the prompt and named after
it. Size categories are turned into comparisons using the rules' sizes; that is a rule, not an assumption.
