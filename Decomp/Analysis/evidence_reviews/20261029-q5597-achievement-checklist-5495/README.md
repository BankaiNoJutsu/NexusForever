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

## Rejected Character save and retry

4 November 2026. The encrypted turn-in fixture installs a SQLite trigger on
the Q5597 completion write and invokes `Player.SaveDirect`. The save surfaces
the trigger rejection and marks the original Quest manager for reload. The
packet trace captures both `ServerItemAdd` messages for Item2 29664 and 81917
and one completed `ServerQuestStateChange` before the save. It records zero
achievement updates before and after rejection.

The fresh Character roster contains the achieved quest and zero reward or
achievement rows. After the fixture removes the trigger, a fresh connection
can turn in again. The two resulting item GUIDs differ from the abandoned
grants, and the next connection reloads the committed rewards once. The test
asserts rollback and safe retry. Its trace records the early packets without
requiring them as future behavior.

All 80 scoped cases passed. The raw TRX and build record are under
`artifacts/continuation-20261104/q5597-save-rejection/`. This covers a direct
Character save on the loopback fixture. Ordinary item and quest-state notices
still need a commit boundary. Other reward kinds, asynchronous save failure,
deployed sockets and retail presentation remain separate gates.

## Commit-gated client turn-in

4 November 2026. The World request for an ordinary quest now retains the
original session's synchronously produced packets. The normal Player save
blocks input and releases the batch after its Character commit. The dialog
closes after release. Rejected saves discard the batch and retire the actor
through the existing reload and disconnect path. Session, queue, account
epoch and inventory ownership are checked before release.

The Q5597 loopback regression stops after the turn-in dispatch and before the
queued Character save. Both the success and SQL rejection cases see the
achieved database state and no item, quest-state, achievement or dialog-end
packet at this boundary. After success the two item grants and completion
arrive, and reload retains the saved identities. After rejection the original
session closes and a fresh session can complete the quest once.

The first broader run passed 323 cases. Its two loopback cases timed out
because the test held Auth on the same thread that processes requests. The
corrected fixture pauses between World ticks. The corrected scoped run passed
all 33 cases. The test results and assembly hashes are under `artifacts/continuation-20261104/quest-turn-in-publication/`.

This gate covers the requesting session's ordinary client turn-in notices.
Server-triggered completion, cross-player script effects, full-stack world
entry and retail presentation remain separate.

## Commit-gated starter tutorial recovery

4 November 2026. Starter tutorial recovery runs achieved receiverless
turn-ins and the resulting quest-chain recovery through one Character save.
Packets produced synchronously for that session release after commit. A
busy save defers recovery on the original session queue. A failed save
retires the actor without releasing the retained notices.

The loopback World regression invokes the actual Player recovery entry point
for an achieved Exile movement quest with fixture item and achievement
rewards. While the write is held, neither completion nor follow-up
acceptance appears on the socket and the database still has only the
achieved quest. A rejected Character update rolls back both quest rows and
the reward rows. A fresh session can recover once and receives both quest
updates after commit. Reload retains the rewards and active follow-up.

The final scoped build and all 31 selected tests passed, including three
Contract save-order controls. Results and binary hashes are under
`artifacts/continuation-20261104/tutorial-turn-in-publication/`. The first
broader attempt hit fixture setup errors and then exhausted free space during
Contract SQLite seeding. It did not produce a complete test result.
Cross-player and delayed script effects, deployed provider behavior and
native tutorial gameplay remain open.

## Quest level publication

4 November 2026. Ordinary quest XP can change the real Player level inside
the turn-in transaction. Its level stat now reaches nearby players and the
Character/Group/Friendship broker route only after the Character save.
The original observer must still own the same session and be visible on
the same map. A superseded level update is dropped.

The loopback request uses an injected 120 XP prize and an actual Player XP
manager. At the held save the actor is level 2, SQL remains at level 1, and
the recording observer sessions and broker publisher have no level update.
SQL rejection leaves both routes silent. A fresh retry commits XP and level
once and publishes the new level to the current observer and broker.
Changing the other observer's account session epoch before commit prevents
publication to that observer. The recording endpoints establish delivery to
the World session and broker producer boundaries. The live broker, new
observer bootstrap, fanfare and native gameplay remain unqualified.

The final six loopback cases passed. The preceding scoped run passed the
other 46 cases and exposed two XP fixture setup failures. The scoped test
results and assembly hashes are under
`artifacts/continuation-20261104/quest-level-publication/`.

## Quest level-up fanfare

4 November 2026. XP earned during ordinary quest completion retains the
level-up cast until the Character save succeeds. The existing tier lookup
and spell pipeline start the cast if the original player is still in the
same map admission. This also keeps delayed spell work from starting before
the save. A rejected save or a replacement map admission drops the fanfare.

The XP loopback exercises the actual spell-start producer with a synthetic
level-two fanfare entry. It observes no start at the held save or after
rollback, then sees the start on the requesting socket and the visible
observer route on a successful retry. A separate case advances the map
admission before commit and confirms that XP and level persist without the
stale fanfare. The fixture qualifies spell-start publication. Native
rendering, the retail fanfare effect payload and deployed behavior remain
open.

All 21 scoped tests passed. The results and binary hashes are under
`artifacts/continuation-20261104/quest-fanfare-publication/`.
