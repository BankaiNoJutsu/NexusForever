# LaughingWS Everstar Grove Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Alizar/EverstarGrove.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-031 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The useful Exo-Lab 71 teleporter checklist rows are already covered by
`Tools/DataMapping/extract_laughingws_small_world_overlays.py`; this pass
classifies the remaining branch-only rows.

## Decision Summary

| Delta group | Queue rows | Decision | Reason |
| --- | ---: | --- | --- |
| Covered branch checklist `entity` rows | 2 | Covered by small-world seed | Creature `28454` Exo-Lab 71 Eldan Teleporter rows are already emitted as WIP/GUESSED coordinate-keyed checklist updates/fallback inserts because the current transport script maps the same creature to `WorldLocation2` row `21760`. |
| Branch-only residual `entity` rows | 14 | Blocked | These source-only placements have no current script, Quest2/DataMapping objective bridge, or smoke proof strong enough for promotion in this pass. |
| Branch-only `entity_stats` rows | 16 | Blocked | Stats are tied to source-local `@GUID` allocation and cannot be promoted without a deterministic reviewed entity owner. |
| Branch-only vendor rows | 29 | Blocked | The vendor/category/item rows are tied to source-local `@GUID+1`; no deterministic vendor placement/catalog proof was found for this Everstar Grove slice. |
| Official-only `entity` rows | 3 | Official-retained | Current official WorldDatabase rows remain authoritative; do not delete or replace them from the branch replacement dump. |

Outcome:

- Promotable rows beyond the existing small-world seed: `0`
- Already-covered checklist rows: `2`
- Official-retained rows: `3`
- Rejected placeholder rows: `0`
- Blocked branch-only residual rows: `59` queue rows

## Row Hash Coverage

These hashes are the normalized row identifiers from the generated review
queue. Together they cover every non-identical Everstar Grove row present in
the queue for this pass.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Covered branch checklist `entity` rows | Covered | `4f2d6418b19d9393`, `e4473e05faf42618` |
| Branch-only residual `entity` rows | Blocked | `14df444ce94749e1`, `2ca4dee4d3da45a1`, `3b6e98359731b09a`, `508ee568d2c81fa2`, `61d44c3d1cde7305`, `7fe5ea6c94f4081a`, `9003033ee9301fed`, `923266a1ac74c260`, `9e9366a4860d0220`, `dc42a3f2489abbeb`, `de966bbc56ae6798`, `e90d236a0292e28d`, `f28cd44e6b6bde15`, `f71706b70d73a922` |
| Branch-only `entity_stats` rows | Blocked | `1165d9dd5fcbc3f3`, `1d8f27fa8ebbf0b7`, `39293da8f9270667`, `487178efa17e599b`, `48fa00937946d45c`, `4b35f69b9c03489d`, `79de63fe4d804de6`, `7d0fca84976cbffc`, `8ec4d5edbb3f9712`, `905c18097a649500`, `943dea6204367793`, `a08f5abd3310927f`, `b522d7509e7566df`, `baa8b7a975c228e4`, `e7651fc5aa209cdd`, `f9cec842958c1f37` |
| Branch-only vendor rows | Blocked | `02a03137c119158d`, `0655900ddff9a71c`, `0db770ec0011e01a`, `0ea688a68a7b5a28`, `17f193059c31d347`, `1ad2a088c06cd247`, `295db0342a954873`, `2bffec88d1252a5b`, `2f9f4b2f9d96e72c`, `330c28888fcab964`, `34f97a9202147c47`, `377c737f1ea1acd6`, `50d44c52b4a69bec`, `57f24dc02174e73b`, `6a679f887b3b0af6`, `6bf29f06ac3f4ef8`, `8a0a0e8b9a0cce13`, `8b34425f22112582`, `9c87e537de554a1f`, `9c8b3815dff064cf`, `a7436e1f35d9455c`, `c117605bb198c486`, `c463e42c0f373e53`, `ccba787d3890d887`, `d9277ef5091f15bd`, `de656cfe739cee6a`, `ef3cb3b4e73357fb`, `efa19fc4ea591529`, `f5e31d472fb37b56` |
| Official-only `entity` rows | Official-retained | `5674091e3b213a06`, `9a97591f02f7bfac`, `c4c7f5ae57cafdce` |

Next allowed states:

- Keep `Map/Open World/Alizar/EverstarGrove.sql` classified as
  `covered_by_laughingws_small_world_wip_seed;blocked_laughingws_everstar_grove_residual_rows`.
- Reopen only for a specific residual row if a future pass provides current
  script need, quest objective proof, official table corroboration, or local
  retail/client smoke with exact coordinates and row identity.
