---

description: "Task list for linked campaigns and the sequel import (issue #179, Phase B)"
---

# Tasks: Linked Campaigns and the Sequel Import

**Input**: Design documents from `specs/011-linked-campaigns/`

**Prerequisites**: plan.md, spec.md, research.md (R1 to R16), data-model.md, contracts/campaign-links.md,
contracts/recall-and-lookup.md, contracts/campaign-sequel.md, contracts/prompts.md, quickstart.md

**Tests**: Requested. The plan names the test classes, the spec's Independent Tests and SC-001 to SC-009 are counted
outcomes, and every test needs only the built program and temporary folders, so none skips (Constitution V). Tests are
written first and fail before the code they cover. The model's part (the import's subagents, the migration's questions,
the sample's answers) is the live runs of quickstart.md, recorded in each PR.

**Organization**: By user story, in the order of the three PRs FR-032 fixes, which is not quite priority order. US1
(P1) needs PR 1's links and is PR 2; US5 (P3) is mostly tests of PR 1's recall scope, so it ships in PR 1.

| PR | Phases | Stories | Branch |
|---|---|---|---|
| 1. Links and recall | 1 to 6 | Foundational, US2, US5, US4 | `011-linked-campaigns` (this branch, with the spec folder) |
| 2. The sequel import | 7 to 9 | US1; then FR-029 (US4) by hand | `011-sequel-import`, from `origin/main` after PR 1 merges |
| 3. Legacy files | 10 to 11 | US3 | `011-legacy-files`, from `origin/main` after PR 2 merges |

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an unfinished task)
- **[Story]**: The spec's user story
- Paths are relative to the repository root. Code is C# on .NET 10, Native AOT; warnings are errors; every type and
  member added or changed gets an XML doc comment, private ones included (CLAUDE.md § Documentation).

## Conventions every task follows

- Every failure is one line, `dmc <command>: <message>`: exit 2 for an argument mistake, exit 1 for anything else. No
  reflection, no new dependency.
- "The linked set" is what `CampaignLinks` (T007) returns: every campaign reachable through Continuity links, both
  ways, each once, with its distance, label (`continues`, `sequel`, `side`, `world`), state and digest holder
  (research R2, data-model.md § Linked set). Nothing else computes it (Constitution III).
- "The same line" is `ContentUnits.Normalise` equality, as migrations use; ids are `ContentUnits.Id`.
- A digest is `adventures/<sequel>/digests/<closed>/` in the shape of data-model.md § Digest. Nothing in this feature
  writes to a closed campaign (FR-016); `ClosedCampaign.Refuse` still guards every writer.
- Black-box tests run the built `dmc` through `tests/Dmc.Tests/Support/DmcRunner.cs` in temporary DMC folders from
  `tests/Dmc.Tests/Support/DmcFolders.cs`; campaign fixtures come from `tests/Dmc.Tests/Campaign/CampaignFixtures.cs`,
  retrieval fixtures from `tests/Dmc.Tests/Retrieval/RetrievalFixtures.cs`, the kit's standard from
  `tests/Dmc.Tests/Campaign/MigrationRoot.cs`. A closed fixture campaign gets its mark from `ClosedCampaign.Write`, or
  a real closing backup from `dmc campaign close NAME --yes` where the test needs one.
- Git: never commit to `main`, never force-add anything under `adventures/`, `modules/` or `rules/`, never skip hooks
  or signing. Commit at each checkpoint, as part of this plan. Fixes from a code review are shown to the user and
  committed only on their approval.

---

## Phase 1: Setup

- [X] T001 Fetch `origin` and confirm branch `011-linked-campaigns` sits on an up-to-date `origin/main`
      (`git fetch origin`, then `git merge-base --is-ancestor origin/main HEAD`); rebase it if not (it has no pushed
      commits)
- [X] T002 Build and test the branch as it stands (`dotnet publish src/Dmc.Cli -r win-x64 -o artifacts/bin`, then
      `dotnet test`), and note any failure before changing anything
- [X] T003 [P] Measure the standing cost's baseline (research R16): bytes ÷ 4 of `kit/dm-rules.md`,
      `kit/skills/dmc-resume-campaign/SKILL.md`, `kit/skills/dmc-campaign-full-save/SKILL.md`, and the `description:`
      line of every `kit/skills/*/SKILL.md`; keep the figures in the scratchpad for T045, T072 and T090

---

## Phase 2: Foundational: the Continuity section and the linked set (PR 1)

**Purpose**: FR-001 to FR-005. Every story reads the linked set: recall (US2, US5), the sequel verb (US1), the
migration's check (US4) and the legacy lines (US3).

**⚠️ CRITICAL**: No story's work starts until T014 passes.

### Tests

- [X] T004 [P] Write `tests/Dmc.Tests/Campaign/CampaignLinksTests.cs` (library tests over temporary DMC folders):
      - reading `## Continuity` in `player-options.md`: `- **Continues**:`, `- **Side campaign of**:` and
        `- **Shares a world with**:`, each `none` or folder names, the last comma-separated. Values are cleaned as
        recall cleans the Predecessor value today (HTML comments, backticks, a leading `adventures/` and slashes
        removed), and names compare case-insensitively;
      - the fallback: a file with no `## Continuity` section is read through `- **Predecessor**:` as Continues
        (FR-005);
      - edges both ways: a campaign that names this one is linked to it (FR-003);
      - breadth-first distances, and each campaign visited once in a two-campaign and a three-campaign ring of Shares a
        world with;
      - the four labels of research R2:
        - `continues`: every step goes from a campaign to the one its Continues line names;
        - `sequel`: every step goes from a campaign to one whose Continues line names it;
        - `side`: one step over a Side campaign of line, in either direction;
        - `world`: anything else;
      - the state, `open` or `closed`, from `ClosedCampaign.IsClosed`;
      - a member whose folder is missing;
      - the digest holder: the member nearest the start, the start included, that holds `digests/<name>/`;
      - the modules reached: each once, at the smallest distance, read with `CampaignExporter.ModuleFolder`, with
        whether the folder exists;
      - the four notices of contracts/campaign-links.md, word for word:
        - `<campaign>: <line> <name> is not in adventures/`, only when no member holds that campaign's digest;
        - `<campaign>: <line> <value> is not a campaign folder name`;
        - `<campaign>: <line> names the campaign itself`;
        - `<campaign>: <line> has more than one name; only <first> is followed`.

        `<line>` is `Continues`, `Side campaign of`, `Shares a world with`, or `Predecessor` for the fallback. A
        Continues link to an open campaign gives no notice (FR-004).
- [X] T005 [P] Add a test to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`: `kit/templates/campaign/player-options.md` has
      no `**Predecessor**` line, and right after `## Rules` has a `## Continuity` section holding exactly the three
      lines, each `none`, with the comments of data-model.md § Continuity section
