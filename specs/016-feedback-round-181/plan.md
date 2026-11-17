# Implementation Plan: The feedback round of #181

**Branch**: `016-feedback-round-181` | **Date**: 2026-11-16 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/016-feedback-round-181/spec.md`

## Summary

Fix the 20 open feedback entries and the stale arc so DMC uses what the files already say (A), corrects a slip without
growing it (B), leaves the player's side to the player, draws fights that show the fight, and revises an overtaken arc.
Most of it is clauses on existing lines of `kit/dm-rules.md` and the skills. Three changes are code, because wording
alone already failed: `dmc context-check` puts the sheet lines of the people in the scene next to the reply (S1) and
prints the out-of-character line (B-hook); `dmc size --arc` flags an Arc plan play has overtaken, for the full save
alone to fix, never shown to the player (research R2); `dmc map` gains a moving aura and a view (S2). One PR,
`Closes #181` (research R11).

**Two changes to agreed decisions, approved by the maintainer on 2026-11-16** (spec § Clarifications):

1. **The arc fix is code, not the full-save skill only**, on the condition that the player never acts on it or sees it:
   `--arc` is separate from `--check`, and the full save fixes its lines silently.
2. **S1 refreshes every 100,000 tokens, not once per session part.**

## Technical Context

**Language/Version**: C# on .NET 10, Native AOT (`src/Dmc.Cli`); Markdown prompts (`kit/`)

**Primary Dependencies**: none new

**Storage**: campaign Markdown and JSON files in a DMC folder; no new file (data-model.md)

**Testing**: MSTest (`tests/Dmc.Tests`): toolkit tests for the prompts and byte cap, contract tests for
`context-check`, `size` and `map`; headless play runs (quickstart.md)

**Target Platform**: Windows and Linux, x64 and arm64

**Project Type**: CLI and prompt kit

**Performance Goals**: `dmc context-check` stays within the hook's budget (it runs before every message); the sheet
part reads only the sheets on the Present line

