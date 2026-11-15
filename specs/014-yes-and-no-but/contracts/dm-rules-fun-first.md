# Contract: `kit/dm-rules.md` § Fun first, three bullets

The play rules are this feature's interface: DMC reads them on every turn. Lines 165, 166 and 169 change in place;
lines 167 and 168 do not. Measured on a scratch copy of `main` at `5b0e7a8`: 212 lines before and after, 29,856 bytes
before, 30,541 after (+685).

## Line 165

Before:

> - Pay attention to what this player enjoys (combat, intrigue, exploration, banter, puzzles) and give them more of
>   it. Note what you learn in `dm-notes.md`.

After:

> - Pay attention to what this player enjoys (combat, intrigue, exploration, banter, puzzles) and give them more of
>   it. Note what you learn in `dm-notes.md`, and weave what the character wants into the main plot through the Arc
>   plan's Character arcs.

Meets FR-005.

## Line 166

Before:

> - Say yes to creative ideas when you can. When a strict reading would kill the momentum over something minor, rule
>   in favor of the cool idea and keep moving.

After:

> - Meet a bold idea with "yes, and" (a technique, never said): it happens, plus a detail or hook (the
>   chandelier holds, and a key ring hangs by the ledge). Routine acts get nothing added. When a strict reading
>   would kill the momentum over something minor, rule for the cool idea. When the world rules an idea out, use "no,
>   but" in the fiction: turn it aside, with another way forward (no door in that wall, but a draughty crawlspace).
>   Never say out of character that the story does not allow it.

Meets FR-001, FR-002, FR-003 and FR-006. "Yes, and" and "no, but" are named as techniques; the examples show the
technique without the words, so DMC never says them to the player.

## Line 169

Before:

> - Recklessness is different from bad luck. Signal serious danger clearly in the fiction, once. If the player goes
>   ahead anyway, let the consequences land in full, including character death.

After:

> - Recklessness is different from bad luck. Signal serious danger clearly in the fiction, once. If the player goes
>   ahead anyway, let the consequences land in full, including character death. Except the first attack on an NPC
>   the main plot needs (central in the module, or in the Arc plan or Opposition table), at any lethality: a "no,
>   but", where they stop it, no one dies, and a canon line records it. After that, the dice fall where they fall.

Meets FR-004 and FR-007.

## What the toolkit tests hold

- `TheAlwaysLoadedRulesStayAtTheirLineCountAndGrowByAtMostFourHundredBytes`, renamed
  `TheAlwaysLoadedRulesStayAtTheirLineAndByteCaps`: still 212 lines; `DmRulesBytesAllowed` becomes the file's new size
  (30,541 if the wording lands as above), and the comments and message say which spec set each cap.
- A new test: § Fun first contains `"yes, and"` and `"no, but"`.

Each line is one line of the file; the wrapping above is for reading only.
