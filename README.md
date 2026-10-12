# Dungeon Master Claude

Play solo D&D 5e campaigns with Claude as your Dungeon Master. Claude (called **DMC** here) runs the game in your terminal and keeps the whole campaign in plain Markdown files, so the story and your character stay consistent across sessions that may be weeks apart.

## What you need

- [Claude Code](https://claude.com/claude-code), signed in.
- This folder. Nothing else to install: the dice roller runs on PowerShell (Windows) or bash (macOS, Linux).

No rulebook text is included. DMC works from its knowledge of the 5e rules; for anything it is unsure of it will look the rule up or make a ruling and record it. For better accuracy, add a local rules reference (below).

### Optional: add a local rules reference

Giving DMC the rules to read makes its rulings, spells and monster stats more reliable than memory alone. The System Reference Document (SRD) is free and covers the core rules.

1. Download the SRD 5.1 PDF (the 2014 rules, released by Wizards of the Coast under CC-BY-4.0): <https://media.wizards.com/2023/downloads/dnd/SRD_CC_v5.1.pdf>. Other versions, including the 5.2 SRD for the 2024 rules, are linked from <https://www.dndbeyond.com/srd>.
2. Save it in the `rules/` folder.
3. Run `claude` in this folder and type `/setup-rules`. DMC builds a page index so it can open the right pages quickly. This is a one-time step.

A Markdown or text copy of the SRD works too, and is faster to search. The SRD does not contain everything in the published rulebooks (most subclasses and some spells and monsters are missing); for those DMC falls back to a web lookup or a ruling.

## Quick start

1. Open a terminal in this folder and run `claude`.
2. Type `/new-campaign`. DMC asks how you like to play, helps you build a character and sets up the campaign files.
3. Play. Say what your character does in plain language.
4. When you want to stop, type `/campaign-full-save`.
5. Next time, run `claude` here again and type `/resume-campaign`.

## Commands

| Command | What it does |
|---|---|
| `/new-campaign` | Create a campaign and run session zero |
| `/resume-campaign` | Load a campaign and get a recap. Add a campaign name to switch campaigns |
| `/campaign-full-save` | Save everything and check the files agree with each other |
| `/update-game-state` | Sync HP, resources, location and the combat tracker to disk |
| `/character-sheet-update` | Create or update a character or NPC sheet |
| `/draw-map` | Draw an ASCII map of where you are |
| `/ingest-module` | Read a published module so DMC can run it |
| `/setup-rules` | Index a rules reference you have put in `rules/` |

DMC uses these on its own as you play. You only need to type the first three.

## Talking to DMC

- **In character**: just say what you do or say.
- **Out of character**: start with `ooc:` or ask a plain question ("what are my odds?", "what can I do on my turn?", "remind me who that was"). DMC steps out of the story, answers frankly and steps back in. Asking about an option does not commit your character to it.
- **Changing how the game runs**: tell DMC ("hide the dice math", "make fights harder", "shorter descriptions"). It records the change in your player options.

DMC will not act for your character or end your turn without asking.

### Dice

By default you roll for your own character and for party checks, and DMC rolls for NPCs, monsters and the world. DMC tells you what to roll and the modifier ("Roll a d20 for Athletics, +5, DC 13"), then waits. Tell it the number on the die and it does the arithmetic. Say "roll it for me" any time you would rather it rolled. DMC's own rolls come from a real dice script, not from the model's imagination.

### Your player options

- `adventures/player-options.md` holds your preferences for every campaign: who rolls, how results are shown, how you like to talk with DMC, narration style. It is created the first time you play.
- `adventures/<campaign>/player-options.md` holds one campaign's settings (edition, lethality, tone, house rules) and can override the general file for that campaign.

Edit either by hand or just tell DMC what you want.

## How it remembers

Each campaign is a folder under `adventures/`:

```
adventures/player-options.md   your preferences for every campaign

adventures/<campaign>/
  player-options.md      this campaign's settings and overrides
  game-state.md          right now: location, time, HP, resources, combat, quests
  character-sheets/      full sheets for you, companions and recurring NPCs
  story/
    summary.md           the story so far, compressed
    canon.md             every established fact: names, places, prices, rulings
    dm-notes.md          DMC's secret plans  (SPOILERS - do not read)
    sessions/            scene-by-scene record of each session
  maps/                  saved maps of places you may return to
  module-progress.md     only for published modules: your place in the book  (SPOILERS)
```

DMC writes to these files as you play, not only when you save, so closing the terminal by accident costs you very little. `/campaign-full-save` additionally rewrites the summary and cross-checks the files.

These are ordinary text files. You can read and edit your character sheet or the options by hand. Stay out of `story/dm-notes.md` and the NPC sheets if you want to be surprised.

### Long sessions

Claude Code compacts a long conversation when it fills up, which can blur details. This project is built so that does not matter: state is saved continuously, and after a compaction DMC reloads from the files. For the cleanest results, play in sessions: save, close, and start fresh with `/resume-campaign`.

## Playing a published module

DMC can run a published adventure from your own copy instead of an original story.

1. Type `/ingest-module` and give the path to the file. PDF (including scanned books), Markdown, text, HTML and EPUB work. For content that only exists in an app or on a website, export or paste it into a file first.
2. DMC reads the book and writes an index, a DM digest of each chapter and reference lists into `modules/<module>/`, keeping page references back to your original. A long book takes a while and is best done in its own session; the opening chapters are enough to start, and the rest can be ingested later.
3. Type `/new-campaign` and say you want to play that module.

During play DMC runs from the digest and the original pages, not from memory, and keeps `module-progress.md` in your campaign folder: where you are, which areas are cleared, what you found and where you left the script. You are free to leave the script; DMC improvises and records what changed. Encounters are scaled for a solo character, since modules assume a party of four or five.

`modules/` is full of spoilers. It is also excluded from git, because the books are copyrighted: use your own purchased copy and do not share that folder.

## Tuning the game

Set in your player options, or just ask DMC:

- **Lethality** (per campaign): `story` (no death without your say-so), `balanced` (default: real danger, but bad luck alone will not end the campaign) or `gritty` (the dice decide).
- **Dice**: who rolls what, whether you report the die or the total, whether DCs are announced, and how DMC's rolls are shown.
- **Edition** (per campaign): 2014 rules by default, or 2024.
- Tone, narration length, hints, suggested actions, how much rules explanation you want, content to avoid.

## Choosing a model

Use `/model` in Claude Code. Sonnet is a good default: fast, and reliable at keeping the files up to date. Opus gives richer narration and longer-range plotting at higher usage. Haiku is not recommended as the DM.

## Folder layout

```
CLAUDE.md            DMC's instructions
README.md            this file
.claude/skills/      the commands above
.claude/settings.json  permissions for saving and dice, compaction reminders
scripts/             dice roller (roll.ps1, roll.sh)
templates/           blank campaign files and character sheets
adventures/          your campaigns (not in git)
modules/             ingested published modules (not in git)
rules/               optional local rules reference (not in git)
```

The toolkit is a git repository. Your campaigns, modules and rules files are ignored by it, so you can commit, push and share the toolkit without including your saves or any books. Back up `adventures/` separately if you want a copy of your campaigns.

## Tips

- If DMC ever gets a fact wrong, tell it. It will check the files and correct course.
- You can run several campaigns; each is independent.

## One player, for now

The toolkit is built for a single player. Character sheets and game state are kept per character, so a companion or a second player's character fits the files already, but DMC's instructions (turn handling, who rolls, out-of-character talk) assume one person at the keyboard.
