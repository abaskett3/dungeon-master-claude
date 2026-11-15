# Research: Large Modules Cost One Area, Not One Chapter

Phase 0 of [plan.md](plan.md). Each item settles one question the spec leaves to the plan, with what was read in the
code, the prompts and the maintainer's play folder. The spec's own clarifications are not repeated here.

## R0. What the maintainer's modules measure

Measured on 2026-11-14 in `D:\Claude\dmc\modules\` with a throwaway script, read-only; nothing was copied out. Sizes
are bytes ÷ 4, a section is a heading's own text (to the next heading of any level), and `source/` is skipped.

| Module | Files | Sections | Section size p50 / p90 / p99 / max | Files at the mark | Sections with two stat blocks | Repeated headings |
|---|---|---|---|---|---|---|
| `the-crooked-moon` (638 pages) | 38 | 965 | 271 / 784 / 1,524 / 2,428 | 3: `monsters-crooked-moon.md` 37.3k, `monsters-legendary.md` 24.1k, `items.md` 21.8k | 0 | 26, all `### PLAYER-SAFE` and `### SPOILER: touchpoints` in `fateweaving.md`, once under each `##` entry |
| `peril-in-pinebrook` (short) | 6 | 32 | 114 / 313 / 637 / 637 | 0 | 0 | 0 |