- [X] T006 [P] Write `tests/Dmc.Tests/Campaign/LinksCommandContract.cs` (black-box), to contracts/campaign-links.md:
      - `dmc campaign links NAME` prints one line per member, nearest first, then by name:
        `<campaign> <label> <distance> <open|closed|missing>[ digest]`. `missing` is a member whose folder is gone;
        ` digest` means a nearer member holds its digest;
      - then one `modules/<name> module <distance>` line per module whose folder exists;
      - then the notices, each prefixed `notice: `, at most three, then `(+N more)`;
      - `no links` for a campaign alone;
      - NAME defaults to the campaign named in `adventures/.active-campaign`, and a closed NAME is allowed;
      - `dmc campaign: no campaign "NAME" in adventures/ (there are: …)` and `dmc campaign: no active campaign: name one (dmc campaign links NAME)`
        exit 1, and usage mistakes exit 2;
      - every byte of the DMC folder is unchanged.

      In `tests/Dmc.Tests/Size/SizeCommandContract.cs`, test that `dmc size NAME --check` prints the same notices
      after its flags, each ending ` (not the full save's to trim)`, at most three, then `(+N more)`; and no link line
      at all for a campaign with no links and no problem (SC-007). Add `["links", "--help"]` to the help data rows in
      `tests/Dmc.Tests/Campaign/CampaignCommandContract.cs`.

### Implementation

- [X] T007 Create `src/Dmc.Cli/Campaign/CampaignLinks.cs` (namespace `Dmc.Campaign`), which makes T004 pass:
      - read a campaign's Continuity section, or its Predecessor line as Continues when it has none (FR-005, R3);
      - build the linked set breadth-first, both ways (R2), by reading `player-options.md` in every folder of
        `adventures/` whose name does not start with a dot;
      - give each member its name, folder, distance, label, state, whether its folder exists, and the nearer member
        holding its digest, if any (data-model.md § Linked set);
      - list the modules reached, and the notices of T004, following only the first value on a single-valued line.

      Recall's module-folder notice stays recall's (T022).
- [X] T008 [P] Add `LinksRequest(string? Name)` to `src/Dmc.Cli/Campaign/CampaignArguments.cs`. Parse
      `links [NAME]`, and add `links` to `CampaignArguments.Commands`, the list the toolkit test's `Choices`
      reads, so the prompts may name `dmc campaign links`
- [X] T009 Implement `links` in `src/Dmc.Cli/Commands/CampaignCommand.cs` (needs T007, T008), which makes T006 pass:
      - the outputs and failures of contracts/campaign-links.md;
      - NAME defaults to the active campaign as `standard` does;
      - it writes nothing;
      - add `dmc campaign links [NAME]` to the usage block, one paragraph to the help, and `links` to the `Summary`
        line and the class's XML comment;
      - in `src/Dmc.Cli/Commands/SizeCommand.cs`, print the linked set's notices after the flags of `--check`, each
        ending with `Flags.NotTheSaves`, at most three, then `(+N more)`, so the full save's rule for its lines is
        unchanged; update its help and XML comments.
- [X] T010 [P] Edit `kit/templates/campaign/player-options.md`: delete the `- **Predecessor**:` line from `## Rules`
      and add, after `## Rules` and before `## Difficulty`, the `## Continuity` section exactly as data-model.md
      § Continuity section gives it (makes T005 pass)
- [X] T011 [P] Edit `kit/skills/dmc-new-campaign/SKILL.md` § 2: the Predecessor bullet becomes a Continuity bullet:
      - write the three lines;
      - write Continues only to a campaign that `dmc campaign standard <it>` reports as `closed`; otherwise refuse,
        say why, and offer `dmc-close-campaign` first (FR-002, US1 scenario 6);
      - one name at most on Continues and on Side campaign of;
      - then run `dmc campaign links <folder>` and tell the player each `notice:` line in one sentence.
- [X] T012 [P] Edit `kit/skills/dmc-resume-campaign/SKILL.md`: delete the Predecessor sentence from § 2 item 1. In
      § 2, where `dmc size <campaign> --check` already runs, say before "Any other line is for the full save" that a
      `notice:` line is told to the player in one sentence. Add no command call (FR-031). Keep the rule that the
      resume reads no other campaign and no `digests/` file (FR-020a)
- [X] T013 [P] Edit `kit/skills/dmc-close-campaign/SKILL.md` § 5: "whose Predecessor line names this campaign" becomes
      "whose Continues line names it" (contracts/prompts.md)
- [X] T014 Run the new tests, then the full suite (`dotnet publish` as in T002, then `dotnet test`); fix until green

**Checkpoint**: `dmc campaign links` works; new campaigns get the Continuity section. Commit.

---

## Phase 3: User Story 2: one recall covers the whole chain, with the digest first (Priority: P1) 🎯 MVP (PR 1)

**Goal**: `dmc recall` searches the campaign, its digests, the linked set and every module, ranks a digest above the
folder it digests, prints each line once, and labels hits from elsewhere (FR-017 to FR-020a).

**Independent Test**: In a chain of three campaigns (`c` continues closed `b`, `b` continues closed `a`, both digests in
`c`), recall a fact only `a` holds, a fact in both `b`'s canon and `b`'s digest, and a fact only `c` holds. The first
is found in `a`'s digest and labelled; the second appears once, from the digest; the third ranks as it does today.

### Tests

- [X] T015 [US2] Extend `tests/Dmc.Tests/Retrieval/RetrievalFixtures.cs`:
      - the sequel's `player-options.md` gets the Continuity section, `- **Continues**: \`old-road\``, and old-road's
        is `none`; one variant keeps the old Predecessor line, for FR-005;
      - add a linked-set fixture in a builder of its own with its own temporary root (`BuildLinkedSet`, apart from
        the existing `NewChain` and `BuildChain`, which existing tests share and whose campaign list `dmc lookup`'s
        error prints): `a` closed; `b` closed, Continues `a`; `c`, Continues `b`, holding `digests/a/` and
        `digests/b/` in FR-013's shape (`index.md`, `people.md`, `places.md`, `world.md`, `spine.md`, `secrets.md`);
      - add a side campaign `d` (Side campaign of `c`) and a same-world campaign `e` (Shares a world with `c`);
      - give each campaign a canon line only it holds.
- [X] T016 [US2] Update and extend `tests/Dmc.Tests/Retrieval/RecallLibraryTests.cs` (needs T015):
      - Move the Predecessor tests to the Continuity section, and change the expected predecessor hit to
        `old-road (continues) story/canon-archive.md:3 s12.3 | ...`.
      - Change the missing-campaign notice to `<campaign>: Continues gone-road is not in adventures/, not searched`.
      - Keep `APredecessorWrittenWithItsFolderAndSlashesIsFollowed` on the old line (FR-005).
      - Add these tests on the linked-set fixture:
        - a fact only `a` holds is printed `c digests/a/<file>:<line> (continues) ...`;
        - a line in both `b`'s canon and `c/digests/b/` prints once, from the digest, and the other copy counts in
          `(N more lines matched)` (FR-018, R5);
        - at equal relevance, `c`'s own canon line ranks above the digest's line, which ranks above `b`'s canon line
          (1.30, then 1.14, then 1.05; R4);
        - `d`'s line is labelled `(side)`, `e`'s `(world)`, and `c`'s own lines carry no label;
        - every line of `digests/*/secrets.md` ends its prefix with ` DM`;
        - with `b`'s folder removed and `c/digests/b/` present, no notice; with the digest removed too, exactly
          `c: Continues b is not in adventures/, not searched` (US2 scenario 4);
        - a closed campaign with only a Predecessor line is still followed (FR-005);
        - recalling in the closed `a` labels the hits from `b` and `c` `(sequel)`;
        - a ring of Shares a world with links searches each campaign once.
