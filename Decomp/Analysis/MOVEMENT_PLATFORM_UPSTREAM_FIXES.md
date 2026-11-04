# Movement queue and platform upstream fixes

Implemented on 2026-10-29 in `I:\GIT\NexusForever-assessment`, after passive/housing
commit `60ffe61f`. The refreshed official `game_rework` remains
`1f643f6374ec017ae46ac3bd72ad99d42c902d46`.

| Source | Reviewed head | Fork change |
| --- | --- | --- |
| [PR #578](https://github.com/NexusForever/NexusForever/pull/578) | `9289cbb1fdfb0ff06b90f124d2a9760a1d5655ab` | Coalesce pending movement per entity while preserving explicit relocation and removal order. |
| [PR #580](https://github.com/NexusForever/NexusForever/pull/580) | `ce327232b3ce4f3c8ff360ffa135e6dafb4ff458` | Apply platform yaw to world/grid coordinates and detach before local/world teleport destinations are sent or transferred. |

These PRs target upstream's `expedition-evil-from-the-ether` branch. They are
adapted to the fork's existing contracts rather than merged wholesale. Queue
backlog and stale platform state affect map lifecycle and transport, so they
were selected ahead of speculative combat changes. PR #557's effective-armor
ordering still needs independent semantics evidence; its author explicitly
identifies that ordering as an assumption. The older #449 vehicle-attach flow
also needs reconciliation with the fork's current movement protocol.

## Relocation queue

Unlike the upstream branch described in #578, this fork retains a one-second
movement relocation timer. That timer and the per-update grid action threshold
are preserved. Movement now explicitly opts into coalescing; existing
two-argument map relocations and one-argument entity relocations retain their
ordered behavior. Default interface overloads preserve compatibility with
custom map/entity implementations that implement the original API.

BaseMap keeps at most one pending movement action per entity between explicit
relocation/removal boundaries. Further movement updates replace its target.
Keys use entity reference identity, so equal or reused GUIDs cannot share an
action. Explicit moves seal the preceding movement action; subsequent movement
gets a new queue entry after that boundary. Removal also seals pending movement.
Processing an older sealed action cannot remove the newer action's tracking.

The action lookup, target update, snapshot and removal share a short lock. The
actual relocation and its existing OnRelocate callback run outside the lock.
This avoids losing an update between dictionary lookup and removal, and avoids
concurrent reads/writes of a Vector3. Updates arriving after a snapshot create
another action. Queued actions for detached entities or entities now on another
map are discarded. Add/retry processing and the existing lifecycle callbacks
are retained.

## Platform movement and teleport

Platform-relative movement positions remain relative in the movement command
and network model. World/grid relocation rotates local X/Z by platform yaw
(`Rotation.X`) before adding the platform's world position. Existing finite
coordinate guards, duplicate-position checks, command progression and tutorial
recovery behavior remain in place.

Local teleport detaches the player before setting and broadcasting its world
destination. The relocation remains an explicit queue action, and loading
screens, control restoration and the callback retain their existing order.
World transfer captures the open-world return position before detaching and
calling the map manager. Existing pending/disabled teleport guards still run
before any detach. The earlier taxi/mount reconciliation already handles
ordinary or missing platforms safely during Dismount, so that stronger recovery
is retained.

No packet layout, database model/migration, entitlement default, entitlement
scope, storefront grant or account-item behavior changes in this slice.

### Queued vehicle passenger movement

4 November 2026. Vehicle relocation uses its original occupied seats.
An attached rider can follow the vehicle outside its visibility list.
The map checks that the rider is still registered and owns the same
seat and map admission when it executes the queued move. A reused unit
ID, dismount, or new map admission cannot inherit the old move.
Passenger moves retain explicit queue ordering.

Four baseline cases reproduced a null relocation or stale movement,
with the current-seat control passing. The owning build and 149
scoped movement, map, visibility and transport tests passed. The final
fixture registers the rider and its replacement in the map index. Local
evidence is under `artifacts/continuation-20261104/vehicle-passenger-movement/`.
Deployed and native movement remain separate.

## Verification

Twenty-five new regression cases extend the 5,964-test passive/housing baseline:

- Ten queue cases cover repeated updates, explicit/removal boundaries,
  interleaved processing, parallel producers, stale-map actions, reference
  identity and a synthetic queue of 170 moving entities ahead of add/remove.
- Nine platform-position cases cover positive/negative yaw, moving platforms,
  unchanged relative wire coordinates, timer preservation, missing platforms
  and non-finite transforms.
- Six teleport cases cover local teleport with/without loading, detachment
  before broadcast/transfer, callbacks, explicit relocation, world return
  capture, pending-transfer rejection and existing nonvehicle dismount recovery.

```powershell
dotnet build Source\NexusForever.slnx -v minimal --nologo
dotnet test Source\NexusForever.slnx --no-build --no-restore -v minimal --nologo --logger 'trx;LogFileName=full-suite.trx' --results-directory I:/GIT/NexusForever-assessment-report/movement-platform
git diff --check
```

All 25 focused cases pass. The full solution build passes with zero errors and
114 existing warnings. All **5,989 tests pass**, with zero failures and zero
skips. Logs and the full-suite TRX are saved outside the checkout at
`I:\GIT\NexusForever-assessment-report\movement-platform`.

The synthetic queue test establishes bounded queue size and lifecycle ordering;
it is not a performance benchmark or a reproduction of upstream's every-tick
170-unit live-server case. Live client/platform gameplay, multi-client load,
MySQL integration and broker traffic were not run.
