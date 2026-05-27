# LaughingWS Levian Bay Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Olyssia/Levian Bay.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-030 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The useful Levian Bay checklist rows are already covered by
`Tools/DataMapping/extract_laughingws_small_world_overlays.py`; this pass
classifies the remaining branch-only/source-only rows.

## Decision Summary

| Delta group | Queue rows | Decision | Reason |
| --- | ---: | --- | --- |
| Covered branch checklist `entity` rows | 14 | Covered by small-world seed | Signal Flare (`26016`) and Drop Pod Landing Beacon (`26417`) coordinate-keyed `questChecklistIdx` updates are already emitted as WIP/GUESSED updates/fallback inserts because DataMapping maps them to the Lighting the Way and Lost in the Fog objectives. |
| Branch-only residual `entity` rows | 36 | Blocked | These are source-only Levian Bay placements across creatures `16623`, `25988`, `26007`, `26049`, `26071`, `26072`, `26084`, `26134`, `26173`, `26613`, `26620`, `26629`, `26640`, `28883`, `46850`, `50949`, `50968`, `51051`, `51052`, and `51053`; this pass found no current script, Quest2/DataMapping objective bridge, or smoke proof strong enough for promotion. |
| Branch-only `entity_stats` rows | 44 | Blocked | Stats are tied to source-local `@GUID` allocation and cannot be promoted without a deterministic reviewed entity owner. |
| Branch-only `entity_spline` rows | 6 | Blocked | Spline rows are tied to source-local entities and have no movement/client proof in this pass. |
| Branch-only vendor rows | 29 | Blocked | The vendor/category/item rows are all tied to source-local `@GUID+1`; no deterministic vendor placement/catalog proof was found for this Levian Bay slice. |
| Official-only `entity` rows | 20 | Official-retained | Current official WorldDatabase rows remain authoritative; do not delete or replace them from the branch replacement dump. |

Outcome:

- Promotable rows beyond the existing small-world seed: `0`
- Already-covered checklist rows: `14`
- Official-retained rows: `20`
- Rejected placeholder rows: `0`
- Blocked branch-only residual rows: `115` queue rows

## Row Hash Coverage

