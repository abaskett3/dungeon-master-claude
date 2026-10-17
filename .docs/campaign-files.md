# Campaign files

This document is for anyone who reads or edits a campaign's files by hand, changes the templates, or needs to know which file holds which fact. The rules for when DMC writes each file are in [CLAUDE.md](../CLAUDE.md) § Campaign files and § Saving as you go, and in the skills; this document describes the files.

## Layout

```
adventures/
  player-options.md          the player's preferences for every campaign
  .active-campaign           the folder name of the campaign being played (one line)
  .reviewed-sessions         session ids already reviewed by /review-transcript
  .transcript-review/        output of dmc digest show, one folder for all campaigns (rewritten on each review)
  <campaign>/
    player-options.md        this campaign's settings and overrides
    game-state.md            the present moment
    character-sheets/
      pc-<name>.md           a player character
      npc-<name>.md          a companion or recurring NPC
      minor-npcs.md          one line per named bit part
    story/
      summary.md             the story so far, compressed
      canon.md               every established fact that can still come up
      canon-archive.md       facts whose subject can no longer come up
      dm-notes.md            DMC's plans, secrets and the Opposition table (spoilers)
      dm-notes-archive.md    closed threads (spoilers)
      sessions/
        session-NNN.md       scene-by-scene record of each play session
    maps/
      <place>.json           a saved place: layout, no creatures
      <place>.md             longer notes about a place, beside its map
      live.json              the scene on the player's screen: a place plus who stands where
      live.html, live-data.js, live.png   written by dmc map
    module-progress.md       only for a published module (spoilers)
    import/                  only for a campaign imported from elsewhere: the export, kept as the record
```

All of `adventures/` is ignored by git. A new campaign is a copy of `templates/campaign/`; the archives, `import/` and the map outputs are created when first needed.

## What each file holds

