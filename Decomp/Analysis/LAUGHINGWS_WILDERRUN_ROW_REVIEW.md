# LaughingWS Wilderrun Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Olyssia/Wilderrun.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-035 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The useful Wilderrun checklist rows are already covered by
`Tools/DataMapping/extract_laughingws_small_world_overlays.py`; this pass
resolves the remaining Dorian rows against current DataMapping outputs.

## Decision Summary

| Delta group | Queue rows | Decision | Reason |
| --- | ---: | --- | --- |
| Covered branch checklist `entity` rows | 33 | Covered by small-world seed | Branch `questChecklistIdx` values for Kel Ulgar Weapon Racks, Tattered Banner, Intact Data Cache, and Cassian Commonwealth Emblem are already emitted as WIP/GUESSED coordinate-keyed updates to retained official rows because DataMapping maps these creature groups to current Wilderrun quest objectives. |
| Branch-only Dorian `entity` rows | 4 | Blocked | The source rows use creature ids `27455`, `27843`, and `58746`, but current `creature_map.csv`, `creature_client_metadata_map.csv`, and `creature_spawn_map.csv` map Dorian Walker to creature `25779` or `51292`, with related objects such as Dorian's Explorer Beacon `27841`, Dorian's Speeder `29676`, Dorian's Map `51761`, Dorian's Ship `53936`, and Dorian's Datachron `47594`. No current DataMapping identity selects the branch creature ids. |
| Branch-only Dorian `entity_stats` row | 1 | Blocked | The stat row is tied to source-local `@GUID+1` and cannot be promoted without a deterministic reviewed Dorian entity owner. |
| Official-only `entity` rows | 33 | Official-retained | Current official rows remain authoritative; the branch checklist values are applied as coordinate-keyed updates by the small-world seed rather than replacing official rows. |

Outcome:

- Promotable rows beyond the existing small-world seed: `0`
- Already-covered checklist rows: `33`
- Official-retained rows: `33`
- Blocked branch-only Dorian rows: `5` queue rows

## DataMapping Identity Check

Current DataMapping rows for `source_name = Dorian Walker` map to:

| Jabbithole ids | Current creature id | World ids observed | Display/outfit |
| --- | ---: | --- | --- |
| `711`, `1731`, `7816`, `11103`, `12402`, `14401`, `19242`, `28560`, `30300` | `25779` | `1421`, `51`, `22`, `1537`, `1263`, `1061` | `25510` / `8952` |
| `14838`, `29482` | `51292` | `1833` | `25510` / `8952` |

The branch rows share Dorian's display/outfit (`25510` / `8952`) but not a
current creature identity. Matching on appearance alone is insufficient for a
runtime-owned seed.

## Row Hash Coverage

These hashes are the normalized row identifiers from the generated review
queue. Together they cover every non-identical Wilderrun row present in the
queue for this pass.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Covered branch checklist `entity` rows | Covered | `0564643d29bd164d`, `08fbeaf5b187a30d`, `0d403081fc50ebbe`, `0e70f17c2bff5bf1`, `1ce61868f5ae6480`, `25ee812dce26127f`, `329fedde10dd4657`, `3479c53a292b2580`, `4a63a4a7591fe2c5`, `5507e02fb227168d`, `69bfee6215a7fb0d`, `718adcc83c9bd8f0`, `7303c73796055fb0`, `7326bfd10088273c`, `75e46d05ffbd8ca2`, `8324da2a8fea9f44`, `841ae7029d72e59c`, `92b5d6a968d6a3da`, `979f0109c4416eb6`, `9c680e50d491be95`, `9cee92def3e59c5b`, `9e498d131ecc623c`, `9e5f7af542c28ca8`, `9f87da2da8a616b1`, `a7878ce5492127e7`, `ab0d692f675dfb76`, `c143cda196c7f6d1`, `c7a76843e5ec0340`, `cd1bc759640209ec`, `d545bce563a67ecd`, `d6e26b4a847789e1`, `e508c6b4ccbd8ab3`, `fbbf2acccaab569b` |
| Branch-only Dorian `entity` rows | Blocked | `24642b40dc59379c`, `31fef227c51a2502`, `34190e66248c6184`, `585ceafe743bc770` |
| Branch-only Dorian `entity_stats` row | Blocked | `a08f5abd3310927f` |
| Official-only `entity` rows | Official-retained | `06d8df1ec38bd740`, `170a703fa9112b27`, `204fbe730b8a8791`, `24111d76794cef8b`, `39f65b2675e49c53`, `3e7ebb1ebc332e73`, `474b36dba6e0ff4c`, `4c23d515ca3c7d8b`, `5297162f2806fd3f`, `59154f1c0349bddd`, `64b5f9bca9d038ec`, `6a67e60c2d576886`, `6e8b60846fd98ff7`, `7aea74e72be1d662`, `8311104202ec9c5d`, `887f0020bd0505d8`, `9a44079f201be526`, `a49c1be16af4aedb`, `a4f94a8edfa7acb0`, `a6944ca4a8d841c7`, `ad0b8a84e6554730`, `b4c9f4d62f8de760`, `b722aca29323633c`, `b97be082d1587057`, `badf38f37e3b9696`, `c09c81d871350dab`, `cd616c67aea26cf3`, `d19796067078210e`, `da3e9d02b534c01c`, `dd02412bb8354d10`, `e8033477ab8f81bd`, `f022ed5c5d390260`, `f69a534a88393e81` |

Next allowed states:

- Keep `Map/Open World/Olyssia/Wilderrun.sql` classified as
  `covered_by_laughingws_small_world_wip_seed;blocked_laughingws_wilderrun_dorian_rows`.
- Reopen the Dorian rows only if a future pass proves the exact branch creature
  ids against current client/reference tables or live-client smoke with row
  identity, coordinates, and interaction need.