- [X] T017 [P] [US2] In `tests/Dmc.Tests/Retrieval/RecallCommandContract.cs` (needs T015), test through the built
      `dmc`:
      - the hit lines of contracts/recall-and-lookup.md § Hit line: a labelled hit from another campaign, a digest
        hit, and a module hit with no label;
      - the 6,000-byte cap over the whole linked set;
      - the predecessor line's new expected start, `old-road (continues) story/canon-archive.md:3 s12.3 | `.
- [X] T018 [P] [US2] In `tests/Dmc.Tests/Retrieval/LookupLibraryTests.cs` (FR-019, R7):
      - `dmc lookup c <a heading in digests/b/people.md>` prints that section;
      - with a sheet of the same heading in `c/character-sheets/`, it prints the usual list of matches;
      - `dmc lookup c/digests/b/people.md <name>` searches that file only.

      No code change is expected. If one fails, fix it in `src/Dmc.Cli/Retrieval/LookupSource.cs`.
- [X] T019 [P] [US2] In `tests/Dmc.Tests/Size/SizeLibraryTests.cs` (SC-001, FR-020a), test that a sequel's resume
      total is identical with and without a `digests/<name>/` folder of several files. No code change is expected,
      since `ReadSet.Build` names files only
- [X] T020 [P] [US2] Grow `tests/Dmc.Tests/Retrieval/RecallTimingTests.cs` to SC-006: a chain of three campaigns, a
      side campaign, a same-world campaign, their digests and a large module, about 3 MB of markdown in all. One
      recall on the release build stays within 1 second and within the output cap, as the test checks today

### Implementation

- [X] T021 [P] [US2] In `src/Dmc.Cli/Retrieval/LineUnits.cs`, make a file DM-only also when its path is
      `digests/<name>/secrets.md` (FR-012), and update the XML comment
- [X] T022 [US2] Rework `src/Dmc.Cli/Retrieval/RecallScope.cs` (needs T007):
      - build the places from `CampaignLinks`, not by walking the Predecessor line: every member whose folder exists,
        and every module once, at its smallest distance;
      - give `Place` the member's link label, null for the recalled campaign and for modules;
      - add `Tier.Digest` between `Canon` and `Archive` in the enum, which is the tie order (contracts/recall-and-
        lookup.md § Weights), for every file under `digests/<name>/`;
      - weight a digest file 1.20 × 0.95^d, where d is the distance of `<name>` in the set, or the holder's distance
        + 1 when `<name>` is not in the set;
      - multiply the weight of every file of a member whose digest a nearer member holds by 0.85, the last resort
        (R4);
      - keep the module-folder notice;
      - take the link notices from `CampaignLinks`, adding `, not searched` to the missing-folder and the
        not-a-folder-name notices;
      - delete `PredecessorLine` and anything else left unused;
      - rewrite the XML comments that describe the Predecessor chain.
- [X] T023 [US2] In `src/Dmc.Cli/Retrieval/Recall.cs` (needs T022):
      - skip a hit whose `ContentUnits.Normalise` text equals that of a hit already printed, and count it in
        `(N more lines matched)` (R5);
      - print the label (contracts/recall-and-lookup.md § Hit line):
        - `<place> (<label>) <file>:<line>...` for a hit from another campaign;
        - `<place> <file>:<line> (<label>)...` for a digest hit, with the digested campaign's label;
        - no label on the recalled campaign's own files or on a module;
      - update the XML comments.
- [X] T024 [P] [US2] Update the XML comment and help text of `src/Dmc.Cli/Commands/RecallCommand.cs`: it searches
      the campaign, its digests, its linked campaigns and their modules
- [X] T025 [P] [US2] Edit Accuracy item 1 of `kit/dm-rules.md` (FR-020):
      - "the earlier campaigns its Predecessor line names" becomes the linked campaigns and their digests;
      - add one clause: a `(side)` or `(world)` hit is a fact of the world, which this campaign's characters do not
        know unless this campaign's files say they learned it, and a `(continues)` hit is known to those who were
        there;
      - grow the file by one line at most.
- [X] T026 [P] [US2] Update the header comment of `tools/recall-test.ps1`: recall follows the linked campaigns and
      their digests, not the Predecessor line
- [X] T027 [US2] Run the Retrieval, Size and Toolkit tests, then the full suite; fix until green

**Checkpoint**: A digest written by hand in FR-013's shape is searched, ranked and looked up correctly. That is what
FR-029 needs. Commit.

---

## Phase 4: User Story 5: modules inside a chain stay shared (Priority: P3) (PR 1)

**Goal**: Every linked campaign's module is reached once, and a campaign's line outranks a module's (FR-017, FR-027).

**Independent Test**: `b`'s canon says the party burned an inn the module describes intact. Recalling the inn in `c`
ranks the canon line, or its digest copy, above the module's, and both are labelled.

- [X] T028 [US5] Add tests to `tests/Dmc.Tests/Retrieval/RecallLibraryTests.cs` (needs T016):
      - a module reached only through the side campaign `d` is searched, and its hits print
        `modules/<name> <file>:<line> DM | ...` with no label;
      - a module two members name is searched once, at the smaller distance;
      - the inn: the canon line (or its digest copy) ranks above the module line, and both are printed (US5
        scenarios 1 and 2).
- [X] T029 [US5] If T028 fails, fix the module list in `src/Dmc.Cli/Campaign/CampaignLinks.cs` or its use in
      `src/Dmc.Cli/Retrieval/RecallScope.cs`. FR-027 adds no rule and no kit text. US5 scenario 3 (no module text in a
      digest) is T050 and T063, in PR 2

**Checkpoint**: Recall reaches every linked module once. Commit.

---

## Phase 5: User Story 4: the existing sequel moves to the new links (Priority: P2) (PR 1)

**Goal**: Migration 005 moves the Predecessor line to the Continuity section and, before its backup, asks whether to
close an open predecessor (FR-028, FR-028a).

**Independent Test**: The golden fixture migrates to the Continuity section, and `verify` passes. The shape check fails
on a file left with the old line.

### Tests

- [X] T030 [P] [US4] Add tests for `continuity-section` to `tests/Dmc.Tests/Campaign/MigrationChecksTests.cs`:
      - it passes on the section with its three lines, whether they hold names or `none` (R14 puts the `none` case
        here);
      - it fails, naming the problem, when a `**Predecessor**` line is present, when `## Continuity` is missing, or
        when any of its three lines is missing.
