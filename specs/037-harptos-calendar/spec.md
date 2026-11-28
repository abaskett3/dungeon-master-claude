# Feature Specification: The Harptos calendar, with a year on every date

**Feature Branch**: `claude/037-harptos-calendar` (spec 037)

**Created**: 2026-11-28

**Status**: Draft (specify; the player's rulings below; no issue filed yet)

**Input**: The player's rulings of 2026-11-28, made while ruling on spec 036's ASSUMED choices: every date needs a
year, so memories can tell one year's Midwinter from the next ("Shale is soon to have another midwinter in
riverhold"); "Lets fully adopt the harptos calendar"; and the holiday and anchor rulings recorded under Clarifications.
Spec 036 (`claude/036-sheet-facts`, T039) already makes `dmc` refuse a record's date without a Harptos month and a
year.

## Background

Both campaigns, The Silver Egg and Rorn's Landing, are closed (`.closed`; closing backups of 2026-11-13 and
2026-11-26). They use a calendar of the player's and DMC's making, not Harptos:

- **13 months of 28 days** (The Silver Egg `story/canon.md:176`, set in s19; Rorn's Landing `story/canon.md:267`).
  Rorn's Landing named them with Harptos's twelve and a thirteenth, **Harvestmoon**, before Nightal. The Silver Egg
  wrote "the first month" and "the second month" after the New Year.
- **Seven named weekdays**; in Rorn's Landing "the 28th is always a Saturday".
- **No year** anywhere.
- **Midsummer** is the 4th of Flamerule; **Midwinter** is "the longest night, the equivalent of the 22nd of December"
  (s41.2); the **harvest festival** fell in Harvestmoon; **New Year** was a festival on the night of the 28th of
  Nightal; **quarter day** is rent day.
- The Silver Egg counted **Day N** in its files: Day 1 was the 10th of Nightal (s19), Day 52, its last, "Thursday the
  5th of the second month".
- Rorn's Landing began on "Saturday the 28th of Kythorn" (session 1) and closed on "Wednesday the 18th of Eleasis".

Uses counted read-only on 2026-11-28 (both campaigns, whole folders): Harptos month names 1,138, weekday names
1,077, Harvestmoon 8, Midsummer 141, Midwinter 39, New Year 6, "first/second month" 62, tendays 0.

Harptos has twelve months of 30 days and five festival days that belong to no month (Midwinter after the 30th of
Hammer, Greengrass after Tarsakh, Midsummer after Flamerule, Highharvestide after Eleint, the Feast of the Moon after
Uktar), with Shieldmeet after Midsummer every fourth year. Years are counted DR.

## Clarifications

### Session 2026-11-28 (the player's rulings)

- Q: Which calendar? → A: Harptos, fully: its months and festival days, years in DR.
- Q: Tendays or seven-day weeks? → A: Seven-day weeks, Monday to Sunday, running straight through festival days
  ("going to 10 days would be weird"; kept on the player's condition that holidays and birthdays cause no later
  problems: `dmc` computes no weekday).
- Q: Harvestmoon? → A: It is not a Harptos month. It becomes **Uktar**, the month in its place before Nightal.
- Q: New Year? → A: The 1st of Hammer.
- Q: Midsummer? → A: Harptos's Midsummer, the festival day after the 30th of Flamerule.
- Q: Midwinter? → A: Harptos's Midwinter, the festival day after the 30th of Hammer (the player: more like Christmas
  than the winter solstice of the 20th of Nightal). An earlier ruling to call it the winter solstice is withdrawn.
- Q: The fall festival / harvest festival? → A: Highharvestide.
- Q: Quarter day? → A: Rent day, not a holiday: the 1st of every month.
- Q: Other holidays? → A: None move. Greengrass, the Feast of the Moon and Shieldmeet join the calendar with no story
  change.
- Q: The years? → A: The Silver Egg is **1492 DR** (Shale met Kibil in 1492) into 1493 after its New Year; Rorn's
  Landing is **1493 DR**.
