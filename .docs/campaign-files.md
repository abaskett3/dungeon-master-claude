# Campaign files

This document is for anyone who reads or edits a campaign's files by hand, changes the templates, or needs to know which file holds which fact. The rules for when DMC writes each file are in [kit/dm-rules.md](../kit/dm-rules.md) (a DMC folder's `CLAUDE.md`) § Campaign files and § Saving as you go, and in the skills (`kit/skills/`); this document describes the files.

## Layout

Everything below is in a **DMC folder**, the folder a player plays in (written by `dmc install`). Next to the player's folders it holds the files `dmc` writes and keeps up to date: `CLAUDE.md`, `.claude/settings.json`, `.claude/skills/dmc-*/`, `templates/` and `migrations/` (their source is `kit/` in the repository; `migrations/` holds the numbered files DMC follows to bring an older campaign's files up to date, see [Bringing a campaign up to date](#bringing-a-campaign-up-to-date)), and `.dmc/manifest.json`, which lists them with their hashes and makes the folder a DMC folder. `dmc install`, `dmc refresh` and `dmc play` write only those, and never anything under `adventures/`, `feedback/`, `modules/`, `rules/` or `exports/`; a generated file the player edited is kept under `.dmc/replaced/<yyyyMMdd-HHmmss>/` before it is replaced. The player's own files (their `.claude/settings.local.json`, their own skills) are never touched.

`feedback/` sits in the DMC folder, beside `adventures/`. It is not part of any campaign: feedback about how DMC itself runs the game is kept there (see [Feedback entries](#feedback-entries)), never in a campaign folder.

`exports/` sits in the DMC folder too. `dmc campaign export` writes its ZIP files there, one per export (`<campaign>-<YYYYMMDDHHmm>.zip`). Each file holds a whole campaign, `story/dm-notes.md` and other spoilers included, and is not part of any campaign; keep copies outside `adventures/`, where DMC would search them.

```
exports/                     one ZIP per dmc campaign export, in the DMC folder (whole campaigns, spoilers included)
adventures/
  player-options.md          the player's preferences for every campaign
  .active-campaign           the folder name of the campaign being played (one line)
  .reviewed-sessions         session ids already reviewed by /review-transcript
  .transcript-review/        output of dmc digest show, one folder for all campaigns (rewritten on each review)
  .dmc-import-<id>/          staging folder of a running dmc campaign import (deleted when it ends; a leftover is safe to delete)
  .restore-<campaign>-<id>/  staging folder of a running dmc campaign restore (a later restore cleans up a leftover)
  .replaced-<campaign>-<id>/ the campaign renamed aside during a restore (a later restore puts it back if the restore was interrupted)
  <campaign>/
    .standard                the campaign's standard: one whole number (no file means 1); written by dmc campaign verify and standard --init
    backups/                 backups of the campaign taken before a migration (dmc campaign backup); never exported
      <campaign>_backup_<yyyyMMddHHmmss>.zip   a whole campaign export, checked when written
      <backup>.zip.migrating                   a migration from this backup is under way (only when one was pending)
      <backup>.zip.accounting.tsv              what the migration reworded or dropped, written by DMC
      <backup>.zip.verified.tsv                the migrated campaign's files and hashes, written by a passing verify
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
      dm-notes.md            DMC's plans, the Arc plan, secrets and the Opposition table (spoilers)
      dm-notes-archive.md    closed threads (spoilers)
      sessions/
        session-NNN.md       scene-by-scene record of each play session
    maps/
      <place>.json           a saved place: layout, no creatures
      <place>.md             longer notes about a place, beside its map
      live.json              the scene on the player's screen: a place plus who stands where
      live.html, live-data.js, live.png   written by dmc map
      live-source.txt        what the page was last drawn from (the map file and its base place), written by dmc map; dmc redraw reads it
    module-progress.md       only for a published module (spoilers)
    import/                  only for a campaign imported from elsewhere: the export, kept as the record
```

`adventures/` is the player's, and `dmc` never writes it (a DMC folder is not a git checkout). A new campaign is a copy of `templates/campaign/` (generated from `kit/templates/campaign/`); the archives, `import/` and the map outputs are created when first needed.

## What each file holds

| File | Holds | Written | Template | Read at resume |
|---|---|---|---|---|
| `adventures/player-options.md` | Dice (who rolls, how rolls are asked for and shown, secret rolls), how to talk with DMC, narration style, maps | When the player asks for a change | `templates/player-options.md` | Yes |
| `player-options.md` | Edition, starting level, leveling, HP on level-up, what is tracked, house rules, the predecessor campaign of a sequel, lethality, combat difficulty, puzzle hints, tone, overrides of the general file | When the player asks for a change | `templates/campaign/player-options.md` | Yes |
| `game-state.md` | Campaign, session number, last full save (date, session and the last scene saved); the scene (location, date and time, conditions, who is present, what is happening, what the player was about to do); party status per PC and companion, plus a "With the party, not companions" block for rescued NPCs, prisoners and guests (HP, hit dice, slots, resources, conditions, concentration, exhaustion, death saves, inspiration, gold, consumables); the combat tracker; plan and standing orders for where the party is now (orders not in play go to canon); quests; clocks; pending | Whenever any of it changes, before the next beat is narrated; in a fight, the tracker block once per reply, in one edit | `templates/campaign/game-state.md` | Yes, first after the options |
| `character-sheets/pc-<name>.md` | Abilities, combat numbers and attacks, features, spellcasting, inventory, story, relationships, notes | On level-up, new items, relationships or goals | `templates/pc-sheet.md` | Yes, every PC |
| `character-sheets/npc-<name>.md` | How to play them (voice note, the names they use for people and places), what they know, secrets, relationships, history with the PC, stat block with every attack's reach or range | When an NPC is introduced, recurs or changes | `templates/npc-sheet.md`; the `character-sheet-update` skill gives a companion the full `templates/pc-sheet.md` | Companions, and NPCs in the current scene |
| `character-sheets/minor-npcs.md` | One line each: name, role, place, a trait, attitude to the PC | When a named bit part appears | `templates/campaign/character-sheets/minor-npcs.md` | No |
| `story/summary.md` | Premise, earlier arcs, recent events, where things stand; within its budget (below) | At every full save | `templates/campaign/story/summary.md` | Yes |
| `story/canon.md` | Rulings, people, places, factions, lore, items, prices and promises, standing orders not in play, timeline (with one line per finished fight: what the characters did or said): one line per fact, with the session | The moment a fact is established; pruned at full saves | `templates/campaign/story/canon.md` | Whole under its limit (below); past that, the Rulings, Prices, debts and promises, and a search for the scene's names, places and threads |
| `story/canon-archive.md` | Facts moved out of `canon.md` once their subject can no longer come up (rulings never move) | At every full save where `dmc size` finds canon over its limit, until it is under the archive mark (below) | none | No; searched when a subject returns |
| `story/dm-notes.md` | The central conflict, the Arc plan (stages, finished ones cut to a line; a "Last played" line overwritten at each save; character arcs, side quests, party gaps, milestones and income), antagonists, the Opposition table (only enemies the party may still meet; a finished fight's rows move to the archive), secrets, open threads, foreshadowing, ideas, notes about what this player enjoys | When plans or threads change | `templates/campaign/story/dm-notes.md` | Yes |
| `story/dm-notes-archive.md` | Closed threads, and the Opposition rows and stat blocks of finished fights (a rewind restores them) | At a full save | none | No; searched when a subject returns |
| `character-sheets/<name>-history.md` | Dated history moved off a sheet by the full save once it is older than the current stretch of the story. Resume does not read it | When the full save trims a sheet | |
| `story/sessions/session-NNN.md` | Each scene of a session (title, location, in-world time, beats) and a closing section. Every resume starts a new session, so one log is one sitting; scenes number from 1 in each, cited `sN.M` | At the end of every scene | `templates/session-log.md` | The last three scenes of the most recent one, or all of it after an unsaved session |
| `maps/*.json` | See [The map files](#the-map-files) | When a reusable map is drawn; `live.json` whenever anyone moves | none | The map of the current location |
| `module-progress.md` | Current position and the digest section to read next, chapter status, areas, encounters with the scaling used, module NPCs, treasure and clues, scripted events, deviations from the module | Whenever the party's position or the module's state changes | `templates/module-progress.md` | Yes, if present |

### How big the files may be

Every file the resume reads that the full save trims has a size budget, in estimated tokens (bytes ÷ 4), and canon has a limit that decides how the resume reads it. `dmc size` measures them (see [dmc-reference.md](dmc-reference.md#size)); the resume runs `dmc size <campaign> --check` once before it reads, and the full save's consistency check runs it and is not complete until it prints nothing or its report says why each line stays. The numbers are set in one place, `src/Dmc.Cli/Size/Budgets.cs`, and a toolkit test keeps this table equal to it; the reasons are in [decisions.md](decisions.md), "Size rules are measured in tokens by `dmc size`".

| What | Budget (estimated tokens) |
|---|---|
| `game-state.md` | 4,000 |
| `story/dm-notes.md` | 10,000 |
| its About this player section | 2,000 |
| its Arc plan section | 1,500 |
| `story/summary.md` | 3,000 |
| a PC's sheet, down to its history | 6,000 |
| any other sheet, down to its history | 4,000 |
| `story/canon.md`: read whole below | 19,000 |
| `story/canon.md`: a full save archives down to | 16,000 |

The player options, the session log and the module files have no budget: the full save does not trim them. Any file the resume reads is flagged when it reaches 20,000 estimated tokens or 1,600 lines, near what the Read tool returns in one call. `dmc size` also flags what the templates say should not stay in a file read whole: more than one Last played line or a per-session Mix tracker, Opposition rows with none left alive, DONE or RESOLVED lines outside the Arc plan, a Prices, debts and promises section over 3,000, and lines over 400 characters.

Spoilers: the player can read and edit any file, but `story/dm-notes.md`, `story/dm-notes-archive.md`, `module-progress.md`, NPC sheets (their secrets) and anything under `modules/` give away what is ahead.

## Bringing a campaign up to date

When the toolkit changes what campaign files should hold, the change ships a **migration**: a numbered file in
`migrations/` of the DMC folder (`002-sheet-history-last.md`, and so on) that DMC follows to move an older campaign's
files to the new shape. A campaign's **standard** is the number in its `.standard` file (1 when there is none); the
kit's is the highest migration number. `dmc campaign standard NAME` says `current`, `behind` (with the pending
migrations), `ahead` (the files were written by a newer `dmc`) or `unfinished` (a migration was started and not
verified or restored). The resume says one line when a campaign is not current, and offers the migration.

| File | What reads and writes it |
|---|---|
| `.standard` | Written only by `dmc campaign verify` on a pass and by `dmc campaign standard NAME --init` (the new-campaign skill); restored with a backup. Read by `standard`, `verify`, `import`, `refresh` and `update`. DMC never edits it. |
| `backups/<campaign>_backup_<stamp>.zip` | Written by `dmc campaign backup`, a campaign export checked whole. Removed only by `backup --prune`, on the player's yes to the question `dmc campaign backups` prints when three or more are kept. Read by `verify` and `restore`. Never part of an export or of another backup; an import of an export made by an older `dmc` that holds one leaves it out. Ordered by the UTC time in its manifest. |
| `backups/<backup>.zip.migrating` | Written by `backup` when the campaign is behind the kit; every mark of the campaign is removed by a passing `verify` or by any `restore`. While it exists the migration is unfinished: `backup` refuses, and `verify` and `restore` use that backup. |
| `backups/<backup>.zip.accounting.tsv` | Written by DMC during a migration, one tab-separated line per unit the migration did not keep verbatim: `<id>`, `reworded` or `dropped:<class>`, and an anchor or a reason; `skipped`, a file and a reason for a file left unmigrated. `verify` reads it. |
| `backups/<backup>.zip.verified.tsv` | Written by a passing `verify`: each campaign file's path and SHA-256. `restore` reads it to warn about play since the migration. |

A backup's companion files go with it when it is pruned. Verification compares the campaign's files without `backups/`
and `.standard`. The procedure is the `dmc-migrate-campaign` skill; the commands are in
[dmc-reference.md](dmc-reference.md#migrations), and the user guide says what the player sees.
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
4. **Files against each other:** when `dmc-resume-campaign` or the full save's consistency check finds two files that disagree, DMC tells the player what disagrees, proposes the most likely truth, and fixes the files only once the player confirms. It never picks one silently.

## The map files

A map is a small JSON file that `dmc map` checks, draws and measures. The full format is in [`kit/skills/dmc-draw-map/map-format.md`](../kit/skills/dmc-draw-map/map-format.md) (`.claude/skills/dmc-draw-map/map-format.md` in a DMC folder); this is a summary.

- **A place file**, `maps/<place>.json`, holds a layout and no creatures: `title`, `cell_ft`, `grid` and optional `legend`, `labels`, `notes`, `edges`, `north` and `scale_note`.
- **The scene file**, `maps/live.json`, puts creatures on a place: `"base"` names the place file (without `.json`, in the same folder) and `"tokens"` lists the creatures. Any key the scene sets replaces the place's, except that a scene cannot change its place's scale.
- **The turn order** of a fight is two optional keys of the scene: `"initiative"`, the creatures in the order they act, and `"current"`, whose turn it is. Write each as a token's `id` (preferred; ids are unique) or the name of exactly one token; a name shared by several tokens is refused, with their ids. `current` must be in `initiative`. `dmc map` draws them as a numbered list beside the grid with the current turn marked. A scene does not inherit them from its base place, and both keys are deleted when the fight ends. DMC keeps them in step with the combat tracker in `game-state.md`.
- **The grid** is a list of rows, one character per cell, every row the same length; a space is empty. Columns are lettered from the left and rows numbered from the top, so `E3` is the fifth cell of the third row. Built-in symbols: `#` wall, `.` floor, `+` door, `=` window, `~` water, `^` hazard, `>` and `<` stairs, `T` tree, `o` pillar, `_` low cover, `,` difficult terrain, `?` not yet seen. The legend renames a symbol or defines a new one.
- **The scale**, `cell_ft`, must be 5 (the default), 30, or 5280, 31680 or 316800 for overland maps (1, 6 or 60 miles a square). Anything else, including `null`, is refused.
- **A token** has an `id` of one or two characters (`@` for the PC, letters for allies, numbers for enemies) and `at`, its top-left cell; optionally `name`, `kind` (`pc`, `ally`, `enemy` or `neutral`), `size` in cells, a short `note` the player can see, and `down` for a creature that is dead or unconscious. At 5 ft two tokens cannot share a cell; on larger squares several can.

`dmc map` writes `live.html`, `live-data.js`, `live-source.txt` (two lines: the file's name, and the path of its base place relative to `maps/`, so that the Stop hook's `dmc redraw` can tell from modification times alone whether anything changed) and, with `--png`, `live.png` next to the file; `dmc redraw` rewrites the first two when the file changes. The player can see everything in a map file, so only what the character has seen or knows goes in.

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

`rules/` holds the player's optional rules reference, normally the SRD, and is the player's. `/dmc-setup-rules` writes `rules/index.md` (which document and edition, a section table with page ranges, a finer breakdown of the long alphabetical sections, and the pages most needed in play). For a PDF it first runs `dmc pdf text`, which writes `rules/extracted/all-text.txt` and one text file per page in `rules/extracted/text/`.

## Feedback entries

`feedback/` in the DMC folder holds feedback about how DMC itself runs the game, one file per entry, `feedback/YYYY-MM-DD-<slug>.md`, from [`kit/templates/feedback.md`](../kit/templates/feedback.md) (`templates/feedback.md` in the folder). It is outside `adventures/` and every campaign, and entries quote the player and can hold spoilers, and nothing in play reads it: resume and full save never touch it, and `dmc install`, `refresh` and `play` never write it. The `dmc-record-feedback` skill writes entries in play; `review-transcript` writes them from the session logs; `review-feedback` reads them and updates their status.

The frontmatter has `status` (`open`, `filed #N`, `fixed in PR #N`, `not a toolkit problem: <why>` or `moved to player options`), `category`, `source` (`player`, `DMC` or `transcript review`), `recorded` and `dmc`, the version number of `dmc` when the entry was recorded (`dmc: 0.2.0`, from `dmc --version` without the leading `dmc `), so a worker knows which release's prompts the entry is about. A worker finds the open entries with one search for `^status: open`; there is no index. (A DMC folder is not a git checkout, so the Grep tool, which skips git-ignored files, searches the entries without help.) The sections are What happened, What the player said, What should happen instead, Occurrences (a repeat adds a line, not a file), Done in play (a rewind, a ruling, or a stopgap preference line to remove once the fix ships), Context (optional pointers into a campaign) and Worker notes. An entry is written so that it makes sense if the campaign has been deleted. Entries are never deleted: a closed status is the record.
