# Quickstart: A wardrobe and keepsakes section on sheets

1. `dotnet test` passes, including `MigrationChecksTests` (wardrobe cases) and
   `SheetsKeepAWardrobeAndAChangeOfClothesIsASwap`.
2. `kit/dm-rules.md` is unchanged: `git diff origin/main -- kit/dm-rules.md` is empty.
3. On the maintainer's computer: run the slip test of issue #286 (fork points A and B, 5 runs per point per arm,
   control and treatment at the same time) and report it against the pass line in the PR.
