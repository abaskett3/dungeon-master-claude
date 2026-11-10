# Quickstart: checking image prompts

How to show the feature works end to end. What each part must do is in [contracts/](contracts/), and why in
[research.md](research.md).

## Prerequisites

- A build: `dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`.
- A scratch DMC folder outside any checkout, made in a terminal outside Claude Code:
  `artifacts\bin\dmc install --path D:\Scratch\dmc-007`.
- A copy of Rorn's Landing and of the general player options, taken from `D:\Claude\dmc\adventures\` and put into the
  scratch folder's `adventures/`. Never use the original.

## 1. Automated checks

```text
dotnet test
```

`ToolkitTests` must pass: the new skill, the README's list, and the template's section.

## 2. Rorn's Landing's styles, moved once

1. In a session on the scratch copy, ask DMC to save the Looney Tunes and Concept art styles from the campaign's
   player options, and to drop the old notes.
2. Check:
   - `adventures/player-options.md` has `## Image styles`, with both styles word for word, and Last used is Looney
     Tunes;
   - the campaign's Other requests has neither note;
   - `story/dm-notes.md` still says the player likes image prompts of scenes and poses, but not which style is the
     default.

## 3. The skill, live (SC-001 to SC-004)

In the scratch copy, after § 2, in a session, ask for at least 10 prompts. Among them:

| # | Ask | Expect |
|---|---|---|
| 1 | an image prompt of this, at a scene with Shale and humans | Shale a dwarf, about chest-high to the humans; everyone present; a wide shot; Looney Tunes |
| 2 | one with Kibil, in three sessions (a `/clear` and resume between) | Kibil's size in the same comparative terms each time |
| 3 | one with Wenna and Pepper | Pepper a grey cat, named as a cat |
| 4 | one with a human NPC | "human", with no elf traits |
| 5 | one with an enemy hidden and unspotted (set it in `dm-notes.md`'s Opposition first) | No figure, shadow, eyes or hint |
| 6 | "show me what's watching us" in that scene | One line declining, revealing nothing more |
| 7 | one of the reunion at the Landing | That scene from its log; nothing learned later |
| 8 | one needing a detail no file holds | The detail left out and named after the block. Then give it, and check the sheet gains it |
| 9 | "in Concept art", then one naming no style | Concept art both times; Last used is Concept art |
| 10 | "the same style, but more abstract" | Abstract; one line saying the saved style is unchanged |
| 11 | "surprise me", twice | Two different styles, each named and marked unsaved; Last used unchanged |
| 12 | "save that as Woodcut", then `/clear`, resume, and one naming no style | Woodcut, unasked |
| 13 | "in Watercolour" (not saved) | One line listing the saved names; no prompt |

After all of them, `game-state.md`, `story/canon.md` and `maps/` are unchanged, apart from the details settled in row 8
(FR-003).