- [X] T031 [P] [US4] Create the golden fixture `tests/Dmc.Tests/Fixtures/migrations/005/`:
      - `before/player-options.md`: a standard-4 campaign file with `- **Predecessor**: old-tale` under `## Rules`,
        as in `tests/Dmc.Tests/Fixtures/migrations/004/after/player-options.md`;
      - `after/player-options.md`: the Predecessor line gone, and `## Continuity` after `## Rules` with Continues
        `old-tale` and the other two lines `none`;
      - `accounting.tsv`: the Predecessor line's unit `reworded`, anchored on the new Continues line.

      `MigrationGoldenTests` and `MigrationFilesTests.EveryMigrationHasAGoldenFixture` pick it up.
- [X] T032 [P] [US4] Add the tests for the optional section:
      - in `tests/Dmc.Tests/Toolkit/MigrationFilesTests.cs`: a migration that has `## Before the backup` places it
        after `## What changes` and before `## Steps`; one without the section still passes (R13);
      - in `tests/Dmc.Tests/Toolkit/MigrationPromptTests.cs`: migration 005's `## Before the backup` holds
        `Migration stopped until the previous campaign is closed. You can close it by running: /dmc-close-campaign
        <predecessor>` word for word, names `dmc campaign standard` and `dmc-close-campaign`, and never tells DMC to
        write `adventures/.active-campaign`;
      - in `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`: the campaign line of `kit/skills/dmc-campaign-full-save/SKILL.md`
        allows a campaign named by the skill that called it;
      - also in `MigrationPromptTests.cs`: `kit/skills/dmc-migrate-campaign/SKILL.md` § 2 names `## Before the
        backup`.

### Implementation

- [X] T033 [US4] Register `continuity-section` in `src/Dmc.Cli/Campaign/MigrationChecks.cs` (R14), with its XML
      comment (makes T030 pass)
- [X] T034 [P] [US4] Write `kit/migrations/005-continuity-links.md` (R13, R14, data-model.md § Migration 005):
      - **Front matter**: `standard: 5`, a title, `shipped: PR #<n>` (filled in by T047),
        `touches: [player-options.md]`, `drops: []`, `checks: [continuity-section]`.
      - **`## What changes`**.
      - **`## Before the backup`**: when the Predecessor line names a folder, run `dmc campaign standard
        <predecessor>`, then:
        - on `behind`: follow `dmc-migrate-campaign` for the predecessor first, then check again;
        - on `closed`, on `none`, or when the folder is missing: go on;
        - otherwise: warn the player that the earlier campaign is still open, and that playing it on would make its
          story and the sequel's disagree. Then ask once whether to close it, saying that closing is permanent.
          - On yes: follow `dmc-close-campaign` for the predecessor, naming it, with its step 2 skipped (this yes is
            that question), and go on. `adventures/.active-campaign` is not changed. If the close skill refuses at
            its step 1, relay the refusal and stop, changing nothing.
          - On no: ask once, simply, "are you sure?". On yes, go on: Continues names the open campaign, and nothing
            repeats the warning later (FR-004). On no, stop with `Migration stopped until the previous campaign is
            closed. You can close it by running: /dmc-close-campaign <predecessor>`, with the folder name in place
            of `<predecessor>`.
      - **`## Steps`**:
        - if the file has no `## Continuity` section, add it after `## Rules` with the three lines, Continues taking
          the Predecessor value (or `none`);
        - delete the Predecessor line;
        - account its unit as `reworded`, anchored on the Continues line.
      - **`## Judgments`**, **`## Declared drops`** (none) and **`## Done when`**.
- [X] T035 [P] [US4] Edit § 2 of `kit/skills/dmc-migrate-campaign/SKILL.md`: after the player's yes, follow each
      pending migration's `## Before the backup`, if it has one, before § 3. A stop there changes nothing, and the
      campaign stays `behind` and playable (R13). Write it as one generic sentence that names no migration. In
      `kit/skills/dmc-campaign-full-save/SKILL.md`, extend the campaign line: the folder named in
      `adventures/.active-campaign`, or the campaign the skill that called this one names (R13; about 10 tokens,
      counted in T045)
- [X] T036 [US4] Run the Campaign and Toolkit tests (golden, files, prompts, checks), then the full suite; fix until
      green

**Checkpoint**: Campaigns at standard 4 migrate to 5; a stop before the backup leaves them playable. Commit.

---

## Phase 6: PR 1 polish: documents, cost, live runs, the PR

- [X] T037 [P] Edit `.docs/campaign-files.md`:
      - the Continuity section of `player-options.md`: its three lines, links followed both ways, and Continues
        naming a closed campaign;
      - `digests/`, a folder a sequel may hold, which recall and lookup search and resume never reads (its files
        are described in PR 2);
      - the sentence about the Predecessor line in § Closing a campaign;
      - § Which file wins: a digest over its campaign's folder, and canon over a module.
- [X] T038 [P] Edit `.docs/dmc-reference.md`:
      - a `dmc campaign links` section, from contracts/campaign-links.md;
      - `dmc recall`'s What it searches, Ranking (`Tier.Digest`, the last-resort factor, duplicates) and Output
        (labels, the changed notice), from contracts/recall-and-lookup.md;
      - `dmc lookup` over digests;
      - `dmc size --check`'s link notices;
      - the `campaign` verb list, and the sequel sentence of the close section.
- [X] T039 [P] Edit `.docs/user-guide.md`: linking campaigns (a sequel, a side campaign, the same world), what recall
      reaches then, and the migration's question about an open predecessor
- [X] T040 [P] Edit `.docs/architecture.md`: recall's scope comes from `CampaignLinks`, and the link notices reach
      resume through the `dmc size --check` it already runs
- [X] T041 [P] Edit `.docs/development.md` § Changing a campaign-file standard: describe the optional
      `## Before the backup` section, where it sits, and that the migrate skill follows it before the backup (R13).
      Spec 005's contract is frozen and stays as it is
- [X] T042 [P] Add to `.docs/decisions.md`, each with its reason and the alternatives rejected:
      - links followed both ways (R2);
      - the digest's weight and the last-resort factor (R4);
      - duplicates found by the migrations' normalisation (R5);
      - the hit labels (R6);
      - the optional `## Before the backup` section (R13).
- [X] T043 [P] Edit `.docs/roadmap.md`: the status of issue #179 Phase B, PR 1 (links and recall)
- [X] T044 Check that every new and changed member has an XML doc comment, private ones included, and that
      `specs/011-linked-campaigns/` still matches what PR 1 ships
