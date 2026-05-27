# LaughingWS Celestion Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Alizar/Celestion.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-012 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The source file is still a broad world replacement file, so it is not a setup
input. The current pass found no row that is safe to promote into a
runtime-owned seed.

## Decision Summary

| Delta group | Queue rows | Source row count | Decision | Reason |
| --- | ---: | ---: | --- | --- |
| Branch-only `entity` | 33 | 33 | Blocked | Source-only placed rows with no current script, quest objective, or retail smoke proof in this pass. |
| Branch-only zero-coordinate `entity` | 2 | 2 | Rejected placeholder | Rows `b3a201eda5f2d9dc` and `7ff52738be93a11e` are at `0,0,0` with area `0`, so they remain audit-only placeholders. |
| Branch-only `entity_stats` | 15 | 48 | Blocked | Stats are tied to source-local `@GUID` allocation and have no promoted deterministic entity owner. |
| Branch-only `entity_vendor` | 2 | 10 | Blocked | Vendor stubs are tied to source-local `@GUID` values and have no reviewed vendor category/item proof in the Celestion delta. |
| Official-only `entity` | 11 | 11 | Official-retained | Current official WorldDatabase rows remain authoritative. |
| Official-only `entity_stats` | 8 | 10 | Official-retained | Current official stats stay paired with official entity rows; the branch replacement does not delete or override them. |

Outcome:

- Promotable rows: `0`
- Official-retained rows: `19` queue rows covering `21` source rows
- Duplicate rows promoted or deleted: `0`
- Rejected placeholder rows: `2`
- Blocked branch-only rows: `50` queue rows covering `91` source rows

## Row Hash Coverage

These hashes are the normalized row identifiers from the generated review
queue. Together they cover every branch-only or official-only Celestion row
present in the queue for this pass.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Branch-only `entity` | Blocked, except zero-coordinate hashes rejected as placeholders | `037c923a31e95276`, `08dc868987c72733`, `0a0a50a6dbb6cbfc`, `1c7c2b192ff01f5f`, `1dc1f095dc3d8cac`, `251e92eecd4de8e0`, `2f6965a8e2d531b7`, `3ede50a30e68e433`, `3ee3f4fd76386012`, `407febc92ec73683`, `4687d5989b1c3440`, `5398707556cc29ea`, `55d762fb4d652536`, `5b82349b3a416b5d`, `6d603e52c2f61aff`, `7ff52738be93a11e`, `8c00987b9cb61ecd`, `8f47280bbd177dd5`, `9336ed23cf263dd1`, `9d2f8deeec0e1be7`, `b3a201eda5f2d9dc`, `c5750f556bf32828`, `ce645e326abb64ca`, `d1d25d0f9682bb6c`, `d82145eb8ed80658`, `d886ad39030cb051`, `de9f6ac3a3655bcb`, `e55d896d73e484e4`, `e997022f3259c93f`, `f3efce568c7383a8`, `f4324364b0c2e656`, `f720616e27440864`, `f7a3b718b88ae73b`, `fd8e49d7ea0315d5`, `fe055941d6549198` |
| Branch-only `entity_stats` | Blocked | `025a70689044a276`, `0f2579cfeea18f80`, `16057c8972d6c0b4`, `27d3b7345ae33787`, `3117e34303d89807`, `487178efa17e599b`, `4fc4102040de5114`, `775c39d0372d5f16`, `79de63fe4d804de6`, `905c18097a649500`, `a08f5abd3310927f`, `b522d7509e7566df`, `baa8b7a975c228e4`, `efe062e572df8c19`, `f341c309b8976536` |
| Branch-only `entity_vendor` | Blocked | `8b34425f22112582`, `980af7bc1c7b3a38` |
| Official-only `entity` | Official-retained | `028395d780accc84`, `3d9716304ee2a574`, `60a74e1c47323833`, `627e19317ac88b7a`, `6e06078c1be4a797`, `7518493da42bbd43`, `7d45e2536950df1d`, `918bd14940dcd7e0`, `c3616b6d4e1b8c79`, `cd1b5ed1c1384a31`, `f1c9ea029d10d5f1` |
| Official-only `entity_stats` | Official-retained | `17e8e275db5bad26`, `29680310d6cfe965`, `3766c72ad7ccd66b`, `467934887183e99d`, `7fdfdd01d9f62a5a`, `9acc12b35b4a4afd`, `b0963d2306b72ab1`, `fdf287f435ef974a` |

Next allowed states:

- Keep `Map/Open World/Alizar/Celestion.sql` audit-only as
  `blocked_laughingws_celestion_residual_rows`.
- Reopen only for a specific row if future evidence maps a current quest,
  script, vendor catalog, deterministic placement, or retail/client smoke path.
