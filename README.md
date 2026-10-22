# Dungeon Master Claude

Play solo D&D 5e campaigns with Claude as your Dungeon Master. Claude (called **DMC** here) runs the game in your terminal and keeps the whole campaign in plain Markdown files, so the story and your character stay consistent across sessions that may be weeks apart.

## What you need

- [Claude Code](https://claude.com/claude-code), signed in.
- This folder, on Windows (x64 or Arm), Linux (x64 or Arm, with glibc 2.34 or later, as in Ubuntu 22.04) or macOS on Apple Silicon.

Nothing else needs installing. The dice roller, the maps, PDF reading, the break reminder and the session review are one small program, `dmc`, which the launcher downloads into `bin/` the first time (checking it against the checksum committed in `tools/dmc-release.json`) and keeps up to date. If you start Claude Code some other way and `bin/dmc` is missing, DMC fetches it itself with `bash tools/get-dmc.sh`, or in PowerShell `powershell -ExecutionPolicy Bypass -File tools/get-dmc.ps1`, which works whatever the execution policy. Without `dmc`, play and saving still work, but DMC cannot roll its own dice, draw maps or read PDFs.

Python is no longer needed. If an older copy of this folder has a `scripts/` folder, you can delete it: it only holds the libraries the old Python scripts used.

No rulebook text is included. DMC works from its knowledge of the 5e rules; for anything it is unsure of it will look the rule up or make a ruling and record it. For better accuracy, add a local rules reference (below).

### Optional: add a local rules reference

Giving DMC the rules to read makes its rulings, spells and monster stats more reliable than memory alone. The System Reference Document (SRD) is free and covers the core rules.

1. Download the SRD 5.1 PDF (the 2014 rules, released by Wizards of the Coast under CC-BY-4.0): <https://media.wizards.com/2023/downloads/dnd/SRD_CC_v5.1.pdf>. Other versions, including the 5.2 SRD for the 2024 rules, are linked from <https://www.dndbeyond.com/srd>.
2. Save it in the `rules/` folder.
3. Start DMC in this folder (see Quick start) and type `/setup-rules`. DMC builds a page index so it can open the right pages quickly. This is a one-time step.

A Markdown or text copy of the SRD works too, and is faster to search. The SRD does not contain everything in the published rulebooks (most subclasses and some spells and monsters are missing); for those DMC falls back to a web lookup or a ruling.

## Quick start

1. Open a terminal in this folder and start DMC with `./play.ps1` in PowerShell on Windows, or `./play.sh` on macOS, Linux or Git Bash. It starts Claude Code on Sonnet at `high` effort with the settings made for play. If PowerShell says running scripts is disabled, use `powershell -ExecutionPolicy Bypass -File .\play.ps1`.
2. Type `/new-campaign`. DMC asks how you like to play, helps you build a character and sets up the campaign files.
3. Play. Say what your character does in plain language.
4. When the conversation gets long, DMC offers a break at a natural pause. Say yes, and when it confirms the save, type `/clear` and then `/resume-campaign`. You carry on from where you were.
5. When you want to stop, type `/campaign-full-save`.
6. Next time, start DMC the same way and type `/resume-campaign`.

Plain `claude` also works, but loads more that play does not use, starts on your account's default model and gets no break reminder.

## Commands

| Command | What it does |
|---|---|
| `/new-campaign` | Create a campaign and run session zero |
| `/resume-campaign` | Load a campaign and get a recap. Add a campaign name to switch campaigns |
| `/campaign-full-save` | Save everything and check the files agree with each other |
| `/update-game-state` | Sync HP, resources, location and the combat tracker to disk |
| `/character-sheet-update` | Create or update a character or NPC sheet |
| `/draw-map` | Show a map of where you are, in your browser |
| `/ingest-module` | Read a published module so DMC can run it |
| `/setup-rules` | Index a rules reference you have put in `rules/` |
| `/rewind-campaign` | Go back to an earlier moment and undo what came after it |
| `/record-feedback` | Record feedback about how DMC itself runs the game, outside the campaign |
| `/review-transcript` | Review recorded play sessions and record the problems found as feedback |
| `/review-feedback` | Work through the recorded feedback and improve DMC's own prompts |

DMC uses these on its own as you play. You only need to type the first three, plus Claude Code's own `/clear` when you take a break. `/review-transcript` and `/review-feedback` are the exception: they are for working on the toolkit, and DMC only runs them when you ask.

## Talking to DMC

- **In character**: just say what you do or say.
- **Out of character**: start with `ooc:` or ask a plain question ("what are my odds?", "what can I do on my turn?", "remind me who that was"). DMC steps out of the story, answers frankly and steps back in. Asking about an option does not commit your character to it.
- **Changing how the game runs**: tell DMC ("hide the dice math", "make fights harder", "shorter descriptions"). It records the change in your player options.

DMC will not act for your character or end your turn without asking.

### Dice

By default you roll for your own character and for party checks, and DMC rolls for NPCs, monsters and the world. DMC asks in dice notation with your modifiers and the target included, and asks for rolls that go together in one message to keep combat quick: "Roll 1d20+6 to hit (AC 13), then roll 1d4+1 for damage." Then it waits. Answer with your final totals ("18 and 4") or show your working ("12+6=18 and 3+1=4"). Say "roll it for me" any time you would rather it rolled. DMC's own rolls come from a real dice roller, not from the model's imagination.

### Maps

When positions matter (a fight, a complicated room, or whenever you ask), DMC opens a map in your web browser: a grid with walls, terrain and a token for everyone in the scene. Keep that tab beside the terminal. It redraws itself within a couple of seconds whenever someone moves, with no reloading. Squares are named like a chessboard (`E4`), so you can say "I move to F4", and hovering over a token shows who it is.

The map is a local file (`adventures/<campaign>/maps/live.html`); nothing is uploaded. `dmc` draws it, so it needs nothing else. If you would rather have plain text maps in the terminal, tell DMC.

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
    canon-archive.md     facts that can no longer come up, searched when needed
    dm-notes.md          DMC's secret plans  (SPOILERS - do not read)
    dm-notes-archive.md  DMC's closed threads  (SPOILERS)
    sessions/            scene-by-scene record of each session
  maps/                  saved maps of places you may return to, and the live map
  module-progress.md     only for published modules: your place in the book  (SPOILERS)
```

DMC writes to these files as you play, not only when you save, so closing the terminal by accident costs you very little. `/campaign-full-save` additionally rewrites the summary and cross-checks the files.

These are ordinary text files. You can read and edit your character sheet or the options by hand. Stay out of `story/dm-notes.md`, `story/dm-notes-archive.md` and the NPC sheets if you want to be surprised.

### Long sessions

Every message re-reads the whole conversation, so a long one uses more of your usage limit with each message, and details blur as it grows. DMC therefore keeps sessions short and reloads from the files, which are always up to date.

Started with the launcher, DMC is told before each of your messages how full the conversation is. Past a point it offers a break at the next natural pause: the end of a scene, the end of a fight, or the start of a combat round with nothing waiting on you. Say yes and it saves everything and checks the files; then type `/clear` and `/resume-campaign`. DMC restates where you are without a full recap, and a fight resumes on the same round with everyone where they stood. Say no and it asks again later.

Claude Code can still compact a conversation on its own. That loses nothing either: state is saved as you play, and after a compaction DMC reloads from the files.

## Playing a published module

DMC can run a published adventure from your own copy instead of an original story.

1. Type `/ingest-module` and give the path to the file. PDF (including scanned books), Markdown, text, HTML and EPUB work. For content that only exists in an app or on a website, export or paste it into a file first.
2. For a PDF, DMC first runs `dmc pdf`, which writes the text of every page and a picture of every page next to your file, so it can read books of any size and study the maps and illustrations as well as the words. Password-protected PDFs cannot be read; print-restricted ones can. On Linux, an encrypted PDF also needs the system's OpenSSL library (`libssl`), which most distributions have.
3. DMC reads the book and writes an index, a DM digest of each chapter and reference lists into `modules/<module>/`, keeping page references back to your original. A long book takes a while and is best done in its own session; the opening chapters are enough to start, and the rest can be ingested later.
4. Type `/new-campaign` and say you want to play that module.

During play DMC runs from the digest and the original pages, not from memory, and keeps `module-progress.md` in your campaign folder: where you are, which areas are cleared, what you found and where you left the script. You are free to leave the script; DMC improvises and records what changed. Encounters are scaled for a solo character, since modules assume a party of four or five.

`modules/` is full of spoilers. It is also excluded from git, because the books are copyrighted: use your own purchased copy and do not share that folder.

## Tuning the game

Set in your player options, or just ask DMC:

- **Lethality** (per campaign): `story` (no death without your say-so), `balanced` (default: real danger, but bad luck alone will not end the campaign) or `gritty` (the dice decide).
- **Combat difficulty** (per campaign): `easy`, `medium` (default) or `hard`. DMC sizes every fight for the party actually present, whatever a module or its own earlier notes say.
- **Talking in character**: free by default, so speech and instructions to companions never cost an action. Set it to `strict` for a by-the-book game.
- **Dice**: who rolls what, whether you report the die or the total, whether DCs are announced, and how DMC's rolls are shown.
- **Edition** (per campaign): 2014 rules by default, or 2024.
- Tone, narration length, hints, suggested actions, how much rules explanation you want, content to avoid.

## Choosing a model

The launcher starts DMC on Sonnet at `high` effort. Sonnet is a good default: fast, and reliable at keeping the files up to date. Opus gives richer narration and longer-range plotting at higher usage. The launchers cannot be switched to it: once `dmc` is installed they hand over to `dmc play`, which always asks for Sonnet. To play on Opus, start Claude Code yourself in this folder, with the same effort and play settings: `claude --model opus --effort high --settings .claude/play-settings.json`. Run `./play.ps1` or `./play.sh` once first, or `bash tools/get-dmc.sh`, so that `bin/dmc` is there. Haiku is not recommended as the DM.

Choose the model before your first message and keep it for the whole session. Switching with `/model` partway through makes Claude Code send the whole conversation to the new model again, which takes a large bite of your usage at once, and changes the DM in the middle of a scene.

DMC runs at `high` effort. Its reasoning fills the conversation faster than the game does, but less reasoning is the biggest risk to keeping facts and numbers straight.

## Improving DMC from your own sessions

There are two kinds of feedback. How *you* like to play ("show my HP all the time") is a player option: tell DMC and it goes in your `player-options.md`. How *DMC* operates (a mistake, a fact that drifted, too much context used, a fight run against the rules, a quest that never ends) is feedback, which has exactly one route. Tell DMC, or start your message with "feedback:", and it records it with `/record-feedback` as one file in `feedback/`, outside your campaign, without leaving the game.

Claude Code also keeps a log of every session on your machine. `/review-transcript` reads the logs for a campaign and records where DMC let the game down (context and token use, facts or numbers that drifted, fights that were sized wrongly, DMC acting for your character, slow stretches, wasteful bookkeeping) as more entries in `feedback/`. It changes nothing.

`/review-feedback` is the one place where DMC is tuned. It works through the open entries, finds the cause in `CLAUDE.md`, the skills and the templates, and proposes changes, which it files as issues or makes once you say which entries to act on. Your own preferences stay in your player options; they are not written into the toolkit.

`dmc` condenses the logs, so it needs nothing else. The condensed transcripts it works from are written to `adventures/.transcript-review/`, which is not in git, and nothing leaves your machine. The entries quote you and can contain spoilers, so `feedback/` is not in git either.

## Folder layout

```
CLAUDE.md            DMC's instructions
README.md            this file
.docs/               product documents: guide, architecture, dmc reference, decisions, roadmap (start at .docs/README.md)
play.ps1, play.sh    start a play session: fetch dmc, then Sonnet, high effort, play settings
.claude/skills/      the commands above, and Spec Kit's speckit- planning commands (toolkit work only, not play)
.claude/settings.json  permissions for saving and dmc, compaction reminders
.claude/play-settings.json  settings for play only
.specify/            Spec Kit's templates, scripts and install state (toolkit work only)
specs/               specs, plans and task lists Spec Kit writes, one folder per feature, created when the first feature is specified (toolkit work only)
bin/                 dmc, the toolkit's program: dice, maps, PDF reading, session digest, break reminder (not in git)
tools/               get-dmc.ps1 and get-dmc.sh, which download dmc; dmc-release.json, the release they install
src/Dmc.Cli/         dmc's source code (C#, .NET 10)
tests/Dmc.Tests/     checks that dmc, the skills and the settings still work
templates/           blank campaign files and character sheets
adventures/          your campaigns (not in git)
feedback/            feedback about DMC, one file per entry, outside any campaign (not in git)
modules/             ingested published modules (not in git)
rules/               optional local rules reference (not in git)
```

The toolkit is a git repository. Your campaigns, modules, rules files and feedback are ignored by it, so you can commit, push and share the toolkit without including your saves or any books. Back up `adventures/` separately if you want a copy of your campaigns, and keep the copy outside `adventures/`: DMC searches that folder, and an old copy can feed it stale facts.

### Tests

Playing needs none of this. After changing `dmc`, a skill or the settings, run the tests from the toolkit folder. They need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (`winget install Microsoft.DotNet.SDK.10` on Windows). Publish `dmc` into `artifacts/bin/` first, because some tests run the program itself, then run them:

```
dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin
dotnet test
```

Use your system's runtime identifier in place of `win-x64`: `win-arm64`, `linux-x64`, `linux-arm64` or `osx-arm64`. This local build needs no C++ toolchain; add `-p:PublishAot=true` for the faster-starting build the releases use. `bin/` is the player's install, which `get-dmc` owns, so publish your own builds to `artifacts/bin/` (the tests insist that program says `dmc 0.0.0-dev`, as every build that is not a release does, and no release does). A build of your own put in `bin/` is replaced by the pinned release the next time `./play.ps1` or `./play.sh` runs, because players run the pinned release. A release gets its version from its tag, so the project file never holds one. To test a different program, set `DMC_BIN` to its path and the tests run that, whatever version it prints. The tests never touch `adventures/`. Tests for something you do not have (bash, PowerShell, Playwright's Chromium) are skipped; install Chromium for the map page tests with `pwsh tests/Dmc.Tests/bin/Debug/net10.0/playwright.ps1 install chromium` after a build. On GitHub the same tests run on Windows, Linux and macOS for every push to `main` and every pull request (`.github/workflows/tests.yml`), with nothing skipped.

A new `dmc` reaches players through a release: push a tag `dmc-vX.Y.Z` on `main`, and `.github/workflows/release.yml` builds it for every system, publishes it and opens a pull request that pins it in `tools/dmc-release.json`.

## Tips

- If DMC ever gets a fact wrong, tell it. It will check the files and correct course.
- You can run several campaigns; each is independent.

## One player, for now

The toolkit is built for a single player. Character sheets and game state are kept per character, so a companion or a second player's character fits the files already, but DMC's instructions (turn handling, who rolls, out-of-character talk) assume one person at the keyboard.
