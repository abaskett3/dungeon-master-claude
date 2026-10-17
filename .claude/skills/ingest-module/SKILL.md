---
name: ingest-module
description: Read a published D&D adventure module (PDF, text, Markdown, HTML or EPUB) and turn it into an indexed DM digest under modules/, so DMC can run it accurately and track progress through it. Use when the player wants to play a published module, supplies a module file, or when a chapter needed for play has not been ingested yet.
---

# Ingest module

Turn the player's copy of a published adventure into files DMC can run from without rereading the whole book: an index, one digest per chapter, and reference lists, all pointing back to exact source pages.

Everything read and written here is a spoiler. Do not summarize the adventure's content to the player while ingesting; report progress only (chapter names and counts).

Work only from the source. If you believe you remember this adventure from elsewhere, ignore that: do not use it to fill gaps. Where the source is unreadable or silent, write "not in source" or "unreadable, p. N" instead of guessing.

## 1. Set up

Ask for the file path if it was not given. Choose a folder name from the module title (lowercase, hyphenated). Create `modules/<module>/` with `source/` and `chapters/`, and copy the supplied files into `source/` (copy, do not move, unless the player says otherwise).

If `modules/<module>/index.md` already exists, read its ingest status and continue from where it stopped.

Reading the source:

- **PDF**: extract it with `dmc pdf`, then read the output. This works whatever the file's size (the Read tool refuses PDFs over 100 MB) and gives you both the words and the pictures.
  1. `./bin/dmc pdf info <pdf>` lists every page with its amount of text and number of images, and names the pages that have little or no text (scans, maps, full-page art).
  2. `./bin/dmc pdf all <pdf>` writes `source/extracted/all-text.txt` (every page, split by `===== PDF PAGE N =====` lines), `source/extracted/text/page-NNN.txt` and `source/extracted/pages/page-NNN.jpg` (a picture of each page). For a long book, do a chapter at a time with `--pages 1-40`. Use `--dpi 160` for a page whose small print or map labels are hard to read at the default.
  3. Read the text files for the words. **Look at the page images too**, with the Read tool, one page per call: see "Pictures" below.
  - If `dmc` is unavailable, fall back to the Read tool's `pages` parameter (at most 20 pages per call; it shows pages as images as well as text) and tell the player that a PDF over 100 MB needs `dmc`.
- **Markdown, text, HTML**: read directly, in sections. Open any image files they link to.
- **EPUB**: it is a zip archive; extract it into `source/` and read the XHTML files in spine order.
- **Anything else** (a website, an app): ask the player to export or paste it into a file.

### Pictures

The text layer of a book leaves out much of what a DM needs. Look at the image of:

- **every page `dmc pdf info` flags as having little text**, and every page whose text mentions a map, diagram, handout, puzzle or illustration;
- **every page of a chapter as you digest it**, when the book is short enough (up to about 60 pages) or the chapter is map-heavy. For a long book, look at least at each chapter's maps, handouts, stat blocks and any page where the extracted text reads as jumbled (tables and multi-column layouts often do).

From the pictures, record in the digest what the words do not say:

- **Maps**: the layout, which areas connect and by what (doors, stairs, secret doors), the scale and compass rose, area numbers, and anything drawn but not described. This is what the ASCII sketch in the chapter digest is made from.
- **Illustrations**: what creatures, NPCs, places and items look like (colors, build, clothing, distinguishing marks), so DMC describes them the way the book shows them. Put these in `npcs.md`, `monsters.md` or the keyed area they belong to, marked "(art, p. N)".
- **Handouts and puzzles**: what the player is meant to see, including handwriting, symbols and anything hidden in the picture.
- **Layout cues**: which boxes are read-aloud text, which are DM tips and which are stat blocks, if the text extraction has run them together.

Check tables and stat blocks against the page image whenever the extracted text looks out of order. If an image and the text disagree, the text of the adventure wins; note the difference.

In the index, list which pages' images were viewed and which were not, so a later session knows what is still unseen.

## 2. Survey and index

Read the cover, table of contents, introduction and any "running the adventure" section. Write `index.md`:

- Title, publisher, rules edition (2014 or 2024), level range, intended party size, setting.
- **Page offset**: the difference between the PDF page number and the printed page number. State which one all references in the digest use (use PDF page numbers, since that is what `dmc pdf` and the Read tool take).
- A DM-only synopsis: the background, the villain's plan, how the adventure is expected to unfold and end.
- Chapter list with page ranges and a one-line purpose for each.
- Adventure flow: what leads to what, which parts are linear, which are open, level expectations per chapter.
- Where the appendices are: monsters, magic items, NPCs, maps, handouts.
- **Ingest status**: a checklist of every chapter and appendix, ticked as each digest is written.

## 3. Digest each chapter

One chapter at a time. Read it, write `chapters/NN-short-title.md`, tick it off in the index, then move on. Never hold several chapters in mind unwritten.

Write in your own words, as terse DM notes, not a copy of the text. Each digest contains:

- **Overview**: what this chapter is for, expected level, how the party arrives and what moves them on.
- **Timeline or triggers**: scripted events and what sets them off.
- **Keyed areas**, one entry each, keeping the module's own numbering:
  - area number and name, source page
  - what is immediately apparent, and what is hidden (with the DCs to find it)
  - creatures present (count and type, where their stat block is) and how they behave
  - traps and hazards (trigger, effect, DCs, damage)
  - treasure (exact contents)
  - exits and where they lead
  - whether there is read-aloud text, and its page (leave the wording in the source; read it from there at the table)
- **NPCs in this chapter**: who, where, what they want, what they know, what they will do.
- **Clues and information**: what the party can learn here and where each clue points.
- **Maps**: the page of each map, plus an ASCII sketch of the layout with the area numbers for DM use. A player-facing version is drawn later with the `draw-map` skill, showing only what has been seen.
- **Milestones**: level-ups or story beats that end the chapter.

Copy numbers exactly: DCs, damage, quantities, HP, prices, distances. A wrong number here becomes a wrong fact in play.

## 4. Reference files

- `monsters.md`: every stat block that is unique to this module, in full and exact. Then a list of standard creatures the module uses, with where each appears.
- `npcs.md`: major NPCs across the whole module: role, appearance, personality, goals, secrets, where they appear, stat block reference.
- `items.md`: module-specific magic items and notable treasure, with their rules and where they are found.
- Handouts and player-facing text: list them in the index with page numbers.

## 5. Verify

- Every chapter and appendix in the index has a digest and is ticked.
- Every keyed area has a page reference.
- Spot-check a sample of numbers in each chapter (a few DCs, a stat block, a treasure list) against the source and fix any mismatch.
- Every map has been looked at and sketched, and the index says which page images were viewed.
- Note in the index anything unreadable or missing.

## 6. Size and context

A full hardcover is hundreds of pages; ingesting it uses a lot of context.

- Do it in its own session, not in the middle of play.
- It does not have to be done at once. The survey plus the first chapter or two is enough to begin; ingest later chapters before the party reaches them, using the status checklist.
- If the player has asked for subagents and they are available, one per chapter works well: run `dmc pdf all` once first, then give each the page range, the paths of the extracted text and page images, the page offset and the digest format above, and have it write its chapter file directly.

When finished or pausing, tell the player which chapters are ready, without describing what is in them.
