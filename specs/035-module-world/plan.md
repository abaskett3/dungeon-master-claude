# Implementation Plan: Keep a module campaign in its module's world

**Branch**: `claude/project-thread-wno9kc` (spec 035) | **Date**: 2026-11-28 | **Spec**: [spec.md](spec.md)

## Summary

One bullet of `kit/dm-rules.md` § Running a published module (line 115) is rewritten to keep the party in the module's
world. Cuts in the same section that change no instruction pay for it, so the file shrinks by 4 bytes. The rule for
after the module goes in `dmc-new-campaign` step 4a, which the full save reads only when it plans the arc after a module
(`kit/skills/dmc-campaign-full-save/SKILL.md:44`), so play does not pay for it on every turn.

## Where each rule lives (Development 6)

| Rule | Home |
|---|---|
| Turn back in the fiction, a closed border holds, what comes from the module | `kit/dm-rules.md` § Running a published module, "Leave the script, not the world" |
| After the module: ask the player; keep the world's character | `kit/skills/dmc-new-campaign/SKILL.md` step 4a, "After a module" |
| A refusal in the fiction ("no, but") | Unchanged: `kit/dm-rules.md` § Fun first |

## Cuts that pay for the bullet (all in § Running a published module)

| Line | Before | After |
|---|---|---|
| 104 | "contains `module-progress.md`. That file names the module and records where the party is in it." | "holds `module-progress.md`, which names the module and the party's place in it." |
| 106 | "the original PDF or files the player supplied" | "the player's original files" |
| 109 | "module-specific stat blocks" | "the module's stat blocks" |
| 111 | "If the module has not been ingested yet, or the chapter you need is missing" | "If the module or the chapter you need is not ingested" |
| 113 | "You may half-remember … must not be used." / "trap details" | "Never trust your memory of a published adventure." / "traps" |
| 114 | "A new session must be able to tell exactly which rooms are cleared and which are untouched." | ", so a new session knows what is cleared." |
| 116 | "as described under … so it is the same if the player returns." | "as "Sizing fights" says … to reuse on a return." |
| 117 | "described under "Out of character"" | "of "Out of character"" |

## Constitution Check

| Principle | Result |
|---|---|
| Core 1 Consistency | Pass. No new state; deviations still go to `module-progress.md` and `dm-notes.md` |
| Core 2 Fun | Pass. The player keeps the module's world they chose, is never refused out of character, and is offered another setting when the module ends |
| Core 3, Development 1 Cost | Pass. `kit/dm-rules.md` −4 bytes (32,507 → 32,503, 212 lines); `dmc-new-campaign` +443 bytes (about 111 tokens), paid when step 4a is read; no resume read changes, so no `dmc size` change |
| Development 2 Migration | Pass. None needed: no template or file shape changes |
| Development 3 One step | Pass. Install and update unchanged |
| Development 4 Grounded | Pass. Lines cited above; the "skills" assumption is marked unverified in the spec |
| Development 6 One home | Pass. See the table above |
| Development 7 | Pass. The test reads files only |
| Dungeon Master 3 | Pass. This change meets it |
| Dungeon Master 4 | Pass. A player's choice still changes the story inside the world; the turn-back is in the fiction |
| Documentation 4 | Planned: README, user guide, product overview, decisions, roadmap |

## Slip test

Waived by the maintainer on 2026-11-28 for this change. The byte cap is held, so the always-loaded rules do not grow.

## Conflict with issue #290

#290 also edits `kit/dm-rules.md` (line 63, § Campaign files, with no net growth). The lines differ; if #290 merges
first, this branch is rebased on it and the byte count checked again.
