# LaughingWS Crimson Isle Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Olyssia/Crimson Isle.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-033 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The useful Crimson Isle checklist rows are already covered by
`Tools/DataMapping/extract_laughingws_small_world_overlays.py`; this pass
classifies the remaining Mondo Zax/Kezrek Warbringer/Supply Officer/fake
teleporter residual rows.

## Decision Summary

| Delta group | Queue rows | Decision | Reason |
| --- | ---: | --- | --- |
| Covered branch checklist `entity` rows | 24 | Covered by small-world seed | Nonzero branch `questChecklistIdx` values for Megatech Terminals, Exile Anti-Air Cannons, Dreg Tents, Dominion Demolitions Experts, Power Regulators, and Tower Controls are already emitted as WIP/GUESSED coordinate-keyed updates/fallback inserts because Quest2/DataMapping maps those creature groups to current Crimson Isle objectives. |
| Branch-only residual `entity` rows | 4 | Blocked | The residual entity rows are the named quest-giver/teleporter suspects (`24337`, `24338`, `24491`, `25886`). The branch `25886` row uses fake `activePropId = 1`, and the quest-giver rows lack current script/smoke proof in this pass. |
| Branch-only `entity_stats` rows | 12 | Blocked | Stats are tied to source-local `@GUID` allocation and cannot be promoted without deterministic reviewed entity owners. |
| Branch-only vendor rows | 58 | Blocked | The Supply Officer-style vendor/category/item rows are tied to source-local `@GUID+1` / `@GUID+2`; no deterministic vendor placement/catalog proof was found for this Crimson Isle slice. |
| Official-only `entity` rows | 24 | Official-retained | Current official WorldDatabase rows remain authoritative; do not delete or replace them from the branch replacement dump. |

Outcome:

- Promotable rows beyond the existing small-world seed: `0`
- Already-covered checklist rows: `24`
- Official-retained rows: `24`
- Rejected placeholder rows: `0`
- Blocked branch-only residual rows: `74` queue rows

## Row Hash Coverage

These hashes are the normalized row identifiers from the generated review
queue. Together they cover every non-identical Crimson Isle row present in the
queue for this pass.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Covered branch checklist `entity` rows | Covered | `03be72a6ffc0fd5c`, `04ef877c6f367dcd`, `0cf0ed16bb4a3acf`, `0efe926a3f16db9e`, `1cccfbedd48d550d`, `1dc553f1d1ad37df`, `4f3e8cd59534f6b9`, `544858c79fb1253e`, `5cc8e1431c55ba16`, `77b7b616b4222cc2`, `7873d91e3f52e845`, `80356fa73ba5ca90`, `8d6e311742d535fb`, `9014e5c2b6b031bc`, `95b1870b6073e8c3`, `9d6272811c5be092`, `9ee1a861fdc4703d`, `a1f4e31b675ab961`, `bbd7a466045055ba`, `d5ae32d7b9168207`, `da55756db18012b8`, `da690ca798a33479`, `dfff7ffe84083887`, `ef009a04feaea451` |
| Branch-only residual `entity` rows | Blocked | `07e9373ec362d531`, `24d4e2effa8b3ebe`, `785d932ad5c60ef6`, `b8f3a53890da90e5` |
| Branch-only `entity_stats` rows | Blocked | `0da9d6abf3f26434`, `37ee7a594f2b3d40`, `48fa00937946d45c`, `4b35f69b9c03489d`, `5b350764383ccd5f`, `79de63fe4d804de6`, `8ec4d5edbb3f9712`, `905c18097a649500`, `943dea6204367793`, `b0963d2306b72ab1`, `b522d7509e7566df`, `baa8b7a975c228e4` |
| Branch-only vendor rows | Blocked | `0258937986eaca16`, `02a03137c119158d`, `0655900ddff9a71c`, `092085c07e4eb6d0`, `0db770ec0011e01a`, `0ea688a68a7b5a28`, `108a11d221857215`, `11e4fd57d5b1b6bc`, `17f193059c31d347`, `19436f71c8ae40a1`, `1ad2a088c06cd247`, `2798712c3034d020`, `295db0342a954873`, `2bcf4686db3d3a70`, `2bffec88d1252a5b`, `2f0ae4b1c489155a`, `2f9f4b2f9d96e72c`, `32b03792a4b644ba`, `330c28888fcab964`, `34f97a9202147c47`, `377c737f1ea1acd6`, `3abf38ccf56c2b90`, `47c7fa534be91855`, `48dded0fbb532ac3`, `4aef84216c81a0ce`, `4dadacedeba595cd`, `4f24b9b14d8b12e1`, `50d44c52b4a69bec`, `5673990bff9695e0`, `57f24dc02174e73b`, `5a4abf131feba0dc`, `6a679f887b3b0af6`, `6bf29f06ac3f4ef8`, `6ea2ad8969a05105`, `85fd7f30527effab`, `8a0a0e8b9a0cce13`, `8b34425f22112582`, `9413fa92e26a8315`, `980af7bc1c7b3a38`, `9c87e537de554a1f`, `9c8b3815dff064cf`, `a01f74272313307e`, `a7436e1f35d9455c`, `a8c738ffdd3ae815`, `bbee98afc8c0b3c1`, `c117605bb198c486`, `c463e42c0f373e53`, `c57156f5bcfcbec8`, `ccba787d3890d887`, `d9277ef5091f15bd`, `de656cfe739cee6a`, `e4e367ab7c449eed`, `ef3cb3b4e73357fb`, `efa19fc4ea591529`, `f289e5d009570afe`, `f2e106d3d4fcb197`, `f5e31d472fb37b56`, `fe15d5f522f67b2a` |
| Official-only `entity` rows | Official-retained | `10ac614bb0398378`, `114322bec7150974`, `15192de10c31fc8e`, `276b55f2382a2cb4`, `2f5e0f42af43e90f`, `3055a5e31959a7b3`, `353297051d300caf`, `38b09a26deeef827`, `5a521255dc4c63a7`, `5ea3b4d8f2605607`, `5f8e099551e69c8f`, `62929469e7ab126a`, `63f89b2179fa0b4a`, `82bf4782e5afe86e`, `8352cf5e663927f9`, `8adf2d815168e3cc`, `9616509795826d23`, `9816d607f4c03ce1`, `9f3563f6051e2309`, `a9e26e882a3c8ccf`, `b942b736defda09b`, `bb85f6b8d6c0eda7`, `d6592cdd9a837392`, `e7fc6c6f5b9af054` |

Next allowed states:

- Keep `Map/Open World/Olyssia/Crimson Isle.sql` classified as
  `covered_by_laughingws_small_world_wip_seed;blocked_laughingws_crimson_isle_residual_rows`.
- Reopen only for a specific residual row if a future pass provides current
  script need, quest objective proof, official table corroboration, or local
  retail/client smoke with exact coordinates and row identity.
