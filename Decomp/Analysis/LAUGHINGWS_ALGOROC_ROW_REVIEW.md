# LaughingWS Algoroc Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Alizar/Algoroc.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-011 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The source file still has a destructive world replacement pattern, so no raw
rows are setup inputs. The row-level decision is conservative: official-only
rows stay authoritative, and branch-only rows remain blocked until a future
quest/script/client-smoke pass proves a narrow runtime need.

## Decision Summary

| Delta group | Queue rows | Source row count | Decision | Reason |
| --- | ---: | ---: | --- | --- |
| Branch-only `entity` | 34 | 34 | Blocked | Source-only Algoroc placements with no current script, quest smoke, or retail placement proof in this pass. One Pure Loftite Vein coordinate overlaps an official row but differs by normalized row payload, so the official row is retained instead of replacing it. |
| Branch-only `entity_stats` | 29 | 85 | Blocked | Stats are tied to source-local `@GUID` allocation (`@GUID+1` and `@GUID+57`) and include broad replacement stat payloads without a promoted deterministic entity owner. |
| Branch-only `entity_vendor` | 1 | 8 | Blocked | Vendor stubs are tied to source-local `@GUID+1` and have no paired reviewed vendor category/item catalog proof in the Algoroc delta. |
| Official-only `entity` | 5 | 5 | Official-retained | Current official WorldDatabase rows remain authoritative; do not delete or replace them from the branch replacement dump. |

Outcome:

- Promotable rows: `0`
- Official-retained rows: `5`
- Duplicate rows promoted or deleted: `0`
- Rejected placeholder rows: `0`
- Blocked branch-only rows: `64` queue rows covering `127` source rows

## Row Hash Coverage

These hashes are the normalized row identifiers from the generated review
queue. Together they cover every branch-only or official-only Algoroc row
present in the queue for this pass.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Branch-only `entity` | Blocked | `0159ede62291d8d7`, `020d2fccff16bc62`, `097f6dcb9f3a9cdc`, `0a11df08ba7b960c`, `0d74ee885db252b6`, `146b6d028e31edea`, `16009b0c650834cf`, `19765cb620d11fae`, `19dc635471bf4aa4`, `2bbd566359d140fe`, `2c108c7a82e77cc0`, `49d7a4d23d1e5911`, `4d27e2c0f1a22378`, `4db3d0ccffe44939`, `500776484f833b2d`, `563ef040ecb6af97`, `7db4d0fc296589fa`, `8414de2980b6ea92`, `87abfcf250a28fa6`, `93b963d8770fa858`, `954f89cc21af00dd`, `9ce16fccff972bf8`, `afe212ade3292dd7`, `b5a5aa76e7a9d1e7`, `b5f70d0f3969ca18`, `c0c14a12be039129`, `c4da6231fc74431b`, `e1874b00de274672`, `eeaddbcf7861c023`, `f3ebf71bfc3a2635`, `f48d76a752ca1c6e`, `fbb8cc7a3a45d9e5`, `fbfd4dc46005e107`, `fdae4cd6657fa9e9` |
| Branch-only `entity_stats` | Blocked | `01401064c3097f32`, `06b313a711595a7a`, `0f2579cfeea18f80`, `1d415636c3676a66`, `37d544bd74c44bd1`, `487178efa17e599b`, `48fa00937946d45c`, `4b35f69b9c03489d`, `573aa0a1a78a7309`, `76daea6f503b945e`, `775c39d0372d5f16`, `79de63fe4d804de6`, `7f7fc079eb640328`, `8ec4d5edbb3f9712`, `8fda9915ee9cb046`, `905c18097a649500`, `943dea6204367793`, `962fdbc8642633c9`, `a08f5abd3310927f`, `a4842a79aaf4734d`, `b522d7509e7566df`, `baa8b7a975c228e4`, `cfe34415958ce8af`, `e00945372de5d0b4`, `e1666b8338899bf1`, `e7651fc5aa209cdd`, `ebdedeaa62485912`, `f4b3f3d67bd2448b`, `fb2f0d6fef695468` |
| Branch-only `entity_vendor` | Blocked | `8b34425f22112582` |
| Official-only `entity` | Official-retained | `0aec29760501851e`, `2e8e8343ee225440`, `79b4dbd55d72324d`, `b1dd2005dd5fc846`, `e71b2835a574c05f` |

Next allowed states:

- Keep `Map/Open World/Alizar/Algoroc.sql` audit-only as
  `blocked_laughingws_algoroc_residual_rows`.
- Reopen only for a specific row if a future pass provides current script need,
  quest objective proof, official table corroboration, or local retail/client
  smoke with exact coordinates and row identity.