**Constraints**: `kit/dm-rules.md` stays at 212 lines; its byte cap (`tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1209`,
30,914) rises by the measured amount only; one-line errors; the maintainer approves each prompt clause's wording before
it is committed (#181 § Process)

**Scale/Scope**: 20 feedback entries; 32 FRs; one campaign's evidence (rorns-landing, s37 to s62)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|
| Core 1. Consistency | the files that hold new state are named | Pass: data-model.md; no new file |
| Core 2. Fun | what the player gains | Pass: fewer corrections (SC-004), no decisions taken from them, fewer break offers, maps that show the fight |
| Core 3. Low context | the cost is counted | Pass: research R10; about 340 tokens per turn, the hook about 2.5% when due |
| Dev 1. Count the cost | lines per file, `dmc size` where a resume read changes | Pass: R10; no `kit/dm-rules.md` line added; the resume reads one more template comment line (FR-032) |
| Dev 2. No change breaks a campaign | a migration or why none | Pass: none needed; R6 (companions), R7 (session log), R9 (maps), the arc flags change no file |
| Dev 3. One step to install and upgrade | no new step or dependency | Pass |
| Dev 4. Grounded in the code | every claim cites a path or says unverified | Pass: research cites each; unverified items listed in research and quickstart |
| Dev 5. A model and effort per skill | changes data-backed | Pass: no skill's model or effort changes |
| Dev 6. One home per rule | each rule's home named | Pass: the table below; the hook reuses the resume's Present matching (R4) |
| Dev 7. Tests skip locally, fail on CI | tests that need a tool | Pass: the new tests need no external tool (the map tests are pure C#, `tests/Dmc.Tests/Map/`); any that does calls `TestEnv.Need` |
| DM 1. An arc | the arc structure kept | Pass: the flags check #228's structure, they add none |
| DM 2. Fair fights | — | Not touched |
| DM 3. The module is the source | — | Pass: the arc flags apply to homebrew campaigns and finished modules only |
| DM 4. The player shapes the story | a choice still changes the story | Pass: a revision keeps what play did; C1 to C3, G, H and FR-018 leave choices to the player |
| Doc 1 to 4 | documents named per PR | Pass: the table below |

**Re-check after Phase 1:** unchanged. The two decision changes above are not constitution breaks; the maintainer
approved both on 2026-11-16.

## Where each change goes (Development 6)

Line numbers are `origin/main` at `0cfdfa1`.

| Req | Home |
|---|---|
| FR-001 (A1) | `kit/dm-rules.md:60` |
| FR-002 (A2) | `kit/dm-rules.md:78` |
| FR-003 (A3) | `kit/templates/session-log.md:13-22`; pointer on `kit/dm-rules.md:81` |
| FR-004 (A4) | `kit/dm-rules.md:74` |
| FR-005 (A5), FR-032's move | `kit/skills/dmc-campaign-full-save/SKILL.md` § 2. Character sheets (lines 16-22) |
| FR-006 (A6) | `kit/skills/dmc-character-sheet-update/SKILL.md:33` |
| FR-007 (A7) | `kit/templates/session-log.md:18` |
| FR-008 (S1), FR-011 (B-hook) | `src/Dmc.Cli/Retrieval/` (a new sheet part beside `CanonReminder.cs`), `src/Dmc.Cli/Commands/ContextCheckCommand.cs`; contract [context-check-sheets.md](contracts/context-check-sheets.md) |
| FR-009 (B1) | `kit/dm-rules.md:85` |
| FR-010 (B2) | `kit/dm-rules.md:87` |
| FR-012 (B3) | `kit/skills/dmc-record-feedback/SKILL.md` § 3 |
| FR-013 (C1) | `kit/dm-rules.md:150` |
| FR-014 (C2) | `kit/dm-rules.md:154` |
| FR-015 (C3) | `kit/dm-rules.md:142` |
| FR-016 (G) | `kit/dm-rules.md:154` |
| FR-017 (H) | `kit/dm-rules.md:148` |
| FR-018 | `kit/dm-rules.md:180` |
| FR-019 (D1) | `kit/dm-rules.md:207` |
| FR-020 (D2) | `src/Dmc.Cli/ContextCheck/BreakReminder.cs:18` |
| FR-021 (E1) | `kit/dm-rules.md:192` (§ Narration's first line, on style; #181 named 193, the NPC voices line, before the lines moved) |
| FR-032 | `kit/templates/campaign/game-state.md:20` |
| FR-022 to FR-024 (F1 to F4) | `kit/skills/dmc-draw-map/SKILL.md` (lines 62, 77, 104); F2 by `"view"` rather than a trimmed copy, which would rename the cells, and F4 by `"aura"` with no stopgap, since S2 ships in the same PR |
| FR-025 (S2) | `src/Dmc.Cli/Maps/` (`PyJson.cs`, `MapChecker.cs`, `SvgDrawing.cs`, `Distances.cs`); `kit/skills/dmc-draw-map/map-format.md`; contract [map-aura-view.md](contracts/map-aura-view.md) |
| FR-026, FR-031 | `src/Dmc.Cli/Size/ArcFlags.cs` (a class of its own beside `Flags.cs`, whose flags are the read set's) and the `size` command's options (`--arc`, `SizeArguments.cs`); one sentence in `kit/skills/dmc-campaign-full-save/SKILL.md` step 5; contract [size-arc-flags.md](contracts/size-arc-flags.md) |
| FR-027 | `tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1209` |
| FR-030 | DMC folder `feedback/*.md` `status:` lines, after the merge |

## Documents (Documentation 4)

- `.docs/dmc-reference.md`: § context-check (the sheet lines, the out-of-character line, the band at 813), § map (the
  aura and view), § size (`--arc` and its flags).
- `.docs/user-guide.md:187` (another 50,000); `.docs/architecture.md:71` (what the hook prints);
  `.docs/campaign-files.md` (the session log's Present line; what a companion is).
- `.docs/decisions.md`: S1 by presence and its refresh; the B-hook in code; the band amended at 145; FR-032 and why no
  migration; the aura and view, no migration; the arc fix in code, against #181's "skill only", and #228's result.
- `kit/skills/dmc-draw-map/map-format.md`, the map format's home.
- `.docs/roadmap.md` (a row); #226 noted as replaced; the quickstart's results.

## Project Structure

### Documentation (this feature)

```text
specs/016-feedback-round-181/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── context-check-sheets.md
│   ├── map-aura-view.md
│   └── size-arc-flags.md
├── checklists/requirements.md
└── tasks.md             # /speckit-tasks
```

### Source Code (repository root)

```text
kit/
├── dm-rules.md
├── settings.json                      # unchanged: one UserPromptSubmit hook
├── skills/dmc-campaign-full-save/SKILL.md
├── skills/dmc-character-sheet-update/SKILL.md
├── skills/dmc-record-feedback/SKILL.md
├── skills/dmc-draw-map/{SKILL.md,map-format.md}
└── templates/{session-log.md,campaign/game-state.md}
src/Dmc.Cli/
├── Commands/ContextCheckCommand.cs
├── ContextCheck/BreakReminder.cs
├── Retrieval/CanonReminder.cs          # and a new sheet part beside it
├── Size/{Flags.cs,ReadSet.cs}
└── Maps/{PyJson.cs,MapChecker.cs,SvgDrawing.cs,Distances.cs}
tests/Dmc.Tests/
├── ContextCheck/
├── Size/
├── Map/
└── Toolkit/ToolkitTests.cs
```

**Structure Decision**: the existing CLI and kit; no new project or folder.

## Complexity Tracking

| Departure | Why needed | Simpler alternative rejected because |
|---|---|
| The arc fix in `dmc size --arc`, against #181's "full-save skill only" | #228's wording in the skill failed its test twice (research R2) | More wording is what failed; flags in `--check` would reach the player through the resume and the save's report |
| S1 refreshes every 100,000 tokens, against FR-008's "once per session part" | once per part fires just after the resume read the same sheets (research R4) | Once per part misses every slip #181 measured |
| The B-hook in `context-check`, against #181's `grep` hook | testable, and one hook (research R5) | A shell `grep` of JSON has no tests and breaks on escaped quotes |
