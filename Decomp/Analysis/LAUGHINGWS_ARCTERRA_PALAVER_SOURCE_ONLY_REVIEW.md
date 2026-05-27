# LaughingWS Arcterra And Palaver Source-Only Row Review

Updated: 2026-05-27

## Decision

Keep LWS-052 mapped-only. Arcterra The Caretaker, the Arcterra Coldblood
Citadel entrance portal, and Palaver Point Ish'amel the Bloodied remain
WIP/GUESSED source-only rows. Do not remove WIP labels or add runtime portal,
interaction, combat, quest, or teleport behavior until client/table smoke proves
the exact placements and behavior.

## Preserved Rows

`Tools/DataMapping/sql/laughingws_small_world_wip_seed.sql` preserves these rows
with deterministic IDs:

| Entity ID | Source | Creature | World | Area | Position | Current decision |
| --- | --- | ---: | ---: | ---: | --- | --- |
| `1100200007` | `Map/Open World/Arcterra.sql` | `70779` | `3335` | `4767` | `-158.918, -895.3669, -311.746` | Source-only Caretaker placement; exact retail placement/stats not corroborated. |
| `1100200008` | `Map/Open World/Arcterra.sql` | `75701` | `3335` | `4776` | `-1161.66, -634.28, 761.08` | Source-only Coldblood Citadel portal; target and interaction behavior not proven. |
| `1100200009` | `Map/Open World/Palaver Point.sql` | `75343` | `3519` | `6016` | `758.9, 10.06005, -835.32` | Source-only Ish'amel placement; Palaver quest evidence exists, but this NPC placement/stat behavior is not smoke-proven. |

The extractor strips destructive world deletes and nondeterministic `@GUID`
allocation. These rows are narrow enough to preserve as WIP data, but their
runtime behavior is not evidence-backed.

## Evidence Boundary

- `Tools/DataMapping/extract_laughingws_small_world_overlays.py` explicitly
  marks the Arcterra Caretaker row as source-only and not
  DataMapping-corroborated.
- The same extractor marks the Coldblood Citadel portal row as source-only and
  blocked on interaction target, placement, and portal behavior proof.
- The Palaver row is source-only: quest objective maps prove Palaver Point
  objectives and event participation around world `3519`, but the source
  Ish'amel creature `75343` row itself is not matched by DataMapping placement
  or runtime smoke.
- `rg` over `Source` did not find current runtime scripts for these exact
  source rows that would prove interaction or portal semantics.

## Required Smoke Bundle

Use the evidence harness before promotion:

```powershell
.\Decomp\Analysis\Start-BlockerEvidenceHarness.ps1 `
  -BundleName LWS-052-arcterra-palaver-source-only `
  -WorldIds 3335,3519 `
  -ObjectiveIds 21469,21470,21477,21478,21485,21486,21487,21488 `
  -Notes "Arcterra Caretaker placement, Coldblood portal target, Palaver Point Ish'amel placement/stat/quest behavior" `
  -NegativeCases "portal with missing/invalid target; Ish'amel absent quest state; Caretaker interaction without expected prerequisite"
```

The bundle must include coordinates, entity ids or source creature ids, quest
state, activation/interaction logs, portal destination or failure behavior,
screenshots/video, and negative-case results.

## Future Implementation Gate

Only after proof exists:

- bind entity scripts or portal rows to the proven runtime surface;
- update the WIP seed comments with evidence references;
- add focused tests for portal success/failure, quest-state gates, and repeated
  activation;
- update this review and the closure matrix from mapped-only to implemented.
