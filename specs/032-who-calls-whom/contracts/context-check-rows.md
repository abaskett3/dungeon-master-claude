# Contract: the rows in `dmc context-check` (FR-005)

A part of the hook's output after the canon reminder, printed only in a campaign with `records/`. It never fails: on
any error it prints nothing (`ContextCheckCommand.Quietly`). Its own cap is 2,400 bytes: word lines first, then the
person blocks. Every due person's "says" line and limits line (T057) come before the rest of any block and may take the part past its cap, up
to what the hook's whole output (9,500 bytes) has left; the rest of each block is printed whole while the part stays
within its cap (T055, found by SC-003's run, 2026-11-27: in one pass a long block, most of it what others call that
person, left no room for the next, and one risk speaker's block was never printed).

## Word lines (each message)

For each row word in the player's message (whole, ignoring case, longest first), one line with every row of that word:

```
"Da": Grumbald to Benno; Ilvane Vell to Benno (as a joke, when she bosses him about)
"Nan": Nettlebone to herself, never Pim Kettering (s4.3)
```

## Person blocks (once per scene)

A header with the scene's key (an 8-hex hash of who is present, in name order, so an HP change in the Combat tracker is not a new scene; T056) and the persons due, then one block each:

```
Called, scene 3fa91c0d (who says what for whom here; dmc records show for more): Pim Kettering; Hessa Kettering
- Pim Kettering says: "Mam" for Hessa Kettering (when she is frightened); "Cap" for Ilvane Vell
- Hessa Kettering: "Mam" (Pim Kettering); "Hessa" (anyone)
- Pim Kettering, limits: Cannot swim yet. (s2.5)
```

The names are invented (the write-path test's fixture).

A person is due when present or named, and no header since the log's last compaction carries this scene key and
their name. Present: the Present line or Combat tracker names them as a message would (in full, or by a word whose
rows all mean them), outside any text in round brackets, where play notes who is away ("(Benno at the mill)").
Named: the message names them so. The header names a person when any line of theirs is printed; a person whose rest
did not fit is not shown it again in this scene.

## What it replaces

In a campaign with `records/`, the canon reminder finds named persons through the index (research R6), so a bare
title such as "Auntie" is no longer dropped.
