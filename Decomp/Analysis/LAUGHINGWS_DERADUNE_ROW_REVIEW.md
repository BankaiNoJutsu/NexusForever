# LaughingWS Deradune Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Olyssia/Deradune.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-016 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The source file is a broad world replacement file. The checklist-index rows
called out by the audit are branch-only entity inserts, not coordinate-keyed
updates to official rows, so they are not promoted without current quest/table
or retail smoke proof for the exact creature and placement.

## Decision Summary

| Delta group | Queue rows | Source row count | Decision | Reason |
| --- | ---: | ---: | --- | --- |
| Branch-only `entity` rows with `questChecklistIdx` | 47 | 47 | Blocked | Checklist values are branch-only placements and lack current Quest2/DataMapping plus smoke proof for safe objective mapping in this pass. |
| Branch-only `entity` rows without `questChecklistIdx` | 30 | 30 | Blocked | Source-only Deradune placements with no current script, quest objective, or retail placement proof in this pass. |
| Branch-only `entity_stats` | 31 | 35 | Blocked | Stats are tied to source-local `@GUID` allocation and have no promoted deterministic entity owner. |
| Official-only `entity` | 200 | 200 | Official-retained | Current official WorldDatabase rows remain authoritative. |
| Official-only `entity_stats` | 305 | 530 | Official-retained | Current official stats stay paired with official rows. |

Outcome:

- Promotable rows: `0`
- Official-retained rows: `505` queue rows covering `730` source rows
- Duplicate rows promoted or deleted: `0`
- Rejected placeholder rows: `0`
- Blocked branch-only rows: `108` queue rows covering `112` source rows

## Checklist Hash Coverage

These are the branch-only checklist row hashes prioritized by LWS-016. They are
all blocked pending exact quest/table/smoke proof:

`5045e394de0ee805`, `368658d48d31e1b5`, `27609ea90dcde09c`,
`fe24557494056892`, `603dfbdc8782cdd9`, `a534b44afeb47221`,
`6573972015bad925`, `e8ed57ba04da5301`, `9309ba2cda93fe52`,
`bcfc4cc4df2640d8`, `f395af3e34c02ab2`, `43c2379d1ad5902f`,
`e74f84c4cede9bd3`, `bb90c8e68f1d7464`, `016ecbd2d1b8c3f6`,
`52d2a23414995c7d`, `4b4823961d2fe7f1`, `1eb8c6466f773161`,
`dfe75ca932349afb`, `8fed0cc6ff250d17`, `88efece5f6b5927c`,
`4a4c341879a01436`, `b9d1221b4f770ba1`, `a9e96cec3c263baf`,
`4fb222196f6423ff`, `c88dc6a8d103cbd4`, `c9a04157f1e31322`,
`60978ed1fddc2600`, `badfc5fd90314c83`, `ee00d18a0ec0a530`,
`15b3537b63e8cd9a`, `8a260214b0457f1e`, `6932d284d708a91b`,
`ac7696d2702ec199`, `b83ba31410ebbef0`, `11ea74fb1b1ec87a`,
`b497b0ec0e48e548`, `b0ff301e52bebd90`, `4f55da2b58c3cb46`,
`28c79a2789c0bfa4`, `eaf2855ec37d5f64`, `740e29a29c33419b`,
`14ee5e439ad446da`, `d3324151bc095bc8`, `a3fcdfc590db0200`,
`e6e28ba24835b4b7`, `b8684d90717e89bc`

The remaining non-checklist branch entities and stats are blocked as the
`branch_only` groups above, and every `official_only` row is retained as a
class.

Next allowed states:

- Keep `Map/Open World/Olyssia/Deradune.sql` audit-only as
  `blocked_laughingws_deradune_residual_rows`.
- Reopen a checklist row only if future evidence maps its creature,
  coordinates, objective/checklist index, and runtime behavior.
