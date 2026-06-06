# WildStar Wiki Archive Audit

- Archive: `I:\GIT\NexusForever\artifacts\wildstar-fandom-wiki-2026-05-18`
- Client SQL: `I:\GIT\NexusForever\wildstar_client_mysql`
- Domains: lore

## Lore Wiki Coverage

| Wiki fact | Present | Result |
| --- | --- | --- |
| Lore page links Galactic Archive and Zone Lore | yes | PASS |
| Codex page lists log panes | yes | PASS |
| Datacube page documents gameplay and Scientist path use | yes | PASS |
| Galactic Archive page documents unlockable articles | yes | PASS |
| Zone Lore pages exist | yes | PASS |
| Datacube entry pages exist | yes | PASS |
| Galactic Archive topic rows exist | yes | PASS |
| Zone Lore pages found | 57 | PASS |
| Datacube entry pages found | 248 | PASS |
| Datacube decryption pages found | 5 | PASS |
| Galactic Archive topic rows found | 107 | PASS |

## Client Lore Table Coverage

| Client table | Rows | Result |
| --- | --- | --- |
| Datacube | 765 | PASS |
| DatacubeVolume | 401 | PASS |
| PathScientistDatacubeDiscovery | 17 | PASS |
| ArchiveArticle | 251 | PASS |
| ArchiveEntry | 796 | PASS |
| ArchiveEntryUnlockRule | 867 | PASS |
| ArchiveLink | 10 | PASS |
| ArchiveCategory | 12 | PASS |
| StoryPanel | 2413 | PASS |

## Client Lore Data Details

- Client Datacube rows: 765
- Datacube type ids: 0:294, 1:45, 2:426
- Datacubes with audio events: 307
- Datacubes with world-zone ids: 749
- Datacubes with world-location ids: 238
- Datacubes with unlock counts: 45
- Datacubes with quest-direction ids: 17
- DatacubeVolume rows: 401
- DatacubeVolume datacube references: 425
- ArchiveArticle rows: 251
- ArchiveArticle rows with entry refs: 251
- ArchiveArticle rows with title rewards: 2
- ArchiveEntry rows: 796
- ArchiveEntry rows with Scientist text: 5
- ArchiveEntry rows with title rewards: 0
- ArchiveEntryUnlockRule type ids: 0:234, 1:208, 2:425
- StoryPanel rows: 2413
- StoryPanel rows with sound events: 172
- StoryPanel rows with prerequisites: 32

## Server Lore Runtime Surface

| Implementation surface | Present | Result |
| --- | --- | --- |
| Character datacube model persists type/id/progress | yes | PASS |
| CharacterContext maps CharacterDatacube rows | yes | PASS |
| DatacubeManager validates Datacube and DatacubeVolume rows | yes | PASS |
| DatacubeManager idempotently merges repeated progress | yes | PASS |
| DatacubeManager sends initial datacube and volume packets | yes | PASS |
| Datacube update packets write 14-bit ids and progress | yes | PASS |
| SimpleEntity activation grants datacubes and journal volumes | yes | PASS |
| Datacube save tracks create/progress | yes | PASS |
| Galactic Archive client unlock/view packets are parsed | yes | PASS |
| Galactic Archive update/refresh packets exist | yes | PASS |
| Galactic Archive state persists unlock/view flags | yes | PASS |
| Galactic Archive rule unlocks and title rewards have server hooks | yes | PASS |
| StoryPanel rows can drive story panels | yes | PASS |
| Story messages support creature/localised/player actors | yes | PASS |
| Archive article chat formatter preserves article ids | yes | PASS |

- Result: INFO, datacube/journal persistence, activation, progress packets, story-panel display, Galactic Archive unlock/view persistence, achievement and quest archive rules, archive title rewards, and archive packet models are covered; PathMission-backed type-1 archive rules remain blocked on server-owned mission progress/completion state, and Scientist discovery missions, StoryPanel prerequisites, and some datacube routing semantics remain blocked.

## Summary

Result: PASS

Warnings:
- ArchiveEntryUnlockRule type 1 maps to PathMission ids, but server path-mission completion/progress persistence remains blocked; client 0x00F9 path progress reports carry the PathMission ObjectId/PowerMap id, not trusted completion state
- Datacube rows include unlock-count fields; server currently tracks progress flags but not client unlock-count semantics
- Datacube rows include quest-direction ids; activation routing/waypoint behavior remains blocked
- PathScientistDatacubeDiscovery rows exist, but Scientist datacube discovery mission completion is not mapped to runtime code
- StoryPanel rows include prerequisites; StoryBuilder does not enforce prerequisite ids
