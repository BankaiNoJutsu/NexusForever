# Solo instance reset and mounted visibility upstream fixes

Implemented on 2026-10-29 in `I:\GIT\NexusForever-assessment`, after movement/platform
commit `d2396d6e`. Official refs were refreshed; `game_rework` remains
`1f643f6374ec017ae46ac3bd72ad99d42c902d46`.

| Source | Reviewed head | Fork change |
| --- | --- | --- |
| [PR #584](https://github.com/NexusForever/NexusForever/pull/584) | `9d6c877131a9a8ef678e63b518afdb3c5d7ddd47` | Replace blanket rejection of /resetinstances with safe removal of the character's vacated solo locks. |
| [PR #449](https://github.com/NexusForever/NexusForever/pull/449) | `cdb3a20cdacce59304da1b437c03baa6c464b156` | Restore a visible mounted player's seat link after rider and vehicle have both been created for the observer. |

These concrete instance/transport gaps follow the prior movement fixes. The
PRs target expedition and game_rework respectively, so their behavior is
adapted to the fork's APIs. No wholesale upstream merge is involved.

## Solo reset

The character's solo lock snapshot is enumerated and locks other than the
current instance are removed. The next normal ContentInstancedMap entry creates
a fresh solo lock and instance ID. Group, match and residence collections are
unaffected. Realm and character identity remain part of the solo-lock key.

Removal includes the expected instance ID and returns whether it succeeded.
An old snapshot cannot remove a replacement lock for the same world. Solo lock
creation, snapshotting and removal share a lock; concurrent creation requests
reuse the existing lock rather than creating competing collections or throwing
on duplicate world insertion. Successful removal drops character membership
from the old lock. Existing locks and map instances otherwise retain their
normal lifecycle; reset does not destroy a live map directly.

The handler preserves the current instance and declines requests while loading
or when CanTeleport rejects travel, protecting pending transfers. Result text
uses the existing system-message path. It does not send ServerInstanceResetResult
for the slash command: upstream reports that its client event assumes an open
Instance Settings dialog. The fork's single-instance dialog reset, difficulty,
prime-level and rally handling are retained.

## Mounted visibility

Player visibility creation now checks whether the player has a visible vehicle
and a matching passenger record. If so, the observer receives the seat link
after entity creation and existing player metadata. When the vehicle arrives
after the player, its creation restores links for all its visible passengers.
The same path handles player recreation for nearby-list refresh and vehicle
visibility re-entry.

Missing, hidden or ordinary platforms and missing/mismatched passenger records
produce no seat-link message. Duplicate AddVisible calls retain their existing
suppression. Seat type/position come from the vehicle's passenger record.

The fork already names opcode 0x089B ServerVehiclePassengerSelf; its existing
two-u32 plus two-bit/three-bit seat layout is retained. This is the packet
upstream #449 calls ServerVehiclePassengerSet. The fork's boarding-time self
packet, passenger-add broadcast, control/platform changes and tutorial recovery
are retained. Upstream's removal of the boarding-time packet is not adopted
without a current fork client trace establishing that it is redundant. Existing
entity auxiliary packets, player position snapshots, group/path metadata,
reciprocal visibility and loot notifications remain in the create path.

## Other reviewed overlap

[PR #582](https://github.com/NexusForever/NexusForever/pull/582), head
`8fef69be625e682e78a8bc4fb8a30735bb0d1ebd`, fixes non-unit explicit interaction
targets in the expedition spell implementation. The fork already selects
IWorldEntity targets through GetPrimaryTargetWorldEntity, dispatches Activate
through HandleEffectActivateWorld and completes pending interactions through
ActivateCastCompletion. Replacing these with upstream's unit-only target helper
would discard the fork's broader world-target support. No production change is
needed for that specific cast-exception path; existing interaction regressions
were included in the focused run.

Entitlement defaults, entitlement scope, storefront/account-item grants, packet
layouts, database models and migrations are unchanged in this slice.

## Verification

Twenty-six new regression cases extend the 5,989-test movement/platform baseline:

- Fifteen solo-reset cases cover vacated/current instances, normal content-map
  re-entry, group-lock preservation, character/realm isolation, travel rejection,
  empty/repeated requests, missing players, stale snapshots, membership removal,
  stable snapshots and concurrent creation.
- Eleven visibility cases cover both arrival orders, pilot/passenger/gunner
  seats, create-before-attach ordering, duplicate suppression, nearby-list
  recreation, missing/wrong passengers, ordinary/hidden platforms, dismounted
  riders, vehicle re-entry and multiple seated players.

```powershell
dotnet build Source\NexusForever.slnx -v minimal --nologo
dotnet test Source\NexusForever.slnx --no-build --no-restore -v minimal --nologo --logger 'trx;LogFileName=full-suite.trx' --results-directory I:/GIT/NexusForever-assessment-report/instance-visibility
git diff --check
```

The final focused run passes 60 tests, including existing instance settings,
instance travel, mount lifecycle and client-interaction cases. The full solution
build passes with zero errors and 114 existing warnings. All **6,015 tests
pass**, with zero failures and zero skips. Logs and the full-suite TRX are at
`I:\GIT\NexusForever-assessment-report\instance-visibility`.

Tests exercise managed runtime and packet-order behavior. Live client gameplay,
MySQL integration, broker traffic and multi-client load were not run. Solo locks
remain runtime-only; this change introduces no persisted lockout schema.
