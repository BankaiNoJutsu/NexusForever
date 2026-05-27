# LaughingWS Sandbox Zone Asset Proof Checklist

Updated: 2026-05-27

This checklist closes LWS-043 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
It does not promote runtime data. These sources are sandbox/client-asset
investigation candidates only until every required proof item below is met for a
specific row set.

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_file_inventory.csv`

Analyzer recommendation:
`sandbox_only_client_asset_check_required`

## Runtime Decision

All files in this checklist are rejected from runtime seed extraction for the
current build-16042 target. They may be reopened only for an explicit sandbox or
client-asset investigation that proves the target world exists in the client,
the row coordinates are reachable/visible, and the behavior has a current
runtime owner.

## Required Proof Before Reopening A Row

Every candidate row set must have:

- Client asset proof: `World.tbl`/client map entry or another build-16042 client
  asset proving the world/map is present and loadable.
- Runtime ownership proof: a current script, command, sandbox feature flag, or
  explicit investigation task that owns the row range without touching normal
  world imports.
- Placement proof: exact world id, coordinates, screenshots/video, and server
  logs from a `Start-BlockerEvidenceHarness.ps1` bundle.
- Row-level proof: specific entity/stat/vendor/script rows selected from the SQL
  source, with source-local `@GUID` allocation converted to deterministic
  sandbox-owned ids.
- Negative proof: a rejection case showing the row is not accidentally available
  through normal starter-zone, city, instance, or live-event flows.
- Safety proof: no `DELETE FROM entity WHERE world = @WORLD`, DDL,
  foreign-key-toggle, broad replacement, or source-local destructive statement
  is imported.

## Sandbox Files

| Source file | Insert rows | Hazard | Current state |
| --- | ---: | --- | --- |
| `Test zones\Not in Client\Coralus.sql` | 1,230 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Lopp Party Island.sql` | 162 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Test maps\AI Test World.sql` | 1 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Test maps\Berke Test World.sql` | 10 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Test maps\Eternity Islands.sql` | 702 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Test maps\Pellicane test world.sql` | 4 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Test maps\Protopia.sql` | 413 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Test maps\Public Quest Island.sql` | 5 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Test maps\Quest Test Island.sql` | 15 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Test maps\Randyland.sql` | 13 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Ultimate Protogames Raid.sql` | 27 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Unfinished zones\Algoroc Neighborhood.sql` | 3 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Unfinished zones\Inferalis.sql` | 6 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Unfinished zones\Initiate Island.sql` | 72 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Unfinished zones\Neighborhood.sql` | 30 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Unfinished zones\sserrasar.sql` | 6 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Not in Client\Unknown areas or zones\Designer Island.sql` | 23 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Unknown areas or zones\Crowe's Nest.sql` | 10 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Unknown areas or zones\Ikthian Island.sql` | 2 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Unknown areas or zones\Resolution Point.sql` | 6 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Unknown areas or zones\The Smallholds.sql` | 20 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Unknown areas or zones\Valis.sql` | 6 | `delete_world_entities` | Sandbox-only; no runtime seed. |
| `Test zones\Unknown areas or zones\Whispering crag fields.sql` | 20 | `delete_world_entities` | Sandbox-only; no runtime seed. |

Summary:

- Files: `23`
- Active insert rows: `2,786`
- Runtime-promoted rows: `0`
- Runtime recommendation: keep `sandbox_only_client_asset_check_required`

## Next Allowed State

Keep these sources classified as `sandbox_only_client_asset_check_required`.
Reopen a specific file only when an investigation task names the target world,
provides client asset proof, captures a blocker-evidence bundle, and proposes a
deterministic sandbox-owned seed that strips all destructive source statements.
