# LaughingWS Northern Wilds Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Alizar/Northern Wilds.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-034 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The useful Northern Wilds quest/objective placements are already covered by
`Tools/DataMapping/extract_laughingws_small_world_overlays.py`; this pass
classifies the remaining Dominion Swordsman spline-mode deltas and Supply
Officer Windward duplicate vendor rows.

## Decision Summary

| Delta group | Queue rows | Decision | Reason |
| --- | ---: | --- | --- |
| Covered branch `entity` rows | 7 | Covered by small-world seed | Deadeye Brightland, Scientist Lusk, Elder Bartol, and the three Q3673 Signal Flare placements are already emitted as WIP/GUESSED deterministic rows because DataMapping/Quest2 evidence selects the useful branch placements while reconciling source-local rotations and area/creature mismatches. |
| Covered branch `entity_stats` rows | 16 | Covered by small-world seed | The source-local stats for the covered Deadeye/Lusk/Bartol rows are already translated onto deterministic small-world entity ids where the seed owns those placed rows. |
| Branch duplicate `entity` row | 1 | Rejected duplicate | The branch Supply Officer Windward row for creature `11066` is a near-coordinate duplicate of the retained official row and should not replace the official WorldDatabase placement. |
| Branch-only `entity_spline` rows | 8 | Blocked | The branch changes the same Dominion Swordsman spline ids from official mode `8` to mode `2`; no movement capture or script proof was found in this pass. |
| Branch-only vendor rows | 29 | Rejected duplicate | The `entity_vendor`, `entity_vendor_category`, and `entity_vendor_item` rows are tied to source-local `@GUID+1` and duplicate the Supply Officer Windward vendor payload already covered by the DataMapping/runtime vendor import. |
| Official-only rows | 9 | Official-retained | The current official entity/spline rows remain authoritative and must not be deleted or replaced by the branch replacement dump. |

Outcome:

- Promotable rows beyond the existing small-world seed: `0`
- Already-covered entity/stat rows: `23`
- Official-retained rows: `9`
- Rejected duplicate rows: `30`
- Blocked branch-only residual rows: `8`

## Row Hash Coverage

These hashes are the normalized row identifiers from the generated review
queue. Together they cover every non-identical Northern Wilds row present in
the queue for this pass.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Covered branch `entity` rows | Covered | `0be95f6f0fad1b9e`, `0ec731652092943b`, `520ca1f035679686`, `54628c5cc307bd9b`, `641a242218c20898`, `733f2feb9489c2f6`, `d5c045289d43fa41` |
| Covered branch `entity_stats` rows | Covered | `025a70689044a276`, `34e903f6f9f145ee`, `4183234a02987c9e`, `48fa00937946d45c`, `4b35f69b9c03489d`, `66aca50b76628f13`, `741f122cd9eaf798`, `76f18ba020907484`, `7e28e1402775d15d`, `8ec4d5edbb3f9712`, `943dea6204367793`, `a6c65bdf42807dff`, `baa8b7a975c228e4`, `cdeb7f3bc5161866`, `e7651fc5aa209cdd`, `f9a390d5a49bb982` |
| Branch duplicate `entity` row | Rejected duplicate | `53fe93950f797378` |
| Branch-only `entity_spline` rows | Blocked | `03d43ca8b19e275b`, `0b90548966ddfc8d`, `3ffcc76ffee19afc`, `62243fa60cda08ba`, `899d3ea45f4bc23d`, `9f84c6fa071845c0`, `c8dceec146fdcce5`, `ecf0531c6656a24a` |
| Branch-only vendor rows | Rejected duplicate | `02a03137c119158d`, `0655900ddff9a71c`, `0db770ec0011e01a`, `0ea688a68a7b5a28`, `17f193059c31d347`, `1ad2a088c06cd247`, `295db0342a954873`, `2bffec88d1252a5b`, `2f9f4b2f9d96e72c`, `330c28888fcab964`, `34f97a9202147c47`, `377c737f1ea1acd6`, `50d44c52b4a69bec`, `57f24dc02174e73b`, `6a679f887b3b0af6`, `6bf29f06ac3f4ef8`, `8a0a0e8b9a0cce13`, `8b34425f22112582`, `9c87e537de554a1f`, `9c8b3815dff064cf`, `a7436e1f35d9455c`, `c117605bb198c486`, `c463e42c0f373e53`, `ccba787d3890d887`, `d9277ef5091f15bd`, `de656cfe739cee6a`, `ef3cb3b4e73357fb`, `efa19fc4ea591529`, `f5e31d472fb37b56` |
| Official-only `entity` rows | Official-retained | `27eab722829a80a5` |
| Official-only `entity_spline` rows | Official-retained | `1c9330b4da82bffb`, `3a33b77c1d1709fe`, `57bda80a28a67981`, `6831b938cdfa9d5a`, `9760d87ffe980d05`, `9b50d1e29f160750`, `b3981c815dae05be`, `f26e4d030aa1baee` |

Next allowed states:

- Keep `Map/Open World/Alizar/Northern Wilds.sql` classified as
  `covered_by_laughingws_small_world_wip_seed;blocked_laughingws_northern_wilds_residual_rows;rejected_laughingws_northern_wilds_vendor_duplicate_rows`.
- Reopen the spline rows only with movement capture or script evidence proving
  the branch mode change for the exact Dominion Swordsman spline ids.
- Reopen the vendor rows only with current vendor-catalog proof that differs
  from the existing DataMapping/runtime Supply Officer Windward import.
