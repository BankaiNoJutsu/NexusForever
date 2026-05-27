# LaughingWS Dust Stalker Quest 4516 Review

Updated: 2026-05-27

## Decision

Keep Dust Stalker quest `4516` (`Boss Xagg's Due`) as mapped-only and
WIP/GUESSED. Do not implement exit-panel behavior, self-destruct timing, escape
timing, or forced teleport/despawn behavior from the LaughingWS SQL alone.

The current seed remains useful for data placement, but the behavior task is
blocked until a quest-instance smoke bundle proves the bridge-control and exit
sequence in a client session.

## Evidence

- `Tools/DataMapping/sql/laughingws_quest_instance_wip_seed.sql` promotes three
  deterministic entity rows for world `1138`, area `950`: Boss Xagg
  (`16707`), Dust Stalker's Bridge Controls (`16733`), and a source-only exit
  panel row (`23758`).
- The source file
  `artifacts/external/LaughingWS.WorldDatabase.New-Zones-and-more/Map/Open World/The Dust Stalker.sql`
  deletes all entities for the world, uses `@GUID = MAX(entity.id)`, gives
  Boss Xagg health `1`, uses source entity type `20` for the bridge controls,
  and labels the exit panel comment `place for real`.
- The extractor strips the destructive world delete and nondeterministic
  `@GUID` allocation, maps the bridge controls to current `SimpleCollidable`
  type `32`, and replaces Boss Xagg's source health with DataMapping-backed
  stats: health `8767`, level `19`, shield `2352`, interrupt armor `0`.
- `Tools/DataMapping/output/quest_objective_client_map.csv` maps quest `4516`
  objectives to:
  - enter Boss Xagg's ship / area `950`;
  - kill creature `16707`;
  - use creature `16733` to set the ship to self-destruct;
  - get out before it explodes through objective creature `23758`.
- `Tools/DataMapping/output/creature_quest_map.csv` corroborates Boss Xagg
  (`16707`), Bridge Controls (`16733`), and Access Panel (`16670`) objective
  links for quest `4516`, but no row-level proof was found for exact
  self-destruct timer, exit-panel activation semantics, cleanup/despawn timing,
  or escape teleport behavior.
- `rg` over `Source` found no Dust Stalker/Boss Xagg/quest `4516` runtime script
  that implements the self-destruct or escape sequence.

## Required Smoke Bundle

Use the blocker evidence harness before removing WIP/GUESSED labels:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -BundleName LWS-051-dust-stalker-q4516 `
  -WorldIds 1138 `
  -ObjectiveIds 7633,6189,7637,7638,7639 `
  -Notes "Quest 4516: Boss Xagg kill, bridge controls self-destruct, exit panel, escape timing" `
  -NegativeCases "activate bridge before kill; activate exit before self-destruct; miss escape timer"
```

The bundle must record character id, quest state, world/area, coordinates,
objective ids, server log lines, client observations/screenshots or video, the
observed timer duration, and negative-case results.

## Future Implementation Gate

Only after smoke proof exists:

- bind the proven bridge-control/exit-panel entity scripts by creature or
  script row;
- implement exact quest-objective transitions and timer duration;
- add focused tests for accepted/missing/completed quest states, premature
  activation, timer expiry, successful escape, cleanup, and repeat behavior;
- update the WIP seed comments and closure docs from mapped-only to implemented.