- Q: How does an event move? → A: By an **anchor date** that both calendars have: count the event's days before or
  after the anchor in the old calendar, and place it the same count from the anchor in Harptos. The Silver Egg
  counts from the 1st of Hammer (New Year); Rorn's Landing from Midsummer.
- Q: The birthdays? → A: Moved by the 1st of Hammer. Checked in the session logs (`session-001.md` scene 4,
  `session-008.md` scene 13, `session-021.md`) and the module (*Peril in Pinebrook*, the hatching cavern): Kibil,
  and Vaelorn and Miira (the two unhatched eggs, hatched as the party spoke with Rorn), on Day 1; Hala on Day 3,
  about 8:23 PM, a minute after Mother died. In Harptos: the 12th and the 14th of Nightal, 1492 DR.
- Q: The canon lines Midwinter's move breaks? → A: "Hala was only days old at the last one" becomes "Hala was so
  young at the time"; "the year's-end holidays" (s80.12) becomes "the midwinter holidays".
- Q: Rorn's Landing's dates? → A: All move by the Midsummer anchor, the dedication feast and the broken Midsummer
  with them.
- Q: Where does it go? → A: Its own spec and migration (012), with a backup of each of the two closed campaigns.

- Q: The 41 days? Harptos puts Midsummer 41 days later in the year than the old calendar (the 213th day, not the
  172nd), so the two anchors stretch the time between The Silver Egg's end and Rorn's Landing's start from 135 days
  to 176. → A: Accepted: the wyrmlings are about nine months old in Rorn's Landing, not "about seven".
- Q: Midwinter 1493 falls on the road home: The Silver Egg's Day 49 (the leave-taking from Riverhold,
  `session-046.md` scene 8) is the 30th of Hammer, so Day 50, the first day on the road, is Midwinter, with Hala in
  Riverhold with the Birches. → A: Accepted.

- Q: The gap between the campaigns (a date after The Silver Egg's end and before Rorn's Landing's start, such as the
  first flight "on the 19th of Ches")? → A: It counts from Midsummer, as Rorn's Landing does, so its weekdays agree
  with Rorn's Landing's and the 41 days fall just after The Silver Egg's end: the first flight becomes the 25th of
  Tarsakh.
- Q: Kole's Fall Festival, "the Autumn Solstice, also called the Harvest Festival"? → A: "Highharvestide, also called
  the Harvest Festival" (Harptos's autumn equinox is the 21st of Eleint, nine days before).
- Q: The Bench's New Year sitting "(a Tuesday: the 28th is Saturday, the 1st Sunday)"? → A: The aside goes; the
  sitting stays on the 3rd of Hammer.
- Q: The quarter-day lines ("she pays the factor on quarter day")? → A: Kept as written; canon states that quarter
  day is rent day, the 1st of every month.
- Q: May the migration change the two closed campaigns? → A: Yes, as an exception the player grants: "This is the
  exception to the rule. The DMC must comply. Approved by me, the creator of this project and lone player of the two
  campaigns." A closed campaign stays closed afterwards; only the run of this migration, on the player's word, may
  write in one.

### Open (to put to the player)

- **A weekday reference** in canon so DMC counts weekdays from one known day. Proposed: "The 1st of Hammer, 1494 DR,
  is a Tuesday" (Rorn's Landing's last day, Wednesday the 12th of Eleint, plus 111 days).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The player reviews every change before a file is rewritten (Priority: P1)

The player asks for the calendar conversion. DMC writes a list of every date and holiday line it would change in
both campaigns, old and new, grouped so the player can rule on a group at once, and the lines that need a ruling
apart. Nothing is rewritten until the player approves.

**Why this priority**: the conversion changes canon across two closed campaigns; the player asked to see it first.

**Independent Test**: the list for the two campaigns names each line's file and line, the old text and the new;
the campaign folders are byte-identical before and after it is made.

**Acceptance Scenarios**:

1. **Given** The Silver Egg's "Day 1 was the 10th of Nightal", **Then** the list shows "the 12th of Nightal, 1492
   DR".
2. **Given** Rorn's Landing's "Wednesday the 18th of Eleasis", **Then** the list shows "Wednesday the 12th of Eleint,
   1493 DR".
