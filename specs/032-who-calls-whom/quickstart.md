# Quickstart: checking spec 032

What proves the feature works, in the order the build runs it. The steps marked **this computer** need the real
campaigns under `adventures/`, which only the maintainer's computer has; they never run in CI and never on the live
campaign.

## In CI (invented fixtures only)

- `dotnet test` runs the contract tests of `dmc records` ([contracts/dmc-records.md](contracts/dmc-records.md)), the
  rows in `dmc context-check` ([contracts/context-check-rows.md](contracts/context-check-rows.md)), the record rules
  ([data-model.md](data-model.md)), and migration 010's golden fixture (`tests/Dmc.Tests/Fixtures/migrations/010/`).
- The write-path test's 20 changes, replayed through `dmc records edited` with a stand-in hook input: each bad change
  refused with its one line, each good one passing.

## On this computer

1. **Migrate a copy** (spec SC-005). Copy the long campaign and the campaign it continues to a scratch DMC folder made
   with `artifacts/bin/dmc install`. Bring the copy to standard 9, then run migration 010 with DMC. Every conflict
   (`dmc records check <campaign> --draft`) goes to the player. #237's rulings are the first data. Then
   `dmc campaign verify <campaign>` passes.
2. **Records the player checks**: `dmc records show <campaign> Hala Tam Kole`.
3. **The probes** (SC-001), each as a `UserPromptSubmit` input to `artifacts/bin/dmc context-check` in the copy:
   "Hala runs to Nytha." gives Tam; "Kibil asks Auntie about it." gives Kole, family only; "What do we know about
   Mother?" gives the hag of the earlier campaign; "Nytha?" prints all three rows.
4. **The check** (SC-002): the draft's findings include the four known cases; a hand check of every finding (20% or
   fewer false); errors planted in a second copy measure the misses.
5. **Tokens** (SC-004): the hook's output on every player message of the long campaign's play logs, with and without
   the rows part, as real tokens; the mean extra per play turn.
6. **The slip test** (SC-003), with the pass line, fork points and runs fixed on #263, against a control at the same
   time.
