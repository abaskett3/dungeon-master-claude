# Implementation Plan: The Harptos calendar, with a year on every date

**Branch**: `claude/037-harptos-calendar` | **Date**: 2026-11-28 | **Spec**: [spec.md](spec.md)

## Summary

The kit states the Harptos calendar and the date's form. A new `dmc calendar` command drafts a change list from a
campaign's files by anchor dates and applies the lines the player approved; it is the one writer allowed in a closed
campaign, on the player's `--yes`, with a backup before and a new closing backup after. Migration 012 drives it.

## Technical Context

- C# on .NET 10, Native AOT (`src/Dmc.Cli`); MSTest (`tests/Dmc.Tests`). No new package.
- Depends on spec 036 (migration 011, the full-date rule with the Harptos months in `RecordDates.Months`). This branch
  is rebased on `main` once spec 036 merges; until then it holds the spec, plan and tasks only.
- The read-only prototype of the draft is `D:\Claude\037-calendar\calscan.py` (outside the repository); its output
  for both campaigns, `changes.md`, is what the player reviewed on 2026-11-28.

## Design

### The calendar (`src/Dmc.Cli/Calendar/Harptos.cs`)

- The twelve months of 30 days and the five festival days in order, Shieldmeet after Midsummer in a year divisible by
  four; a date as (year, day of the year); a date's text, "the 12th of Eleint, 1493 DR", or "Midsummer, 1493 DR".
- Weekdays are not computed (the player's ruling keeps seven-day weeks; a weekday in the text moves unchanged).
- `RecordDates.Months` reads its list from here.

### The old calendar and the anchors (`src/Dmc.Cli/Calendar/OldCalendar.cs`, `Anchors.cs`)

- The campaigns' 13 months of 28 days (the twelve names and Harvestmoon before Nightal), and "the first month",
  "the second month", "the Nth day of the new year".
- A **span** is a range of old dates with an anchor and a year: an old date's count of days from the anchor is placed
  the same count from the anchor's Harptos date. The spans of these two campaigns come from `calendar.json` in the
  campaign folder, which the migration writes from the player's rulings (below), so `dmc` holds no campaign's facts.
- A line of one campaign about the other (the digests; a sequel fact in a closed campaign's canon, "(Rorn's Landing
  s28.1)") takes the other campaign's span.

`calendar.json` for Rorn's Landing (The Silver Egg's has the first span only):

```json
{
  "spans": [
    { "until": "the 5th of the second month", "year": 1493, "anchor": "the 1st of Hammer", "harptos": "the 1st of Hammer, 1493 DR", "nightal": 1492 },
    { "from": "the 6th of the second month", "anchor": "the 4th of Flamerule", "harptos": "Midsummer, 1493 DR" }
  ],
  "renames": { "Harvestmoon": "Uktar", "the harvest festival": "Highharvestide", "the fall festival": "Highharvestide" }
}
```

### The draft (`dmc calendar draft <campaign>`)

- Reads every `.md` and `.json` file of the campaign except `import/`, `backups/`, `exports/` and `maps/`, finds each
  old date (a day or range with an old month, with or without its weekday, and no year after it), and writes
  `backups/<campaign>.calendar-draft.md`: the counts, the rules, then one line a change (file:line, old, new) in
  groups (span, session logs, holiday words, rulings). Writes nothing else; a closed campaign is not refused, since
  `backups/` drafts are outside what `verify` compares.
- A date it cannot place (a Nightal of neither span, Harvestmoon in a calendar line) goes in "a ruling" with `?`.

### The run (`dmc calendar apply <campaign> --yes`)

- Refuses unless the draft has no `?` left and its header says `approved: <date>` (the player's word, written by the
  migration step that asked).
- A closed campaign: `verify` against its closing backup first (it must match); then a backup,
  `backups/<campaign>_calendar_<stamp>.zip`; then each change, a line at a time, refusing a line whose old text is
  not where the draft says (nothing written for that campaign); then a new closing backup pair
  (`backups/<campaign>_closed_<stamp>.zip` and its copy in `exports/`), and `.closed` rewritten to name it. The old
  closing backups stay (nothing prunes them). This is the only command that writes in a closed campaign, and it
  writes only these files.
- An open campaign: the usual migration backup, then the changes.
- Prints the accounting: dates found, changed, kept with a reason, ruled.

### The kit

- `kit/dm-rules.md` § Time and distance: the in-world date in the form "Wednesday the 12th of Eleint, 1493 DR";
  festival days by name; a pointer to `kit/calendar.md` (new: the months, festival days, Shieldmeet, the weeks, DR).
- `kit/templates/campaign/game-state.md`: the date line's comment shows the form with a year.
- `dmc-new-campaign`: the first in-world date has a year.
- `kit/migrations/012-harptos-calendar.md`, standard 12: ask the player for each span's anchor and year (an open
  campaign) or apply the rulings recorded in its `calendar.json`; run the draft; put the groups and every `?` to the
  player; write `approved:`; run `apply`; rewrite the canon calendar lines and the lines the player ruled; check.

### Docs

`.docs/campaign-files.md` (`calendar.json`, the closed-campaign exception, the new backups), `dmc-reference.md`
(`dmc calendar`), `decisions.md` (Harptos, seven-day weeks, anchors, the exception and its reason), `roadmap.md`.

## Constitution check

- The player rules every change before a file changes (FR-004); nothing is assumed silently: the open points are
  listed in the spec.
- A closed campaign is changed only with the player's explicit grant (recorded in the spec), with backups before and
  after, and stays closed.

## Slip test

Migration 012 changes campaign files and `kit/dm-rules.md`, so it counts. The two campaigns are closed and no
campaign is in play; the date rule's effect on play can be tested only in a later campaign. The PR will say so and
what was run instead: the draft and the run on scratch copies of both campaigns, with the accounting checked.