- Areas, NPCs and stat blocks already have headings of their own in both modules: the skill's habit held. The loss
  is in the resume's pointer, which names a chapter section, and in three reference files at the mark, not two
  (`items.md` was not in the issue's figures).
- Stat blocks are written `- AC 15; Initiative …` and `- HP 45 (…)`: 57 `AC n` and 56 `HP n` in the 56 stat-block
  sections of `monsters-crooked-moon.md`.
- The one campaign on a module that is still open, `the-silver-egg`, has `Digest to read before continuing: none.
  Consult … only when …`, which today's `dmc size` already reads as no digest. Nothing in it breaks.
  *Corrected in Phase 8 (T039):* it holds only because that module is finished, so no digest is read at all. The
  pointer's prose names `items.md`, which the digest pattern matched, so on an unfinished campaign it was measured as
  a pointer to `items.md`. `dmc size` now reads a pointer that starts with the word `none` as no digest.

## R1. Where the check lives

- **Decision:** A new command, `dmc module check NAME [--folder PATH]` (contracts/dmc-module-check.md), in a new
  `Dmc.Modules` namespace (`src/Dmc.Cli/Modules/ModuleCheck.cs`), with `ModuleCommand` added to `CommandTable`.
  `dmc size --check` calls the same `ModuleCheck` for a module campaign and prints one line from its count (R6).
- **Rationale:** The check is about a module, not a campaign, and covers it once whatever campaigns use it (spec
  Assumptions). `dmc campaign` takes campaign names, `dmc lookup` prints sections, and `dmc size` measures a resume:
  putting a module check in any of them mixes two contracts. A `module` command leaves room for later module verbs
  without a rename. The ingest skill runs it directly; play reaches it only through `dmc size --check`, which resume
  and the full save already run, so play makes no new call.
- **Alternatives considered:** `dmc lookup NAME --check` (lookup's output is a section or a refusal; a list of
  problems is a different shape); `dmc size --module NAME` (size takes a campaign and its report is a read set);
  `dmc pdf check` (the check reads the digest, not the PDF).

## R2. The section limit

- **Decision:** `Budgets.ModuleSection = 3000` estimated tokens, for a heading's own text. A section over it is
  reported.
- **Rationale:** The measured large module's largest section is 2,428 and its p99 is 1,524, so 3,000 reports
  nothing in a module written as the skill asks, while a chapter's areas under one heading (a hub chapter is 13.8k)
  or a reference file's stat blocks under one heading (650 each) are far over. It keeps an area read at resume near
  the "about 2k" of SC-001. It is a new budget, so it lives in `Budgets.cs` with the others (constitution III); the
  near-cap mark is reused for files (spec FR-006).
- **Alternatives considered:** 2,000 (two real sections over it, both single areas); 4,000 or more (lets two or three
  areas share a heading unseen).

## R3. A section's extent

- **Decision:** For the check, a section is a heading's own text: from the heading to the next heading of any level.
  For the resume and lookup, a section stays what it is today: to the next heading of the same or a higher level,
  its subheadings included (`MarkdownHeadings.SectionEnd`).
- **Rationale:** The check asks whether one heading holds too much, so a parent with small children must pass; the
  resume wants the area with its parts. Both use `MarkdownFile`, so fences and comments mean the same to the check as
  to lookup (Phase A research R3).

## R4. Recognising a stat block

- **Decision:** A stat block is an armor-class line followed within eight lines by a hit-points line. An armor-class
  line is one whose visible text, after list marks, `>` and bold or italic marks, starts with `AC` or `Armor Class`
  and then a number; a hit-points line likewise with `HP` or `Hit Points`. A section's own text with two or more is
  reported, with the count. Fenced lines are skipped.
- **Rationale:** Both editions print both lines on every stat block (2014: "Armor Class 15", 2024: "AC 15"), the
  measured module writes them as list items, and requiring the pair keeps a prose line ("its AC drops to 12") from
  counting. Other systems rely on the section limit alone (spec Assumptions).
- **Alternatives considered:** Counting `STR`/`DEX` ability rows (often a table the extraction breaks); counting
  `Actions` headings (the measured module has none).

## R5. A repeated heading

- **Decision:** A heading is repeated when the same text (compared as `MarkdownHeadings.Key` compares) appears twice
  at the same level under the same parent heading in one file. Repeats across files, and repeats under different
  parents, are not reported. FR-008 and the Edge Cases are worded to match.
- **Rationale:** `fateweaving.md` repeats `### PLAYER-SAFE` under each of its entries, a deliberate shape; a lookup
  for the entry finds it with its parts. Two `## Goblin` headings in one file, though, make a lookup ambiguous with no
  way to narrow it.
- **Alternatives considered:** Any repeat in a file (26 false reports on the real module); repeats at level 2 only
  (misses a repeat inside a chapter's `##` area).

## R6. The line in `dmc size --check`

- **Decision:** For a module campaign that is not finished and whose module folder exists, `--check` prints, after
  the flags and before the link lines, at most one line:
  `module: modules/<name> has <n> problem(s): run dmc module check <name> (not the full save's to trim)`. A sound
  module, a finished one, a homebrew campaign and a missing module folder (already shown as not ingested) print
  none. The full report (`dmc size` without `--check`) prints the same line under its flags.
- **Rationale:** The user chose a line at resume (spec FR-013). One line with a count keeps the resume's context
  small; the details come from the command it names. The marker makes the full save leave it (its step 7 rule for
  such lines), and its step 8 report names each line still printed, so the player hears of it once per save, out of
  character, not mid-scene.
- **Cost:** The check reads every markdown file of the module once. For the 1.44 MB module that is the same work as
  one `dmc recall` over it, which Phase A measured well under one second; SC-006 holds it to that.

## R7. The pointer that names an area

- **Decision:** `module-progress.md`'s line keeps its label and gains a form:
  `- **Digest to read before continuing**: chapters/NN-title.md § <heading>; § <heading>` — one file, then one or
  more headings, each after `§`, separated by `;`. The old forms (`chapters/NN-title.md, section <name>`, a file
  alone, `none`) keep their meaning. `dmc size` reads each named heading with lookup's matcher on that one file (R8)
  and counts each section once.
- **Rationale:** An area and the scripted event it depends on can be two headings (spec Edge Cases). `§` is already
  what the kit and the docs write for "section", and it cannot start a heading's text by accident, so a pointer
  written before this change has no `§` and is read the old way.
- **Alternatives considered:** One heading only (loses the event); a list under the line (a template shape change,
  which would need a migration).

## R8. One matcher for the resume and for `dmc size`

- **Decision:** The resume reads each pointer heading with `dmc lookup <module>/<file> "<heading>"`. `dmc size`
  resolves the same headings with the same code: `ReadSet.DigestSection` calls a new internal
  `Lookup.FindSection(file, heading)` that returns the heading's index or the nearest headings, which `Lookup.Run`
  also uses. A heading that matches nothing is measured as the whole chapter, as today, and adds a flag line,
  `module-progress.md: no heading "<heading>" in <file> (nearest: …): name one`, which holds the save (the pointer is
  a file the full save keeps).
- **Rationale:** FR-004 needs the report to be what DMC reads, and today's `DigestSection` matches by "contains,
  first hit", which can pick a different section than lookup does. One matcher is one home (constitution III). The
  lookup output adds one header line per section, a few tokens, which the report does not count.
- **Alternatives considered:** Read tool with line ranges (DMC would have to search for the heading first, a second
  call per heading); keeping `DigestSection`'s own match (two rules that can disagree).

## R9. What the ingest skill asks for

- **Decision:** § 3 and § 4 of `dmc-ingest-module` say each keyed area, NPC, stat block, module item, spell, rule or
  player option and handout gets its own heading `NAME (PDF n)`; a chapter's NPCs become one heading each under the
  chapter's NPC heading. § 4 says to split a reference file before it reaches the mark, by letter or by kind
  (`monsters-a-m.md`), since lookup searches every top-level file. § 5 runs `dmc module check <module>` over what is
  written and fixes each line before § 6 reports the chapters ready; a fix is checked again. The skill states the
  shape, not the numbers: the numbers live in `Budgets.cs` and the check prints them (spec FR-011, as
  `dmc size` does for the campaign budgets).
- **Rationale:** The shape is what DMC writes; the check confirms it. Naming numbers in the prompt would give the
  rule two homes.

## R10. The resume step and the rules

- **Decision:** `dmc-resume-campaign` step 9: read `module-progress.md`, the index, and each heading the pointer names
  with `dmc lookup`; an old pointer that names a file and a section is read the same way; a lookup that fails lists
  the nearest headings, so read the nearest one and tell the player in one line, out of character, that the pointer
  needs fixing (FR-003). The finished case is unchanged. `templates/module-progress.md`'s comment shows the new form.
  `kit/dm-rules.md` § Running a published module, "Keep your place": when the party moves to another area, the
  pointer names its heading. The sentence is rewritten, not added to, so the file grows by no line (FR-015 allows 2).
- **Phase A decision:** The `.docs/decisions.md` entry "Lookup and recall are for play only until Phase D" gets an
  **Amended** line: Phase C changes the module part of the resume (spec FR-016, clarified with the user).

## R11. Migration and existing folders

- **Decision:** No migration. The template keeps its line and label; only its comment changes, and every old pointer
  stays valid (R7). A module is not a campaign and has no standard to migrate. `dmc install` and `dmc refresh` do not
  touch `modules/` today, and this change keeps that. The PR says so.

## R12. Standing play cost

- **Decision:** Measured in the PR. Expected: the resume skill's step 9 grows by about two sentences (read at every
  resume), the `dm-rules.md` line is rewritten at about the same length, and `--check` prints nothing for a sound
  module. The ingest skill loads only when ingesting.
