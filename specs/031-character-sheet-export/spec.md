# Feature Specification: Printable character sheets, `dmc sheet` (issue #191, phase 1)

**Feature Branch**: `claude/project-thread-p5cvi9` (spec 031)

**Created**: 2026-11-26

**Status**: Draft

**Input**: Issue #191, "Character sheet export: dmc sheet writes a structured, spoiler-free, printable sheet from the
campaign files". The maintainer approved starting it and chose, on 2026-11-26, to ship it in two phases: this one,
the command over the files as they are today; and a second, the changes to what the files hold.

## Background

In a long campaign the player asked for sheets twice. Both times DMC answered from memory at a very large context
(230,777 and 294,375 tokens), read no file, and gave an abridged sheet; spoilers were kept out by its judgement alone
(#191, Why). #191 plans a `dmc` command that writes the sheet from the files instead. It was written before play cards
(spec 013, migration 006): a companion's fight numbers now live only on its card, which #191's plan to split the NPC
stat block into one field per fact would contradict, and moving facts the PC has not learned into Secrets needs the
maintainer's campaign to judge.

## Clarifications

### Session 2026-11-26

- Q: One PR or phases? → A: Two phases (the maintainer's choice on the thread). Phase 1 here: the command, the check,
  the page, the skill section, and the one template field the eighteen skills need. Phase 2, later, in #191: the
  knowledge rule in `kit/dm-rules.md`, one field per fact on the stat block and card, the companion template rule, a
  migration that moves unlearned facts into Secrets and writes `Other skills` for every sheet, and `dmc sheet`'s line
  for a campaign behind that standard.
- Q: Where do a companion's numbers come from? → A: The play card, which is their only copy (migration 006 step 3).
  A box whose field the card holds instead prints "on the play card" when the field is gone; the card's lines print as
  their own boxes.
- Q: Does a Party status line off its template refuse the export? → A: No: it warns and is not printed. The real
  campaign has several such lines, and which become template fields is phase 2's question.
- Q: Which companion fields are spoilers? → A: Who, the names DMC voices them with, their motives (Toward the PC,
  what they want and fear, their attitude), relationships, ideals, backstory, aims and history, and on the NPC
  template Role in the story, Usually found and First appeared. Their looks, voice, appearance, personality, numbers,
  gear and wardrobe print.
- Q: The slip test failed at point A. What changes? → A: A hook line, not `kit/dm-rules.md` (at its cap): for any
  message that says "sheet", `dmc context-check` names `dmc sheet` (FR-019), and the skill's description names a plain
  request for sheets. Point A runs again on the maintainer's computer before the fix merges.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The party's sheets in one step (Priority: P1)

The player asks for the party's sheets. DMC runs `dmc sheet --party`, which prints one line, and gives the player the
page (or sends it when the player is remote). The page has every box of a 5e sheet, the same order every time.

**Independent Test**: the contract tests on the fixture campaign; the slip test's fork point A (below).

**Acceptance Scenarios**:

1. **Given** a campaign with a PC and two companions, **When** DMC runs `dmc sheet --party`, **Then** one page holds
   three sheets, each on its own printed page, and a Party status block with no sheet is named as skipped.
2. **Given** the same, **When** the page is searched for any text of Secrets, What they know or a companion's motives,
   **Then** none is found.

### User Story 2 - A sheet out of shape is fixed, not exported wrongly (Priority: P1)

A sheet has a line the template has no place for, or a blank field. `dmc sheet` refuses, with one line per thing to
fix; DMC fixes the sheet with Read and Edit and runs it again.

**Independent Test**: `AFileOutOfShapeIsRefusedWithOneLineForEachThingToFix`.

### User Story 3 - One companion's sheet (Priority: P2)

The player asks for one companion's full sheet. DMC runs `dmc sheet NAME`. An NPC outside the party is refused.

**Independent Test**: the slip test's fork point B; `AnNpcOutsideThePartyIsRefused`.

### Edge Cases

- A name that matches two sheets' titles: refused, asking for the file name.
- A companion whose card holds its numbers: those boxes say "on the play card".
- A written value the arithmetic does not give (a feature, a magic item): a warning; the written value prints.
- A PC sheet from before this change has no `Other skills`: the check names it, and DMC writes it.
- A closed campaign: exported like any other; nothing is written under `adventures/`.

## Requirements *(mandatory)*

- **FR-001**: `dmc sheet [NAME ...] [--campaign NAME] [--party] [--history] [--open] [--check] [--folder PATH]`, a class
  in `src/Dmc.Cli/Commands/` listed in `CommandTable.All`, the work in `src/Dmc.Cli/Sheets/`.
- **FR-002**: No name: every `character-sheets/pc-*.md`. `--party`: every `### ` block under Party status but "With the
  party, not companions". Names and `--party` do not mix.
- **FR-003**: A name or a block matches a sheet only by its exact title or file name, ignoring case. A name that is not
  a PC's or a party member's sheet is refused in one line.
- **FR-004**: The campaign is `--campaign` or the active one, found as `dmc size` finds it.
- **FR-005**: One self-contained HTML file with print styles, a sheet per printed page, readable on a phone, at
  `exports/sheets/<campaign>/` in the DMC folder: `<sheet>.html`, `party.html` or `sheets.html`, replaced by the next
  export of that name. `--open` opens it.
- **FR-006**: Output as `dmc campaign verify`: a written export prints its warnings and one line; `--check` prints its
  lines and exits 0; a refused export prints its lines on stdout, one line on stderr, exit 1; at most 12 lines;
  argument mistakes one line, exit 2.
- **FR-007**: The PC template gains `- **Other skills** (every other skill, with total bonus):` after Skills.
- **FR-008**: Three fixed layouts (PC; companion on the NPC template; companion on the PC template), each a fixed list
  of panels and boxes; an empty box prints `none`; the Now panel is the Party status block; the heading has the
  Scene's in-world date and the export's date. History only with `--history`, and only a PC's.
- **FR-009**: Each box reads one place: a labelled field, a free-form section, the ability table, the attack table, the
  two skill fields (the grid), or a Party status field.
- **FR-010**: The structure check refuses: a sheet on neither template; a companion with no play card; a section not on
  the template; in a structured section, an unknown label, an unlabelled line, a field twice, a subsection other than
  Combat's Attacks; a printed field missing or blank (a companion's card-held fields may be missing); the ability
  table's rows incomplete; no attack table; the eighteen skills not each written once with a bonus.
- **FR-011**: A test maps every label of `pc-sheet.md`, `npc-sheet.md` and the Party status block to a box or to a
  reason it is not printed.
- **FR-012**: The arithmetic of modifiers, saves, skills and passive Perception is checked and only warns.
- **FR-013**: A Party status line off its template warns and is not printed; a missing block warns.
- **FR-014**: The allow-list: only the places the layouts name print. Never Secrets, What they know or the fields
  of the Clarifications.
- **FR-015**: `dmc-character-sheet-update` gains § Showing a sheet to the player and a few words in its description.
- **FR-016**: No new package; everything from a file is escaped (`Py.HtmlEscape`) before any markup.
- **FR-017**: Tests: contract (in-process and binary), error-path rows, the label map, the golden party page, the
  Markdown and number readers, and a Playwright check of the printed page.
- **FR-018**: Documents: `.docs/dmc-reference.md`, `campaign-files.md`, `architecture.md`, `user-guide.md`,
  `development.md`, `roadmap.md`, `decisions.md`, and the README.
- **FR-019**: After the phase 1 slip test failed at point A (a plain request for the party's sheets never reached the
  skill's § Showing a sheet to the player), `dmc context-check` prints one line last for a message that holds `sheet`
  or `sheets` as a whole word and is not a command. The line names `dmc sheet` (the player characters),
  `dmc sheet --party` and `dmc sheet NAME`, says to give the path it prints and never to type a sheet out; its wording
  is in `.docs/dmc-reference.md` § context-check. The skill's description says "asks for sheets or to see one (the
  party's or one character's)". Tested in `ContextCheck/SheetRequestReminderContract.cs`.

## Success Criteria *(mandatory)*

- **SC-001**: The slip test of #191 passes (below).
- **SC-002**: `kit/dm-rules.md` is unchanged; the skill grows by under 700 bytes.
- **SC-003**: All tests pass; no field outside the allow-list reaches the page.

## The slip test (fixed before any run)

Fork point A: the player asks for the party's sheets. Fork point B: the player asks for one companion's full
character sheet. Both from the campaign the issue measured. 5 runs per point per arm, control (`main`) and treatment
(this branch) at the same time. Pass: in all 10 treatment runs DMC runs `dmc sheet` and types no sheet out in its
reply, and no treatment reply holds a spoiler; and the treatment slips no more often than the control over the same
runs.

**Result and rerun.** The run on 2026-11-26 (on #191) failed at A: 0 of 5 treatment runs ran `dmc sheet`; B passed
5 of 5. The FR-019 fix is tested by running point A again, 5 runs per arm, control `main` and treatment the fix's
branch, at the same time. Its pass line: all 5 treatment runs run `dmc sheet` and type no sheet out, no treatment reply
holds a spoiler, and the treatment slips no more often than the control.

## Assumptions

- The slip test needs the maintainer's Claude Code logs and file history, so it runs on the maintainer's computer
  (`.docs/development.md` § The slip test).
- Phase 2 keeps #191 open; this PR says `Part of #191`.