| File | Holds | Written | Template | Read at resume |
|---|---|---|---|---|
| `adventures/player-options.md` | Dice (who rolls, how rolls are asked for and shown, secret rolls), how to talk with DMC, narration style, maps | When the player asks for a change | `templates/player-options.md` | Yes |
| `player-options.md` | Edition, starting level, leveling, HP on level-up, what is tracked, house rules, lethality, combat difficulty, puzzle hints, tone, overrides of the general file | When the player asks for a change | `templates/campaign/player-options.md` | Yes |
| `game-state.md` | Campaign, session number, last full save; the scene (location, date and time, conditions, who is present, what is happening, what the player was about to do); party status per character (HP, hit dice, slots, resources, conditions, concentration, exhaustion, death saves, inspiration, gold, consumables); the combat tracker; plan and standing orders; quests; clocks; pending | Whenever any of it changes, before the next beat is narrated; in a fight, the tracker block once per reply, in one edit | `templates/campaign/game-state.md` | Yes, first after the options |
| `character-sheets/pc-<name>.md` | Abilities, combat numbers and attacks, features, spellcasting, inventory, story, relationships, notes | On level-up, new items, relationships or goals | `templates/pc-sheet.md` | Yes, every PC |
| `character-sheets/npc-<name>.md` | How to play them (voice note), what they know, secrets, relationships, history with the PC, stat block with every attack's reach or range | When an NPC is introduced, recurs or changes | `templates/npc-sheet.md`; the `character-sheet-update` skill gives a companion the full `templates/pc-sheet.md` | Companions, and NPCs in the current scene |
| `character-sheets/minor-npcs.md` | One line each: name, role, place, a trait, attitude to the PC | When a named bit part appears | `templates/campaign/character-sheets/minor-npcs.md` | No |
| `story/summary.md` | Premise, earlier arcs, recent events, where things stand; under about 600 words | At every full save | `templates/campaign/story/summary.md` | Yes |
| `story/canon.md` | Rulings, people, places, factions, lore, items, prices and promises, timeline: one line per fact, with the session | The moment a fact is established; pruned at full saves | `templates/campaign/story/canon.md` | Whole under about 230 lines; past that, the Rulings plus a search for the scene's names, places and threads |
| `story/canon-archive.md` | Facts moved out of `canon.md` once their subject can no longer come up (rulings never move) | At every full save that finds canon past about 230 lines, until it is under about 200 | none | No; searched when a subject returns |
| `story/dm-notes.md` | The central conflict, antagonists, the Opposition table, secrets, open threads, foreshadowing, ideas, notes about what this player enjoys | When plans or threads change | `templates/campaign/story/dm-notes.md` | Yes |
| `story/dm-notes-archive.md` | Closed threads | At a full save | none | No; searched when a subject returns |
| `story/sessions/session-NNN.md` | Each scene of a session (title, location, in-world time, beats) and a closing section | At the end of every scene | `templates/session-log.md` | The last three scenes of the most recent one |
| `maps/*.json` | See [The map files](#the-map-files) | When a reusable map is drawn; `live.json` whenever anyone moves | none | The map of the current location |
| `module-progress.md` | Current position and the digest section to read next, chapter status, areas, encounters with the scaling used, module NPCs, treasure and clues, scripted events, deviations from the module | Whenever the party's position or the module's state changes | `templates/module-progress.md` | Yes, if present |

Spoilers: the player can read and edit any file, but `story/dm-notes.md`, `story/dm-notes-archive.md`, `module-progress.md`, NPC sheets (their secrets) and anything under `modules/` give away what is ahead.

## Which file wins

Each kind of fact has one home, so that two files cannot disagree about it:

| Fact | Its home |
|---|---|
| Current HP, slots, resources, conditions, position in a fight | `game-state.md` (the sheet holds the maximums and the full inventory; both are updated when an item is gained or lost) |
| How many enemies there are, where, and which are dead | The Opposition table in `story/dm-notes.md`, and nowhere else |
| Where everyone stands on the map | `maps/live.json`, matched by the tracker's Position column |
| An established fact, price, distance or ruling | `story/canon.md` (or its archive) |
| How the game is run | `player-options.md` files |

When sources still disagree, this order decides, from `CLAUDE.md`:

1. **Player options:** the campaign's file, then the general file, then the defaults in `CLAUDE.md`.
2. **Facts:** the campaign files, then the module as written (where play has not diverged from it), then the 5e rules in the campaign's edition, then new invention, which must not contradict the others.
3. **Files against memory:** the files win over DMC's recollection, over the player's recollection, and over a compacted summary of the conversation.
4. **Files against each other:** when `resume-campaign` or the full save's consistency check finds two files that disagree, DMC tells the player what disagrees, proposes the most likely truth, and fixes the files only once the player confirms. It never picks one silently.

## The map files

A map is a small JSON file that `dmc map` checks, draws and measures. The full format is in [`.claude/skills/draw-map/map-format.md`](../.claude/skills/draw-map/map-format.md); this is a summary.

- **A place file**, `maps/<place>.json`, holds a layout and no creatures: `title`, `cell_ft`, `grid` and optional `legend`, `labels`, `notes`, `edges`, `north` and `scale_note`.
- **The scene file**, `maps/live.json`, puts creatures on a place: `"base"` names the place file (without `.json`, in the same folder) and `"tokens"` lists the creatures. Any key the scene sets replaces the place's, except that a scene cannot change its place's scale.
- **The turn order** of a fight is two optional keys of the scene: `"initiative"`, the creatures in the order they act, and `"current"`, whose turn it is. Write each as a token's `id` (preferred; ids are unique) or the name of exactly one token; a name shared by several tokens is refused, with their ids. `current` must be in `initiative`. `dmc map` draws them as a numbered list beside the grid with the current turn marked. A scene does not inherit them from its base place, and both keys are deleted when the fight ends. DMC keeps them in step with the combat tracker in `game-state.md`.
- **The grid** is a list of rows, one character per cell, every row the same length; a space is empty. Columns are lettered from the left and rows numbered from the top, so `E3` is the fifth cell of the third row. Built-in symbols: `#` wall, `.` floor, `+` door, `=` window, `~` water, `^` hazard, `>` and `<` stairs, `T` tree, `o` pillar, `_` low cover, `,` difficult terrain, `?` not yet seen. The legend renames a symbol or defines a new one.
- **The scale**, `cell_ft`, must be 5 (the default), 30, or 5280, 31680 or 316800 for overland maps (1, 6 or 60 miles a square). Anything else, including `null`, is refused.
- **A token** has an `id` of one or two characters (`@` for the PC, letters for allies, numbers for enemies) and `at`, its top-left cell; optionally `name`, `kind` (`pc`, `ally`, `enemy` or `neutral`), `size` in cells, a short `note` the player can see, and `down` for a creature that is dead or unconscious. At 5 ft two tokens cannot share a cell; on larger squares several can.

`dmc map` writes `live.html`, `live-data.js` and, with `--png`, `live.png` next to the file. The player can see everything in a map file, so only what the character has seen or knows goes in.

## Module folders

An ingested module lives outside the campaign, in `modules/<module>/`, so several campaigns can share it. Everything in it is a spoiler, and git ignores it.

```
modules/<module>/
  source/                  the player's original files (copied, not moved)
    extracted/             written by dmc pdf all: all-text.txt, text/page-NNN.txt, pages/page-NNN.jpg
  index.md                 title, edition, level range, page offset, DM synopsis, chapter list, flow, ingest status
  chapters/NN-title.md     a DM digest of each chapter, with keyed areas and source page references
  monsters.md              stat blocks unique to the module, and where standard creatures appear
  npcs.md                  major NPCs across the module
  items.md                 module-specific items and notable treasure
```

A campaign is a module campaign when its folder holds `module-progress.md`, which names the module and records the party's place in it. Page references in the digests are PDF page numbers, as `dmc pdf` and the Read tool use them; `index.md` records the offset to the printed numbers.

## The rules folder

`rules/` holds the player's optional rules reference, normally the SRD, and is ignored by git. `/setup-rules` writes `rules/index.md` (which document and edition, a section table with page ranges, a finer breakdown of the long alphabetical sections, and the pages most needed in play). For a PDF it first runs `dmc pdf text`, which writes `rules/extracted/all-text.txt` and one text file per page in `rules/extracted/text/`.
