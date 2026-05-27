# LaughingWS Whitevale Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Alizar/Whitevale.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-015 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The source file is a broad world replacement file. This pass found no row with
enough current quest, script, vendor, or retail smoke proof to promote into a
runtime-owned seed.

## Decision Summary

| Delta group | Queue rows | Source row count | Decision | Reason |
| --- | ---: | ---: | --- | --- |
| Branch-only `entity` | 63 | 63 | Blocked | Source-only Whitevale placements with no current script, quest objective, or retail placement proof in this pass. |
| Branch-only `entity_stats` | 42 | 79 | Blocked | Stats are tied to source-local `@GUID` allocation and have no promoted deterministic entity owner. |
| Official-only `entity` | 14 | 14 | Official-retained | Current official WorldDatabase rows remain authoritative. |
| Official-only `entity_stats` | 1 | 1 | Official-retained | Current official stats stay paired with official rows. |

Outcome:

- Promotable rows: `0`
- Official-retained rows: `15` queue rows covering `15` source rows
- Duplicate rows promoted or deleted: `0`
- Rejected placeholder rows: `0`
- Blocked branch-only rows: `105` queue rows covering `142` source rows

## Row Hash Coverage

These hashes are the normalized row identifiers from the generated review
queue. Together they cover every branch-only or official-only Whitevale row
present in the queue for this pass.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Branch-only `entity` | Blocked | `0b17524425249352`, `1574ccf8d5b8f59b`, `15803b5607d2951b`, `18621fe3fc96ea44`, `1960aa1fd1763216`, `198875d567e24bca`, `2219840cc554db3a`, `2320f1c4eea7ec06`, `23e012008562ed7e`, `24febf64f8179023`, `250b9903f5cc227b`, `263c1ced10f29235`, `2d2f189fc474f9e1`, `2e06f928ade7b3ed`, `305a33eaed13ce6c`, `316d6aefa6f9e11d`, `31778a22c9bbc669`, `39075961f5f571df`, `3d81bb6f296786ab`, `3fa6af1b6a4b9970`, `44e38e15aec12b9b`, `44f232b4f9b5174e`, `4cd52dc53d5da86a`, `4fbc926b8dea6b31`, `526774d336719807`, `54d5befdf93855fa`, `58566ffa0617ee27`, `621b6e389ca0b127`, `638a2c36e794b8f6`, `6b7f3edf74e1aec5`, `7906418fdd05180b`, `79eec92751993315`, `7a557f355c343481`, `7c88de80c639e488`, `8053dea88acc47f3`, `84f3a45431e81afc`, `860fb7e5e9510410`, `884906fb2eb9d071`, `88ade09c2032b217`, `8d87c0086182fad3`, `924ad9861e7db964`, `92a2c73e421f3898`, `92a6c23f3c1b5055`, `944aee733f7dc935`, `ac4f7c4a181302cc`, `acddfa7481afca19`, `ad90040aa3fbf49c`, `aec4acc4286cdc4c`, `b032011347a0ce4e`, `b1e442a1bd29543a`, `bd34357025b5b932`, `c3a6d507936d8ea1`, `c830e0991cbd6fff`, `ca7ea7b45ce9d60b`, `cad60b15eaad74a4`, `d192dce8feb91d5c`, `d1d63c23e56da087`, `dc6dfc6689403275`, `dc7b780aa42561f7`, `ddcca2d96c5403e4`, `deec273040b9ac0c`, `f1a1dbe01986a708`, `f52d11608bacce66` |
| Branch-only `entity_stats` | Blocked | `0142ae08a1cddcdd`, `0c2f163c0c0f2f8b`, `0f2579cfeea18f80`, `1e291628bb5c2454`, `2173d0dfc3731231`, `2c67a841b2eb3f61`, `2f9ed01020fb6e7d`, `300b6663ce94c902`, `3766c72ad7ccd66b`, `379a700498e6dc32`, `3889400bab19db79`, `487178efa17e599b`, `48fa00937946d45c`, `4b35f69b9c03489d`, `51f4e4dc83f94a56`, `592478d9b86343e7`, `5e24b729d4d02bbb`, `71ff225d2cc98fd9`, `72e2b680dcb9d84e`, `73f6727d31c6955c`, `79de63fe4d804de6`, `7e28e1402775d15d`, `858181b3ecd14640`, `88ed5d4cdbb14f64`, `8e25b2bed096c61b`, `8ec4d5edbb3f9712`, `905c18097a649500`, `9399f249cbf9ae87`, `943dea6204367793`, `9acc12b35b4a4afd`, `9bb5c16bb58cfb69`, `a08f5abd3310927f`, `a387b7716daa4f63`, `af592e2c52a8bc57`, `b0963d2306b72ab1`, `b522d7509e7566df`, `baa8b7a975c228e4`, `d1eebef237c1b733`, `dc8bed989be4c773`, `ddcd7cf6656e1325`, `e7651fc5aa209cdd`, `f90527aecb6f4f28` |
| Official-only `entity` | Official-retained | `12fdf94181870878`, `1ec62f0c8714952e`, `28011fa6562ec3fd`, `2eeaff28eec9e303`, `36b6cdf259793883`, `41147fdce4fecb0f`, `4c454999450159a9`, `536b73121a88b7eb`, `6a2a708b56a92c48`, `6afb8d766b898805`, `80753bede9b874fe`, `a858e4cb573231e0`, `b63b243a61106dad`, `f8d91898cb6e59b6` |
| Official-only `entity_stats` | Official-retained | `9f98bb6815ec5d7d` |

Next allowed states:

- Keep `Map/Open World/Alizar/Whitevale.sql` audit-only as
  `blocked_laughingws_whitevale_residual_rows`.
- Reopen only for a specific row if future evidence maps a current quest,
  script, deterministic placement, or retail/client smoke path.
