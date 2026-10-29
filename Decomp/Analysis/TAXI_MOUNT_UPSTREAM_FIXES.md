# Taxi discovery and mount lifecycle upstream fixes

Implemented on 2026-10-29 in the fresh assessment clone after travel/archive
commit `f9174acc`. Upstream was refreshed and `game_rework` remains
`1f643f6374ec017ae46ac3bd72ad99d42c902d46`.

This slice addresses the next concrete gaps in the assessment: taxi discovery
and route authorization from [PR #604](https://github.com/NexusForever/NexusForever/pull/604)
at `19786895e3bb399a95576295916318b4dfc9bbc2`, and mount lifecycle corrections
from [PR #607](https://github.com/NexusForever/NexusForever/pull/607) at
`7f044aad0a20419a788b8140d263ac83fcd93217`.

## Taxi discovery and persistence

- Add character-owned `character_taxi_node` records with a composite
  character/node key and cascading foreign key. Generate additive MySQL and
  SQLite migrations and snapshots against the fork's current bind-aware model.
  Character loading includes the optional discovered-node collection.
- Discover visible kiosks on the same map within 30 units, checked every two
  seconds, and on guarded taxi/shuttle interactions (events 45 through 48).
  Successful flight purchases also discover the destination.
- Validate static node existence, faction access and the 14-bit notification
  width. Neutral nodes are available to all factions; faction-specific nodes
  require the matching Exile/Dominion faction. Unknown faction values do not
  bypass faction restrictions. Duplicate discoveries do not emit duplicate
  notifications or stage duplicate inserts.
- A successful character save acknowledges only the node set staged by that
  save. Failed inserts remain pending for retry, and discoveries made while a
  save is in flight remain pending for the next save. Node state and save
  acknowledgments share a lock.
- Send known nodes in sorted order after each world entry using the existing
  `ServerFlightPathUpdate.FlightPathIds` payload. Reuse the existing 0x0186
  `ServerEntitySelectAuxUInt14` producer for incremental unlocks. Do not add a
  duplicate registration or rename existing packet APIs.

New characters receive no automatic node grants. Existing entitlement defaults
and grant policy are unchanged.

## Flight purchases

Keep the existing 0x00FF `ClientFlightPathPurchase` packet, strict truncated-read
validation, handler, contiguous-route requirement and teleport-based transport.
Before charging, the handler now validates:

1. A nonempty request containing at most 30 legs.
2. Every source, intermediate and destination node exists and is faction-safe.
3. The player has a visible source kiosk on their current map within 15 units.
4. The destination has a valid world table entry, a world id representable by
   the existing teleport API, and finite coordinates.
5. No teleport is pending, and the player can afford the total price.

Preserve the current sum of `TaxiRoute.Price`, accumulated in a ulong, and the
existing insufficient-funds and invalid-route errors. Apply destination facing
and discover the destination on an accepted purchase. Existing rapid-transport
spell handling and cast-context prerequisites are unchanged.

PR #604's description advertises #608 and #609 as fare/ride follow-ups, but both
PR lookups returned NOT_FOUND during this review. Those follow-ups are not
treated as reviewed source. This change does not claim client fare parity or
spline-based taxi rides; it retains the fork's established pricing and travel
implementation.

## Mount lifecycle

Reserve a summoned mount before enqueueing it onto the map. CanMount refuses
another summon while the reservation is pending, including the interval after
map entry but before delayed passenger attachment. A monotonic ten-second
timeout permits retry if the summon never arrives. Pilot seating and dismount
clear the reservation, preserving immediate remount behavior.

When the visible vehicle is missing or no longer contains the player,
Dismount clears the platform and restores player control instead of calling
PassengerRemove on a nonexistent passenger. Valid passengers keep the existing
vehicle removal path, including its visual and passenger notifications.

The fork already casts Riding License and Mount Sprint with explicit
UserInitiatedSpellCast=false and IgnoreGlobalCooldown=true. Preserve that
behavior and the existing GlobalCooldownEnum handling rather than applying
upstream's broad cooldown bypass for all non-user casts. A rejected map-entry
request no longer applies the mount auras. Existing AddSpell, Disembark,
hoverboard customization and spell lifetime implementations remain in place.

## Verification

There are 39 additional regression cases relative to the 5,885-case
travel/archive baseline:

- Twelve discovery/persistence/migration cases: node and faction validation,
  14-bit boundaries, timed discovery and map/range filtering, insert/reload,
  failed-save retry, in-flight discovery, and SQLite upgrade/rollback preserving
  bind and entitlement rows.
- Twelve flight cases: invalid origins, intermediate nodes, faction values,
  worlds, coordinates and pending transfers reject before charging; oversized
  route lists reject; large fares do not overflow a uint.
- Seven mount lifecycle cases: pending map entry/seating, timeout, stale
  vehicles, control restoration, successful seating cleanup and normal removal.
- Six guarded taxi interaction cases and two mount-effect cases that verify
  reservation/map-entry behavior and explicit aura cooldown flags.

An existing accepted-flight test also checks destination discovery. The
insufficient-credit fixture now uses a valid route to a real destination, so
the funds error is exercised after route validation. The earlier bind migration
test locates its migration by name instead of assuming it is always the newest.

```powershell
dotnet build Source\NexusForever.slnx -v minimal --nologo
dotnet test Source\NexusForever.slnx --no-build --no-restore -v minimal --nologo
git diff --check
```

The focused slice passes 73 tests. The full build passes with zero errors and
114 existing warnings. The full suite passes **5,924 tests**, with zero failures
and zero skips. EF reports no pending model changes for either MySQL or SQLite.
The generated MySQL delta contains only the taxi-node table/key/FK and its
migration-history insert.

Build/test logs, TRX results, migration checks and the MySQL delta script are
saved outside the checkout at
`I:\GIT\NexusForever-assessment-report\taxi-mount`.

Deployments must apply the CharacterTaxiNode migration for their selected
provider before running this code. SQLite migration paths were exercised in
tests. Live MySQL application, client gameplay and broker traffic were not run.
