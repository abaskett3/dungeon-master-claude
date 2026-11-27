# Contract: the rows in `dmc context-check` (FR-005)

A part of the hook's output after the canon reminder, printed only in a campaign with `records/`. It never fails: on
any error it prints nothing (`ContextCheckCommand.Quietly`). Capped at 2,400 bytes; past the cap, person blocks are
cut before word lines.

## Word lines (each message)

For each row word in the player's message (whole, ignoring case, longest first), one line with every row of that word:

```
"Da": Grumbald to Benno; Ilvane Vell to Benno (as a joke, when she bosses him about)
"Nan": Nettlebone to herself, never Pim Kettering (s4.3)
```

## Person blocks (once per scene)

A header with the scene's key (an 8-hex hash of the Present text) and the persons due, then one block each:

```
Called, scene 3fa91c0d (who says what for whom here; dmc records show for more): Pim Kettering; Hessa Kettering
- Pim Kettering says: "Mam" for Hessa Kettering (when she is frightened); "Cap" for Ilvane Vell
- Hessa Kettering: "Mam" (Pim Kettering); "Hessa" (anyone)
- Pim Kettering, limits: Cannot swim yet. (s2.5)
```

The names are invented (the write-path test's fixture).

A person is due when present (the Present line or Combat tracker) or named (a word whose rows all mean them), and no
header since the log's last compaction carries this scene key and their name.

## What it replaces

In a campaign with `records/`, the canon reminder finds named persons through the index (research R6), so a bare
title such as "Auntie" is no longer dropped.