- [X] T045 Measure the standing cost again as in T003, and record PR 1's share of FR-031's 150 tokens. Measured: +149 bytes, about 37 tokens (dm-rules +174, resume -82, full save +57, descriptions 0), and no command call added. After the code review's `(sequel)` clause (T048): +188 bytes, about 47 tokens (dm-rules +213)
- [X] T046 Run quickstart.md § PR 1 in a scratch DMC folder outside any checkout:
      - steps 1 to 5 with the release build;
      - step 6, migration 005's four paths, in a live `dmc play` with the player (or as the player decides);
      - step 7 is T045;
      - record what DMC said and wrote, for the PR.

      Steps 1 to 5 done on 2026-11-11 with the published binary in a manifest-only scratch folder, all as quickstart says (recall 127 ms; 120 ms after T048's fixes).
      Step 6 done on 2026-11-12, as the user chose: headless Claude Code sessions (`claude -p`, Sonnet at high effort,
      as `dmc play` starts them, the branch's `dmc` first on PATH) in a DMC folder installed from the branch, one pair
      of campaigns per path, DMC answering for the player. All five paths pass after three kit fixes the runs found:
      - the close skill closed the active campaign (the sequel) instead of the predecessor the migration named; it now
        takes the campaign its caller names and leaves `.active-campaign` as it is;
      - after the close, the migration did not resume; the close skill now hands back to its caller, and the migrate
        skill goes on to its backup after a skill its Before the backup ran;
      - "are you sure?" was once asked as "are you sure you want to keep it open?", where yes would stop; migration
        005 now asks whether the player is sure they want to go on without closing it.
      The runs also showed the migrate skill had no default campaign when the player accepts resume's offer (it once
      took every behind campaign); it now takes the active one
- [X] T047 Open PR 1, then check it:
      - commit, push, and open the PR with the full description: what changed, why, the sources (spec 011 FR-001 to
        FR-005, FR-017 to FR-020a, FR-027, FR-028, FR-028a, and the research items), the tests, the documents
        changed, migration 005 and why it is needed, the measured cost, and `Part of #179` on its own line;
      - set `shipped: PR #<n>` in `kit/migrations/005-continuity-links.md` and push;
      - check the description with `gh pr view <n> --json body`.
- [X] T048 Finish PR 1:
      - run the built-in code review once on PR 1, and check each finding yourself;
      - show the fixes to the user, and commit them only on their approval;
      - check that the documents are current;
      - wait for CI to pass, then ask the user to merge.

---

## Phase 7: User Story 1: a sequel starts from a closed campaign and loses nothing (Priority: P1) (PR 2)

**Goal**: `dmc-new-campaign` imports a closed campaign through subagents into a brief and a digest, and
`dmc campaign sequel` proves it: the spine, the accounting against the closing backup, and the sample draw (FR-006
to FR-016, FR-008a, FR-021 to FR-024).

**Independent Test**: Close the fixture campaign, start a sequel of it, and import. The brief's facts are in the
sequel's always-read sections, the companion's sheet states her history's version, `sequel --check` passes, and the
sample test passes.

- [X] T049 [US1] After PR 1 merges: delete its branch, fetch `origin`, and create `011-sequel-import` from
      `origin/main`

### Tests

- [X] T050 [P] [US1] Create the fixture `tests/Dmc.Tests/Fixtures/sequel/`:
      - `campaign/`: a small campaign at the kit's standard (5) that `dmc campaign close --yes` accepts. It holds:
        - a carried companion whose species default ("immune to cold") her history contradicts, with the reason,
          spread over `story/canon-archive.md`, her sheet's history and one session log;
        - a debt, a ruling, a secret never revealed, and one revealed (`revealed s2`);
        - an NPC sheet with every section of `kit/templates/npc-sheet.md` filled, its `## Secrets` included, and a
          dead enemy's sheet with a stat block;
        - a fact that lives only in a session log;
        - a test fight that § How it ended names as not canon;
        - a `module-progress.md` with a deviation and a module NPC's status;
        - session logs with `### Scene N:` headings in the shapes The Silver Egg has: `(place, time)`, one value
          (`(Day 1)`), several commas, and a log titled `# Session 3 (void)` with no scene headings.
      - `digest/`: its correct digest, without `index.md` and `spine.md`, which `--start` writes: `people.md`,
        `places.md`, `factions.md`, `items.md`, `world.md` (with `## Module`; US5 scenario 3), `secrets.md` and
        `accounting.tsv`.

      Add a helper to `tests/Dmc.Tests/Campaign/CampaignFixtures.cs` that copies the campaign into a temporary DMC
      folder, closes it with the built `dmc`, and creates a sequel whose Continues names it.
- [X] T051 [P] [US1] In `tests/Dmc.Tests/Campaign/AccountingFileTests.cs`:
      - a digest's accounting accepts `merged` lines whose anchor has 8 characters or more, and rejects `reworded`;
      - a migration's accounting rejects `merged`;
      - each error names its line, and the existing tests still pass.
- [X] T052 [P] [US1] Write `tests/Dmc.Tests/Campaign/SequelDigestTests.cs` (library, needs T050).

      **Start:**
      - `digests/<closed>/index.md` holds data-model.md's six lines: `- **Format**: 1`, `- **Campaign**:`,
        `- **Closing backup**: <file> sha256:<hex>`, `- **Chain**:`, `- **Carried**: none` and `- **Files**:`;
      - `spine.md` holds, per log, `## <the log's # title> (session-NNN.md)`, then `- sN.M <the heading's text after
        "Scene M: ">` for each `### Scene M:` heading, word for word and in order, parsing nothing; a log with no
        scene headings gets its session line alone (data-model.md § Digest);
      - each `digests/<X>/` of the closed campaign is copied byte for byte and named on Chain, nearest first;
      - the parts plan's part 1 is `player-options.md` and `story/dm-notes.md` whole; the other parts cover every
        accounted source and session log once, in parts of about 100 KB, split only at a log's boundary or a source's
        `## ` heading; a source the campaign lacks is left out and counts no units;
      - a second run keeps Carried and rewrites the generated lines;
      - it refuses when `digests/<closed>/index.md` names another campaign;
      - no byte of the closed campaign changes (FR-016).

      **Units:**
      - they are read from the closing backup: an edit to the live folder after closing changes none of them;
      - only the FR-021 sources count;
      - every line of every file in `character-sheets/` counts, each sheet whole;
      - `dm-notes.md` counts only § Secrets, and `summary.md` only § How it ended.

      **Check:** it passes on the fixture's digest with the counts line. A line of 20 characters or more contained
      in a longer digest line counts as copied. It fails with one line per problem, at most 40, then a count:
      - one copied line deleted from `people.md`: `unaccounted: 1 units (dmc campaign sequel NAME --list)`;
      - an unknown id, a class not declared, or an anchor not in the digest: `accounting: <id>: <reason>`;
      - one byte changed in a chain copy: `copy differs: digests/<X>/<file>`;
      - an index line missing, or a checksum that differs: `index: <line>`;
      - a file over 20,000 estimated tokens: `too large: digests/<closed>/<file> <N>k tokens (split it by subject)`;
      - one heading in both `people-1.md` and `people-2.md`: `heading twice: digests/<closed>/people: <heading>`.
- [X] T053 [P] [US1] Write `tests/Dmc.Tests/Campaign/SampleDrawTests.cs`:
      - the same backup and round give the same 25 items, and round 2 gives a different draw;
      - the 20 facts are split between copied lines and session-only lines in proportion, rounded, with at least one
        of each when both exist;
      - the 5 people come from the closing backup (each sheet's `# ` title but `*-history.md` and `minor-npcs.md`, and
        the Name cell of each row of `minor-npcs.md`'s table), and at least one is on the index's Carried line when
        that line names anyone;
      - a person with a sheet in the backup but no heading in the digest can be drawn;
      - with fewer lines or people than asked, all of them are drawn;
      - `the digest has no fact lines` and `the closing backup has no characters` exit 1;
      - the seed prints as 16 hex digits, `+`, then the round.
- [X] T054 [P] [US1] Write `tests/Dmc.Tests/Campaign/SequelCommandContract.cs` (black-box), to
      contracts/campaign-sequel.md:
      - every form;
      - the six refusals of § Refusals, word for word, each exit 1; usage mistakes exit 2;
      - the output shapes of `--start`, `--list`, `--check` and `--sample`;
      - `--list` stops at 40 lines, then `(+N more: --all)`; `--all` prints everything; and `nothing unaccounted`
        when done;
      - only `--start` writes, and only under `adventures/NAME/digests/`.

      Add `["sequel", "--help"]` to the help data rows in `CampaignCommandContract.cs`.
- [X] T055 [P] [US1] Add tests to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`:
      - `kit/skills/dmc-new-campaign/SKILL.md` has `## 1c. A sequel` after its `## 1b.` section, and names
        `dmc campaign sequel` with `--start`, `--list`, `--check` and `--sample`, plus `import/progress.md` and
        `digests/`;
      - its description contains "continue a closed campaign as a sequel";
      - `kit/skills/dmc-character-sheet-update/SKILL.md` holds the clause for characters from linked campaigns, and
        names `digests/` and `dmc recall` or `dmc lookup`;
      - § 1c names `dmc-character-sheet-update` for the carried characters' sheets and does not restate the rule (no
        "species or class defaults" in § 1c).

### Implementation

- [X] T056 [P] [US1] In `src/Dmc.Cli/Campaign/AccountingFile.cs`, let the parser take the keyword it accepts for a
      unit kept in changed form: `reworded` for migrations (the default), `merged` for a digest, both with the
      existing anchor rule (`MinAnchor`). The error for a wrong keyword names the accepted one ("use merged or
      dropped:<class>"). Update `CampaignVerifier`'s call, and the XML comments (makes T051 pass)
- [X] T057 [US1] Create `src/Dmc.Cli/Campaign/SequelDigest.cs`, with the refusals and the start (R8, R10;
      contracts/campaign-sequel.md):
      - the six refusals, in the contract's order:
        - NAME closed (`ClosedCampaign.Refusal`);
        - no Continues link;
        - the link names an open campaign;
        - the closing backup is missing (`ClosedCampaign.Read` and `BackupFolder.Closing`);
        - the backup fails the export check (`BackupFolder.Open`);
        - the closed campaign differs from its backup (make `CampaignVerifier.VerifyClosed`'s byte check internal
          and reuse it, rather than copy it);
      - `Start` writes the index, the spine from the backup's session logs, and the chain copies, prints the parts
        plan, and on a rerun keeps the Carried line.
- [X] T058 [US1] In `SequelDigest.cs`, add the accounted units and `--list` (R9):
      - read the FR-021 sources from the closing backup with `CampaignVerifier.OpenBackup`;
      - turn them into units with `ContentUnits.Split`, `Normalise` and `Id`, within the sections T052 names;
      - each unit's status is copied, merged, dropped or unaccounted;
      - `--list` prints `unaccounted: <id> <path>:<line> | <text>` lines, 40 at most without `--all`.
- [X] T059 [US1] In `SequelDigest.cs`, add `--check` (R10):
      - the accounting is complete;
      - the declared classes are `superseded`, `not-canon`, `structure`, `bookkeeping` and `stat-block`;
      - each chain copy is byte-identical to the closing backup's;
      - the index's lines are present, and its checksum equals the backup's SHA-256;
      - every file is at or under `Budgets.NearCapTokens`, using `Measurement`'s estimate;
      - no `## ` heading appears twice across the parts of `people*`, `places*`, `factions*`, `items*` or `world*`.

      On a pass, print `checked digests/<closed>/: N units (C copied, M merged, D dropped), F files, largest Xk
      tokens`, using `Measurement.FormatTokens`. Together with T057 and T058, this makes T052 pass.
- [X] T060 [US1] Create `src/Dmc.Cli/Campaign/SampleDraw.cs` (R11; needs T058, whose accounted units tell a copied
      line from a session-only one), which makes T053 pass:
      - seed with the first 8 bytes of the closing backup's SHA-256 plus the round;
      - draw with a small PRNG written out in the class (SplitMix64), not `System.Random`, so the draw never changes
        with the runtime;
      - fact lines are the non-blank, non-heading, non-comment lines of the digest's subject files; people are read
        from the closing backup's sheets and `minor-npcs.md` (R11);
      - a fact line is a copy when it matches an accounted unit, and session-only when it does not;
      - print the `--sample` lines of the contract.
- [X] T061 [P] [US1] Add `SequelRequest` to `src/Dmc.Cli/Campaign/CampaignArguments.cs`, and `sequel` to
      `CampaignArguments.Commands`:
      - exactly one of `--start`, `--list`, `--check` and `--sample`;
      - `--all` only with `--list`;
      - `--round N`, with N of 1 or more, only with `--sample`.
- [X] T062 [US1] Implement `sequel` in `src/Dmc.Cli/Commands/CampaignCommand.cs` (needs T056 to T061):
      - run the four forms, so that T054 passes;
      - add the four forms to the usage block, one paragraph to the help, and `sequel` to the `Summary` line and the
        class's XML comment.
- [X] T063 [US1] Write `## 1c. A sequel` in `kit/skills/dmc-new-campaign/SKILL.md`, after § 1b, as
      contracts/prompts.md's PR 2 row says (R15):
      - confirm the closed campaign with `dmc campaign standard`;
      - ask which characters carry over;
      - run `sequel --start`, then write those names on the new index's Carried line (it starts as `none`, and a rerun
        keeps it);
      - run one subagent per part, in order. Part 1's subagent writes the brief's open and deferred threads into the sequel's
        arc plan, the house rules and standing requests into its `player-options.md`, and the DM notes' secrets into
        the digest's `secrets.md` (and, where they can still come up, the sequel's DM notes). Each is told:
        - its part's files and ranges, and to read the closed campaign only through them;
        - the digest's shape, from data-model.md;
        - to copy lines word for word under one subject;
        - to add session-only facts with their `sN.M`;
        - to keep secrets in `secrets.md`, with their outcomes, a sheet's own `## Secrets` lines included, under the
          character's name;
        - to copy a living character's stat block under their heading, and account a dead one's as
          `dropped:stat-block`;
        - what to leave out (FR-014), including any module text, with `module-progress.md`'s deviations and module
          NPC statuses going to `world.md` § Module (US5 scenario 3);
        - for the parts that hold them, the brief's targets in the sequel (FR-007), and to write each carried
          character's sheet by following `dmc-character-sheet-update`, whose clause (T064) is the rule's one home
          (FR-008);
      - then `sequel --list` and the accounting, then `sequel --check`;
      - then `sequel --sample`, with one fresh subagent given only the digest, the comparison in the main
        conversation (reading only the lines it needs), and a new draw after any fix;
      - record progress in `import/progress.md`;
      - open the first scene only after the sample passes;
      - for parts and subagents, point to § 1b rather than repeat it.

      Add "or to continue a closed campaign as a sequel" to the description.
