# Contract: the kit changes

What each PR changes in `kit/`, as obligations; the wording is the implementer's, held to `.docs/development.md`
§ Writing prompts.

## PR 1: links and recall

| File | Change |
|---|---|
| `kit/templates/campaign/player-options.md` | The Predecessor line goes; `## Continuity` with its three lines and comments goes after `## Rules` (data-model.md). |
| `kit/migrations/005-continuity-links.md` | New (R13, R14). `## Before the backup`: run `dmc campaign standard <predecessor>` when Predecessor names a folder; on `behind`, migrate it first with this skill; on an open state, the warning that playing the earlier campaign on would make its story and the sequel's disagree, the close question (saying closing is permanent), the close through `dmc-close-campaign`, naming the predecessor, with its step 2 skipped and `.active-campaign` unchanged, or "are you sure?", then go on or stop with `Migration stopped until the previous campaign is closed. You can close it by running: /dmc-close-campaign <predecessor>`. `## Steps`: add the section, delete the Predecessor line, account. |
| `kit/skills/dmc-campaign-full-save/SKILL.md` | The campaign line: `adventures/<name>/`, where `<name>` is in `adventures/.active-campaign`, "or is the campaign the skill that called this one names" (R13), so the close the migration runs saves the predecessor without switching the active campaign. |
| `kit/skills/dmc-migrate-campaign/SKILL.md` | § 2: after the yes, follow each pending migration's `## Before the backup` before § 3; a stop there changes nothing. |
| `kit/skills/dmc-new-campaign/SKILL.md` | § 2: the Predecessor bullet becomes the Continuity bullet; Continues only to a campaign `dmc campaign standard` reports `closed` (else offer `dmc-close-campaign`); then `dmc campaign links <folder>`, relaying its `notice:` lines. |
| `kit/skills/dmc-resume-campaign/SKILL.md` | § 2 item 1: the Predecessor sentence goes. § 2, where `dmc size <campaign> --check` already runs: a `notice:` line is told to the player in one sentence. No command call is added. The resume still reads no other campaign. |
| `src/Dmc.Cli/Commands/SizeCommand.cs` | `--check` prints the linked set's notices after its flags, each ending `(not the full save's to trim)`, so the full save's rule for its lines is unchanged (contracts/campaign-links.md). |
| `kit/skills/dmc-close-campaign/SKILL.md` | § 5: "whose Predecessor line names this campaign" becomes "whose Continues line names it". |
| `kit/dm-rules.md` | Accuracy item 1: "the earlier campaigns its Predecessor line ... names" becomes the linked campaigns and digests; one clause that a `(side)` or `(world)` hit is a fact of the world, not known to this campaign's characters unless its files say so, and a `(continues)` hit is known to those who were there. At most one line more (FR-020). |
| `src/Dmc.Cli/Campaign/MigrationChecks.cs` | `continuity-section`. |

## PR 2: the sequel import

| File | Change |
|---|---|
| `kit/skills/dmc-new-campaign/SKILL.md` | New `## 1c. A sequel` (R15): confirm closed; ask which characters carry over; `sequel --start`, then write them on the index's Carried line; one subagent per part, in order, each told: its part's files and ranges, to read the closed campaign only through them, the digest's shape (data-model.md), to copy lines word for word under one subject, to add session-only facts with `sN.M`, to keep secrets in `secrets.md` with outcomes (a sheet's own `## Secrets` lines included, under the character's name), to copy a living character's stat block under their heading and account a dead one's as `dropped:stat-block`, what to leave out (FR-014), and, for the parts holding them, the brief's targets in the sequel (FR-007), and to write the carried characters' sheets by following `dmc-character-sheet-update` (FR-008), without restating its rule; `sequel --list` and the accounting; `sequel --check`; `sequel --sample`, one subagent with only the digest, the comparison, a redraw after any fix; progress in `import/progress.md`; the first scene only after the sample passes. The description gains "or to continue a closed campaign as a sequel". |
| `kit/skills/dmc-character-sheet-update/SKILL.md` | One clause (FR-008, FR-008a), the rule's one home: a character from a linked campaign, carried at a sequel's import or returning later, is recalled or looked up in the digest (or that campaign) first, and the sheet built from their history before defaults. |
| `kit/templates/campaign/` | No change: a digest is created by `--start`, not copied from templates. |

## PR 3: legacy files

| File | Change |
|---|---|
| `kit/templates/legacy.md` | New: the header, the Written line (`- **Written**: session N, yyyy-MM-dd`), `## What changed in the world`, guidance in a comment (what to put: changes others would see or hear of, in the world's terms, with `sN.M`; what not: secrets, the party's private plans; the 3k budget). |
| `kit/skills/dmc-campaign-full-save/SKILL.md` | One clause: when `dmc size <campaign> --check` prints `legacy due:`, rewrite `legacy.md` from `templates/legacy.md` before step 7's check; fold any legacy read since the last save into canon, marked as world knowledge unless the characters saw it, and record its session on `game-state.md`'s `- **Legacies folded**:` line (`<campaign> sN`, comma-separated). |
| `kit/skills/dmc-resume-campaign/SKILL.md` | § 2's size-check sentence gains: for each `legacy:` line, read that file and no other file of that campaign. |
| `kit/skills/dmc-close-campaign/SKILL.md` | None: the closing save follows the full save, so it writes `legacy.md` too. |
