# Feature Specification: Asset Packs: Battlemaps, Token Icons, Region Maps, Art and Handouts

**Feature Branch**: `008-module-assets`

**Created**: 2026-11-10

**Status**: Draft

**Input**: Tracked by issue #183, opened when this spec was written. User description: "next item on the list is maps,
icons, and module provided assets. The crooked moon provides a bunch of assets. like maps (with and without grids) and
character icons. We need a way for our map drawer to take those into consideration or for the player to at least assign
icons and load them. typically these are asset packs."

## Background

A published adventure often comes with **asset packs**: archives of pictures made for playing it on a screen. The
maintainer owns four for *The Crooked Moon*, the module of their next campaign. They were looked at for this spec (the
archives' file lists, and a sample of the pictures):

| Pack | Size | What it holds |
|------|------|---------------|
| Digital Map Pack | 638 MB | 55 battlemaps, each in eight files: with the pack's grid drawn on it or without, in colour or (for 39 of them) black and white, as JPEG and as WebP. 7 region maps and 2 cutaways, with no grid. |
| Digital Token Pack | 127 MB | 151 round portrait tokens and 94 top-down tokens, each as PNG and as WebP. |
| Digital Artwork and Handout Pack | 467 MB | Art of 64 NPCs, 95 monsters, 52 items, 66 scenes, the species and classes; 11 adventure handouts as pictures; printable handouts, GM tools and player sheets as PDFs. |
| Digital Spell Scroll Pack | 2 MB | Two files. |

What the files themselves say, without anyone looking at a picture:

- **A battlemap's name gives its place and its size in squares**: `G_12_1_The_Crooked_House_1_21x24.webp` is map 1 of
  chapter 12, 21 squares by 24, with the grid (`GL_` without it, `G_BW_` black and white). Every battlemap sampled is
  140 pixels to a square, so the two whose names carry no size (`G_19_8_Balefire_Chamber`, 3500 by 4340 pixels) still
  give it: 25 by 31.
- **Battlemaps are much larger than the maps DMC draws.** DMC keeps a map "under about 30 cells wide"; the pack's run
  from 11 by 8 to 73 by 56 squares (10,220 by 7,840 pixels, 10 MB as a JPEG).
- **A token's name gives its creature and its size**: `C_Kackle_Medium.png`, `C_Tall Man_Huge.png`,
  `C_NPC_Raum_Large.webp` and `C_NPC_Raum_Medium.webp` (one NPC at two sizes), `TD_Kackle_medium_undead.webp` (a
  top-down token, with the creature's type). The top-down set has no NPCs.
- **Names are spoilers**: `G_18_4_Den_of_the_Blighted_One`, `BOSS_Crimson_Abbot_Transformed.png`,
  `G_19_7_Tomb_of_the_Shrouded_platform`. So are the pictures: a battlemap shows every room of a place at once, and a
  cutaway shows every level of a mine.
- **Archives carry clutter**: a `__MACOSX` folder and `.DS_Store` files, about as many entries as there are pictures.

Today DMC can use none of it:

- **A map is a grid of symbols.** DMC describes a place as a small file (walls, floor, doors, the creatures on it);
  `dmc map` checks the file, draws it in flat colours and prints the distances
  (`kit/skills/dmc-draw-map/map-format.md`). The file has no way to name a picture.
- **A module's maps reach play as DMC's own sketches.** The ingest skill looks at each map page of the book and writes
  an ASCII sketch for DM use; a player-facing map is drawn later from it, showing only what has been seen
  (`kit/skills/dmc-ingest-module/SKILL.md`).
- **A creature is a coloured circle with one or two characters on it** (`@`, `K`, `1`).
- **Nothing shows the player a picture that is not a map**: no portrait when an NPC walks in, no handout when the
  character is handed a letter.
- **A remote player** (the maintainer plays this way) cannot see the browser page; DMC sends them the map as a picture.

This feature lets a player add asset packs to their DMC folder and has DMC use them: a pack's battlemap as the picture
of a fight's map, its tokens as the creatures' icons, its region maps, art and handouts shown when the character would
see them. It also lets the player give any creature an icon of their own choosing, their own character first of all,
with or without a pack.

## Clarifications

### Session 2026-11-10

- Q: Which of the packs' contents does this spec cover? → A: All four kinds: battlemaps, token icons, region maps and
  cutaways, and art and handouts.
- Q: A pack battlemap shows the whole place, and DMC cannot tell walls from floor without looking at it. How does DMC
  work with one? → A: The player sees the pack's picture. DMC traces walls, doors and terrain into its usual symbol
  grid once; the tracing is saved, is not drawn, and is what placement checks, DMC's knowledge of the room and the
  covering of unseen areas rest on.
- Q: How does a creature get its icon? → A: DMC matches a creature to a pack token by name and size and uses it; the
  player can assign or change any icon, their own character's included; a creature with no match keeps the lettered
  circle.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Add an asset pack (Priority: P1)

The player has downloaded a pack for the module they are about to play. They tell DMC where the archive or folder is
and which module it belongs to. DMC brings the pictures into the DMC folder and writes a catalogue of them: what each
picture is, what in the module it belongs to, and how large each battlemap is. From then on DMC knows what the pack
offers without looking through it again.

**Why this priority**: Every other story draws on the catalogue. Without it DMC would have to list and look at hundreds
of pictures during play, which costs the player context and time in every session.

**Independent Test**: Add the four Crooked Moon packs to a DMC folder that holds the ingested module. The catalogue
lists every battlemap, token, region map, cutaway, piece of art and handout once, with the battlemaps' sizes in squares
and each picture tied to a chapter, area, creature, NPC or item of the module where its name allows. Nothing was shown
to the player, and a resume of a campaign reads nothing new.

**Acceptance Scenarios**:

1. **Given** a module in `modules/` and a pack archive elsewhere on the computer, **When** the player asks DMC to add
   the pack to that module, **Then** the pictures are in the module's folder, a catalogue lists each of them once, and
   DMC reports in a few lines how many of each kind it found, with no names of places, creatures or people.
2. **Given** a pack that offers the same picture as JPEG and as WebP (or PNG and WebP), **When** it is added, **Then**
   the catalogue has one entry for that picture and the DMC folder holds one file for it, the smaller.
3. **Given** a battlemap offered with and without the pack's grid, in colour and in black and white, **When** the pack
   is added, **Then** the catalogue has one battlemap with those variants, not four.
4. **Given** a battlemap whose file name gives its size in squares, **When** the pack is added, **Then** the catalogue
   records that size; **Given** one whose name does not, **Then** the size is worked out from the picture and the
   pack's other maps, and recorded as worked out.
5. **Given** an archive that holds system clutter, a file that is not a picture or a document, or an entry whose path
   leads outside the place the pack is unpacked to, **When** it is added, **Then** the clutter is left out, the other
   file is listed as skipped, and the entry leading outside is refused and nothing is written outside the pack's folder.
6. **Given** a pack already added, **When** the player adds it again (the same archive, or a newer one), **Then**
   pictures already there are not duplicated, new ones are added, and what DMC and the player have recorded about a
   picture (a tracing, an assignment, a correction) is kept.
7. **Given** a campaign with no module, **When** the player adds a pack or a single picture to that campaign, **Then**
   it is catalogued the same way and is available to that campaign.

---

### User Story 2 - Fight on the pack's battlemap (Priority: P1)

The party walks into a place the pack has a battlemap for. When positions start to matter, the player's map is the
pack's picture of the place, with DMC's lettered columns and numbered rows along its edges, the creatures standing on
it, the turn order beside it, and only the part of the place the character has seen. DMC still knows where the walls
and doors are, still refuses to stand a creature inside a wall, and still reads every distance from `dmc`.

**Why this priority**: This is the request: the map drawer taking the module's maps into account. The module's own
battlemap is also the most accurate picture of the place there is, more accurate than a sketch DMC made from a page of
the book.

**Independent Test**: With the Map Pack added, start a fight in a place the pack covers (a carriage of the Ghostlight
Express). The player's map shows the pack's art with coordinates that line up with the art's squares, tokens in the
right squares, and the distances `dmc` prints agree with counting squares on the picture. Moving a creature into a
wall square is refused.

**Acceptance Scenarios**:

1. **Given** the party is in a place the catalogue has a battlemap for, **When** DMC draws the map, **Then** the
   player sees the pack's picture with DMC's coordinates, and each of DMC's squares covers one square of the picture.
2. **Given** a battlemap DMC has not used before, **When** it is first needed, **Then** DMC looks at the picture,
   traces its walls, doors, windows, stairs, water, hazards, cover and difficult ground into a layout, checks the
   tracing against the picture, and saves it with the module, so that no later fight, session or campaign traces that
   map again.
3. **Given** a traced battlemap, **When** DMC puts a creature in a wall square, off the map, or on another creature,
   **Then** the map is refused in one line as it is today, and nothing is drawn.
4. **Given** a place with rooms the character has not seen, **When** the map is drawn, **Then** those rooms are
   covered, opaque, in what the player sees, and are uncovered in the turn they are described.
5. **Given** a secret door or a hidden room on the pack's picture, **When** the map is drawn before it is found,
   **Then** nothing of it shows, and the covered area's shape does not outline it more than an ordinary unseen area
   would.
6. **Given** a battlemap larger than fits a readable picture (73 by 56 squares), **When** the map is drawn, **Then**
   the player sees the part in play at a readable size, the coordinates are those of the whole map, and they stay the
   same as the view moves.
7. **Given** the player says the tracing is wrong ("that is a doorway, not a wall"), **When** DMC corrects it,
   **Then** the correction is saved with the tracing and holds in every later use.
8. **Given** the pack offers the map with and without its own grid, **When** the map is drawn, **Then** the player
   sees one grid, not two laid over each other.
9. **Given** a player whose options ask for text maps, or a machine where `dmc` is unavailable, **When** a fight
   starts on a place the pack covers, **Then** DMC draws the text map from the tracing, as it does today from its own
   layout.
10. **Given** a place the pack has no battlemap for, **When** a fight starts there, **Then** DMC draws its own map
    exactly as it does today.

---

### User Story 3 - Creatures wear the pack's tokens (Priority: P2)

A Kackle steps out of the corn. On the map it is the pack's Kackle token, not a circle with a `1` on it. DMC found the
token by the creature's name and size, without being told. Three Kackles are three of the same token that the player
can still tell apart, and the lettered circle remains for anything the pack has no token for.

**Why this priority**: Icons make the map readable at a glance and are the second half of the request, but a fight on
the pack's battlemap works with lettered circles.

**Independent Test**: With the Token Pack added, put a Kackle (Medium), the Tall Man (Huge), the NPC Raum and a
creature the pack lacks on any map, a pack battlemap or one DMC drew. The first three show their tokens at the right
size, the fourth a lettered circle, and the panel beside the map names all four.

**Acceptance Scenarios**:

1. **Given** a creature whose name and size match a token in the catalogue, **When** it is put on a map, **Then** it
   is drawn with that token, without the player asking.
2. **Given** a token offered at several sizes (Raum, Medium and Large), **When** the creature is put on a map,
   **Then** the token of the creature's current size is used.
3. **Given** several creatures with the same token, **When** they are on the map, **Then** each carries its own mark,
   and the player can tell them apart on the map, in the panel and in the turn order.
4. **Given** a creature with a token, **When** it is drawn, **Then** the player can still see at a glance whether it
   is their character, an ally, an enemy or neutral, whether it is down, and its status note.
5. **Given** a creature with no matching token, **When** it is drawn, **Then** it is the lettered circle of today, and
   DMC does not put a different creature's token on it.
6. **Given** a creature whose true nature the character does not know (a villager who is something else), **When** it
   is drawn, **Then** its icon is that of what the character takes it for, and changes in the turn the truth is
   revealed.
7. **Given** the player options prefer top-down tokens and the pack has both styles, **When** a creature is drawn,
   **Then** its top-down token is used, and a creature with only a round token uses that.
8. **Given** a map at a scale above 5 ft, where several creatures share a square, **When** it is drawn, **Then**
   tokens are used where they stay recognisable and the rule for crowded squares holds as today.

---

### User Story 4 - The player assigns an icon (Priority: P2)

The player has a picture of their character and wants it on the map. They give DMC the file; from then on their
character is that picture in every fight of the campaign. The same works for any creature: a companion, an NPC with no
pack token, a creature whose automatic match the player dislikes. It works in a homebrew campaign with no pack at all.

**Why this priority**: It is the fallback the request names ("or for the player to at least assign icons and load
them"), and no pack has a token for the player's own character.

**Independent Test**: In a campaign with no module and no pack, give DMC a picture for the player's character and
start a fight. The character's token is the picture, in this session and after a resume, and in an export of the
campaign imported elsewhere.

**Acceptance Scenarios**:

1. **Given** a picture file anywhere on the computer, **When** the player asks DMC to use it for a creature, **Then**
   the picture is kept with the campaign and that creature is drawn with it on every map from then on.
2. **Given** an assignment made in one session, **When** the campaign is resumed, exported and imported, or backed up
   and restored, **Then** the assignment and its picture are still there.
3. **Given** a creature with an automatic match, **When** the player assigns something else, or asks for the lettered
   circle, **Then** the player's choice wins from then on.
4. **Given** a picture that is not square, is very large, or has no transparent corners, **When** it is assigned,
   **Then** it is fitted to the token without being stretched and stays a clear token at map size.
5. **Given** a file that is not a picture DMC can draw, **When** the player assigns it, **Then** DMC says so in a line
   and the creature keeps what it had.
6. **Given** the player asks what tokens the pack has for a creature the character has met, **When** DMC answers,
   **Then** it names only tokens of creatures and people the character has met.

---

### User Story 5 - Region maps and cutaways (Priority: P3)

The party unrolls a map of Druskenvald, or the player asks where Wickermoor Village lies. DMC shows the pack's region
map, with a mark where the party is. A cutaway of a place is shown when the character could have that view of it.

**Why this priority**: Region maps orient the player between fights and cost little once pictures can be shown, but no
fight depends on them.

**Independent Test**: With the Map Pack added and the party in Wickermoor Village, ask for the map of the region. The
player sees the pack's region map with the party's place marked. Ask for the cutaway of a mine the party has not
entered: DMC does not show it.

**Acceptance Scenarios**:

1. **Given** a region map of where the party is, **When** the player asks for the map of the region, or the character
   comes by such a map, **Then** the player is shown the pack's picture.
2. **Given** DMC knows where on that picture the party is, **When** it is shown, **Then** the party's place is marked;
   **Given** it does not, **Then** the picture is shown without a mark, never with a guessed one.
3. **Given** a region map or cutaway that shows something the character does not know (a labelled lair, the lower
   levels of a mine), **When** it would be shown, **Then** DMC does not show it and draws or describes what the
   character does know instead.
4. **Given** a region map on the screen, **When** DMC states a distance or a travel time, **Then** it comes from the
   module and the campaign's files as it does today, not from measuring the picture.

---

### User Story 6 - Art and handouts (Priority: P3)

The character meets Father Renathyr, and the player sees his portrait. The character is handed a planchette, and the
player sees the handout. Later the player asks to see the letter again and gets it. Art appears when the character
sees the thing, never before, and as often as the player likes it to.

**Why this priority**: It adds atmosphere and the handouts the module's authors meant players to see, but the game
plays without it, and it carries the largest spoiler risk of the six stories.

**Independent Test**: With the Art and Handout Pack added, play to a first meeting with an NPC the pack has art for
and to a scene where the module gives the party a handout. The portrait appears with the reply that introduces the
NPC, the handout with the reply in which it is handed over, and a later "show me that letter again" shows it again.
No picture of anything not yet met was shown or named.

**Acceptance Scenarios**:

1. **Given** art of an NPC, monster, item or scene in the catalogue, **When** the character first sees that thing and
   the player options allow it, **Then** the player is shown the art with that reply, once.
2. **Given** a handout the module gives the party at a point in the story, **When** the character receives it,
   **Then** the player is shown it, and the campaign records that the party holds it.
3. **Given** handouts the party holds, **When** the player asks to see one, or asks what handouts they have, **Then**
   DMC shows it or lists them, and lists no other.
4. **Given** art that shows more than the character has seen (a creature's transformed shape, a disguised NPC as they
   truly are), **When** the character meets the thing, **Then** that art is not shown until what it shows has happened
   in play.
5. **Given** a fight with the map on the screen, **When** art or a handout is shown, **Then** the map is still there
   for the player afterwards without DMC drawing it again.
6. **Given** player options that say art is shown only when asked for, or never, **When** the character meets
   something the pack has art for, **Then** nothing is shown unprompted, or nothing at all.
7. **Given** a handout or player sheet that is a document, not a picture, **When** the character receives it,
   **Then** DMC tells the player where the file is and opens or sends it; a GM tool is never shown or offered.
8. **Given** art of a thing in the catalogue, **When** DMC describes that thing, **Then** the description agrees with
   the art, as it does today with the book's own illustrations.

---

### Edge Cases

- **The pack is for a different module, or for none.** A pack added to a module whose names it does not share
  catalogues with nothing tied to the module; DMC says so in a line and the pictures remain usable by name.
- **Two packs offer the same thing** (a token of the same creature from two packs): the pack added to the campaign's
  module wins over a general one, and the player's own assignment over both.
- **A battlemap's squares are not where the name says** (a border round the picture, a different number of pixels to
  a square). The alignment is checked when the map is traced; a map whose squares cannot be lined up is not used as a
  battlemap, and DMC draws its own.
- **A battlemap comes in layers or states** (`Tomb_of_the_Shrouded` as `all`, `base` and `platform`; a building's
  floors 1 to 4). Each is its own map of the same place; DMC shows the one that matches the state of play.
- **The place in play differs from the picture.** Play has changed it (a wall blown out, a door barred, a fire). The
  campaign's own changes are drawn over the pack's picture, and the tracing the module shares is not changed.
- **A map in the black-and-white set only**, or a creature in one token style only: what exists is used.
- **A creature changes size** in play (enlarged, a second form): the token of the new size if there is one, the same
  token scaled if not.
- **A token name that matches several catalogue entries** (two NPCs with one first name): no automatic match; the
  lettered circle until the player or the module's text settles it.
- **The pack is removed or its files deleted** after maps and assignments refer to them: the map falls back to DMC's
  own drawing from the tracing and the creature to its lettered circle, with one line saying what is missing; nothing
  is refused.
- **An imported campaign** refers to a pack the importing machine does not have: the same fallback.
- **A rewind** to before a handout was received or a room was seen: the handout is no longer held and the room is
  covered again.
- **A remote player** is sent pictures instead of seeing a page: every picture in every story reaches them as a file
  of a size that can be sent and read on a phone.
- **A very large archive** (638 MB) or a pack of thousands of files: adding it finishes, and reports progress so the
  player does not take it for hung.
- **A picture DMC must look at to trace is too large to look at whole**: it is looked at in parts.

## Requirements *(mandatory)*

### Functional Requirements

**Adding and cataloguing packs**

- **FR-001**: A player MUST be able to add an asset pack, given as an archive or a folder anywhere on their computer,
  to a module in their DMC folder, and to a campaign that has no module. A single picture MUST be addable the same way.
- **FR-002**: Adding a pack MUST copy its pictures and documents into the DMC folder, so that the pack keeps working
  when the original archive is moved or deleted. It MUST write nothing outside the pack's own folder there, whatever
  the paths inside the archive say, and MUST leave the original untouched.
- **FR-003**: Adding a pack MUST leave out system clutter (`__MACOSX`, `.DS_Store` and the like), keep one copy of a
  picture offered in several file formats, and list as skipped any file it cannot use.
- **FR-004**: Adding a pack MUST produce a catalogue with one entry per picture or document, giving its kind
  (battlemap, token, region map, cutaway, art, handout, GM tool, other), its variants (pack grid or none, colour or
  black and white, token style, size), and what in the module it belongs to (chapter, map or area, creature, NPC,
  item) where its name or folder allows.
- **FR-005**: A battlemap's entry MUST give its size in squares, taken from its name, or worked out from the picture
  and marked as worked out. An entry whose size cannot be established MUST say so, and that map is not used as a
  battlemap until it is.
- **FR-006**: Building the catalogue MUST NOT require DMC to look at the pictures: names, folders and picture sizes
  are enough for it, and a picture is looked at only when it is first used (FR-012, FR-031) or when a tie to the
  module is in doubt.
- **FR-007**: The catalogue MUST be a spoiler file like the rest of the module's folder: DMC MUST NOT recite it, list
  its entries, or name a picture of anything the character has not met, in a report of adding a pack or at any other
  time (FR-037).
- **FR-008**: Adding a pack again MUST add what is new, duplicate nothing, and keep every tracing, tie, correction and
  assignment already recorded.
- **FR-009**: The catalogue and the pictures MUST NOT be read when a campaign is resumed. During play DMC MUST be able
  to find what the catalogue holds for one place, creature, NPC or item without reading the whole of it.
- **FR-010**: A pack's files are copyrighted and personal. They MUST stay under folders git ignores, MUST NOT be part
  of a campaign export or backup, and the toolkit's repository, tests and documentation MUST NOT contain any of them.
  Tests use pictures made for the purpose.

**Battlemaps**

- **FR-011**: A map file MUST be able to name a catalogued battlemap as its picture. The player's map then shows that
  picture with DMC's lettered columns and numbered rows, the creatures, labels, status notes, turn order, scale and
  title drawn as they are on a map today.
- **FR-012**: Before a battlemap is first shown, DMC MUST trace it into a layout in the existing symbols (wall, floor,
  door, window, water, hazard, stairs, tree, pillar, low cover, difficult ground, empty), with the same number of
  squares as the picture, and MUST check the tracing against the picture by looking at the two together.
- **FR-013**: The tracing MUST be saved with the pack's module (or campaign, for a campaign's own pack), so that it is
  made once and used by every fight, session and campaign that uses the map. It MAY be made ahead of play, when the
  pack is added or before a session, as well as at first use.
- **FR-014**: The tracing is DM knowledge: it holds everything the picture shows, secret doors and hidden rooms
  included, alongside the area numbers of the module's key where DMC can place them. It MUST NOT be drawn for the
  player.
- **FR-015**: Every check `dmc map` makes today MUST hold on a map with a picture, using the tracing: a creature in a
  wall or empty space, off the edge, or sharing a square at 5 ft is refused in one line and nothing is drawn.
- **FR-016**: Each of DMC's squares MUST cover one square of the picture, to within a tenth of a square anywhere on
  the map. A battlemap that cannot be lined up to that MUST NOT be used as a map's picture.
- **FR-017**: The player MUST see one grid: where the pack offers the map without its own grid that variant is shown
  under DMC's lines, and where it offers only a gridded one DMC draws its coordinates along the edges and no second
  set of lines.
- **FR-018**: A map with a picture MUST show only what the character has seen. The campaign records which parts of
  the place have been seen; every other square is covered, opaque, in everything the player is shown or sent, and is
  uncovered in the turn it is described. A place with nothing to hide MAY be uncovered whole at once.
- **FR-019**: What the player is shown MUST NOT reveal a covered area by other means: no label, token, tooltip, title,
  file name or caption of something unseen, and the picture file DMC sends a remote player MUST hold nothing under the
  cover.
- **FR-020**: Distances MUST be measured and printed exactly as today, in the picture's squares at the map's scale. A
  pack battlemap is at 5 ft to a square unless the module says otherwise.
- **FR-021**: A battlemap of any size the packs ship (up to at least 80 by 60 squares) MUST be usable. When the whole
  map does not fit a readable picture, the player is shown the part in play, with the whole map's coordinates, which
  never change as the view moves. The guide of about 30 cells applies to maps DMC draws itself and not to these.
- **FR-022**: A campaign MUST be able to draw its own changes to a place over the pack's picture (a new opening, a
  blocked door, a labelled feature) without changing the shared tracing or the pack's file.
