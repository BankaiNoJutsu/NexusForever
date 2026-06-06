# WildStar Wiki Archive Audit

- Archive: `I:\GIT\NexusForever\artifacts\wildstar-fandom-wiki-2026-05-18`
- Client SQL: `I:\GIT\NexusForever\wildstar_client_mysql`
- Domains: housing

## Housing Wiki Coverage

| Wiki fact | Present | Result |
| --- | --- | --- |
| Housing page documents level 14 unlock | yes | PASS |
| Housing page documents rested XP | yes | PASS |
| Housing page documents all five decor bonus categories | yes | PASS |
| Housing page documents sockets and plugs | yes | PASS |
| Housing page documents visitor rules | yes | PASS |
| Housing page documents housing expeditions | yes | PASS |
| Housing page documents communities | yes | PASS |
| Customizing page exposes room-configuration sections | yes | PASS |
| Decor bonus categories found | Ambience, Aroma, Comfort, Lighting, Pride | PASS |
| Room-configuration sections found | 5 | PASS |

## Client Housing Table Coverage

| Client table | Rows | Result |
| --- | --- | --- |
| HousingResidenceInfo | 23 | PASS |
| HousingPropertyInfo | 65 | PASS |
| HousingMapInfo | 7 | PASS |
| HousingPlotInfo | 420 | PASS |
| HousingPlotType | 27 | PASS |
| HousingPlugItem | 514 | PASS |
| HousingDecorInfo | 3248 | PASS |
| HousingDecorLimitCategory | 4 | PASS |
| HousingWallpaperInfo | 280 | PASS |
| HousingNeighborhoodInfo | 13 | PASS |

## Client Housing Data Details

- Residences with default decor or wallpaper: 20
- Plot rows with default plug items: 28
- Plug rows with upgrade chain: 33
- Plug rows with contribution cost ids: 364
- Plug rows with upkeep fields: 263
- Decor rows with active-prop creature ids: 684
- Decor rows with unlock prerequisites: 130
- Decor rows with limit categories: 473
- Decor rows with interior buff spells: 16
- Decor/campfire `RestedXpDecorBonus` spell-effect rows: 25
- Decor interior buff spells using `RestedXpDecorBonus`: 15
- Decor scale min range: 0.2-1
- Decor scale max range: 0.9-16

## Server Housing Runtime Surface

| Implementation surface | Present | Result |
| --- | --- | --- |
| Residence validates and saves decor/wallpaper game-table ids | yes | PASS |
| Residence creates plots from HousingPlotInfo rows | yes | PASS |
| Plot default plugs use HousingPlugItem rows | yes | PASS |
| Decor serializes ServerHousingResidenceDecor payloads | yes | PASS |
| Client decor update packet reads DecorInfo records | yes | PASS |
| WorldServer routes decor updates into ResidenceMapInstance | yes | PASS |
| Client plug update packet reads mapped plug fields | yes | PASS |
| WorldServer routes plug updates into ResidenceMapInstance | yes | PASS |
| Plug placement validates plot and plug table rows | yes | PASS |
| Unsupported plug cost/prereq/runtime paths are explicitly bounded | yes | PASS |
| Interior wallpaper update packet is parsed | yes | PASS |
| Interior wallpaper update is diagnostic-only until persistence is mapped | no | FAIL |

## Housing Rested-XP Surface

| Implementation surface | Present | Result |
| --- | --- | --- |
| RestBonusXp is persisted on the character model | yes | PASS |
| ServerPlayerCreate sends RestBonusXp | yes | PASS |
| ServerExperienceGained sends RestXpAmount | yes | PASS |
| Kill creature XP consumes the rested pool | yes | PASS |
| Direct ModifyRestedXP effect modifies rested pool | yes | PASS |
| RestedXpDecorBonus has no mutating handler | yes | PASS |
| Decompile notes record RestedXpDecorBonus as blocked | yes | PASS |

- Result: INFO, residence/plot/plug/decor surfaces are table-backed; `RestedXpDecorBonus` remains mapped-only/blocked.

## Summary

Result: FAIL
- server housing runtime surface missing marker: Interior wallpaper update is diagnostic-only until persistence is mapped

Warnings:
- wiki housing scale text says five times bigger/smaller, but client HousingDecorInfo allows broader per-row scale data
- client decor/campfire buff spells include RestedXpDecorBonus rows; accrual timing, stacking, and persistence remain blocked by decompile evidence
- base housing rested-XP login accrual exists in server code, but decor/category multipliers remain blocked until stronger client/runtime evidence is mapped
