# LaughingWS New-Zone Broad Row Review

Updated: 2026-05-27

This review tracks LWS-040, LWS-041, and LWS-042 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
It records why the broad Dreadmoor, Halon Ring, and Murkmire source files remain
blocked or partially rejected instead of runtime-promoted.

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_file_inventory.csv`

Analyzer recommendation:
`blocked_laughingws_new_zone_broad_rows`

## Decision Summary

| Source file | World ids | Insert rows | Tables | Hazard | Decision |
| --- | --- | ---: | --- | --- | --- |
| `Map\Open World\New Zones\Dreadmoor.sql` | `22` | 1,256 | `736` `entity`, `520` `entity_stats` | `delete_world_entities` | Blocked. Existing audit found the entity slice is zero-coordinate placeholder content, and no current client-asset/runtime script proof selects a safe row subset. |
| `Map\Open World\New Zones\Halon Ring.sql` | `1068` | 1,692 | `1,489` `entity`, `201` `entity_stats`, `2` `entity_vendor` | `delete_world_entities` | Partially covered only by curated city/transport crumbs; broad residual rows stay blocked because most rows are zero-coordinate or area-zero placeholder content. |
| `Map\Open World\New Zones\Murkmire.sql` | `51` | 841 | `653` `entity`, `188` `entity_stats` | `delete_world_entities` | Blocked. Source remains TODO-heavy with quest-checklist/stat placeholders and no current script/client proof for runtime seeding. |

Outcome:

- Runtime-promoted rows from these broad files: `0`
- Already-covered adjacent Halon transport/city crumbs: handled by
  `Tools/DataMapping/sql/laughingws_city_content_seed.sql`
- Remaining broad rows: `3,789` insert rows kept blocked or rejected from
  runtime import

## Reopen Requirements

Reopen only a narrow row subset, never the broad source file. Required proof:

- Build-16042 client asset proof for the target world or destination.
- Current runtime owner: script, transport handler, quest objective, sandbox
  feature flag, or explicit investigation task.
- Exact row selection with source deletes, `@GUID`, DDL, and broad replacement
  semantics stripped.
- `Start-BlockerEvidenceHarness.ps1` bundle with coordinates, screenshots or
  video, server logs, player/world ids, and at least one negative case.
- Focused verifier or test for any promoted seed/script behavior.

## Next Allowed State

Keep Dreadmoor, Halon Ring broad residuals, and Murkmire classified as
`blocked_laughingws_new_zone_broad_rows`. The only currently allowed Halon Ring
runtime data remains the previously reviewed city/transport slice.

Harness progress (2026-05-28): `Start-BlockerEvidenceHarness.ps1` now supports
`-NewZoneAssetProof`. The preset creates
`new-zone-asset-proof-targets.md`, preloads Dreadmoor/Halon/Murkmire target
worlds when `-WorldIds` are omitted, and records negative/safety cases for
zero-coordinate placeholders, destructive broad SQL replacement, and wrong-world
or missing-client-asset proof. This is a capture aid only; no broad row is
promoted without completed row-level evidence.

Dry-run guard progress (2026-05-28): `test_blocker_evidence_harness_presets.py`
now runs `-NewZoneAssetProof` with `-CreateBundleOnly` and verifies the
manifest, target worksheet, default world ids, helper files, and negative-case
scaffold. This protects the asset-proof workflow only; Dreadmoor, Halon Ring
broad residuals, and Murkmire remain blocked until a completed row-level bundle
proves client asset, runtime placement, and safety.

Analyzer guard progress (2026-05-28): `Tools/DataMapping/test_laughingws_worlddb_classifications.py`
now runs the real `analyze_laughingws_worlddb.py` recommendation logic against
the local LaughingWS external snapshot and asserts that Dreadmoor, Halon Ring,
and Murkmire keep `blocked_laughingws_new_zone_broad_rows` while avoiding
`extract_additive_world_overlay`.