- **FR-023**: The player MUST be able to correct a tracing in words, and the correction MUST be saved with the
  tracing.
- **FR-024**: The player options MUST let the player choose colour or black-and-white battlemaps where a pack offers
  both, and whether pack battlemaps are used at all. With them off, or text maps on, or `dmc` unavailable, DMC draws
  the place from the tracing as it draws its own maps today.
- **FR-025**: The picture DMC itself looks at to check a map MUST stay small enough to cost no more context than a
  map check does today, whatever the size of the pack's file.
- **FR-026**: Where the catalogue has a battlemap for a place, DMC MUST use its tracing as the layout of that place in
  preference to a sketch made from the book's page, so that the place is the same in the story as on the map.

**Token icons**

- **FR-027**: A creature on a map MUST be drawable with a picture in place of the lettered circle, on a map with a
  pack picture and on a map DMC drew, at every scale.
- **FR-028**: When a creature is put on a map, DMC MUST look for a catalogued token whose name and size match it and
  use it without being asked. A name that matches nothing, or more than one thing, gets no automatic token.
- **FR-029**: The match MUST be made on what the character takes the creature for. A creature under a false identity
  or an unrecognised one MUST NOT be given the token of what it truly is until that is revealed in play.
- **FR-030**: A token MUST be drawn at the creature's size on the grid, and the picture is never stretched.
- **FR-031**: The player MUST be able to assign any picture file to any creature, replace an automatic match, and go
  back to the lettered circle. An assigned picture is copied into the campaign; DMC looks at it once to see that it
  reads as a token and fits it without stretching it.
