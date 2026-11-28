# Research: Who calls whom what, and limits, held by `dmc` (spec 032)

Each decision cites the code or test it rests on (constitution Development 4). "Unverified" marks what is not yet
measured.

## R1. Where the records live, and their shape

- **Decision**: one file per person, `adventures/<campaign>/records/<slug>.json`: `{ "person", "of"?, "rows", "limits" }`.
  A row is `{ "kind", "from", "word", "when"?, "never"?, "source" }`; the record's person is the row's "to". `of`
  names the linked earlier campaign a person belongs to (FR clarification: "Mother" lives in the later campaign).
- **Rationale**: it is the shape the write-path test ran (#263, arm B: 0 wrong rows kept in 3 runs). A folder of its
  own, not beside the sheets, because persons with no sheet get records too (Vaelorn, Miira, and the hag of the
  earlier campaign). One file per person keeps each Read small, the arm B cost driver (about 10 Reads a run).
- **Alternatives**: one `rows` file for the campaign (rejected: every edit reads every row); records inside the
  sheets as fenced JSON (rejected: the check after an edit would parse Markdown, and the sheet budget would count it);
  separate `called` and `relationships` lists (rejected: not the tested shape; the text view groups them instead).

## R2. Sources, and how a scene is checked

- **Decision**: a source is `sN` or `sN.M` (this campaign), `<campaign> sN[.M]` (a linked earlier campaign, read only),
  `player: "…"`, `setup` or `inference`. A scene exists when `story/sessions/session-NNN.md` exists and, for `.M`,
  holds at least M `### Scene` headings.
- **Rationale**: the session log template's headings are `### Scene N: …` (`kit/templates/session-log.md`); log files
  are `session-001.md` (the long campaign's `story/sessions/`). Scene ids in this form are already parsed elsewhere
  (`src/Dmc.Cli/Campaign/MigrationChecks.cs:81`, `src/Dmc.Cli/Retrieval/LineUnits.cs:77`).
- **Alternatives**: free-text sources (rejected: FR-003 needs a check); scene titles (rejected: titles change).

## R3. The check after each edit (FR-006)

- **Decision**: a `PostToolUse` hook in `kit/settings.json`, matcher `Edit|Write|MultiEdit`, running
  `dmc records edited`. It reads the hook's JSON (`src/Dmc.Cli/HookInput.cs`) and:
  1. for a file under the active campaign's `records/`, checks every record (R1, R2 and data-model.md § Rules) and on
     a bad one prints one line on standard error and exits 2;
  2. for any new `player: "…"` source in the edit, looks for the quotation in the player's typed messages of the
     session log (`transcript_path`; `src/Dmc.Cli/SessionLog/UserText.cs:31`), and exits 2 when it is not there;
  3. for an Edit of a campaign Markdown file whose new text both changes a line and adds a credit to the player
     ("the player", "player's words", "by the player"), exits 2 (FR-007 class 4, at the edit);
  4. for a new usage that makes one word mean two persons to one speaker, prints the note as `additionalContext`
     and exits 0.

  Any other failure prints nothing and exits 0 (`NeverFails`, as `dmc redraw`: `src/Dmc.Cli/Commands/RedrawCommand.cs:49`, `:80`).
- **Rationale**: the write-path test showed Claude Code records an exit 2 from a `PostToolUse` hook as
  `hook_blocking_error` and DMC fixed or undid the file before replying (#263, pilot 2). The map's check runs at
  `Stop` (`kit/settings.json`, Stop hook); after the edit is earlier, before the reply.
- **Alternatives**: `dmc` commands only (arm A, rejected by the test); `PreToolUse` that simulates the edit (rejected:
  it must apply `old_string`/`new_string` itself and Write's content, a second editor to keep right).

## R4. The index (FR-004)

- **Decision**: built in memory on each run from `records/*.json` of the active campaign and of every member of its
  linked set with `Before` (the campaigns it continues: `src/Dmc.Cli/Campaign/CampaignLinks.cs:154`, `LinkedCampaign.Before`)
  that has `records/`. Two views: by word (lower case), and by speaker. A word is matched whole, ignoring case, longest
  first, so "Auntie Em" wins over "Auntie" (`Register.cs:134` matches the same way).
- **Rationale**: no stored index to go stale; the records are small. No title or kin list: `Register.cs:88` drops a
  bare title by `ReadSet.IsCommonNameWord` (`src/Dmc.Cli/Size/ReadSet.cs:108`), which is the "Auntie" probe's failure.
- **Alternatives**: a cached index file (rejected: one more file to keep right; unverified need).
- **Unverified**: the index's run time on the long campaign. `dmc recall` over two campaigns takes 0.64 s (#238); the
  records are a few kilobytes, so far less is expected. Measured in the build (quickstart.md).

## R5. The hook's lines (FR-005, the clarification on timing)

- **Decision**: a new part of `dmc context-check`, `RowsReminder` (`src/Dmc.Cli/Retrieval/`), printed after the canon
  reminder (`src/Dmc.Cli/Commands/ContextCheckCommand.cs:68`):
  - **Words**: every row whose word is in the player's message, grouped by word, each time.
  - **Persons**: for each person present (the Present line and Combat tracker, `ReadSet.PresentText`,
    `ReadSet.cs:281`, matched to records by their name rows) or named (a word whose rows all mean one person), once per
    scene: the rows between the persons present, that person's words for them, and their limits.
  - **Scene**: a short hash of the Present text. The header carries it; a person is due when no header since the log's
    last compaction carries this hash and their name, which is how `SheetReminder` and `CanonReminder` find what they
    printed (`SheetReminder.cs:199`, `CanonReminder.cs:141`).
  - A cap of 2,400 bytes on the part; past it, persons are cut before words. Amended after SC-003's run (2026-11-27,
    T055): every person's "says" line prints first and may pass the part's cap, up to the room the hook's 9,500-byte
    output has left; only the rest of the blocks is cut.
- **Rationale**: the clarification (once per scene; a word each time). The log-based lookback keeps the hook
  stateless, as the other parts are.
- **Unverified**: the extra tokens per average turn (SC-004), measured in the build against the present output on
  the play logs.

## R6. The canon reminder and `Register.cs` (FR-004)

- **Decision**: in a campaign with `records/`, `CanonReminder` finds the persons a message names through the index (a
  word whose rows all mean one person, or a name row), then ranks that person's canon lines as now
  (`Register.QueryText`). Without `records/` (a campaign before standard 10) it uses `Register` as today. People
  entries keep their bold name, so `Register.Of` still lists the persons and `people-entries-named` still holds.
- **Rationale**: FR-004 replaces the bare-title rule for migrated campaigns; older campaigns keep working
  (Development 2).

## R7. The check at the full save, and the migration's draft (FR-007, FR-008, SC-002)

- **Decision**: one checker, two inputs.
  - `dmc records check <campaign>` checks the records and the sheets' sample lines: classes 1 (a usage with no
    speaker), 2 (a sample that breaks its speaker's rows), 3 (two rows that disagree) and 5 (a sample against rows and
    the key words of limits). Class 4 is checked at the edit (R3).
  - `dmc records check <campaign> --draft` checks the migration's draft: the rows the migration proposes before the
    player rules, which may hold conflicts the records cannot (no speaker; a `never` and a usage). Three classes are the
    draft's own: `ambiguous-no-speaker` (a word for two persons with a row lacking its speaker: "Auntie"),
    `names-lines-differ` (two Names lines of one speaker giving different rows for one person: the five sheets) and
    `no-source` (a line with no scene, quotation or setup). Its findings are the conflicts put to the player, and SC-002
    is measured on them.
  - `dmc records draft <campaign>` starts the draft with what has a regular shape: each People `(also: …)` item as a
    row with no speaker, sourced to its canon line. The model adds the rows of the Names lines (prose), each sourced to
    its sheet line. The draft is a companion file of the migration's backup
    (`BackupFolder.Companion`, as the accounting file: `src/Dmc.Cli/Campaign/CampaignVerifier.cs:78`), so it is never
    a campaign file and DMC never deletes it.
- **Rationale**: the known cases of SC-002 (the People line with no speaker, "Tam" in Hala's sample, the five sheets
  with two Names lines, "Auntie" for two persons) exist only before the player rules, so the check must see the draft.
  #237's prose parser gave 57% false findings; here `dmc` parses only the regular shapes and JSON, and the model turns
  prose into draft rows, each pointing to its line for the player.
- **Alternatives**: findings from the Markdown directly (rejected: #237's 57%); writing draft rows into `records/`
  (rejected: the check after each edit refuses them).

## R8. The migration's accounting

- **Decision**: each Names line and People `(also: …)` item that goes is accounted `reworded`, with an anchor in the
  record that now holds it (`"word": "…"`).
- **Rationale**: `dmc campaign verify` searches every file's text for an anchor, JSON included
  (`CampaignVerifier.cs:82`, `:272`). A JSON file is one unit (`CampaignVerifier.cs:41`, `:259`), so the new records
  need no accounting of their own.

## R9. Migration 003's check

- **Decision**: `npc-sheets-have-names-line` (`MigrationChecks.cs:225`) also passes a sheet with a "Names for places
  and things" line when the campaign has `records/`.
- **Rationale**: `dmc campaign verify` runs the checks of every pending migration on the final files
  (`CampaignVerifier.cs:129`). A campaign below standard 3 migrated to 10 would fail 003's check once 010 removes the
  Names lines.

## R10. JSON in a Native AOT build

- **Decision**: `System.Text.Json` with a source-generated `JsonSerializerContext` for the record types, as the
  manifest does (`src/Dmc.Cli/Campaign/Manifest.cs:70`), and `JsonDocument` for line numbers in errors.
- **Rationale**: warnings are errors and reflection-based serialisation warns under AOT (constitution, Build and
  Output Constraints).

## R11. What the kit says, and where

- **Decision**: the write reference is `kit/skills/dmc-character-sheet-update/records-format.md`, with a worked
  example of each kind (the write-path test's lesson), pointed to by the sheet-update, full-save, resume and migrate
  skills. `kit/dm-rules.md:78` and `:81` name "Names they use"; they are reworded to the record with no net growth
  (cap 32,510 bytes, `tests/Dmc.Tests/Toolkit/ToolkitTests.cs:1378`; file 32,507 bytes). Line 63 is unchanged.
- **Rationale**: the map's reference sits beside its skill (`kit/skills/dmc-draw-map/map-format.md`), so it loads only
  when needed (`.docs/development.md` § Writing prompts).