These hashes are the normalized row identifiers from the generated review
queue. Together they cover every non-identical Levian Bay row present in the
queue for this pass.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Covered branch checklist `entity` rows | Covered | `027770fcd00790db`, `0bcc34cda54f3c8d`, `1961fc7c7523413c`, `2c8e6b55178ff989`, `6eb5a86041e7b656`, `6f4ffe2e273bad5c`, `7c0d89f8f45f22d4`, `8789520ae98eff77`, `97d0c8f4d49f17d5`, `99d97f7077b0f311`, `a775a76a4f27b0a3`, `b3b41e9e3cee2c6f`, `cdab176a16d1ee96`, `f2a3ec3ef0ac9d18` |
| Branch-only residual `entity` rows | Blocked | `00b700cb8856bbd6`, `32234e435c117c1c`, `3321aae6e350eeeb`, `33e4daac44c63c95`, `4a424122732afd50`, `4cddc1ccb826876c`, `50ce06ba72c70883`, `57a5839143384d53`, `5fb0758e78370393`, `667270fd6dcd22a6`, `74fe7ad8a9ac3c84`, `7870121a4c120eb5`, `857c17aa8f98d883`, `85c873dd254b13a6`, `86c4de85e095868d`, `88b3eb3369166bb5`, `89e15adc4dc40116`, `8d73f3ab70c1b6fe`, `9432cc16cc099e9a`, `99a68c126066ec16`, `9b2344319b110dfd`, `9bde6c1562e0b82f`, `b5fe4fff519a2744`, `b7e93d1b2d2773db`, `bece199363a4c610`, `c2e32c99e48ba0f3`, `c423802960ae3cd9`, `cb4e0861cf6c538f`, `cc2d80f610705414`, `d4119f93403fef02`, `daeb6cd89cdcace5`, `dca5b26e35ff481f`, `e57a6f0f11febb47`, `ed3caccbcfba4927`, `f7a95aa7d749873a`, `f96ddf7857383182` |
| Branch-only `entity_stats` rows | Blocked | `025a70689044a276`, `088937ca85834bd8`, `08f5ab908a0a1b86`, `0f0eef5378c4dde2`, `1e291628bb5c2454`, `26c3f4b4f2c95a09`, `2b1837e6e335e459`, `2c8a90455337b70f`, `2ca9ddc594d3c8f9`, `2d8b92a5524e0baf`, `34102810cbd6bdf5`, `34e903f6f9f145ee`, `357ea69ad8ccd438`, `37b841517bca2eaa`, `4183234a02987c9e`, `43a2c279d208a229`, `48fa00937946d45c`, `4b35f69b9c03489d`, `4e1660d11db5b97e`, `592478d9b86343e7`, `624c6190bab2ff63`, `67c6d186d9388cc6`, `6c98fcfb65ad40ff`, `75694bb9cc65acb9`, `75d9ac8336078f99`, `76f18ba020907484`, `7e28e1402775d15d`, `871c0c871d41d1b7`, `88ed5d4cdbb14f64`, `8ec4d5edbb3f9712`, `943dea6204367793`, `962fdbc8642633c9`, `97c70b71fde7dcd6`, `a08f5abd3310927f`, `a3fb0ed61d207b8d`, `ada85b5fce4782e6`, `b5c30c4100885f6f`, `baa8b7a975c228e4`, `cadd6a6bb5f1d70f`, `cdeb7f3bc5161866`, `e0247cb00422cb2c`, `e7651fc5aa209cdd`, `e99fcd3f4c50d210`, `fcff21a7fe8d1ce6` |
| Branch-only `entity_spline` rows | Blocked | `40d0e52cfb45c30e`, `8d59adffc14c65e8`, `b7930f1282de54e8`, `e7eb1f9e45ea0080`, `fa9aa392622648cc`, `fe551671042d1fa7` |
| Branch-only vendor rows | Blocked | `02a03137c119158d`, `0655900ddff9a71c`, `0db770ec0011e01a`, `0ea688a68a7b5a28`, `17f193059c31d347`, `1ad2a088c06cd247`, `295db0342a954873`, `2bffec88d1252a5b`, `2f9f4b2f9d96e72c`, `330c28888fcab964`, `34f97a9202147c47`, `377c737f1ea1acd6`, `50d44c52b4a69bec`, `57f24dc02174e73b`, `6a679f887b3b0af6`, `6bf29f06ac3f4ef8`, `8a0a0e8b9a0cce13`, `8b34425f22112582`, `9c87e537de554a1f`, `9c8b3815dff064cf`, `a7436e1f35d9455c`, `c117605bb198c486`, `c463e42c0f373e53`, `ccba787d3890d887`, `d9277ef5091f15bd`, `de656cfe739cee6a`, `ef3cb3b4e73357fb`, `efa19fc4ea591529`, `f5e31d472fb37b56` |
| Official-only `entity` rows | Official-retained | `00c43e9f8df36d01`, `0281443da4860cec`, `08298da4b74f7053`, `0d640f5fb63bf1c9`, `14d2be3fefcde470`, `17d512909d0252e3`, `35656b141e9ee661`, `3dd2a7fc478ef75c`, `449493c0facdcf11`, `5cc388fb29ca677b`, `6af22a7f34c0b35c`, `75c759edc5dc6389`, `8bc6c76d3bade03c`, `8ed549f1c5df4ce6`, `b58acb0461a3b195`, `beb8a33686743445`, `c9ce6e04b0a65d69`, `cd09a637d634f591`, `eb673e20d49587ba`, `fe28c1d3719410ac` |

Next allowed states:

- Keep `Map/Open World/Olyssia/Levian Bay.sql` classified as
  `covered_by_laughingws_small_world_wip_seed;blocked_laughingws_levian_bay_residual_rows`.
- Reopen only for a specific residual row if a future pass provides current
  script need, quest objective proof, official table corroboration, or local
  retail/client smoke with exact coordinates and row identity.
