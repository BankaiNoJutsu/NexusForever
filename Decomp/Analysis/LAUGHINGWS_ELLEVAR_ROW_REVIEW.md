# LaughingWS Ellevar Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Olyssia/Ellevar.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-017 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The source file is a broad world replacement file. The checklist-index rows
called out by the audit are branch-only entity inserts, not coordinate-keyed
updates to official rows, so they are not promoted without current quest/table
or retail smoke proof for the exact creature and placement.

## Decision Summary

| Delta group | Queue rows | Source row count | Decision | Reason |
| --- | ---: | ---: | --- | --- |
| Branch-only `entity` rows with `questChecklistIdx` | 65 | 65 | Blocked | Checklist values are branch-only placements and lack current Quest2/DataMapping plus smoke proof for safe objective mapping in this pass. |
| Branch-only `entity` rows without `questChecklistIdx` | 24 | 24 | Blocked | Source-only Ellevar placements with no current script, quest objective, or retail placement proof in this pass. |
| Branch-only `entity_stats` | 39 | 57 | Blocked | Stats are tied to source-local `@GUID` allocation and have no promoted deterministic entity owner. |
| Branch-only `entity_vendor` | 1 | 6 | Blocked | Vendor stubs are tied to source-local `@GUID+1` with no reviewed vendor category/item owner proof. |
| Official-only `entity` | 51 | 51 | Official-retained | Current official WorldDatabase rows remain authoritative. |

Outcome:

- Promotable rows: `0`
- Official-retained rows: `51`
- Duplicate rows promoted or deleted: `0`
- Rejected placeholder rows: `0`
- Blocked branch-only rows: `129` queue rows covering `152` source rows

## Checklist Hash Coverage

These are the branch-only checklist row hashes prioritized by LWS-017. They are
all blocked pending exact quest/table/smoke proof:

`504adb9ead20d9a9`, `d557e61693f8793e`, `902478f8b72f7877`,
`07ad9b67cb16327d`, `60606ba36a2fd1bd`, `6dfcf5bf2c99292c`,
`cc66f763239602b0`, `360a5063dab63145`, `ac3831adb5513e3f`,
`3bf9db478405607d`, `2b3842832ed07abb`, `54894515141d08b2`,
`0ae800cf1bc8196c`, `8726d90bc0a309bb`, `00f764a4cc183615`,
`b0767f3ab1cfd1fb`, `080dc771df7891d3`, `a3730628f21277c8`,
`9d8da993172fc678`, `60a427a11f2f37fd`, `2ff1cc0d0de130b8`,
`b69f05a3287791ee`, `be63b7461dfa52a2`, `8a5bfb21496b43d0`,
`aa9bd761761d08fa`, `82fe609a92cb8f56`, `fe72f6b887297922`,
`a651e9595a4f8117`, `98db7237c14d8f97`, `68e5de645758efae`,
`9fc08f87d343929f`, `50b96a8865e9252c`, `9f989e4543173f91`,
`b1195427ed3acc8a`, `af78c3b9495f0e2a`, `fbbadab7182a93c6`,
`b0e05448e9ac0128`, `b611093e05145dbf`, `bcc4fae2c13004db`,
`5a14de0fcd77d67d`, `583cb8add1503e43`, `cbba4e1888c8dd70`,
`7b977343251daf5f`, `f4e60869376c6a0e`, `53ac0ed46528205f`,
`dbee5248c54ec2de`, `d4ae0855f0d0c352`, `887c0136cfe702a1`,
`537aac5646aa6060`, `23c0190f5eab1713`, `4cb53baedff2f335`,
`1ac26db45d9011b8`, `4ad0916b9c3134a3`, `a799556895315403`,
`2fc492533231a725`, `6e202d4de4dcfcc6`, `de27cebece97dab8`,
`12875668a1a230f7`, `5975468efb4fda81`, `6744a7c021a4ba45`,
`cbacd06c9e0a5ae3`, `9b6c2e5c10341fe1`, `c126807e57cab0c1`,
`82e62c792450f41d`, `eb6a6fb615389d0b`

The remaining non-checklist branch entities, stats, and vendor row are blocked
as the `branch_only` groups above, and every `official_only` row is retained as
a class.

Next allowed states:

- Keep `Map/Open World/Olyssia/Ellevar.sql` audit-only as
  `blocked_laughingws_ellevar_residual_rows`.
- Reopen a checklist row only if future evidence maps its creature,
  coordinates, objective/checklist index, and runtime behavior.
