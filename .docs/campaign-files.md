# Campaign files

This document is for anyone who reads or edits a campaign's files by hand, changes the templates, or needs to know which file holds which fact. The rules for when DMC writes each file are in [kit/dm-rules.md](../kit/dm-rules.md) (a DMC folder's `CLAUDE.md`) § Campaign files and § Saving as you go, and in the skills (`kit/skills/`); this document describes the files.

## Layout

Everything below is in a **DMC folder**, the folder a player plays in (written by `dmc install`). Next to the player's folders it holds the files `dmc` writes and keeps up to date: `CLAUDE.md`, `.claude/settings.json`, `.claude/skills/dmc-*/`, `templates/` and `migrations/` (their source is `kit/` in the repository; `migrations/` holds the numbered files DMC follows to bring an older campaign's files up to date, see [Bringing a campaign up to date](#bringing-a-campaign-up-to-date)), and `.dmc/manifest.json`, which lists them with their hashes and makes the folder a DMC folder. `dmc install`, `dmc refresh` and `dmc play` write only those, and never anything under `adventures/`, `feedback/`, `modules/`, `rules/` or `exports/`; a generated file the player edited is kept under `.dmc/replaced/<yyyyMMdd-HHmmss>/` before it is replaced. The player's own files (their `.claude/settings.local.json`, their own skills) are never touched.

`feedback/` sits in the DMC folder, beside `adventures/`. It is not part of any campaign: feedback about how DMC itself runs the game is kept there (see [Feedback entries](#feedback-entries)), never in a campaign folder.

`exports/` sits in the DMC folder too. `dmc campaign export` writes its ZIP files there, one per export (`<campaign>-<YYYYMMDDHHmm>.zip`), and `dmc campaign close` puts the copy of a closing backup there (`<campaign>_closed_<yyyyMMddHHmmss>.zip`). Each file holds a whole campaign, `story/dm-notes.md` and other spoilers included, and is not part of any campaign; keep copies outside `adventures/`, where DMC would search them.

```
exports/                     one ZIP per dmc campaign export, and the copy of each closing backup, in the DMC folder (whole campaigns, spoilers included)
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
    .closed                  only in a closed campaign: when it was closed and its closing backup; written by dmc campaign close --yes, never removed
    backups/                 backups of the campaign taken before a migration (dmc campaign backup); never exported
      <campaign>_backup_<yyyyMMddHHmmss>.zip   a whole campaign export, checked when written
      <campaign>_closed_<yyyyMMddHHmmss>.zip   only in a closed campaign: the closing backup, never counted, pruned or removed
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
    import/                  only for a campaign imported from elsewhere (the export, kept as the record) or a sequel: progress.md, where an import stands
    digests/<campaign>/      only in a sequel: a closed campaign's facts by subject (spoilers); searched, never read at resume
    legacy.md                only when linked to an open campaign: what changed in the shared world, for the linked campaigns
```

`adventures/` is the player's, and `dmc` never writes it (a DMC folder is not a git checkout). A new campaign is a copy of `templates/campaign/` (generated from `kit/templates/campaign/`); the archives, `import/` and the map outputs are created when first needed.

## What each file holds

| File | Holds | Written | Template | Read at resume |
|---|---|---|---|---|
| `adventures/player-options.md` | Dice (who rolls, how rolls are asked for and shown, secret rolls), how to talk with DMC, narration style, maps; Image styles (optional: the saved looks for image prompts and the last one used, written by `dmc-generate-image-prompt` when the player saves, changes or uses a saved style) | When the player asks for a change, and by `dmc-generate-image-prompt` | `templates/player-options.md` | Yes |
| `player-options.md` | Edition, starting level, leveling, HP on level-up, what is tracked, house rules, the campaign's links to other campaigns (§ Continuity, see [Linked campaigns](#linked-campaigns)), lethality, combat difficulty, puzzle hints, tone, overrides of the general file | When the player asks for a change | `templates/campaign/player-options.md` | Yes |
| `game-state.md` | Campaign, session number, last full save (date, session and the last scene saved); the scene (location, date and time, conditions, who is present, what is happening, what the player was about to do); party status per PC and companion, plus a "With the party, not companions" block for rescued NPCs, prisoners and guests (HP, hit dice, slots, resources, conditions, concentration, exhaustion, death saves, inspiration, gold, consumables); the combat tracker; plan and standing orders for where the party is now (orders not in play go to canon); quests; clocks; pending; in a linked campaign only, the Legacies folded line (see [Legacy files](#legacy-files)) | Whenever any of it changes, before the next beat is narrated; in a fight, the tracker block once per reply, in one edit | `templates/campaign/game-state.md` | Yes, first after the options |
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
| `legacy.md` | Only in a campaign linked to an open campaign: what changed in the shared world, one line per change with its `sN.M`, oldest first; see [Legacy files](#legacy-files) | At every full save, the closing save included | `templates/legacy.md` | Not by its own resume; once by each linked campaign's resume while it is news |

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
| `legacy.md` | 3,000 |

`legacy.md` is measured and flagged like the files above, but its own resume never reads it, so it is outside the resume total. The player options, the session log and the module files have no budget: the full save does not trim them. Any file the resume reads is flagged when it reaches 20,000 estimated tokens or 1,600 lines, near what the Read tool returns in one call. `dmc size` also flags what the templates say should not stay in a file read whole: more than one Last played line or a per-session Mix tracker, Opposition rows with none left alive, DONE or RESOLVED lines outside the Arc plan, a Prices, debts and promises section over 3,000, and lines over 400 characters.

Spoilers: the player can read and edit any file, but `story/dm-notes.md`, `story/dm-notes-archive.md`, `module-progress.md`, NPC sheets (their secrets) and anything under `modules/` give away what is ahead.

## Bringing a campaign up to date

When the toolkit changes what campaign files should hold, the change ships a **migration**: a numbered file in
`migrations/` of the DMC folder (`002-sheet-history-last.md`, and so on) that DMC follows to move an older campaign's
files to the new shape. A campaign's **standard** is the number in its `.standard` file (1 when there is none); the
kit's is the highest migration number. `dmc campaign standard NAME` says `current`, `behind` (with the pending
migrations), `ahead` (the files were written by a newer `dmc`) or `unfinished` (a migration was started and not
verified or restored), or `closed: <time>` for a closed campaign, which is never migrated (see [Closing a campaign](#closing-a-campaign)). The resume says one line when a campaign is not current, and offers the migration.

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

## Closing a campaign

A finished campaign can be closed for good (spec 010, issue #180), when the player asks. The `dmc-close-campaign` skill
asks once, then makes a last full save with these differences, so a sequel finds every fact where it looks first:

| Record | Where |
|---|---|
| The ending: how the story ended, the in-world date, each PC's and companion's final level, condition and place, each recurring NPC's place, and what in the folder is not canon (a test fight, an import) | `story/summary.md` § How it ended, in place of § Where things stand |
| Every secret, gathered back from `story/dm-notes-archive.md`, each line ending `revealed sN`, `never revealed` or `abandoned` | `story/dm-notes.md` § Secrets |
| Each loose end, as a fact in the character's terms; promises and debts under **Prices, debts and promises** | `story/canon.md` |
| The reason and the means of each fact, where the sessions established them | `story/canon.md`, on the fact's line |

The closing save moves nothing to `story/canon-archive.md` or to a sheet's `-history.md` file, trims nothing for size
and adds no planned break: a closed campaign is never resumed into play. There is no template change, so open campaigns
need no migration.

`dmc campaign close <campaign> --yes` then writes the two copies of the closing backup, each checked, and the mark
last, so a failure, or the close being stopped part-way, leaves the campaign open; a failure removes what the run wrote.

| File | What reads and writes it |
|---|---|
| `.closed` | One line, `<UTC time> <closing backup file name>`. Written only by `close --yes`; nothing removes it. Its presence closes the campaign: every `dmc` command that writes under the folder refuses, `standard` prints `closed: <time>`, resume says the campaign is closed and offers a sequel, and DMC is told never to change its files (`CLAUDE.md` § Accuracy) and is denied edits to the mark. Travels with every export, so an import of a closing backup is closed too. |
| `backups/<campaign>_closed_<stamp>.zip` | The closing backup: an export of the campaign with its mark, checked whole. Its name is outside the counted backups' pattern, so no prune ever removes it. `verify` compares the campaign with it, byte for byte, and writes nothing. |
| `exports/<campaign>_closed_<stamp>.zip` | A byte copy of the closing backup in the DMC folder, checked by SHA-256, so the campaign survives losing its folder. Keep a further copy somewhere else. |

A sequel is a new campaign whose `player-options.md` Continues line names the closed one. It starts by importing the
closed campaign into a brief, folded into its own files, and a digest ([A digest](#a-digest)); `dmc recall` searches
the digest first and the closed campaign's own files last, and nothing writes to them.

## Linked campaigns

A campaign's `player-options.md` holds its links to other campaigns in a `## Continuity` section, after `## Rules`
(migration 005 moved the older single Predecessor line there):

| Line | Value |
|---|---|
| **Continues** | `none`, or the folder of the closed campaign this sequel carries on. `dmc-new-campaign` writes only a closed one. |
| **Side campaign of** | `none`, or the folder of the main campaign this one is played alongside, in its world. |
| **Shares a world with** | `none`, or the folders of other campaigns in the same world, separated by commas. |

`dmc` follows links both ways: a campaign is linked to every campaign it names and to every campaign that names it,
and its **linked set** is every campaign reachable through links from it, each once, with its distance in steps and
how it was reached: `continues` (an earlier campaign of the chain), `sequel` (a later one), `side` (one Side campaign
of step) or `world` (anything else). A closed campaign that still has a Predecessor line keeps its link: `dmc` reads
the line as Continues wherever a file has no Continuity section. A line inside an HTML comment is not read. `dmc
campaign links NAME` lists the set; the resume hears of a link that cannot be followed (a missing folder, a value that
is not a folder name, a link to itself, a second name for a one-name line) through the `dmc size --check` it already
runs, for the lines of campaigns that are not closed: a closed campaign's file can no longer be changed.

`dmc recall` searches the whole linked set and each campaign's module once, and labels a hit from another campaign
with its link; a fact from a `side` or `world` campaign is a fact of the world, not something this campaign's
characters know unless its own files say they learned it, and a fact from a `sequel` (recalled in an earlier campaign
kept open) has not happened yet.

A sequel holds, at its top level, `digests/<campaign>/`: a closed campaign's facts copied word for word and
reorganised by subject, with its scene spine and its secrets (see [A digest](#a-digest) below). `dmc recall` and `dmc lookup` search a digest like the campaign's own
files, except its `index.md` and `spine.md`, which hold no facts (recall leaves both out; lookup still finds the spine's headings); the resume never reads one, so `dmc size` leaves it out of the
resume total; and a digest's secrets and its `## Module` section (a copy of `module-progress.md`) are marked DM-only in
recall. A campaign whose digest a campaign nearer the start holds (a `digests/<campaign>/` folder with a `.md` file in
it) is the last resort: its own folder is still searched, but the digest's line ranks first, and recall prints a line
the two share once.

### Legacy files

Two campaigns still in play in one world (a main campaign and a side campaign, say) pass what changed in the world to
each other through `legacy.md`, at the top of each one's folder. A campaign alone, or one whose only link is Continues
to a closed campaign, has none and pays nothing for it.

```markdown
# Legacy of side-quest

- **Written**: session 14, 2026-11-20

## What changed in the world

- The ford bridge on the east road is burned down (s13.2).
- The baron of Hollin is dead (s14.1).
```

- **Who writes it.** At every full save, the closing save included, `dmc size --check` prints `legacy due: at the full save, update this campaign's legacy.md for open
  linked campaigns <names>: add this save's changes and drop the lines from session N and earlier` while the
  campaign's linked set holds an open campaign; N is the lowest session of this campaign's legacy that every open
  linked campaign has folded, so the lines dropped are ones they all have, and the line says `keep every line the budget allows` while
  any has folded none. The save adds its changes at the end, stamps the Written line and drops what it is told. Past its
  budget the oldest lines go; the writer's canon still holds them, and recall reaches it.
- **Who reads it.** At resume, `dmc size --check` prints `legacy: read adventures/<campaign>/legacy.md (written
  session N; record <campaign> sN)` for each linked campaign whose legacy was written in a later session than the one
  this campaign last folded, or never folded; not for a campaign whose digest this one, or a campaign nearer it in the set, holds (a sequel's
  predecessor), which the digest already carries, and nothing at all for a closed campaign. The resume reads that file and no other
  file of that campaign.
- **The fold.** The next full save sees the same line at its own check, puts the changes into canon as world knowledge
  (unless the characters saw them), and records `<campaign> sN` on `game-state.md`'s `- **Legacies folded**:` line,
  which the save keeps: it is a record, not history. The line is not in the template; the first fold adds it.

Session numbers order legacies, not dates or times, which DMC does not write reliably. One campaign is played at a time,
in one terminal: two linked campaigns played at once are not supported.

### A digest

`dmc-new-campaign` § 1c writes a sequel's digest when it starts the sequel, with subagents, part by part, and
`dmc campaign sequel` does the exact parts ([dmc-reference.md](dmc-reference.md#a-sequels-import)). Everything is
written in the sequel; nothing is ever written to the closed campaign. In `digests/<closed>/`:

| File | Holds | Written by |
|---|---|---|
| `index.md` | Format, Campaign, Closing backup (its file name and SHA-256), Chain (the digests copied with it, nearest first), Carried (the characters the sequel carried over), Files. No facts. | `sequel --start`; Carried by DMC |
| `people.md`, `places.md`, `factions.md`, `items.md` | One `## ` heading per subject, then its lines. A living character's stat block goes under their heading. A kind near 20k tokens goes on in `people-2.md` and so on, each part holding whole subjects. | DMC |
| `world.md` | `## Rulings`, `## Prices, debts and promises`, `## Lore`, `## How it ended`, `## Module` (the deviations and module NPCs' statuses of `module-progress.md`, DM-only in recall) and any other subject of the world. | DMC |
| `spine.md` | Per session log, in session order, `## <its # title> (session-NNN.md)`, then `- sN.M <the text after "Scene M: ">` for each scene heading, word for word; a log with no scene headings has its line alone. | `sequel --start` |
| `secrets.md` | The closed campaign's secrets, one `## ` heading per subject, each line ending with its outcome (`revealed sN`, `never revealed`, `abandoned`); a sheet's own `## Secrets` lines under the character's name. DM-only in recall. | DMC |
| `accounting.tsv` | One line per line of the closed campaign not copied word for word: `<id>`, a tab, `merged`, a tab and words of the line it went into; or `<id>`, a tab, `dropped:<class>`, a tab and the reason. The classes are `superseded`, `not-canon`, `structure`, `bookkeeping` and `stat-block`. | DMC |

Every line copied keeps its `sN` or `sN.M`, and a fact that lived only in a session log is added under its subject
with its `sN.M`. Plans the story overtook, scene and combat state, a dead character's stat block, dice bookkeeping in
the logs, maps, `import/`, `exports/`, `backups/`, the module's own text and whatever the closing save named as not
canon are left out. Every line of the brief keeps its `sN` or `sN.M` and names the campaign it came from. A part
reads only the sections it adds to, found with `dmc lookup` or a search, never a whole digest file, and before it ends
moves whole subjects to the kind's next file for each `too large:` line of `sequel --check`. Without subagents, the
import does one part per session, and the sample is answered in a session that reads only the digest. The lines that
must be accounted for are those of `story/canon.md`, `story/canon-archive.md`,
`module-progress.md` and every file in `character-sheets/`, with § Secrets of `story/dm-notes.md` and § How it ended of
`story/summary.md`, all read from the closing backup; `sequel --check` proves each one is copied, merged or dropped
with a reason, that the copied digests match the closing backup's byte for byte, that no file is over 20k tokens and
that no subject has two headings. A sample of 20 facts and 5 people, answered by a fresh subagent from the digest
alone, then has to pass with no miss.

A digest never changes once its check and its sample pass, and a later sequel copies it byte for byte. A change to
the digest's format that older digests cannot meet ships as a migration that rebuilds them from the closing backup
`index.md` names.

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
2. **Facts:** the campaign files, then the module as written (where play has not diverged from it), then the 5e rules in the campaign's edition, then new invention, which must not contradict the others. Among the campaigns of a linked set, a campaign's own canon wins over a digest of an earlier one, a digest over that campaign's own folder, and every campaign over a module as printed; recall ranks them so.
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
