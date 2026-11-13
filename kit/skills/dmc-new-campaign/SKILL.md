---
name: dmc-new-campaign
description: Create a new solo D&D campaign folder from the templates and run session zero (player preferences, character creation, opening premise). Use when the player wants to start a new adventure, to carry on a campaign played elsewhere (a chat export or transcript) or to continue a closed campaign as a sequel, or when no campaign exists yet.
---

# New campaign

Set up a campaign so that every file a future session needs exists and is filled in before play begins.

## 1. Name and folder

Ask for a campaign title if one was not given (offer two or three ideas if the player has none). Turn it into a folder name: lowercase, words joined by hyphens, no other punctuation. If `adventures/<folder>/` already exists, stop and ask whether to resume it or pick another name.

Copy everything in `templates/campaign/` to `adventures/<folder>/`, run `dmc campaign standard <folder> --init` to record that its files follow the current rules, then write the folder name as the only line of `adventures/.active-campaign`.

## 1a. Homebrew or published module

Ask whether this is an original adventure or a published module.

For a module:

- Look in `modules/` for it. If it is not there, ask for the file and follow the `dmc-ingest-module` skill. The survey and the opening chapter must be ingested before play; the rest can follow later.
- Copy `templates/module-progress.md` to `adventures/<folder>/module-progress.md` and fill in the header and the starting position from the module's `index.md`.
- In session zero, take the edition, starting level and setting from the module, and tell the player those without spoiling the plot. Offer the module's own character hooks or backgrounds if it has them.
- In step 4, the conflict, antagonists and secrets in `dm-notes.md` come from the module's `index.md`. Use that file for how this player's character ties in and for anything you add.
- Modules expect four or five characters. Agree how to handle that: companions run by DMC, a higher starting level, scaled encounters, or a mix. Record the choice in `module-progress.md`.

## 1b. A campaign played elsewhere

If the player wants to carry on a campaign played somewhere else (a chat export, a transcript, a handoff note), import it first. Put the files in `adventures/<folder>/import/` and leave them there as the record.

Never read the export into this conversation. Whatever is read stays in context for the rest of the session, so reading a long export in parts still fills it. Give each part its own fresh context instead:

1. Cut the export into parts of about 100 KB, at session or scene breaks, and note each part's line range.
2. For each part in order, start a subagent with the Agent tool. Run them one after another, never at the same time: they write the same files. Tell each one: the campaign folder and its part's line range; to read the part with the Read tool's offset and limit, in pieces small enough for one read; to read the campaign files as earlier parts left them before adding to them; and to write, before it ends, everything its part establishes: canon lines (one per fact, with the session), character sheets from `templates/pc-sheet.md` and `templates/npc-sheet.md`, one session log per original session from `templates/session-log.md`, and DM plans, enemy numbers and anything the player has not seen in `story/dm-notes.md`. Only the last part writes `game-state.md`, as the present moment. Where the export contradicts itself, it adds the contradiction and a proposed reading under "Known inconsistencies" in `canon.md` instead of choosing silently. It replies with a short note only: where it stopped (line and in-world moment) and what it could not settle.
3. If subagents are not available, do one part per session instead: write the files, record where you stopped in `import/progress.md`, and ask the player to type `/clear` and then ask you to continue the import.

When every part is done, read the campaign files as `dmc-resume-campaign` does, run the consistency check from `dmc-campaign-full-save` (step 7), and settle the known inconsistencies with the player. Then go through steps 2 to 4 below only for what the export did not cover, such as the player options.

## 1c. A sequel

If the player wants to continue a closed campaign, import it before the first scene. Nothing is ever written to the closed campaign: everything goes into the new one, and `dmc` does the exact parts.