3. **Given** a line naming Harvestmoon, Midwinter, Midsummer or the harvest festival, **Then** it is in the group of
   its holiday, with the new text.

### User Story 2 - The conversion runs with backups and accounts for every line (Priority: P1)

On the player's approval DMC backs up each campaign, rewrites the approved lines, and accounts for every date it
found: changed, unchanged with a reason, or ruled by the player.

**Independent Test**: after the run, no line holds Harvestmoon, "the first month" or "the second month", a date
without a year, or a Midsummer on the 4th of Flamerule; each campaign has a backup from before the run; the count of
dates found equals the count accounted for.

**Acceptance Scenarios**:

1. **Given** the closed Silver Egg, **When** the conversion runs, **Then** a backup is made first and the closing
   backup is left as it is.
2. **Given** a date the player ruled to keep, **Then** it gains its year only.

### User Story 3 - Play dates every day in Harptos with its year (Priority: P1)

In a later campaign DMC writes the in-world date as "Wednesday the 12th of Eleint, 1493 DR", names festival days by
their names, and never writes a 29th of a month as the last day or Harvestmoon.

**Independent Test**: the kit's date rule names the months, the festival days and the year; a new campaign's
`game-state.md` date line has a year.

### Edge Cases

- A date written with a weekday keeps its weekday: counting from an anchor moves every date of a span by the same
  number of days.
- A range ("the 8th to the 10th of Flamerule") moves as one.
- A date that falls on a festival day by the count is written as the festival ("Midwinter, 1493 DR"), not as a day
  of a month.
- A quotation of the player (`player: "…"`) is not rewritten; it gains a note of the Harptos date, if any.
- A Rorn's Landing line about a Silver Egg date (the digests, "Silver Egg Day 6") moves by The Silver Egg's anchor.
- A date years back ("22 years ago", the caravan "third week of Harvestmoon") is renamed and given its year (1470
  DR), not counted.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001 (the calendar in the kit)**: The kit states the calendar: Harptos's twelve months of 30 days, its five
  festival days and Shieldmeet, seven-day weeks Monday to Sunday that run through festival days, years in DR, and a
  date's form, "the 12th of Eleint, 1493 DR". `kit/dm-rules.md` and the templates point to it; `dmc-new-campaign`
  sets a year in the first in-world date.
- **FR-002 (the anchors)**: An event's new date is its old date's count of days from its span's anchor, placed the
  same count from that anchor in Harptos: The Silver Egg from the 1st of Hammer 1493 (its Day N is the 12th of
  Nightal 1492 plus N − 1 days), Rorn's Landing from Midsummer 1493.
- **FR-003 (the holidays)**: A named holiday becomes its Harptos day (Clarifications), whatever its count. Quarter
  day is rent day on the 1st of every month.
- **FR-004 (the change list)**: Before any rewrite, the conversion writes the list of User Story 1 outside the
  campaign folders and stops for the player.
- **FR-005 (the run)**: On approval, a backup of each campaign, then the approved changes, then an accounting of every
  date found. A closed campaign stays closed; its closing backup is never changed.
- **FR-006 (the canon lines)**: The canon lines the player ruled (Clarifications) are rewritten as ruled; the calendar
  lines of both canons are replaced by the Harptos calendar with each campaign's years.

### Key Entities

- **Span**: the dates a campaign's play covers, with its anchor. The gap between two spans has none.
- **Change**: one line's old text and new, its file and line, and its group (a holiday, a span, a ruling).

## Success Criteria *(mandatory)*

- **SC-001**: Every date line in both campaigns is accounted for: changed, kept with a reason, or ruled.
- **SC-002**: The player approves the list before any file changes, and the run changes only approved lines.
- **SC-003**: After the run, the full-date rule of spec 036 finds no date without a Harptos month and a year in the
  records of either campaign.

## Assumptions

- Spec 036 merges first; migration 012 follows its migration 011.
- The session logs and canon are where the dates are; imported transcripts (`import/`) are kept as they are, a
  record of what was said.

## Out of Scope

- Moon phases, seasons' weather and the Realms' lore of each festival.
- Converting a date said aloud in a quotation of the player.
