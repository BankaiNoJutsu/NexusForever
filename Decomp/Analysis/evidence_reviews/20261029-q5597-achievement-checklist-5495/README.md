# Q5597 achievement checklist 5495: current runtime review

Reviewed 29 October 2026. This is a new, versioned review and planning record.
The user confirmed that the historical June 19 `234445` CreateBundleOnly
worksheet is unavailable. Its original contents and checksum cannot be
recovered or authenticated. This record supersedes its current planning
references; it does not recreate that historical artifact.

## Evidence checked

The existing generator identifies its former reference as a **CreateBundleOnly
worksheet**. Its claim is a smoke plan, not a completed client UI observation.
The current harness was run with `-CreateBundleOnly`, world 870/objective 8256
and explicit pending UI/negative cases. It skipped server and client launch.
The generated manifest is retained here as `worksheet-manifest.json`.

A fresh run against the current built test assembly completed successfully:

```powershell
dotnet test Source/NexusForever.Game.Tests/NexusForever.Game.Tests.csproj --no-build --no-restore --nologo -v minimal --filter 'FullyQualifiedName~Q5597' --logger 'trx;LogFileName=q5597-recovery-review.trx'
```

**16 passed, zero failed, zero skipped.** Actual output and test results are
retained here as `tests.log` and `tests.trx`.

`QuestTests.QuestComplete_Q5597FixedRewardsGrantAllItemsAndUpdatesBloodstoneAchievement`
uses the real QuestManager/CharacterAchievementManager completion path with
visible Mondo Zax receiver 24187. It asserts both Item2 81917 and 29664 grants
from reward rows 3000/5236 and achievement 4134's completed checklist mask 2
(checklist 5495/bit 1), with prerequisite 18 satisfied. The focused set also
covers missing Mondo, objective activation guards and completion/follow-up
behavior. Its inventory/player contexts are test fixtures, not native UI.

The user-restored Crimson Isle videos are readable at the existing referenced
paths. Their durations match the earlier video ledger; restored checksums are
recorded in `Decomp/Analysis/VIDEO_EVIDENCE_MAP.md`. Those historical videos
corroborate retail quest completion/follow-up and do not prove current emulator
achievement UI or inventory persistence.

## Still required

- Native Q5597 turn-in/reward presentation and achievement toast/log/journal UI.
- Persisted reward inventory and achievement state across relog.
- Duplicate completion and the listed missing-prerequisite/receiver negatives.
- Local Q5596 → Q5597 → Q5604 end-to-end behavior, including objective activation,
  route guidance and spawn/despawn timing.

The separate user-confirmed world-entry smoke is not evidence for these cases.
Computer Use remains stopped. No quest-specific native UI action was performed
by this recovery review. `manual_validation` remains pending and all affected
content rows remain `not_retail_complete`. The strict source-existence validator
is unchanged.
