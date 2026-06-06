# WildStar Wiki Archive Audit

- Archive: `I:\GIT\NexusForever\artifacts\wildstar-fandom-wiki-2026-05-18`
- Client SQL: `I:\GIT\NexusForever\wildstar_client_mysql`
- Domains: amps

## AMP Wiki Page Coverage

- AMP infobox pages: 208
- Removed AMP pages: 1

| Branch | Wiki AMP pages | Result |
| --- | --- | --- |
| Assault | 32 | PASS |
| Hybrid A/S | 24 | PASS |
| Support | 43 | PASS |
| Hybrid S/U | 27 | PASS |
| Utility | 37 | PASS |
| Hybrid A/U | 30 | PASS |
| Unclassified | 15 | INFO |

| Class | Wiki class AMP category refs | Client class rows | Result |
| --- | --- | --- | --- |
| Warrior | 45 | 87 | PASS |
| Engineer | 84 | 87 | PASS |
| Esper | 84 | 87 | PASS |
| Medic | 84 | 87 | PASS |
| Spellslinger | 84 | 87 | PASS |
| Stalker | 45 | 87 | PASS |
| Global | n/a | 376 | PASS |

## Client EldanAugmentation Data

- Client AMP rows: 910
- Client AMP category rows: 6
- Client AMP id range: 1-976
- Client AMP ids above byte range: 675
- Client AMP ids above ushort range: 0
- Rows with required predecessor AMP: 395
- Rows with inlaid item unlock id: 19
- Rows with category tier lock: 565
- Result: PASS

## Server AMP Id Persistence Surface

| Implementation surface | Present | Result |
| --- | --- | --- |
| Database model stores AmpId as ushort | yes | PASS |
| Database column is unsigned smallint | yes | PASS |
| Client commit packet reads ushort AMP ids | yes | PASS |
| Server AMP list writes ushort AMP ids | yes | PASS |
| ActionSetAmp save preserves ushort AMP ids | yes | PASS |
| ActionSet server list preserves AMP ids | yes | PASS |

## Server AMP Validation Surface

| Implementation surface | Present | Result |
| --- | --- | --- |
| Commit handler validates AMP ids | yes | PASS |
| Commit handler validates AMP power | yes | PASS |
| Commit handler validates AMP class | yes | PASS |
| Commit handler validates AMP series | yes | PASS |
| Action-set change handler validates AMP ids | yes | PASS |
| Action-set change handler validates AMP power | yes | PASS |
| Action-set change handler validates AMP class | yes | PASS |
| Action-set change handler validates AMP series | yes | PASS |

- Result: INFO, id/power/class/series validation is covered; inlaid unlock and category-tier validation remain blocked.

## Summary

Result: PASS

Warnings:
- 15 wiki AMP page(s) have no audited branch value
- client EldanAugmentation rows include inlaid item unlock ids; learned inlaid AMP state remains blocked
- client EldanAugmentation rows include category tier locks; tier unlock spending rules remain blocked
