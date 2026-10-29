# Loot and group upstream reconciliation

Implemented on 2026-10-29 in the fresh assessment clone, after group and
entitlement commit `7a2c93f5`. The upstream `game_rework` ref was refreshed and
remains `1f643f6374ec017ae46ac3bd72ad99d42c902d46`.

## Upstream behavior reconciled

| Upstream change | Fork implementation |
| --- | --- |
| Initial loot work `e8465e89` ([PR #364](https://github.com/NexusForever/NexusForever/pull/364)) and distribution refactor `0b8aa1da` | Death processing gathers contributors, preserves their individual quest/achievement/path/XP rewards, and generates one shared creature loot instance per contributing group plus independent solo loot. Duplicate contributors are ignored. Personal Omnibit kill bonuses remain per contributor. |
| Generation refactor `a3e4a7b4` | Carry the eligible player subset through each parent/child loot group. Apply class/race/level/faction/quest conditions to all eligible recipients rather than only the first contributor. Preserve eligibility through item merging, notification, rolling and master assignment. Non-equipment rewards avoid equipment roll rules. |
| Read-only group state `474b429e` | Cached membership snapshots and snapshots after member removal expose read-only lists. Snapshot replacement and association changes share a lock; round-robin rotation retains its own lock. |
| Primary group promotion `4422db7c` | Leaving either of two groups publishes the remaining primary group instead of clearing the character's association. Secondary snapshots/removals cannot overwrite an explicit primary association. |
| Map synchronization `99ff632e` | Retain the fork's existing map-synchronized player mutation and packet enqueue; update the authoritative group association separately from membership snapshots. |
| Upstream map-owned loot manager | Retain the fork's global manager and bind creature loot instances to their source map. Reject generation/access/delivery on another map and scope corpse cleanup to the map, because entity GUIDs can repeat across maps. |

The loot interfaces keep their existing entry points. Default implementations
of the new distribution and map-cleanup overloads preserve compatibility with
specialized implementations; the production global manager implements the
reconciled behavior.

## Fork behavior retained

- Existing round-robin, harvest first-tagger/rotation, trash-item rules,
  need/greed/pass, master loot, bind-on-pickup confirmation, inventory-full
  recovery and deferred delivery remain in place. Player and pet deaths do not
  generate creature loot.
- Imported direct creature loot, flat DataMapping duplicate suppression,
  quest-only tables, loot bags, salvage, crafting and delivery preflight paths
  retain their existing implementation. Account and character entitlement
  defaults are unchanged.
- Keep the existing database schema, `MinDrop`/`MaxDrop` model names, enum column
  types, migrations and seed compatibility. Upstream's schema/type rename is
  not replayed onto populated fork databases.
- Keep the evidence-backed five boolean fields in the fork's loot-item packet,
  including separate Explosion and Granted flags. Upstream's four-boolean
  serialization is not substituted. Existing packet regressions remain green.
- Keep correct probability direction: zero never drops and 100 always drops.
  Upstream's `Probability <= roll` checks invert those boundaries. Item and
  group count ranges use an inclusive upper bound without signed integer
  overflow; NaN probabilities cannot generate rewards. Existing bounded
  minimum-drop retry semantics are preserved.

## Regression coverage and verification

There are 25 additional test cases relative to the 5,832-case group/entitlement
baseline:

- 19 loot cases cover one drop per contributing group and independent solo
  drops, conditional group eligibility, roll exclusion, distinct eligible
  sets during merging, non-equipment rewards, empty eligibility, map-isolated
  cleanup/access, player-death exclusion, quest/unknown conditions,
  probability boundaries and unsigned quantity bounds.
- Four group-state cases cover primary/secondary associations, authoritative
  removal, immutable snapshots before and after removal, and concurrent
  snapshot updates/readers.
- Two relational SQLite cases remove either the primary or secondary group
  using the real group-server character wrapper and inspect the persisted
  association outbox message for the remaining primary.

```powershell
dotnet build Source\NexusForever.slnx -v minimal --nologo
dotnet test Source\NexusForever.slnx --no-build --no-restore -v minimal --nologo
git diff --check
```

Final solution build: zero errors, 114 existing warnings. Full test suite:
**5,857 passed, zero failed, zero skipped**. A prior focused loot/group slice
passed 131 cases before the last three guard cases were added; the final full
run includes those guards and the snapshot-removal assertion.

Build/test logs and the final TRX are saved outside the checkout at
`I:\GIT\NexusForever-assessment-report\loot-reconciliation`. These gates exercise
managed runtime behavior and relational SQLite group persistence. Live
client gameplay, MySQL integration and broker stress were not run.