- **FR-032**: Assignments, automatic and the player's, MUST be recorded in the campaign so that a creature keeps its
  icon across fights, sessions, a resume, an export and import, and a backup and restore. The player's own pictures
  travel with the campaign; a pack's do not (FR-010), and a campaign that arrives without its pack falls back as
  FR-036 says.
- **FR-033**: A creature drawn with a picture MUST still show which kind it is (the player's character, ally, enemy,
  neutral), whether it is down, its status note, and a mark that tells it from others with the same picture. The
  panel beside the map and the turn order MUST show the same picture and mark beside its name.
- **FR-034**: The player options MUST let the player choose the token style a pack offers (round or top-down) and
  whether pack tokens are used automatically. A creature the preferred style lacks uses the other.
- **FR-035**: Order of preference for a creature's icon: the player's assignment, then a token from a pack of the
  campaign's module, then one from the campaign's own packs, then the lettered circle.
- **FR-036**: A map that names a picture or token that is not there (a removed pack, an imported campaign) MUST still
  be drawn: from the tracing if there is one, with lettered circles, and with one line telling DMC what is missing.

**Pictures that are not fight maps**

- **FR-037**: DMC MUST show the player a picture from a pack only when the character has seen, met or been given what
  it shows, and MUST decide that from the campaign's files, not from the picture's name. Before showing any picture
  for the first time DMC MUST have looked at it and found nothing in it the character does not know; a picture that
  fails is not shown, and DMC does not say that one exists.
