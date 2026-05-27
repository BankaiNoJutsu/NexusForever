# LaughingWS Galeras Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Alizar/Galeras.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-013 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The source file is still a broad world replacement file. This pass found no
official-only row loss and no row with enough current quest, script, vendor, or
retail smoke proof to promote.

## Decision Summary

| Delta group | Queue rows | Source row count | Decision | Reason |
| --- | ---: | ---: | --- | --- |
| Branch-only `entity` | 22 | 22 | Blocked | Source-only Galeras placements with no current script, quest objective, or retail placement proof in this pass. |
| Branch-only `entity_stats` | 19 | 68 | Blocked | Stats are tied entirely to source-local `@GUID+1` allocation and have no promoted deterministic entity owner. |

Outcome:

- Promotable rows: `0`
- Official-retained rows: `0`
- Duplicate rows promoted or deleted: `0`
- Rejected placeholder rows: `0`
- Blocked branch-only rows: `41` queue rows covering `90` source rows

## Row Hash Coverage

These hashes are the normalized row identifiers from the generated review
queue. Together they cover every branch-only Galeras row present in the queue
for this pass.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Branch-only `entity` | Blocked | `028a0a5e905439ae`, `0699e6a8ea0adbc3`, `0beebdb77729fcdb`, `14685be8e8dc77e2`, `1b9f0898a6ab4535`, `33e9042bb1452ce0`, `3843c2b010ae6370`, `46ea09349442d0dc`, `5bb44ffa9cc30253`, `63aec54e20e000d8`, `6461e61793b6a921`, `8766f3a430327641`, `92a3e69032b485c5`, `93acf72afb0e8b39`, `93eebca49a8efd8b`, `9a8d9d101303e23f`, `af691ddcd5209b17`, `c08ce4c445a8fae1`, `ccff8f96da7ec0fb`, `f698d5d132400336`, `f719ae406f16ee0e`, `fda27f3ef6818aeb` |
| Branch-only `entity_stats` | Blocked | `0f2579cfeea18f80`, `287ada7fa39f210d`, `487178efa17e599b`, `48fa00937946d45c`, `4b35f69b9c03489d`, `700a45d9f06829c6`, `79de63fe4d804de6`, `7c54676b2c484b9c`, `80f0bbfbe79db232`, `8ec4d5edbb3f9712`, `943dea6204367793`, `95b59f4975035932`, `a08f5abd3310927f`, `b522d7509e7566df`, `baa8b7a975c228e4`, `dd22d4e5cd1593be`, `e7651fc5aa209cdd`, `f7d7862ca0f275f8`, `f819d999cc787879` |

Next allowed states:

- Keep `Map/Open World/Alizar/Galeras.sql` audit-only as
  `blocked_laughingws_galeras_residual_rows`.
- Reopen only for a specific row if future evidence maps a current quest,
  script, deterministic placement, or retail/client smoke path.
