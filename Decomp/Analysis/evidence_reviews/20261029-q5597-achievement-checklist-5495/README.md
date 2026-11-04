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

The source/SQLite capacity and reload case is now covered by the follow-up
below. The remaining gates require additional evidence.

- Native Q5597 turn-in/reward presentation and achievement toast/log/journal UI.
- Reward inventory and achievement state across a full-stack relog with
  Auth, character selection, and actual world data. The loopback fixture
  below covers the World packet and persistence path.
- Native duplicate completion and missing-prerequisite/receiver observations.
- Local Q5596 → Q5597 → Q5604 end-to-end behavior, including objective activation,
  route guidance and spawn/despawn timing.

The separate user-confirmed world-entry smoke is not evidence for these cases.
Computer Use remains stopped. No quest-specific native UI action was performed
by this recovery review. `manual_validation` remains pending and all affected
content rows remain `not_retail_complete`. The strict source-existence validator
is unchanged.

## Fixed reward capacity and SQLite reload

4 November 2026. The original turn-in checked both fixed rewards against
the same free space. The one-slot reproduction reached Completed even
though the separate item grants needed two slots.

Ordinary Quest completion now checks the fixed items and chosen item as
one batch on the real inventory. Repeated rows for the same item share
the same satchel and compatible stack capacity. All items share the bag's
unreserved slots. The capacity check accounts for the whole stacks and
partial stack space that completion will reclaim, using the same pushed
item rows as the turn-in. If the batch cannot fit, completion returns the
existing inventory-full result before reclaiming quest items or granting
rewards.

A real Player, QuestManager, Inventory and CharacterAchievementManager
exercise the visible-receiver turn-in using fixture rows for Q5597 and
its documented rewards 29664 and 81917. The one-slot case keeps the
quest achieved with no reward items or achievement. With two slots,
ordinary Character save persists both items and completion bit 1
for checklist 5495 on achievement 4134. Fresh actors reconstructed from
the SQLite rows retain both outcomes. The completed actor rejects a
duplicate turn-in after reload.

Review found that the first candidate could refuse a turn-in that would fit
once a pushed item was removed. A synthetic pushed-item variant exercises
that existing path. Separate capacity controls verify partial and full
reclamation and shared satchel headroom.

The owning build and 244 scoped tests passed. Test execution took 17 seconds.
Evidence is in `artifacts/continuation-20261104/q5597-reward-batch/`.
The baseline's two-slot failure used the wrong checklist assertion
field. Plain checklist bits persist in ProgressState, while
CreditedChecklistMask tracks rows for checklist-count achievements.
The final cases assert ProgressState and the completion timestamp.
The earlier fixture build and missing-array failures are retained separately.

## Loopback turn-in and fresh World session

4 November 2026. Two local TCP connections use the real `WorldSession`
encryption and message dispatch. The first sends `ClientQuestComplete`
for an achieved Q5597 and receives both item grants. Socket disconnect
runs the logout manager and Character cleanup. A fresh roster query
loads the same two item GUIDs and stack counts, completed quest, and
achievement 4134 with checklist mask 2 and its completion timestamp.

The second connection receives those exact item identities in
`ServerPlayerCreate`, the completion in `ServerQuestInit`, and the saved
checklist and timestamp in `ServerAchievementInit`. A duplicate request
returns the completed state, and the final database read confirms the
same item rows and achievement timestamp.

The fixture supplies the achieved quest, visible receiver, prepared player
and bootstrap boundary. It uses disposable SQLite Auth and Character schemas.
The first attempt exposed a missing fixture XP table. A later run passed 48
related cases and exposed the fixture's missing Auth storefront table. The
corrected Q5597 run passed all 20 scoped cases. Logs and hashes are under
`artifacts/continuation-20261104/q5597-loopback/`.

Full-stack Auth and character selection, physical map entry, native UI, and
the complete Q5596 → Q5597 → Q5604 chain still need separate evidence.
