# LaughingWS Thayd Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Alizar/Thayd.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-014 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The source file remains a broad replacement file with `DELETE FROM entity WHERE
world = @WORLD`, so the replacement SQL is not a setup input. The already
reviewed housing intro slice remains covered by
`Tools/DataMapping/sql/laughingws_city_content_seed.sql`; the remaining Thayd
delta is blocked or official-retained.

## Decision Summary

| Delta group | Queue rows | Source row count | Decision | Reason |
| --- | ---: | ---: | --- | --- |
| Branch-only housing intro `entity` rows | 3 | 3 | Already covered | Creatures `54400`, `54403`, and `54404` are the branch housing intro story-panel props already handled by the city-content seed as WIP/GUESSED coordinate-keyed update/fallback content. No new seed is emitted here. |
| Branch-only `entity_vendor` | 4 | 48 | Blocked | Vendor stubs are tied to source-local `@GUID` values and cannot be safely attached to a deterministic reviewed vendor owner from this broad replacement file. |
| Branch-only `entity_vendor_category` | 2 | 2 | Blocked | Categories are tied to source-local `@GUID+1` and lack safe reviewed vendor ownership. |
| Branch-only `entity_vendor_item` | 19 | 19 | Blocked | Items are tied to source-local `@GUID+1` and are not promoted without a proven vendor placement/catalog owner. |
| Official-only `entity` | 727 | 727 | Official-retained | These are current official WorldDatabase rows that the branch replacement omits; keep them authoritative. |
| Official-only `entity_stats` | 919 | 2,228 | Official-retained | These stats stay paired with official rows; the branch replacement must not delete or replace them. |

Outcome:

- New promotable rows: `0`
- Already-covered rows: `3`
- Official-retained rows: `1,646` queue rows covering `2,955` source rows
- Duplicate rows promoted or deleted: `0`
- Rejected placeholder rows: `0`
- Blocked branch-only vendor rows: `25` queue rows covering `69` source rows

## Branch Row Hash Coverage

The official-only groups above apply to every Thayd queue row whose `side` is
`official_only`; those rows remain official-retained as a class. The branch-only
hashes below cover every non-official row that needed a direct decision.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Branch-only housing intro `entity` | Already covered by city-content seed | `1ba7819c445605ed`, `59db259e7c2db671`, `be911e11441fb0b8` |
| Branch-only `entity_vendor` | Blocked | `1825f01be2edbcc3`, `3ca65a1b9fd93431`, `8b34425f22112582`, `980af7bc1c7b3a38` |
| Branch-only `entity_vendor_category` | Blocked | `b26e772b3d895721`, `ee7b11d11cbe2cf7` |
| Branch-only `entity_vendor_item` | Blocked | `07902fa5c2aa2a41`, `1a164e9f309cec7f`, `5554c6aca1e189de`, `5d3fe25d52d026f9`, `5f3e653da4d8d998`, `756e7619e15a0cf0`, `7b10cc32945e0ca2`, `7c3ca3e3cae92792`, `7eef8125e0168d3b`, `828dd4d2a30bac54`, `97e1ceb5cfc6cf75`, `bd1182d18e4b8e7b`, `be69a67d10a19b8c`, `c18e686a0ce77760`, `c72f77a50bfa11e0`, `c80cb530e8fc5a1b`, `d75ec2c1060102a4`, `e23a95f0058fbc2b`, `eccbea80b47deeeb` |

Next allowed states:

- Keep `Map/Open World/Alizar/Thayd.sql` classified as
  `covered_by_laughingws_city_content_seed;blocked_laughingws_thayd_residual_rows`.
- Reopen only for a specific branch vendor row if future evidence proves a
  deterministic vendor entity, category, item list, and retail/client behavior.
