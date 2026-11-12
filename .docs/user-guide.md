# User guide

This guide is for the player: how to start DMC, play, save, take breaks and fix the problems that can come up. It assumes no programming knowledge.

## Getting started

### What you need

- [Claude Code](https://claude.com/claude-code), installed and signed in.
- `dmc`, the toolkit's helper program. It rolls DMC's dice, draws maps, reads PDFs, reminds DMC to take breaks, and writes and updates your **DMC folder**, the folder you play in. `dmc install` puts it on your computer and writes the folder.
- One of these systems:

| System | Supported |
|---|---|
| Windows | x64 or Arm |
| Linux | x64 or Arm, with glibc 2.34 or later (Ubuntu 22.04 or newer) |

macOS is not supported: `dmc` is no longer built for it.

Install `dmc` with one line, in a terminal:

```
Windows (PowerShell):  irm https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.ps1 | iex
Linux:                 curl -fsSL https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.sh | sh
```

Nothing else needs installing. To build `dmc` from source instead, see [development.md](development.md); it needs the .NET 10 SDK.

### Installing

The install line downloads the latest release of `dmc` and runs `dmc install` for you; it checks the download against the checksums the release lists and against the digest GitHub reports for it before it unpacks anything (if GitHub's limit for requests without sign-in has been reached, it checks the list alone and says so in one line). You can give the folder in the line: in PowerShell `& ([scriptblock]::Create((irm https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.ps1))) -Path D:\Games\dmc`, on Linux `curl -fsSL https://github.com/abaskett3/dungeon-master-claude/releases/latest/download/install.sh | sh -s -- --path ~/games/dmc`. On Windows, use the PowerShell line (`install.sh` refuses there). You can also run `dmc install` yourself (from a local build: `artifacts/bin/dmc install --force`). It asks where to put your DMC folder:

```
DMC folder [C:\Users\you\dmc]:
```

Press Enter for the default, or type a folder (`~` means your home folder). `dmc install --path <folder>` answers for you. It then:

1. copies `dmc` to your user folder and makes it reachable by name (Windows: `%LOCALAPPDATA%\Programs\dmc`, added to your `Path`; Linux: `~/.local/share/dmc`, linked as `~/.local/bin/dmc`). Open a new terminal afterwards. A development build is copied only with `--force`;
2. writes the DMC folder: `CLAUDE.md` (DMC's rules), `.claude/` (its settings and skills), `templates/`, and `.dmc/`, which records which files `dmc` wrote;
3. makes empty `adventures/`, `feedback/`, `modules/` and `rules/` folders, if you have none. It writes nothing in them.

`dmc install` refuses a folder inside the toolkit's source repository, and a folder where a file it would write already exists (it names the file: move it, or choose another folder). It refuses to run inside a Claude Code session; run it in a terminal.

### Updating

Run `dmc update` in a terminal. It replaces `dmc` with the latest release, then rewrites the rules, skills, settings and templates of every DMC folder you installed: the same refresh `dmc play` does, with the same promise (nothing of yours is touched; a file you edited by hand is kept under `.dmc/replaced/`). It installs a release only if the release's checksum list carries a valid signature from the DMC release key built into your `dmc` and the download matches that list and GitHub's own digest; otherwise it changes nothing and says why in one line. It says `dmc X.Y.Z is already the latest` when there is nothing newer, and it refuses inside a Claude Code session and for a development build.

Changes made to the toolkit's repository never reach you before a release, and you choose when to update: `dmc play` refreshes your folder from the `dmc` you have, and never downloads anything.

An update can change what DMC keeps in a campaign's files. When it does, `dmc update` (or `dmc refresh`) says how many of your campaigns are behind the new rules, and the next time you resume one DMC offers, in one line, to bring its files up to date. See [Bringing a campaign up to date](#bringing-a-campaign-up-to-date).

### Starting a session

Run `dmc play` in a terminal, from any folder. It does these things:

1. If your DMC folder's rules, skills, settings or templates are not what this `dmc` writes (after you installed a newer `dmc`, say, or if a file was deleted or changed), it rewrites them first and prints a line saying how many. A file you edited by hand is put back, and your edited copy is kept under `.dmc/replaced/<date and time>/`. It never touches your adventures, feedback, modules, rules or exports.
2. It starts Claude Code in the DMC folder on Sonnet at `high` effort. The folder's settings (`.claude/settings.json`) switch off connectors and tools a game does not use, block shell deletes (`rm`, `Remove-Item` and their aliases), stop DMC editing its own rules, add the break reminder and redraw the map after each reply.
3. Anything you type after `dmc play` goes on to Claude Code, so `dmc play --continue` works.

`dmc play` plays in the current folder if it is a DMC folder, and otherwise in the first one `dmc install` recorded. `dmc folder` prints the folder it would use.

DMC never deletes or moves your campaign files unless you ask for that file to go. When you do, it cannot run the delete or move itself in a play session (`rm`, `mv`, `Remove-Item`, `Move-Item`, `git clean` and `find -delete` are blocked): it tells you the command to run in your own terminal. The block is not complete (see [decisions.md](decisions.md)), so the rule in `CLAUDE.md` is the first line of defence. DMC also cannot edit `CLAUDE.md`, `.claude/`, `templates/` or `.dmc/`, or run `dmc install`, `dmc refresh` or `dmc update`.

Starting plain `claude` in the DMC folder also works: the same rules, skills and hooks load. It starts on your account's default model, and `dmc` must be on your `PATH`. If `dmc` is missing or does not run, Claude Code shows DMC a one-line notice at the start of the session and DMC tells you to reinstall.

### Your first campaign

Type `/dmc-new-campaign`. DMC then:

1. asks for a title (or offers a few) and creates the campaign folder under `adventures/`;
2. asks whether this is an original adventure or a published module (see [Published modules](#published-modules));
3. runs a short session zero: how you like dice handled, what you enjoy, narration length, the rules edition (2014 by default, or 2024), starting level, tone and lethality, and, for a sequel, which earlier campaign it continues (DMC then checks that campaign's files for facts from it);
4. builds your character with you, following the edition's rules. You make every choice; DMC explains options and does the arithmetic;
5. writes its own notes for the world behind the curtain, and opens the first scene.

To carry on a campaign you played somewhere else, say so and give DMC the chat export or transcript. It imports it into a new campaign folder in pieces, without reading the whole export into the conversation, and asks you about anything the export contradicts.

### Resuming

Start DMC the same way (`dmc play`) and type `/dmc-resume-campaign`. DMC reads the campaign files, checks that they agree with each other, and gives a short recap: where you are, what just happened, your condition and open quests. To keep the resume small it reads your character's sheet, the party's and those of anyone in the scene (no other sheets), only the last few scenes of the latest session log (the whole log if the last session ended without a full save), and, once the campaign's canon file is long, only its rulings plus what a search for this scene's names and places finds; the full save archives what can no longer come up, which keeps canon short where it can. To switch campaigns, add the campaign's name: `/dmc-resume-campaign <name>`.

If the files disagree (HP on the sheet differs from the game state, say), DMC tells you what it found, proposes the likely truth and fixes the files once you confirm.

A campaign you have closed (see [Closing a campaign](#closing-a-campaign)) is not resumed: DMC says it is closed, opens no scene, and offers to start a sequel.

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
- In a fight DMC changes `maps/live.json` and finishes its reply, and the map redraws after the reply (the first map of a fight is opened in your browser the same way, after the text), so the picture normally changes only after the text that describes it. This is done by a hook in the play settings (`dmc redraw`), so it needs a session in your DMC folder (`dmc play`) and a `dmc` of release 0.1.4 or later, and it does not apply to text maps. If a move ever shows before you have read about it, or the map does not change, tell DMC.
- Hovering over a token shows who it is.
- In a fight, a "Turn order" list beside the grid shows who acts when, numbered, with whose turn it is highlighted, so you can plan ahead.
- Every map is to scale. Fights use 5 ft squares, or 30 ft squares for very large places; overland maps use 1, 6 or 60 miles a square.
- The map shows only what your character has seen.
- The map is a local file, `adventures/<campaign>/maps/live.html`. Nothing is uploaded.

If you prefer text maps in the terminal, tell DMC. If you play away from the computer's screen (so you cannot keep the browser tab open), tell DMC that too: it sends you the picture, `live.png`, before its reply text instead. For that case the hook does not help (a hook cannot send a file), so the order depends on DMC writing the text first.

### Image prompts

DMC can write a prompt for an image-generation model of a moment in your game, for you to paste into the image model you use (DMC makes no pictures itself). Type `/dmc-generate-image-prompt`, or just ask: "give me an image prompt of this", "an image prompt of the reunion at the Landing", "Shale and Kibil at the gate, close up".

- **What is in it.** Unless you say otherwise, the current scene: everyone present, in a wide shot. A past moment you name is taken from its session log. Everyone is described from their sheet: species, looks, gear and injuries, with each figure's size compared with someone else in the picture ("chest-high to the humans"), so sizes stay the same from picture to picture, and animals are named by kind.
- **No spoilers.** A prompt shows only what your character has perceived: no enemy you have not spotted (not even a shadow or eyes), no disguised being's true form, no secret and nothing from the module ahead. If you ask for something your character has not seen, DMC says so in a line and leaves it out.
- **Nothing made up.** A detail the files do not hold (a hair colour, say) is left out, and DMC names it after the prompt. Tell DMC the detail and it goes on that character's sheet, so every later picture has it.
- **Nothing changes in the game.** No time passes and no roll is made.

**Styles.** Each prompt is labelled with its style.

- **The default.** With no style saved, DMC uses its built-in Storybook style, a high-fantasy storybook illustration. Once you have saved styles, a prompt that names none gets the saved style you used last.
- **Saving and switching.** Describe any style in the request ("as a charcoal sketch") and that is used, unsaved. When you like a look, say "save that as Woodcut"; it is kept in `adventures/player-options.md` under Image styles, for every campaign. Switch with "in Woodcut". Ask DMC to list, rename, change or delete your styles at any time.
- **Varying.** "The same style, but more abstract" changes that picture only; DMC tells you the saved style is unchanged and saves the change into it if you say so.
- **Surprise me.** Ask for something different without describing it, and DMC picks a style unlike the ones you have just seen, marked unsaved, for you to save if you like it.

## Saving and breaks

DMC saves as it goes: every change to HP, resources, inventory, location or time is written before the next beat, every new fact goes into the canon file at once, and each scene is appended to the session log when it ends. Closing the terminal by accident costs at most the current exchange.

Type `/dmc-campaign-full-save` when you stop. It also rewrites the story summary, prunes the canon, and rereads the files to check they agree with each other. Run it too before Claude Code's `/compact` or `/clear`.

### Breaks

Every message you send re-reads the whole conversation, so a long conversation uses more of your usage limit with each message, and details blur as it grows. DMC therefore offers breaks.

In a DMC folder, DMC is told before each of your messages how large the conversation is. When it is large, DMC offers a break at the next natural pause: the end of a scene, the end of a fight, or the top of a combat round with nothing waiting on you. Never while a roll or decision is pending.

"Large" is measured from where the conversation settled after DMC loaded your campaign. The first offer comes when the conversation has grown by 40,000 tokens beyond that point, and never before 150,000 tokens in all. So a campaign that loads a lot is not offered a break one round in.

- Say **yes**: DMC runs a full save and checks it, then tells you it is safe. Type `/clear` and then `/dmc-resume-campaign`. DMC restates where you are without a full recap; a fight resumes on the same round with everyone where they stood.
- Say **no**: DMC asks again when the conversation has grown by another 40,000 tokens.

A fresh session starts smaller than it used to, because the resume reads less (see Resuming), so a break gives back more room.

Claude Code may still compact a long conversation on its own. That loses nothing: state is saved as you play, and after a compaction DMC reloads from the files before narrating.

## Rewinds

To undo only the last exchange, just say so; DMC corrects the state and carries on.

To go further back, type `/dmc-rewind-campaign` (or ask to rewind). DMC finds the moment in the session logs, says it back in one line and waits for your confirmation. It then rewrites every campaign file to that moment: HP and resources, inventory, enemies, facts, maps and notes. The session log keeps one line saying a rewind happened. Your character has not seen what lay ahead, so DMC may change things the party had not yet discovered; anything established before the rewind point stays as it was.

A rewind can leave files behind: maps of places the party has now never seen. DMC does not delete files, so it lists them, asks you to confirm they should go, and gives you the command to run yourself. Until you have, it notes them as "do not load or draw" and does not use them.

## Player options

Two files say how you want the game run. Both are plain text; edit them by hand or just tell DMC.

| File | Holds |
|---|---|
| `adventures/player-options.md` | Your preferences for every campaign: who rolls, how rolls are shown, how you talk with DMC, narration style, maps, and your saved image styles. Created from `templates/player-options.md` in your DMC folder the first time you play. |
| `adventures/<campaign>/player-options.md` | One campaign's settings: edition, starting level, leveling, lethality, combat difficulty, tone, house rules, the earlier campaign of a sequel, and any overrides of the general file. |

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

1. Type `/dmc-ingest-module` and give the path to the file. PDF (including scanned books), Markdown, text, HTML and EPUB work. For content that only exists in an app or on a website, export or paste it into a file first.
2. For a PDF, DMC runs `dmc pdf`, which writes the text of every page and a picture of every page next to the file, so it can read books of any size and look at the maps and art.
3. DMC writes an index, a digest of each chapter and reference lists into `modules/<module>/`, with page references back to your copy. A long book takes a while and is best done in its own session; the first chapter or two are enough to start.
4. Type `/dmc-new-campaign` and say you want to play that module.

During play DMC runs from the digest and the original pages, keeps `module-progress.md` in the campaign folder (where you are, what is cleared, what you found, where you left the script), and scales encounters for a solo character. You are free to leave the script.

Password-protected PDFs cannot be read; print-restricted ones can. `modules/` is full of spoilers, and git ignores it because the books are copyrighted: do not share that folder.

## The rules reference

DMC knows the 5e rules, but reading them is more reliable than remembering them. To give DMC a local copy:

1. Download the free System Reference Document. The SRD 5.1 covers the 2014 rules: <https://media.wizards.com/2023/downloads/dnd/SRD_CC_v5.1.pdf>. Other versions, including the 5.2 SRD for the 2024 rules, are linked from <https://www.dndbeyond.com/srd>.
2. Save it in the `rules/` folder.
3. Type `/dmc-setup-rules`. DMC builds a page index once, checks five entries against it, and tells you which edition it covers.

A Markdown or text copy works too and is faster to search. The SRD lacks most subclasses and some spells and monsters; for those DMC falls back to a web lookup or a ruling, which it records so it is applied the same way next time.

## Giving feedback about DMC

There are two kinds of input, and they go to different places.

- **Player options**: a preference about your own game that you want changed ("show my HP all the time", "shorter replies", "harder fights"). Tell DMC and it writes a one-line preference in the campaign's `player-options.md`, or in `adventures/player-options.md` if it is for every campaign (it asks which if it is unclear).
- **Feedback**: something is wrong with *DMC* and it should change. A mistake, a fact or number that drifted, too much context used, a fight run against the rules, a quest or a conversation that never ends. The test: would the same behaviour have been wrong for a different player with different preferences? If yes, it is DMC feedback.

To give DMC feedback, say it in play, in your own words or after "feedback:". DMC records it with the `dmc-record-feedback` skill as one file in `feedback/`, `feedback/YYYY-MM-DD-<slug>.md`, with the version of `dmc` in its `dmc:` key, and answers with one line out of character. Nothing is written into the campaign, and DMC does not change the toolkit mid-game. If you raise the same problem again, DMC adds a line to the same file instead of starting a new one, which also shows how much it matters.

Entries quote you and can contain spoilers, so `feedback/` is not in git. Each one makes sense without the adventure.

To act on the entries, work on the toolkit (not in a game): start Claude Code in a clone of the toolkit's repository and ask for `/review-feedback`, naming your DMC folder (by default it uses the one `dmc folder` prints). It lists the open entries, checks whether each is already fixed, finds the cause in `kit/` (the source of the rules, skills, settings and templates `dmc` writes into a DMC folder), and reports. Players get a change through a newer `dmc`. It files issues or makes changes only once you say which entries to act on, and marks each entry `filed #N` or `fixed in PR #N`; it never deletes an entry.

## Reviewing your sessions

`/review-transcript` (also run from a clone of the toolkit's repository, on your DMC folder) reads Claude Code's logs of a campaign's sessions and looks for where DMC let the game down: context use, facts or numbers that drifted, badly sized fights, DMC acting for your character, slow stretches. It records each problem as an entry in `feedback/`, new or added to an existing one, the same as feedback you give in play, and changes nothing else. Acting on the entries is `/review-feedback`. It reads DMC's side of the game, so entries can contain spoilers. Nothing leaves your machine.

## Choosing a model

`dmc play` starts DMC on Sonnet at `high` effort, a good default: fast and reliable at keeping the files up to date. Opus gives richer narration at higher usage, and `dmc play` always asks for Sonnet. To play on Opus, start Claude Code yourself in your DMC folder with the same effort:

```
claude --model opus --effort high
```

The rules, skills, settings and break reminder load because the folder is a Claude Code project. `dmc` must be on your `PATH` (it is, after `dmc install` and a new terminal). Haiku is not recommended. Choose before your first message and keep it for the session: switching with `/model` partway sends the whole conversation to the new model again.

## Your files

Your DMC folder holds two kinds of file, and `dmc` keeps them apart:

| Path | Whose | What happens to it |
|---|---|---|
| `CLAUDE.md`, `.claude/settings.json`, `.claude/skills/dmc-*/`, `templates/` | `dmc` | Written by `dmc install`, `dmc refresh` and `dmc play`. Do not edit them: a refresh puts them back and keeps your edit under `.dmc/replaced/`. |
| `.dmc/manifest.json` | `dmc` | The list of files `dmc` wrote, with their hashes. It is what makes the folder a DMC folder. |
| `adventures/`, `feedback/`, `modules/`, `rules/`, `exports/` | you | Never written, replaced or removed by `dmc install`, `dmc refresh` or `dmc play`. DMC writes `adventures/` and `feedback/` during play, as you ask. |
| anything else (your own `.claude/settings.local.json`, your own skill in `.claude/skills/`, notes) | you | Never touched. |

Campaigns are ordinary text files under `adventures/`. You can read and edit your character sheet and the options by hand. To keep surprises, stay out of `story/dm-notes.md`, `story/dm-notes-archive.md`, `module-progress.md` and the NPC sheets. [campaign-files.md](campaign-files.md) describes every file.

**How big your campaign's files are.** The longer a campaign runs, the more DMC reads when a session starts, and past a point it starts losing track. `dmc size` shows what a resume reads, file by file, in estimated tokens, the largest sections, and anything to trim (a file close to what DMC can read in one go, a file over its budget, leftovers such as finished fights). Run it in a terminal in your DMC folder (`dmc size`, or `dmc size <campaign>`), or ask DMC "are your files too big?". It changes nothing: the next full save trims what it flags, and says what it kept and why. [campaign-files.md](campaign-files.md#how-big-the-files-may-be) lists the budgets.

**Finding a fact in your files.** When a character, place or promise from long ago comes back, DMC recalls it with `dmc recall`, which searches the campaign, the campaign it continues and their modules and returns only the best-matching lines; for a module's area, a stat block or a rules entry it uses `dmc lookup`, which prints that one section. You need do nothing. Both only read, and if either fails DMC searches the files itself, so the game goes on. You can run them in a terminal too: `dmc recall <campaign> <words>`, `dmc lookup <campaign, module or rules> <heading>`.

Feedback about DMC is under `feedback/` in the DMC folder, outside every campaign. To back up a campaign, restore one or move to another computer, see [Back up, copy or move a campaign](#back-up-copy-or-move-a-campaign). Keep a backup outside `adventures/`: DMC searches that folder, and an old copy can feed it stale facts.

### Back up, copy or move a campaign

`dmc campaign` puts a whole campaign into one file and brings it back:

```
dmc campaign export the-silver-egg
dmc campaign show exports/the-silver-egg-202610220605.zip
dmc campaign import exports/the-silver-egg-202610220605.zip --as silver-egg-copy
```

- **Back up.** `export` writes `exports/<campaign>-<date and time>.zip` in the DMC folder and changes nothing in `adventures/`. Keep a copy somewhere else too. A second export in the same minute is numbered `-2`.
- **Backups are left out.** A campaign's own `backups/` folder (below) is never put in an export. An export made by an older `dmc` may hold one; `import` leaves it out and says so.
- **Restore, or make a copy.** `import` creates `adventures/<name>` from an export. It never replaces a campaign: if the name is taken, it says so, and you choose another with `--as`. It checks every file against the checksums in the file first, so a damaged export is refused and nothing is left half done. It never touches your general `adventures/player-options.md` (it writes one only if you have none) and never changes which campaign is active: start a session and ask DMC to resume the new name.
- **Move to another computer.** Copy the `.zip` across (the file name does not matter), install `dmc` and a DMC folder there, and import it. If the export was made before an update, `import` says the campaign is behind and DMC offers to bring it up to date when you resume it; if it was made by a newer `dmc`, update `dmc` first. A published module is not in the export, because it is your own copy of a book: if the campaign runs one, `import` and `show` say so, and you ingest it again from your PDF with `/dmc-ingest-module`.
- **Look first.** `show` says what a file holds (campaign, date, files, size, module) and writes nothing.
- **Run it inside your DMC folder.** `dmc campaign` finds `adventures/` in the DMC folder (the current folder if it is one, else the first recorded), and fails with `no DMC folder: run dmc install` when there is none.

An export holds everything in the campaign, including `story/dm-notes.md`, so do not open it before you have played the story, and do not send it to anyone who will play it. To bring in a campaign played elsewhere (a chat export or transcript), use `/dmc-new-campaign` instead. Every message and option is in [dmc-reference.md](dmc-reference.md#campaign).

### Bringing a campaign up to date

When an update changes what DMC keeps in a campaign's files (for example, finished fights now leave the DM notes for an
archive), an older campaign keeps its old shape until it is brought up to date. Resume tells you in one line when a
campaign is behind; say yes, or ask DMC at any time to "bring my campaign files up to date". You can say no: nothing
changes, and the offer comes back next time. DMC does not start in the middle of a fight or with a roll pending.

What happens, in order:

1. **DMC tells you what will change**, including anything it may remove and why (a session-by-session log that one line
   replaces, say), and waits for your yes.
2. **A backup is taken**: the whole campaign, checked, in `adventures/<campaign>/backups/`. When you already keep three,
   DMC asks whether the oldest may go (or, with more, all but the latest two); a no keeps them all.
3. **DMC rewrites the files**, following the steps for each change in order, and asks you about anything it is unsure of
   (whether a fight is really over, say). A file it does not recognise, because you edited it by hand, is left as it is
   and named to you.
4. **`dmc` checks that nothing was lost.** Every line of the backup must be in the new files, or be one DMC says it
   reworded (and the new wording must be there), or be one it removed under a heading of what it told you it may
   remove; and each change's new shape must be in place. If anything is unaccounted for, the campaign is not marked up
   to date, the backup is untouched, and DMC offers to put the files back.
5. **When it passes**, DMC tells you where the backup is, and mentions anything the next full save will trim.

What the check proves: no line was lost verbatim or without a stated reason. It cannot prove a reworded line kept its
meaning; that is why removals are limited to what you were told and doubtful content is kept and shown to you.

**Going back.** `dmc campaign restore <campaign>` shows what would change (and warns if you have played since the
migration, because that play would be lost); `dmc campaign restore <campaign> --yes` puts the files back as the backup
holds them, keeping the backups. DMC can run it for you.

**If a session ends part way** through a migration, the next resume says a migration was interrupted, and play waits
until DMC finishes it (against the same backup) or you restore.

### Closing a campaign

When a story is over, you can close its campaign for good: ask DMC to "close the campaign". DMC may suggest it once
when the main story ends, and once more when the last open thread is resolved; it never closes a campaign on its own.

1. **DMC checks it can be closed.** A campaign whose files are behind the current rules is brought up to date first.
2. **DMC asks once.** Closing is permanent: the campaign can never be played or changed again, and the only way back
   to its story is a sequel. Anything but a clear yes closes nothing.
3. **A last full save** that keeps everything a sequel will need: how the story ended and where everyone is, what
   became of every secret (revealed, never revealed or abandoned), and every loose end written down as a fact.
4. **`dmc` closes it**: it marks the campaign closed and writes a checked closing backup in
   `adventures/<campaign>/backups/` (never removed when old backups are cleared) and a copy of it in `exports/`. If any
   step fails, nothing is left from it and the campaign is still open; ask again.

Keep a further copy of the file in `exports/` somewhere outside the DMC folder. A closed campaign still opens for
reading (`dmc recall`, `dmc lookup`, `dmc campaign show` and `export`), but nothing changes it; `dmc campaign verify
<campaign>` says whether any file differs from its closing backup. To go on with its characters, start a new campaign
with `/dmc-new-campaign` and name the closed one as the one it continues: DMC searches the closed campaign's facts when
they come up, and never writes to it.

### Moving campaigns from a repository checkout

Before DMC had a DMC folder, you played in a checkout of the toolkit's repository, and your campaigns, feedback, modules and rules files were in its `adventures/`, `feedback/`, `modules/` and `rules/` folders (git ignored them). To play from a DMC folder instead, move them across. **DMC never runs these commands: you run them yourself, in your own terminal.**

1. **Back up first.** Copy those of the four folders the checkout has somewhere safe, outside the checkout. Check the copy is there before you go on.
2. **Install.** Run `dmc install` (or `dmc install --path <folder>`) and note the folder it prints.
3. **Move the files.** Set the two folders at the top and run the commands for your shell. They move everything in `adventures/`, `modules/` and `rules/`, and everything in `feedback/`. They leave behind any `.gitkeep` or `.ignore` placeholder an older checkout still has, and skip a folder the checkout does not have.

   PowerShell:

   ```powershell
   $checkout = "D:\Claude\dungeon-master-claude"   # your old checkout
   $dmc = "$HOME\dmc"                               # the DMC folder dmc install printed
   foreach ($name in "adventures", "feedback", "modules", "rules") {
       if (Test-Path "$checkout\$name") {
           Get-ChildItem -Force "$checkout\$name" | Where-Object { $_.Name -notin ".gitkeep", ".ignore" } | Move-Item -Destination "$dmc\$name"
       }
   }
   ```

   bash (Linux, Git Bash):

   ```bash
   checkout=~/dungeon-master-claude     # your old checkout
   dmc=~/dmc                            # the DMC folder dmc install printed
   for name in adventures feedback modules rules; do
     [ -d "$checkout/$name" ] || continue
     find "$checkout/$name" -mindepth 1 -maxdepth 1 ! -name .gitkeep ! -name .ignore -exec mv {} "$dmc/$name/" \;
   done
   ```

   If you also have a `exports/` folder in the checkout, move it the same way.
4. **Play.** Run `dmc play` and type `/dmc-resume-campaign`. The recap should match what you remember. If anything is missing, the backup from step 1 still has it.

Your old session logs stay where Claude Code put them: under the checkout's Claude Code project folder (`~/.claude/projects/<the checkout's path with every other character turned into "-">`), not the DMC folder's. They are not lost: `dmc digest list --logs <that folder>` reads them, and `/review-transcript` works on them the same way.

## Troubleshooting

| What you see | What it means and what to do |
|---|---|
| `dmc: unknown command "install"` (or `folder`, `refresh`) | Your `dmc` is older than the one that writes DMC folders. Install a newer one (see [Installing](#installing)). |
| `dmc <command>: no DMC folder: run dmc install` | `dmc` found no DMC folder: you are not in one, and none was recorded. Run `dmc install`, or `cd` into your DMC folder. |
| `dmc install: cannot run inside a Claude Code session: run it in a terminal` (also `refresh`) | These commands change your DMC folder, so they refuse to run from inside a game. Open a terminal and run them there. |
| `dmc install: <file> already exists and was not written by dmc: move it, or choose another folder` | The folder you chose already holds a file `dmc` would write, and it did not write it. Move the file, or choose another folder. Nothing was written. |
| `dmc install: <folder> is inside the DMC source repository: choose another folder` | You chose a folder in a checkout of the toolkit's repository. Choose another. |
| `dmc install: could not add <folder> to your PATH: ...` | The DMC folder was written, but your `Path` could not be changed. Add the folder shown to your `Path` yourself. |
| `dmc refresh: <folder>: <n> edited files kept in .dmc/replaced/<stamp>/` | You (or a tool) had changed a file `dmc` writes. It was put back, and your copy is in the folder named. Move anything you want to keep into a file of your own. |
| `dmc: unknown command "campaign" (try roll, ...)` | Your `dmc` is older than 0.1.4, which added `campaign`. Install a newer one. |
| DMC says `dmc` is missing or does not run | The start-of-session check found no working `dmc` on the `PATH`. Reinstall: run `dmc install` again, or the install line from the [README](../README.md) (at <https://github.com/abaskett3/dungeon-master-claude>). Without `dmc`, DMC cannot roll its own dice, draw maps or read PDFs. |
| `dmc play: Claude Code (claude) is not on the PATH: ...` | Install Claude Code, or open a new terminal after installing it. `dmc play` exits with code 127 in this case. |
| `dmc play: only ...claude.cmd was found, ...` (Windows) | Only npm's command shim was found, without its PowerShell script. Install Claude Code's own `claude.exe`. |
| The map tab did not open, or stopped updating | Open `adventures/<campaign>/maps/live.html` in your browser yourself. It refreshes itself while DMC redraws. |
| `This PDF is password-protected. ...` | Give DMC an unlocked copy. |
| `This PDF is encrypted, and reading it needs OpenSSL (libssl), ...` (Linux) | Install your distribution's OpenSSL library. |
| DMC got a fact wrong | Tell it. It checks the files before agreeing, and corrects course. |

[dmc-reference.md](dmc-reference.md) lists every message `dmc` can print by command.