- [X] T064 [P] [US1] Add one clause to § New character of `kit/skills/dmc-character-sheet-update/SKILL.md` (FR-008,
      FR-008a), the rule's only home in the kit:
      - a character from a linked campaign, carried over at a sequel's import or returning later, is first looked up
        with `dmc lookup` or `dmc recall`, in the digest or in that campaign;
      - the sheet is built from their history, with species or class defaults only where it says nothing;
      - their voice, the names they use and their appearance go on the sheet itself.
- [X] T065 [US1] Run the Campaign and Toolkit tests, then the full suite; fix until green

**Checkpoint**: A sequel can be imported and proved. Commit.

---

## Phase 8: PR 2 polish: documents, cost, the live run, the PR

- [X] T066 [P] Edit `.docs/campaign-files.md`:
      - the digest's files and rules;
      - that a digest never changes after its check;
      - that a format change ships a migration that rebuilds digests from the closing backup.
- [X] T067 [P] Edit `.docs/dmc-reference.md`: a `dmc campaign sequel` section, from contracts/campaign-sequel.md
- [X] T068 [P] Edit `.docs/user-guide.md`: starting a sequel, and what the player sees (the question about who carries
      over, the import part by part, no scene until the sample passes)
- [X] T069 [P] Edit `.docs/architecture.md`: the import, the subagents' part and `dmc`'s exact parts
- [X] T070 [P] Add to `.docs/decisions.md`, each with its reason:
      - the accounting is checked against the closing backup (R9);
      - the `merged` keyword (R9);
      - the sample's seed and size (R11);
      - the verb's name, `sequel`, which avoids `dmc digest` (R8).
