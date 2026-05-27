# LaughingWS Auroria Row Review

Updated: 2026-05-27

Source file: `Map/Open World/Olyssia/Auroria.sql`

Audit source:
`artifacts/laughingws_worlddb_audit_latest/laughingws_worlddb_row_review_queue.csv`

This review closes LWS-032 from
`Decomp/Analysis/LAUGHINGWS_REMAINING_BLOCKERS_IMPLEMENTATION_PLAN.md`.
The useful Auroria checklist rows are already covered by
`Tools/DataMapping/extract_laughingws_small_world_overlays.py`; this pass
classifies the remaining branch-only rows.

## Decision Summary

| Delta group | Queue rows | Decision | Reason |
| --- | ---: | --- | --- |
| Covered branch checklist `entity` rows | 67 | Covered by small-world seed | Nonzero branch `questChecklistIdx` values for Black Hoods Alchemy Supplies, Cubig Security Zapper Console, Seismic Thumper, Hycrest infected/plague-victim clusters, Freebot Drill, Navigation Control Panel, Bingberry Still, and GL-04 Auto Turrets are already emitted as WIP/GUESSED coordinate-keyed updates/fallback inserts because Quest2/DataMapping maps those creature groups to current Auroria objectives. |
| Branch-only residual `entity` rows | 41 | Blocked | Residual source-only placements, including zero-checklist rows in otherwise covered creature groups, have no current script, Quest2/DataMapping objective bridge, or smoke proof strong enough for promotion in this pass. |
| Branch-only `entity_stats` rows | 15 | Blocked | Stats are tied to source-local `@GUID` allocation and cannot be promoted without a deterministic reviewed entity owner. |
| Official-only `entity` rows | 74 | Official-retained | Current official WorldDatabase rows remain authoritative; do not delete or replace them from the branch replacement dump. |

Outcome:

- Promotable rows beyond the existing small-world seed: `0`
- Already-covered checklist rows: `67`
- Official-retained rows: `74`
- Rejected placeholder rows: `0`
- Blocked branch-only residual rows: `56` queue rows

## Row Hash Coverage

These hashes are the normalized row identifiers from the generated review
queue. Together they cover every non-identical Auroria row present in the queue
for this pass.