1. Check that `dmc campaign standard <closed>` says `closed`; if not, offer to close it first with `dmc-close-campaign`. Write it on this campaign's Continues line (step 2's Continuity), and ask which characters carry over.
2. Run `dmc campaign sequel <folder> --start`. It writes `digests/<closed>/` with its index and scene spine, copies the digests the closed campaign holds, and prints the parts. Write the carried characters' names on the index's Carried line as the closed campaign's sheets title them.
3. For each part in order, start a subagent as § 1b does, one after another. Tell each one its part's files and line ranges, and to read the closed campaign only through them, and what earlier parts wrote before adding to it. Part 1 writes the brief into this campaign's own files: open and deferred threads into the Arc plan of `story/dm-notes.md`, house rules and standing requests into `player-options.md`, and the DM notes' secrets into the digest's `secrets.md` (and into this campaign's DM notes where they can still come up). Every other part writes the digest, in `digests/<closed>/` only:
   - one `## ` heading per subject in `people.md`, `places.md`, `factions.md`, `items.md` and `world.md` (`## Rulings`, `## Prices, debts and promises`, `## Lore`, `## How it ended`, `## Module`), a person's heading being the title of their sheet; a kind near 20k tokens goes on in `people-2.md`, a part holding whole subjects;
   - each line copied word for word under one subject, keeping its `sN` or `sN.M`; a fact that lives only in a session log added under its subject with its `sN.M`;
   - secrets in `secrets.md`, each with its outcome (`revealed sN`, `never revealed`, `abandoned`), a sheet's own `## Secrets` lines under the character's name;
   - a living character's stat block under their heading; a dead one's left out;
   - nothing of plans the story overtook, scene or combat state, or module text; `module-progress.md`'s deviations and module NPCs' statuses go to `world.md` § Module.

   The part that holds canon also copies its Rulings and its Prices, debts and promises into this campaign's `story/canon.md`; the part that holds the summary writes this campaign's premise and earlier arcs into its `story/summary.md`; the part that holds a carried character's sheet writes their sheet here by following `dmc-character-sheet-update`.
4. Run `dmc campaign sequel <folder> --list`, and account each line it prints in `digests/<closed>/accounting.tsv`: `<id>`, a tab, `merged`, a tab and words of the digest line it went into; or `<id>`, a tab, `dropped:<class>`, a tab and the reason, the class one of `superseded` (name the line that replaced it), `not-canon`, `structure`, `bookkeeping` or `stat-block` (name the sheet). A line lost by mistake is copied, not dropped. Then run `--check` and fix what it names until it passes.
5. Run `--sample`. Give one fresh subagent only the digest folder's path and the items, and ask it for each fact's who, what, when, where, why and how, and for each person who they are, what happened to them and where they stand. Compare its answers yourself with the closed campaign's files. Here and in step 4, never read a whole file of the closed campaign or of the digest into this conversation: find the lines with `dmc recall <folder> <words>` or a search, and read only those lines. A miss is a question the closed campaign answers that the answer gets wrong or leaves out: fix the digest, run `--check` again and draw again with `--round <next>`. The sample passes with no miss.
6. Keep `import/progress.md` up to date as you go (the last part done, the check, the round), so an interrupted import carries on where it stopped.

Then go through steps 2 to 4 below only for what the brief does not cover. Open the first scene only after the sample passes.

## 2. Session zero

Have a short out-of-character conversation. Ask a few questions at a time, not a form.

**General preferences.** If `adventures/player-options.md` does not exist, this is the player's first campaign: create it from `templates/player-options.md` and ask about what it covers:

- **Dice**: the default is that the player rolls for their own character and party checks and DMC rolls for everything else. Confirm, or change it.
- **What they enjoy**: combat, intrigue, exploration, puzzles, roleplay; any content to avoid.
- **Pacing and narration length**, and whether they want suggested actions offered.

If the file already exists, read it, tell the player in a line or two what it says, and ask only whether anything should be different for this campaign. Differences go in the campaign file's Overrides section.

**This campaign.** Write these into the campaign's `player-options.md`:

- **Rules edition**: 2014 (default) or 2024 5e. Starting level.
- **Tone**: heroic, grim, comic.
- **Lethality**: story, balanced or gritty. Explain each in a sentence.
- **Setting and premise**: their idea, or pitch three short hooks and let them choose.
- **Continuity**: its links to other campaigns, one name or `none` on each line. **Continues**: for a sequel, the folder in `adventures/` of the campaign it carries on, which must be closed: if `dmc campaign standard <it>` does not say `closed`, say so and offer to close it first with `dmc-close-campaign`. **Side campaign of**: the main campaign this one is played alongside, in its world. **Shares a world with**: any other campaigns in the same world, separated by commas. Then run `dmc campaign links <folder>` and tell the player any `notice:` line in a sentence. `dmc recall` searches every linked campaign with this one.

Leave defaults where the player has no preference.

## 3. The player character

Build the character together, following the rules for the chosen edition. The player makes every choice; you explain options and do the arithmetic. If they want rolled stats, the player rolls them unless the player options say DMC rolls.

Create `character-sheets/pc-<name>.md` from `templates/pc-sheet.md` and fill in every field. Double-check derived numbers (modifiers, proficiency bonus, AC, HP, save DCs, attack bonuses) before saving. Ask for a few lines of backstory, a goal and a tie to the setting.

If the player wants a companion, create it the same way from `templates/npc-sheet.md`.

## 4. The world behind the curtain

Before the first scene, write down enough that you will stay consistent later:

- `story/dm-notes.md`: the central conflict, the main antagonist and what they want, two or three secrets, a few possible directions. Keep it loose; this is a plan, not a script. For a homebrew campaign, fill in the Arc plan for the first arc as the template's comment lays out, checking the party's gaps against the threats you plan. Fill in the Opposition table for whatever stands between the character and the first goal, sized for the party as described under "Sizing fights" in `CLAUDE.md`, and check that the party has some way to rest or heal along the way.
- `story/canon.md`: the facts the player already knows (setting name, starting town, anyone they know).
- `character-sheets/`: a sheet for each NPC who will matter in the first session.
- `game-state.md`: starting location, date and time, the PC's current HP and resources, the opening quest hook if there is one.
- `story/summary.md`: a two or three sentence premise.
- `story/sessions/session-001.md` from `templates/session-log.md`.

## 5. Begin

Confirm the player is ready, then open the first scene.