- [X] T071 [P] Edit `.docs/roadmap.md`: the status of Phase B, PR 2
- [X] T072 Measure the standing cost as in T003, and record PR 2's share. Only `dmc-new-campaign`'s description
      should grow. Measured: +45 bytes, about 11 tokens, all of it that description; the three always-read files are
      unchanged. With PR 1's 188 bytes, 233 bytes, about 58 tokens of FR-031's 150
- [X] T073 Run quickstart.md § PR 2, steps 1 to 7, on the fixture campaign in a scratch DMC folder: a live import with
      the player (or as the player decides), `--check`, `--sample` twice and with `--round 2`, a chain, and the
      resume's transcript check (SC-001); record the results for the PR. Run after PR #196 opened, headless in
      `D:\Claude\test-campaign` (Sonnet, high effort): all seven steps pass (grey-tarn closed, ember-gate imported, 123
      units checked, the sample passed in round 2 after one miss was fixed; the chain to third-bell copied grey-tarn's
      digest byte for byte; the resume read no digest and no closed campaign; `dmc size` 8.2k either way). Two § 1c
      fixes: Carried names and people headings take the sheets' titles, which the sample matches; the accounting and
      the comparison read only the lines they need, never a whole file
- [X] T074 Check the XML doc comments and that the spec folder still matches what ships. Then open PR 2:
      - commit, push, and open the PR with the full description: the sources (FR-006 to FR-016, FR-008a, FR-021 to
        FR-024) and `Part of #179` on its own line;
      - say why it needs no migration: the digest is a new folder that only new writes create
        (`.docs/development.md` § Changing a campaign-file standard);
      - check it with `gh pr view <n> --json body`.
- [X] T075 Run the code review once; show the fixes to the user and commit them on their approval; check the
      documents; wait for CI to pass, then ask the user to merge

---

## Phase 9: PR 2 follow-up: the maintainer's sequel, by hand (FR-029, User Story 4)

**Goal**: Rorn's Landing gets The Silver Egg's digest once, with the player, on local data that is never committed.

- [ ] T076 [US4] With the player, in `D:\Claude\dmc`:
      - make sure The Silver Egg is closed, at Rorn's Landing's migration 005 or with `dmc-close-campaign`;
      - run `dmc campaign sequel rorns-landing --start`;
      - build the digest with subagents as § 1c says: the digest only, with no new campaign and no brief;
      - account with `--list`;
      - `--check` passes (SC-002);
      - run `--sample` with one fresh subagent: 0 misses (SC-003).
- [ ] T077 [US4] Rerun `tools/recall-test.ps1` on the play folder (SC-004):
      - at least 28 of 30 named and 23 of 30 indirect;
      - every fact held both in the digest and in The Silver Egg's folder is printed from the digest;
      - with the user's go-ahead, post the figures on issue #179.

---

## Phase 10: User Story 3: campaigns in one world stay in step (Priority: P2) (PR 3)

**Goal**: A campaign linked to an open campaign writes `legacy.md` at each full save. `dmc size --check` names a newer
legacy at resume, and the next full save folds it into canon (FR-025, FR-026).

**Independent Test**: Full-save side campaign `d` with a world change, and resume `c`. One `legacy:` line names `d`,
and `c`'s next full save folds the change into canon. Resume again, and nothing is named.

- [ ] T078 [US3] After PR 2 merges: delete its branch, fetch `origin`, and create `011-legacy-files` from
      `origin/main`

### Tests