- **FR-038**: The player MUST be able to see a region map of where the party is, on request or when the character
  comes by one, with the party's place marked when DMC knows where on the picture it is, and unmarked otherwise.
- **FR-039**: A region map or cutaway MUST NOT be used to measure. Distances and travel times come from the module and
  the campaign's files.
- **FR-040**: When the character first sees an NPC, monster, item or scene the catalogue has art for, DMC MUST show
  that art with the reply that introduces it, once, unless the player options say art is shown only on request or
  never. Art is shown for what matters in the scene, not for every item in a list.
- **FR-041**: When the character receives a handout the module gives the party, DMC MUST show it, and the campaign
  MUST record that the party holds it. The player can ask to see a held handout again, and to list the handouts held.
- **FR-042**: A handout or player sheet that is a document MUST be offered to the player as a file (its place named,
  opened or sent). A GM tool MUST NOT be shown or offered; DMC may read it as it reads the module.
- **FR-043**: Showing a picture MUST NOT take a fight's map from the player: the map is there afterwards without being
  drawn again.
- **FR-044**: When DMC describes something the catalogue has art for, the description MUST agree with the art, under
  the rule the ingest skill already has for the book's illustrations.

**Every picture, every player**

- **FR-045**: A remote player MUST receive every picture this feature shows (a map with a pack picture and tokens, a
  region map, art, a handout) as a file sent to them, before the reply, as the map is sent today, at a size that
  sends quickly and reads on a phone.
