# Contract: the kit changes

What changes in `kit/`, as obligations; the wording is the implementer's, held to `.docs/development.md` § Writing
prompts. One PR.

| File | Change |
|---|---|
| `kit/skills/dmc-ingest-module/SKILL.md` | § 3: each keyed area is a heading `NN. Name (PDF n)` of its own; a chapter's NPCs each get a heading under the chapter's NPC heading; scripted events that an area depends on are named in that area. § 4: every stat block, module item, spell, rule, player option and handout under its own heading `NAME (PDF n)`; keep a reference file under the mark `dmc module check` prints, splitting it by letter or kind (`monsters-a-m.md`) before it reaches it. § 5: run `dmc module check <module>` over what is written, fix each line, run it again; § 6 reports chapters ready only after it prints nothing. No numbers in the skill (research R9). The "If `dmc` is unavailable" fallback for PDFs is left for issue #208, not extended. |
| `kit/skills/dmc-resume-campaign/SKILL.md` | Step 9: read `module-progress.md`, the index, and each heading the pointer names, with `dmc lookup <module>/<chapter file> "<heading>"`; on a failed lookup, read the nearest heading it lists and tell the player in one line, out of character, that the pointer needs fixing; the finished case unchanged. Where `dmc size --check` already runs: a `module:` line is for the full save; do not mention it (it ends with the marker, like the others). |
| `kit/templates/module-progress.md` | The Digest line's comment shows the new form: `chapters/NN-title.md § <area heading>; § <event heading>`. Label unchanged. |
| `kit/dm-rules.md` | § Running a published module, "Keep your place": rewritten so it says the pointer names the heading of the area the party is in, updated when they move. No line added (FR-015). |
| `kit/skills/dmc-campaign-full-save/SKILL.md` | No change: step 7 already leaves a line with the marker, and step 8 already names every line still printed. |

## Tests that hold this

- `ToolkitTests`: every command, path and heading the changed prompts name is real (existing tests); `dm-rules.md`
  grows by at most two lines against `origin/main` (the existing growth test, or one added the same way).
- A new prompt test: the ingest skill names `dmc module check`, the resume skill's step 9 names `dmc lookup` and the
  template's comment shows `§`.