| Delta group | Decision | Row hashes |
| --- | --- | --- |
| Covered branch checklist `entity` rows | Covered | `00a63d27b4acd8da`, `024ae9a4b257eb97`, `0827a449907a1d70`, `0fdf70e1ec0ef395`, `13132d7c76e7a051`, `15da48ae275176ab`, `15e42ee625565ea9`, `24041fe473ccbdc6`, `2ae12d0c8d4bad99`, `2b36b6c66ce41142`, `2bfd1f5587b22e67`, `2d6db6a2367d1118`, `32e61aa23b2d572f`, `357bc837085e78ce`, `3713b8543a8c907e`, `3a40cbb0b7494b2e`, `3c34113a6bf2e5cf`, `3e30a96f28160608`, `3f71f8ad62e4d128`, `434305c31b69817f`, `4679e88fefa1d5ba`, `4877c1a4f044673a`, `530239f1e4a0c7fe`, `5463c44b0f968fc3`, `5e0cdc3fff1fd08a`, `64c28bdac57d7aa3`, `685fabf46e4cd860`, `6fcc099687fa45a8`, `6fec311ee045bee8`, `71f67b537af7a0c8`, `7655e267a1ff2906`, `76c64412e21e87f4`, `772322eadefe8dab`, `7f8da86e6beaab8f`, `8805cfbb921f179d`, `882439193f2240b5`, `90d6d6bc26b65ed2`, `9288fe9ad1f4cd5b`, `930d90a1d5e0c7eb`, `943e5538bd7b4095`, `9638ad4abab5350a`, `9a593f094d1c7710`, `a6e0bbbf6ab25649`, `aa34d2de4e11f11c`, `aa94d6b564ed910c`, `ac9d96c160b646c5`, `af9dd192a2d52736`, `b4688c577ecc1470`, `b7d4b016b15e4d68`, `b7e1ee452d2ce10d`, `ba8ae3b6aef48719`, `bacda444cacfcbef`, `bbea1d787f542b0f`, `bf54d8067550591f`, `c14b92c74d9fc03a`, `c3ef2abf6173b59b`, `c90b4ec79757d400`, `cc05a39a9bc225aa`, `cefc48a2518e2103`, `d837032158230d4a`, `e42495a11a848a08`, `e7a7d462fee33aa8`, `e8e91004461b3401`, `f27b398bc79bade4`, `f7fa07059c58f540`, `f88830f72395c899`, `fa146206c73ac5b9` |
| Branch-only residual `entity` rows | Blocked | `05926fa913094da3`, `09c72d2801dc886d`, `1b743b6b15f6f5e7`, `1c427471e141aa57`, `1c62a113c128fe74`, `25cb68397d3dcf8b`, `2702ec0c2dfe0f4c`, `351b4c8e545fc9bd`, `40227f40fb3679ba`, `49645544fe3b5a89`, `4ab4b78f0f0f44a9`, `506f59ef93ac3963`, `571fd3f392e46f51`, `5c08f9e41daf41b1`, `62dffcc9e98da781`, `639e1652482b96c3`, `67a211e4894f75fa`, `6ab188e8f99e6c57`, `7168ec5481038e02`, `735431a2aa2cf62b`, `7fc790b4be94619e`, `8ad98a664dfd2071`, `8de7c6de98c100f0`, `91d609b8dcedb535`, `97acd50246f16da4`, `a061485ba45fec63`, `ad7027bf2a1d8631`, `b18789a7c61c53cf`, `b640aeeb97dded0a`, `b6520bcdb93274dc`, `b67a9bcff02e30e6`, `b68053f1c0319baa`, `bdc845800e852d3c`, `c4c4f83b595c2f16`, `c4df4db83cabf976`, `cb49c73e11d2bd5c`, `d4f5b41b9eb8aaaf`, `da1b837f451e35e4`, `e75b4d59feb51509`, `f67075dca517f1d6`, `fc0e75501ba006c5` |
| Branch-only `entity_stats` rows | Blocked | `025a70689044a276`, `0f2579cfeea18f80`, `1280ecdb5f8870a0`, `2c67a841b2eb3f61`, `487178efa17e599b`, `5e24b729d4d02bbb`, `5e878001c516df64`, `6265e7545a40db52`, `7e28e1402775d15d`, `98707d5618c27b6b`, `a08f5abd3310927f`, `a4842a79aaf4734d`, `baa8b7a975c228e4`, `e7651fc5aa209cdd`, `fb2f0d6fef695468` |
| Official-only `entity` rows | Official-retained | `03403d37f8ac3517`, `0a9e64186817beb4`, `0b625c23b4873c09`, `0d2f030afa229ba2`, `11d0b5bf6f546a59`, `15f35cec2458703d`, `1af8136b3a2edbe3`, `1b15942568178040`, `1b8139f144a2f9c2`, `1e8a2898b7a7af85`, `20b189e02af09bd1`, `26ff43fdf26675a2`, `27b4716e261c65e4`, `2a92ae5fa43f829a`, `2cc4919c0a29d72b`, `2efedbb28d79b5bb`, `32b71774eaea0039`, `3e2def0c7463f40c`, `407fa6d17e517355`, `40ddb04d1e988cab`, `418ccad1bf373159`, `44bad80b967b9165`, `49757ef3e545b9e6`, `49ea5857ca26ad8a`, `50efaf0085edaaad`, `51b7c2a81605d206`, `59f2aa1715611189`, `610dad24d3f2efb3`, `68ce15ddd7b22ae3`, `7029a2c4cf039ad0`, `7120e71fef26acec`, `790050b929bb2ea5`, `7ce22d068c1e77bf`, `7dac798051685fed`, `7ddb45ae6a5def49`, `814c640d00ff7abb`, `839a65e734e8cd80`, `84ec5b4e444991b6`, `85f6487d486af4f4`, `8971e219933f7917`, `8c3c7255a589ae03`, `8da5e64f2137da1e`, `93aedc4854bd774a`, `941fb8da417a710d`, `9a3192ea7b3593d1`, `9e4ef7068752cd68`, `a378d45e03d9abe5`, `a62ddd86642260a6`, `a93277efa7ac18d1`, `aa076495e57e5a8b`, `aa1c178fd827ef34`, `ab2e0dd496f26989`, `aeacd69953bc978e`, `b498c73299dab5cb`, `b701e0c51bf384a6`, `ba76ea370bfd937e`, `be76bdf8d19bcc88`, `c677a75e515a9dc6`, `c8e5f7a7981f79f7`, `cc95b24798042df8`, `d3c47f01372fefe2`, `d8103e2906e727aa`, `d93c2c96e92985d7`, `e4a2857390f50121`, `e80696d2802722e4`, `ec39886259d49e25`, `f1c368880549b243`, `f1fe72fa9e5796ac`, `f243733e26ed7d31`, `f27b06c3b65d8687`, `f3633dc64d5e589a`, `f551e0b96ebc1b44`, `f561d5fde18a91be`, `fd85bf5d07ae97e5` |

Next allowed states:

- Keep `Map/Open World/Olyssia/Auroria.sql` classified as
  `covered_by_laughingws_small_world_wip_seed;blocked_laughingws_auroria_residual_rows`.
- Reopen only for a specific residual row if a future pass provides current
  script need, quest objective proof, official table corroboration, or local
  retail/client smoke with exact coordinates and row identity.
