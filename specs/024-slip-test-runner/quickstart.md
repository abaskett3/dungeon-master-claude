# Quickstart: the pilot and the base rate (SC-001 to SC-003)

On the maintainer's computer, with Claude Code signed in, the logs and file history kept (#239), and the copy folder
trusted (#238). Nothing below goes into git.

## Q1. The pilot

1. Make a work folder, for example `D:\Claude\DMC\slip-test\pilot\`.
2. Pick four player messages from play logs, two with a known slip in the reply and two without. Write `points.tsv`
   (`id<TAB>session id<TAB>message uuid`).
3. Write `key.tsv` with one row per point: what would count as a slip there.
4. Copy the present `kit/dm-rules.md` to `arms/control.md`, and the same file with one harmless sentence changed to
   `arms/treatment.md`.
5. Write `trial.json` with `mode: fork`, the folders, `runs: 1`, and the two arms.
6. Run, from the repository:

   ```
   dotnet run --project src/Dmc.SlipTest -- prepare --work D:\Claude\DMC\slip-test\pilot
   dotnet run --project src/Dmc.SlipTest -- run --pilot --work D:\Claude\DMC\slip-test\pilot
   dotnet run --project src/Dmc.SlipTest -- pilot --work D:\Claude\DMC\slip-test\pilot
   ```

7. If a check fails, read the named type in the fork log and the original log. Fix the runner, or, when the
   difference cannot reach the reply, add it to `allow.tsv` with its reason. Rerun until the verdict is pass.
8. Post `pilot.md` and the cost lines of `runs.tsv` as a comment on #249.

## Q2. The base rate

1. A new work folder with about 50 fork points from many play sessions, `runs: 2`, one arm `control` (the present
   kit rules), no `pass`.
2. `prepare`, `run`, `export`.
3. The scorer fills in `export/scores.tsv` from `export/replies.md` and `key.tsv` only.
4. `score`. Post the control line of `result.md` and the cost line on #249.
