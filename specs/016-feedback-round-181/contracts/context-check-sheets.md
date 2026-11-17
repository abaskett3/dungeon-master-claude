# Contract: `dmc context-check`'s sheet lines and out-of-character line

`dmc context-check` runs before each player message (`kit/settings.json`, `UserPromptSubmit`). Today it prints the
break line, then the canon reminder (`src/Dmc.Cli/Retrieval/CanonReminder.cs`). This adds two parts after them. Like
the rest of the command, any failure prints nothing and exits 0.

## Sheet lines (FR-008, research R4)

**When:** the prompt is not a command (`/…`); the active campaign is open; `game-state.md` has a Present line.

**Who:** each sheet (`character-sheets/*.md`, not `minor-npcs.md` or `*-history.md`, not a PC's) whose title the Scene
block's Present line or the Combat tracker names, matched as the resume matches them (`ReadSet.PresentText`,
`ReadSet.Mentions`). A crowd or any name with no sheet prints nothing.

**Skipped:** a person whose lines this hook printed, or whose sheet a Read tool call in the log opened, within the last
100,000 tokens of context since the latest compaction boundary.

**Lines:** from the sheet, in this order, each the whole line as written, the first of each pair found:

| Field | Play card shape | Older shape |
|---|---|---|
| What they look like and wear | `- **Looks**:` | `- **Appearance**:` |
| Where they are | `- **Usually found**:` | `- **Usually found**:` |
| How they move | `- **AC, max HP, speed, saves, senses**:` | `- **Speed**:` |
| What they can and cannot do | `- **Standing traits that change play**` | `- **Traits**:` |
| What they carry | `- **Items used in play**:` | none |

A sheet with none of them falls back to the canon reminder for that person (today's behaviour).

**Output:**

```
Sheets of Hala Rornsdottir; Tam Birch (what their sheets say now; read the sheet for more):
character-sheets/npc-hala-rornsdottir.md | - **Appearance**: …
character-sheets/npc-hala-rornsdottir.md | - **Speed**: 30 ft, **no fly speed: she cannot fly yet** (the player, s59.1)
…
```

**Cap:** 10,000 bytes, header included. People in the Present line's order; a person whose lines do not all fit is left
out of the header, so a later message tries again.

## Out-of-character line (FR-011, research R5)

**When:** the prompt, after leading spaces, starts with `ooc` (any case) followed by a space, `:`, or the end; or holds
`((` anywhere (the player options allow an aside in double brackets mid-message; widened after the code review of PR #230).

**Output:** one line, about 230 bytes, the wording of FR-010 as approved: "Out of character: if this says a fact is
wrong or asks how someone knew, read the files, open your reply by quoting what they say, for and against, say what you
think, propose one fix, and change no file until the player agrees." (Approved 2026-11-16 after the headless run; the
first draft let DMC change `game-state.md` and did not ask for the quotes first.)

**Silent:** for in-character text and for "ooc" anywhere but the start.

## Tests

`tests/Dmc.Tests/ContextCheck/`: a Present line with a companion, an NPC of the older shape, a crowd and a name with no
sheet; the cap with 8 people; the 100,000-token skip, after a hook print and after a resume's Read; a compaction resets
it; each `ooc` form and the silent cases.
