---
name: setup-rules
description: Index a local copy of the D&D 5e SRD (or other rules reference) in the rules/ folder so DMC can look rules up by page or search instead of relying on memory. Use after the player adds a rules file to rules/, or when rules/ has files but no index.
---

# Set up the rules reference

`rules/` holds the player's local rules reference, normally the free System Reference Document. With an index, DMC can open the exact pages for a spell, condition, monster or rule in one step.

## 1. See what is there

List `rules/`. If it holds nothing but `.gitkeep`, tell the player how to add the SRD (the README has the link) and stop.

Note the format of each file:

- **Markdown or text**: searchable directly with Grep. An index is still useful for knowing which file covers what.
- **PDF**: run `./bin/dmc pdf text <pdf>` first. It writes `rules/extracted/all-text.txt` and one text file per page in `rules/extracted/text/`, so the rules can be searched with Grep and a hit's file name gives its PDF page. (Without `dmc`, skip this step.) To see a page as printed, for a table or stat block whose extracted text is jumbled, read it with the Read tool's `pages` parameter, at most 20 pages per call, or render it with `./bin/dmc pdf render <pdf> --pages N`. A PDF still needs a page index to be usable.

## 2. Build `rules/index.md`

For a PDF, read the table of contents, then skim the document in 20-page steps far enough to confirm where each section really starts. Record PDF page numbers (what the Read tool takes), and note the offset from printed page numbers if they differ.

Write:

- Which document this is, its version and rules edition (the 5.1 SRD is the 2014 rules; the 5.2 SRD is the 2024 rules).
- A section table: races or species, each class, backgrounds, equipment, feats, ability checks, adventuring, combat, spellcasting rules, spell lists, spell descriptions, conditions, magic items, monsters, NPCs. One line each with its page range or file.
- For long alphabetical sections (spell descriptions, magic items, monsters), a finer breakdown: the page each letter starts on, so any entry can be reached in one read.
- A short list of the pages most often needed in play: conditions, actions in combat, cover, resting, death saves, grappling, opportunity attacks, concentration.

Do not copy rules text into the index. It is a map only.

## 3. Check it

Pick five entries from different sections (a spell, a monster, a condition, a class feature, a magic item), use the index to open each, and confirm it is where the index says. Fix any that are off.

## 4. Report

Tell the player the rules reference is ready and which edition it covers. If that edition differs from a campaign's `player-options.md`, point it out.

## Using it in play

When a rule, spell, item or stat block matters and you are not certain of it, open the index, read the cited pages (or search `rules/extracted/text/` if the PDF was extracted), and apply what they say. Things the SRD does not include (many subclasses, some spells and monsters) are simply absent: say so and fall back to a web lookup or a recorded ruling.