- [ ] T079 [P] [US3] Test the legacy lines in `tests/Dmc.Tests/Campaign/CampaignLinksTests.cs` and
      `tests/Dmc.Tests/Size/SizeCommandContract.cs` (contracts/campaign-links.md § In `dmc size NAME --check`).
      `legacy: <campaign> legacy.md (written session N) (not the full save's to trim)` is printed by
      `dmc size NAME --check`:
      - for a member, open or closed, whose `legacy.md` `- **Written**: session N, yyyy-MM-dd` line names a session
        later than the one NAME's `game-state.md` `- **Legacies folded**:` line records for it (`<campaign> sN`,
        comma-separated);
      - when NAME records nothing for that member, or has no Legacies folded line;
      - as `written unknown` when the member's Written line is unreadable;
      - never for NAME's own `legacy.md`, and never when the recorded session is the same or later;
      - never at all for a campaign alone (SC-007);
      - and, for the case of two sessions at once: a legacy written in a session of the member that NAME has not
        recorded is printed whatever the dates say, including when both carry the same date.

      `legacy due: open linked campaigns <names>: rewrite legacy.md` is printed when the set holds an open campaign
      and NAME's own `legacy.md` is missing or its Written session is earlier than the leading number of NAME's
      `- **Current session**:` line (which may run on with text, as The Silver Egg's does); never for a campaign
      alone, or one whose set holds only closed campaigns.
- [ ] T080 [P] [US3] In `tests/Dmc.Tests/Size/SizeLibraryTests.cs`, test that a `legacy.md` over 3,000 estimated
      tokens is flagged, and that it is never part of the resume total
- [ ] T081 [P] [US3] Add tests to `tests/Dmc.Tests/Toolkit/ToolkitTests.cs`:
      - `kit/templates/legacy.md` has `# Legacy of`, a `- **Written**:` line and `## What changed in the world`, and
        keeps its guidance in an HTML comment;
      - `kit/skills/dmc-campaign-full-save/SKILL.md` names `legacy due` and `templates/legacy.md`;
      - the resume's size-check sentence covers `legacy:` lines.

### Implementation

- [ ] T082 [P] [US3] Create `kit/templates/legacy.md` (R12, data-model.md § Legacy file):
      - the header, the Written line (`- **Written**: session N, yyyy-MM-dd`), and `## What changed in the world`;
      - an HTML comment with the guidance: changes the other campaigns would see or hear of, in the world's terms,
        each with its `sN.M`; no secrets and none of the party's private plans; a budget of 3,000 tokens.
- [ ] T083 [US3] Add the legacy comparison to `src/Dmc.Cli/Campaign/CampaignLinks.cs`, and the `legacy:` and
      `legacy due:` lines to `dmc size --check` in `src/Dmc.Cli/Commands/SizeCommand.cs`, outside the three-notice cap
      (makes T079 pass). Update the help text and XML comments
- [ ] T084 [P] [US3] Add `Budgets.Legacy = 3000` and `BudgetKind.Legacy` to `src/Dmc.Cli/Size/Budgets.cs`. Make
      `dmc size` measure `legacy.md`, when it is present, as a part that `SizeReport.Total` does not count, and add a
      flag in `src/Dmc.Cli/Size/Flags.cs` when it is over budget (makes T080 pass).
      `src/Dmc.Cli/Size/ReadSet.cs` and `SizeReport.cs` need the change too: `dmc size` has no part outside the total
      today, and the plan names only `Budgets.cs` and `Flags.cs`
- [ ] T085 [P] [US3] Add one clause to `kit/skills/dmc-campaign-full-save/SKILL.md`:
      - when `dmc size <campaign> --check` prints `legacy due:`, rewrite `legacy.md` from `templates/legacy.md`,
        within its budget, before step 7's check;
      - fold into canon any legacy read since the last save, marked as world knowledge unless the characters saw it,
        and record its session on `game-state.md`'s `- **Legacies folded**:` line (`<campaign> sN`, comma-separated),
        adding the line if the file has none (FR-025, FR-026).
- [ ] T086 [P] [US3] In `kit/skills/dmc-resume-campaign/SKILL.md`, extend § 2's size-check sentence: for each
      `legacy:` line, read that file, and no other file of that campaign. A `legacy due:` line is already covered by
      "for the full save"
- [ ] T087 [US3] Run the Campaign, Size and Toolkit tests, then the full suite; fix until green

**Checkpoint**: Two open campaigns in one world stay in step; a campaign alone pays nothing. Commit.

---

## Phase 11: PR 3 polish, and convergence

- [ ] T088 [P] Edit `.docs/campaign-files.md` (`legacy.md`: what it holds, its budget, who writes and reads it; the
      `Legacies folded` line of `game-state.md`, which only a linked campaign has) and
      `.docs/user-guide.md` (campaigns in one world)
- [ ] T089 [P] Edit `.docs/dmc-reference.md` (`dmc size --check`'s `legacy:` and `legacy due:` lines; the legacy
      budget),
      `.docs/architecture.md` (the full save writes `legacy.md`), `.docs/decisions.md` (session numbers rather than
      dates, times or file times, R12) and `.docs/roadmap.md` (Phase B done; Phases C and D remain)
- [ ] T090 Measure the standing cost as in T003. Record PR 3's share and the three PRs' total, which must be under
      150 tokens (FR-031, SC-007)
- [ ] T091 Run quickstart.md § PR 3, steps 1 to 4, in a scratch DMC folder, and record the results for the PR
- [ ] T092 Check the XML doc comments and the spec folder. Then open PR 3:
      - commit, push, and open the PR with the full description and `Part of #179` on its own line;
      - say why it needs no migration: `legacy.md` is a new file that only new writes create;
      - check it with `gh pr view <n> --json body`.
- [ ] T093 Run the code review once; show the fixes to the user and commit them on their approval; check the
      documents; wait for CI to pass, then ask the user to merge
- [ ] T094 After each PR merges, the user runs `/speckit-converge`. The spec is frozen once a pass adds no tasks and
      its PR has merged (`.docs/development.md` § Spec-driven changes)

---

## Dependencies and order

- **Phase 1 → Phase 2** → the stories. Phase 2 (T004 to T014) blocks everything, because every story reads the
  linked set.
- **US2 (Phase 3)** needs Phase 2; T022 needs T007. **US5 (Phase 4)** needs US2 (T016, T022). **US4 (Phase 5)** needs
  Phase 2 only, so it can run alongside US2.
- **PR 1 polish (Phase 6)** needs Phases 2 to 5.
- **US1 (Phase 7)** starts after PR 1 merges. It needs `CampaignLinks` (Continues) and PR 1's recall, which
  searches the digest it writes.
- **FR-029 (Phase 9)** needs PR 2 merged and installed, and The Silver Egg closed.
- **US3 (Phase 10)** starts after PR 2 merges. It needs `CampaignLinks` only; it comes last because FR-032 orders the
  PRs that way.
- Within a story: tests first (they fail), then the code in the order listed, then the prompts.

## Parallel examples

- Phase 2: T004, T005 and T006 together; then T007 and T008 together, with T010 to T013 alongside; then T009.
- US2: after T015, T017 to T020 together, and T016; then T021, T024, T025 and T026 alongside T022; then T023.
- US4: T030, T031 and T032 together; then T033, T034 and T035 together.
- US1: after T050, T051 to T055 together; then T056 and T061 together, alongside T057 to T059 (one file, in order),
  and T063 and T064; T060 after T058; then T062.
- US3: T079, T080 and T081 together; then T082, T084, T085 and T086 alongside T083.
- Polish phases: the document tasks together (T037 to T043; T066 to T071; T088 and T089).

## Implementation strategy

- **MVP**: Phases 1 to 3 (Foundational and US2). One recall covers the linked set and ranks a hand-written digest
  first, which is FR-029's first need. It cannot ship alone, though: the template's Continuity section changes what
  campaign files hold, so the same PR must carry migration 005 (US4). FR-005's fallback keeps an unmigrated campaign
  linked in the meantime, but it does not bring the campaign up to the new standard.
- **PR 1**: add US5 (tests) and US4 (the migration). This is the first shippable increment.
- **PR 2**: US1, the reason for the phase. It is proved on the fixture, then by hand on the maintainer's sequel
  (Phase 9).
- **PR 3**: US3, for players who run more than one open campaign in a world.
- Each PR leaves the build green and its documents current, says `Part of #179`, and gets one code review.

---

## Phase 12: Convergence

- [X] T095 Say in `kit/skills/dmc-new-campaign/SKILL.md` § 1c that each line the brief writes into the sequel's files keeps its original `sN` or `sN.M` and names the campaign it came from, per FR-007 (partial)
- [X] T096 Add to the clause in `kit/skills/dmc-character-sheet-update/SKILL.md` § New character that where the history and a species or class default disagree, the sheet states the history's version and, where the history gives one, its reason, per FR-008 (partial)
- [X] T097 Complete § 1c's list of what the digest leaves out with dice bookkeeping in the logs, maps, and whatever the closing save named as not canon, per FR-014 (partial)
- [X] T098 Name in § 1c the fallback without subagents (one part per session, as § 1b says, with `import/progress.md`), per FR-006 (partial)
