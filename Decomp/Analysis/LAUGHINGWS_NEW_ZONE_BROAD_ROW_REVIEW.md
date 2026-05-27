# LaughingWS New-Zone Broad Row Review

Updated: 2026-05-27

This review closes LWS-040, LWS-041, and LWS-042 from
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