- **FR-046**: Every failure MUST be one line that says what is wrong (a pack that is not an archive or folder, a
  picture that cannot be read, a battlemap with no tracing, a token file that is missing), and what `dmc` prints while
  adding a pack and drawing a map MUST stay small, as its output is today.
- **FR-047**: What a campaign holds changes with this feature (icon assignments, the parts of a place seen, the
  handouts held, the player's pictures). A campaign made before it MUST keep playing with no change to its files, and
  the change MUST come with a migration or a line saying why none is needed, as `.docs/development.md` § Changing a
  campaign-file standard requires.
- **FR-048**: The rules loaded into every play session MUST NOT grow for this feature beyond the lines that point at
  the skills that hold the procedures.
- **FR-049**: The documentation MUST be updated in the same change: the map format, the campaign and module file
  descriptions, the user guide (adding a pack, assigning an icon, the options), the reference, the architecture, the
  roadmap and the decisions.

### Key Entities

- **Asset pack**: a set of pictures and documents the player owns, added to a module or to a campaign. Has a name,
  where it came from, when it was added, and its files.
- **Catalogue**: the list of a module's or campaign's assets, one entry each: kind, variants, the file for each
  variant, what in the module it belongs to, and for a battlemap its size in squares. A spoiler file. Never read on a
  resume.
- **Battlemap**: a picture of a place drawn to a square grid. Has a size in squares, a scale, variants (pack grid or
  none, colour or black and white), sometimes layers or floors, and at most one tracing.
- **Tracing**: DMC's layout of a battlemap in the map symbols, square for square, with the module's area numbers and
  the player's corrections. DM knowledge; shared by every campaign that uses the map.
- **Seen area**: per campaign and place, which squares the character has seen. Decides what is covered.
- **Token**: a picture of one creature or NPC at one size in one style (round or top-down).
- **Icon assignment**: per campaign, which picture a creature is drawn with, and whether the player chose it or DMC
  matched it.
- **Region map, cutaway**: a picture of an area with no usable grid. May carry a mark of the party's place.
- **Art**: a picture of an NPC, monster, item, scene, species or class. Shown at most once unprompted.
- **Handout**: something the module means the players to see, a picture or a document. The campaign records which the
  party holds.
- **GM tool**: a document for the DM only.
- **Player's picture**: a file the player gave for a creature or added to a campaign. Kept with the campaign and
  travels with it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The four Crooked Moon packs (about 1.2 GB of archives) are added to a module in under 10 minutes, each
  with one request from the player, and the catalogue accounts for every picture and document in them: each is an
  entry, a variant of an entry, or on the skipped list.
- **SC-002**: At least 90% of the packs' battlemaps are tied to the right chapter of the module, and at least 90% of
  the tokens to the right creature or NPC, without DMC looking at a picture.
- **SC-003**: Resuming a campaign whose module has all four packs reads no more than resuming it without them.
- **SC-004**: On five pack battlemaps of different sizes, the coordinates the player sees line up with the art's
  squares to within a tenth of a square at all four corners.
- **SC-005**: On the same five maps, at least 95% of squares are traced as what a person reading the picture says
  they are (passable, blocked, door), and every door between two areas is traced.
- **SC-006**: A battlemap is traced once: the second fight on it, in the same or another campaign, starts with DMC
  looking at no picture of it.
- **SC-007**: In a play test of a fight on a pack battlemap, no creature stands in a wall, every distance DMC states
  matches the squares on the picture, and the turn order and tokens match the tracker in every reply.
- **SC-008**: In a play test across at least two sessions with all four packs, the player is shown or told the name of
  no picture of a place, creature, person or item the character has not met, and no covered part of a map shows
  through.
- **SC-009**: The player gives their character a picture with one request, and it is on the next map and on every map
  after a resume and after an export and import.
- **SC-010**: At least 9 in 10 creatures the Token Pack covers appear with their token the first time they are on a
  map, with nothing asked of the player.
- **SC-011**: A remote player receives every map with a pack picture as a file under 2 MB, readable on a phone,
  before the reply it belongs to.
- **SC-012**: A campaign made before this feature, and a player who adds no pack, see no difference: every map is
  drawn as it is today and every existing test of the map drawer passes unchanged.
- **SC-013**: Showing a map with a pack picture costs DMC no more context per reply than a map DMC drew itself.

## Assumptions

- **The player owns the packs and adds them themselves.** DMC downloads nothing and has no pack built in. The player
  could open any file in a pack; DMC's duty is not to show or name what the character has not met, not to lock the
  files away. This is the same footing `modules/` stands on today.
- **Packs live with the module**, beside its digests, because a module's assets are spoilers and are shared by every
  campaign of that module, as the digests are. A campaign's own pictures (the player's character, a homebrew
  campaign's pack) live in the campaign and are exported and backed up with it; large packs added to a campaign are
  the player's choice and make its export larger. A store of assets shared by every campaign and module is not part of
  this feature.
- **File names follow a pattern the catalogue can read** (kind, chapter, name, size), as the Crooked Moon packs' do.
  A pack with unhelpful names is still added; its entries are tied to the module by DMC looking, or by the player, as
  they come up.
- **Pack battlemaps are square-gridded and at 5 ft.** Hex maps, isometric maps and maps with no grid at all are shown
  as pictures (as a region map is), not fought on.
- **The spell scroll pack and similar printables** are catalogued as handouts or other; nothing special is done with
  them.
- **Animated maps, sound, music and video** in a pack are skipped.
- **Pictures are PNG, JPEG or WebP; documents are PDF.** Other formats are skipped and listed.
- **The image-prompt skill (spec 007) is separate.** It may later use pack art as a source for what a thing looks
  like; this feature does not change it.
- **Place sheets (issue #182) are separate.** A tracing is a layout of one battlemap, not a record of a location.
- **Showing a picture to a local player uses the browser**, as the map does, and a remote player gets a file, as the
  map does. How the page arranges a map and a picture is for the plan.
- **Art is shown at first meeting by default**, a preference the player options can change; a player who finds it too
  much turns it down there, and no toolkit rule sets a rate.
- **Tracing by looking at a picture is accurate enough** to play on once checked against the picture, and the
  player's corrections cover the rest. If the plan's research finds it is not, for the largest maps especially, the
  fallback is tracing only the part in play, and the spec's criteria SC-004 to SC-006 are measured on that.
