---
name: dmc-migrate-campaign
description: Bring a campaign's files up to the current standard after an update, with a backup first and a check that nothing was lost. Use when the player asks to bring their files up to date, or accepts the offer resume makes when a campaign is behind.
---

# Migrate a campaign

A migration moves a campaign's files to the shape the current rules expect, following the numbered files in
`migrations/`. `dmc` does the exact parts (where the campaign stands, the backup, the check that nothing was lost, the
way back); you do the reading and rewriting, and the player decides.

## 0. Not now

Do not start in the middle of a fight, with a roll pending or with a question to the player unanswered: say so in one
line and finish that first. Do not edit `.standard`, anything in `migrations/`, or anything in the campaign's
`backups/` folder except the accounting file this skill names.

## 1. Where each campaign stands

Take the campaign the player named, or every folder in `adventures/` whose name does not start with a dot if they asked
for all. For each, run `dmc campaign standard <campaign>`:

- `current`: say it is up to date, and go to the next campaign.
- `closed: ...`: say it is closed and is never migrated, and go to the next campaign.
- `ahead: ...`: say this `dmc` is older than the one that wrote the files and the player should update it (in a
  terminal, outside play); do not migrate.
- `unfinished: <backup file>`: an earlier migration stopped part way. Say so, take no new backup, and go straight to
  step 4 against that backup.
- `behind: ...`: it lists the pending migrations. Go on.

## 2. Ask

Read each pending `migrations/NNN-*.md` it listed. Show the player each one's `## What changes`, and spell out each
class under its `## Declared drops` (what may be removed and why). Ask once whether to go ahead, and wait. A no
changes nothing.

## 3. Back up

Run `dmc campaign backups <campaign>`. If its last line starts `ask:`, put that question to the player in plain words,
once. Then run `dmc campaign backup <campaign>`, adding the flag the `ask:` line names (`--prune oldest` or
`--prune old`) only if the player said yes. If the backup fails, say so and stop: nothing has changed.

The backup's file name is in the line it prints. The accounting file is the same name with `.accounting.tsv` added,
in the same folder: `adventures/<campaign>/backups/<backup file>.accounting.tsv`.

## 4. Migrate

Follow each pending migration in order, oldest first: its `## Steps` in order, writing new text before deleting the
old, and applying its `## Judgments`. Show the player anything a judgment says to ask about, and wait. Change no file
outside the migration's `touches`. The steps are safe to repeat, so a migration that was interrupted is simply run
again.

A file whose shape a step does not recognise (hand-edited, say) is left unchanged: name it to the player and add a line
to the accounting file, with tabs between the fields: `skipped`, the file's path in the campaign, and the reason.

## 5. Account

Run `dmc campaign verify <campaign> --list`. Each `unaccounted:` line is a line of the old files that is not in the new
ones; add `--all` if it ends with a count of more. For each, add a line to the accounting file, with tabs between
the fields: the id it printed; `reworded` and an anchor (at least 8 characters copied from where the content now is),
or `dropped:<class>` with a class from the migration's `## Declared drops` and a short reason. If a line was lost by
mistake, put it back instead. A `check` line means a file is not yet in the new shape: finish that step. Run it again
until it lists nothing but the files you skipped.

## 6. Verify

Run `dmc campaign verify <campaign>`.

- **It passes** (`verified ...`): tell the player the files are up to date, where the backup is, and that
  `dmc campaign restore <campaign> --yes` puts them back as they were. Then run `dmc size <campaign> --check`. Say each
  line it prints, in a sentence, as something the next full save will trim, not as a problem with the migration;
  nothing printed means nothing to say. If it fails, say so in one line: the migration still stands.
- **It fails**: name what it lists in plain words, say the backup is untouched, and offer
  `dmc campaign restore <campaign>` (it shows what would change; `--yes` does it). Do not go on with play until the
  player has chosen to restore, or to let you fix what it lists and verify again.

If the session ends part way, the next one finds `unfinished` in step 1 and carries on against the same backup.
