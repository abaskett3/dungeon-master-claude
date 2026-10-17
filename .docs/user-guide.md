# User guide

This guide is for the player: how to start DMC, play, save, take breaks and fix the problems that can come up. It assumes no programming knowledge.

## Getting started

### What you need

- [Claude Code](https://claude.com/claude-code), installed and signed in.
- This toolkit folder, on one of these systems:

| System | Supported |
|---|---|
| Windows | x64 or Arm |
| Linux | x64 or Arm, with glibc 2.34 or later (Ubuntu 22.04 or newer) |
| macOS | Apple Silicon only, not Intel Macs |

Nothing else needs installing. The helper program `dmc`, which rolls DMC's dice, draws maps, reads PDFs and reminds DMC to take breaks, is downloaded by the launcher the first time.

### Starting a session

Open a terminal in the toolkit folder and run the launcher:

| Shell | Command |
|---|---|
| PowerShell on Windows | `./play.ps1` |
| macOS, Linux or Git Bash | `./play.sh` |

If PowerShell says running scripts is disabled, use `powershell -ExecutionPolicy Bypass -File .\play.ps1` instead.

The launcher does three things:

1. It runs `tools/get-dmc`, which installs or updates `dmc` in `bin/`. It prints one line when it installs something and nothing when `dmc` is already current. Each download is checked against a SHA-256 committed in `tools/dmc-release.json` before anything in it is run.
2. It starts Claude Code on Sonnet at `high` effort, with the settings made for play (`.claude/play-settings.json`), which switch off connectors and tools a game does not use and add the break reminder.
3. Anything you type after the launcher's name goes on to Claude Code, so `./play.sh --continue` works.

If `dmc` cannot be had (no network, an unsupported system), the launcher says so in a line and starts Claude Code anyway. You can play and save, but DMC cannot roll its own dice, draw maps or read PDFs until `dmc` is fetched. See [Troubleshooting](#troubleshooting).

Starting plain `claude` in the folder also works, but it starts on your account's default model, loads more than play needs and gets no break reminder. In such a session, if `bin/dmc` is missing, Claude Code shows DMC a one-line notice at the start and DMC fetches `dmc` itself.

### Your first campaign

Type `/new-campaign`. DMC then:

1. asks for a title (or offers a few) and creates the campaign folder under `adventures/`;
2. asks whether this is an original adventure or a published module (see [Published modules](#published-modules));
3. runs a short session zero: how you like dice handled, what you enjoy, narration length, the rules edition (2014 by default, or 2024), starting level, tone and lethality;
4. builds your character with you, following the edition's rules. You make every choice; DMC explains options and does the arithmetic;
5. writes its own notes for the world behind the curtain, and opens the first scene.

To carry on a campaign you played somewhere else, say so and give DMC the chat export or transcript. It imports it into a new campaign folder in pieces, without reading the whole export into the conversation, and asks you about anything the export contradicts.

### Resuming

Start DMC the same way and type `/resume-campaign`. DMC reads the campaign files, checks that they agree with each other, and gives a short recap: where you are, what just happened, your condition and open quests. To switch campaigns, add the campaign's name: `/resume-campaign <name>`.

If the files disagree (HP on the sheet differs from the game state, say), DMC tells you what it found, proposes the likely truth and fixes the files once you confirm.

## Everyday play

### Talking to DMC

- **In character**: just say what your character does or says.
- **Out of character**: start with `ooc:` or put it in (( double brackets )), or simply ask a question your character could not ask ("what are my odds?", "remind me who that was"). DMC answers frankly and steps back into the story. Asking about an option never commits your character to it.
- **Changing how the game runs**: tell DMC ("hide the dice math", "shorter descriptions", "make fights harder"). It writes the change into your player options.

DMC will not act or speak for your character, end your turn, or spend a limited resource without asking. Talking in character, to companions or to a pet costs no action by default.

### Dice

By default you roll for your own character and for party checks, and DMC rolls for everyone else. DMC asks in dice notation with your modifiers already in it and the target stated, and asks for rolls that go together in one message:

> Roll 1d20+6 to hit (AC 13), then roll 1d4+1 for damage.

Answer with the totals ("18 and 4"), or show your working ("12+6=18 and 3+1=4"). DMC reads natural 20s and 1s from your working, or from a total that equals 20 or 1 plus the modifier. Say "roll it for me" when you would rather DMC rolled one.

DMC's own rolls come from `dmc roll`. With the default options, each is shown on its own line, exactly as `dmc` printed it, with a label, the dice, the total, the target and the result:

```
Goblin, shortbow: 1d20+4: [11] +4 = 15 vs AC 14, hit
```

Against an armor class, a natural 20 hits and a natural 1 misses whatever the total, and the line says so (`hit (natural 20)`). Checks and saves have no such rule. If a house rule of your campaign changes a result, DMC shows the line as rolled and states the ruling after it.

### Fights and plans

- At the start of a fight DMC writes each enemy's numbers into the combat tracker and keeps them fixed. When you ask what an enemy can do, the answer comes from that list, ranged and thrown attacks included.
- "Wait for X", "hold until X" or "when X, do Y" is a readied action, not a pass. DMC says it back in a line and mentions any rule that would stop it before you confirm.
- When you agree a plan with your companions, DMC checks it once against the map and the turn order, raises only what would make it fail outright, and writes it down. Companions then carry out their part without being asked again.
- If something you based a decision on turns out to be wrong (a range, a position), DMC stops and tells you before resolving what follows.

### Maps

When positions matter, DMC opens a map in your web browser: a grid with walls, terrain and a token for everyone in the scene. Keep the tab beside the terminal; it checks for changes every two seconds and redraws itself, with no reloading.

- Squares are named by column letter and row number, as printed along the edges: `E4` is the fifth column of the fourth row, counting rows from the top. You can answer the same way: "I move to F4".
- In a fight DMC changes `maps/live.json` and finishes its reply, and the map redraws after the reply, so the picture normally changes only after the text that describes it. This is done by a hook in the play settings (`dmc redraw`), so it needs the launcher and a `dmc` of release 0.1.3 or later, and it does not apply to text maps. If a move ever shows before you have read about it, or the map does not change, tell DMC.
- Hovering over a token shows who it is.
- In a fight, a "Turn order" list beside the grid shows who acts when, numbered, with whose turn it is highlighted, so you can plan ahead.
- Every map is to scale. Fights use 5 ft squares, or 30 ft squares for very large places; overland maps use 1, 6 or 60 miles a square.
- The map shows only what your character has seen.
- The map is a local file, `adventures/<campaign>/maps/live.html`. Nothing is uploaded.

If you prefer text maps in the terminal, tell DMC. If you play away from the computer's screen (so you cannot keep the browser tab open), tell DMC that too: it sends you the picture, `live.png`, after its reply text instead. For that case the hook does not help (a hook cannot send a file), so the order depends on DMC writing the text first.

## Saving and breaks

DMC saves as it goes: every change to HP, resources, inventory, location or time is written before the next beat, every new fact goes into the canon file at once, and each scene is appended to the session log when it ends. Closing the terminal by accident costs at most the current exchange.

Type `/campaign-full-save` when you stop. It also rewrites the story summary, prunes the canon, and rereads the files to check they agree with each other. Run it too before Claude Code's `/compact` or `/clear`.

### Breaks

Every message you send re-reads the whole conversation, so a long conversation uses more of your usage limit with each message, and details blur as it grows. DMC therefore offers breaks.

Started with a launcher, DMC is told before each of your messages how large the conversation is. From about 150,000 tokens it offers a break at the next natural pause: the end of a scene, the end of a fight, or the top of a combat round with nothing waiting on you. Never while a roll or decision is pending.

- Say **yes**: DMC runs a full save and checks it, then tells you it is safe. Type `/clear` and then `/resume-campaign`. DMC restates where you are without a full recap; a fight resumes on the same round with everyone where they stood.
- Say **no**: DMC asks again when the conversation reaches the next band. The bands are fixed: 150,000 tokens, then every 40,000 after it (190,000, 230,000 and so on).

Claude Code may still compact a long conversation on its own. That loses nothing: state is saved as you play, and after a compaction DMC reloads from the files before narrating.

## Rewinds

To undo only the last exchange, just say so; DMC corrects the state and carries on.

To go further back, type `/rewind-campaign` (or ask to rewind). DMC finds the moment in the session logs, says it back in one line and waits for your confirmation. It then rewrites every campaign file to that moment: HP and resources, inventory, enemies, facts, maps and notes. The session log keeps one line saying a rewind happened. Your character has not seen what lay ahead, so DMC may change things the party had not yet discovered; anything established before the rewind point stays as it was.

## Player options

Two files say how you want the game run. Both are plain text; edit them by hand or just tell DMC.

| File | Holds |
|---|---|
| `adventures/player-options.md` | Your preferences for every campaign: who rolls, how rolls are shown, how you talk with DMC, narration style, maps. Created from `templates/player-options.md` the first time you play. |
| `adventures/<campaign>/player-options.md` | One campaign's settings: edition, starting level, leveling, lethality, combat difficulty, tone, house rules, and any overrides of the general file. |

The campaign file wins over the general file, which wins over DMC's defaults. When you ask for a change that could belong in either, DMC asks whether it is for this campaign or all of them.

Settings you can change:

| Setting | Choices (default first) |
|---|---|
| Lethality, per campaign | `balanced` (real danger, but bad luck alone will not end the campaign), `story` (no death without your agreement), `gritty` (the dice decide) |
| Combat difficulty, per campaign | `medium`, `easy`, `hard` |
| Edition, per campaign | 5e 2014, 5e 2024 |
| Who rolls | you roll for your character and party checks, or DMC rolls everything |
| How DMC's rolls are shown | dice, modifier, total and target; total only; narrated with no numbers |
| Talking in character | free; or `strict`, where long speech in a fight costs what the rules say |
| Maps | a picture in the browser (`browser`), ASCII in the terminal (`text`), or a picture sent to you (`remote`) |
| Also | DCs announced or not, secret rolls hidden or shown, suggested actions, rules explanations, recap length, narration length, content to avoid |

## Published modules

DMC can run a published adventure from your own copy.

1. Type `/ingest-module` and give the path to the file. PDF (including scanned books), Markdown, text, HTML and EPUB work. For content that only exists in an app or on a website, export or paste it into a file first.
2. For a PDF, DMC runs `dmc pdf`, which writes the text of every page and a picture of every page next to the file, so it can read books of any size and look at the maps and art.
3. DMC writes an index, a digest of each chapter and reference lists into `modules/<module>/`, with page references back to your copy. A long book takes a while and is best done in its own session; the first chapter or two are enough to start.
4. Type `/new-campaign` and say you want to play that module.

During play DMC runs from the digest and the original pages, keeps `module-progress.md` in the campaign folder (where you are, what is cleared, what you found, where you left the script), and scales encounters for a solo character. You are free to leave the script.

Password-protected PDFs cannot be read; print-restricted ones can. `modules/` is full of spoilers, and git ignores it because the books are copyrighted: do not share that folder.

## The rules reference

DMC knows the 5e rules, but reading them is more reliable than remembering them. To give DMC a local copy:

1. Download the free System Reference Document. The SRD 5.1 covers the 2014 rules: <https://media.wizards.com/2023/downloads/dnd/SRD_CC_v5.1.pdf>. Other versions, including the 5.2 SRD for the 2024 rules, are linked from <https://www.dndbeyond.com/srd>.
2. Save it in the `rules/` folder.
3. Type `/setup-rules`. DMC builds a page index once, checks five entries against it, and tells you which edition it covers.

A Markdown or text copy works too and is faster to search. The SRD lacks most subclasses and some spells and monsters; for those DMC falls back to a web lookup or a ruling, which it records so it is applied the same way next time.

## Reviewing your sessions

`/review-transcript` reads Claude Code's logs of a campaign's sessions and reports where DMC's instructions let the game down: context use, facts or numbers that drifted, badly sized fights, DMC acting for your character, slow stretches. It proposes changes to the toolkit and makes them only once you say which. It reads DMC's side of the game, so its report can contain spoilers. Nothing leaves your machine.

## Choosing a model

The launchers start DMC on Sonnet at `high` effort, a good default: fast and reliable at keeping the files up to date. Opus gives richer narration at higher usage. The launchers cannot be switched to it: once `dmc` is installed they hand over to `dmc play`, which always asks for Sonnet. To play on Opus, start Claude Code yourself in the toolkit folder, with the same effort and play settings:

```
claude --model opus --effort high --settings .claude/play-settings.json
```

This keeps the break reminder and the play settings. Run a launcher once first, or `bash tools/get-dmc.sh`, so that `bin/dmc` is there. Haiku is not recommended. Choose before your first message and keep it for the session: switching with `/model` partway sends the whole conversation to the new model again.

## Your files

Campaigns are ordinary text files under `adventures/`. You can read and edit your character sheet and the options by hand. To keep surprises, stay out of `story/dm-notes.md`, `story/dm-notes-archive.md`, `module-progress.md` and the NPC sheets. [campaign-files.md](campaign-files.md) describes every file.

Git ignores `adventures/`, `modules/` and `rules/`, so the toolkit can be shared without your saves or books. Back up `adventures/` separately, and keep the copy outside `adventures/`: DMC searches that folder, and an old copy can feed it stale facts.

## Troubleshooting

| What you see | What it means and what to do |
|---|---|
| PowerShell: running scripts is disabled on this system | The execution policy blocks local scripts. Start with `powershell -ExecutionPolicy Bypass -File .\play.ps1`. |
| `play: dmc is not available, so Claude Code starts without it, ...` | `get-dmc` could not install `dmc`; the line above it says why. Fix the cause, then run `bash tools/get-dmc.sh` (in PowerShell, `powershell -ExecutionPolicy Bypass -File tools/get-dmc.ps1`), or ask DMC to. |
| DMC says `bin/dmc` is missing or does not run | The start-of-session check found no working `dmc`. Let DMC run `get-dmc` when it asks; it is on the allow list. |
| `dmc: this is dmc X, but the toolkit pins dmc-vY: run ...` | The toolkit was updated (for example by `git pull`) but `bin/dmc` was not. Run the command the line names, or start with a launcher, which updates it. |
| `get-dmc: could not download ...` | No network, or GitHub could not be reached. Play still starts without `dmc`; try again later. |
| `get-dmc: ... does not match its SHA-256 in tools/dmc-release.json, so nothing was installed` | The download was not the file the toolkit expects. Nothing was installed. Try again; if it keeps happening, update the toolkit folder. |
| `get-dmc: the dmc of dmc-vX does not run on this computer, ...` | The program downloaded but cannot run here, for example on a Linux older than glibc 2.34. |
| `get-dmc: dmc is not built for Intel Macs: ...` or `... not built for <system>` | This system is not supported. Play works without `dmc`, but with no dice roller, maps or PDF reading. |
| `get-dmc: no dmc release is pinned yet ...` | The toolkit folder is from before the first release. Update it. |
| `dmc play: Claude Code (claude) is not on the PATH: ...` | Install Claude Code, or open a new terminal after installing it. |
| `dmc play: only ...claude.cmd was found, ...` (Windows) | Only npm's command shim was found, without its PowerShell script. Install Claude Code's own `claude.exe`, or start with `./play.ps1`. |
| The map tab did not open, or stopped updating | Open `adventures/<campaign>/maps/live.html` in your browser yourself. It refreshes itself while DMC redraws. |
| `This PDF is password-protected. ...` | Give DMC an unlocked copy. |
| `This PDF is encrypted, and reading it needs OpenSSL (libssl), ...` (Linux) | Install your distribution's OpenSSL library. |
| DMC got a fact wrong | Tell it. It checks the files before agreeing, and corrects course. |

[dmc-reference.md](dmc-reference.md) lists every message `dmc` can print by command.
